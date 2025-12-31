using FluentValidation;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Options;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class OptionsConfiguratorTests
{
    #region GetOptions Tests

    [Fact]
    public void GetOptions_ReturnsOptions_WhenOptionsAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<TestOptions>(options =>
        {
            options.Name = "Test";
            options.Value = 42;
        });

        // Act
        var actual = services.GetOptions<TestOptions>();

        // Assert
        actual.ShouldNotBeNull();
        actual.Name.ShouldBe("Test");
        actual.Value.ShouldBe(42);
    }

    [Fact]
    public void GetOptions_ThrowsException_WhenOptionsNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => services.GetOptions<TestOptions>());
    }

    #endregion

    #region RegisterConfigureOptions Tests

    [Fact]
    public void RegisterConfigureOptions_RegistersOptions_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSection:Name"] = "ConfigTest",
                ["TestSection:Value"] = "99"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.RegisterConfigureOptions<TestOptions, TestOptions.TestOptionsValidator>("TestSection");

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var actual = serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value;
        actual.Name.ShouldBe("ConfigTest");
        actual.Value.ShouldBe(99);
    }

    [Fact]
    public void RegisterConfigureOptions_RegistersValidator_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSection:Name"] = "Test",
                ["TestSection:Value"] = "10"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.RegisterConfigureOptions<TestOptions, TestOptions.TestOptionsValidator>("TestSection");

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var validator = serviceProvider.GetService<IValidator<TestOptions>>();
        validator.ShouldNotBeNull();
        validator.ShouldBeOfType<TestOptions.TestOptionsValidator>();
    }

    [Fact]
    public void RegisterConfigureOptions_RegistersIStartupOptionsLoggable_WhenOptionsImplementsInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSection:Name"] = "LoggableTest",
                ["TestSection:Value"] = "50"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.RegisterConfigureOptions<TestOptionsWithLogging, TestOptionsWithLogging.TestOptionsWithLoggingValidator>("TestSection");

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var loggables = serviceProvider.GetServices<IStartupOptionsLogger>();
        loggables.ShouldNotBeEmpty();
        loggables.ShouldContain(l => l.GetType() == typeof(TestOptionsWithLogging));
    }

    [Fact]
    public void RegisterConfigureOptions_DoesNotRegisterIStartupOptionsLoggable_WhenOptionsDoesNotImplementInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSection:Name"] = "NonLoggable",
                ["TestSection:Value"] = "30"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.RegisterConfigureOptions<TestOptions, TestOptions.TestOptionsValidator>("TestSection");

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var loggables = serviceProvider.GetServices<IStartupOptionsLogger>();
        loggables.ShouldNotContain(l => l.GetType() == typeof(TestOptions));
    }

    [Fact]
    public void RegisterConfigureOptions_ValidatesOptions_WhenValidationPasses()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSection:Name"] = "ValidTest",
                ["TestSection:Value"] = "15"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.RegisterConfigureOptions<TestOptions, TestOptions.TestOptionsValidator>("TestSection");
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - 유효성 검사를 통과하면 예외가 발생하지 않아야 함
        Should.NotThrow(() =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value;
            options.ShouldNotBeNull();
        });
    }

    [Fact]
    public void RegisterConfigureOptions_ThrowsOptionsValidationException_WhenValidationFails()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestSection:Name"] = "",  // Empty name - validation failure
                ["TestSection:Value"] = "150"  // Out of range - validation failure
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.RegisterConfigureOptions<TestOptions, TestOptions.TestOptionsValidator>("TestSection");
        using var serviceProvider = services.BuildServiceProvider();

        // Assert - ValidateOnStart가 호출되지 않으므로 IOptions.Value 접근 시 검증
        Should.Throw<OptionsValidationException>(() =>
        {
            _ = serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value;
        });
    }

    #endregion

    #region Test Helper Classes

    // 테스트용 옵션 클래스
    public class TestOptions
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        // 테스트용 Validator
        public class TestOptionsValidator : AbstractValidator<TestOptions>
        {
            public TestOptionsValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Name은 필수입니다.");

                RuleFor(x => x.Value)
                    .InclusiveBetween(1, 100)
                    .WithMessage("Value는 1 이상 100 이하여야 합니다.");
            }
        }
    }

    // IStartupOptionsLoggable을 구현하는 테스트용 옵션
    public class TestOptionsWithLogging : IStartupOptionsLogger
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }

        public void LogConfiguration(ILogger logger)
        {
            logger.LogInformation("TestOptionsWithLogging: Name={Name}, Value={Value}", Name, Value);
        }

        // IStartupOptionsLoggable을 구현하는 테스트용 Validator
        public class TestOptionsWithLoggingValidator : AbstractValidator<TestOptionsWithLogging>
        {
            public TestOptionsWithLoggingValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Name은 필수입니다.");

                RuleFor(x => x.Value)
                    .InclusiveBetween(1, 100)
                    .WithMessage("Value는 1 이상 100 이하여야 합니다.");
            }
        }
    }

    #endregion
}
