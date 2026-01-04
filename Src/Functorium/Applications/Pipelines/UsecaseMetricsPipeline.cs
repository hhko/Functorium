using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Abstractions;
using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities;
using Functorium.Applications.Cqrs;
using Functorium.Applications.Observabilities;

using LanguageExt.Common;

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

        // 응답 태그 생성 (성공/실패 + 에러 정보)
        TagList responseTags = CreateResponseTags(requestTags, response);
        responseCounter.Add(1, responseTags);

        return response;
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

    /// <summary>
    /// 에러에서 타입과 코드 정보를 추출합니다.
    /// ManyErrors의 경우 대표 에러를 선정합니다 (Exceptional 우선).
    /// </summary>
    private static (string ErrorType, string ErrorCode) GetErrorInfo(Error error)
    {
        return error switch
        {
            ManyErrors many => (
                ErrorType: ObservabilityNaming.ErrorTypes.Aggregate,
                ErrorCode: GetPrimaryErrorCode(many)
            ),
            ErrorCodeExceptional exceptional => (
                ErrorType: ObservabilityNaming.ErrorTypes.Exceptional,
                ErrorCode: exceptional.ErrorCode
            ),
            ErrorCodeExpected expected => (
                ErrorType: ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: expected.ErrorCode
            ),
            ErrorCodeExpected<string> expectedT => (
                ErrorType: ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: expectedT.ErrorCode
            ),
            _ when error.GetType().Name.StartsWith("ErrorCodeExpected") => (
                ErrorType: ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: GetErrorCodeByReflection(error)
            ),
            _ => (
                ErrorType: error.IsExceptional
                    ? ObservabilityNaming.ErrorTypes.Exceptional
                    : ObservabilityNaming.ErrorTypes.Expected,
                ErrorCode: error.GetType().Name
            )
        };
    }

    /// <summary>
    /// 리플렉션을 사용하여 ErrorCode 속성을 추출합니다.
    /// ErrorCodeExpected&lt;T&gt;, ErrorCodeExpected&lt;T1, T2&gt;, ErrorCodeExpected&lt;T1, T2, T3&gt; 등
    /// 제네릭 타입에서 ErrorCode 속성을 가져옵니다.
    /// </summary>
    private static string GetErrorCodeByReflection(Error error)
    {
        var errorCodeProperty = error.GetType().GetProperty("ErrorCode");
        if (errorCodeProperty?.GetValue(error) is string code)
        {
            return code;
        }
        return error.GetType().Name;
    }

    /// <summary>
    /// ManyErrors에서 대표 에러 코드를 선정합니다.
    /// 우선순위: Exceptional > Expected > First
    /// </summary>
    private static string GetPrimaryErrorCode(ManyErrors many)
    {
        // 1순위: Exceptional 에러 (시스템 에러가 더 심각)
        foreach (var e in many.Errors)
        {
            if (e.IsExceptional)
                return GetErrorCode(e);
        }

        // 2순위: 첫 번째 에러
        return many.Errors.Head.Match(
            Some: GetErrorCode,
            None: () => nameof(ManyErrors));
    }

    /// <summary>
    /// 단일 에러에서 에러 코드를 추출합니다.
    /// </summary>
    private static string GetErrorCode(Error error)
    {
        return error switch
        {
            ErrorCodeExceptional exceptional => exceptional.ErrorCode,
            ErrorCodeExpected expected => expected.ErrorCode,
            ErrorCodeExpected<string> expectedT => expectedT.ErrorCode,
            _ when error.GetType().Name.StartsWith("ErrorCodeExpected") => GetErrorCodeByReflection(error),
            _ => error.GetType().Name
        };
    }
}
