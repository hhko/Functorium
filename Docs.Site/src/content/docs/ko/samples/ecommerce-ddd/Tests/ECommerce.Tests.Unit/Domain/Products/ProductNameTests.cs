using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Tests.Unit.Domain.Products;

public class ProductNameTests
{
    [Theory]
    [InlineData("Laptop")]
    [InlineData("a")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = ProductName.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = ProductName.Create("  Laptop  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("Laptop");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = ProductName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', ProductName.MaxLength + 1);

        // Act
        var actual = ProductName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
