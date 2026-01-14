using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Abstractions;
using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Cqrs;

using LanguageExt.Common;

using Mediator;
using Microsoft.Extensions.Options;

namespace Functorium.Adapters.Observabilities.Pipelines;

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
internal sealed class UsecaseMetricsPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
    , IDisposable
        where TRequest : IMessage
        where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;
    private readonly string _requestCqrs;
    private readonly string _requestHandler;

    public UsecaseMetricsPipeline(
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        IMeterFactory meterFactory)
    {
        OpenTelemetryOptions otelOptions = openTelemetryOptions.Value;
        string meterName = $"{otelOptions.ServiceNamespace}.{ObservabilityNaming.Layers.Application}";
        _meter = meterFactory.Create(meterName);

        // 핸들러 정보 미리 계산 (제네릭 타입 기반)
        _requestCqrs = GetRequestCqrs(typeof(TRequest));
        string requestCqrsField = _requestCqrs.ToLower();
        _requestHandler = GetRequestHandler();

        // 메트릭 인스턴스 생성 (재사용)
        _requestCounter = _meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.UsecaseRequest(requestCqrsField),
            unit: "{request}",
            description: $"Total number of {_requestHandler} requests");

        _responseCounter = _meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.UsecaseResponse(requestCqrsField),
            unit: "{response}",
            description: $"Total number of {_requestHandler} responses");

        _durationHistogram = _meter.CreateHistogram<double>(
            name: ObservabilityNaming.Metrics.UsecaseDuration(requestCqrsField),
            unit: "s",
            description: $"Duration of {_requestHandler} request processing in seconds");
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        // 요청 태그 생성 (캐시된 정보 사용)
        TagList requestTags = new TagList
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application },
            { ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Usecase },
            { ObservabilityNaming.CustomAttributes.RequestHandlerCqrs, _requestCqrs },
            { ObservabilityNaming.CustomAttributes.RequestHandler, _requestHandler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.Handle }
        };

        // 요청 수 증가 (캐시된 Counter 사용)
        _requestCounter.Add(1, requestTags);

        // 시간 측정 시작
        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        // 다음 파이프라인 실행 (실제 핸들러 포함)
        TResponse response = await next(request, cancellationToken);

        // 시간 측정 종료
        double elapsedSeconds = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

        // Histogram에 처리 시간 기록 (초 단위, 캐시된 Histogram 사용)
        _durationHistogram.Record(elapsedSeconds, requestTags);

        // 응답 태그 생성 (성공/실패 + 에러 정보)
        TagList responseTags = CreateResponseTags(requestTags, response);
        _responseCounter.Add(1, responseTags);

        return response;
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }

    /// <summary>
    /// 응답 태그를 생성합니다.
    /// 성공 시: requestTags + response.status
    /// 실패 시: requestTags + response.status + error.type + error.code
    /// </summary>
    private static TagList CreateResponseTags(TagList requestTags, TResponse response)
    {
        // requestTags 복사
        TagList tags = new();
        foreach (var tag in requestTags)
        {
            tags.Add(tag);
        }

        if (response.IsSucc)
        {
            tags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
            return tags;
        }

        tags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);

        // 실패 시 에러 정보 추가
        if (response is IFinResponseWithError { Error: var error })
        {
            var (errorType, errorCode) = GetErrorInfo(error);
            tags.Add(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
            tags.Add(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
        }

        return tags;
    }
}
