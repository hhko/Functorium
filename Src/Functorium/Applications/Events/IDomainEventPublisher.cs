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
    /// IDomainEventCollector에서 추적된 모든 Aggregate의 도메인 이벤트를 발행합니다.
    /// 각 Aggregate의 이벤트를 모두 발행 시도하며, 발행 후 이벤트를 클리어합니다.
    /// </summary>
    FinT<IO, Seq<PublishResult>> PublishTrackedEvents(
        CancellationToken cancellationToken = default);
}
