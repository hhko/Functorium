using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;
using Shouldly;
using Xunit;

namespace OptionsPattern.Demo.Tests.Unit.Production;

public class Production01_ConfigReloadTests
{
    [Fact]
    public void Should_Support_ReloadOnChange_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiClient:BaseUrl", "https://api.example.com" },
                { "ApiClient:TimeoutSeconds", "30" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<ApiClientOptions>()
            .BindConfiguration(ApiClientOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<ApiClientOptions>>();
        var currentValue = monitor.CurrentValue;

        // Assert
        monitor.ShouldNotBeNull();
        currentValue.ShouldNotBeNull();
        currentValue.BaseUrl.ShouldBe("https://api.example.com");
        currentValue.TimeoutSeconds.ShouldBe(30);
    }

    [Fact]
    public void Should_Have_ReloadToken()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiClient:BaseUrl", "https://api.example.com" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<ApiClientOptions>()
            .BindConfiguration(ApiClientOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var reloadToken = configuration.GetReloadToken();

        // Assert
        reloadToken.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Register_Multiple_Monitors()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiClient:BaseUrl", "https://api.example.com" },
                { "Database:ConnectionString", "Server=localhost" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<ApiClientOptions>()
            .BindConfiguration(ApiClientOptions.SectionName);
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var apiMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ApiClientOptions>>();
        var dbMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<DatabaseOptions>>();

        // Assert
        apiMonitor.ShouldNotBeNull();
        dbMonitor.ShouldNotBeNull();
        apiMonitor.CurrentValue.BaseUrl.ShouldBe("https://api.example.com");
        dbMonitor.CurrentValue.ConnectionString.ShouldBe("Server=localhost");
    }
}
