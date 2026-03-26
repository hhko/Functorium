using System.Diagnostics;

using Functorium.Adapters.Observabilities.Naming;
using Functorium.Domains.Observabilities;

using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities;

/// <summary>
/// IObservableSignalFactory 구현.
/// ObservableSignalScope에서 공통 필드를 가져오고, ILogger + Activity Event로 출력합니다.
/// </summary>
/// <remarks>
/// <para>Pillar 범위:</para>
/// <list type="bullet">
/// <item>Debug → Logging만</item>
/// <item>Warning → Logging + Activity Event</item>
/// <item>Error → Logging + Activity Event</item>
/// </list>
/// </remarks>
internal sealed class ObservableSignalFactory : IObservableSignalFactory
{
    public void Log(LogLevel level, string message, (string Key, object? Value)[]? context, Exception? exception)
    {
        var scope = ObservableSignalScope.Current;
        if (scope is null)
            return; // 스코프 밖에서 호출 시 no-op

        var logger = scope.Logger;
        if (!logger.IsEnabled(level))
            return;

        // EventId 결정
        var eventId = level switch
        {
            LogLevel.Debug => ObservabilityNaming.EventIds.Adapter.ObservableSignalDebug,
            LogLevel.Warning => ObservabilityNaming.EventIds.Adapter.ObservableSignalWarning,
            LogLevel.Error => ObservabilityNaming.EventIds.Adapter.ObservableSignalError,
            _ => ObservabilityNaming.EventIds.Adapter.ObservableSignalDebug,
        };

        // 구조화된 로그 출력 — 공통 필드 + 개발자 메시지 + 부가 컨텍스트
        if (context is { Length: > 0 })
        {
            // 부가 컨텍스트를 Dictionary로 변환하여 구조화 로깅
            var contextDict = new Dictionary<string, object?>(context.Length);
            foreach (var (key, value) in context)
            {
                contextDict[key] = value;
            }

            logger.Log(
                logLevel: level,
                eventId: eventId,
                exception: exception,
                message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} — {adapter.log.message} {@adapter.log.context}",
                scope.Layer,
                scope.Category,
                scope.Handler,
                scope.Method,
                message,
                contextDict);
        }
        else
        {
            logger.Log(
                logLevel: level,
                eventId: eventId,
                exception: exception,
                message: "{request.layer} {request.category.name} {request.handler.name}.{request.handler.method} — {adapter.log.message}",
                scope.Layer,
                scope.Category,
                scope.Handler,
                scope.Method,
                message);
        }

        // Warning/Error → Activity Event 추가 (Tracing)
        if (level >= LogLevel.Warning)
        {
            AddActivityEvent(level, message, scope, context);
        }
    }

    private static void AddActivityEvent(
        LogLevel level,
        string message,
        ObservableSignalScope scope,
        (string Key, object? Value)[]? context)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        var tags = new ActivityTagsCollection
        {
            { "adapter.log.level", level.ToString().ToLowerInvariant() },
            { "adapter.log.message", message },
            { ObservabilityNaming.CustomAttributes.RequestCategoryName, scope.Category },
            { ObservabilityNaming.CustomAttributes.RequestHandlerName, scope.Handler },
            { ObservabilityNaming.CustomAttributes.RequestHandlerMethod, scope.Method },
        };

        if (context is not null)
        {
            foreach (var (key, value) in context)
            {
                tags[key] = value;
            }
        }

        activity.AddEvent(new ActivityEvent("adapter.signal", tags: tags));
    }
}
