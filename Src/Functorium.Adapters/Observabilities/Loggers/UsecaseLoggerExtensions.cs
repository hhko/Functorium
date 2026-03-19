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
    /// Usecase 요청 로그를 출력합니다.
    /// </summary>
    /// <remarks>
    /// 제네릭 타입 T로 인해 LoggerMessage.Define을 사용할 수 없어 직접 호출합니다.
    /// </remarks>
    public static void LogUsecaseRequest<T>(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCategoryType,
        string requestHandler,
        string requestHandlerMethod,
        T? request)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationRequest,
            message: "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} requesting with {@request.message}",
            requestLayer,
            requestCategory,
            requestCategoryType,
            requestHandler,
            requestHandlerMethod,
            request);
    }

    // ===== Response - 성공 =====

    /// <summary>
    /// Usecase 응답 성공 로그를 출력합니다.
    /// </summary>
    /// <remarks>
    /// 제네릭 타입 T로 인해 LoggerMessage.Define을 사용할 수 없어 직접 호출합니다.
    /// </remarks>
    public static void LogUsecaseResponseSuccess<T>(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCategoryType,
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
            message: "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} responded {response.status} in {response.elapsed:0.0000} s with {@response.message}",
            requestLayer,
            requestCategory,
            requestCategoryType,
            requestHandler,
            requestHandlerMethod,
            responseStatus,
            responseElapsed,
            response);
    }

    // ===== Response - 실패, 경고 ErrorCodeExpected =====

    /// <summary>
    /// Usecase 응답 경고 로그를 출력합니다 (예상된 에러).
    /// </summary>
    /// <remarks>
    /// Error 필드 로깅이 필요한 경우 직접 호출을 사용합니다.
    /// LoggerMessage.Define은 7개 파라미터까지만 지원합니다.
    /// </remarks>
    public static void LogUsecaseResponseWarning(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCategoryType,
        string requestHandler,
        string requestHandlerMethod,
        string responseStatus,
        double responseElapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        // Error 객체를 포함하여 로깅 (LoggerMessage.Define 파라미터 제한으로 직접 호출)
        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseWarning,
            message: "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            requestLayer,
            requestCategory,
            requestCategoryType,
            requestHandler,
            requestHandlerMethod,
            responseStatus,
            responseElapsed,
            errorType,
            errorCode,
            error);
    }

    // ===== Response - 실패, 에러 ErrorCodeExceptional =====

    /// <summary>
    /// Usecase 응답 에러 로그를 출력합니다 (예외적 에러).
    /// </summary>
    /// <remarks>
    /// Error 필드 로깅이 필요한 경우 직접 호출을 사용합니다.
    /// LoggerMessage.Define은 7개 파라미터까지만 지원합니다.
    /// </remarks>
    public static void LogUsecaseResponseError(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCategoryType,
        string requestHandler,
        string requestHandlerMethod,
        string responseStatus,
        double responseElapsed,
        string errorType,
        string errorCode,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        // Error 객체를 포함하여 로깅 (LoggerMessage.Define 파라미터 제한으로 직접 호출)
        logger.LogError(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseError,
            message: "{request.layer} {request.category}.{request.category_type} {request.handler}.{request.handler_method} responded {response.status} in {response.elapsed:0.0000} s with {error.type}:{error.code} {@error}",
            requestLayer,
            requestCategory,
            requestCategoryType,
            requestHandler,
            requestHandlerMethod,
            responseStatus,
            responseElapsed,
            errorType,
            errorCode,
            error);
    }
}
