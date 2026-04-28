using EcommerceFiltering.Domain;
using EcommerceFiltering.Domain.Specifications;
using EcommerceFiltering.Domain.ValueObjects;

namespace EcommerceFiltering.Tests.Unit;

public class ProductPriceRangeSpecTests
{
    private static Product CreateProduct(decimal price) => new(
        new ProductName("테스트"), new Money(price), new Quantity(1), new Category("기타"));

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenPriceIsWithinRange()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(new Money(100_000m), new Money(500_000m));
        var product = CreateProduct(300_000m);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenPriceEqualsMinBoundary()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(new Money(100_000m), new Money(500_000m));
        var product = CreateProduct(100_000m);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnTrue_WhenPriceEqualsMaxBoundary()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(new Money(100_000m), new Money(500_000m));
        var product = CreateProduct(500_000m);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenPriceIsBelowRange()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(new Money(100_000m), new Money(500_000m));
        var product = CreateProduct(99_999m);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ShouldReturnFalse_WhenPriceIsAboveRange()
    {
        // Arrange
        var spec = new ProductPriceRangeSpec(new Money(100_000m), new Money(500_000m));
        var product = CreateProduct(500_001m);

        // Act & Assert
        spec.IsSatisfiedBy(product).ShouldBeFalse();
    }
}
