namespace DapperQueryAdapter;

/// <summary>
/// Dapper Query Adapter에서 사용하는 SQL 빌딩 개념을 보여주는 유틸리티.
/// DapperQueryBase가 내부적으로 수행하는 SQL 조합 패턴을 단순화하여 학습합니다.
/// </summary>
public static class SqlQueryBuilder
{
    /// <summary>
    /// Offset 기반 페이지네이션 SELECT 쿼리를 생성합니다.
    /// </summary>
    public static string BuildSelectWithPagination(
        string table, string? where, string orderBy, int page, int pageSize)
    {
        var sql = $"SELECT * FROM {table}";
        if (!string.IsNullOrEmpty(where))
            sql += $" WHERE {where}";
        sql += $" ORDER BY {orderBy}";
        sql += $" LIMIT {pageSize} OFFSET {(page - 1) * pageSize}";
        return sql;
    }

    /// <summary>
    /// Cursor(Keyset) 기반 페이지네이션 SELECT 쿼리를 생성합니다.
    /// </summary>
    public static string BuildSelectWithCursor(
        string table, string? where, string cursorColumn, string? cursorValue, int pageSize)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrEmpty(where))
            conditions.Add(where);
        if (!string.IsNullOrEmpty(cursorValue))
            conditions.Add($"{cursorColumn} > @CursorValue");

        var sql = $"SELECT * FROM {table}";
        if (conditions.Count > 0)
            sql += $" WHERE {string.Join(" AND ", conditions)}";
        sql += $" ORDER BY {cursorColumn} LIMIT {pageSize}";
        return sql;
    }

    /// <summary>
    /// COUNT 쿼리를 생성합니다.
    /// </summary>
    public static string BuildCount(string table, string? where)
    {
        var sql = $"SELECT COUNT(*) FROM {table}";
        if (!string.IsNullOrEmpty(where))
            sql += $" WHERE {where}";
        return sql;
    }

    /// <summary>
    /// ORDER BY 절을 생성합니다.
    /// </summary>
    public static string BuildOrderBy(
        string fieldName, string direction, Dictionary<string, string> allowedColumns)
    {
        var column = allowedColumns.GetValueOrDefault(fieldName, fieldName);
        return $"{column} {direction.ToUpperInvariant()}";
    }
}
