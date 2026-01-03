using System.Diagnostics;
using Functorium.Adapters.Observabilities.Context;
using Functorium.Applications.Observabilities;
using Functorium.Applications.Observabilities.Context;
using Functorium.Applications.Observabilities.Spans;

namespace Functorium.Adapters.Observabilities.Spans;

/// <summary>
/// ActivitySource를 사용하여 Span을 생성하는 ISpanFactory 구현체입니다.
/// </summary>
/// <remarks>
/// <para>
/// Span 생성 시 태그는 <see cref="TagList"/> 구조체를 사용합니다.
/// <c>ActivityTagsCollection</c> 클래스 대신 구조체를 사용하여
/// 힙 할당을 방지하고 GC 부담을 최소화합니다.
/// </para>
/// </remarks>
public sealed class OpenTelemetrySpanFactory : ISpanFactory
{
    private readonly ActivitySource _activitySource;

    public OpenTelemetrySpanFactory(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    // public ISpan? CreateSpan(
    //     string operationName,
    //     string category,
    //     string handler,
    //     string method)
    // {
    //     return CreateChildSpan(null, operationName, category, handler, method);
    // }

    public ISpan? CreateChildSpan(
        IObservabilityContext? parentContext,
        string operationName,
        string category,
        string handler,
        string method)
    {
        // TagList: 구조체로 스택에 할당되어 GC 부담 최소화
        TagList tags = new()
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
            { ObservabilityNaming.CustomAttributes.RequestCategory, category },
            { ObservabilityNaming.CustomAttributes.RequestHandler, handler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, method }
        };

        ActivityContext actualParentContext = DetermineParentContext(parentContext);

        Activity? activity = _activitySource.StartActivity(
            operationName,
            ActivityKind.Internal,
            actualParentContext,
            tags,
            links: null,
            DateTimeOffset.UtcNow);

        if (activity == null)
            return null;

        return new OpenTelemetrySpan(activity);
    }

    /// <summary>
    /// 부모 컨텍스트를 결정합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 우선순위:
    /// <list type="number">
    ///   <item>Activity.Current - 가장 가까운 부모 (표준 OpenTelemetry 동작)</item>
    ///   <item>AsyncLocal에 저장된 Traverse Activity (FinT의 AsyncLocal 복원 문제 우회)</item>
    ///   <item>명시적으로 전달된 parentContext (HTTP 요청 레벨 폴백)</item>
    /// </list>
    /// </para>
    /// <para>
    /// 기대되는 Trace 계층 구조:
    /// <code>
    /// HttpRequestIn (ROOT)
    /// └── UsecaseActivity (Activity.Current) ← 우선순위 1
    ///     └── AdapterSpan (이 메서드의 결과)
    /// </code>
    /// </para>
    /// </remarks>
    internal static ActivityContext DetermineParentContext(IObservabilityContext? parentContext)
    {
        // 1. Activity.Current - 가장 가까운 부모 (표준 OpenTelemetry 동작)
        Activity? currentActivity = Activity.Current;
        if (currentActivity != null)
            return currentActivity.Context;

        // 2. AsyncLocal에 저장된 Traverse Activity (FinT의 AsyncLocal 복원 문제 우회)
        Activity? traverseActivity = ActivityContextHolder.GetCurrentActivity();
        if (traverseActivity != null)
            return traverseActivity.Context;

        // 3. 명시적으로 전달된 parentContext (HTTP 요청 레벨 폴백)
        if (parentContext is ObservabilityContext otelContext)
            return otelContext.ActivityContext;

        return default;
    }
}
