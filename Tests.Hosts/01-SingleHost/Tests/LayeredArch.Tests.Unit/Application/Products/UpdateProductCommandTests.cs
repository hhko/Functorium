using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Commands;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class UpdateProductCommandTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly UpdateProductCommand.Usecase _sut;

    public UpdateProductCommandTests()
    {
        _sut = new UpdateProductCommand.Usecase(_productRepository);
    }

    private static Product CreateExistingProduct()
    {
        return Product.Create(
            ProductName.Create("Old Product").ThrowIfFail(),
            ProductDescription.Create("Old Desc").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var existingProduct = CreateExistingProduct();
        var request = new UpdateProductCommand.Request(
            existingProduct.Id.ToString(), "Updated Product", "Updated Desc", 200m);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(existingProduct));
        _productRepository.Exists(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(false));
        _productRepository.Update(Arg.Any<Product>())
            .Returns(call => FinTFactory.Succ(call.Arg<Product>()));

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
            ProductId.New().ToString(), "Updated", "Desc", 200m);

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
            existingProduct.Id.ToString(), "Duplicate Name", "Desc", 200m);

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(existingProduct));
        _productRepository.Exists(Arg.Any<Specification<Product>>())
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
            ProductId.New().ToString(), "", "Desc", 200m);

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
