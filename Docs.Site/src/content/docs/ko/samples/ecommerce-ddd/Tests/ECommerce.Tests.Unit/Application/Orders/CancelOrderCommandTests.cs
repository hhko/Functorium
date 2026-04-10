using ECommerce.Application.Usecases.Orders.Commands;
using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Orders.ValueObjects;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Orders;

public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommand.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenRequestIsValid()
    {
        // Arrange
        var request = new CancelOrderCommand.Request(OrderId.New().ToString());

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOrderIdIsInvalid()
    {
        // Arrange
        var request = new CancelOrderCommand.Request("invalid-id");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "OrderId");
    }
}

public class CancelOrderCommandTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly CancelOrderCommand.Usecase _sut;

    public CancelOrderCommandTests()
    {
        _sut = new CancelOrderCommand.Usecase(_orderRepository);
    }

    private static Order CreateOrderWithStatus(OrderStatus targetStatus)
    {
        var line = OrderLine.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail()).ThrowIfFail();
        var order = Order.Create(
            CustomerId.New(),
            [line],
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

        if (targetStatus == OrderStatus.Confirmed)
            order.Confirm();
        else if (targetStatus == OrderStatus.Shipped)
        {
            order.Confirm();
            order.Ship();
        }
        else if (targetStatus == OrderStatus.Delivered)
        {
            order.Confirm();
            order.Ship();
            order.Deliver();
        }

        order.ClearDomainEvents();
        return order;
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenOrderIsPending()
    {
        // Arrange
        var order = CreateOrderWithStatus(OrderStatus.Pending);
        var request = new CancelOrderCommand.Request(order.Id.ToString());

        _orderRepository.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Succ(order));
        _orderRepository.Update(Arg.Any<Order>())
            .Returns(call => FinTFactory.Succ(call.Arg<Order>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().OrderId.ShouldBe(order.Id.ToString());
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenOrderIsConfirmed()
    {
        // Arrange
        var order = CreateOrderWithStatus(OrderStatus.Confirmed);
        var request = new CancelOrderCommand.Request(order.Id.ToString());

        _orderRepository.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Succ(order));
        _orderRepository.Update(Arg.Any<Order>())
            .Returns(call => FinTFactory.Succ(call.Arg<Order>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().OrderId.ShouldBe(order.Id.ToString());
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenOrderIsShipped()
    {
        // Arrange
        var order = CreateOrderWithStatus(OrderStatus.Shipped);
        var request = new CancelOrderCommand.Request(order.Id.ToString());

        _orderRepository.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Succ(order));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenOrderNotFound()
    {
        // Arrange
        var request = new CancelOrderCommand.Request(OrderId.New().ToString());

        _orderRepository.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Fail<Order>(Error.New("Order not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
