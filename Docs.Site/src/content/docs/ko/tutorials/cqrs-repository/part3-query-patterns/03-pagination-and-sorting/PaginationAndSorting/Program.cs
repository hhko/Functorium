using Functorium.Applications.Queries;
using PaginationAndSorting;

// ---------------------------------------------------------------
// Chapter 11: Pagination and Sorting
// ---------------------------------------------------------------
// PageRequest, CursorPageRequest, SortExpression을 사용하여
// 페이지네이션과 정렬을 수행하는 방법을 보여줍니다.
// ---------------------------------------------------------------

Console.WriteLine("=== Chapter 11: Pagination and Sorting ===");
Console.WriteLine();

// 샘플 데이터
var products = new List<ProductDto>
{
    new("1", "Apple", 1_500m, "Fruit"),
    new("2", "Banana", 2_000m, "Fruit"),
    new("3", "Cherry", 8_000m, "Fruit"),
    new("4", "Durian", 25_000m, "Fruit"),
    new("5", "Elderberry", 15_000m, "Fruit"),
};

// 1. Offset 기반 페이지네이션
Console.WriteLine("[Offset Pagination]");
var page1 = new PageRequest(page: 1, pageSize: 2);
var result1 = PaginationDemo.CreatePagedResult(products, page1);
Console.WriteLine($"  Page {result1.Page}/{result1.TotalPages} (Total: {result1.TotalCount})");
Console.WriteLine($"  Items: {string.Join(", ", result1.Items.Select(p => p.Name))}");
Console.WriteLine($"  HasPrev: {result1.HasPreviousPage}, HasNext: {result1.HasNextPage}");
Console.WriteLine();

// 2. Cursor 기반 페이지네이션
Console.WriteLine("[Cursor Pagination]");
var cursor1 = new CursorPageRequest(after: null, pageSize: 2);
var cursorResult1 = PaginationDemo.CreateCursorPagedResult(products, cursor1, p => p.Id);
Console.WriteLine($"  Items: {string.Join(", ", cursorResult1.Items.Select(p => p.Name))}");
Console.WriteLine($"  NextCursor: {cursorResult1.NextCursor}, HasMore: {cursorResult1.HasMore}");
Console.WriteLine();

// 3. SortExpression
Console.WriteLine("[SortExpression]");
var sort = SortExpression.By("Price", SortDirection.Descending);
var sorted = PaginationDemo.ApplySort(
    products, sort,
    fieldName => fieldName switch
    {
        "Price" => p => p.Price,
        "Name" => p => p.Name,
        _ => p => p.Name
    },
    "Name");
Console.WriteLine($"  Sorted by Price DESC: {string.Join(", ", sorted.Select(p => $"{p.Name}({p.Price:N0})"))}");
