using MyShop.Domain.AggregateRoots.Products.ValueObjects;
using Shouldly;

namespace MyShop.Tests.Unit.Products;

public class ProductNameTests
{
    [Fact]
    public void Create_ShouldSucceed_WhenNameIsValid()
    {
        // Act
        var result = ProductName.Create("노트북");

        // Assert
        result.IsSucc.ShouldBeTrue();
        ((string)result.ThrowIfFail()).ShouldBe("노트북");
    }

    [Fact]
    public void Create_ShouldFail_WhenNameIsEmpty()
    {
        // Act
        var result = ProductName.Create("");

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldFail_WhenNameIsNull()
    {
        // Act
        var result = ProductName.Create(null);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Act
        var result = ProductName.Create("  노트북  ");

        // Assert
        result.IsSucc.ShouldBeTrue();
        ((string)result.ThrowIfFail()).ShouldBe("노트북");
    }
}
