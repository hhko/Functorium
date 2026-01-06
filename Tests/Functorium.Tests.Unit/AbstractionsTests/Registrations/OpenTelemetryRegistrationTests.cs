using System.Diagnostics;
using System.Reflection;
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AbstractionsTests.Registrations;

[Trait(nameof(UnitTest), UnitTest.Functorium_Abstractions)]
public class OpenTelemetryRegistrationTests
{
    // 테스트용 어셈블리 (현재 테스트 어셈블리 사용)
    private static readonly Assembly TestAssembly = typeof(OpenTelemetryRegistrationTests).Assembly;

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

    #region RegisterOpenTelemetry Basic Tests

    [Fact]
    public void RegisterOpenTelemetry_ReturnsOpenTelemetryBuilder_WhenConfigurationIsValid()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        var actual = services.RegisterOpenTelemetry(configuration, TestAssembly);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldBeOfType<OpenTelemetryBuilder>();
    }

    [Fact]
    public void RegisterOpenTelemetry_RegistersOpenTelemetryOptions_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("MyService"));

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetService<IOptions<OpenTelemetryOptions>>();

        // Assert
        actual.ShouldNotBeNull();
        actual.Value.ServiceName.ShouldBe("MyService");
    }

    [Fact]
    public void RegisterOpenTelemetry_RegistersActivitySource_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("TestService"));

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetService<ActivitySource>();

        // Assert
        actual.ShouldNotBeNull();
        actual.Name.ShouldBe("TestService");
    }

    #endregion

    #region OpenTelemetryOptions Configuration Tests

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresServiceName_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings(serviceName: "CustomServiceName");
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.ServiceName.ShouldBe("CustomServiceName");
    }

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresCollectorEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings(collectorEndpoint: "http://custom-collector:4317");
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.CollectorEndpoint.ShouldBe("http://custom-collector:4317");
    }

    [Theory]
    [InlineData("Grpc")]
    [InlineData("HttpProtobuf")]
    public void RegisterOpenTelemetry_ConfiguresCollectorProtocol_WhenSetInConfiguration(string protocol)
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:CollectorProtocol"] = protocol;
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.CollectorProtocol.ShouldBe(protocol);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void RegisterOpenTelemetry_ConfiguresSamplingRate_WhenSetInConfiguration(double samplingRate)
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:SamplingRate"] = samplingRate.ToString();
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.SamplingRate.ShouldBe(samplingRate);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RegisterOpenTelemetry_ConfiguresEnablePrometheusExporter_WhenSetInConfiguration(bool enablePrometheus)
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:EnablePrometheusExporter"] = enablePrometheus.ToString();
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.EnablePrometheusExporter.ShouldBe(enablePrometheus);
    }

    #endregion

    #region Individual Endpoint Configuration Tests

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresTracingEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:TracingEndpoint"] = "http://tracing-collector:21890";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.TracingEndpoint.ShouldBe("http://tracing-collector:21890");
    }

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresMetricsEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:MetricsEndpoint"] = "http://metrics-collector:21891";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.MetricsEndpoint.ShouldBe("http://metrics-collector:21891");
    }

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresLoggingEndpoint_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:LoggingEndpoint"] = "http://logging-collector:21892";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.LoggingEndpoint.ShouldBe("http://logging-collector:21892");
    }

    #endregion

    #region Individual Protocol Configuration Tests

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresTracingProtocol_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:TracingProtocol"] = "HttpProtobuf";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.TracingProtocol.ShouldBe("HttpProtobuf");
    }

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresMetricsProtocol_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:MetricsProtocol"] = "HttpProtobuf";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.MetricsProtocol.ShouldBe("HttpProtobuf");
    }

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresLoggingProtocol_WhenSetInConfiguration()
    {
        // Arrange
        var settings = CreateValidOpenTelemetrySettings();
        settings["OpenTelemetry:LoggingProtocol"] = "HttpProtobuf";
        var (services, configuration) = CreateServicesWithConfiguration(settings);

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        actual.LoggingProtocol.ShouldBe("HttpProtobuf");
    }

    #endregion

    #region OpenTelemetryBuilder Options Access Tests

    [Fact]
    public void RegisterOpenTelemetry_ReturnsBuilderWithCorrectOptions_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("BuilderTestService"));

        // Act
        var builder = services.RegisterOpenTelemetry(configuration, TestAssembly);

        // Assert - IOptions<OpenTelemetryOptions>가 올바르게 등록되었는지 확인
        using var serviceProvider = services.BuildServiceProvider();
        var options = builder.GetOptions(serviceProvider);
        options.ShouldNotBeNull();
        options.ServiceName.ShouldBe("BuilderTestService");
    }

    #endregion

    #region Service Registration Verification Tests

    [Fact]
    public void RegisterOpenTelemetry_RegistersSerilogLogging_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);

        // Assert
        services.ShouldContain(sd => sd.ServiceType.Name.Contains("ILoggerFactory") ||
                                      sd.ServiceType.Name.Contains("ILoggerProvider"));
    }

    [Fact]
    public void RegisterOpenTelemetry_RegistersActivitySourceAsSingleton_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);

        // Assert
        var activitySourceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ActivitySource));
        activitySourceDescriptor.ShouldNotBeNull();
        activitySourceDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterOpenTelemetry_RegistersOpenTelemetryOptionsViaConfigure_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);

        // Assert - IOptions<T>는 Configure를 통해 등록되므로 IConfigureOptions<T>를 확인
        var configureOptionsDescriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IConfigureOptions<OpenTelemetryOptions>));
        configureOptionsDescriptor.ShouldNotBeNull();
    }

    #endregion

    #region ActivitySource Configuration Tests

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresActivitySourceWithServiceName_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings("ActivitySourceService"));

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<ActivitySource>();

        // Assert
        actual.Name.ShouldBe("ActivitySourceService");
    }

    [Fact]
    public void RegisterOpenTelemetry_ConfiguresActivitySourceWithServiceVersion_WhenCalled()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<ActivitySource>();

        // Assert
        actual.Version.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Multiple Calls Tests

    [Fact]
    public void RegisterOpenTelemetry_ReturnsSameOptionsInstance_WhenCalledMultipleTimes()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
        var second = provider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;

        // Assert
        first.ShouldBeSameAs(second);
    }

    [Fact]
    public void RegisterOpenTelemetry_ReturnsSameActivitySourceInstance_WhenCalledMultipleTimes()
    {
        // Arrange
        var (services, configuration) = CreateServicesWithConfiguration(CreateValidOpenTelemetrySettings());

        // Act
        services.RegisterOpenTelemetry(configuration, TestAssembly);
        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<ActivitySource>();
        var second = provider.GetRequiredService<ActivitySource>();

        // Assert
        first.ShouldBeSameAs(second);
    }

    #endregion
}
