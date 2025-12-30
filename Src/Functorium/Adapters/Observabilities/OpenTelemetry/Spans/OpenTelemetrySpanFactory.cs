using System.Diagnostics;
using Functorium.Applications.Observabilities;

namespace Functorium.Adapters.Observabilities.OpenTelemetry;

/// <summary>
/// ActivitySource를 사용하여 Span을 생성하는 ISpanFactory 구현체입니다.
/// </summary>
public sealed class OpenTelemetrySpanFactory : ISpanFactory
{
    private readonly ActivitySource _activitySource;

    public OpenTelemetrySpanFactory(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    public ISpan? CreateSpan(
        string operationName,
        string category,
        string handler,
        string method)
    {
        return CreateChildSpan(null, operationName, category, handler, method);
    }

    public ISpan? CreateChildSpan(
        IObservabilityContext? parentContext,
        string operationName,
        string category,
        string handler,
        string method)
    {
        // 태그 설정
        ActivityTagsCollection tags = new()
        {
            { ObservabilityNaming.Tags.Layer, ObservabilityNaming.Layers.Adapter },
            { ObservabilityNaming.Tags.Category, category },
            { ObservabilityNaming.Tags.Handler, handler },
            { ObservabilityNaming.Tags.Method, method }
        };

        // 부모 컨텍스트 결정
        ActivityContext actualParentContext = DetermineParentContext(parentContext);

        // Activity 생성
        IEnumerable<ActivityLink>? links = null;
        Activity? activity = _activitySource.StartActivity(
            operationName,
            ActivityKind.Internal,
            actualParentContext,
            tags,
            links,
            DateTimeOffset.UtcNow);

        if (activity == null)
            return null;

        return new OpenTelemetrySpan(activity);
    }

    /// <summary>
    /// 부모 컨텍스트를 결정합니다.
    /// </summary>
    /// <remarks>
    /// 우선순위:
    /// 1. AsyncLocal에 저장된 Traverse Activity (FinT의 AsyncLocal 복원 문제 우회)
    /// 2. 명시적으로 전달된 parentContext
    /// 3. Activity.Current
    /// </remarks>
    private static ActivityContext DetermineParentContext(IObservabilityContext? parentContext)
    {
        // 1. AsyncLocal에 저장된 Traverse Activity 우선 사용
        Activity? traverseActivity = ActivityContextHolder.GetCurrentActivity();
        if (traverseActivity != null)
        {
            return traverseActivity.Context;
        }

        // 2. 명시적으로 전달된 parentContext 사용
        if (parentContext is ObservabilityContext otelContext)
        {
            return otelContext.ActivityContext;
        }

        // 3. Activity.Current 폴백
        Activity? currentActivity = Activity.Current;
        if (currentActivity != null)
        {
            return currentActivity.Context;
        }

        return default;
    }
}
