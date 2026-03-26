using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class RiskTierTests
{
    [Theory]
    [InlineData("Minimal")]
    [InlineData("Limited")]
    [InlineData("High")]
    [InlineData("Unacceptable")]
    public void Create_ShouldSucceed_WhenValidValue(string value)
    {
        // Act
        var actual = RiskTier.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidValue()
    {
        // Act
        var actual = RiskTier.Create("Invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("High", true)]
    [InlineData("Unacceptable", true)]
    [InlineData("Minimal", false)]
    [InlineData("Limited", false)]
    public void RequiresComplianceAssessment_ShouldReturnExpected(string value, bool expected)
    {
        // Arrange
        var sut = RiskTier.CreateFromValidated(value);

        // Act
        var actual = sut.RequiresComplianceAssessment;

        // Assert
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Unacceptable", true)]
    [InlineData("High", false)]
    [InlineData("Limited", false)]
    [InlineData("Minimal", false)]
    public void IsProhibited_ShouldReturnExpected(string value, bool expected)
    {
        // Arrange
        var sut = RiskTier.CreateFromValidated(value);

        // Act
        var actual = sut.IsProhibited;

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void CreateFromValidated_ShouldThrow_WhenInvalidValue()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() => RiskTier.CreateFromValidated("Invalid"));
    }
}
