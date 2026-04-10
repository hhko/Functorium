using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class ModelNameTests
{
    [Theory]
    [InlineData("GPT-4")]
    [InlineData("a")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = ModelName.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = ModelName.Create("  GPT-4  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("GPT-4");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = ModelName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', ModelName.MaxLength + 1);

        // Act
        var actual = ModelName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
