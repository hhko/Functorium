using System.Diagnostics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders.Configurators;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Trace;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Configurators;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class TracingConfiguratorTests
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

    #region AddSource Tests

    [Fact]
    public void AddSource_ReturnsConfigurator_WhenCalled()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act
        var actual = sut.AddSource("TestSource");

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void AddSource_ThrowsArgumentException_WhenSourceNameIsNull()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.AddSource(null!));
    }

    [Fact]
    public void AddSource_ThrowsArgumentException_WhenSourceNameIsEmpty()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.AddSource(string.Empty));
    }

    [Fact]
    public void AddSource_ThrowsArgumentException_WhenSourceNameIsWhitespace()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.AddSource("   "));
    }

    #endregion

    #region AddProcessor Tests

    [Fact]
    public void AddProcessor_ReturnsConfigurator_WhenCalled()
    {
        // Arrange
        var sut = CreateConfigurator();
        var processor = new TestProcessor();

        // Act
        var actual = sut.AddProcessor(processor);

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void AddProcessor_ThrowsArgumentNullException_WhenProcessorIsNull()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.AddProcessor(null!));
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

    #endregion

    #region Apply Tests

    [Fact]
    public void Apply_AddsSource_WhenSourceWasAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();
        sut.AddSource("TestSource");

        // Act
        ApplyConfigurator(sut, tracerProviderBuilder);

        // Assert
        tracerProviderBuilder.Received(1).AddSource("TestSource");
    }

    [Fact]
    public void Apply_AddsMultipleSources_WhenMultipleSourcesWereAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();
        sut
            .AddSource("TestSource1")
            .AddSource("TestSource2");

        // Act
        ApplyConfigurator(sut, tracerProviderBuilder);

        // Assert
        tracerProviderBuilder.Received(1).AddSource("TestSource1");
        tracerProviderBuilder.Received(1).AddSource("TestSource2");
    }

    [Fact]
    public void Apply_AddsProcessor_WhenProcessorWasAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();
        var processor = new TestProcessor();
        sut.AddProcessor(processor);

        // Act
        ApplyConfigurator(sut, tracerProviderBuilder);

        // Assert
        tracerProviderBuilder.Received(1).AddProcessor(processor);
    }

    [Fact]
    public void Apply_InvokesConfiguration_WhenConfigurationWasAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();
        bool configurationInvoked = false;
        sut.Configure(_ => configurationInvoked = true);

        // Act
        ApplyConfigurator(sut, tracerProviderBuilder);

        // Assert
        configurationInvoked.ShouldBeTrue();
    }

    #endregion

    #region Chaining Tests

    [Fact]
    public void Chaining_AllowsFluentConfiguration()
    {
        // Arrange
        var sut = CreateConfigurator();
        var tracerProviderBuilder = Substitute.For<TracerProviderBuilder>();
        var processor = new TestProcessor();
        bool configurationInvoked = false;

        // Act
        sut
            .AddSource("TestSource1")
            .AddSource("TestSource2")
            .AddProcessor(processor)
            .Configure(_ => configurationInvoked = true);

        ApplyConfigurator(sut, tracerProviderBuilder);

        // Assert
        tracerProviderBuilder.Received(1).AddSource("TestSource1");
        tracerProviderBuilder.Received(1).AddSource("TestSource2");
        tracerProviderBuilder.Received(1).AddProcessor(processor);
        configurationInvoked.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods and Test Classes

    private TracingConfigurator CreateConfigurator()
    {
        // TracingConfigurator의 생성자는 internal이므로 리플렉션 사용
        var constructor = typeof(TracingConfigurator)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                [typeof(OpenTelemetryOptions)],
                null);

        return (TracingConfigurator)constructor!.Invoke([_options]);
    }

    private static void ApplyConfigurator(TracingConfigurator configurator, TracerProviderBuilder tracerProviderBuilder)
    {
        // Apply 메서드는 internal이므로 리플렉션 사용
        var applyMethod = typeof(TracingConfigurator)
            .GetMethod("Apply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        applyMethod!.Invoke(configurator, [tracerProviderBuilder]);
    }

    private class TestProcessor : BaseProcessor<Activity>
    {
    }

    #endregion
}
