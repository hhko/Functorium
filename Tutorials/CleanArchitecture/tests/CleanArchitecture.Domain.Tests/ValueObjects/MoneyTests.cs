using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_CreatesMoney()
    {
        // Act
        var result = Money.Create(100m, "USD");

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: money =>
            {
                money.Amount.ShouldBe(100m);
                money.Currency.ShouldBe("USD");
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Create_WithNegativeAmount_ReturnsFail()
    {
        // Act
        var result = Money.Create(-10m, "USD");

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithInvalidCurrency_ReturnsFail()
    {
        // Act
        var result = Money.Create(100m, "US");

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_NormalizesCurrencyToUppercase()
    {
        // Act
        var result = Money.Create(100m, "usd");

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: money => money.Currency.ShouldBe("USD"),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(100m, "USD");

        // Assert
        money1.Match(
            Succ: m1 => money2.Match(
                Succ: m2 => m1.ShouldBe(m2),
                Fail: _ => Assert.Fail("Should succeed")),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(100m, "EUR");

        // Assert
        money1.Match(
            Succ: m1 => money2.Match(
                Succ: m2 => m1.ShouldNotBe(m2),
                Fail: _ => Assert.Fail("Should succeed")),
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange & Act
        var result = Money.Create(1234.56m, "USD");

        // Assert
        result.Match(
            Succ: money => money.ToString().ShouldContain("USD"),
            Fail: _ => Assert.Fail("Should succeed"));
    }
}
