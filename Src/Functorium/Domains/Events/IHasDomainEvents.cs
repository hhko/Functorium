namespace Functorium.Domains.Events;

/// <summary>
/// 도메인 이벤트를 가진 Aggregate를 추적하기 위한 비제네릭 마커 인터페이스.
/// 도메인 계층의 읽기 전용 계약으로, 이벤트 조회만 허용합니다.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
}
