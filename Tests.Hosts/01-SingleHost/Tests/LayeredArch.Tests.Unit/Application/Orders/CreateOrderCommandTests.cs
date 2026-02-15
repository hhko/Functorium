using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly CreateOrderCommand.Usecase _sut;

    public CreateOrderCommandTests()
    {
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _sut = new CreateOrderCommand.Usecase(_orderRepository, _productCatalog, _unitOfWork, _eventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var productId = ProductId.New();
        var request = new CreateOrderCommand.Request(productId.ToString(), 2, "Seoul, Korea");

        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(true));
        _productCatalog.GetPrice(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(Money.Create(100m).ThrowIfFail()));
        _orderRepository.Create(Arg.Any<Order>())
            .Returns(call => FinTFactory.Succ(call.Arg<Order>()));
        _eventPublisher.PublishEvents(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));

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
            .Returns(FinTFactory.Succ(false));

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

    [Fact]
    public async Task Handle_ShouldCallSaveChangesBeforePublishEvents_WhenRequestIsValid()
    {
        // Arrange
        var callOrder = new List<string>();
        var productId = ProductId.New();
        var request = new CreateOrderCommand.Request(productId.ToString(), 2, "Seoul, Korea");

        _productCatalog.ExistsById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(true));
        _productCatalog.GetPrice(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(Money.Create(100m).ThrowIfFail()));
        _orderRepository.Create(Arg.Any<Order>())
            .Returns(call => FinTFactory.Succ(call.Arg<Order>()));
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit))
            .AndDoes(_ => callOrder.Add("SaveChanges"));
        _eventPublisher.PublishEvents(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit))
            .AndDoes(_ => callOrder.Add("PublishEvents"));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        callOrder.ShouldBe(["SaveChanges", "PublishEvents"]);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSaveChanges_WhenValidationFails()
    {
        // Arrange
        var request = new CreateOrderCommand.Request(ProductId.New().ToString(), 2, "");

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
        _unitOfWork.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
    }
}
