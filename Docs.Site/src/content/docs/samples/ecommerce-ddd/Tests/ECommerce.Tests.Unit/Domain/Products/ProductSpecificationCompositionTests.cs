using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Products;

public class ProductSpecificationCompositionTests
{
    private static Product CreateSampleProduct(
        string name = "Test Product",
        decimal price = 100m)
    {
        return Product.Create(
            ProductName.Create(name).ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(price).ThrowIfFail());
    }

    [Fact]
    public void AndOperator_ReturnsTrue_WhenBothSpecsSatisfied()
    {
        // Arrange: 가격 100~200 AND 이름 일치
        var product = CreateSampleProduct(name: "Test Product", price: 150m);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            & new ProductNameUniqueSpec(ProductName.Create("Test Product").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void AndOperator_ReturnsFalse_WhenOnlyPriceSpecSatisfied()
    {
        // Arrange: 가격 범위 내이지만 이름 불일치
        var product = CreateSampleProduct(name: "Test Product", price: 150m);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            & new ProductNameUniqueSpec(ProductName.Create("Other Name").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void OrOperator_ReturnsTrue_WhenEitherSpecSatisfied()
    {
        // Arrange: 가격 범위 내 OR 이름 일치
        var product = CreateSampleProduct(name: "Test Product", price: 150m);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            | new ProductNameUniqueSpec(ProductName.Create("Other Name").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void NotOperator_ReturnsTrue_WhenSpecNotSatisfied()
    {
        // Arrange: 이름이 일치하지 않는 상품
        var product = CreateSampleProduct(name: "Test Product");
        var sut = !new ProductNameUniqueSpec(ProductName.Create("Other Name").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Composite_PriceRangeAndNotNameMatch_ReturnsExpected()
    {
        // Arrange: 가격 범위 내이면서 특정 이름이 아닌 상품
        var product = CreateSampleProduct(name: "Test Product", price: 150m);
        var sut = new ProductPriceRangeSpec(
                Money.Create(100m).ThrowIfFail(),
                Money.Create(200m).ThrowIfFail())
            & !new ProductNameUniqueSpec(ProductName.Create("Other Name").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
