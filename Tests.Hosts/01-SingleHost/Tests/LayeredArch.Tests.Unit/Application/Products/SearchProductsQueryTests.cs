using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Application.Products;

public class SearchProductsQueryTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly SearchProductsQuery.Usecase _sut;

    public SearchProductsQueryTests()
    {
        _sut = new SearchProductsQuery.Usecase(_productRepository);
    }

    private static Seq<Product> CreateSampleProducts()
    {
        return Seq(
            Product.Create(
                ProductName.Create("Cheap Item").ThrowIfFail(),
                ProductDescription.Create("Desc").ThrowIfFail(),
                Money.Create(50m).ThrowIfFail(),
                Quantity.Create(100).ThrowIfFail()),
            Product.Create(
                ProductName.Create("Mid Item").ThrowIfFail(),
                ProductDescription.Create("Desc").ThrowIfFail(),
                Money.Create(150m).ThrowIfFail(),
                Quantity.Create(3).ThrowIfFail()),
            Product.Create(
                ProductName.Create("Expensive Item").ThrowIfFail(),
                ProductDescription.Create("Desc").ThrowIfFail(),
                Money.Create(500m).ThrowIfFail(),
                Quantity.Create(2).ThrowIfFail()));
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenNoFiltersProvided()
    {
        // Arrange
        var products = CreateSampleProducts();
        var request = new SearchProductsQuery.Request(null, null, null);

        _productRepository.FindAll(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(products));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPriceRangeProvided()
    {
        // Arrange
        var products = CreateSampleProducts();
        var matchingProducts = products.Where(p => p.Price >= (Money)Money.Create(100m).ThrowIfFail()
                                                && p.Price <= (Money)Money.Create(200m).ThrowIfFail()).ToSeq();
        var request = new SearchProductsQuery.Request(100m, 200m, null);

        _productRepository.FindAll(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(matchingProducts));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(1);
        actual.ThrowIfFail().Products[0].Name.ShouldBe("Mid Item");
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenLowStockThresholdProvided()
    {
        // Arrange
        var products = CreateSampleProducts();
        var matchingProducts = products.Where(p => p.StockQuantity < (Quantity)Quantity.Create(5).ThrowIfFail()).ToSeq();
        var request = new SearchProductsQuery.Request(null, null, 5);

        _productRepository.FindAll(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(matchingProducts));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenMultipleFiltersProvided()
    {
        // Arrange
        var products = CreateSampleProducts();
        var matchingProducts = products
            .Where(p => p.Price >= (Money)Money.Create(100m).ThrowIfFail()
                     && p.Price <= (Money)Money.Create(200m).ThrowIfFail()
                     && p.StockQuantity < (Quantity)Quantity.Create(5).ThrowIfFail())
            .ToSeq();
        var request = new SearchProductsQuery.Request(100m, 200m, 5);

        _productRepository.FindAll(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(matchingProducts));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.ThrowIfFail().Products.Count.ShouldBe(1);
        actual.ThrowIfFail().Products[0].Name.ShouldBe("Mid Item");
    }
}
