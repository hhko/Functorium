using FluentValidation;
using Functorium.Adapters.Options;
using Functorium.Applications.Observabilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.ApplicationsTests.Observabilities;

[Trait(nameof(UnitTest), UnitTest.Functorium_Applications)]
public class SloConfigurationTests
{
    #region Default Values Tests

    [Fact]
    public void DefaultValues_ShouldHaveCorrectGlobalDefaults()
    {
        // Arrange
        var config = new SloConfiguration();

        // Act & Assert
        config.GlobalDefaults.AvailabilityPercent.ShouldBe(99.9);
        config.GlobalDefaults.LatencyP95Milliseconds.ShouldBe(500);
        config.GlobalDefaults.LatencyP99Milliseconds.ShouldBe(1000);
        config.GlobalDefaults.ErrorBudgetWindowDays.ShouldBe(30);
    }

    [Fact]
    public void DefaultValues_ShouldHaveCorrectCommandDefaults()
    {
        // Arrange
        var config = new SloConfiguration();

        // Act & Assert
        config.CqrsDefaults.Command.AvailabilityPercent.ShouldBe(99.9);
        config.CqrsDefaults.Command.LatencyP95Milliseconds.ShouldBe(500);
        config.CqrsDefaults.Command.LatencyP99Milliseconds.ShouldBe(1000);
    }

    [Fact]
    public void DefaultValues_ShouldHaveCorrectQueryDefaults()
    {
        // Arrange
        var config = new SloConfiguration();

        // Act & Assert
        config.CqrsDefaults.Query.AvailabilityPercent.ShouldBe(99.5);
        config.CqrsDefaults.Query.LatencyP95Milliseconds.ShouldBe(200);
        config.CqrsDefaults.Query.LatencyP99Milliseconds.ShouldBe(500);
    }

    [Fact]
    public void DefaultHistogramBuckets_ShouldBeSortedAscending()
    {
        // Arrange & Act
        var buckets = SloConfiguration.DefaultHistogramBuckets;

        // Assert
        buckets.ShouldBe(buckets.OrderBy(b => b).ToArray());
    }

    [Fact]
    public void DefaultHistogramBuckets_ShouldContainSloThresholds()
    {
        // Arrange & Act
        var buckets = SloConfiguration.DefaultHistogramBuckets;

        // Assert - SLO 임계값 (500ms, 1s) 포함 확인
        buckets.ShouldContain(0.5);  // 500ms (Command P95)
        buckets.ShouldContain(1.0);  // 1s (Command P99)
    }

    #endregion

    #region GetTargetsForHandler Tests

    [Fact]
    public void GetTargetsForHandler_ShouldReturnHandlerOverride_WhenOverrideExists()
    {
        // Arrange
        var config = new SloConfiguration();
        config.HandlerOverrides["CreateOrderCommand"] = new SloTargets
        {
            AvailabilityPercent = 99.95,
            LatencyP95Milliseconds = 600
        };

        // Act
        var targets = config.GetTargetsForHandler("CreateOrderCommand", "command");

        // Assert
        targets.AvailabilityPercent.ShouldBe(99.95);
        targets.LatencyP95Milliseconds.ShouldBe(600);
    }

    [Fact]
    public void GetTargetsForHandler_ShouldMergeWithCqrsDefaults_WhenOverrideIsPartial()
    {
        // Arrange
        var config = new SloConfiguration();
        config.HandlerOverrides["CreateOrderCommand"] = new SloTargets
        {
            LatencyP95Milliseconds = 600
            // AvailabilityPercent is null - should inherit from CqrsDefaults
        };

        // Act
        var targets = config.GetTargetsForHandler("CreateOrderCommand", "command");

        // Assert
        targets.LatencyP95Milliseconds.ShouldBe(600);
        targets.AvailabilityPercent.ShouldBe(99.9); // Inherited from Command defaults
    }

    [Fact]
    public void GetTargetsForHandler_ShouldReturnCqrsDefaults_WhenNoOverrideExists()
    {
        // Arrange
        var config = new SloConfiguration();

        // Act
        var commandTargets = config.GetTargetsForHandler("SomeCommand", "command");
        var queryTargets = config.GetTargetsForHandler("SomeQuery", "query");

        // Assert
        commandTargets.LatencyP95Milliseconds.ShouldBe(500); // Command default
        queryTargets.LatencyP95Milliseconds.ShouldBe(200);   // Query default
    }

    [Fact]
    public void GetTargetsForHandler_ShouldReturnGlobalDefaults_WhenCqrsTypeIsUnknown()
    {
        // Arrange
        var config = new SloConfiguration();

        // Act
        var targets = config.GetTargetsForHandler("SomeHandler", "unknown");

        // Assert
        targets.AvailabilityPercent.ShouldBe(99.9); // GlobalDefaults
    }

    #endregion

    #region Error Budget Calculation Tests

    [Fact]
    public void GetErrorBudgetPercent_ShouldCalculateCorrectly()
    {
        // Arrange
        var targets = new SloTargets { AvailabilityPercent = 99.9 };

        // Act
        var errorBudget = targets.GetErrorBudgetPercent();

        // Assert - IEEE 754 부동소수점 연산 정밀도 허용
        errorBudget.ShouldBe(0.1, tolerance: 0.0001); // 100 - 99.9 ≈ 0.1%
    }

    [Fact]
    public void GetErrorBudgetPercent_ShouldUseDefaultWhenNull()
    {
        // Arrange
        var targets = new SloTargets { AvailabilityPercent = null };

        // Act
        var errorBudget = targets.GetErrorBudgetPercent();

        // Assert - IEEE 754 부동소수점 연산 정밀도 허용
        errorBudget.ShouldBe(0.1, tolerance: 0.0001); // 100 - 99.9 (default) ≈ 0.1%
    }

    #endregion

    #region Configuration Binding Tests

    [Fact]
    public void ConfigurationBinding_ShouldBindFromAppSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Observability:Slo:GlobalDefaults:AvailabilityPercent"] = "99.8",
                ["Observability:Slo:GlobalDefaults:LatencyP95Milliseconds"] = "300",
                ["Observability:Slo:CqrsDefaults:Command:LatencyP95Milliseconds"] = "400",
                ["Observability:Slo:HandlerOverrides:CreateOrderCommand:LatencyP95Milliseconds"] = "700"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.RegisterConfigureOptions<SloConfiguration, SloConfiguration.Validator>(SloConfiguration.SectionName);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var sloConfig = serviceProvider.GetRequiredService<IOptions<SloConfiguration>>().Value;

        sloConfig.GlobalDefaults.AvailabilityPercent.ShouldBe(99.8);
        sloConfig.GlobalDefaults.LatencyP95Milliseconds.ShouldBe(300);
        sloConfig.CqrsDefaults.Command.LatencyP95Milliseconds.ShouldBe(400);
        sloConfig.HandlerOverrides["CreateOrderCommand"].LatencyP95Milliseconds.ShouldBe(700);
    }

    [Fact]
    public void ConfigurationBinding_ShouldBindHistogramBuckets()
    {
        // Arrange - 직접 Configuration 바인딩 테스트
        var configValues = new KeyValuePair<string, string?>[]
        {
            new("Observability:Slo:HistogramBuckets:0", "0.01"),
            new("Observability:Slo:HistogramBuckets:1", "0.05"),
            new("Observability:Slo:HistogramBuckets:2", "0.1"),
            new("Observability:Slo:HistogramBuckets:3", "0.5"),
            new("Observability:Slo:HistogramBuckets:4", "1")
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        // Act - 빈 배열로 초기화 후 바인딩 (Bind는 기존 배열에 추가하므로)
        var sloConfig = new SloConfiguration { HistogramBuckets = [] };
        configuration.GetSection(SloConfiguration.SectionName).Bind(sloConfig);

        // Assert
        sloConfig.HistogramBuckets.ShouldBe([0.01, 0.05, 0.1, 0.5, 1]);
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validator_ShouldPass_WhenConfigurationIsValid()
    {
        // Arrange
        var config = new SloConfiguration();
        var validator = new SloConfiguration.Validator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validator_ShouldFail_WhenHistogramBucketsIsEmpty()
    {
        // Arrange
        var config = new SloConfiguration { HistogramBuckets = [] };
        var validator = new SloConfiguration.Validator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "HistogramBuckets");
    }

    [Fact]
    public void Validator_ShouldFail_WhenHistogramBucketsContainsNegativeValue()
    {
        // Arrange
        var config = new SloConfiguration { HistogramBuckets = [0.1, -0.5, 1.0] };
        var validator = new SloConfiguration.Validator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validator_ShouldFail_WhenHistogramBucketsIsNotSorted()
    {
        // Arrange
        var config = new SloConfiguration { HistogramBuckets = [0.1, 0.5, 0.3, 1.0] };
        var validator = new SloConfiguration.Validator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validator_ShouldFail_WhenP99IsLessThanP95()
    {
        // Arrange
        var config = new SloConfiguration
        {
            GlobalDefaults = new SloTargets
            {
                LatencyP95Milliseconds = 500,
                LatencyP99Milliseconds = 300 // P99 < P95 is invalid
            }
        };
        var validator = new SloConfiguration.Validator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validator_ShouldFail_WhenAvailabilityIsOutOfRange()
    {
        // Arrange
        var config = new SloConfiguration
        {
            GlobalDefaults = new SloTargets
            {
                AvailabilityPercent = 101.0 // Out of range
            }
        };
        var validator = new SloConfiguration.Validator();

        // Act
        var result = validator.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    #endregion
}
