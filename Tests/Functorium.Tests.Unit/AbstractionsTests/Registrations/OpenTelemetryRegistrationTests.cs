using System.Diagnostics;
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AbstractionsTests.Registrations;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class OpenTelemetryRegistrationTests
{
    private static IConfiguration CreateConfiguration(Dictionary<string, string?> settings)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    private static (IServiceCollection services, IConfiguration configuration) CreateServicesWithConfiguration(
        Dictionary<string, string?> settings)
    {
        var configuration = CreateConfiguration(settings);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        return (services, configuration);
    }

    private static Dictionary<string, string?> CreateValidOpenTelemetrySettings(
        string serviceName = "TestService",
        string collectorEndpoint = "http://localhost:4317")
    {
        return new Dictionary<string, string?>
        {
            ["OpenTelemetry:ServiceName"] = serviceName,
            ["OpenTelemetry:CollectorEndpoint"] = collectorEndpoint,
            ["OpenTelemetry:CollectorProtocol"] = "Grpc",
            ["OpenTelemetry:SamplingRate"] = "1.0"
        };
    }

    #region RegisterObservability Basic Tests

    [Fact]
    public void RegisterObservability_ReturnsOpenTelemetryBuilder_WhenConfigurationIsValid()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        var actual = services.RegisterObservability(configuration);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldBeOfType<OpenTelemetryBuilder>();
    }

    [Fact]
    public void RegisterObservability_RegistersIOpenTelemetryOptions_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("MyService"));

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetService<IOpenTelemetryOptions>();

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldBeAssignableTo<OpenTelemetryOptions>();
        ((OpenTelemetryOptions)actual).ServiceName.ShouldBe("MyService");
    }

    [Fact]
    public void RegisterObservability_RegistersActivitySource_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("TestService"));

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetService<ActivitySource>();

        // Assert
        actual.ShouldNotBeNull();
        actual.Name.ShouldBe("TestService");
    }

    #endregion

    #region OpenTelemetryOptions Configuration Tests

    [Fact]
    public void RegisterObservability_ConfiguresServiceName_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings(serviceName: "CustomServiceName");
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.ServiceName.ShouldBe("CustomServiceName");
    }

    [Fact]
    public void RegisterObservability_ConfiguresCollectorEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings(collectorEndpoint: "http://custom-collector:4317");
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.CollectorEndpoint.ShouldBe("http://custom-collector:4317");
    }

    [Theory]
    [InlineData("Grpc")]
    [InlineData("HttpProtobuf")]
    public void RegisterObservability_ConfiguresCollectorProtocol_WhenSetInConfiguration(string protocol)
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:CollectorProtocol"] = protocol;
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.CollectorProtocol.ShouldBe(protocol);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void RegisterObservability_ConfiguresSamplingRate_WhenSetInConfiguration(double samplingRate)
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:SamplingRate"] = samplingRate.ToString();
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.SamplingRate.ShouldBe(samplingRate);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RegisterObservability_ConfiguresEnablePrometheusExporter_WhenSetInConfiguration(bool enablePrometheus)
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:EnablePrometheusExporter"] = enablePrometheus.ToString();
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.EnablePrometheusExporter.ShouldBe(enablePrometheus);
    }

    #endregion

    #region Individual Endpoint Configuration Tests

    [Fact]
    public void RegisterObservability_ConfiguresTracingCollectorEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:TracingCollectorEndpoint"] = "http://tracing-collector:21890";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.TracingCollectorEndpoint.ShouldBe("http://tracing-collector:21890");
    }

    [Fact]
    public void RegisterObservability_ConfiguresMetricsCollectorEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:MetricsCollectorEndpoint"] = "http://metrics-collector:21891";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.MetricsCollectorEndpoint.ShouldBe("http://metrics-collector:21891");
    }

    [Fact]
    public void RegisterObservability_ConfiguresLoggingCollectorEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:LoggingCollectorEndpoint"] = "http://logging-collector:21892";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.LoggingCollectorEndpoint.ShouldBe("http://logging-collector:21892");
    }

    #endregion

    #region Individual Protocol Configuration Tests

    [Fact]
    public void RegisterObservability_ConfiguresTracingCollectorProtocol_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:TracingCollectorProtocol"] = "HttpProtobuf";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.TracingCollectorProtocol.ShouldBe("HttpProtobuf");
    }

    [Fact]
    public void RegisterObservability_ConfiguresMetricsCollectorProtocol_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:MetricsCollectorProtocol"] = "HttpProtobuf";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.MetricsCollectorProtocol.ShouldBe("HttpProtobuf");
    }

    [Fact]
    public void RegisterObservability_ConfiguresLoggingCollectorProtocol_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:LoggingCollectorProtocol"] = "HttpProtobuf";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = (OpenTelemetryOptions)provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        actual.LoggingCollectorProtocol.ShouldBe("HttpProtobuf");
    }

    #endregion

    #region OpenTelemetryBuilder Options Access Tests

    [Fact]
    public void RegisterObservability_ReturnsBuilderWithCorrectOptions_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("BuilderTestService"));

        // Act
        var actual = services.RegisterObservability(configuration);

        // Assert
        actual.Options.ShouldNotBeNull();
        actual.Options.ServiceName.ShouldBe("BuilderTestService");
    }

    #endregion

    #region Service Registration Verification Tests

    [Fact]
    public void RegisterObservability_RegistersSerilogLogging_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterObservability(configuration);

        // Assert
        services.ShouldContain(sd => sd.ServiceType.Name.Contains("ILoggerFactory") ||
                                      sd.ServiceType.Name.Contains("ILoggerProvider"));
    }

    [Fact]
    public void RegisterObservability_RegistersActivitySourceAsSingleton_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterObservability(configuration);

        // Assert
        var activitySourceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ActivitySource));
        activitySourceDescriptor.ShouldNotBeNull();
        activitySourceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterObservability_RegistersIOpenTelemetryOptionsAsSingleton_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterObservability(configuration);

        // Assert
        var optionsDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IOpenTelemetryOptions));
        optionsDescriptor.ShouldNotBeNull();
        optionsDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    #endregion

    #region ActivitySource Configuration Tests

    [Fact]
    public void RegisterObservability_ConfiguresActivitySourceWithServiceName_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("ActivitySourceService"));

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<ActivitySource>();

        // Assert
        actual.Name.ShouldBe("ActivitySourceService");
    }

    [Fact]
    public void RegisterObservability_ConfiguresActivitySourceWithServiceVersion_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<ActivitySource>();

        // Assert
        actual.Version.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Multiple Calls Tests

    [Fact]
    public void RegisterObservability_ReturnsSameOptionsInstance_WhenCalledMultipleTimes()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IOpenTelemetryOptions>();
        var second = provider.GetRequiredService<IOpenTelemetryOptions>();

        // Assert
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void RegisterObservability_ReturnsSameActivitySourceInstance_WhenCalledMultipleTimes()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterObservability(configuration);
        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<ActivitySource>();
        var second = provider.GetRequiredService<ActivitySource>();

        // Assert
        first.ShouldBeSameAs(second);
    }

    #endregion
}
