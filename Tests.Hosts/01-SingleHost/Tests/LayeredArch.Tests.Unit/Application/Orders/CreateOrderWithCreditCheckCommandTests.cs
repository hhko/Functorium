using LayeredArch.Application.Usecases.Orders.Commands;
using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.Services;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Orders;

public class CreateOrderWithCreditCheckCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly OrderCreditCheckService _creditCheckService = new();
    private readonly CreateOrderWithCreditCheckCommand.Usecase _sut;

    public CreateOrderWithCreditCheckCommandTests()
    {
        _sut = new CreateOrderWithCreditCheckCommand.Usecase(
            _customerRepository, _orderRepository, _productCatalog, _creditCheckService);
    }

    private static Customer CreateSampleCustomer(decimal creditLimit = 5000m)
    {
        return Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(creditLimit).ThrowIfFail());
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenCreditLimitIsSufficient()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var productId = ProductId.New();
        var request = new CreateOrderWithCreditCheckCommand.Request(
            customer.Id.ToString(), productId.ToString(), 2, "Seoul, Korea");

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(true));
        _productCatalog.GetPrice(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(Money.Create(1000m).ThrowIfFail()));
        _orderRepository.Create(Arg.Any<Order>())
            .Returns(call => FinTFactory.Succ(call.Arg<Order>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().TotalAmount.ShouldBe(2000m);
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenCreditLimitExceeded()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 1000m);
        var productId = ProductId.New();
        var request = new CreateOrderWithCreditCheckCommand.Request(
            customer.Id.ToString(), productId.ToString(), 2, "Seoul, Korea");

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(true));
        _productCatalog.GetPrice(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(Money.Create(1000m).ThrowIfFail()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var customer = CreateSampleCustomer();
        var request = new CreateOrderWithCreditCheckCommand.Request(
            customer.Id.ToString(), ProductId.New().ToString(), 2, "Seoul, Korea");

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(false));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenCustomerNotFound()
    {
        // Arrange
        var request = new CreateOrderWithCreditCheckCommand.Request(
            CustomerId.New().ToString(), ProductId.New().ToString(), 2, "Seoul, Korea");

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Fail<Customer>(Error.New("Customer not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
