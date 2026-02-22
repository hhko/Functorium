using LayeredArch.Application.Usecases.Products.Commands;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class RestoreProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly RestoreProductCommand.Usecase _sut;

    public RestoreProductCommandTests()
    {
        _sut = new RestoreProductCommand.Usecase(_productRepository);
    }

    private static Product CreateDeletedProduct()
    {
        var product = Product.Create(
            ProductName.Create("Deleted Product").ThrowIfFail(),
            ProductDescription.Create("Deleted Desc").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail());
        product.Delete("admin");
        return product;
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenDeletedProductExists()
    {
        // Arrange
        var deletedProduct = CreateDeletedProduct();
        var request = new RestoreProductCommand.Request(
            deletedProduct.Id.ToString());

        _productRepository.GetByIdIncludingDeleted(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(deletedProduct));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().ProductId.ShouldBe(deletedProduct.Id.ToString());
        actual.ThrowIfFail().Name.ShouldBe("Deleted Product");
        actual.ThrowIfFail().Price.ShouldBe(100m);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenProductNotDeleted()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Active Product").ThrowIfFail(),
            ProductDescription.Create("Active Desc").ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());
        var request = new RestoreProductCommand.Request(
            product.Id.ToString());

        _productRepository.GetByIdIncludingDeleted(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(product));
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
        var request = new RestoreProductCommand.Request(
            ProductId.New().ToString());

        _productRepository.GetByIdIncludingDeleted(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<Product>(Error.New("Product not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
