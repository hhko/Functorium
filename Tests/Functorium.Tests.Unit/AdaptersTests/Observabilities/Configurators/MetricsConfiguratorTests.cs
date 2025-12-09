using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders.Configurators;
using NSubstitute;
using OpenTelemetry.Metrics;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Configurators;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class MetricsConfiguratorTests
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

    #region AddMeter Tests

    [Fact]
    public void AddMeter_ReturnsConfigurator_WhenCalled()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act
        var actual = sut.AddMeter("TestMeter");

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void AddMeter_ThrowsArgumentException_WhenMeterNameIsNull()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.AddMeter(null!));
    }

    [Fact]
    public void AddMeter_ThrowsArgumentException_WhenMeterNameIsEmpty()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.AddMeter(string.Empty));
    }

    [Fact]
    public void AddMeter_ThrowsArgumentException_WhenMeterNameIsWhitespace()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentException>(() => sut.AddMeter("   "));
    }

    #endregion

    #region AddInstrumentation Tests

    [Fact]
    public void AddInstrumentation_ReturnsConfigurator_WhenCalled()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act
        var actual = sut.AddInstrumentation(_ => { });

        // Assert
        actual.ShouldBe(sut);
    }

    [Fact]
    public void AddInstrumentation_ThrowsArgumentNullException_WhenActionIsNull()
    {
        // Arrange
        var sut = CreateConfigurator();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.AddInstrumentation(null!));
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
    public void Apply_AddsMeter_WhenMeterWasAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var meterProviderBuilder = Substitute.For<MeterProviderBuilder>();
        sut.AddMeter("TestMeter");

        // Act
        ApplyConfigurator(sut, meterProviderBuilder);

        // Assert
        meterProviderBuilder.Received(1).AddMeter("TestMeter");
    }

    [Fact]
    public void Apply_AddsMultipleMeters_WhenMultipleMetersWereAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var meterProviderBuilder = Substitute.For<MeterProviderBuilder>();
        sut
            .AddMeter("TestMeter1")
            .AddMeter("TestMeter2");

        // Act
        ApplyConfigurator(sut, meterProviderBuilder);

        // Assert
        meterProviderBuilder.Received(1).AddMeter("TestMeter1");
        meterProviderBuilder.Received(1).AddMeter("TestMeter2");
    }

    [Fact]
    public void Apply_InvokesInstrumentation_WhenInstrumentationWasAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var meterProviderBuilder = Substitute.For<MeterProviderBuilder>();
        bool instrumentationInvoked = false;
        sut.AddInstrumentation(_ => instrumentationInvoked = true);

        // Act
        ApplyConfigurator(sut, meterProviderBuilder);

        // Assert
        instrumentationInvoked.ShouldBeTrue();
    }

    [Fact]
    public void Apply_InvokesConfiguration_WhenConfigurationWasAdded()
    {
        // Arrange
        var sut = CreateConfigurator();
        var meterProviderBuilder = Substitute.For<MeterProviderBuilder>();
        bool configurationInvoked = false;
        sut.Configure(_ => configurationInvoked = true);

        // Act
        ApplyConfigurator(sut, meterProviderBuilder);

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
        var meterProviderBuilder = Substitute.For<MeterProviderBuilder>();
        bool instrumentationInvoked = false;
        bool configurationInvoked = false;

        // Act
        sut
            .AddMeter("TestMeter1")
            .AddMeter("TestMeter2")
            .AddInstrumentation(_ => instrumentationInvoked = true)
            .Configure(_ => configurationInvoked = true);

        ApplyConfigurator(sut, meterProviderBuilder);

        // Assert
        meterProviderBuilder.Received(1).AddMeter("TestMeter1");
        meterProviderBuilder.Received(1).AddMeter("TestMeter2");
        instrumentationInvoked.ShouldBeTrue();
        configurationInvoked.ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private MetricsConfigurator CreateConfigurator()
    {
        // MetricsConfigurator의 생성자는 internal이므로 리플렉션 사용
        var constructor = typeof(MetricsConfigurator)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                [typeof(OpenTelemetryOptions)],
                null);

        return (MetricsConfigurator)constructor!.Invoke([_options]);
    }

    private static void ApplyConfigurator(MetricsConfigurator configurator, MeterProviderBuilder meterProviderBuilder)
    {
        // Apply 메서드는 internal이므로 리플렉션 사용
        var applyMethod = typeof(MetricsConfigurator)
            .GetMethod("Apply", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        applyMethod!.Invoke(configurator, [meterProviderBuilder]);
    }

    #endregion
}
