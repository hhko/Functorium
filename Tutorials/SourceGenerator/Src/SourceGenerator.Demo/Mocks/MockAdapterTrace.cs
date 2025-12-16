using System.Diagnostics;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// IAdapterTrace Mock 구현체.
/// 실제로는 OpenTelemetry를 사용하여 트레이싱을 기록합니다.
/// </summary>
public class MockAdapterTrace : IAdapterTrace
{
    public Activity? Request(
        ActivityContext parentContext,
        string requestCategory,
        string requestHandler,
        string requestHandlerMethod,
        DateTimeOffset startTime)
    {
        Console.WriteLine($"[TRACE] Request: {requestCategory}/{requestHandler}.{requestHandlerMethod}");
        return null;
    }

    public void ResponseSuccess(Activity? activity, double elapsedMs)
    {
        Console.WriteLine($"[TRACE] Response Success: {elapsedMs:F2}ms");
    }

    public void ResponseFailure(Activity? activity, double elapsedMs, LanguageExt.Common.Error error)
    {
        Console.WriteLine($"[TRACE] Response Failure: {elapsedMs:F2}ms - {error}");
    }
}
