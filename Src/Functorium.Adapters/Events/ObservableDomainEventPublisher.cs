using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functorium.Adapters.Events;

/// <summary>
/// 관찰성(로깅, 추적, 메트릭)이 통합된 도메인 이벤트 발행자 데코레이터.
/// </summary>
public sealed class ObservableDomainEventPublisher : IDomainEventPublisher, IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IDomainEventPublisher _inner;
    private readonly IDomainEventCollector _collector;
    private readonly ILogger<ObservableDomainEventPublisher> _logger;
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    public ObservableDomainEventPublisher(
        ActivitySource activitySource,
        IDomainEventPublisher inner,
        IDomainEventCollector collector,
        ILogger<ObservableDomainEventPublisher> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions)
    {
        _activitySource = activitySource;
        _inner = inner;
        _collector = collector;
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
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Event);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerName, eventType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.Publish);

            _logger.LogDomainEventPublisherRequest(domainEvent);

            TagList requestTags = new TagList
            {
                { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
                { ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Event },
                { ObservabilityNaming.CustomAttributes.RequestHandlerName, eventType },
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
                    _durationHistogram.Record(elapsed, requestTags);
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
                    _durationHistogram.Record(elapsed, requestTags);
                    _responseCounter.Add(1, failureTags);
                });

            return result;
        });
    }

    /// <inheritdoc />
    public FinT<IO, Seq<PublishResult>> PublishTrackedEvents(
        CancellationToken cancellationToken = default)
    {
        return IO.liftAsync(async () =>
        {
            using var activity = _activitySource.StartActivity(
                ObservabilityNaming.Spans.OperationName(
                    ObservabilityNaming.Layers.Adapter,
                    ObservabilityNaming.Categories.Event,
                    nameof(PublishTrackedEvents),
                    ObservabilityNaming.Methods.PublishTrackedEvents));

            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Event);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerName, nameof(PublishTrackedEvents));
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.PublishTrackedEvents);

            // Collector에서 추적 중인 이벤트 건수 계산
            int trackedEventCount = _collector.GetTrackedAggregates().Sum(a => a.DomainEvents.Count)
                                  + _collector.GetDirectlyTrackedEvents().Count;
            activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventCount, trackedEventCount);

            _logger.LogDomainEventsPublisherRequest(nameof(PublishTrackedEvents), ObservabilityNaming.Methods.PublishTrackedEvents, trackedEventCount);

            TagList requestTags = new TagList
            {
                { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Adapter },
                { ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Event },
                { ObservabilityNaming.CustomAttributes.RequestHandlerName, nameof(PublishTrackedEvents) },
                { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.PublishTrackedEvents }
            };
            _requestCounter.Add(1, requestTags);

            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            var result = await _inner.PublishTrackedEvents(cancellationToken).Run().RunAsync();

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

            result.Match(
                Succ: publishResults =>
                {
                    int totalEvents = publishResults.Sum(r => r.TotalCount);
                    int eventTypeCount = publishResults.Count;

                    activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventCount, totalEvents);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestAggregateCount, eventTypeCount);
                    activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);

                    bool allSuccessful = publishResults.ForAll(r => r.IsAllSuccessful);
                    if (allSuccessful)
                    {
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                        activity?.SetStatus(ActivityStatusCode.Ok);
                        _logger.LogDomainEventsPublisherResponseSuccess(nameof(PublishTrackedEvents), ObservabilityNaming.Methods.PublishTrackedEvents, totalEvents, elapsed);

                        TagList successTags = CreateSuccessTags(requestTags);
                        _durationHistogram.Record(elapsed, requestTags);
                        _responseCounter.Add(1, successTags);
                    }
                    else
                    {
                        int successCount = publishResults.Sum(r => r.SuccessCount);
                        int failureCount = publishResults.Sum(r => r.FailureCount);

                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseEventSuccessCount, successCount);
                        activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseEventFailureCount, failureCount);
                        activity?.SetStatus(ActivityStatusCode.Error, $"Partial failure: {failureCount} of {totalEvents} events failed");

                        _logger.LogDomainEventsPublisherResponsePartialFailure(
                            nameof(PublishTrackedEvents),
                            ObservabilityNaming.Methods.PublishTrackedEvents,
                            totalEvents,
                            successCount,
                            failureCount,
                            elapsed);

                        TagList partialFailureTags = CreatePartialFailureTags(requestTags);
                        _durationHistogram.Record(elapsed, requestTags);
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
                        _logger.LogDomainEventsPublisherResponseError(nameof(PublishTrackedEvents), ObservabilityNaming.Methods.PublishTrackedEvents, 0, elapsed, errorType, errorCode, error);
                    }
                    else
                    {
                        _logger.LogDomainEventsPublisherResponseWarning(nameof(PublishTrackedEvents), ObservabilityNaming.Methods.PublishTrackedEvents, 0, elapsed, errorType, errorCode, error);
                    }

                    TagList failureTags = CreateFailureTags(requestTags, errorType, errorCode);
                    _durationHistogram.Record(elapsed, requestTags);
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
