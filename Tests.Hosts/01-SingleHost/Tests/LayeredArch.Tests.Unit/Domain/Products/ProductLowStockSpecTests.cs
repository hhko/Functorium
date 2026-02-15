using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Products;

public class ProductLowStockSpecTests
{
    private static Product CreateSampleProduct(int stockQuantity = 10)
    {
        return Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail(),
            Quantity.Create(stockQuantity).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenStockBelowThreshold()
    {
        // Arrange
        var product = CreateSampleProduct(stockQuantity: 3);
        var sut = new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenStockAboveThreshold()
    {
        // Arrange
        var product = CreateSampleProduct(stockQuantity: 10);
        var sut = new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenStockEqualsThreshold()
    {
        // Arrange
        var product = CreateSampleProduct(stockQuantity: 5);
        var sut = new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }
}
