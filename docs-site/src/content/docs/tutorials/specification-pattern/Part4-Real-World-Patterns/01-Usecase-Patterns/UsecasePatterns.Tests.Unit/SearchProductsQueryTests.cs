using UsecasePatterns;
using UsecasePatterns.Usecases;

namespace UsecasePatterns.Tests.Unit;

[Trait("Part4-UsecasePatterns", "Query")]
public class SearchProductsQueryTests
{
    private static readonly List<Product> SampleProducts =
    [
        new("Laptop", 1500, 10, "Electronics"),
        new("Mouse", 25, 50, "Electronics"),
        new("Desk", 300, 0, "Furniture"),
        new("Chair", 200, 5, "Furniture"),
        new("Keyboard", 75, 30, "Electronics"),
    ];

    private readonly SearchProductsQueryHandler _handler;

    public SearchProductsQueryTests()
    {
        var repository = new InMemoryProductRepository(SampleProducts);
        _handler = new SearchProductsQueryHandler(repository);
    }

    [Fact]
    public void Handle_ShouldReturnAll_WhenNoFiltersApplied()
    {
        // Arrange
        var query = new SearchProductsQuery(null, null, null, null);

        // Act
        var results = _handler.Handle(query).ToList();

        // Assert
        results.Count.ShouldBe(5);
    }

    [Fact]
    public void Handle_ShouldFilterByCategory_WhenCategoryProvided()
    {
        // Arrange
        var query = new SearchProductsQuery("Electronics", null, null, null);

        // Act
        var results = _handler.Handle(query).ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldAllBe(p => p.Category == "Electronics");
    }

    [Fact]
    public void Handle_ShouldFilterByPriceRange_WhenPriceRangeProvided()
    {
        // Arrange
        var query = new SearchProductsQuery(null, 50, 500, null);

        // Act
        var results = _handler.Handle(query).ToList();

        // Assert
        results.ShouldAllBe(p => p.Price >= 50 && p.Price <= 500);
    }

    [Fact]
    public void Handle_ShouldFilterByInStock_WhenInStockOnlyIsTrue()
    {
        // Arrange
        var query = new SearchProductsQuery(null, null, null, true);

        // Act
        var results = _handler.Handle(query).ToList();

        // Assert
        results.ShouldAllBe(p => p.Stock > 0);
        results.ShouldNotContain(p => p.Name == "Desk");
    }

    [Fact]
    public void Handle_ShouldCombineFilters_WhenMultipleFiltersProvided()
    {
        // Arrange
        var query = new SearchProductsQuery("Electronics", 50, 500, true);

        // Act
        var results = _handler.Handle(query).ToList();

        // Assert
        results.ShouldAllBe(p =>
            p.Category == "Electronics" &&
            p.Price >= 50 && p.Price <= 500 &&
            p.Stock > 0);
    }
}
