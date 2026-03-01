using DapperQueryAdapter;

namespace DapperQueryAdapter.Tests.Unit;

public sealed class SqlQueryBuilderTests
{
    // --- BuildSelectWithPagination Tests ---

    [Fact]
    public void BuildSelectWithPagination_T1_WithWhere_T2_ShouldIncludeWhereAndLimitOffset_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithPagination(
            "products", "category = @Category", "name ASC", page: 2, pageSize: 10);

        // Assert
        sql.ShouldContain("SELECT * FROM products");
        sql.ShouldContain("WHERE category = @Category");
        sql.ShouldContain("ORDER BY name ASC");
        sql.ShouldContain("LIMIT 10 OFFSET 10");
    }

    [Fact]
    public void BuildSelectWithPagination_T1_WithoutWhere_T2_ShouldOmitWhereClause_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithPagination(
            "products", null, "name ASC", page: 1, pageSize: 20);

        // Assert
        sql.ShouldNotContain("WHERE");
        sql.ShouldContain("LIMIT 20 OFFSET 0");
    }

    [Fact]
    public void BuildSelectWithPagination_T1_FirstPage_T2_ShouldHaveZeroOffset_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithPagination(
            "products", null, "id ASC", page: 1, pageSize: 5);

        // Assert
        sql.ShouldContain("OFFSET 0");
    }

    [Fact]
    public void BuildSelectWithPagination_T1_ThirdPage_T2_ShouldCalculateCorrectOffset_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithPagination(
            "products", null, "id ASC", page: 3, pageSize: 10);

        // Assert
        sql.ShouldContain("OFFSET 20");
    }

    // --- BuildSelectWithCursor Tests ---

    [Fact]
    public void BuildSelectWithCursor_T1_WithCursor_T2_ShouldIncludeCursorCondition_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithCursor(
            "products", null, "id", "abc-123", pageSize: 10);

        // Assert
        sql.ShouldContain("WHERE id > @CursorValue");
        sql.ShouldContain("ORDER BY id");
        sql.ShouldContain("LIMIT 10");
    }

    [Fact]
    public void BuildSelectWithCursor_T1_WithWhereAndCursor_T2_ShouldCombineConditions_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithCursor(
            "products", "stock > 0", "id", "abc-123", pageSize: 10);

        // Assert
        sql.ShouldContain("WHERE stock > 0 AND id > @CursorValue");
    }

    [Fact]
    public void BuildSelectWithCursor_T1_WithoutCursor_T2_ShouldOmitCursorCondition_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithCursor(
            "products", null, "id", null, pageSize: 10);

        // Assert
        sql.ShouldNotContain("WHERE");
        sql.ShouldContain("ORDER BY id LIMIT 10");
    }

    [Fact]
    public void BuildSelectWithCursor_T1_WithWhereOnly_T2_ShouldIncludeWhereWithoutCursor_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildSelectWithCursor(
            "products", "category = @Category", "id", null, pageSize: 5);

        // Assert
        sql.ShouldContain("WHERE category = @Category");
        sql.ShouldNotContain("@CursorValue");
    }

    // --- BuildCount Tests ---

    [Fact]
    public void BuildCount_T1_WithWhere_T2_ShouldIncludeWhereClause_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildCount("products", "stock > 0");

        // Assert
        sql.ShouldBe("SELECT COUNT(*) FROM products WHERE stock > 0");
    }

    [Fact]
    public void BuildCount_T1_WithoutWhere_T2_ShouldCountAll_T3()
    {
        // Act
        var sql = SqlQueryBuilder.BuildCount("products", null);

        // Assert
        sql.ShouldBe("SELECT COUNT(*) FROM products");
    }

    // --- BuildOrderBy Tests ---

    [Fact]
    public void BuildOrderBy_T1_MappedColumn_T2_ShouldUseAllowedColumn_T3()
    {
        // Arrange
        var allowed = new Dictionary<string, string>
        {
            ["Name"] = "p.name",
            ["Price"] = "p.price"
        };

        // Act
        var orderBy = SqlQueryBuilder.BuildOrderBy("Price", "desc", allowed);

        // Assert
        orderBy.ShouldBe("p.price DESC");
    }

    [Fact]
    public void BuildOrderBy_T1_UnmappedColumn_T2_ShouldFallbackToFieldName_T3()
    {
        // Arrange
        var allowed = new Dictionary<string, string>
        {
            ["Name"] = "p.name"
        };

        // Act
        var orderBy = SqlQueryBuilder.BuildOrderBy("Unknown", "asc", allowed);

        // Assert
        orderBy.ShouldBe("Unknown ASC");
    }
}
