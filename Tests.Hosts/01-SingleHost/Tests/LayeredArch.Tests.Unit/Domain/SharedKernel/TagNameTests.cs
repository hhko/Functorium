using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.SharedKernel;

public class TagNameTests
{
    [Theory]
    [InlineData("electronics")]
    [InlineData("a")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = TagName.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = TagName.Create("  electronics  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("electronics");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = TagName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', TagName.MaxLength + 1);

        // Act
        var actual = TagName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
