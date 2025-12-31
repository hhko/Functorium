using System.Diagnostics;
using Functorium.Applications.Observabilities.Context;

namespace Functorium.Adapters.Observabilities.Context;

/// <summary>
/// ActivityContext를 래핑하는 IObservabilityContext 구현체입니다.
/// </summary>
internal sealed class ObservabilityContext : IObservabilityContext
{
    private readonly ActivityContext _activityContext;

    private ObservabilityContext(ActivityContext activityContext, string traceId, string spanId)
    {
        _activityContext = activityContext;
        TraceId = traceId;
        SpanId = spanId;
    }

    public string TraceId { get; }
    public string SpanId { get; }

    /// <summary>
    /// 내부 ActivityContext를 반환합니다.
    /// </summary>
    internal ActivityContext ActivityContext => _activityContext;

    /// <summary>
    /// ActivityContext에서 ObservabilityContext를 생성합니다.
    /// </summary>
    public static ObservabilityContext FromActivityContext(ActivityContext context)
    {
        return new ObservabilityContext(
            context,
            context.TraceId.ToString(),
            context.SpanId.ToString());
    }

    /// <summary>
    /// Activity에서 ObservabilityContext를 생성합니다.
    /// </summary>
    public static ObservabilityContext? FromActivity(Activity? activity)
    {
        if (activity == null)
            return null;

        return FromActivityContext(activity.Context);
    }

    /// <summary>
    /// TraceId와 SpanId로 ObservabilityContext를 생성합니다.
    /// </summary>
    public static ObservabilityContext Create(string traceId, string spanId)
    {
        return new ObservabilityContext(default, traceId, spanId);
    }
}
