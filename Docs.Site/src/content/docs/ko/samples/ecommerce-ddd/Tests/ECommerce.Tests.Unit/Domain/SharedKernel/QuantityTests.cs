using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.SharedModels;

public class QuantityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1000)]
    public void Create_ShouldSucceed_WhenValueIsNonNegative(int value)
    {
        // Act
        var actual = Quantity.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((int)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ShouldFail_WhenValueIsNegative(int value)
    {
        // Act
        var actual = Quantity.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Add_ShouldReturnSum()
    {
        // Arrange
        var sut = Quantity.Create(10).ThrowIfFail();

        // Act
        var actual = sut.Add(5);

        // Assert
        ((int)actual).ShouldBe(15);
    }

    [Fact]
    public void Subtract_ShouldReturnDifference()
    {
        // Arrange
        var sut = Quantity.Create(10).ThrowIfFail();

        // Act
        var actual = sut.Subtract(3);

        // Assert
        ((int)actual).ShouldBe(7);
    }

    [Fact]
    public void Subtract_ShouldReturnZero_WhenSubtractingMoreThanAvailable()
    {
        // Arrange
        var sut = Quantity.Create(5).ThrowIfFail();

        // Act
        var actual = sut.Subtract(10);

        // Assert
        ((int)actual).ShouldBe(0);
    }
}
