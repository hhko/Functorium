using QueryUsecase;

namespace QueryUsecase.Tests.Unit;

public sealed class SearchProductsQueryTests
{
    private readonly List<Product> _products =
    [
        Product.Create("노트북 Pro", 2_000_000m),
        Product.Create("노트북 Air", 1_500_000m),
        Product.Create("마우스", 25_000m),
    ];

    [Fact]
    public async Task Handle_ReturnsMatchingProducts_WhenKeywordMatches()
    {
        // Arrange
        var query = new InMemoryProductQuery(_products);
        var sut = new SearchProductsQuery.Usecase(query);

        // Act
        var result = await sut.Handle(new SearchProductsQuery.Request("노트북"));

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.Products.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoMatch()
    {
        // Arrange
        var query = new InMemoryProductQuery(_products);
        var sut = new SearchProductsQuery.Usecase(query);

        // Act
        var result = await sut.Handle(new SearchProductsQuery.Request("키보드"));

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.Products.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ReturnsAllFields_WhenProductFound()
    {
        // Arrange
        var query = new InMemoryProductQuery(_products);
        var sut = new SearchProductsQuery.Usecase(query);

        // Act
        var result = await sut.Handle(new SearchProductsQuery.Request("마우스"));

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.Products.Count.ShouldBe(1);
        response.Products[0].Name.ShouldBe("마우스");
        response.Products[0].Price.ShouldBe(25_000m);
        response.Products[0].IsActive.ShouldBeTrue();
    }
}
