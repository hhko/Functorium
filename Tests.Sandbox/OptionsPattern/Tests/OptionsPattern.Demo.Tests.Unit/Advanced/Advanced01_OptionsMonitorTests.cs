using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;
using Shouldly;
using Xunit;

namespace OptionsPattern.Demo.Tests.Unit.Advanced;

public class Advanced01_OptionsMonitorTests
{
    [Fact]
    public void Should_Return_CurrentValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Simple:Name", "TestApp" },
                { "Simple:Value", "42" },
                { "Simple:Enabled", "true" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SimpleOptions>()
            .BindConfiguration(SimpleOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();
        var currentValue = monitor.CurrentValue;

        // Assert
        monitor.ShouldNotBeNull();
        currentValue.ShouldNotBeNull();
        currentValue.Name.ShouldBe("TestApp");
        currentValue.Value.ShouldBe(42);
        currentValue.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void Should_Return_Same_Instance_For_Multiple_CurrentValue_Calls()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options => options.Name = "Test");

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();
        var value1 = monitor.CurrentValue;
        var value2 = monitor.CurrentValue;

        // Assert
        // CurrentValue는 항상 같은 인스턴스를 반환합니다 (설정이 변경되지 않는 한)
        ReferenceEquals(value1, value2).ShouldBeTrue();
    }

    [Fact]
    public void Should_Be_Singleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options => options.Name = "Test");

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var monitor1 = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();
        var monitor2 = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();

        // Assert
        // IOptionsMonitor<T>는 Singleton입니다
        ReferenceEquals(monitor1, monitor2).ShouldBeTrue();
    }
}
