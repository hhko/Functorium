using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_CreatesMoney()
    {
        // Act
        var money = new Money(100m, "USD");

        // Assert
        Assert.Equal(100m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Money(-10m, "USD"));
        Assert.Equal("Amount cannot be negative", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidCurrency_ThrowsDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Money(100m, "US"));
        Assert.Equal("Currency must be a 3-letter ISO code", exception.Message);
    }

    [Fact]
    public void Create_NormalizesCurrencyToUppercase()
    {
        // Act
        var money = new Money(100m, "usd");

        // Assert
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");

        // Assert
        Assert.Equal(money1, money2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "EUR");

        // Assert
        Assert.NotEqual(money1, money2);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var money = new Money(1234.56m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        Assert.Contains("USD", result);
    }
}
