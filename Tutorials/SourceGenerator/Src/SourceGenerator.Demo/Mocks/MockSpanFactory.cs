using Functorium.Adapters.Observabilities.Abstractions;

namespace SourceGenerator.Demo.Mocks;

/// <summary>
/// ISpanFactory Mock 구현체.
/// 실제로는 OpenTelemetry를 사용하여 트레이싱을 기록합니다.
/// </summary>
public class MockSpanFactory : ISpanFactory
{
    public ISpan? CreateSpan(
        string operationName,
        string category,
        string handler,
        string method)
    {
        Console.WriteLine($"[TRACING] CreateSpan: {operationName} - {category}/{handler}.{method}");
        return new MockSpan(operationName);
    }

    public ISpan? CreateChildSpan(
        IObservabilityContext? parentContext,
        string operationName,
        string category,
        string handler,
        string method)
    {
        Console.WriteLine($"[TRACING] CreateChildSpan: {operationName} - {category}/{handler}.{method}");
        return new MockSpan(operationName);
    }

    private class MockSpan : ISpan
    {
        private readonly string _operationName;

        public MockSpan(string operationName)
        {
            _operationName = operationName;
            SpanId = Guid.NewGuid().ToString("N")[..16];
            TraceId = Guid.NewGuid().ToString("N");
        }

        public string SpanId { get; }
        public string TraceId { get; }
        public IObservabilityContext Context => new MockObservabilityContext();

        public void SetTag(string key, object? value)
        {
            Console.WriteLine($"[TRACING] SetTag: {key}={value}");
        }

        public void SetSuccess(double? elapsedMs = null)
        {
            Console.WriteLine($"[TRACING] SetSuccess: {_operationName} - {elapsedMs:F2}ms");
        }

        public void SetFailure(string message, double? elapsedMs = null)
        {
            Console.WriteLine($"[TRACING] SetFailure: {_operationName} - {message} - {elapsedMs:F2}ms");
        }

        public void SetFailure(LanguageExt.Common.Error error, double? elapsedMs = null)
        {
            Console.WriteLine($"[TRACING] SetFailure: {_operationName} - {error} - {elapsedMs:F2}ms");
        }

        public void Dispose()
        {
            Console.WriteLine($"[TRACING] Dispose: {_operationName}");
        }
    }

    private class MockObservabilityContext : IObservabilityContext
    {
        public string TraceId => Guid.NewGuid().ToString("N");
        public string SpanId => Guid.NewGuid().ToString("N")[..16];
    }
}
