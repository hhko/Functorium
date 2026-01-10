using Functorium.Adapters.Observabilities.Naming;

using LanguageExt.Common;

using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// Usecase Pipeline에서 사용하는 로거 확장 메서드
/// </summary>
public static class UsecaseLoggerExtensions
{
    // Request

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

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [ObservabilityNaming.LogKeys.RequestLayer] = requestLayer,
            [ObservabilityNaming.LogKeys.RequestCategory] = requestCategory,
            [ObservabilityNaming.LogKeys.RequestHandler] = requestHandler,
            [ObservabilityNaming.LogKeys.RequestHandlerCqrs] = requestCqrs,
            [ObservabilityNaming.LogKeys.RequestHandlerMethod] = requestHandlerMethod,
            [ObservabilityNaming.LogKeys.RequestData] = request
        });

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationRequest,
            message: "{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} {@Request:Request} requesting",
                requestLayer,
                requestCategory,
                requestCqrs,
                requestHandler,
                requestHandlerMethod,
                request);
    }

    // Response - 성공
    public static void LogResponseMessageSuccess<T>(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        T? response,
        string status,
        double elapsed)
    {
        if (!logger.IsEnabled(LogLevel.Information))
            return;

        // response를 그대로 사용 (Fin<T>에서 값 추출은 호출자에서 처리)
        T? value = response;

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [ObservabilityNaming.LogKeys.RequestLayer] = requestLayer,
            [ObservabilityNaming.LogKeys.RequestCategory] = requestCategory,
            [ObservabilityNaming.LogKeys.RequestHandler] = requestHandler,
            [ObservabilityNaming.LogKeys.RequestHandlerCqrs] = requestCqrs,
            [ObservabilityNaming.LogKeys.RequestHandlerMethod] = requestHandlerMethod,

            [ObservabilityNaming.LogKeys.ResponseData] = value,
            [ObservabilityNaming.LogKeys.ResponseStatus] = status,
            [ObservabilityNaming.LogKeys.ResponseElapsed] = elapsed,
        });

        logger.LogInformation(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseSuccess,
            message: "{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} {@Response:Response} responded {Status} in {Elapsed:0.0000} ms",
                requestLayer,
                requestCategory,
                requestCqrs,
                requestHandler,
                requestHandlerMethod,
                value,
                status,
                elapsed);
    }

    // Response - 실패, 경고 ErrorCodeExpected
    public static void LogResponseMessageWarning(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        string status,
        double elapsed,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [ObservabilityNaming.LogKeys.RequestLayer] = requestLayer,
            [ObservabilityNaming.LogKeys.RequestCategory] = requestCategory,
            [ObservabilityNaming.LogKeys.RequestHandler] = requestHandler,
            [ObservabilityNaming.LogKeys.RequestHandlerCqrs] = requestCqrs,
            [ObservabilityNaming.LogKeys.RequestHandlerMethod] = requestHandlerMethod,

            [ObservabilityNaming.LogKeys.ResponseStatus] = status,
            [ObservabilityNaming.LogKeys.ResponseElapsed] = elapsed,
            [ObservabilityNaming.LogKeys.ErrorData] = error
        });

        logger.LogWarning(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseWarning,
            message: "{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}",
                requestLayer,
                requestCategory,
                requestCqrs,
                requestHandler,
                requestHandlerMethod,
                status,
                elapsed,
                error);
    }

    // Response - 실패, 에러 ErrorCodeExceptional
    public static void LogResponseMessageError(
        this ILogger logger,
        string requestLayer,
        string requestCategory,
        string requestCqrs,
        string requestHandler,
        string requestHandlerMethod,
        string status,
        double elapsed,
        Error error)
    {
        if (!logger.IsEnabled(LogLevel.Error))
            return;

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [ObservabilityNaming.LogKeys.RequestLayer] = requestLayer,
            [ObservabilityNaming.LogKeys.RequestCategory] = requestCategory,
            [ObservabilityNaming.LogKeys.RequestHandler] = requestHandler,
            [ObservabilityNaming.LogKeys.RequestHandlerCqrs] = requestCqrs,
            [ObservabilityNaming.LogKeys.RequestHandlerMethod] = requestHandlerMethod,

            [ObservabilityNaming.LogKeys.ResponseStatus] = status,
            [ObservabilityNaming.LogKeys.ResponseElapsed] = elapsed,
            [ObservabilityNaming.LogKeys.ErrorData] = error
        });

        logger.LogError(
            eventId: ObservabilityNaming.EventIds.Application.ApplicationResponseError,
            message: "{RequestLayer} {RequestCategory}.{RequestHandlerCqrs} {RequestHandler}.{RequestHandlerMethod} responded {Status} in {Elapsed:0.0000} ms with {@Error:Error}",
                requestLayer,
                requestCategory,
                requestCqrs,
                requestHandler,
                requestHandlerMethod,
                status,
                elapsed,
                error);
    }
}
