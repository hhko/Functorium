using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Events;

/// <summary>
/// Mediator.IPublisher를 사용한 도메인 이벤트 발행자 구현.
/// 개별 이벤트를 Mediator를 통해 발행하고, 배치 핸들러가 등록된 경우 직접 호출합니다.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly IDomainEventCollector _collector;
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivitySource? _activitySource;
    private readonly ILogger<DomainEventPublisher>? _logger;
    private readonly Counter<long>? _batchRequestCounter;
    private readonly Counter<long>? _batchResponseCounter;
    private readonly Histogram<double>? _batchDurationHistogram;

    public DomainEventPublisher(
        IPublisher publisher,
        IDomainEventCollector collector,
        IServiceProvider serviceProvider)
    {
        _publisher = publisher;
        _collector = collector;
        _serviceProvider = serviceProvider;

        // 관찰 가능성 의존성은 선택적 (테스트에서는 null 허용)
        _activitySource = serviceProvider.GetService<ActivitySource>();
        _logger = serviceProvider.GetService<ILogger<DomainEventPublisher>>();

        var meterFactory = serviceProvider.GetService<IMeterFactory>();
        if (meterFactory is not null)
        {
            var meter = meterFactory.Create(
                $"{ObservabilityNaming.Layers.Application}.{ObservabilityNaming.Categories.Event}.batch");
            _batchRequestCounter = meter.CreateCounter<long>(
                name: $"{ObservabilityNaming.Layers.Application}.{ObservabilityNaming.Categories.Event}.batch.requests",
                unit: "{request}",
                description: "Total number of batch event handler requests");
            _batchResponseCounter = meter.CreateCounter<long>(
                name: $"{ObservabilityNaming.Layers.Application}.{ObservabilityNaming.Categories.Event}.batch.responses",
                unit: "{response}",
                description: "Total number of batch event handler responses");
            _batchDurationHistogram = meter.CreateHistogram<double>(
                name: $"{ObservabilityNaming.Layers.Application}.{ObservabilityNaming.Categories.Event}.batch.duration",
                unit: "s",
                description: "Duration of batch event handler processing in seconds");
        }
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

            // 2. 타입별 그룹화
            var grouped = allEvents.GroupBy(e => e.GetType());
            var results = new List<PublishResult>();

            foreach (var group in grouped)
            {
                var events = group.ToList();

                // 벌크 판단: 같은 타입 이벤트가 2개 이상이고 배치 핸들러가 등록됨 → 배치 처리
                if (events.Count > 1)
                {
                    bool handled = await InvokeBatchHandlerIfRegistered(group.Key, events, cancellationToken);
                    if (handled)
                    {
                        // 배치 처리 완료 → 개별 발행 스킵
                        results.Add(PublishResult.Success(new Seq<IDomainEvent>(events)));
                        continue;
                    }
                }

                // 개별 발행: 1개짜리 이벤트 또는 배치 핸들러 미등록 시 폴백
                var result = await PublishIndividualEvents(events, cancellationToken);
                results.Add(result);
            }

            return Fin.Succ(new Seq<PublishResult>(results));
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

    /// <summary>
    /// 이벤트 타입에 대한 IDomainEventBatchHandler가 등록되어 있으면 직접 호출합니다.
    /// Mediator 라우팅을 우회하되, 배치 호출 단위로 관찰 가능성(Activity, 로그, 지표)을 적용합니다.
    /// </summary>
    /// <returns>배치 핸들러가 호출되었으면 true, 미등록이면 false</returns>
    private async Task<bool> InvokeBatchHandlerIfRegistered(
        Type eventType,
        List<IDomainEvent> events,
        CancellationToken cancellationToken)
    {
        var batchHandlerType = typeof(IDomainEventBatchHandler<>).MakeGenericType(eventType);
        var batchHandler = _serviceProvider.GetService(batchHandlerType);
        if (batchHandler is null)
            return false;

        var handleBatchMethod = batchHandlerType.GetMethod(nameof(IDomainEventBatchHandler<IDomainEvent>.HandleBatch));
        if (handleBatchMethod is null)
            return false;

        string handlerName = batchHandler.GetType().Name;
        string eventTypeName = eventType.Name;

        // Activity span 생성
        using var activity = _activitySource?.StartActivity(
            $"{ObservabilityNaming.Layers.Application} {ObservabilityNaming.Categories.Usecase}.{ObservabilityNaming.CategoryTypes.Event} {handlerName}.{ObservabilityNaming.Methods.HandleBatch}");

        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Usecase);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategoryType, ObservabilityNaming.CategoryTypes.Event);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerName, handlerName);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.HandleBatch);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventType, eventTypeName);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventCount, events.Count);

        // 로그 request
        _logger?.LogDomainEventBatchHandlerRequest(handlerName, eventTypeName, events.Count);

        // 지표 request
        TagList requestTags = new()
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application },
            { ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Usecase },
            { ObservabilityNaming.CustomAttributes.RequestCategoryType, ObservabilityNaming.CategoryTypes.Event },
            { ObservabilityNaming.CustomAttributes.RequestHandlerName, handlerName },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, ObservabilityNaming.Methods.HandleBatch }
        };
        _batchRequestCounter?.Add(1, requestTags);

        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        try
        {
            // Seq<TEvent> 생성 및 HandleBatch 호출
            var seqType = typeof(Seq<>).MakeGenericType(eventType);
            var seqConstructor = seqType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(eventType)]);
            var castedEvents = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.Cast))!
                .MakeGenericMethod(eventType)
                .Invoke(null, [events]);
            var seq = seqConstructor!.Invoke([castedEvents]);

            var task = (ValueTask)handleBatchMethod.Invoke(batchHandler, [seq, cancellationToken])!;
            await task;

            // 성공 response
            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger?.LogDomainEventBatchHandlerResponseSuccess(handlerName, eventTypeName, events.Count, elapsed);

            TagList successTags = new();
            foreach (var tag in requestTags) successTags.Add(tag);
            successTags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
            _batchDurationHistogram?.Record(elapsed, successTags);
            _batchResponseCounter?.Add(1, successTags);

            return true;
        }
        catch (Exception ex)
        {
            // 에러 response
            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
            var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(ex);

            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseElapsed, elapsed);
            activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger?.LogDomainEventBatchHandlerResponseError(
                handlerName, eventTypeName, events.Count, elapsed, errorType, errorCode, ex);

            TagList failureTags = new();
            foreach (var tag in requestTags) failureTags.Add(tag);
            failureTags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
            failureTags.Add(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
            failureTags.Add(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
            _batchDurationHistogram?.Record(elapsed, failureTags);
            _batchResponseCounter?.Add(1, failureTags);

            throw;
        }
    }
}
