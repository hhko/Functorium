using ECommerce.Domain.AggregateRoots.Customers.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Customers;

public class CustomerNameTests
{
    [Theory]
    [InlineData("John")]
    [InlineData("a")]
    public void Create_ShouldSucceed_WhenValueIsValid(string value)
    {
        // Act
        var actual = CustomerName.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValue()
    {
        // Act
        var actual = CustomerName.Create("  John  ").ThrowIfFail();

        // Assert
        ((string)actual).ShouldBe("John");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Create_ShouldFail_WhenValueIsEmptyOrNull(string? value)
    {
        // Act
        var actual = CustomerName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueExceedsMaxLength()
    {
        // Arrange
        var value = new string('a', CustomerName.MaxLength + 1);

        // Act
        var actual = CustomerName.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
