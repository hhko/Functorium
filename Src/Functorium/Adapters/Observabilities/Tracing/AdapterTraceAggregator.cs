using System.Diagnostics;
using Functorium.Applications.Observabilities;

namespace Functorium.Adapters.Observabilities.Tracing;

public class AdapterTraceAggregator : IAdapterTrace
{
    private readonly IEnumerable<IAdapterTrace> _traces;

    public AdapterTraceAggregator(IEnumerable<IAdapterTrace> traces)
    {
        _traces = traces ?? throw new ArgumentNullException(nameof(traces));
    }

    public Activity? Request(ActivityContext parentContext, string requestCategory, string requestHandler, string requestHandlerMethod, DateTimeOffset startTimestamp)
    {
        Activity? result = null;
        foreach (IAdapterTrace trace in _traces)
        {
            Activity? activity = trace.Request(parentContext, requestCategory, requestHandler, requestHandlerMethod, startTimestamp);
            result ??= activity; // 첫 번째 non-null Activity 사용
        }
        return result;
    }

    public void ResponseSuccess(Activity? activity, double elapsed)
    {
        foreach (IAdapterTrace trace in _traces)
        {
            trace.ResponseSuccess(activity, elapsed);
        }
    }

    public void ResponseFailure(Activity? activity, double elapsed, Error error)
    {
        foreach (IAdapterTrace trace in _traces)
        {
            trace.ResponseFailure(activity, elapsed, error);
        }
    }
}
