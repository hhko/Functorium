using LayeredArch.Application.Usecases.Products.Commands;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class DeleteProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly DeleteProductCommand.Usecase _sut;

    public DeleteProductCommandTests()
    {
        _sut = new DeleteProductCommand.Usecase(_productRepository);
    }

    private static Product CreateExistingProduct()
    {
        return Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Test Desc").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail());
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenProductExists()
    {
        // Arrange
        var existingProduct = CreateExistingProduct();
        var request = new DeleteProductCommand.Request(
            existingProduct.Id.ToString(), "admin");

        _productRepository.GetByIdIncludingDeleted(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(existingProduct));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ProductId.ShouldBe(existingProduct.Id.ToString());
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenProductAlreadyDeleted()
    {
        // Arrange
        var existingProduct = CreateExistingProduct();
        existingProduct.Delete("previousUser");
        var request = new DeleteProductCommand.Request(
            existingProduct.Id.ToString(), "admin");

        _productRepository.GetByIdIncludingDeleted(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(existingProduct));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenProductNotFound()
    {
        // Arrange
        var request = new DeleteProductCommand.Request(
            ProductId.New().ToString(), "admin");

        _productRepository.GetByIdIncludingDeleted(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<Product>(Error.New("Product not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
