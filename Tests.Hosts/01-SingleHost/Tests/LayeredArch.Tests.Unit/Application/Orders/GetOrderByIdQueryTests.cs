using LayeredArch.Application.Usecases.Orders;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Orders;

public class GetOrderByIdQueryTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly GetOrderByIdQuery.Usecase _sut;

    public GetOrderByIdQueryTests()
    {
        _sut = new GetOrderByIdQuery.Usecase(_orderRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrder_WhenExists()
    {
        // Arrange
        var order = Order.Create(
            ProductId.New(),
            Quantity.Create(2).ThrowIfFail(),
            Money.Create(100m).ThrowIfFail(),
            ShippingAddress.Create("Seoul, Korea").ThrowIfFail());

        var request = new GetOrderByIdQuery.Request(order.Id.ToString());

        _orderRepository.GetById(Arg.Any<OrderId>())
            .Returns(TestIO.Succ(order));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Quantity.ShouldBe(2);
        actual.ThrowIfFail().TotalAmount.ShouldBe(200m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var request = new GetOrderByIdQuery.Request(OrderId.New().ToString());

        _orderRepository.GetById(Arg.Any<OrderId>())
            .Returns(TestIO.Fail<Order>(Error.New("Order not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
