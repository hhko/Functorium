using ECommerce.Application.Usecases.Orders.Commands;
using ECommerce.Application.Usecases.Orders;
using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Customers.ValueObjects;
using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Orders;

public class PlaceOrderCommandValidatorTests
{
    private readonly PlaceOrderCommand.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenRequestIsValid()
    {
        // Arrange
        var request = new PlaceOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "Seoul, Korea");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ReturnsValidationError_WhenShippingAddressIsEmpty()
    {
        // Arrange
        var request = new PlaceOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "ShippingAddress");
    }
}

public class PlaceOrderCommandTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly PlaceOrderCommand.Usecase _sut;

    public PlaceOrderCommandTests()
    {
        _sut = new PlaceOrderCommand.Usecase(
            _customerRepository, _orderRepository, _inventoryRepository, _productCatalog);
    }

    private static Customer CreateSampleCustomer(decimal creditLimit = 5000m)
    {
        return Customer.Create(
            CustomerName.Create("John").ThrowIfFail(),
            Email.Create("john@example.com").ThrowIfFail(),
            Money.Create(creditLimit).ThrowIfFail());
    }

    private static Inventory CreateInventoryWithStock(ProductId productId, int stock)
    {
        return Inventory.Create(productId, Quantity.Create(stock).ThrowIfFail());
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenCreditLimitAndStockSufficient()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 5000m);
        var productId = ProductId.New();
        var inventory = CreateInventoryWithStock(productId, 10);
        var request = new PlaceOrderCommand.Request(
            customer.Id.ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(productId.ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(inventory));
        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));
        _orderRepository.Create(Arg.Any<Order>())
            .Returns(call => FinTFactory.Succ(call.Arg<Order>()));
        _inventoryRepository.UpdateRange(Arg.Any<IReadOnlyList<Inventory>>())
            .Returns(call => FinTFactory.Succ(toSeq(call.Arg<IReadOnlyList<Inventory>>())));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().TotalAmount.ShouldBe(2000m);
        actual.ThrowIfFail().DeductedStocks.Count.ShouldBe(1);
        actual.ThrowIfFail().DeductedStocks[0].RemainingStock.ShouldBe(8);
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenCreditLimitExceeded()
    {
        // Arrange
        var customer = CreateSampleCustomer(creditLimit: 1000m);
        var productId = ProductId.New();
        var inventory = CreateInventoryWithStock(productId, 10);
        var request = new PlaceOrderCommand.Request(
            customer.Id.ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(productId.ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(inventory));
        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Succ(customer));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenInsufficientStock()
    {
        // Arrange
        var productId = ProductId.New();
        var inventory = CreateInventoryWithStock(productId, 1);
        var request = new PlaceOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(productId.ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(inventory));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var request = new PlaceOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "Seoul, Korea");

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
        var productId = ProductId.New();
        var inventory = CreateInventoryWithStock(productId, 10);
        var request = new PlaceOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(productId.ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(inventory));
        _customerRepository.GetById(Arg.Any<CustomerId>())
            .Returns(FinTFactory.Fail<Customer>(Error.New("Customer not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenInventoryNotFound()
    {
        // Arrange
        var request = new PlaceOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new PlaceOrderCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(1000m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
        _inventoryRepository.GetByProductId(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<Inventory>(Error.New("Inventory not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
