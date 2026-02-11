using Functorium.Applications.Events;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class DeductStockCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly DeductStockCommand.Usecase _sut;

    public DeductStockCommandTests()
    {
        _sut = new DeductStockCommand.Usecase(_productRepository, _eventPublisher);
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
            .Returns(TestIO.Succ(product));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => TestIO.Succ(call.Arg<Product>()));
        _eventPublisher.PublishEvents(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(TestIO.Succ(unit));

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
            .Returns(TestIO.Succ(product));

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
            .Returns(TestIO.Fail<Product>(Error.New("Product not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
