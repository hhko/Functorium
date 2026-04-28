using ECommerce.Domain.AggregateRoots.Orders.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Orders;

public class ShippingAddressTests
{
    [Theory]
    [InlineData("123 Main St")]
    [InlineData("Seoul, Korea")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = ShippingAddress.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = ShippingAddress.Create("  123 Main St  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("123 Main St");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = ShippingAddress.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', ShippingAddress.MaxLength + 1);

        // Act
        var actual = ShippingAddress.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
