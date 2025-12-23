using System.Diagnostics;

using Functorium.Abstractions;
using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities;
using Functorium.Applications.Cqrs;

using Mediator;

using ObservabilityFields = Functorium.Adapters.Observabilities.ObservabilityFields;

namespace Functorium.Applications.Pipelines;

public sealed class UsecaseTracePipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse<IResponse>
{
    private readonly ActivitySource _activitySource;
    //private static readonly ActivitySource _activitySource = new(typeof(UsecaseTracePipeline<,>).Namespace!);

    public UsecaseTracePipeline(ActivitySource activitySource)
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
            ? _activitySource.StartActivity($"{ObservabilityFields.Request.Layer.Application} {ObservabilityFields.Request.Category.Usecase}.{requestCqrs} {requestHandler}.{requestHandlerMethod}", ActivityKind.Internal, parentActivity.Context)
            : _activitySource.StartActivity($"{ObservabilityFields.Request.Layer.Application} {ObservabilityFields.Request.Category.Usecase}.{requestCqrs} {requestHandler}.{requestHandlerMethod}");

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
        activity.SetTag(ObservabilityFields.Request.TelemetryKeys.Layer, ObservabilityFields.Request.Layer.Application);
        activity.SetTag(ObservabilityFields.Request.TelemetryKeys.Category, ObservabilityFields.Request.Category.Usecase);
        activity.SetTag(ObservabilityFields.Request.TelemetryKeys.HandlerCqrs, requestCqrs);
        activity.SetTag(ObservabilityFields.Request.TelemetryKeys.Handler, requestHandler);
        //activity.SetTag("request.handler.path", requestHandlerPath);
    }

    private static void SetResponseTags(Activity activity, TResponse response, double elapsed)
    {
        SetTimeTags(activity, elapsed);
        SetStatusTags(activity, response);
    }

    private static void SetTimeTags(Activity activity, double elapsed)
    {
        activity.SetTag(ObservabilityFields.Response.TelemetryKeys.Elapsed, elapsed);
    }

    private static void SetStatusTags(Activity activity, TResponse response)
    {
        if (response.IsSucc)
        {
            SetSuccessStatus(activity);
        }
        else
        {
            SetFailureStatus(activity, response.Error);
        }
    }

    private static void SetSuccessStatus(Activity activity)
    {
        activity.SetTag(ObservabilityFields.Response.TelemetryKeys.Status, ObservabilityFields.Response.Status.Success);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void SetFailureStatus(Activity activity, Error? error)
    {
        activity.SetTag(ObservabilityFields.Response.TelemetryKeys.Status, ObservabilityFields.Response.Status.Failure);
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
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Type, nameof(ErrorCodeExpected));
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Code, error.ErrorCode);
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Message, error.Message);
    }

    private static void SetErrorCodeExceptionalTags(Activity activity, ErrorCodeExceptional error)
    {
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Type, nameof(ErrorCodeExceptional));
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Code, error.ErrorCode);
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Message, error.Message);
    }

    private static void SetManyErrorsTags(Activity activity, ManyErrors error)
    {
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Type, nameof(ManyErrors));
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Count, error.Errors.Count);
    }

    private static void SetUnknownErrorTags(Activity activity, Error error)
    {
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Type, error.GetType().Name);
        activity.SetTag(ObservabilityFields.Errors.TelemetryKeys.Message, error.Message);
    }
}