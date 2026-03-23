using MyShop.Domain.AggregateRoots.Products.ValueObjects;
using Shouldly;

namespace MyShop.Tests.Unit.Products;

public class MoneyTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenValueIsPositive()
    {
        // Act
        var result = Money.Create(1500m);

        // Assert
        result.IsSucc.ShouldBeTrue();
        ((decimal)result.ThrowIfFail()).ShouldBe(1500m);
    }

    [Fact]
    public void Create_ShouldFail_WhenValueIsNegative()
    {
        // Act
        var result = Money.Create(-1m);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenValueIsZero()
    {
        // Act
        var result = Money.Create(0m);

        // Assert
        result.IsFail.ShouldBeTrue();
    }
}
