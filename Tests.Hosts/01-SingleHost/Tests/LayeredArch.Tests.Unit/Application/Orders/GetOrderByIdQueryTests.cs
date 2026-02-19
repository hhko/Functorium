using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Application.Usecases.Orders.Queries;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Orders;

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
            orderId.ToString(), productId.ToString(), 2, 100m, 200m,
            "Seoul, Korea", DateTime.UtcNow);

        var request = new GetOrderByIdQuery.Request(orderId.ToString());

        _adapter.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Succ(dto));

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

        _adapter.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Fail<OrderDetailDto>(Error.New("Order not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
