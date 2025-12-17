using System.Diagnostics;

using Functorium.Applications.Observabilities;

namespace SourceGenerator.Demo.Mocks;

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
        Console.WriteLine($"[METRICS] Request: {requestCategory}/{requestHandler}.{requestHandlerMethod}");
    }

    public void ResponseSuccess(Activity? activity, string requestCategory, double elapsedMs)
    {
        Console.WriteLine($"[METRICS] Response Success: {requestCategory} - {elapsedMs:F2}ms");
    }

    public void ResponseFailure(Activity? activity, string requestCategory, double elapsedMs, LanguageExt.Common.Error error)
    {
        Console.WriteLine($"[METRICS] Response Failure: {requestCategory} - {elapsedMs:F2}ms - {error}");
    }
}
