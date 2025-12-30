namespace Functorium.Applications.Observabilities;

/// <summary>
/// 기술 독립적인 관찰 가능성 컨텍스트를 나타냅니다.
/// 분산 추적에서 요청 간 상관관계를 위한 식별 정보를 제공합니다.
/// </summary>
public interface IObservabilityContext
{
    /// <summary>
    /// 전체 추적을 식별하는 고유 ID입니다.
    /// 하나의 요청이 여러 서비스를 거치더라도 동일한 TraceId를 공유합니다.
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// 현재 작업 단위를 식별하는 고유 ID입니다.
    /// 각 Span은 고유한 SpanId를 가집니다.
    /// </summary>
    string SpanId { get; }
}
