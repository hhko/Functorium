using System.Diagnostics;
using Functorium.Applications.Observabilities;

namespace Functorium.Adapters.Observabilities.Tracing;

public class AdapterTrace : IAdapterTrace
{
    private readonly ActivitySource _activitySource;

    public AdapterTrace(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    public Activity? Request(ActivityContext parentContext, string requestCategory, string requestHandler, string requestHandlerMethod, DateTimeOffset startTimestamp)
    {
        //Console.WriteLine($"[AdapterTrace.Request] {requestHandler}.{requestHandlerMethod}");

        // 성능 최적화: Activity 생성 시 태그들을 한 번에 설정하여 개별 SetTag 호출 최소화
        ActivityTagsCollection tags = new ActivityTagsCollection
        {
            { ObservabilityFields.Request.TelemetryKeys.Layer,  ObservabilityFields.Request.Layer.Adapter },
            { ObservabilityFields.Request.TelemetryKeys.Category, requestCategory },
            { ObservabilityFields.Request.TelemetryKeys.Handler, requestHandler },
            { ObservabilityFields.Request.TelemetryKeys.HandlerMethod, requestHandlerMethod }
        };

        // AsyncLocal에 저장된 Traverse Activity를 우선 사용
        // (FinT의 AsyncLocal 복원 문제를 우회)
        Activity? traverseActivity = TraceParentActivityHolder.GetCurrent();
        ActivityContext actualParentContext;

        if (traverseActivity != null)
        {
            //Console.WriteLine($"[AdapterTrace.Request] Using AsyncLocal Traverse Activity: {traverseActivity.DisplayName}");
            actualParentContext = traverseActivity.Context;
        }
        else
        {
            //Console.WriteLine($"[AdapterTrace.Request] Using captured parentContext (Usecase)");
            // Traverse Activity가 없으면 캡처된 context 사용 (Usecase Activity가 부모)
            actualParentContext = parentContext;
        }

        Activity? activity = _activitySource.StartActivity(
            name: $"{ObservabilityFields.Request.Layer.Adapter} {requestCategory} {requestHandler}.{requestHandlerMethod}",
            kind: ActivityKind.Internal,
            actualParentContext,
            tags,
            links: null,
            startTime: startTimestamp);

        return activity;
    }

    public void ResponseSuccess(Activity? activity, double elapsed)
    {
        //Console.WriteLine($"[AdapterTrace.ResponseSuccess] Setting tags: {activity?.DisplayName ?? "null"}, Current = {Activity.Current?.DisplayName ?? "null"}");
        activity?.SetTag(ObservabilityFields.Response.TelemetryKeys.Elapsed, elapsed);
        activity?.SetStatus(ActivityStatusCode.Ok);
        //Console.WriteLine($"[AdapterTrace.ResponseSuccess] Tags set: {activity?.DisplayName ?? "null"}, Current = {Activity.Current?.DisplayName ?? "null"}");
    }

    public void ResponseFailure(Activity? activity, double elapsed, Error error)
    {
        activity?.SetTag(ObservabilityFields.Response.TelemetryKeys.Elapsed, elapsed);
        activity?.SetStatus(ActivityStatusCode.Error, error.Message);
    }
}