using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Products;

public class ProductPriceRangeSpecTests
{
    private static Product CreateSampleProduct(decimal price = 100m)
    {
        return Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(price).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenPriceWithinRange()
    {
        // Arrange
        var product = CreateSampleProduct(price: 150m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenPriceBelowRange()
    {
        // Arrange
        var product = CreateSampleProduct(price: 50m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenPriceAboveRange()
    {
        // Arrange
        var product = CreateSampleProduct(price: 250m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenPriceEqualsMinPrice()
    {
        // Arrange
        var product = CreateSampleProduct(price: 100m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenPriceEqualsMaxPrice()
    {
        // Arrange
        var product = CreateSampleProduct(price: 200m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
