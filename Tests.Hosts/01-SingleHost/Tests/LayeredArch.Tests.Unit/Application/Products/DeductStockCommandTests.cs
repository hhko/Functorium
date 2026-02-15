using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class DeductStockCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly DeductStockCommand.Usecase _sut;

    public DeductStockCommandTests()
    {
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _sut = new DeductStockCommand.Usecase(_productRepository, _unitOfWork, _eventPublisher);
    }

    private static Product CreateProductWithStock(int stock)
    {
        return Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Desc").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail(),
            Quantity.Create(stock).ThrowIfFail());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSufficientStock()
    {
        // Arrange
        var product = CreateProductWithStock(10);
        var request = new DeductStockCommand.Request(product.Id.ToString(), 3);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(product));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _eventPublisher.PublishEvents(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().RemainingStock.ShouldBe(7);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenInsufficientStock()
    {
        // Arrange
        var product = CreateProductWithStock(2);
        var request = new DeductStockCommand.Request(product.Id.ToString(), 5);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(product));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var request = new DeductStockCommand.Request(ProductId.New().ToString(), 3);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<Product>(Error.New("Product not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesBeforePublishEvents_WhenSufficientStock()
    {
        // Arrange
        var callOrder = new List<string>();
        var product = CreateProductWithStock(10);
        var request = new DeductStockCommand.Request(product.Id.ToString(), 3);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(product));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit))
            .AndDoes(_ => callOrder.Add("SaveChanges"));
        _eventPublisher.PublishEvents(Arg.Any<Product>(), Arg.Any<CancellationToken>())
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
        // Arrange — 수량 -1은 VO 생성에 실패하여 조기 반환됨
        var request = new DeductStockCommand.Request(ProductId.New().ToString(), -1);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
        _unitOfWork.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
    }
}
