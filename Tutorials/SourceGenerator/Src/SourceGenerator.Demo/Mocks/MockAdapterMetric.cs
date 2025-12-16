using System.Diagnostics;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// IAdapterMetric Mock 구현체.
/// 실제로는 OpenTelemetry Metrics를 사용하여 메트릭을 기록합니다.
/// </summary>
public class MockAdapterMetric : IAdapterMetric
{
    public void Request(
        Activity? activity,
        string requestCategory,
        string requestHandler,
        string requestHandlerMethod,
        DateTimeOffset startTime)
    {
        Console.WriteLine($"[METRIC] Request: {requestCategory}/{requestHandler}.{requestHandlerMethod}");
    }

    public void ResponseSuccess(Activity? activity, string requestCategory, double elapsedMs)
    {
        Console.WriteLine($"[METRIC] Response Success: {requestCategory} - {elapsedMs:F2}ms");
    }

    public void ResponseFailure(Activity? activity, string requestCategory, double elapsedMs, LanguageExt.Common.Error error)
    {
        Console.WriteLine($"[METRIC] Response Failure: {requestCategory} - {elapsedMs:F2}ms - {error}");
    }
}
