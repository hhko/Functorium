using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class GetAllProductsQueryTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly GetAllProductsQuery.Usecase _sut;

    public GetAllProductsQueryTests()
    {
        _sut = new GetAllProductsQuery.Usecase(_productRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnProducts()
    {
        // Arrange
        var products = Seq(
            Product.Create(
                ProductName.Create("Product A").ThrowIfFail(),
                ProductDescription.Create("Desc A").ThrowIfFail(),
                Money.Create(100m).ThrowIfFail(),
                Quantity.Create(10).ThrowIfFail()),
            Product.Create(
                ProductName.Create("Product B").ThrowIfFail(),
                ProductDescription.Create("Desc B").ThrowIfFail(),
                Money.Create(200m).ThrowIfFail(),
                Quantity.Create(20).ThrowIfFail()));

        var request = new GetAllProductsQuery.Request();

        _productRepository.GetAll()
            .Returns(FinTFactory.Succ(products));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(2);
    }
}
