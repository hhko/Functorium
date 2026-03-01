using DapperQueryAdapter;

// ---------------------------------------------------------------
// Chapter 13: Dapper Query Adapter
// ---------------------------------------------------------------
// DapperQueryBase는 SQL 기반 Query Adapter의 공통 인프라입니다.
// 실제 DB 없이 SqlQueryBuilder로 SQL 생성 개념을 학습합니다.
// ---------------------------------------------------------------

Console.WriteLine("=== Chapter 13: Dapper Query Adapter ===");
Console.WriteLine();

// 1. Offset 기반 페이지네이션 SQL
Console.WriteLine("[Offset Pagination SQL]");
var offsetSql = SqlQueryBuilder.BuildSelectWithPagination(
    "products", "category = @Category", "name ASC", page: 2, pageSize: 10);
Console.WriteLine($"  {offsetSql}");
Console.WriteLine();

// 2. Cursor 기반 페이지네이션 SQL
Console.WriteLine("[Cursor Pagination SQL]");
var cursorSql = SqlQueryBuilder.BuildSelectWithCursor(
    "products", "category = @Category", "id", "cursor-value", pageSize: 10);
Console.WriteLine($"  {cursorSql}");
Console.WriteLine();

// 3. COUNT SQL
Console.WriteLine("[Count SQL]");
var countSql = SqlQueryBuilder.BuildCount("products", "stock > 0");
Console.WriteLine($"  {countSql}");
Console.WriteLine();

// 4. ORDER BY with AllowedSortColumns
Console.WriteLine("[OrderBy with AllowedSortColumns]");
var allowedColumns = new Dictionary<string, string>
{
    ["Name"] = "p.name",
    ["Price"] = "p.price",
    ["Category"] = "p.category"
};
var orderBy = SqlQueryBuilder.BuildOrderBy("Price", "desc", allowedColumns);
Console.WriteLine($"  ORDER BY {orderBy}");
Console.WriteLine();

// 5. DapperQueryBase 설명
Console.WriteLine("[DapperQueryBase 구조]");
Console.WriteLine("  서브클래스가 구현할 항목:");
Console.WriteLine("  - SelectSql     : SELECT p.id, p.name, ... FROM products p");
Console.WriteLine("  - CountSql      : SELECT COUNT(*) FROM products p");
Console.WriteLine("  - DefaultOrderBy: p.name ASC");
Console.WriteLine("  - AllowedSortColumns: { Name -> p.name, Price -> p.price }");
Console.WriteLine("  - BuildWhereClause: Specification -> SQL WHERE 절");
