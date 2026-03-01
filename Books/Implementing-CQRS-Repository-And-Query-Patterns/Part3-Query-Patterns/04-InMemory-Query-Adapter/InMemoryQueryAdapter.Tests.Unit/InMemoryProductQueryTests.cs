using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using InMemoryQueryAdapter;
using LanguageExt;
using SortDirection = Functorium.Applications.Queries.SortDirection;

namespace InMemoryQueryAdapter.Tests.Unit;

public sealed class InMemoryProductQueryTests
{
    private static InMemoryProductQuery CreateQueryWithSampleData()
    {
        var query = new InMemoryProductQuery();
        query.Add(new Product(ProductId.New(), "Apple", 1_500m, 10, "Fruit"));
        query.Add(new Product(ProductId.New(), "Banana", 2_000m, 20, "Fruit"));
        query.Add(new Product(ProductId.New(), "Cherry", 8_000m, 0, "Fruit"));
        query.Add(new Product(ProductId.New(), "Durian", 25_000m, 5, "Fruit"));
        query.Add(new Product(ProductId.New(), "Elderberry", 15_000m, 15, "Fruit"));
        return query;
    }

    // --- Search Tests ---

    [Fact]
    public async Task Search_T1_AllSpec_T2_ShouldReturnAllItems_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            Specification<Product>.All,
            new PageRequest(1, 10),
            SortExpression.By("Name"))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(5);
        paged.Items.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Search_T1_WithPaging_T2_ShouldReturnCorrectPage_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            Specification<Product>.All,
            new PageRequest(1, 2),
            SortExpression.By("Name"))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.Items.Count.ShouldBe(2);
        paged.TotalCount.ShouldBe(5);
        paged.TotalPages.ShouldBe(3);
        paged.HasNextPage.ShouldBeTrue();
        paged.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Search_T1_SecondPage_T2_ShouldReturnNextItems_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            Specification<Product>.All,
            new PageRequest(2, 2),
            SortExpression.By("Name"))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.Items.Count.ShouldBe(2);
        paged.Items[0].Name.ShouldBe("Cherry");
        paged.Items[1].Name.ShouldBe("Durian");
    }

    [Fact]
    public async Task Search_T1_InStockSpec_T2_ShouldFilterOutOfStock_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            new InStockSpec(),
            new PageRequest(1, 10),
            SortExpression.By("Name"))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.TotalCount.ShouldBe(4); // Cherry(Stock=0) 제외
        paged.Items.ShouldAllBe(p => p.Stock > 0);
    }

    [Fact]
    public async Task Search_T1_SortByPriceDesc_T2_ShouldReturnSorted_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();

        // Act
        var result = await query.Search(
            Specification<Product>.All,
            new PageRequest(1, 10),
            SortExpression.By("Price", SortDirection.Descending))
            .Run().RunAsync();

        // Assert
        var paged = result.ThrowIfFail();
        paged.Items[0].Name.ShouldBe("Durian");     // 25,000
        paged.Items[^1].Name.ShouldBe("Apple");     // 1,500
    }

    // --- SearchByCursor Tests ---

    [Fact]
    public async Task SearchByCursor_T1_FirstPage_T2_ShouldReturnItemsWithCursor_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();

        // Act
        var result = await query.SearchByCursor(
            Specification<Product>.All,
            new CursorPageRequest(pageSize: 2),
            SortExpression.By("Name"))
            .Run().RunAsync();

        // Assert
        var cursored = result.ThrowIfFail();
        cursored.Items.Count.ShouldBe(2);
        cursored.HasMore.ShouldBeTrue();
        cursored.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchByCursor_T1_WithAfterCursor_T2_ShouldReturnNextItems_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();
        // 첫 페이지를 조회하여 커서 획득
        var firstResult = await query.SearchByCursor(
            Specification<Product>.All,
            new CursorPageRequest(pageSize: 2),
            SortExpression.By("Name"))
            .Run().RunAsync();
        var nextCursor = firstResult.ThrowIfFail().NextCursor;

        // Act - 커서를 사용하여 다음 페이지 조회
        var result = await query.SearchByCursor(
            Specification<Product>.All,
            new CursorPageRequest(after: nextCursor, pageSize: 2),
            SortExpression.By("Name"))
            .Run().RunAsync();

        // Assert
        var cursored = result.ThrowIfFail();
        cursored.Items.Count.ShouldBe(2);
        // 첫 페이지 이후 항목들이어야 함
        cursored.Items[0].Name.ShouldNotBe("Apple");
        cursored.Items[0].Name.ShouldNotBe("Banana");
    }

    // --- Stream Tests ---

    [Fact]
    public async Task Stream_T1_AllSpec_T2_ShouldReturnAllItems_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();
        var items = new List<ProductDto>();

        // Act
        await foreach (var item in query.Stream(
            Specification<Product>.All, SortExpression.By("Name")))
        {
            items.Add(item);
        }

        // Assert
        items.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Stream_T1_InStockSpec_T2_ShouldFilterAndStream_T3()
    {
        // Arrange
        var query = CreateQueryWithSampleData();
        var items = new List<ProductDto>();

        // Act
        await foreach (var item in query.Stream(new InStockSpec(), SortExpression.Empty))
        {
            items.Add(item);
        }

        // Assert
        items.Count.ShouldBe(4); // Cherry(Stock=0) 제외
        items.ShouldAllBe(p => p.Stock > 0);
    }
}
