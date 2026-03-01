using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Application.Usecases.Customers.Queries;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Tests.Unit.Application.Customers;

public class GetCustomerOrdersQueryValidatorTests
{
    private readonly GetCustomerOrdersQuery.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenCustomerIdProvided()
    {
        // Arrange
        var request = new GetCustomerOrdersQuery.Request(CustomerId.New().ToString());

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenCustomerIdEmpty()
    {
        // Arrange
        var request = new GetCustomerOrdersQuery.Request("");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }
}

public class GetCustomerOrdersQueryTests
{
    private readonly ICustomerOrdersQuery _readAdapter = Substitute.For<ICustomerOrdersQuery>();
    private readonly GetCustomerOrdersQuery.Usecase _sut;

    public GetCustomerOrdersQueryTests()
    {
        _sut = new GetCustomerOrdersQuery.Usecase(_readAdapter);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenCustomerHasOrders()
    {
        // Arrange
        var customerId = CustomerId.New();
        var dto = new CustomerOrdersDto(
            customerId.ToString(),
            "Test Customer",
            Seq(new CustomerOrderDto(
                "order-1",
                Seq(new CustomerOrderLineDto(ProductId.New().ToString(), "Product A", 2, 100m, 200m)),
                200m,
                "Confirmed",
                DateTime.UtcNow)));

        var request = new GetCustomerOrdersQuery.Request(customerId.ToString());

        _readAdapter.GetByCustomerId(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(dto));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var result = actual.ThrowIfFail().CustomerOrders;
        result.CustomerName.ShouldBe("Test Customer");
        result.Orders.Count.ShouldBe(1);
        result.Orders[0].OrderLines.Count.ShouldBe(1);
        result.Orders[0].OrderLines[0].ProductName.ShouldBe("Product A");
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenCustomerHasNoOrders()
    {
        // Arrange
        var customerId = CustomerId.New();
        var dto = new CustomerOrdersDto(
            customerId.ToString(),
            "New Customer",
            Seq<CustomerOrderDto>());

        var request = new GetCustomerOrdersQuery.Request(customerId.ToString());

        _readAdapter.GetByCustomerId(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(dto));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().CustomerOrders.Orders.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenCustomerHasMultipleOrdersWithMultipleLines()
    {
        // Arrange
        var customerId = CustomerId.New();
        var dto = new CustomerOrdersDto(
            customerId.ToString(),
            "VIP Customer",
            Seq(
                new CustomerOrderDto(
                    "order-1",
                    Seq(
                        new CustomerOrderLineDto(ProductId.New().ToString(), "Product A", 1, 100m, 100m),
                        new CustomerOrderLineDto(ProductId.New().ToString(), "Product B", 2, 50m, 100m)),
                    200m, "Delivered", DateTime.UtcNow.AddDays(-10)),
                new CustomerOrderDto(
                    "order-2",
                    Seq(new CustomerOrderLineDto(ProductId.New().ToString(), "Product C", 3, 200m, 600m)),
                    600m, "Pending", DateTime.UtcNow)));

        var request = new GetCustomerOrdersQuery.Request(customerId.ToString());

        _readAdapter.GetByCustomerId(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(dto));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var result = actual.ThrowIfFail().CustomerOrders;
        result.Orders.Count.ShouldBe(2);
        result.Orders[0].OrderLines.Count.ShouldBe(2);
        result.Orders[1].OrderLines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenCustomerNotFound()
    {
        // Arrange
        var request = new GetCustomerOrdersQuery.Request(CustomerId.New().ToString());

        _readAdapter.GetByCustomerId(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Fail<CustomerOrdersDto>(Error.New("Customer not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
