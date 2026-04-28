using System.Collections.Concurrent;
using CatalogSearch;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LanguageExt;

namespace CatalogSearch.Tests.Unit;

public sealed class StreamTests
{
    private readonly ConcurrentDictionary<ProductId, Product> _store = new();
    private readonly InMemoryCatalogQuery _query;

    public StreamTests()
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
    public async Task Stream_All_ReturnsAllItems()
    {
        var items = new List<ProductDto>();
        await foreach (var dto in _query.Stream(
            Specification<Product>.All, SortExpression.By("Name")))
        {
            items.Add(dto);
        }

        items.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Stream_WithSpec_FiltersCorrectly()
    {
        var items = new List<ProductDto>();
        await foreach (var dto in _query.Stream(
            new InStockSpec(), SortExpression.By("Name")))
        {
            items.Add(dto);
        }

        items.Count.ShouldBe(4); // C-Stand (stock=0) excluded
        items.ShouldAllBe(dto => dto.Stock > 0);
    }

    [Fact]
    public async Task Stream_ComposedSpec_WorksCorrectly()
    {
        var spec = new InStockSpec() & new PriceRangeSpec(50_000m, 100_000m);
        var items = new List<ProductDto>();

        await foreach (var dto in _query.Stream(spec, SortExpression.By("Price")))
        {
            items.Add(dto);
        }

        items.ShouldAllBe(dto => dto.Stock > 0 && dto.Price >= 50_000m && dto.Price <= 100_000m);
    }

    [Fact]
    public async Task Stream_SortedByName_IsOrdered()
    {
        var items = new List<ProductDto>();
        await foreach (var dto in _query.Stream(
            Specification<Product>.All, SortExpression.By("Name")))
        {
            items.Add(dto);
        }

        for (int i = 1; i < items.Count; i++)
        {
            string.Compare(items[i - 1].Name, items[i].Name, StringComparison.Ordinal)
                .ShouldBeLessThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task Stream_Cancellation_StopsEnumeration()
    {
        using var cts = new CancellationTokenSource();
        var items = new List<ProductDto>();

        try
        {
            await foreach (var dto in _query.Stream(
                Specification<Product>.All, SortExpression.By("Name"), cts.Token))
            {
                items.Add(dto);
                if (items.Count == 2) cts.Cancel();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected: cancellation throws on next MoveNext()
        }

        items.Count.ShouldBe(2);
    }
}
