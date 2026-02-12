using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class GetProductByIdQueryTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly GetProductByIdQuery.Usecase _sut;

    public GetProductByIdQueryTests()
    {
        _sut = new GetProductByIdQuery.Usecase(_productRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnProduct_WhenExists()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Desc").ThrowIfFail(),
            Money.Create(100m).ThrowIfFail(),
            Quantity.Create(10).ThrowIfFail());

        var request = new GetProductByIdQuery.Request(product.Id.ToString());

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Succ(product));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Name.ShouldBe("Test Product");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenNotFound()
    {
        // Arrange
        var request = new GetProductByIdQuery.Request(ProductId.New().ToString());

        _productRepository.GetById(Arg.Any<ProductId>())
            .Returns(FinTFactory.Fail<Product>(Error.New("Product not found")));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeFalse();
    }
}
