namespace Functorium.Applications.Observabilities.Context;

/// <summary>
/// 관찰 가능성 컨텍스트를 나타내는 인터페이스입니다.
/// TraceId와 SpanId를 포함하여 분산 추적을 지원합니다.
/// </summary>
public interface IObservabilityContext
{
    /// <summary>
    /// 추적 ID (Trace ID)를 가져옵니다.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// 스팬 ID (Span ID)를 가져옵니다.
    /// </summary>
    string SpanId { get; }
}
