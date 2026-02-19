using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.SharedModels;

public class MoneyTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(99999.99)]
    public void Create_ShouldSucceed_WhenValueIsPositive(decimal value)
    {
        // Act
        var actual = Money.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((decimal)actual.ThrowIfFail()).ShouldBe(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Create_ShouldFail_WhenValueIsZeroOrNegative(decimal value)
    {
        // Act
        var actual = Money.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Add_ShouldReturnSum()
    {
        // Arrange
        var sut = Money.Create(100m).ThrowIfFail();
        var other = Money.Create(50m).ThrowIfFail();

        // Act
        var actual = sut.Add(other);

        // Assert
        ((decimal)actual).ShouldBe(150m);
    }

    [Fact]
    public void Subtract_ShouldReturnDifference()
    {
        // Arrange
        var sut = Money.Create(100m).ThrowIfFail();
        var other = Money.Create(30m).ThrowIfFail();

        // Act
        var actual = sut.Subtract(other);

        // Assert
        ((decimal)actual).ShouldBe(70m);
    }

    [Fact]
    public void Multiply_ShouldReturnProduct()
    {
        // Arrange
        var sut = Money.Create(100m).ThrowIfFail();

        // Act
        var actual = sut.Multiply(3);

        // Assert
        ((decimal)actual).ShouldBe(300m);
    }
}
