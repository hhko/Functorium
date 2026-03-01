using LayeredArch.Application.Usecases.Orders.Commands;
using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Orders;

public class CreateOrderWithCreditCheckCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly CreateOrderWithCreditCheckCommand.Usecase _sut;

    public CreateOrderWithCreditCheckCommandTests()
    {
        _sut = new CreateOrderWithCreditCheckCommand.Usecase(
            _customerRepository, _orderRepository, _productCatalog);
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
            customer.Id.ToString(),
            Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(productId.ToString(), 2)),
            "Seoul, Korea");

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
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
            customer.Id.ToString(),
            Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(productId.ToString(), 2)),
            "Seoul, Korea");

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });

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
            customer.Id.ToString(),
            Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "Seoul, Korea");

        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(FinTFactory.Succ(LanguageExt.Seq<(ProductId Id, Money Price)>.Empty));

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
            CustomerId.New().ToString(),
            Seq(new CreateOrderWithCreditCheckCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(100m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Fail<Customer>(Error.New("Customer not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
