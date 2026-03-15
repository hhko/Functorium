using ECommerce.Application.Usecases.Orders.Ports;
using ECommerce.Application.Usecases.Orders.Queries;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Tests.Unit.Application.Orders;

public class GetOrderByIdQueryTests
{
    private readonly IOrderDetailQuery _adapter = Substitute.For<IOrderDetailQuery>();
    private readonly GetOrderByIdQuery.Usecase _sut;

    public GetOrderByIdQueryTests()
    {
        _sut = new GetOrderByIdQuery.Usecase(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrder_WhenExists()
    {
        // Arrange
        var orderId = OrderId.New();
        var productId = ProductId.New();
        var dto = new OrderDetailDto(
            orderId.ToString(),
            Seq(new OrderLineDetailDto(productId.ToString(), 2, 100m, 200m)),
            200m,
            "Seoul, Korea",
            DateTime.UtcNow);

        var request = new GetOrderByIdQuery.Request(orderId.ToString());

        _adapter.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Succ(dto));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().OrderLines.Count.ShouldBe(1);
        actual.ThrowIfFail().TotalAmount.ShouldBe(200m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var request = new GetOrderByIdQuery.Request(OrderId.New().ToString());

        _adapter.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Fail<OrderDetailDto>(Error.New("Order not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
