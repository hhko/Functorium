using System.Diagnostics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Domains.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Events;

/// <summary>
/// 도메인 이벤트 핸들러에 대한 관찰성(로깅, 추적)을 제공하는 INotificationPublisher 구현체.
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
public sealed class ObservableDomainEventNotificationPublisher : INotificationPublisher
{
    private static readonly ActivitySource ActivitySource = new(
        ObservabilityNaming.DomainEventHandlers.ActivitySourceName);

    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// ObservableDomainEventNotificationPublisher를 생성합니다.
    /// </summary>
    /// <param name="loggerFactory">로거 팩토리</param>
    public ObservableDomainEventNotificationPublisher(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
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
            logger.LogDomainEventHandlerResponseSuccess(handlerName, elapsed);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Success);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
            var (errorType, errorCode) = ErrorInfoExtractor.GetErrorInfo(ex);

            logger.LogDomainEventHandlerResponseError(handlerName, elapsed, errorType, errorCode, ex);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ResponseStatus, ObservabilityNaming.Status.Failure);
            activity?.SetTag(ObservabilityNaming.OTelAttributes.ErrorType, errorType);
            activity?.SetTag(ObservabilityNaming.CustomAttributes.ErrorCode, errorCode);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
    }
}
