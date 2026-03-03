using System.Collections.Concurrent;
using CatalogSearch;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LanguageExt;

namespace CatalogSearch.Tests.Unit;

public sealed class CursorPaginationTests
{
    private readonly ConcurrentDictionary<ProductId, Product> _store = new();
    private readonly InMemoryCatalogQuery _query;

    public CursorPaginationTests()
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
            Product.Create("C-Stand", "Accessories", 45_000m, 10).ThrowIfFail(),
            Product.Create("D-Hub", "Accessories", 32_000m, 100).ThrowIfFail(),
            Product.Create("E-Webcam", "Peripherals", 120_000m, 15).ThrowIfFail(),
        };
        foreach (var p in products) _store[p.Id] = p;
    }

    [Fact]
    public async Task SearchByCursor_FirstPage_ReturnsItems()
    {
        var result = await _query
            .SearchByCursor(
                Specification<Product>.All,
                new CursorPageRequest(pageSize: 2),
                SortExpression.By("Name"))
            .Run().RunAsync();

        var page = result.ThrowIfFail();
        page.Items.Count.ShouldBe(2);
        page.HasMore.ShouldBeTrue();
        page.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchByCursor_PaginateThroughAll_NoDuplicates()
    {
        var allItems = new List<ProductDto>();
        string? cursor = null;

        while (true)
        {
            var result = await _query
                .SearchByCursor(
                    Specification<Product>.All,
                    new CursorPageRequest(after: cursor, pageSize: 2),
                    SortExpression.By("Name"))
                .Run().RunAsync();

            var page = result.ThrowIfFail();
            allItems.AddRange(page.Items);

            if (!page.HasMore) break;
            cursor = page.NextCursor;
        }

        allItems.Count.ShouldBe(5);
        allItems.Select(x => x.Name).Distinct().Count().ShouldBe(5);
    }

    [Fact]
    public async Task SearchByCursor_WithSpec_FiltersCorrectly()
    {
        var spec = new PriceRangeSpec(30_000m, 90_000m);

        var result = await _query
            .SearchByCursor(spec, new CursorPageRequest(pageSize: 10), SortExpression.By("Name"))
            .Run().RunAsync();

        var page = result.ThrowIfFail();
        page.Items.ShouldAllBe(dto => dto.Price >= 30_000m && dto.Price <= 90_000m);
    }

    [Fact]
    public async Task SearchByCursor_EmptyResult_HasMoreIsFalse()
    {
        var spec = new PriceRangeSpec(999_999m, 1_000_000m);

        var result = await _query
            .SearchByCursor(spec, new CursorPageRequest(pageSize: 10), SortExpression.By("Name"))
            .Run().RunAsync();

        var page = result.ThrowIfFail();
        page.Items.Count.ShouldBe(0);
        page.HasMore.ShouldBeFalse();
    }
}
