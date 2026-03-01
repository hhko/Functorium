using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using InMemoryQueryAdapter;
using LanguageExt;

// ---------------------------------------------------------------
// Chapter 12: InMemory Query Adapter
// ---------------------------------------------------------------
// InMemoryQueryBase를 상속하여 InMemoryProductQuery를 구현합니다.
// GetProjectedItems, SortSelector, DefaultSortField만 오버라이드하면
// Search, SearchByCursor, Stream이 자동으로 동작합니다.
// ---------------------------------------------------------------

Console.WriteLine("=== Chapter 12: InMemory Query Adapter ===");
Console.WriteLine();

// 데이터 준비
var query = new InMemoryProductQuery();
query.Add(new Product(ProductId.New(), "Keyboard", 89_000m, 50, "Electronics"));
query.Add(new Product(ProductId.New(), "Mouse", 35_000m, 100, "Electronics"));
query.Add(new Product(ProductId.New(), "Monitor", 350_000m, 20, "Electronics"));
query.Add(new Product(ProductId.New(), "Notebook", 12_000m, 0, "Stationery"));
query.Add(new Product(ProductId.New(), "Pen", 3_000m, 200, "Stationery"));

// 1. Search (Offset 기반)
Console.WriteLine("[Search - 전체 조회]");
var allSpec = Specification<Product>.All;
var searchResult = await query.Search(allSpec, new PageRequest(1, 3), SortExpression.By("Name"))
    .Run().RunAsync();
var paged = searchResult.ThrowIfFail();
Console.WriteLine($"  Page {paged.Page}/{paged.TotalPages} (Total: {paged.TotalCount})");
foreach (var p in paged.Items)
    Console.WriteLine($"  - {p.Name}: {p.Price:N0}원 (재고: {p.Stock})");
Console.WriteLine();

// 2. Search with Specification (재고 있는 상품만)
Console.WriteLine("[Search - InStockSpec 적용]");
var inStockResult = await query.Search(new InStockSpec(), new PageRequest(1, 10), SortExpression.By("Price"))
    .Run().RunAsync();
var inStock = inStockResult.ThrowIfFail();
Console.WriteLine($"  재고 있는 상품: {inStock.TotalCount}개");
foreach (var p in inStock.Items)
    Console.WriteLine($"  - {p.Name}: {p.Price:N0}원 (재고: {p.Stock})");
Console.WriteLine();

// 3. Stream
Console.WriteLine("[Stream - 전체 스트리밍]");
await foreach (var p in query.Stream(allSpec, SortExpression.By("Price", SortDirection.Descending)))
{
    Console.WriteLine($"  - {p.Name}: {p.Price:N0}원");
}
