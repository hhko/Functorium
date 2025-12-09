using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders.Configurators;
using NSubstitute;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Configurators;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class LoggingConfiguratorTests
{
    private readonly OpenTelemetryOptions _options = new()
    {
        ServiceName = "TestService",
        CollectorEndpoint = "http://localhost:4317"
    };

    #region Options Property Tests

    [Fact]
    public void Options_ReturnsProvidedOptions_WhenAccessed()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act
        var actual = sut.Options;

        // Assert
        actual.ShouldBe(_options);
    }

    #endregion

    #region AddDestructuringPolicy Tests

    [Fact]
    public void AddDestructuringPolicy_ReturnsConfigurator_WhenCalled()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act
        var actual = sut.AddDestructuringPolicy<TestDestructuringPolicy>();

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void AddDestructuringPolicy_AppliesPolicyToLoggerConfiguration_WhenApplyCalled()
    {
        // Arrange
        var sut = CreateConfigurator();
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        sut.AddDestructuringPolicy<TestDestructuringPolicy>();
        ApplyConfigurator(sut, loggerConfiguration);

        // Assert - Logger builds successfully with policy applied
        using var logger = loggerConfiguration.CreateLogger();
        logger.ShouldNotBeNull();
    }

    #endregion

    #region AddEnricher Tests

    [Fact]
    public void AddEnricher_ReturnsConfigurator_WhenCalledWithInstance()
    {
        // Arrange
        var sut = CreateConfigurator();
        var enricher = new TestEnricher();

        // Act
        var actual = sut.AddEnricher(enricher);

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void AddEnricher_ThrowsArgumentNullException_WhenEnricherIsNull()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.AddEnricher(null!));
    }

    [Fact]
    public void AddEnricherGeneric_ReturnsConfigurator_WhenCalled()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act
        var actual = sut.AddEnricher<TestEnricher>();

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void AddEnricher_AppliesEnricherToLoggerConfiguration_WhenApplyCalled()
    {
        // Arrange
        var sut = CreateConfigurator();
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        sut.AddEnricher<TestEnricher>();
        ApplyConfigurator(sut, loggerConfiguration);

        // Assert - Logger builds successfully with enricher applied
        using var logger = loggerConfiguration.CreateLogger();
        logger.ShouldNotBeNull();
    }

    #endregion

    #region Configure Tests

    [Fact]
    public void Configure_ReturnsConfigurator_WhenCalled()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act
        var actual = sut.Configure(_ => { });

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void Configure_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.Configure(null!));
    }

    [Fact]
    public void Configure_AppliesConfigurationToLoggerConfiguration_WhenApplyCalled()
    {
        // Arrange
        var sut = CreateConfigurator();
        var loggerConfiguration = new LoggerConfiguration();
        bool configurationApplied = false;

        // Act
        sut.Configure(_ => configurationApplied = true);
        ApplyConfigurator(sut, loggerConfiguration);

        // Assert
        configurationApplied.ShouldBeTrue();
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Chaining_AllowsFluentConfiguration()
    {
        // Arrange
        var sut = CreateConfigurator();
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        sut
            .AddDestructuringPolicy<TestDestructuringPolicy>()
            .AddEnricher<TestEnricher>()
            .Configure(_ => { });

        ApplyConfigurator(sut, loggerConfiguration);

        // Assert - Logger builds successfully with all configurations applied
        using var logger = loggerConfiguration.CreateLogger();
        logger.ShouldNotBeNull();
    }

    #endregion

    #region Helper Methods and Test Classes

    private LoggingConfigurator CreateConfigurator()
    {
        // LoggingConfigurator의 생성자는 internal이므로 리플렉션 사용
        var constructor = typeof(LoggingConfigurator)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                [typeof(OpenTelemetryOptions)],
                null);

        return (LoggingConfigurator)constructor!.Invoke([_options]);
    }

    private static void ApplyConfigurator(LoggingConfigurator configurator, LoggerConfiguration loggerConfiguration)
    {
        // Apply 메서드는 internal이므로 리플렉션 사용
        var applyMethod = typeof(LoggingConfigurator)
            .GetMethod("Apply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        applyMethod!.Invoke(configurator, [loggerConfiguration]);
    }

    private class TestDestructuringPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LogEventPropertyValue? result)
        {
            result = null;
            return false;
        }
    }

    private class TestEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TestProperty", "TestValue"));
        }
    }

    #endregion
}
