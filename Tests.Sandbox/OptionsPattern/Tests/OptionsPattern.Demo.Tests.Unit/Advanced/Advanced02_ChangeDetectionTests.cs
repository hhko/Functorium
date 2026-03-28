using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;
using Shouldly;
using Xunit;

namespace OptionsPattern.Demo.Tests.Unit.Advanced;

public class Advanced02_ChangeDetectionTests
{
    [Fact]
    public void Should_Register_OnChange_Callback()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Simple:Name", "Initial" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SimpleOptions>()
            .BindConfiguration(SimpleOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();

        bool callbackInvoked = false;
        string? changedName = null;

        // Act
        var changeToken = monitor.OnChange(options =>
        {
            callbackInvoked = true;
            changedName = options.Name;
        });

        // Assert
        changeToken.ShouldNotBeNull();
        // 초기에는 콜백이 호출되지 않아야 합니다
        callbackInvoked.ShouldBeFalse();
        
        // 정리
        changeToken.Dispose();
    }

    [Fact]
    public void Should_Dispose_ChangeToken()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options => options.Name = "Test");

        var serviceProvider = services.BuildServiceProvider();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();

        // Act
        var changeToken = monitor.OnChange(_ => { });
        
        // Assert
        changeToken.ShouldNotBeNull();
        
        // Dispose는 예외를 발생시키지 않아야 합니다
        Should.NotThrow(() => changeToken.Dispose());
    }

    [Fact]
    public void Should_Support_Multiple_Callbacks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options => options.Name = "Test");

        var serviceProvider = services.BuildServiceProvider();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();

        int callback1Count = 0;
        int callback2Count = 0;

        // Act
        var token1 = monitor.OnChange(_ => callback1Count++);
        var token2 = monitor.OnChange(_ => callback2Count++);

        // Assert
        token1.ShouldNotBeNull();
        token2.ShouldNotBeNull();
        
        // 정리
        token1.Dispose();
        token2.Dispose();
    }
}
