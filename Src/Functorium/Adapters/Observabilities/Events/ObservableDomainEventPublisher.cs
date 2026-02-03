using System.Diagnostics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Events;
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Events;

/// <summary>
/// 관찰성(로깅, 추적, 메트릭)이 통합된 도메인 이벤트 발행자 데코레이터.
/// </summary>
public sealed class ObservableDomainEventPublisher : IDomainEventPublisher
{
    private static readonly ActivitySource ActivitySource = new(
        ObservabilityNaming.DomainEvents.ActivitySourceName);

    private readonly IDomainEventPublisher _inner;
    private readonly ILogger<ObservableDomainEventPublisher> _logger;

    public ObservableDomainEventPublisher(
        IDomainEventPublisher inner,
        ILogger<ObservableDomainEventPublisher> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc />
    public FinT<IO, LanguageExt.Unit> PublishEvents<TId>(
        AggregateRoot<TId> aggregate,
        CancellationToken cancellationToken = default)
        where TId : struct, IEntityId<TId>
    {
        return IO.liftAsync(async () =>
        {
            var eventCount = aggregate.DomainEvents.Count;
            var aggregateType = aggregate.GetType().Name;

            using var activity = ActivitySource.StartActivity(
                $"{ObservabilityNaming.DomainEvents.Category} {aggregateType}.PublishEvents");

            activity?.SetTag("aggregate.type", aggregateType);
            activity?.SetTag("event.count", eventCount);

            _logger.LogDomainEventsPublisherRequest(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsMethod, eventCount);

            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            var result = await _inner.PublishEvents(aggregate, cancellationToken).Run().RunAsync();

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

            result.Match(
                Succ: _ =>
                {
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    _logger.LogDomainEventsPublisherResponseSuccess(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsMethod, eventCount, elapsed);
                },
                Fail: error =>
                {
                    var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(error);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                    activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
                    activity?.SetStatus(ActivityStatusCode.Error, error.Message);

                    if (error.IsExceptional)
                    {
                        _logger.LogDomainEventsPublisherResponseError(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsMethod, eventCount, elapsed, errorType, errorCode, error);
                    }
                    else
                    {
                        _logger.LogDomainEventsPublisherResponseWarning(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsMethod, eventCount, elapsed, errorType, errorCode, error);
                    }
                });

            return result;
        });
    }

    /// <inheritdoc />
    public FinT<IO, LanguageExt.Unit> Publish<TEvent>(
        TEvent domainEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        return IO.liftAsync(async () =>
        {
            var eventType = typeof(TEvent).Name;

            using var activity = ActivitySource.StartActivity(
                $"{ObservabilityNaming.DomainEvents.Category} {eventType}.Publish");

            activity?.SetTag("event.type", eventType);
            activity?.SetTag("event.occurred_at", domainEvent.OccurredAt.ToString("O"));

            _logger.LogDomainEventPublisherRequest(domainEvent);

            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            var result = await _inner.Publish(domainEvent, cancellationToken).Run().RunAsync();

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

            result.Match(
                Succ: _ =>
                {
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    _logger.LogDomainEventPublisherResponseSuccess(domainEvent, elapsed);
                },
                Fail: error =>
                {
                    var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(error);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                    activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
                    activity?.SetStatus(ActivityStatusCode.Error, error.Message);

                    if (error.IsExceptional)
                    {
                        _logger.LogDomainEventPublisherResponseError(domainEvent, elapsed, errorType, errorCode, error);
                    }
                    else
                    {
                        _logger.LogDomainEventPublisherResponseWarning(domainEvent, elapsed, errorType, errorCode, error);
                    }
                });

            return result;
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
            var eventCount = aggregate.DomainEvents.Count;
            var aggregateType = aggregate.GetType().Name;

            using var activity = ActivitySource.StartActivity(
                $"{ObservabilityNaming.DomainEvents.Category} {aggregateType}.PublishEventsWithResult");

            activity?.SetTag("aggregate.type", aggregateType);
            activity?.SetTag("event.count", eventCount);

            _logger.LogDomainEventsPublisherRequest(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsWithResultMethod, eventCount);

            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            var result = await _inner.PublishEventsWithResult(aggregate, cancellationToken).Run().RunAsync();

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

            result.Match(
                Succ: publishResult =>
                {
                    if (publishResult.IsAllSuccessful)
                    {
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                        activity?.SetStatus(ActivityStatusCode.Ok);
                        _logger.LogDomainEventsPublisherResponseSuccess(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsWithResultMethod, eventCount, elapsed);
                    }
                    else
                    {
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                        activity?.SetTag("event.success_count", publishResult.SuccessCount);
                        activity?.SetTag("event.failure_count", publishResult.FailureCount);
                        activity?.SetStatus(ActivityStatusCode.Error, $"Partial failure: {publishResult.FailureCount} of {publishResult.TotalCount} events failed");

                        _logger.LogDomainEventsPublisherResponsePartialFailure(
                            aggregateType,
                            ObservabilityNaming.DomainEvents.PublishEventsWithResultMethod,
                            eventCount,
                            publishResult.SuccessCount,
                            publishResult.FailureCount,
                            elapsed);
                    }
                },
                Fail: error =>
                {
                    var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(error);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                    activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
                    activity?.SetStatus(ActivityStatusCode.Error, error.Message);

                    if (error.IsExceptional)
                    {
                        _logger.LogDomainEventsPublisherResponseError(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsWithResultMethod, eventCount, elapsed, errorType, errorCode, error);
                    }
                    else
                    {
                        _logger.LogDomainEventsPublisherResponseWarning(aggregateType, ObservabilityNaming.DomainEvents.PublishEventsWithResultMethod, eventCount, elapsed, errorType, errorCode, error);
                    }
                });

            return result;
        });
    }
}
