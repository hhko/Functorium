using ECommerce.Application.Usecases.Inventories.EventHandlers;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Inventories;

public class DetectLowStockOnStockDeductedHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly DetectLowStockOnStockDeductedHandler _sut;

    public DetectLowStockOnStockDeductedHandlerTests()
    {
        _sut = new DetectLowStockOnStockDeductedHandler(_inventoryRepository);
    }

    [Fact]
    public async Task Handle_RaisesLowStockDetected_WhenBelowThreshold()
    {
        // Arrange
        var inventory = Inventory.Create(ProductId.New(), Quantity.Create(5).ThrowIfFail());
        inventory.ClearDomainEvents();

        var notification = new Inventory.StockDeductedEvent(
            inventory.Id, inventory.ProductId, Quantity.Create(3).ThrowIfFail());

        _inventoryRepository.GetById(inventory.Id)
            .Returns(FinTFactory.Succ(inventory));

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert — 재고 5 < 임계값 10 → LowStockDetectedEvent 발생
        inventory.DomainEvents.OfType<Inventory.LowStockDetectedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Handle_DoesNotRaise_WhenAboveThreshold()
    {
        // Arrange
        var inventory = Inventory.Create(ProductId.New(), Quantity.Create(15).ThrowIfFail());
        inventory.ClearDomainEvents();

        var notification = new Inventory.StockDeductedEvent(
            inventory.Id, inventory.ProductId, Quantity.Create(3).ThrowIfFail());

        _inventoryRepository.GetById(inventory.Id)
            .Returns(FinTFactory.Succ(inventory));

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert — 재고 15 >= 임계값 10 → 이벤트 없음
        inventory.DomainEvents.OfType<Inventory.LowStockDetectedEvent>().ShouldBeEmpty();
    }
}
