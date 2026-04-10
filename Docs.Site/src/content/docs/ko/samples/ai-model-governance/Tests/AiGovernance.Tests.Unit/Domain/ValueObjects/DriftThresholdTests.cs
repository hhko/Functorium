using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class DriftThresholdTests
{
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void Create_ShouldSucceed_WhenValueIsInRange(decimal value)
    {
        // Act
        var actual = DriftThreshold.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((decimal)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    public void Create_ShouldFail_WhenValueIsOutOfRange(decimal value)
    {
        // Act
        var actual = DriftThreshold.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
