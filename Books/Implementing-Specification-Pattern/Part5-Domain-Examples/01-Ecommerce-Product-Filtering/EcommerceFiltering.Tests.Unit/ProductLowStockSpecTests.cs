using EcommerceFiltering.Domain;
using EcommerceFiltering.Domain.Specifications;
using EcommerceFiltering.Domain.ValueObjects;

namespace EcommerceFiltering.Tests.Unit;

public class ProductLowStockSpecTests
{
    private static Product CreateProduct(int stock) => new(
        new ProductName("테스트"), new Money(10_000m), new Quantity(stock), new Category("기타"));

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenStockIsBelowThreshold()
    {
        // Arrange
        var spec = new ProductLowStockSpec(new Quantity(5));
        var product = CreateProduct(3);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenStockEqualsThreshold()
    {
        // Arrange
        var spec = new ProductLowStockSpec(new Quantity(5));
        var product = CreateProduct(5);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenStockIsAboveThreshold()
    {
        // Arrange
        var spec = new ProductLowStockSpec(new Quantity(5));
        var product = CreateProduct(10);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenStockIsZero()
    {
        // Arrange
        var spec = new ProductLowStockSpec(new Quantity(1));
        var product = CreateProduct(0);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }
}
