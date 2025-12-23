using Functorium.Abstractions.Errors;

using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// Usecase Pipeline에서 사용하는 로거 확장 메서드
/// </summary>
public static class UsecaseLoggerExtensions
{
    /// <summary>
    /// 요청 메시지를 로깅합니다.
    /// </summary>
    public static void LogRequestMessage<TRequest>(
        this ILogger logger,
        string layer,
        string category,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        TRequest request)
    {
        logger.LogInformation(
            "[{Layer}] [{Category}] [{RequestCqrs}] {RequestHandler}.{RequestHandlerMethod} - Request: {@Request}",
            layer,
            category,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            request);
    }

    /// <summary>
    /// 성공 응답 메시지를 로깅합니다.
    /// </summary>
    public static void LogResponseMessage<TResponse>(
        this ILogger logger,
        string layer,
        string category,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        TResponse response,
        string status,
        double elapsed)
    {
        logger.LogInformation(
            "[{Layer}] [{Category}] [{RequestCqrs}] {RequestHandler}.{RequestHandlerMethod} - Response: {@Response}, Status: {Status}, Elapsed: {Elapsed}ms",
            layer,
            category,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            response,
            status,
            elapsed);
    }

    /// <summary>
    /// 에러 응답 메시지를 로깅합니다 (Exceptional - Error 레벨).
    /// </summary>
    public static void LogResponseMessageError(
        this ILogger logger,
        string layer,
        string category,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        string status,
        double elapsed,
        Error error)
    {
        logger.LogError(
            "[{Layer}] [{Category}] [{RequestCqrs}] {RequestHandler}.{RequestHandlerMethod} - Status: {Status}, Elapsed: {Elapsed}ms, Error: {@Error}",
            layer,
            category,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            status,
            elapsed,
            error);
    }

    /// <summary>
    /// 경고 응답 메시지를 로깅합니다 (Expected - Warning 레벨).
    /// </summary>
    public static void LogResponseMessageWarning(
        this ILogger logger,
        string layer,
        string category,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        string status,
        double elapsed,
        Error error)
    {
        logger.LogWarning(
            "[{Layer}] [{Category}] [{RequestCqrs}] {RequestHandler}.{RequestHandlerMethod} - Status: {Status}, Elapsed: {Elapsed}ms, Error: {@Error}",
            layer,
            category,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            status,
            elapsed,
            error);
    }
}
