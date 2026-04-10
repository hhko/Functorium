using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Tests.Unit.Domain.ValueObjects;

public class ModelPurposeTests
{
    [Theory]
    [InlineData("Image classification")]
    [InlineData("Natural language processing")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = ModelPurpose.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = ModelPurpose.Create("  Image classification  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("Image classification");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = ModelPurpose.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', ModelPurpose.MaxLength + 1);

        // Act
        var actual = ModelPurpose.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
