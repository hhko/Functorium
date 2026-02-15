namespace Functorium.Domains.Events;

/// <summary>
/// 도메인 이벤트를 가진 Aggregate를 추적하기 위한 비제네릭 마커 인터페이스.
/// UsecaseTransactionPipeline에서 Aggregate의 도메인 이벤트를 수집할 때 사용됩니다.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
