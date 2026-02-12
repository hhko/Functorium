using Functorium.Applications.Events;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class UpdateProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly UpdateProductCommand.Usecase _sut;

    public UpdateProductCommandTests()
    {
        _sut = new UpdateProductCommand.Usecase(_productRepository, _eventPublisher);
    }

    private static Product CreateExistingProduct()
    {
        return Product.Create(
            ProductName.Create("Old Product").ThrowIfFail(),
            ProductDescription.Create("Old Desc").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail(),
            Quantity.Create(10).ThrowIfFail());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var existingProduct = CreateExistingProduct();
        var request = new UpdateProductCommand.Request(
            existingProduct.Id.ToString(), "Updated Product", "Updated Desc", 200m, 20);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(existingProduct));
        _productRepository.ExistsByName(Arg.Any<ProductName>(), Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(false));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));
        _eventPublisher.PublishEvents(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Updated Product");
        actual.ThrowIfFail().Price.ShouldBe(200m);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        // Arrange
        var request = new UpdateProductCommand.Request(
            ProductId.New().ToString(), "Updated", "Desc", 200m, 20);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<Product>(Error.New("Product not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDuplicateName()
    {
        // Arrange
        var existingProduct = CreateExistingProduct();
        var request = new UpdateProductCommand.Request(
            existingProduct.Id.ToString(), "Duplicate Name", "Desc", 200m, 20);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(existingProduct));
        _productRepository.ExistsByName(Arg.Any<ProductName>(), Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(true));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenVOIsInvalid()
    {
        // Arrange
        var request = new UpdateProductCommand.Request(
            ProductId.New().ToString(), "", "Desc", 200m, 20);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
