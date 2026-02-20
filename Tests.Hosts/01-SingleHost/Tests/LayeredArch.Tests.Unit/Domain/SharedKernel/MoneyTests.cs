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
    public void Subtract_ShouldReturnSuccess_WhenResultIsPositive()
    {
        // Arrange
        var sut = Money.Create(100m).ThrowIfFail();
        var other = Money.Create(30m).ThrowIfFail();

        // Act
        var actual = sut.Subtract(other);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((decimal)actual.ThrowIfFail()).ShouldBe(70m);
    }

    [Fact]
    public void Subtract_ShouldReturnFail_WhenResultIsZero()
    {
        // Arrange
        var sut = Money.Create(100m).ThrowIfFail();
        var other = Money.Create(100m).ThrowIfFail();

        // Act
        var actual = sut.Subtract(other);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Subtract_ShouldReturnFail_WhenResultIsNegative()
    {
        // Arrange
        var sut = Money.Create(50m).ThrowIfFail();
        var other = Money.Create(100m).ThrowIfFail();

        // Act
        var actual = sut.Subtract(other);

        // Assert
        actual.IsFail.ShouldBeTrue();
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

    [Fact]
    public void Zero_ShouldHaveValueOfZero()
    {
        // Act
        var actual = Money.Zero;

        // Assert
        ((decimal)actual).ShouldBe(0m);
    }

    [Fact]
    public void Sum_ShouldReturnTotalOfAllValues()
    {
        // Arrange
        var values = new[]
        {
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail(),
            Money.Create(300m).ThrowIfFail()
        };

        // Act
        var actual = Money.Sum(values);

        // Assert
        ((decimal)actual).ShouldBe(600m);
    }

    [Fact]
    public void Sum_ShouldReturnZero_WhenEmpty()
    {
        // Act
        var actual = Money.Sum(Enumerable.Empty<Money>());

        // Assert
        ((decimal)actual).ShouldBe(0m);
    }
}
