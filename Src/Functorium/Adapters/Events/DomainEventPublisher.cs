using Functorium.Applications.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using LanguageExt;
using Mediator;
using static LanguageExt.Prelude;

namespace Functorium.Adapters.Events;

/// <summary>
/// Mediator.IPublisher를 사용한 도메인 이벤트 발행자 구현.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;

    public DomainEventPublisher(IPublisher publisher)
    {
        _publisher = publisher;
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
    public FinT<IO, LanguageExt.Unit> PublishEvents<TId>(
        AggregateRoot<TId> aggregate,
        CancellationToken cancellationToken = default)
        where TId : struct, IEntityId<TId>
    {
        return IO.liftAsync(async () =>
        {
            var events = aggregate.DomainEvents.ToList();
            aggregate.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                try
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return Fin.Fail<LanguageExt.Unit>(
                        EventError.For<DomainEventPublisher>(
                            new EventErrorType.PublishCancelled(),
                            domainEvent.GetType().Name,
                            "Event publishing was cancelled"));
                }
                catch (Exception ex)
                {
                    return Fin.Fail<LanguageExt.Unit>(
                        EventError.FromException<DomainEventPublisher>(
                            new EventErrorType.PublishFailed(),
                            ex));
                }
            }
            return Fin.Succ(LanguageExt.Unit.Default);
        });
    }

    /// <inheritdoc />
    public FinT<IO, PublishResult> PublishEventsWithResult<TId>(
        AggregateRoot<TId> aggregate,
        CancellationToken cancellationToken = default)
        where TId : struct, IEntityId<TId>
    {
        return IO.liftAsync(async () =>
        {
            var events = aggregate.DomainEvents.ToList();
            aggregate.ClearDomainEvents();

            var successfulEvents = new List<IDomainEvent>();
            var failedEvents = new List<(IDomainEvent Event, LanguageExt.Common.Error Error)>();

            foreach (var domainEvent in events)
            {
                try
                {
                    await _publisher.Publish(domainEvent, cancellationToken);
                    successfulEvents.Add(domainEvent);
                }
                catch (OperationCanceledException)
                {
                    var error = EventError.For<DomainEventPublisher>(
                        new EventErrorType.PublishCancelled(),
                        domainEvent.GetType().Name,
                        "Event publishing was cancelled");
                    failedEvents.Add((domainEvent, error));
                }
                catch (Exception ex)
                {
                    var error = EventError.FromException<DomainEventPublisher>(
                        new EventErrorType.PublishFailed(),
                        ex);
                    failedEvents.Add((domainEvent, error));
                }
            }

            var result = new PublishResult(
                new Seq<IDomainEvent>(successfulEvents),
                new Seq<(IDomainEvent Event, LanguageExt.Common.Error Error)>(failedEvents));

            return Fin.Succ(result);
        });
    }
}
