using Functorium.Applications.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;
using Mediator;

namespace Functorium.Adapters.Events;

/// <summary>
/// Mediator.IPublisher를 사용한 도메인 이벤트 발행자 구현.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly IDomainEventCollector _collector;

    public DomainEventPublisher(IPublisher publisher, IDomainEventCollector collector)
    {
        _publisher = publisher;
        _collector = collector;
    }

    /// <inheritdoc />
    public FinT<IO, LanguageExt.Unit> Publish<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                await _publisher.Publish(domainEvent, cancellationToken);
                return Fin.Succ(LanguageExt.Unit.Default);
            }
            catch (OperationCanceledException)
            {
                return Fin.Fail<LanguageExt.Unit>(
                    EventError.For<DomainEventPublisher>(
                        new EventErrorType.PublishCancelled(),
                        typeof(TEvent).Name,
                        "Event publishing was cancelled"));
            }
            catch (Exception ex)
            {
                return Fin.Fail<LanguageExt.Unit>(
                    EventError.FromException<DomainEventPublisher>(
                        new EventErrorType.PublishFailed(),
                        ex));
            }
        });
    }

    /// <inheritdoc />
    public FinT<IO, Seq<PublishResult>> PublishTrackedEvents(
        CancellationToken cancellationToken = default)
    {
        return IO.liftAsync(async () =>
        {
            // 1. 모든 이벤트 수집 (Aggregate + 직접 추적)
            var allEvents = new List<IDomainEvent>();

            var trackedAggregates = _collector.GetTrackedAggregates();
            foreach (var aggregate in trackedAggregates)
            {
                allEvents.AddRange(aggregate.DomainEvents);
                (aggregate as IDomainEventDrain)?.ClearDomainEvents();
            }

            allEvents.AddRange(_collector.GetDirectlyTrackedEvents());

            if (allEvents.Count == 0)
                return Fin.Succ(LanguageExt.Seq<PublishResult>.Empty);

            // 2. 타입별 그룹화 → BulkDomainEvent로 발행
            var grouped = allEvents.GroupBy(e => e.GetType());
            var results = new List<PublishResult>();

            foreach (var group in grouped)
            {
                var result = await PublishBulkEventGroup(
                    group.Key, group.ToList(), cancellationToken);
                results.Add(result);
            }

            return Fin.Succ(new Seq<PublishResult>(results));
        });
    }

    /// <summary>
    /// 동일 타입 이벤트 그룹을 BulkDomainEvent로 래핑하여 발행합니다.
    /// </summary>
    private async Task<PublishResult> PublishBulkEventGroup(
        Type eventType,
        List<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        try
        {
            var bulkEvent = new BulkDomainEvent(events, eventType);
            await _publisher.Publish(bulkEvent, cancellationToken);
            return PublishResult.Success(new Seq<IDomainEvent>(events));
        }
        catch (OperationCanceledException)
        {
            var error = EventError.For<DomainEventPublisher>(
                new EventErrorType.PublishCancelled(),
                eventType.Name,
                $"Bulk event publishing was cancelled ({events.Count} events)");
            return PublishResult.Failure(
                new Seq<(IDomainEvent, LanguageExt.Common.Error)>(
                    events.Select(e => (e, (LanguageExt.Common.Error)error))));
        }
        catch (Exception ex)
        {
            var error = EventError.FromException<DomainEventPublisher>(
                new EventErrorType.PublishFailed(),
                ex);
            return PublishResult.Failure(
                new Seq<(IDomainEvent, LanguageExt.Common.Error)>(
                    events.Select(e => (e, (LanguageExt.Common.Error)error))));
        }
    }
}
