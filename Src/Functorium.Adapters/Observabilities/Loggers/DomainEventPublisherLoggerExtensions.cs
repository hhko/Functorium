using Functorium.Adapters.Observabilities.Naming;
using Functorium.Domains.Events;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// 도메인 이벤트 발행자(Publisher)에서 사용하는 로거 확장 메서드.
/// </summary>
public static class DomainEventPublisherLoggerExtensions
{
    /// <summary>
    /// 도메인 이벤트 발행 요청 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventPublisherRequest<TEvent>(
        this ILogger logger,
        TEvent domainEvent)
        where TEvent : IDomainEvent
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterRequest,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {@request.message}",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            typeof(TEvent).Name,
            ObservabilityNaming.Methods.Publish,
            domainEvent);
    }

    /// <summary>
    /// 도메인 이벤트 발행 응답 성공 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventPublisherResponseSuccess<TEvent>(
        this ILogger logger,
        TEvent domainEvent,
        double elapsed)
        where TEvent : IDomainEvent
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseSuccess,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            typeof(TEvent).Name,
            ObservabilityNaming.Methods.Publish,
            ObservabilityNaming.Status.Success,
            elapsed);
    }

    /// <summary>
    /// 도메인 이벤트 발행 응답 경고 로그를 출력합니다 (예상된 에러).
    /// </summary>
    public static void LogDomainEventPublisherResponseWarning<TEvent>(
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
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseWarning,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            typeof(TEvent).Name,
            ObservabilityNaming.Methods.Publish,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// 도메인 이벤트 발행 응답 에러 로그를 출력합니다 (예외적 에러).
    /// </summary>
    public static void LogDomainEventPublisherResponseError<TEvent>(
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
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseError,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            typeof(TEvent).Name,
            ObservabilityNaming.Methods.Publish,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// Aggregate의 모든 도메인 이벤트 발행 요청 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventsPublisherRequest(
        this ILogger logger,
        string aggregateType,
        string methodName,
        int eventCount)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterRequest,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} requesting with {request.event.count} events",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            aggregateType,
            methodName,
            eventCount);
    }

    /// <summary>
    /// Aggregate의 모든 도메인 이벤트 발행 응답 성공 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventsPublisherResponseSuccess(
        this ILogger logger,
        string aggregateType,
        string methodName,
        int eventCount,
        double elapsed)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseSuccess,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            aggregateType,
            methodName,
            ObservabilityNaming.Status.Success,
            elapsed,
            eventCount);
    }

    /// <summary>
    /// Aggregate의 도메인 이벤트 발행 응답 에러 로그를 출력합니다 (예외적 에러).
    /// </summary>
    public static void LogDomainEventsPublisherResponseError(
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
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseError,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            aggregateType,
            methodName,
            ObservabilityNaming.Status.Failure,
            elapsed,
            eventCount,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// Aggregate의 도메인 이벤트 발행 응답 경고 로그를 출력합니다 (예상된 에러).
    /// </summary>
    public static void LogDomainEventsPublisherResponseWarning(
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
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseWarning,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events with {error.type}:{error.code} {@error}",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            aggregateType,
            methodName,
            ObservabilityNaming.Status.Failure,
            elapsed,
            eventCount,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// Aggregate의 도메인 이벤트 부분 실패 응답 경고 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventsPublisherResponsePartialFailure(
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
            eventId: ObservabilityNaming.EventIds.Adapter.AdapterResponseWarning,
            message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {request.event.count} events partial failure: {response.event.success_count} succeeded, {response.event.failure_count} failed",
            ObservabilityNaming.Layers.Adapter,
            ObservabilityNaming.Categories.Event,
            aggregateType,
            methodName,
            ObservabilityNaming.Status.Failure,
            elapsed,
            eventCount,
            successCount,
            failureCount);
    }
}
