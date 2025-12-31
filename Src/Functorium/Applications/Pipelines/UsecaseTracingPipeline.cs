using System.Diagnostics;

using Functorium.Abstractions;
using Functorium.Abstractions.Errors;
using Functorium.Applications.Cqrs;
using Functorium.Applications.Observabilities;

using LanguageExt.Common;

using Mediator;

namespace Functorium.Applications.Pipelines;

/// <summary>
/// Result 패턴을 위한 분산 추적 Pipeline.
/// IsSucc/IsFail 패턴을 사용하여 안전하게 추적 정보를 기록합니다.
/// </summary>
public sealed class UsecaseTracingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly ActivitySource _activitySource;

    public UsecaseTracingPipeline(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        string requestCqrs = GetRequestCqrs(request);
        string requestHandler = GetRequestHandler();
        string requestHandlerPath = GetRequestHandlerPath();
        Activity? parentActivity = Activity.Current;

        //
        // AddSource에 사전에 ActivitySource 이름이 등록되어 있어야 정상적으로 객체를 생성할 수 있습니다.
        //
        string requestHandlerMethod = "Handle";
        using Activity? activity = parentActivity != null
            ? _activitySource.StartActivity($"{ObservabilityNaming.Layers.Application} {ObservabilityNaming.Categories.Usecase}.{requestCqrs} {requestHandler}.{requestHandlerMethod}", ActivityKind.Internal, parentActivity.Context)
            : _activitySource.StartActivity($"{ObservabilityNaming.Layers.Application} {ObservabilityNaming.Categories.Usecase}.{requestCqrs} {requestHandler}.{requestHandlerMethod}");

        if (activity == null)
        {
            // Activity 생성 실패 시 추적 없이 다음 Pipeline으로
            return await next(request, cancellationToken);
        }

        SetRequestTags(activity, requestCqrs, requestHandler, requestHandlerPath);
        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        TResponse response = await next(request, cancellationToken);

        double elapsed = ElapsedTimeCalculator.CalculateElapsedMilliseconds(startTimestamp);
        SetResponseTags(activity, response, elapsed);

        return response;
    }

    private static void SetRequestTags(Activity activity, string requestCqrs, string requestHandler, string requestHandlerPath)
    {
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application);
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Usecase);
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerCqrs, requestCqrs);
        activity.SetTag(ObservabilityNaming.CustomAttributes.RequestHandler, requestHandler);
        activity.SetTag(ObservabilityNaming.OTelAttributes.CodeFunction, requestHandlerPath);
    }

    private static void SetResponseTags(Activity activity, TResponse response, double elapsed)
    {
        SetTimeTags(activity, elapsed);
        SetStatusTags(activity, response);
    }

    private static void SetTimeTags(Activity activity, double elapsed)
    {
        activity.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
    }

    private static void SetStatusTags(Activity activity, TResponse response)
    {
        if (response.IsSucc)
        {
            SetSuccessStatus(activity);
        }
        else
        {
            // IFinResponseWithError를 통해 Error 접근
            if (response is IFinResponseWithError errorResponse)
            {
                SetFailureStatus(activity, errorResponse.Error);
            }
            else
            {
                SetFailureStatus(activity, null);
            }
        }
    }

    private static void SetSuccessStatus(Activity activity)
    {
        activity.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void SetFailureStatus(Activity activity, Error? error)
    {
        activity.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
        activity.SetStatus(ActivityStatusCode.Error, error?.Message ?? "Unknown error");

        if (error is not null)
        {
            SetErrorTags(activity, error);
        }
    }

    private static void SetErrorTags(Activity activity, Error error)
    {
        switch (error)
        {
            case ErrorCodeExpected errorCodeExpected:
                SetErrorCodeExpectedTags(activity, errorCodeExpected);
                break;
            case ErrorCodeExceptional errorCodeExceptional:
                SetErrorCodeExceptionalTags(activity, errorCodeExceptional);
                break;
            case ManyErrors manyErrors:
                SetManyErrorsTags(activity, manyErrors);
                break;

            // Expected
            // Exceptional
            default:
                SetUnknownErrorTags(activity, error);
                break;
        }
    }

    private static void SetErrorCodeExpectedTags(Activity activity, ErrorCodeExpected error)
    {
        activity.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, nameof(ErrorCodeExpected));
        activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, error.ErrorCode);
        activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorMessage, error.Message);
    }

    private static void SetErrorCodeExceptionalTags(Activity activity, ErrorCodeExceptional error)
    {
        activity.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, nameof(ErrorCodeExceptional));
        activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, error.ErrorCode);
        activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorMessage, error.Message);
    }

    private static void SetManyErrorsTags(Activity activity, ManyErrors error)
    {
        activity.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, nameof(ManyErrors));
        activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorCount, error.Errors.Count);
    }

    private static void SetUnknownErrorTags(Activity activity, Error error)
    {
        activity.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, error.GetType().Name);
        activity.SetTag(ObservabilityNaming.CustomAttributes.ErrorMessage, error.Message);
    }
}
