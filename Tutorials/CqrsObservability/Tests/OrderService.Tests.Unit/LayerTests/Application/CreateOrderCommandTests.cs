using LanguageExt;
using LanguageExt.Common;
using NSubstitute;
using OrderService.Adapters.Messaging;
using CqrsObservability.Messages;
using OrderService.Domain;
using OrderService.Usecases;
using Shouldly;
using static LanguageExt.Prelude;
using Xunit;

namespace OrderService.Tests.Unit.LayerTests.Application;

/// <summary>
/// CreateOrderCommand Usecase 단위 테스트
/// </summary>
public sealed class CreateOrderCommandTests
{
    [Fact]
    public async Task Handle_ReturnsSuccess_WhenInventoryIsAvailable()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateOrderCommand.Request(productId, Quantity: 5);

        var inventoryMessaging = Substitute.For<IInventoryMessaging>();
        var checkResponse = new CheckInventoryResponse(
            ProductId: productId,
            IsAvailable: true,
            AvailableQuantity: 10);
        inventoryMessaging.CheckInventory(Arg.Any<CheckInventoryRequest>())
            .Returns(IO.lift(() => Fin.Succ(checkResponse)));

        var orderRepository = Substitute.For<IOrderRepository>();
        var createdOrder = new Order(Guid.NewGuid(), productId, 5, DateTime.UtcNow);
        orderRepository.Create(Arg.Any<Order>())
            .Returns(IO.lift(() => Fin.Succ(createdOrder)));

        inventoryMessaging.ReserveInventory(Arg.Any<ReserveInventoryCommand>())
            .Returns(IO.lift(() => Fin.Succ(unit)));

        var usecase = new CreateOrderCommand.Usecase(inventoryMessaging, orderRepository);

        // Act
        var result = await usecase.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Match(
            Succ: response =>
            {
                response.ShouldNotBeNull();
                response.ProductId.ShouldBe(productId);
                response.Quantity.ShouldBe(5);
            },
            Fail: _ => throw new Exception("Should be success"));
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenInventoryIsInsufficient()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateOrderCommand.Request(productId, Quantity: 10);

        var inventoryMessaging = Substitute.For<IInventoryMessaging>();
        var checkResponse = new CheckInventoryResponse(
            ProductId: productId,
            IsAvailable: false,
            AvailableQuantity: 5);
        inventoryMessaging.CheckInventory(Arg.Any<CheckInventoryRequest>())
            .Returns(IO.lift(() => Fin.Succ(checkResponse)));

        var orderRepository = Substitute.For<IOrderRepository>();
        var usecase = new CreateOrderCommand.Usecase(inventoryMessaging, orderRepository);

        // Act
        var result = await usecase.Handle(request, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: error => error.Message.ShouldContain("재고"));
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenCheckInventoryFails()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new CreateOrderCommand.Request(productId, Quantity: 5);

        var inventoryMessaging = Substitute.For<IInventoryMessaging>();
        var error = Error.New("재고 확인 실패");
        inventoryMessaging.CheckInventory(Arg.Any<CheckInventoryRequest>())
            .Returns(IO.lift(() => Fin.Fail<CheckInventoryResponse>(error)));

        var orderRepository = Substitute.For<IOrderRepository>();
        var usecase = new CreateOrderCommand.Usecase(inventoryMessaging, orderRepository);

        // Act
        var result = await usecase.Handle(request, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Match(
            Succ: _ => throw new Exception("Should be failure"),
            Fail: err => err.Message.ShouldContain("재고 확인 실패"));
    }
}

