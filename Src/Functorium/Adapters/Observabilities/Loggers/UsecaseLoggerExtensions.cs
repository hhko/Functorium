using Functorium.Adapters.Observabilities.Naming;

using LanguageExt.Common;

using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// Usecase Pipeline에서 사용하는 로거 확장 메서드.
/// BeginScope를 제거하여 필드 중복과 Dictionary 할당을 방지합니다.
/// </summary>
/// <remarks>
/// Application 레이어의 로그는 파라미터 수가 6개를 초과하여
/// LoggerMessage.Define을 사용할 수 없습니다.
/// (LoggerMessage.Define은 최대 6개 타입 파라미터만 지원)
/// </remarks>
public static class UsecaseLoggerExtensions
{
    // ===== Request =====

    /// <summary>
    /// Request 로그를 출력합니다.
    /// </summary>
    /// <remarks>
    /// 제네릭 타입 T로 인해 LoggerMessage.Define을 사용할 수 없어 직접 호출합니다.
    /// </remarks>
    public static void LogRequestMessage<T>(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        T? request)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationRequest,
            message: "{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} {@request.message} requesting",
            requestLayer,
            requestCategory,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            request);
    }

    // ===== Response - 성공 =====

    /// <summary>
    /// Response 성공 로그를 출력합니다.
    /// </summary>
    /// <remarks>
    /// 제네릭 타입 T로 인해 LoggerMessage.Define을 사용할 수 없어 직접 호출합니다.
    /// </remarks>
    public static void LogResponseMessageSuccess<T>(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        T? response,
        string responseStatus,
        double responseElapsed)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseSuccess,
            message: "{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} {@response.message} responded {response.status} in {response.elapsed:0.0000} s",
            requestLayer,
            requestCategory,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            response,
            responseStatus,
            responseElapsed);
    }

    // ===== Response - 실패, 경고 ErrorCodeExpected =====

    /// <summary>
    /// Response 경고 로그를 출력합니다 (예상된 에러).
    /// </summary>
    /// <remarks>
    /// Error 필드 로깅이 필요한 경우 직접 호출을 사용합니다.
    /// LoggerMessage.Define은 7개 파라미터까지만 지원합니다.
    /// </remarks>
    public static void LogResponseMessageWarning(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        string responseStatus,
        double responseElapsed,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        // Error 객체를 포함하여 로깅 (LoggerMessage.Define 파라미터 제한으로 직접 호출)
        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseWarning,
            message: "{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@error}",
            requestLayer,
            requestCategory,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            responseStatus,
            responseElapsed,
            error);
    }

    // ===== Response - 실패, 에러 ErrorCodeExceptional =====

    /// <summary>
    /// Response 에러 로그를 출력합니다 (예외적 에러).
    /// </summary>
    /// <remarks>
    /// Error 필드 로깅이 필요한 경우 직접 호출을 사용합니다.
    /// LoggerMessage.Define은 7개 파라미터까지만 지원합니다.
    /// </remarks>
    public static void LogResponseMessageError(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        string responseStatus,
        double responseElapsed,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        // Error 객체를 포함하여 로깅 (LoggerMessage.Define 파라미터 제한으로 직접 호출)
        logger.LogError(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseError,
            message: "{request.layer} {request.category}.{request.handler.cqrs} {request.handler}.{request.handler.method} responded {response.status} in {response.elapsed:0.0000} s with {@error}",
            requestLayer,
            requestCategory,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            responseStatus,
            responseElapsed,
            error);
    }
}
