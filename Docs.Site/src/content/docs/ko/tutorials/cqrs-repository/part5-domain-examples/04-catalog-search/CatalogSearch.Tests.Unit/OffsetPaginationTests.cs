using System.Collections.Concurrent;
using CatalogSearch;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LanguageExt;

namespace CatalogSearch.Tests.Unit;

public sealed class OffsetPaginationTests
{
    private readonly ConcurrentDictionary<ProductId, Product> _store = new();
    private readonly InMemoryCatalogQuery _query;

    public OffsetPaginationTests()
    {
        _query = new InMemoryCatalogQuery(_store);
        SeedData();
    }

    private void SeedData()
    {
        var products = new[]
        {
            Product.Create("A-Keyboard", "Peripherals", 89_000m, 50).ThrowIfFail(),
            Product.Create("B-Mouse", "Peripherals", 65_000m, 30).ThrowIfFail(),
            Product.Create("C-Stand", "Accessories", 45_000m, 0).ThrowIfFail(),
            Product.Create("D-Hub", "Accessories", 32_000m, 100).ThrowIfFail(),
            Product.Create("E-Webcam", "Peripherals", 120_000m, 15).ThrowIfFail(),
        };
        foreach (var p in products) _store[p.Id] = p;
    }

    [Fact]
    public async Task Search_All_ReturnsTotalCount()
    {
        var result = await _query
            .Search(Specification<Product>.All, new PageRequest(1, 10), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(5);
    }

    [Fact]
    public async Task Search_Page1Of2_ReturnsCorrectItems()
    {
        var result = await _query
            .Search(Specification<Product>.All, new PageRequest(1, 3), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.Items.Count.ShouldBe(3);
        paged.HasNextPage.ShouldBeTrue();
        paged.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Search_Page2Of2_ReturnsRemainingItems()
    {
        var result = await _query
            .Search(Specification<Product>.All, new PageRequest(2, 3), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.Items.Count.ShouldBe(2);
        paged.HasNextPage.ShouldBeFalse();
        paged.HasPreviousPage.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_InStockSpec_ExcludesOutOfStock()
    {
        var result = await _query
            .Search(new InStockSpec(), new PageRequest(1, 10), SortExpression.By("Name"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(4); // C-Stand (stock=0) excluded
        paged.Items.ShouldAllBe(dto => dto.Stock > 0);
    }

    [Fact]
    public async Task Search_ComposedSpec_FiltersCorrectly()
    {
        var spec = new InStockSpec() & new PriceRangeSpec(50_000m, 100_000m);

        var result = await _query
            .Search(spec, new PageRequest(1, 10), SortExpression.By("Price"))
            .Run().RunAsync();

        var paged = result.ThrowIfFail();
        paged.Items.ShouldAllBe(dto => dto.Stock > 0 && dto.Price >= 50_000m && dto.Price <= 100_000m);
    }

    [Fact]
    public async Task Search_SortByPriceDescending_OrdersCorrectly()
    {
        var result = await _query
            .Search(
                Specification<Product>.All,
                new PageRequest(1, 10),
                SortExpression.By("Price", Functorium.Applications.Queries.SortDirection.Descending))
            .Run().RunAsync();

        var items = result.ThrowIfFail().Items;
        items[0].Price.ShouldBe(120_000m);
        items[^1].Price.ShouldBe(32_000m);
    }
}
