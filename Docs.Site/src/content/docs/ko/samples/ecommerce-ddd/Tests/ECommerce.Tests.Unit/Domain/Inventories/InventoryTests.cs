using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Inventories;

public class InventoryTests
{
    private static Inventory CreateSampleInventory(int stockQuantity = 10)
    {
        return Inventory.Create(
            ProductId.New(),
            Quantity.Create(stockQuantity).ThrowIfFail());
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Act
        var sut = CreateSampleInventory();

        // Assert
        sut.Id.ShouldNotBe(default);
        sut.DomainEvents.ShouldContain(e => e is Inventory.CreatedEvent);
    }

    [Fact]
    public void DeductStock_ShouldSucceed_WhenSufficientStock()
    {
        // Arrange
        var sut = CreateSampleInventory(10);
        sut.ClearDomainEvents();

        // Act
        var result = sut.DeductStock(Quantity.Create(3).ThrowIfFail());

        // Assert
        result.IsSucc.ShouldBeTrue();
        ((int)sut.StockQuantity).ShouldBe(7);
        sut.DomainEvents.ShouldContain(e => e is Inventory.StockDeductedEvent);
    }

    [Fact]
    public void DeductStock_ShouldFail_WhenInsufficientStock()
    {
        // Arrange
        var sut = CreateSampleInventory(2);

        // Act
        var result = sut.DeductStock(Quantity.Create(5).ThrowIfFail());

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void DeductStock_ShouldPublishStockDeductedEvent()
    {
        // Arrange
        var sut = CreateSampleInventory(10);
        sut.ClearDomainEvents();

        // Act
        sut.DeductStock(Quantity.Create(3).ThrowIfFail());

        // Assert
        sut.DomainEvents.ShouldContain(e => e is Inventory.StockDeductedEvent);
    }

    [Fact]
    public void AddStock_ShouldIncreaseQuantity()
    {
        // Arrange
        var sut = CreateSampleInventory(10);
        sut.ClearDomainEvents();

        // Act
        sut.AddStock(Quantity.Create(5).ThrowIfFail());

        // Assert
        ((int)sut.StockQuantity).ShouldBe(15);
        sut.DomainEvents.ShouldContain(e => e is Inventory.StockAddedEvent);
    }

    [Fact]
    public void CreateFromValidated_ShouldNotPublishEvents()
    {
        // Arrange
        var id = InventoryId.New();
        var productId = ProductId.New();
        var stockQuantity = Quantity.Create(10).ThrowIfFail();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = Inventory.CreateFromValidated(id, productId, stockQuantity, rowVersion, createdAt, updatedAt);

        // Assert
        sut.DomainEvents.ShouldBeEmpty();
    }
}
