using System.Diagnostics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Domains.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Events;

/// <summary>
/// 도메인 이벤트 핸들러에 대한 관찰성(로깅, 추적)을 제공하는 INotificationPublisher 구현체.
/// Handler 관점의 관찰 가능성을 제공합니다.
/// </summary>
/// <remarks>
/// Mediator 3.0은 INotification에 IPipelineBehavior를 지원하지 않으므로,
/// INotificationPublisher를 커스터마이징하여 Handler 관점의 관찰 가능성을 구현합니다.
/// </remarks>
public sealed class ObservableDomainEventNotificationPublisher : INotificationPublisher
{
    private static readonly ActivitySource ActivitySource = new(
        ObservabilityNaming.DomainEventHandlers.ActivitySourceName);

    private readonly INotificationPublisher _inner;
    private readonly ILoggerFactory _loggerFactory;

    public ObservableDomainEventNotificationPublisher(
        INotificationPublisher inner,
        ILoggerFactory loggerFactory)
    {
        _inner = inner;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// 도메인 이벤트를 모든 핸들러에 발행합니다.
    /// IDomainEvent인 경우에만 Handler 관점 관찰 가능성을 적용합니다.
    /// </summary>
    public async ValueTask Publish<TNotification>(
        NotificationHandlers<TNotification> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        // IDomainEvent가 아닌 경우 기본 처리
        if (notification is not IDomainEvent domainEvent)
        {
            await _inner.Publish(handlers, notification, cancellationToken);
            return;
        }

        // IDomainEvent인 경우 Handler 관점 관찰 가능성 적용
        foreach (var handler in handlers)
        {
            if (handler is null)
                continue;

            var handlerType = handler.GetType();
            var handlerName = handlerType.Name;
            var logger = _loggerFactory.CreateLogger(handlerType);

            using var activity = ActivitySource.StartActivity(
                $"{ObservabilityNaming.DomainEventHandlers.Category} {handlerName}.{ObservabilityNaming.Methods.Handle}");

            activity?.SetTag("handler.type", handlerName);
            activity?.SetTag("event.type", domainEvent.GetType().Name);
            activity?.SetTag("event.id", domainEvent.EventId.ToString());

            logger.LogDomainEventHandlerRequest(handlerName, domainEvent);
            long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

            try
            {
                await handler.Handle(notification, cancellationToken);

                double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
                logger.LogDomainEventHandlerSuccess(handlerName, elapsed);
                activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
                var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(ex);

                logger.LogDomainEventHandlerError(handlerName, elapsed, errorType, errorCode, ex);
                activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
                activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
                activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                throw;
            }
        }
    }
}
