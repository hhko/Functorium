using Functorium.Applications.Queries;
using PaginationAndSorting;
using SortDirection = Functorium.Applications.Queries.SortDirection;

namespace PaginationAndSorting.Tests.Unit;

public sealed class PaginationTests
{
    private static readonly List<ProductDto> SampleProducts =
    [
        new("1", "Apple", 1_500m, "Fruit"),
        new("2", "Banana", 2_000m, "Fruit"),
        new("3", "Cherry", 8_000m, "Fruit"),
        new("4", "Durian", 25_000m, "Fruit"),
        new("5", "Elderberry", 15_000m, "Fruit"),
    ];

    // --- PagedResult Tests ---

    [Fact]
    public void PagedResult_T1_FirstPage_T2_ShouldCalculateProperties_T3()
    {
        // Arrange
        var page = new PageRequest(page: 1, pageSize: 2);

        // Act
        var result = PaginationDemo.CreatePagedResult(SampleProducts, page);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(5);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(2);
        result.TotalPages.ShouldBe(3);
        result.HasPreviousPage.ShouldBeFalse();
        result.HasNextPage.ShouldBeTrue();
    }

    [Fact]
    public void PagedResult_T1_LastPage_T2_ShouldHaveNoPreviousAndNoNext_T3()
    {
        // Arrange
        var page = new PageRequest(page: 3, pageSize: 2);

        // Act
        var result = PaginationDemo.CreatePagedResult(SampleProducts, page);

        // Assert
        result.Items.Count.ShouldBe(1); // 마지막 페이지에 1개
        result.HasPreviousPage.ShouldBeTrue();
        result.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void PagedResult_T1_MiddlePage_T2_ShouldHaveBothNavigation_T3()
    {
        // Arrange
        var page = new PageRequest(page: 2, pageSize: 2);

        // Act
        var result = PaginationDemo.CreatePagedResult(SampleProducts, page);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.HasPreviousPage.ShouldBeTrue();
        result.HasNextPage.ShouldBeTrue();
    }

    // --- PageRequest Tests ---

    [Fact]
    public void PageRequest_T1_DefaultValues_T2_ShouldUseDefaults_T3()
    {
        // Act
        var page = new PageRequest();

        // Assert
        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(PageRequest.DefaultPageSize);
        page.Skip.ShouldBe(0);
    }

    [Fact]
    public void PageRequest_T1_NegativePage_T2_ShouldClampToOne_T3()
    {
        // Act
        var page = new PageRequest(page: -1, pageSize: 10);

        // Assert
        page.Page.ShouldBe(1);
    }

    [Fact]
    public void PageRequest_T1_ExcessivePageSize_T2_ShouldClampToMax_T3()
    {
        // Act
        var page = new PageRequest(page: 1, pageSize: 99_999);

        // Assert
        page.PageSize.ShouldBe(PageRequest.MaxPageSize);
    }

    // --- CursorPagedResult Tests ---

    [Fact]
    public void CursorPagedResult_T1_FirstPage_T2_ShouldReturnItemsWithCursor_T3()
    {
        // Arrange
        var cursor = new CursorPageRequest(after: null, pageSize: 2);

        // Act
        var result = PaginationDemo.CreateCursorPagedResult(
            SampleProducts, cursor, p => p.Id);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.HasMore.ShouldBeTrue();
        result.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public void CursorPagedResult_T1_AfterCursor_T2_ShouldReturnNextItems_T3()
    {
        // Arrange - 첫 페이지 후 커서로 두 번째 페이지 조회
        var cursor = new CursorPageRequest(after: "2", pageSize: 2);

        // Act
        var result = PaginationDemo.CreateCursorPagedResult(
            SampleProducts, cursor, p => p.Id);

        // Assert
        result.Items.Count.ShouldBe(2);
        result.Items[0].Name.ShouldBe("Cherry");
        result.Items[1].Name.ShouldBe("Durian");
        result.HasMore.ShouldBeTrue();
    }

    [Fact]
    public void CursorPagedResult_T1_LastPage_T2_ShouldHaveNoMore_T3()
    {
        // Arrange
        var cursor = new CursorPageRequest(after: "4", pageSize: 2);

        // Act
        var result = PaginationDemo.CreateCursorPagedResult(
            SampleProducts, cursor, p => p.Id);

        // Assert
        result.Items.Count.ShouldBe(1);
        result.HasMore.ShouldBeFalse();
        result.NextCursor.ShouldBeNull();
    }

    // --- SortExpression Tests ---

    [Fact]
    public void SortExpression_T1_Empty_T2_ShouldBeEmpty_T3()
    {
        // Act
        var sort = SortExpression.Empty;

        // Assert
        sort.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void SortExpression_T1_SingleField_T2_ShouldHaveOneField_T3()
    {
        // Act
        var sort = SortExpression.By("Name");

        // Assert
        sort.IsEmpty.ShouldBeFalse();
        sort.Fields.Count.ShouldBe(1);
        sort.Fields[0].FieldName.ShouldBe("Name");
        sort.Fields[0].Direction.ShouldBe(SortDirection.Ascending);
    }

    [Fact]
    public void SortExpression_T1_MultipleFields_T2_ShouldChainFields_T3()
    {
        // Act
        var sort = SortExpression
            .By("Category")
            .ThenBy("Price", SortDirection.Descending);

        // Assert
        sort.Fields.Count.ShouldBe(2);
        sort.Fields[0].FieldName.ShouldBe("Category");
        sort.Fields[0].Direction.ShouldBe(SortDirection.Ascending);
        sort.Fields[1].FieldName.ShouldBe("Price");
        sort.Fields[1].Direction.ShouldBe(SortDirection.Descending);
    }

    [Fact]
    public void ApplySort_T1_DescendingPrice_T2_ShouldSortCorrectly_T3()
    {
        // Arrange
        var sort = SortExpression.By("Price", SortDirection.Descending);

        // Act
        var sorted = PaginationDemo.ApplySort(
            SampleProducts, sort,
            field => field switch
            {
                "Price" => p => p.Price,
                _ => p => p.Name
            },
            "Name");

        // Assert
        sorted[0].Name.ShouldBe("Durian");     // 25,000
        sorted[1].Name.ShouldBe("Elderberry");  // 15,000
        sorted[2].Name.ShouldBe("Cherry");       // 8,000
        sorted[3].Name.ShouldBe("Banana");       // 2,000
        sorted[4].Name.ShouldBe("Apple");        // 1,500
    }
}
