using Observability.Adapters.Infrastructure.Abstractions.Options;

namespace Observability.Tests.Integration.AdaptersInfrastructureTests;

/// <summary>
/// FtpOptions 서비스 등록 및 설정에 대한 통합 테스트
/// WebApplicationFactory를 사용하여 실제 호스트 환경에서 테스트합니다.
/// </summary>
[Trait(nameof(IntegrationTest), IntegrationTest.AdaptersInfrastructure)]
public class FtpOptionsIntegrationTests : IClassFixture<FtpOptionsIntegrationTests.FtpTestFixture>
{
    private readonly FtpTestFixture _fixture;

    public FtpOptionsIntegrationTests(FtpTestFixture fixture)
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
    public void FtpOptions_ShouldBeRegistered()
    {
        // Arrange & Act
        var options = _fixture.Services.GetService<IOptions<FtpOptions>>();

        // Assert
        options.ShouldNotBeNull();
    }

    [Fact]
    public void FtpOptions_ShouldBeValidatedAndBound()
    {
        // Arrange & Act
        var optionsMonitor = _fixture.Services.GetService<IOptionsMonitor<FtpOptions>>();

        // Assert
        optionsMonitor.ShouldNotBeNull();
        optionsMonitor.CurrentValue.ShouldNotBeNull();
    }

    [Fact]
    public void FtpOptions_Host_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.Host.ShouldBe("ftp.test.local");
    }

    [Fact]
    public void FtpOptions_Port_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.Port.ShouldBe(2121);
    }

    [Fact]
    public void FtpOptions_Username_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.Username.ShouldBe("testuser");
    }

    [Fact]
    public void FtpOptions_Password_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.Password.ShouldBe("testpass123");
    }

    [Fact]
    public void FtpOptions_UsePassive_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.UsePassive.ShouldBeTrue();
    }

    [Fact]
    public void FtpOptions_UseTls_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.UseTls.ShouldBeTrue();
    }

    [Fact]
    public void FtpOptions_ConnectionTimeout_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.ConnectionTimeout.ShouldBe(60);
    }

    [Fact]
    public void FtpOptions_RootDirectory_ShouldMatchConfiguration()
    {
        // Arrange & Act
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Assert - appsettings.json에서 설정한 값
        options.RootDirectory.ShouldBe("/test/uploads");
    }

    [Fact]
    public void FtpOptions_ShouldBeRegisteredAsStartupOptionsLogger()
    {
        // Arrange & Act
        var startupLoggers = _fixture.Services.GetServices<Functorium.Adapters.Observabilities.Loggers.IStartupOptionsLogger>();

        // Assert - FtpOptions가 IStartupOptionsLogger로 등록되어야 함
        var ftpOptionsLogger = startupLoggers.OfType<FtpOptions>().FirstOrDefault();
        ftpOptionsLogger.ShouldNotBeNull();
    }

    [Fact]
    public void FtpOptions_Validation_ShouldPassWithValidConfiguration()
    {
        // Arrange
        var options = _fixture.Services.GetRequiredService<IOptionsMonitor<FtpOptions>>().CurrentValue;

        // Act & Assert - 유효성 검증이 통과했으므로 예외가 발생하지 않음
        options.Host.ShouldNotBeNullOrWhiteSpace();
        options.Port.ShouldBeInRange(1, 65535);
        options.Username.ShouldNotBeNullOrWhiteSpace();
        options.Password.ShouldNotBeNullOrWhiteSpace();
        options.ConnectionTimeout.ShouldBeInRange(1, 300);
        options.RootDirectory.ShouldNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// FTP 옵션 테스트용 Fixture
    /// HostTestFixture를 상속하여 WebApplicationFactory 기능을 재사용합니다.
    /// EnvironmentName을 "FtpTest"로 설정하여 appsettings.FtpTest.json을 로드합니다.
    /// </summary>
    public class FtpTestFixture : HostTestFixture<Program>
    {
        protected override string EnvironmentName => "FtpTest";
    }
}
