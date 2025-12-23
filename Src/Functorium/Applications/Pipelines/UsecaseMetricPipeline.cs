using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Abstractions;
using Functorium.Adapters.Observabilities;
using Functorium.Applications.Cqrs;
using Functorium.Applications.Observabilities;

using Mediator;

using ObservabilityFields = Functorium.Adapters.Observabilities.ObservabilityFields;

namespace Functorium.Applications.Pipelines;

/// <summary>
/// 모든 Usecase에 대해 범용 Metrics를 자동으로 수집하는 Pipeline
/// 핸들러별로 기본 메트릭(요청 수, 응답 수, 처리 시간)을 생성합니다.
///
/// ServiceName 결정 우선순위:
/// 1. appsettings.json의 "Observability:ServiceName" 설정값
/// 2. 설정이 없으면 요청 타입의 어셈블리 이름 (Fallback)
/// </summary>
public sealed class UsecaseMetricPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse<IResponse>
{
    private readonly IMeterFactory _meterFactory;
    private readonly string _meterName;
    //private readonly string _metricPrefix;

    public UsecaseMetricPipeline(IOpenTelemetryOptions openTelemetryOptions, IMeterFactory meterFactory)
    {
        _meterFactory = meterFactory;

        _meterName = $"{openTelemetryOptions.ServiceNamespace}.{ObservabilityFields.Request.Layer.Application}";
        //_metricPrefix = ObservabilityFields.MetricPrefix.Application.Usecase;
    }

    

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        string requestCqrs = GetRequestCqrs(request);
        string requestCqrsField = requestCqrs.ToLower();
        string requestHandler = GetRequestHandler();

        // 핸들러별 Meter 및 Metrics 생성
        using Meter meter = _meterFactory.Create(_meterName);
        //string handlerName = requestHandler.ToLowerInvariant(); //.Replace("query", "_query").Replace("command", "_command");

        //// Counter: 총 요청 수
        //Counter<long> requestCounter = meter.CreateCounter<long>(
        //    name: $"{_metricPrefix}.{requestCqrs}.{handlerName}.requests_total",
        //    unit: CountUnit,
        //    description: $"Total number of {requestHandler} requests");

        //// Counter: 성공 응답 수
        //Counter<long> responseSuccessCounter = meter.CreateCounter<long>(
        //    name: $"{_metricPrefix}.{requestCqrs}.{handlerName}.responses_success_total",
        //    unit: CountUnit,
        //    description: $"Total number of successful {requestHandler} responses");

        //// Counter: 실패 응답 수
        //Counter<long> responseFailureCounter = meter.CreateCounter<long>(
        //    name: $"{_metricPrefix}.{requestCqrs}.{handlerName}.responses_failure_total",
        //    unit: CountUnit,
        //    description: $"Total number of failed {requestHandler} responses");

        //// Histogram: 요청 처리 시간
        //Histogram<double> durationHistogram = meter.CreateHistogram<double>(
        //    name: $"{_metricPrefix}.{requestCqrs}.{handlerName}.duration",
        //    unit: DurationUnit,
        //    description: $"Duration of {requestHandler} request processing in milliseconds");

        // Counter: 총 요청 수 (Prometheus가 자동으로 _total 접미사 추가)
        Counter<long> requestCounter = meter.CreateCounter<long>(
            name: UsecaseFields.Metrics.GetRequest(requestCqrsField), // 이전: $"{_metricPrefix}.{requestCqrsField}.requests"
            unit: "{request}",
            description: $"Total number of {requestHandler} requests");

        // Counter: 성공 응답 수
        Counter<long> responseSuccessCounter = meter.CreateCounter<long>(
            name: UsecaseFields.Metrics.GetResponseSuccess(requestCqrsField), // 이전: $"{_metricPrefix}.{requestCqrsField}.responses.success"
            unit: "{response}",
            description: $"Total number of successful {requestHandler} responses");

        // Counter: 실패 응답 수
        Counter<long> responseFailureCounter = meter.CreateCounter<long>(
            name: UsecaseFields.Metrics.GetResponseFailure(requestCqrsField), // 이전: $"{_metricPrefix}.{requestCqrsField}.responses.failure"
            unit: "{response}",
            description: $"Total number of failed {requestHandler} responses");

        // Histogram: 요청 처리 시간 (초 단위)
        Histogram<double> durationHistogram = meter.CreateHistogram<double>(
            name: UsecaseFields.Metrics.GetDuration(requestCqrsField), // 이전: $"{_metricPrefix}.{requestCqrsField}.duration"
            unit: "s",
            description: $"Duration of {requestHandler} request processing in seconds");

        // Tags 생성 (공통)
        TagList tags = new TagList
        {
            { ObservabilityFields.Request.TelemetryKeys.Layer, ObservabilityFields.Request.Layer.Application },
            { ObservabilityFields.Request.TelemetryKeys.Category, ObservabilityFields.Request.Category.Usecase },
            { ObservabilityFields.Request.TelemetryKeys.HandlerCqrs, requestCqrs },
            { ObservabilityFields.Request.TelemetryKeys.Handler, requestHandler },
            { ObservabilityFields.Request.TelemetryKeys.HandlerMethod, "Handle" }
        };

        // 요청 수 증가
        requestCounter.Add(1, tags);

        // 시간 측정 시작
        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        // 다음 파이프라인 실행 (실제 핸들러 포함)
        TResponse response = await next(request, cancellationToken);

        // 시간 측정 종료
        double elapsed = ElapsedTimeCalculator.CalculateElapsedMilliseconds(startTimestamp);

        // Histogram에 처리 시간 기록 (밀리초를 초로 변환)
        durationHistogram.Record(elapsed / 1000.0, tags);

        // 성공/실패 응답 수 기록
        if (response.IsSucc)
        {
            responseSuccessCounter.Add(1,
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Layer, 
                        ObservabilityFields.Request.Layer.Application),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Category, 
                        ObservabilityFields.Request.Category.Usecase),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.HandlerCqrs, 
                        requestCqrs),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Handler, 
                        requestHandler),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Response.TelemetryKeys.Status, 
                        ObservabilityFields.Response.Status.Success));
        }
        else
        {
            responseFailureCounter.Add(1,
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Layer,
                        ObservabilityFields.Request.Layer.Application),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Category,
                        ObservabilityFields.Request.Category.Usecase),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.HandlerCqrs, 
                        requestCqrs),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Request.TelemetryKeys.Handler, 
                        requestHandler),
                new KeyValuePair<string, object?>(
                        ObservabilityFields.Response.TelemetryKeys.Status, 
                        ObservabilityFields.Response.Status.Failure));
        }

        return response;
    }
}

