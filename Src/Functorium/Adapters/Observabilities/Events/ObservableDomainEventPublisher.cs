using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Events;
using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functorium.Adapters.Observabilities.Events;

/// <summary>
/// 관찰성(로깅, 추적, 메트릭)이 통합된 도메인 이벤트 발행자 데코레이터.
/// </summary>
public sealed class ObservableDomainEventPublisher : IDomainEventPublisher, IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IDomainEventPublisher _inner;
    private readonly ILogger<ObservableDomainEventPublisher> _logger;
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    public ObservableDomainEventPublisher(
        ActivitySource activitySource,
        IDomainEventPublisher inner,
        ILogger<ObservableDomainEventPublisher> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions)
    {
        _activitySource = activitySource;
        _inner = inner;
        _logger = logger;

        string meterName = $"{openTelemetryOptions.Value.ServiceNamespace}.{ObservabilityNaming.Layers.Adapter}.{ObservabilityNaming.Categories.Event}";
        _meter = meterFactory.Create(meterName);

        _requestCounter = _meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.AdapterRequest(ObservabilityNaming.Categories.Event),
            unit: "{request}",
            description: "Total number of domain event publish requests");

        _responseCounter = _meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.AdapterResponse(ObservabilityNaming.Categories.Event),
            unit: "{response}",
            description: "Total number of domain event publish responses");

        _durationHistogram = _meter.CreateHistogram<double>(
            name: ObservabilityNaming.Metrics.AdapterDuration(ObservabilityNaming.Categories.Event),
            unit: "s",
            description: "Duration of domain event publish processing in seconds");
    }

    public void Dispose()
    {
        _meter?.Dispose();
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

            using var activity = _activitySource.StartActivity(
                ObservabilityNaming.Spans.OperationName(
                    ObservabilityNaming.Layers.Adapter,
                    ObservabilityNaming.Categories.Event,
                    eventType,
                    ObservabilityNaming.Methods.Publish));

            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Event);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandler, eventType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.Publish);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.EventType, eventType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.EventOccurredAt, domainEvent.OccurredAt.ToString("O"));

            _logger.LogDomainEventPublisherRequest(domainEvent);

            TagList requestTags = new TagList
            {
                { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
                { ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Event },
                { ObservabilityNaming.CustomAttributes.RequestHandler, eventType },
                { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.Publish }
            };
            _requestCounter.Add(1, requestTags);

            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            var result = await _inner.Publish(domainEvent, cancellationToken).Run().RunAsync();

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

            result.Match(
                Succ: _ =>
                {
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    _logger.LogDomainEventPublisherResponseSuccess(domainEvent, elapsed);

                    TagList successTags = CreateSuccessTags(requestTags);
                    _durationHistogram.Record(elapsed, successTags);
                    _responseCounter.Add(1, successTags);
                },
                Fail: error =>
                {
                    var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(error);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
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

                    TagList failureTags = CreateFailureTags(requestTags, errorType, errorCode);
                    _durationHistogram.Record(elapsed, failureTags);
                    _responseCounter.Add(1, failureTags);
                });

            return result;
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
            var eventCount = aggregate.DomainEvents.Count;
            var aggregateType = aggregate.GetType().Name;

            using var activity = _activitySource.StartActivity(
                ObservabilityNaming.Spans.OperationName(
                    ObservabilityNaming.Layers.Adapter,
                    ObservabilityNaming.Categories.Event,
                    aggregateType,
                    ObservabilityNaming.Methods.PublishEvents));

            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Event);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandler, aggregateType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.PublishEvents);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.AggregateType, aggregateType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventCount, eventCount);

            _logger.LogDomainEventsPublisherRequest(aggregateType, ObservabilityNaming.Methods.PublishEvents, eventCount);

            TagList requestTags = new TagList
            {
                { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
                { ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Event },
                { ObservabilityNaming.CustomAttributes.RequestHandler, aggregateType },
                { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.PublishEvents }
            };
            _requestCounter.Add(1, requestTags);

            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            var result = await _inner.PublishEvents(aggregate, cancellationToken).Run().RunAsync();

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

            result.Match(
                Succ: _ =>
                {
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    _logger.LogDomainEventsPublisherResponseSuccess(aggregateType, ObservabilityNaming.Methods.PublishEvents, eventCount, elapsed);

                    TagList successTags = CreateSuccessTags(requestTags);
                    _durationHistogram.Record(elapsed, successTags);
                    _responseCounter.Add(1, successTags);
                },
                Fail: error =>
                {
                    var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(error);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                    activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
                    activity?.SetStatus(ActivityStatusCode.Error, error.Message);

                    if (error.IsExceptional)
                    {
                        _logger.LogDomainEventsPublisherResponseError(aggregateType, ObservabilityNaming.Methods.PublishEvents, eventCount, elapsed, errorType, errorCode, error);
                    }
                    else
                    {
                        _logger.LogDomainEventsPublisherResponseWarning(aggregateType, ObservabilityNaming.Methods.PublishEvents, eventCount, elapsed, errorType, errorCode, error);
                    }

                    TagList failureTags = CreateFailureTags(requestTags, errorType, errorCode);
                    _durationHistogram.Record(elapsed, failureTags);
                    _responseCounter.Add(1, failureTags);
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

            using var activity = _activitySource.StartActivity(
                ObservabilityNaming.Spans.OperationName(
                    ObservabilityNaming.Layers.Adapter,
                    ObservabilityNaming.Categories.Event,
                    aggregateType,
                    ObservabilityNaming.Methods.PublishEventsWithResult));

            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Event);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandler, aggregateType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.PublishEventsWithResult);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.AggregateType, aggregateType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventCount, eventCount);

            _logger.LogDomainEventsPublisherRequest(aggregateType, ObservabilityNaming.Methods.PublishEventsWithResult, eventCount);

            TagList requestTags = new TagList
            {
                { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
                { ObservabilityNaming.CustomAttributes.RequestCategory, ObservabilityNaming.Categories.Event },
                { ObservabilityNaming.CustomAttributes.RequestHandler, aggregateType },
                { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.PublishEventsWithResult }
            };
            _requestCounter.Add(1, requestTags);

            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            var result = await _inner.PublishEventsWithResult(aggregate, cancellationToken).Run().RunAsync();

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

            result.Match(
                Succ: publishResult =>
                {
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
                    if (publishResult.IsAllSuccessful)
                    {
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                        activity?.SetStatus(ActivityStatusCode.Ok);
                        _logger.LogDomainEventsPublisherResponseSuccess(aggregateType, ObservabilityNaming.Methods.PublishEventsWithResult, eventCount, elapsed);

                        TagList successTags = CreateSuccessTags(requestTags);
                        _durationHistogram.Record(elapsed, successTags);
                        _responseCounter.Add(1, successTags);
                    }
                    else
                    {
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseEventSuccessCount, publishResult.SuccessCount);
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseEventFailureCount, publishResult.FailureCount);
                        activity?.SetStatus(ActivityStatusCode.Error, $"Partial failure: {publishResult.FailureCount} of {publishResult.TotalCount} events failed");

                        _logger.LogDomainEventsPublisherResponsePartialFailure(
                            aggregateType,
                            ObservabilityNaming.Methods.PublishEventsWithResult,
                            eventCount,
                            publishResult.SuccessCount,
                            publishResult.FailureCount,
                            elapsed);

                        TagList partialFailureTags = CreatePartialFailureTags(requestTags);
                        _durationHistogram.Record(elapsed, partialFailureTags);
                        _responseCounter.Add(1, partialFailureTags);
                    }
                },
                Fail: error =>
                {
                    var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(error);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                    activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
                    activity?.SetStatus(ActivityStatusCode.Error, error.Message);

                    if (error.IsExceptional)
                    {
                        _logger.LogDomainEventsPublisherResponseError(aggregateType, ObservabilityNaming.Methods.PublishEventsWithResult, eventCount, elapsed, errorType, errorCode, error);
                    }
                    else
                    {
                        _logger.LogDomainEventsPublisherResponseWarning(aggregateType, ObservabilityNaming.Methods.PublishEventsWithResult, eventCount, elapsed, errorType, errorCode, error);
                    }

                    TagList failureTags = CreateFailureTags(requestTags, errorType, errorCode);
                    _durationHistogram.Record(elapsed, failureTags);
                    _responseCounter.Add(1, failureTags);
                });

            return result;
        });
    }

    private static TagList CreateSuccessTags(TagList requestTags)
    {
        TagList tags = new();
        foreach (var tag in requestTags)
        {
            tags.Add(tag);
        }
        tags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
        return tags;
    }

    private static TagList CreatePartialFailureTags(TagList requestTags)
    {
        TagList tags = new();
        foreach (var tag in requestTags)
        {
            tags.Add(tag);
        }
        tags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
        return tags;
    }

    private static TagList CreateFailureTags(TagList requestTags, string errorType, string errorCode)
    {
        TagList tags = new();
        foreach (var tag in requestTags)
        {
            tags.Add(tag);
        }
        tags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
        tags.Add(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
        tags.Add(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
        return tags;
    }
}
