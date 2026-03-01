using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Application.Usecases.Orders.Queries;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Orders;

public class GetOrderWithProductsQueryValidatorTests
{
    private readonly GetOrderWithProductsQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenOrderIdProvided()
    {
        // Arrange
        var request = new GetOrderWithProductsQuery.Request(OrderId.New().ToString());

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenOrderIdEmpty()
    {
        // Arrange
        var request = new GetOrderWithProductsQuery.Request("");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "OrderId");
    }
}

public class GetOrderWithProductsQueryTests
{
    private readonly IOrderWithProductsQuery _readAdapter = Substitute.For<IOrderWithProductsQuery>();
    private readonly GetOrderWithProductsQuery.Usecase _sut;

    public GetOrderWithProductsQueryTests()
    {
        _sut = new GetOrderWithProductsQuery.Usecase(_readAdapter);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenOrderExists()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = CustomerId.New();
        var productId = ProductId.New();
        var dto = new OrderWithProductsDto(
            orderId.ToString(),
            customerId.ToString(),
            Seq(new OrderLineWithProductDto(productId.ToString(), "Test Product", 2, 100m, 200m)),
            200m,
            "Pending",
            DateTime.UtcNow);

        var request = new GetOrderWithProductsQuery.Request(orderId.ToString());

        _readAdapter.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Succ(dto));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var order = actual.ThrowIfFail().Order;
        order.OrderId.ShouldBe(orderId.ToString());
        order.OrderLines.Count.ShouldBe(1);
        order.OrderLines[0].ProductName.ShouldBe("Test Product");
        order.TotalAmount.ShouldBe(200m);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenOrderHasMultipleLines()
    {
        // Arrange
        var orderId = OrderId.New();
        var customerId = CustomerId.New();
        var dto = new OrderWithProductsDto(
            orderId.ToString(),
            customerId.ToString(),
            Seq(
                new OrderLineWithProductDto(ProductId.New().ToString(), "Product A", 1, 100m, 100m),
                new OrderLineWithProductDto(ProductId.New().ToString(), "Product B", 3, 50m, 150m)),
            250m,
            "Confirmed",
            DateTime.UtcNow);

        var request = new GetOrderWithProductsQuery.Request(orderId.ToString());

        _readAdapter.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Succ(dto));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Order.OrderLines.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenOrderNotFound()
    {
        // Arrange
        var request = new GetOrderWithProductsQuery.Request(OrderId.New().ToString());

        _readAdapter.GetById(Arg.Any<OrderId>())
            .Returns(FinTFactory.Fail<OrderWithProductsDto>(Error.New("Order not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
