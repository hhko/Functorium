using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Products;

public class ProductNameUniqueSpecTests
{
    private static Product CreateSampleProduct(
        string name = "Test Product",
        string description = "Test Description",
        decimal price = 100m)
    {
        return Product.Create(
            ProductName.Create(name).ThrowIfFail(),
            ProductDescription.Create(description).ThrowIfFail(),
            Money.Create(price).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenNameMatches()
    {
        // Arrange
        var product = CreateSampleProduct(name: "Laptop");
        var sut = new ProductNameUniqueSpec(ProductName.Create("Laptop").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenNameDoesNotMatch()
    {
        // Arrange
        var product = CreateSampleProduct(name: "Laptop");
        var sut = new ProductNameUniqueSpec(ProductName.Create("Phone").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenNameMatchesButExcludeIdMatches()
    {
        // Arrange
        var product = CreateSampleProduct(name: "Laptop");
        var sut = new ProductNameUniqueSpec(
            ProductName.Create("Laptop").ThrowIfFail(),
            excludeId: product.Id);

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenNameMatchesAndExcludeIdDiffers()
    {
        // Arrange
        var product = CreateSampleProduct(name: "Laptop");
        var otherId = ProductId.New();
        var sut = new ProductNameUniqueSpec(
            ProductName.Create("Laptop").ThrowIfFail(),
            excludeId: otherId);

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
