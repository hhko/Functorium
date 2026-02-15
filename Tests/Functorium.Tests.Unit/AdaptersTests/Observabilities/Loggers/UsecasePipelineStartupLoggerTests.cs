using Functorium.Adapters.Observabilities.Loggers;
using Microsoft.Extensions.Logging;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Loggers;

/// <summary>
/// UsecasePipelineStartupLogger 테스트
/// </summary>
public sealed class UsecasePipelineStartupLoggerTests
{
    [Fact]
    public void LogConfiguration_OutputsCommandAndQueryPipelines()
    {
        // Arrange
        var logger = new CapturingLogger();
        var sut = CreateLogger(
            commandPipelines: ["Metrics", "Tracing", "Logging", "Handler"],
            queryPipelines: ["Metrics", "Tracing", "Logging", "Handler"]);

        // Act
        sut.LogConfiguration(logger);

        // Assert
        var messages = logger.Messages;
        messages.ShouldContain(m => m.Contains("Command Pipeline"));
        messages.ShouldContain(m => m.Contains("Query Pipeline"));
    }

    [Fact]
    public void LogConfiguration_CommandPipelineIncludesTransaction_QueryExcludes()
    {
        // Arrange
        var logger = new CapturingLogger();
        var sut = CreateLogger(
            commandPipelines: ["Metrics", "Exception", "Transaction", "Handler"],
            queryPipelines: ["Metrics", "Exception", "Handler"]);

        // Act
        sut.LogConfiguration(logger);

        // Assert
        var messages = logger.Messages;
        messages.ShouldContain(m => m.Contains("Metrics → Exception → Transaction → Handler"));
        messages.ShouldContain(m => m.Contains("Metrics → Exception → Handler"));
    }

    /// <summary>
    /// UsecasePipelineStartupLogger를 생성합니다.
    /// internal 생성자이므로 리플렉션을 사용합니다.
    /// </summary>
    private static UsecasePipelineStartupLogger CreateLogger(
        IReadOnlyList<string> commandPipelines,
        IReadOnlyList<string> queryPipelines)
    {
        return (UsecasePipelineStartupLogger)Activator.CreateInstance(
            typeof(UsecasePipelineStartupLogger),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: [commandPipelines, queryPipelines],
            culture: null)!;
    }

    #region Test Fixtures

    /// <summary>
    /// 로그 메시지를 캡처하는 간단한 ILogger 구현
    /// </summary>
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    #endregion
}
