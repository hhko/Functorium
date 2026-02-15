using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Products;

public class ProductSpecificationCompositionTests
{
    private static Product CreateSampleProduct(decimal price = 100m, int stockQuantity = 10)
    {
        return Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(price).ThrowIfFail(),
            Quantity.Create(stockQuantity).ThrowIfFail());
    }

    [Fact]
    public void AndOperator_ReturnsTrue_WhenBothSpecsSatisfied()
    {
        // Arrange: 가격 100~200 AND 재고 < 5
        var product = CreateSampleProduct(price: 150m, stockQuantity: 3);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            & new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void AndOperator_ReturnsFalse_WhenOnlyPriceSpecSatisfied()
    {
        // Arrange: 가격 범위 내이지만 재고 충분
        var product = CreateSampleProduct(price: 150m, stockQuantity: 10);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            & new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void OrOperator_ReturnsTrue_WhenEitherSpecSatisfied()
    {
        // Arrange: 가격 범위 내 OR 재고 부족
        var product = CreateSampleProduct(price: 150m, stockQuantity: 10);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            | new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void NotOperator_ReturnsTrue_WhenSpecNotSatisfied()
    {
        // Arrange: 재고 부족이 아닌 상품
        var product = CreateSampleProduct(stockQuantity: 10);
        var sut = !new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Composite_PriceRangeAndNotLowStock_ReturnsExpected()
    {
        // Arrange: 가격 범위 내이면서 재고 충분한 상품
        var product = CreateSampleProduct(price: 150m, stockQuantity: 10);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            & !new ProductLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
