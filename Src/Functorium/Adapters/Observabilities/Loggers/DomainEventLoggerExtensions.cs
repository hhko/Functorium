using Functorium.Adapters.Observabilities.Naming;
using Functorium.Domains.Events;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// 도메인 이벤트 발행에서 사용하는 로거 확장 메서드.
/// </summary>
public static class DomainEventLoggerExtensions
{
    /// <summary>
    /// 도메인 이벤트 발행 시작 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventPublish<TEvent>(
        this ILogger logger,
        TEvent domainEvent)
        where TEvent : IDomainEvent
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventRequest,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} {@request.message} requesting",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
            ObservabilityNaming.DomainEvents.PublishMethod,
            domainEvent);
    }

    /// <summary>
    /// 도메인 이벤트 발행 성공 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventPublishSuccess<TEvent>(
        this ILogger logger,
        TEvent domainEvent,
        double elapsed)
        where TEvent : IDomainEvent
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventResponseSuccess,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
            ObservabilityNaming.DomainEvents.PublishMethod,
            ObservabilityNaming.Status.Success,
            elapsed);
    }

    /// <summary>
    /// 도메인 이벤트 발행 경고 로그를 출력합니다 (예상된 에러).
    /// </summary>
    public static void LogDomainEventPublishWarning<TEvent>(
        this ILogger logger,
        TEvent domainEvent,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
        where TEvent : IDomainEvent
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventResponseWarning,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
            ObservabilityNaming.DomainEvents.PublishMethod,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// 도메인 이벤트 발행 에러 로그를 출력합니다 (예외적 에러).
    /// </summary>
    public static void LogDomainEventPublishError<TEvent>(
        this ILogger logger,
        TEvent domainEvent,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
        where TEvent : IDomainEvent
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        logger.LogError(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventResponseError,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
            ObservabilityNaming.DomainEvents.PublishMethod,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// Aggregate의 모든 도메인 이벤트 발행 시작 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventsPublish(
        this ILogger logger,
        string aggregateType,
        string methodName,
        int eventCount)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventRequest,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} {event.count} events requesting",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
            methodName,
            eventCount);
    }

    /// <summary>
    /// Aggregate의 모든 도메인 이벤트 발행 성공 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventsPublishSuccess(
        this ILogger logger,
        string aggregateType,
        string methodName,
        int eventCount,
        double elapsed)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventResponseSuccess,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} {event.count} events responded {response.status} in {response.elapsed:0.0000} s",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
            methodName,
            eventCount,
            ObservabilityNaming.Status.Success,
            elapsed);
    }

    /// <summary>
    /// Aggregate의 도메인 이벤트 발행 실패 로그를 출력합니다 (예외적 에러).
    /// </summary>
    public static void LogDomainEventsPublishError(
        this ILogger logger,
        string aggregateType,
        string methodName,
        int eventCount,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        logger.LogError(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventResponseError,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} {event.count} events responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
            methodName,
            eventCount,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// Aggregate의 도메인 이벤트 발행 경고 로그를 출력합니다 (예상된 에러).
    /// </summary>
    public static void LogDomainEventsPublishWarning(
        this ILogger logger,
        string aggregateType,
        string methodName,
        int eventCount,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventResponseWarning,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} {event.count} events responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
            methodName,
            eventCount,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// Aggregate의 도메인 이벤트 부분 실패 경고 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventsPublishPartialFailure(
        this ILogger logger,
        string aggregateType,
        string methodName,
        int eventCount,
        int successCount,
        int failureCount,
        double elapsed)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventResponseWarning,
            message: "{request.layer} {request.category} {request.handler}.{request.handler.method} {event.count} events with partial failure: {success_count} succeeded, {failure_count} failed responded {response.status} in {response.elapsed:0.0000} s",
            ObservabilityNaming.DomainEvents.Layer,
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
            methodName,
            eventCount,
            successCount,
            failureCount,
            ObservabilityNaming.Status.Failure,
            elapsed);
    }
}
