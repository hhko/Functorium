using ECommerce.Application.Usecases.Orders.EventHandlers;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Orders;

public class RestoreInventoryOnOrderCancelledHandlerTests
{
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly RestoreInventoryOnOrderCancelledHandler _sut;

    public RestoreInventoryOnOrderCancelledHandlerTests()
    {
        _sut = new RestoreInventoryOnOrderCancelledHandler(_inventoryRepository);
    }

    [Fact]
    public async Task Handle_RestoresStock_ForEachOrderLine()
    {
        // Arrange
        var productId1 = ProductId.New();
        var productId2 = ProductId.New();
        var inventory1 = Inventory.Create(productId1, Quantity.Create(5).ThrowIfFail());
        var inventory2 = Inventory.Create(productId2, Quantity.Create(3).ThrowIfFail());

        var notification = new Order.CancelledEvent(
            OrderId.New(),
            Seq(
                new Order.OrderLineInfo(productId1, Quantity.Create(2).ThrowIfFail(), Money.Create(100m).ThrowIfFail(), Money.Create(200m).ThrowIfFail()),
                new Order.OrderLineInfo(productId2, Quantity.Create(1).ThrowIfFail(), Money.Create(50m).ThrowIfFail(), Money.Create(50m).ThrowIfFail())));

        _inventoryRepository.GetByProductId(productId1)
            .Returns(FinTFactory.Succ(inventory1));
        _inventoryRepository.GetByProductId(productId2)
            .Returns(FinTFactory.Succ(inventory2));
        _inventoryRepository.Update(Arg.Any<Inventory>())
            .Returns(call => FinTFactory.Succ(call.Arg<Inventory>()));

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        ((int)inventory1.StockQuantity).ShouldBe(7); // 5 + 2
        ((int)inventory2.StockQuantity).ShouldBe(4); // 3 + 1
    }

    [Fact]
    public async Task Handle_ContinuesProcessing_WhenOneInventoryNotFound()
    {
        // Arrange
        var productId1 = ProductId.New();
        var productId2 = ProductId.New();
        var inventory2 = Inventory.Create(productId2, Quantity.Create(3).ThrowIfFail());

        var notification = new Order.CancelledEvent(
            OrderId.New(),
            Seq(
                new Order.OrderLineInfo(productId1, Quantity.Create(2).ThrowIfFail(), Money.Create(100m).ThrowIfFail(), Money.Create(200m).ThrowIfFail()),
                new Order.OrderLineInfo(productId2, Quantity.Create(1).ThrowIfFail(), Money.Create(50m).ThrowIfFail(), Money.Create(50m).ThrowIfFail())));

        _inventoryRepository.GetByProductId(productId1)
            .Returns(FinTFactory.Fail<Inventory>(Error.New("Inventory not found")));
        _inventoryRepository.GetByProductId(productId2)
            .Returns(FinTFactory.Succ(inventory2));
        _inventoryRepository.Update(Arg.Any<Inventory>())
            .Returns(call => FinTFactory.Succ(call.Arg<Inventory>()));

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert — productId2의 재고는 정상 복원
        ((int)inventory2.StockQuantity).ShouldBe(4); // 3 + 1
    }
}
