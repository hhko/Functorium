using Microsoft.Extensions.Logging;

using Serilog.Events;

namespace Functorium.Testing.Arrangements.Loggers;

/// <summary>
/// Serilog의 구조화된 로깅을 제대로 활용하는 TestLogger
/// LoggerMessage 어트리뷰트로 생성된 메서드들의 구조화된 로깅을 올바르게 처리합니다.
/// </summary>
public class StructuredTestLogger<T> : ILogger<T>
{
    private readonly Serilog.ILogger _serilogLogger;

    public StructuredTestLogger(Serilog.ILogger serilogLogger)
    {
        _serilogLogger = serilogLogger;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var serilogLevel = logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };

        // LoggerMessage 어트리뷰트로 생성된 메서드들은 구조화된 로깅을 사용합니다.
        // 이 경우 state는 IReadOnlyList<KeyValuePair<string, object?>> 형태입니다.
        if (state is IReadOnlyList<KeyValuePair<string, object?>> structuredState)
        {
            // LoggerMessage는 OriginalFormat이 마지막에 위치하고, 나머지가 속성들입니다.
            var originalFormat = structuredState.LastOrDefault(x => x.Key == "{OriginalFormat}").Value?.ToString() ?? "";

            // OriginalFormat을 제외한 나머지 항목들은 속성들입니다.
            var properties = structuredState.Where(x => x.Key != "{OriginalFormat}").ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);

            // {@Error:Error} 형태의 모든 명시적 속성명 처리
            var processedProperties = new Dictionary<string, object?>();
            foreach (var kvp in properties)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                // {@Error}일 때 매개변수 이름이 Error error인 경우, 출력 시 변수 이름 그대로 error로 노출되는 문제를 해결하기 위해
                // OriginalFormat에서 {@Error:Error} 형태의 명시적 속성명 추출
                var explicitPropertyPattern = @"\{@" + key + @":([^}]+)\}";
                var match = System.Text.RegularExpressions.Regex.Match(originalFormat, explicitPropertyPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    key = match.Groups[1].Value; // 명시적으로 지정된 속성명 사용
                }

                processedProperties[key] = value;
            }

            // OriginalFormat도 속성에 포함시켜서 Verify에서 확인할 수 있도록 함
            processedProperties["OriginalFormat"] = originalFormat;

            // Serilog의 구조화된 로깅 사용
            if (processedProperties.Count > 0)
            {
                // LogEvent를 직접 생성하여 속성명을 명시적으로 지정
                var logEventProperties = processedProperties.Select(kvp =>
                    new LogEventProperty(kvp.Key, new ScalarValue(kvp.Value))).ToList();

                var logEvent = new LogEvent(
                    DateTimeOffset.Now,
                    serilogLevel,
                    exception,
                    MessageTemplate.Empty,
                    logEventProperties);

                _serilogLogger.Write(logEvent);
            }
            else
            {
                _serilogLogger.Write(serilogLevel, exception, originalFormat);
            }
        }
        else
        {
            // 일반적인 로깅 (fallback)
            _serilogLogger.Write(serilogLevel, exception, formatter(state, exception));
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
