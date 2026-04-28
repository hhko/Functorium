using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Tests.Unit.Domain.Products;

public class ProductDescriptionTests
{
    [Theory]
    [InlineData("A great product")]
    [InlineData("")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = ProductDescription.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = ProductDescription.Create("  description  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("description");
    }

    [Fact]
    public void Create_ShouldFail_WhenValueIsNull()
    {
        // Act
        var actual = ProductDescription.Create(null);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', ProductDescription.MaxLength + 1);

        // Act
        var actual = ProductDescription.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
