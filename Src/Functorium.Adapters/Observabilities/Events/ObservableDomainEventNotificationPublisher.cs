using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Observabilities;
using Functorium.Domains.Events;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functorium.Adapters.Observabilities.Events;

/// <summary>
/// 도메인 이벤트 핸들러에 대한 관찰성(로깅, 추적, 메트릭)을 제공하는 INotificationPublisher 구현체.
/// Handler 관점의 관찰 가능성을 제공합니다.
/// </summary>
/// <remarks>
/// <para>
/// Mediator 3.0은 INotification에 IPipelineBehavior를 지원하지 않습니다.
/// 또한 Mediator.SourceGenerator는 INotificationPublisher 인터페이스가 아닌 구체 타입을 직접 사용합니다.
/// 따라서 Scrutor의 Decorate 패턴은 동작하지 않으며, NotificationPublisherType 설정을 사용해야 합니다.
/// </para>
/// <para>
/// 사용 방법:
/// <code>
/// services.AddMediator(options =>
/// {
///     options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
/// });
/// </code>
/// </para>
/// </remarks>
public sealed class ObservableDomainEventNotificationPublisher : INotificationPublisher, IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Meter _meter;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// ObservableDomainEventNotificationPublisher를 생성합니다.
    /// </summary>
    /// <param name="activitySource">ActivitySource (DI 주입)</param>
    /// <param name="loggerFactory">로거 팩토리</param>
    /// <param name="meterFactory">Meter 팩토리</param>
    /// <param name="openTelemetryOptions">OpenTelemetry 옵션</param>
    /// <param name="serviceProvider">DI 서비스 프로바이더 (IDomainEventLogEnricher 해석용)</param>
    public ObservableDomainEventNotificationPublisher(
        ActivitySource activitySource,
        ILoggerFactory loggerFactory,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        IServiceProvider serviceProvider)
    {
        _activitySource = activitySource;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;

        string meterName = $"{openTelemetryOptions.Value.ServiceNamespace}.{ObservabilityNaming.Layers.Application}";
        _meter = meterFactory.Create(meterName);

        _requestCounter = _meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.UsecaseRequest(ObservabilityNaming.CategoryTypes.Event),
            unit: "{request}",
            description: "Total number of domain event handler requests");

        _responseCounter = _meter.CreateCounter<long>(
            name: ObservabilityNaming.Metrics.UsecaseResponse(ObservabilityNaming.CategoryTypes.Event),
            unit: "{response}",
            description: "Total number of domain event handler responses");

        _durationHistogram = _meter.CreateHistogram<double>(
            name: ObservabilityNaming.Metrics.UsecaseDuration(ObservabilityNaming.CategoryTypes.Event),
            unit: "s",
            description: "Duration of domain event handler processing in seconds");
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }

    /// <summary>
    /// Notification을 모든 핸들러에 발행합니다.
    /// IDomainEvent인 경우에만 Handler 관점 관찰 가능성을 적용합니다.
    /// </summary>
    public async ValueTask Publish<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        // IDomainEvent가 아닌 경우 기본 처리 (ForeachAwait 방식)
        if (notification is not IDomainEvent domainEvent)
        {
            await PublishWithoutObservability(handlers, notification, cancellationToken);
            return;
        }

        // IDomainEvent인 경우 Handler 관점 관찰 가능성 적용
        await PublishWithObservability(handlers, notification, domainEvent, cancellationToken);
    }

    /// <summary>
    /// 관찰 가능성 없이 notification을 발행합니다 (ForeachAwait 방식).
    /// </summary>
    private static async ValueTask PublishWithoutObservability<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is not null)
            throw new AggregateException(exceptions);
    }

    /// <summary>
    /// 관찰 가능성을 적용하여 도메인 이벤트를 발행합니다.
    /// </summary>
    private async ValueTask PublishWithObservability<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
        {
            try
            {
                await HandleWithObservability(handler, notification, domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                exceptions ??= [];
                exceptions.Add(ex);
            }
        }

        if (exceptions is not null)
            throw new AggregateException(exceptions);
    }

    /// <summary>
    /// 개별 핸들러에 관찰 가능성을 적용하여 실행합니다.
    /// </summary>
    private async ValueTask HandleWithObservability<TNotification>(
        INotificationHandler<TNotification> handler,
        TNotification notification,
        IDomainEvent domainEvent,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        // .NET 10 preview에서 일부 프록시 객체의 GetType() 호출 시
        // AccessViolationException이 발생할 수 있어 방어적으로 처리
        Type handlerType;
        string handlerName;
        try
        {
            handlerType = handler.GetType();
            handlerName = handlerType.Name;
        }
        catch
        {
            handlerType = typeof(INotificationHandler<TNotification>);
            handlerName = $"INotificationHandler<{typeof(TNotification).Name}>";
        }

        var logger = _loggerFactory.CreateLogger(handlerType);

        // Enricher가 등록되어 있으면 LogContext에 커스텀 속성 Push
        using var enrichment = ResolveEnrichment(domainEvent);

        string requestCategoryType = ObservabilityNaming.CategoryTypes.Event;
        string requestHandlerMethod = ObservabilityNaming.Methods.Handle;
        using var activity = _activitySource.StartActivity(
            $"{ObservabilityNaming.Layers.Application} {ObservabilityNaming.Categories.Usecase}.{requestCategoryType} {handlerName}.{requestHandlerMethod}");

        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Usecase);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestCategoryType, requestCategoryType);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerName, handlerName);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestHandlerMethod, requestHandlerMethod);
        string eventTypeName = domainEvent.GetType().Name;
        string eventId = domainEvent.EventId.ToString();
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventType, eventTypeName);
        activity?.SetTag(ObservabilityNaming.CustomAttributes.RequestEventId, eventId);

        // BulkDomainEvent인 경우 벌크(Bulk) 메타데이터를 Activity/로그에 추가
        if (domainEvent is IBulkEventInfo bulkInfo)
        {
            activity?.SetTag("bulk.event_count", bulkInfo.Count);
            activity?.SetTag("bulk.event_type", bulkInfo.InnerEventTypeName);
            logger.LogInformation(
                "[DomainEvent] Bulk handler invoked: {HandlerName}, EventType: {EventType}, Count: {Count}",
                handlerName, bulkInfo.InnerEventTypeName, bulkInfo.Count);
        }

        logger.LogDomainEventHandlerRequest(handlerName, domainEvent);

        TagList requestTags = new TagList
        {
            { ObservabilityNaming.CustomAttributes.RequestLayer, ObservabilityNaming.Layers.Application },
            { ObservabilityNaming.CustomAttributes.RequestCategoryName, ObservabilityNaming.Categories.Usecase },
            { ObservabilityNaming.CustomAttributes.RequestCategoryType, requestCategoryType },
            { ObservabilityNaming.CustomAttributes.RequestHandlerName, handlerName },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, requestHandlerMethod }
        };
        _requestCounter.Add(1, requestTags);

        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        try
        {
            await handler.Handle(notification, cancellationToken);

            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
            logger.LogDomainEventHandlerResponseSuccess(handlerName, eventTypeName, eventId, elapsed);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
            activity?.SetStatus(ActivityStatusCode.Ok);

            TagList successTags = new();
            foreach (var tag in requestTags)
            {
                successTags.Add(tag);
            }
            successTags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
            _durationHistogram.Record(elapsed, requestTags);
            _responseCounter.Add(1, successTags);
        }
        catch (Exception ex)
        {
            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
            var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(ex);

            logger.LogDomainEventHandlerResponseError(handlerName, eventTypeName, eventId, elapsed, errorType, errorCode, ex);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
            activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            TagList failureTags = new();
            foreach (var tag in requestTags)
            {
                failureTags.Add(tag);
            }
            failureTags.Add(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
            failureTags.Add(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
            failureTags.Add(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
            _durationHistogram.Record(elapsed, requestTags);
            _responseCounter.Add(1, failureTags);

            throw;
        }
    }

    /// <summary>
    /// 도메인 이벤트 타입에 해당하는 IDomainEventLogEnricher를 DI에서 해석하여 EnrichLog를 호출합니다.
    /// 등록되지 않은 경우 null을 반환합니다.
    /// </summary>
    private IDisposable? ResolveEnrichment(IDomainEvent domainEvent)
    {
        var enricherServiceType = typeof(IDomainEventLogEnricher<>).MakeGenericType(domainEvent.GetType());
        return (_serviceProvider.GetService(enricherServiceType) as IDomainEventLogEnricher)?.EnrichLog(domainEvent);
    }
}
