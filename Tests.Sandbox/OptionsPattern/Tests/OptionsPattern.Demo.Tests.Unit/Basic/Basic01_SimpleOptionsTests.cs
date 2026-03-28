using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;
using Shouldly;
using Xunit;

namespace OptionsPattern.Demo.Tests.Unit.Basic;

public class Basic01_SimpleOptionsTests
{
    [Fact]
    public void Should_Register_And_Resolve_IOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options =>
            {
                options.Name = "TestApp";
                options.Value = 42;
                options.Enabled = true;
            });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();

        // Assert
        options.ShouldNotBeNull();
        options.Value.Name.ShouldBe("TestApp");
        options.Value.Value.ShouldBe(42);
        options.Value.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void Should_Return_Same_Instance_For_IOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options => options.Name = "Test");

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options1 = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();
        var options2 = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();

        // Assert
        // IOptions<T>는 Singleton이므로 같은 인스턴스를 반환해야 합니다
        ReferenceEquals(options1, options2).ShouldBeTrue();
    }

    [Fact]
    public void Should_Support_Multiple_Configure_Calls()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options => options.Name = "First")
            .Configure(options => options.Name = "Second")
            .Configure(options => options.Value = 100);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();

        // Assert
        options.Value.Name.ShouldBe("Second"); // 마지막 Configure가 적용됨
        options.Value.Value.ShouldBe(100);
    }
}
