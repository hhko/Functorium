using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Persistence.Mappers;

public class InventoryMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        // Arrange
        var inventory = Inventory.Create(
            ProductId.New(),
            Quantity.Create(42).ThrowIfFail());

        // Act
        var actual = inventory.ToModel().ToDomain();

        // Assert
        actual.Id.ToString().ShouldBe(inventory.Id.ToString());
        actual.ProductId.ToString().ShouldBe(inventory.ProductId.ToString());
        ((int)actual.StockQuantity).ShouldBe(inventory.StockQuantity);
        actual.CreatedAt.ShouldBe(inventory.CreatedAt);
        actual.UpdatedAt.ShouldBe(inventory.UpdatedAt);
    }

    [Fact]
    public void RoundTrip_ShouldClearDomainEvents()
    {
        // Arrange
        var inventory = Inventory.Create(
            ProductId.New(),
            Quantity.Create(10).ThrowIfFail());

        // Act
        var actual = inventory.ToModel().ToDomain();

        // Assert - 복원 과정에서 발행된 이벤트는 제거되어야 함
        actual.DomainEvents.ShouldBeEmpty();
    }
}
