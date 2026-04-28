using System.Collections.Concurrent;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using InventoryManagement;
using LanguageExt;

namespace InventoryManagement.Tests.Unit;

public sealed class ProductCursorPaginationTests
{
    private readonly ConcurrentDictionary<ProductId, Product> _store = new();
    private readonly InMemoryProductQuery _query;

    public ProductCursorPaginationTests()
    {
        _query = new InMemoryProductQuery(_store);

        var products = new[]
        {
            Product.Create("Apple Watch", 599_000m, 30).ThrowIfFail(),
            Product.Create("Galaxy Buds", 179_000m, 50).ThrowIfFail(),
            Product.Create("iPad Pro", 1_299_000m, 15).ThrowIfFail(),
            Product.Create("MacBook Air", 1_590_000m, 10).ThrowIfFail(),
            Product.Create("Pixel Phone", 999_000m, 25).ThrowIfFail(),
        };

        foreach (var p in products)
            _store[p.Id] = p;
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
    public async Task SearchByCursor_SecondPage_UsesAfterCursor()
    {
        // First page
        var first = await _query
            .SearchByCursor(
                Specification<Product>.All,
                new CursorPageRequest(pageSize: 2),
                SortExpression.By("Name"))
            .Run().RunAsync();
        var firstPage = first.ThrowIfFail();

        // Second page
        var second = await _query
            .SearchByCursor(
                Specification<Product>.All,
                new CursorPageRequest(after: firstPage.NextCursor, pageSize: 2),
                SortExpression.By("Name"))
            .Run().RunAsync();
        var secondPage = second.ThrowIfFail();

        secondPage.Items.Count.ShouldBe(2);
        // Items should not overlap with first page
        secondPage.Items.ShouldAllBe(dto =>
            firstPage.Items.All(f => f.Name != dto.Name));
    }

    [Fact]
    public async Task SearchByCursor_LastPage_HasMoreIsFalse()
    {
        // Fetch all in one page
        var result = await _query
            .SearchByCursor(
                Specification<Product>.All,
                new CursorPageRequest(pageSize: 10),
                SortExpression.By("Name"))
            .Run().RunAsync();

        var page = result.ThrowIfFail();
        page.Items.Count.ShouldBe(5);
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task SearchByCursor_WithActiveSpec_ExcludesDeleted()
    {
        // Delete one product
        var toDelete = _store.Values.First();
        toDelete.Delete();

        var result = await _query
            .SearchByCursor(
                new ActiveProductSpec(),
                new CursorPageRequest(pageSize: 10),
                SortExpression.By("Name"))
            .Run().RunAsync();

        var page = result.ThrowIfFail();
        page.Items.Count.ShouldBe(4);
        page.Items.ShouldAllBe(dto => dto.Name != toDelete.Name);
    }

    [Fact]
    public async Task Stream_ReturnsAllItems()
    {
        var items = new List<ProductDto>();
        await foreach (var dto in _query.Stream(
            Specification<Product>.All, SortExpression.By("Name")))
        {
            items.Add(dto);
        }

        items.Count.ShouldBe(5);
    }
}
