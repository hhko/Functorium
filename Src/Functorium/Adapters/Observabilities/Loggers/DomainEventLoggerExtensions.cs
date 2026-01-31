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
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublish,
            message: "{domain_event.category} {domain_event.type} {@domain_event.payload} publishing",
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
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
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublishSuccess,
            message: "{domain_event.category} {domain_event.type} published {response.status} in {response.elapsed:0.0000} s",
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
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
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublishWarning,
            message: "{domain_event.category} {domain_event.type} published {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
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
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublishError,
            message: "{domain_event.category} {domain_event.type} published {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Category,
            typeof(TEvent).Name,
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
        int eventCount)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublish,
            message: "{domain_event.category} {aggregate.type} publishing {event.count} events",
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
            eventCount);
    }

    /// <summary>
    /// Aggregate의 모든 도메인 이벤트 발행 성공 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventsPublishSuccess(
        this ILogger logger,
        string aggregateType,
        int eventCount,
        double elapsed)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublishSuccess,
            message: "{domain_event.category} {aggregate.type} published {event.count} events {response.status} in {response.elapsed:0.0000} s",
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
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
        int eventCount,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        logger.LogError(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublishError,
            message: "{domain_event.category} {aggregate.type} published {event.count} events {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
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
        int eventCount,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.DomainEvent.DomainEventPublishWarning,
            message: "{domain_event.category} {aggregate.type} published {event.count} events {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.DomainEvents.Category,
            aggregateType,
            eventCount,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }
}
