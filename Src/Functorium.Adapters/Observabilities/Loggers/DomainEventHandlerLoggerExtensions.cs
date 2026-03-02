using Functorium.Adapters.Observabilities.Naming;
using Functorium.Domains.Events;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// 도메인 이벤트 핸들러에서 사용하는 로거 확장 메서드.
/// Handler 관점의 관찰 가능성을 제공합니다.
/// </summary>
public static class DomainEventHandlerLoggerExtensions
{
    /// <summary>
    /// 도메인 이벤트 핸들러 요청 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventHandlerRequest<TEvent>(
        this ILogger logger,
        string handlerName,
        TEvent domainEvent)
        where TEvent : IDomainEvent
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationRequest,
            message: "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} requesting with {@request.message}",
            ObservabilityNaming.Layers.Application,
            ObservabilityNaming.Categories.Usecase,
            ObservabilityNaming.CategoryTypes.Event,
            handlerName,
            ObservabilityNaming.Methods.Handle,
            domainEvent.GetType().Name,
            domainEvent.EventId.ToString(),
            domainEvent);
    }

    /// <summary>
    /// 도메인 이벤트 핸들러 응답 성공 로그를 출력합니다.
    /// </summary>
    public static void LogDomainEventHandlerResponseSuccess(
        this ILogger logger,
        string handlerName,
        string eventTypeName,
        string eventId,
        double elapsed)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseSuccess,
            message: "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s",
            ObservabilityNaming.Layers.Application,
            ObservabilityNaming.Categories.Usecase,
            ObservabilityNaming.CategoryTypes.Event,
            handlerName,
            ObservabilityNaming.Methods.Handle,
            eventTypeName,
            eventId,
            ObservabilityNaming.Status.Success,
            elapsed);
    }

    /// <summary>
    /// 도메인 이벤트 핸들러 응답 경고 로그를 출력합니다 (예상된 에러).
    /// </summary>
    public static void LogDomainEventHandlerResponseWarning(
        this ILogger logger,
        string handlerName,
        string eventTypeName,
        string eventId,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseWarning,
            message: "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.Layers.Application,
            ObservabilityNaming.Categories.Usecase,
            ObservabilityNaming.CategoryTypes.Event,
            handlerName,
            ObservabilityNaming.Methods.Handle,
            eventTypeName,
            eventId,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// 도메인 이벤트 핸들러 응답 에러 로그를 출력합니다 (예외적 에러).
    /// </summary>
    public static void LogDomainEventHandlerResponseError(
        this ILogger logger,
        string handlerName,
        string eventTypeName,
        string eventId,
        double elapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        logger.LogError(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseError,
            message: "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            ObservabilityNaming.Layers.Application,
            ObservabilityNaming.Categories.Usecase,
            ObservabilityNaming.CategoryTypes.Event,
            handlerName,
            ObservabilityNaming.Methods.Handle,
            eventTypeName,
            eventId,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode,
            error);
    }

    /// <summary>
    /// 도메인 이벤트 핸들러 응답 에러 로그를 출력합니다 (예외 포함).
    /// </summary>
    public static void LogDomainEventHandlerResponseError(
        this ILogger logger,
        string handlerName,
        string eventTypeName,
        string eventId,
        double elapsed,
        string errorType,
        string errorCode,
        Exception exception)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        logger.LogError(
            ObservabilityNaming.EventIds.Application.ApplicationResponseError,
            exception,
            "{request.layer} {request.category}.{request.category.type} {request.handler}.{request.handler.method} {request.event.type} {request.event.id} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code}",
            ObservabilityNaming.Layers.Application,
            ObservabilityNaming.Categories.Usecase,
            ObservabilityNaming.CategoryTypes.Event,
            handlerName,
            ObservabilityNaming.Methods.Handle,
            eventTypeName,
            eventId,
            ObservabilityNaming.Status.Failure,
            elapsed,
            errorType,
            errorCode);
    }
}
