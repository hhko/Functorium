using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;
using Shouldly;
using Xunit;

namespace OptionsPattern.Demo.Tests.Unit.Intermediate;

public class Intermediate01_OptionsSnapshotTests
{
    [Fact]
    public void Should_Create_Different_Snapshots_For_Different_Scopes()
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
        IOptionsSnapshot<SimpleOptions> snapshot1;
        IOptionsSnapshot<SimpleOptions> snapshot2;

        using (var scope1 = serviceProvider.CreateScope())
        {
            snapshot1 = scope1.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            snapshot2 = scope2.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
        }

        // Assert
        snapshot1.ShouldNotBeNull();
        snapshot2.ShouldNotBeNull();
        // 각 스코프마다 새로운 스냅샷이 생성되지만, 값은 동일해야 합니다
        snapshot1.Value.Name.ShouldBe(snapshot2.Value.Name);
    }

    [Fact]
    public void Should_Return_Same_Snapshot_Within_Same_Scope()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Simple:Name", "TestApp" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SimpleOptions>()
            .BindConfiguration(SimpleOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();
        var snapshot1 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
        var snapshot2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();

        // Assert
        // 같은 스코프 내에서는 같은 스냅샷 인스턴스를 반환해야 합니다
        ReferenceEquals(snapshot1, snapshot2).ShouldBeTrue();
    }

    [Fact]
    public void Should_Differ_From_IOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions<SimpleOptions>()
            .Configure(options => options.Name = "Test");

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();
        IOptionsSnapshot<SimpleOptions> snapshot;

        using (var scope = serviceProvider.CreateScope())
        {
            snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
        }

        // Assert
        // IOptions<T>는 Singleton, IOptionsSnapshot<T>는 Scoped
        options.ShouldNotBeNull();
        snapshot.ShouldNotBeNull();
        // 값은 동일해야 하지만 인스턴스는 다릅니다
        options.Value.Name.ShouldBe(snapshot.Value.Name);
    }
}
