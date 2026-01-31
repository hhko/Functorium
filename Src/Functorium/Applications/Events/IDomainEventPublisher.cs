using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using LanguageExt;

namespace Functorium.Applications.Events;

/// <summary>
/// 도메인 이벤트 발행자 인터페이스.
/// Repository/Port와 동일한 FinT 반환 패턴.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Aggregate의 모든 도메인 이벤트를 발행하고 클리어합니다.
    /// 첫 번째 실패 시 즉시 중단됩니다.
    /// </summary>
    /// <typeparam name="TId">EntityId 타입</typeparam>
    /// <param name="aggregate">도메인 이벤트를 가진 Aggregate Root</param>
    /// <param name="cancellationToken">취소 토큰</param>
    FinT<IO, LanguageExt.Unit> PublishEvents<TId>(
        AggregateRoot<TId> aggregate,
        CancellationToken cancellationToken = default)
        where TId : struct, IEntityId<TId>;

    /// <summary>
    /// 단일 도메인 이벤트를 발행합니다.
    /// </summary>
    /// <typeparam name="TEvent">도메인 이벤트 타입</typeparam>
    /// <param name="domainEvent">발행할 도메인 이벤트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    FinT<IO, LanguageExt.Unit> Publish<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Aggregate의 모든 도메인 이벤트를 발행하고 상세 결과를 반환합니다.
    /// 실패한 이벤트가 있어도 모든 이벤트 발행을 시도합니다.
    /// </summary>
    /// <typeparam name="TId">EntityId 타입</typeparam>
    /// <param name="aggregate">도메인 이벤트를 가진 Aggregate Root</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>성공/실패 이벤트를 포함한 발행 결과</returns>
    FinT<IO, PublishResult> PublishEventsWithResult<TId>(
        AggregateRoot<TId> aggregate,
        CancellationToken cancellationToken = default)
        where TId : struct, IEntityId<TId>;
}
