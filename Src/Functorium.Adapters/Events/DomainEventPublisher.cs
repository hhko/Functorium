using Functorium.Applications.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;
using Mediator;

namespace Functorium.Adapters.Events;

/// <summary>
/// Mediator.IPublisher를 사용한 도메인 이벤트 발행자 구현.
/// 개별 이벤트를 Mediator를 통해 발행합니다.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly IDomainEventCollector _collector;

    public DomainEventPublisher(
        IPublisher publisher,
        IDomainEventCollector collector)
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
            // 1. Aggregate에서 이벤트 수집
            var allEvents = new List<IDomainEvent>();

            var trackedAggregates = _collector.GetTrackedAggregates();
            foreach (var aggregate in trackedAggregates)
            {
                allEvents.AddRange(aggregate.DomainEvents);
                (aggregate as IDomainEventDrain)?.ClearDomainEvents();
            }

            // 2. Domain Service 직접 추적 이벤트 수집
            allEvents.AddRange(_collector.GetDirectlyTrackedEvents());

            if (allEvents.Count == 0)
                return Fin.Succ(LanguageExt.Seq<PublishResult>.Empty);

            // 3. 모든 이벤트를 개별 발행
            var result = await PublishIndividualEvents(allEvents, cancellationToken);
            return Fin.Succ(new Seq<PublishResult>(new[] { result }));
        });
    }

    /// <summary>
    /// 각 이벤트를 개별적으로 Mediator를 통해 발행합니다.
    /// </summary>
    private async Task<PublishResult> PublishIndividualEvents(
        List<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        var successful = new List<IDomainEvent>();
        var failed = new List<(IDomainEvent, LanguageExt.Common.Error)>();

        foreach (var evt in events)
        {
            try
            {
                await _publisher.Publish(evt, cancellationToken);
                successful.Add(evt);
            }
            catch (OperationCanceledException)
            {
                var error = EventError.For<DomainEventPublisher>(
                    new EventErrorType.PublishCancelled(),
                    evt.GetType().Name,
                    $"Event publishing was cancelled");
                failed.Add((evt, error));
            }
            catch (Exception ex)
            {
                var error = EventError.FromException<DomainEventPublisher>(
                    new EventErrorType.PublishFailed(),
                    ex);
                failed.Add((evt, error));
            }
        }

        return new PublishResult(
            new Seq<IDomainEvent>(successful),
            new Seq<(IDomainEvent, LanguageExt.Common.Error)>(failed));
    }

}
