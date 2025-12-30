using System.Diagnostics;
using Functorium.Applications.Observabilities;

namespace Functorium.Adapters.Observabilities.Metrics;

public class AdapterMetricAggregator : IAdapterMetric
{
    private readonly IEnumerable<IAdapterMetric> _metrics;

    public AdapterMetricAggregator(IEnumerable<IAdapterMetric> metrics)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public void Request(Activity? activity, string requestCategory, string requestHandler, string requestHandlerMethod, DateTimeOffset startTimestamp)
    {
        Activity? result = activity;
        foreach (IAdapterMetric metric in _metrics)
        {
            metric.Request(result, requestCategory, requestHandler, requestHandlerMethod, startTimestamp);
        }
    }

    public void ResponseSuccess(Activity? activity, string requestCategory, double elapsed)
    {
        foreach (IAdapterMetric metric in _metrics)
        {
            metric.ResponseSuccess(activity, requestCategory, elapsed);
        }
    }

    public void ResponseFailure(Activity? activity, string requestCategory, double elapsed, Error error)
    {
        foreach (IAdapterMetric metric in _metrics)
        {
            metric.ResponseFailure(activity, requestCategory, elapsed, error);
        }
    }
}