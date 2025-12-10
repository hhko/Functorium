namespace Observability.Tests.Integration.AdaptersInfrastructureTests;

/// <summary>
/// OpenTelemetry 서비스 등록 및 설정에 대한 통합 테스트
/// WebApplicationFactory를 사용하여 실제 호스트 환경에서 테스트합니다.
/// </summary>
[Trait(nameof(IntegrationTest), IntegrationTest.AdaptersInfrastructure)]
public class OpenTelemetryIntegrationTests : IClassFixture<OpenTelemetryIntegrationTests.OpenTelemetryTestFixture>
{
    private readonly OpenTelemetryTestFixture _fixture;

    public OpenTelemetryIntegrationTests(OpenTelemetryTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Host_ShouldStartSuccessfully()
    {
        // Arrange & Act - Fixture가 이미 호스트를 시작함
        // Assert
        _fixture.Services.ShouldNotBeNull();
    }

    [Fact]
    public void IOpenTelemetryOptions_ShouldBeRegistered()
    {
        // Arrange & Act
        var options = _fixture.Services.GetService<IOpenTelemetryOptions>();

        // Assert
        options.ShouldNotBeNull();
    }

    [Fact]
    public void IOpenTelemetryOptions_EnablePrometheusExporter_ShouldMatchConfiguration()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOpenTelemetryOptions>();

        // Assert - appsettings.json에서 EnablePrometheusExporter: false로 설정됨
        options.EnablePrometheusExporter.ShouldBeFalse();
    }

    [Fact]
    public void OpenTelemetryOptions_ShouldBeValidatedAndBound()
    {
        // Arrange & Act
        var optionsMonitor = _fixture.Services.GetService<IOptionsMonitor<OpenTelemetryOptions>>();

        // Assert
        optionsMonitor.ShouldNotBeNull();
        optionsMonitor.CurrentValue.ShouldNotBeNull();
        optionsMonitor.CurrentValue.ServiceName.ShouldBe("Observability.Tests.Integration");
    }

    [Fact]
    public void OpenTelemetryOptions_ServiceVersion_ShouldBeAssemblyVersion()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Assert
        options.ServiceVersion.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void OpenTelemetryOptions_CollectorEndpoint_ShouldBeConfigured()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Assert
        options.CollectorEndpoint.ShouldBe("http://127.0.0.1:18889");
    }

    [Fact]
    public void OpenTelemetryOptions_CollectorProtocol_ShouldBeGrpc()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Assert
        options.CollectorProtocol.ShouldBe("Grpc");
    }

    [Fact]
    public void OpenTelemetryOptions_SamplingRate_ShouldBeOverriddenByEnvironment()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Assert - appsettings.OpenTelemetryTest.json에서 0.5로 오버라이드됨
        options.SamplingRate.ShouldBe(0.5);
    }

    [Fact]
    public void ActivitySource_ShouldBeRegistered()
    {
        // Arrange & Act
        var activitySource = _fixture.Services.GetService<ActivitySource>();

        // Assert
        activitySource.ShouldNotBeNull();
    }

    [Fact]
    public void ActivitySource_ShouldHaveCorrectServiceName()
    {
        // Arrange & Act
        var activitySource = _fixture.Services.GetRequiredService<ActivitySource>();

        // Assert
        activitySource.Name.ShouldBe("Observability.Tests.Integration");
    }

    [Fact]
    public void OpenTelemetryOptions_GetTracingEndpoint_ShouldReturnEmptyWhenDisabled()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Act
        var tracingEndpoint = options.GetTracingEndpoint();

        // Assert - TracingCollectorEndpoint가 빈 문자열로 설정되어 비활성화됨
        tracingEndpoint.ShouldBeEmpty();
    }

    [Fact]
    public void OpenTelemetryOptions_GetMetricsEndpoint_ShouldReturnEmptyWhenDisabled()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Act
        var metricsEndpoint = options.GetMetricsEndpoint();

        // Assert - MetricsCollectorEndpoint가 빈 문자열로 설정되어 비활성화됨
        metricsEndpoint.ShouldBeEmpty();
    }

    [Fact]
    public void OpenTelemetryOptions_GetLoggingEndpoint_ShouldReturnEmptyWhenDisabled()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Act
        var loggingEndpoint = options.GetLoggingEndpoint();

        // Assert - LoggingCollectorEndpoint가 빈 문자열로 설정되어 비활성화됨
        loggingEndpoint.ShouldBeEmpty();
    }

    [Fact]
    public void OpenTelemetryOptions_GetTracingProtocol_ShouldReturnGrpc()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Act
        var protocol = options.GetTracingProtocol();

        // Assert
        protocol.Name.ShouldBe("Grpc");
    }

    [Fact]
    public void OpenTelemetryOptions_GetMetricsProtocol_ShouldReturnGrpc()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Act
        var protocol = options.GetMetricsProtocol();

        // Assert
        protocol.Name.ShouldBe("Grpc");
    }

    [Fact]
    public void OpenTelemetryOptions_GetLogsProtocol_ShouldReturnGrpc()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<OpenTelemetryOptions>>().CurrentValue;

        // Act
        var protocol = options.GetLogsProtocol();

        // Assert
        protocol.Name.ShouldBe("Grpc");
    }

    /// <summary>
    /// OpenTelemetry 옵션 테스트용 Fixture
    /// HostTestFixture를 상속하여 WebApplicationFactory 기능을 재사용합니다.
    /// EnvironmentName을 "OpenTelemetryTest"로 설정하여 appsettings.OpenTelemetryTest.json을 로드합니다.
    /// </summary>
    public class OpenTelemetryTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "OpenTelemetryTest";
    }
}
