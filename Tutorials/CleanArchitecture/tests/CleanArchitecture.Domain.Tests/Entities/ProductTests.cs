using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ReturnsProduct()
    {
        // Arrange
        var price = new Money(99.99m, "USD");

        // Act
        var product = Product.Create("Laptop", "LAP-001", price);

        // Assert
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Laptop", product.Name);
        Assert.Equal("LAP-001", product.Sku);
        Assert.Equal(99.99m, product.Price.Amount);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsDomainException()
    {
        // Arrange
        var price = new Money(99.99m, "USD");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            Product.Create("", "LAP-001", price));

        Assert.Equal("Product name is required", exception.Message);
    }

    [Fact]
    public void Create_WithLongName_ThrowsDomainException()
    {
        // Arrange
        var price = new Money(99.99m, "USD");
        var longName = new string('a', 201);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            Product.Create(longName, "LAP-001", price));

        Assert.Equal("Product name cannot exceed 200 characters", exception.Message);
    }

    [Fact]
    public void AddStock_WithPositiveQuantity_IncreasesStock()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));

        // Act
        product.AddStock(10);

        // Assert
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public void AddStock_WithZeroQuantity_ThrowsDomainException()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));

        // Act & Assert
        Assert.Throws<DomainException>(() => product.AddStock(0));
    }

    [Fact]
    public void RemoveStock_WithInsufficientStock_ThrowsDomainException()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));
        product.AddStock(5);

        // Act & Assert
        Assert.Throws<DomainException>(() => product.RemoveStock(10));
    }

    [Fact]
    public void RemoveStock_WithSufficientStock_DecreasesStock()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));
        product.AddStock(10);

        // Act
        product.RemoveStock(3);

        // Assert
        Assert.Equal(7, product.StockQuantity);
    }

    [Fact]
    public void UpdatePrice_WithValidPrice_UpdatesPrice()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));
        var newPrice = new Money(149.99m, "USD");

        // Act
        product.UpdatePrice(newPrice);

        // Assert
        Assert.Equal(149.99m, product.Price.Amount);
    }

    [Fact]
    public void UpdatePrice_WithZeroPrice_ThrowsDomainException()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));
        var zeroPrice = new Money(0m, "USD");

        // Act & Assert
        Assert.Throws<DomainException>(() => product.UpdatePrice(zeroPrice));
    }

    [Fact]
    public void Deactivate_WhenActive_SetsIsActiveToFalse()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));

        // Act
        product.Deactivate();

        // Assert
        Assert.False(product.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        // Arrange
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));
        product.Deactivate();

        // Act & Assert
        Assert.Throws<DomainException>(() => product.Deactivate());
    }
}
