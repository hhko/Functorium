using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Abstractions;
using Functorium.Adapters.Observabilities;
using Functorium.Applications.Cqrs;
using Functorium.Applications.Observabilities;

using Mediator;

namespace Functorium.Applications.Pipelines;

/// <summary>
/// 모든 Usecase에 대해 범용 Metrics를 자동으로 수집하는 Pipeline
/// 핸들러별로 기본 메트릭(요청 수, 응답 수, 처리 시간)을 생성합니다.
///
/// ServiceName 결정 우선순위:
/// 1. appsettings.json의 "Observability:ServiceName" 설정값
/// 2. 설정이 없으면 요청 타입의 어셈블리 이름 (Fallback)
///
/// IsSucc/IsFail 패턴을 사용하여 안전하게 메트릭을 기록합니다.
/// </summary>
public sealed class UsecaseMetricsPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly IMeterFactory _meterFactory;
    private readonly string _meterName;

    public UsecaseMetricsPipeline(IOpenTelemetryOptions openTelemetryOptions, IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;
        _meterName = $"{openTelemetryOptions.ServiceNamespace}.{ObservabilityNaming.Layers.Application}";
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        string requestCqrs = GetRequestCqrs(request);
        string requestCqrsField = requestCqrs.ToLower();
        string requestHandler = GetRequestHandler();

        // 핸들러별 Meter 및 Metrics 생성
        using Meter meter = _meterFactory.Create(_meterName);

        // Counter: 총 요청 수 (Prometheus가 자동으로 _total 접미사 추가)
        Counter<long> requestCounter = meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.UsecaseRequest(requestCqrsField),
            unit: "{request}",
            description: $"Total number of {requestHandler} requests");

        // Counter: 응답 수 (성공/실패를 response.status 태그로 구분)
        Counter<long> responseCounter = meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.UsecaseResponse(requestCqrsField),
            unit: "{response}",
            description: $"Total number of {requestHandler} responses");

        // Histogram: 요청 처리 시간 (초 단위)
        Histogram<double> durationHistogram = meter.CreateHistogram<double>(
            name: ObservabilityNaming.Metrics.UsecaseDuration(requestCqrsField),
            unit: "s",
            description: $"Duration of {requestHandler} request processing in seconds");

        // 요청 태그 (requests, duration 공통)
        TagList requestTags = new TagList
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application },
            { ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Usecase },
            { ObservabilityNaming.CustomAttributes.RequestHandlerCqrs, requestCqrs },
            { ObservabilityNaming.CustomAttributes.RequestHandler, requestHandler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, "Handle" }
        };

        // 요청 수 증가
        requestCounter.Add(1, requestTags);

        // 시간 측정 시작
        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        // 다음 파이프라인 실행 (실제 핸들러 포함)
        TResponse response = await next(request, cancellationToken);

        // 시간 측정 종료
        double elapsed = ElapsedTimeCalculator.CalculateElapsedMilliseconds(startTimestamp);

        // Histogram에 처리 시간 기록 (밀리초를 초로 변환)
        durationHistogram.Record(elapsed / 1000.0, requestTags);

        // 응답 수 기록 (requestTags + ResponseStatus로 성공/실패 구분)
        string responseStatus = response.IsSucc
            ? ObservabilityNaming.Status.Success
            : ObservabilityNaming.Status.Failure;

        TagList responseTags = new TagList
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application },
            { ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Usecase },
            { ObservabilityNaming.CustomAttributes.RequestHandlerCqrs, requestCqrs },
            { ObservabilityNaming.CustomAttributes.RequestHandler, requestHandler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, "Handle" },
            { ObservabilityNaming.CustomAttributes.ResponseStatus, responseStatus }
        };
        responseCounter.Add(1, responseTags);

        return response;
    }
}
