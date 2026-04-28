using ECommerce.Application.Usecases.Orders.Commands;
using ECommerce.Application.Usecases.Orders;
using ECommerce.Domain.AggregateRoots.Customers;
using ECommerce.Domain.AggregateRoots.Orders;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Application.Orders;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommand.Validator _sut = new();

    [Fact]
    public void Validate_ReturnsNoError_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new CreateOrderCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
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
        var request = new CreateOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new CreateOrderCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "");

        // Act
        var actual = _sut.Validate(request);

        // Assert
        actual.IsValid.ShouldBeFalse();
        actual.Errors.ShouldContain(e => e.PropertyName == "ShippingAddress");
    }
}

public class CreateOrderCommandTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly CreateOrderCommand.Usecase _sut;

    public CreateOrderCommandTests()
    {
        _sut = new CreateOrderCommand.Usecase(_orderRepository, _productCatalog);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var customerId = CustomerId.New();
        var productId = ProductId.New();
        var request = new CreateOrderCommand.Request(
            customerId.ToString(),
            Seq(new CreateOrderCommand.OrderLineRequest(productId.ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<ProductId>>();
                var prices = toSeq(ids.Select(id => (id, Money.Create(100m).ThrowIfFail())));
                return FinTFactory.Succ(prices);
            });
        _orderRepository.Create(Arg.Any<Order>())
            .Returns(call => FinTFactory.Succ(call.Arg<Order>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().OrderLines.Count.ShouldBe(1);
        actual.ThrowIfFail().TotalAmount.ShouldBe(200m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var request = new CreateOrderCommand.Request(
            CustomerId.New().ToString(),
            Seq(new CreateOrderCommand.OrderLineRequest(ProductId.New().ToString(), 2)),
            "Seoul, Korea");

        _productCatalog.GetPricesForProducts(Arg.Any<IReadOnlyList<ProductId>>())
            .Returns(FinTFactory.Succ(LanguageExt.Seq<(ProductId Id, Money Price)>.Empty));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
