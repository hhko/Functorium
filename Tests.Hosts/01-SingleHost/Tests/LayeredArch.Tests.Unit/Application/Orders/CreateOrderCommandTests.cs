using Functorium.Applications.Events;
using LayeredArch.Application.Usecases.Orders;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.Ports;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Orders;

public class CreateOrderCommandTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IProductCatalog _productCatalog = Substitute.For<IProductCatalog>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly CreateOrderCommand.Usecase _sut;

    public CreateOrderCommandTests()
    {
        _sut = new CreateOrderCommand.Usecase(_orderRepository, _productCatalog, _eventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var productId = ProductId.New();
        var request = new CreateOrderCommand.Request(productId.ToString(), 2, "Seoul, Korea");

        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(TestIO.Succ(true));
        _productCatalog.GetPrice(Arg.Any<ProductId>())
            .Returns(TestIO.Succ(Money.Create(100m).ThrowIfFail()));
        _orderRepository.Create(Arg.Any<Order>())
            .Returns(call => TestIO.Succ(call.Arg<Order>()));
        _eventPublisher.PublishEvents(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(TestIO.Succ(unit));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Quantity.ShouldBe(2);
        actual.ThrowIfFail().TotalAmount.ShouldBe(200m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var request = new CreateOrderCommand.Request(ProductId.New().ToString(), 2, "Seoul, Korea");

        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(TestIO.Succ(false));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenShippingAddressIsEmpty()
    {
        // Arrange
        var request = new CreateOrderCommand.Request(ProductId.New().ToString(), 2, "");

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
