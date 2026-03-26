using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class ModelVersionTests
{
    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.1.3")]
    [InlineData("0.0.1")]
    [InlineData("1.0.0-beta")]
    [InlineData("1.0.0-rc.1")]
    public void Create_ShouldSucceed_WhenValueIsValidSemVer(string value)
    {
        // Act
        var actual = ModelVersion.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = ModelVersion.Create("  1.0.0  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("1.0.0");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = ModelVersion.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    [InlineData("abc")]
    [InlineData("1.0.0.0")]
    public void Create_ShouldFail_WhenValueIsInvalidSemVer(string value)
    {
        // Act
        var actual = ModelVersion.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
