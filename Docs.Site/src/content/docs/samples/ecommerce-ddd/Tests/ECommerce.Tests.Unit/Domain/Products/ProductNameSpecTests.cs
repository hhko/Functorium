using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Products;

public class ProductNameSpecTests
{
    private static Product CreateSampleProduct(string name = "Test Product")
    {
        return Product.Create(
            ProductName.Create(name).ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenNameMatches()
    {
        // Arrange
        var product = CreateSampleProduct(name: "Test Product");
        var sut = new ProductNameSpec(ProductName.Create("Test Product").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenNameDoesNotMatch()
    {
        // Arrange
        var product = CreateSampleProduct(name: "Test Product");
        var sut = new ProductNameSpec(ProductName.Create("Other Product").ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeFalse();
    }
}
