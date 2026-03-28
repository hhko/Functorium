using Functorium.Applications.Queries;
using QueryUsecase;

namespace QueryUsecase.Tests.Unit;

public sealed class SearchProductsQueryTests
{
    private readonly InMemoryProductQuery _query;

    public SearchProductsQueryTests()
    {
        _query = new InMemoryProductQuery();
        _query.Add(Product.Create("노트북 Pro", 2_000_000m));
        _query.Add(Product.Create("노트북 Air", 1_500_000m));
        _query.Add(Product.Create("마우스", 25_000m));
    }

    [Fact]
    public async Task Handle_ReturnsMatchingProducts_WhenKeywordMatches()
    {
        // Arrange
        var sut = new SearchProductsQuery.Usecase(_query);
        var request = new SearchProductsQuery.Request("노트북", new PageRequest(1, 10), SortExpression.Empty);

        // Act
        var result = await sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.Products.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoMatch()
    {
        // Arrange
        var sut = new SearchProductsQuery.Usecase(_query);
        var request = new SearchProductsQuery.Request("키보드", new PageRequest(1, 10), SortExpression.Empty);

        // Act
        var result = await sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.Products.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ReturnsAllFields_WhenProductFound()
    {
        // Arrange
        var sut = new SearchProductsQuery.Usecase(_query);
        var request = new SearchProductsQuery.Request("마우스", new PageRequest(1, 10), SortExpression.Empty);

        // Act
        var result = await sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        var response = result.ThrowIfFail();
        response.Products.TotalCount.ShouldBe(1);
        response.Products.Items[0].Name.ShouldBe("마우스");
        response.Products.Items[0].Price.ShouldBe(25_000m);
        response.Products.Items[0].IsActive.ShouldBeTrue();
    }
}
