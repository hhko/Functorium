using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Inventories.Specifications;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Inventories;

public class InventoryLowStockSpecTests
{
    private static Inventory CreateSampleInventory(int stockQuantity = 10)
    {
        return Inventory.Create(
            ProductId.New(),
            Quantity.Create(stockQuantity).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenStockBelowThreshold()
    {
        // Arrange
        var inventory = CreateSampleInventory(stockQuantity: 3);
        var sut = new InventoryLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(inventory);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenStockAboveThreshold()
    {
        // Arrange
        var inventory = CreateSampleInventory(stockQuantity: 10);
        var sut = new InventoryLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(inventory);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsFalse_WhenStockEqualsThreshold()
    {
        // Arrange
        var inventory = CreateSampleInventory(stockQuantity: 5);
        var sut = new InventoryLowStockSpec(Quantity.Create(5).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(inventory);

        // Assert
        actual.ShouldBeFalse();
    }
}
