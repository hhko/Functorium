using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ReturnsProduct()
    {
        // Arrange
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.IsSucc.ShouldBeTrue();

        // Act & Assert
        priceResult.Match(
            Succ: price =>
            {
                var result = Product.Create("Laptop", "LAP-001", price);
                result.IsSucc.ShouldBeTrue();
                result.Match(
                    Succ: product =>
                    {
                        product.Id.ShouldNotBe(ProductId.Empty);
                        product.Name.ShouldBe("Laptop");
                        product.Sku.ShouldBe("LAP-001");
                        product.Price.Amount.ShouldBe(99.99m);
                        product.IsActive.ShouldBeTrue();
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Create_WithEmptyName_ReturnsFail()
    {
        // Arrange
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.IsSucc.ShouldBeTrue();

        // Act & Assert
        priceResult.Match(
            Succ: price =>
            {
                var result = Product.Create("", "LAP-001", price);
                result.IsFail.ShouldBeTrue();
                result.Match(
                    Succ: _ => Assert.Fail("Should fail"),
                    Fail: error => error.Message.ShouldContain("Product name is required"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Create_WithLongName_ReturnsFail()
    {
        // Arrange
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.IsSucc.ShouldBeTrue();
        var longName = new string('a', 201);

        // Act & Assert
        priceResult.Match(
            Succ: price =>
            {
                var result = Product.Create(longName, "LAP-001", price);
                result.IsFail.ShouldBeTrue();
                result.Match(
                    Succ: _ => Assert.Fail("Should fail"),
                    Fail: error => error.Message.ShouldContain("Product name cannot exceed 200 characters"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void AddStock_WithPositiveQuantity_IncreasesStock()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.Match(
            Succ: price =>
            {
                var productResult = Product.Create("Laptop", "LAP-001", price);
                productResult.Match(
                    Succ: product =>
                    {
                        var result = product.AddStock(10);
                        result.IsSucc.ShouldBeTrue();
                        product.StockQuantity.ShouldBe(10);
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void AddStock_WithZeroQuantity_ReturnsFail()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.Match(
            Succ: price =>
            {
                var productResult = Product.Create("Laptop", "LAP-001", price);
                productResult.Match(
                    Succ: product =>
                    {
                        var result = product.AddStock(0);
                        result.IsFail.ShouldBeTrue();
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void RemoveStock_WithInsufficientStock_ReturnsFail()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.Match(
            Succ: price =>
            {
                var productResult = Product.Create("Laptop", "LAP-001", price);
                productResult.Match(
                    Succ: product =>
                    {
                        product.AddStock(5);
                        var result = product.RemoveStock(10);
                        result.IsFail.ShouldBeTrue();
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void RemoveStock_WithSufficientStock_DecreasesStock()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.Match(
            Succ: price =>
            {
                var productResult = Product.Create("Laptop", "LAP-001", price);
                productResult.Match(
                    Succ: product =>
                    {
                        product.AddStock(10);
                        var result = product.RemoveStock(3);
                        result.IsSucc.ShouldBeTrue();
                        product.StockQuantity.ShouldBe(7);
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void UpdatePrice_WithValidPrice_UpdatesPrice()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        var newPriceResult = Money.Create(149.99m, "USD");

        priceResult.Match(
            Succ: price =>
            {
                newPriceResult.Match(
                    Succ: newPrice =>
                    {
                        var productResult = Product.Create("Laptop", "LAP-001", price);
                        productResult.Match(
                            Succ: product =>
                            {
                                var result = product.UpdatePrice(newPrice);
                                result.IsSucc.ShouldBeTrue();
                                product.Price.Amount.ShouldBe(149.99m);
                            },
                            Fail: _ => Assert.Fail("Should succeed"));
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void UpdatePrice_WithZeroPrice_ReturnsFail()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        var zeroPriceResult = Money.Create(0m, "USD");

        priceResult.Match(
            Succ: price =>
            {
                zeroPriceResult.Match(
                    Succ: zeroPrice =>
                    {
                        var productResult = Product.Create("Laptop", "LAP-001", price);
                        productResult.Match(
                            Succ: product =>
                            {
                                var result = product.UpdatePrice(zeroPrice);
                                result.IsFail.ShouldBeTrue();
                            },
                            Fail: _ => Assert.Fail("Should succeed"));
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveToFalse()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.Match(
            Succ: price =>
            {
                var productResult = Product.Create("Laptop", "LAP-001", price);
                productResult.Match(
                    Succ: product =>
                    {
                        var result = product.Deactivate();
                        result.IsSucc.ShouldBeTrue();
                        product.IsActive.ShouldBeFalse();
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ReturnsFail()
    {
        // Arrange & Act & Assert
        var priceResult = Money.Create(99.99m, "USD");
        priceResult.Match(
            Succ: price =>
            {
                var productResult = Product.Create("Laptop", "LAP-001", price);
                productResult.Match(
                    Succ: product =>
                    {
                        product.Deactivate();
                        var result = product.Deactivate();
                        result.IsFail.ShouldBeTrue();
                    },
                    Fail: _ => Assert.Fail("Should succeed"));
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }
}
