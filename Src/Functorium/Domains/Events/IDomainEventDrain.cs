namespace Functorium.Domains.Events;

/// <summary>
/// 도메인 이벤트 정리를 위한 인프라 인터페이스.
/// 이벤트 발행 후 Aggregate의 이벤트를 제거하는 데 사용됩니다.
/// 도메인 계약(IHasDomainEvents)과 분리하여, 이벤트 정리가 인프라 관심사임을 명시합니다.
/// </summary>
internal interface IDomainEventDrain : IHasDomainEvents
{
    void ClearDomainEvents();
}
