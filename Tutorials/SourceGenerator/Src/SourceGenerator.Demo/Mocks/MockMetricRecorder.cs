using Functorium.Applications.Observabilities.Metrics;

namespace SourceGenerator.Demo.Mocks;

/// <summary>
/// IMetricRecorder Mock 구현체.
/// 실제로는 OpenTelemetry Metrics를 사용하여 메트릭을 기록합니다.
/// </summary>
public class MockMetricRecorder : IMetricRecorder
{
    public void RecordRequest(
        string category,
        string handler,
        string method)
    {
        Console.WriteLine($"[METRICS] RecordRequest: {category}/{handler}.{method}");
    }

    public void RecordResponseSuccess(
        string category,
        string handler,
        string method,
        double elapsedMs)
    {
        Console.WriteLine($"[METRICS] RecordResponseSuccess: {category}/{handler}.{method} - {elapsedMs:F2}ms");
    }

    public void RecordResponseFailure(
        string category,
        string handler,
        string method,
        double elapsedMs,
        LanguageExt.Common.Error error)
    {
        Console.WriteLine($"[METRICS] RecordResponseFailure: {category}/{handler}.{method} - {elapsedMs:F2}ms - {error}");
    }
}
