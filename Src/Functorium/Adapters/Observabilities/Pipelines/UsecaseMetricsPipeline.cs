using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Abstractions;
using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Configurations;
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
    private readonly SloTargets _sloTargets;

    public UsecaseMetricsPipeline(
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        IMeterFactory meterFactory,
        IOptions<SloConfiguration> sloConfigurationOptions)
    {
        OpenTelemetryOptions otelOptions = openTelemetryOptions.Value;
        string meterName = $"{otelOptions.ServiceNamespace}.{ObservabilityNaming.Layers.Application}";
        _meter = meterFactory.Create(meterName);

        // 핸들러 정보 미리 계산 (제네릭 타입 기반)
        _requestCqrs = GetRequestCqrs(typeof(TRequest));
        string requestCqrsField = _requestCqrs.ToLower();
        _requestHandler = GetRequestHandler();

        // Handler별 SLO 타겟 조회 (캐시)
        SloConfiguration sloConfiguration = sloConfigurationOptions.Value;
        _sloTargets = sloConfiguration.GetTargetsForHandler(_requestHandler, requestCqrsField);

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
        double elapsedMs = ElapsedTimeCalculator.CalculateElapsedMilliseconds(startTimestamp);

        // SLO 상태 판단 (3단계: ok, p95_exceeded, p99_exceeded)
        string sloLatencyStatus = GetSloLatencyStatus(elapsedMs, _sloTargets);

        // Duration 태그 생성 (requestTags + slo.latency 태그)
        TagList durationTags = CreateDurationTags(requestTags, sloLatencyStatus);

        // Histogram에 처리 시간 기록 (밀리초를 초로 변환, 캐시된 Histogram 사용)
        _durationHistogram.Record(elapsedMs / 1000.0, durationTags);

        // 응답 태그 생성 (성공/실패 + 에러 정보 + slo.latency)
        TagList responseTags = CreateResponseTags(requestTags, response, sloLatencyStatus);
        _responseCounter.Add(1, responseTags);

        return response;
    }

    /// <summary>
    /// Duration histogram 태그를 생성합니다.
    /// requestTags + slo.latency
    /// </summary>
    private static TagList CreateDurationTags(TagList requestTags, string sloLatencyStatus)
    {
        TagList tags = new();
        foreach (var tag in requestTags)
        {
            tags.Add(tag);
        }
        tags.Add(ObservabilityNaming.CustomAttributes.SloLatency, sloLatencyStatus);
        return tags;
    }

    /// <summary>
    /// SLO 지연시간 상태를 판단합니다.
    /// ok: elapsed &lt;= P95
    /// p95_exceeded: P95 &lt; elapsed &lt;= P99
    /// p99_exceeded: elapsed &gt; P99
    /// </summary>
    private static string GetSloLatencyStatus(double elapsedMs, SloTargets targets)
    {
        double p95 = targets.LatencyP95Milliseconds ?? 500;
        double p99 = targets.LatencyP99Milliseconds ?? 1000;

        if (elapsedMs > p99)
            return ObservabilityNaming.SloStatus.P99Exceeded;

        if (elapsedMs > p95)
            return ObservabilityNaming.SloStatus.P95Exceeded;

        return ObservabilityNaming.SloStatus.Ok;
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }

    /// <summary>
    /// 응답 태그를 생성합니다.
    /// 성공 시: requestTags + slo.latency + response.status
    /// 실패 시: requestTags + slo.latency + response.status + error.type + error.code
    /// </summary>
    private static TagList CreateResponseTags(TagList requestTags, TResponse response, string sloLatencyStatus)
    {
        // requestTags 복사
        TagList tags = new();
        foreach (var tag in requestTags)
        {
            tags.Add(tag);
        }

        // SLO 상태 태그 추가
        tags.Add(ObservabilityNaming.CustomAttributes.SloLatency, sloLatencyStatus);

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
