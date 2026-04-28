using System.Collections.Concurrent;
using CatalogSearch;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using LanguageExt;

Console.WriteLine("=== Chapter 22: Catalog Search ===\n");

// 1. 데이터 준비
var store = new ConcurrentDictionary<ProductId, Product>();
var products = new[]
{
    Product.Create("무선 키보드", "주변기기", 89_000m, 50).ThrowIfFail(),
    Product.Create("게이밍 마우스", "주변기기", 65_000m, 30).ThrowIfFail(),
    Product.Create("노트북 거치대", "액세서리", 45_000m, 0).ThrowIfFail(),   // 품절
    Product.Create("USB 허브", "액세서리", 32_000m, 100).ThrowIfFail(),
    Product.Create("웹캠", "주변기기", 120_000m, 15).ThrowIfFail(),
    Product.Create("모니터 암", "액세서리", 78_000m, 25).ThrowIfFail(),
    Product.Create("기계식 키보드", "주변기기", 150_000m, 20).ThrowIfFail(),
};

foreach (var p in products)
    store[p.Id] = p;

var query = new InMemoryCatalogQuery(store);

// 2. Specification 조합
var inStockAndAffordable = new InStockSpec() & new PriceRangeSpec(30_000m, 100_000m);

// ─── Offset 페이지네이션 ────────────────────────────
Console.WriteLine("--- Offset Pagination (Search) ---");
var offsetResult = await query
    .Search(inStockAndAffordable, new PageRequest(1, 3), SortExpression.By("Price"))
    .Run().RunAsync();
var paged = offsetResult.ThrowIfFail();

Console.WriteLine($"TotalCount={paged.TotalCount}, Page={paged.Page}/{paged.TotalPages}");
foreach (var dto in paged.Items)
    Console.WriteLine($"  {dto.Name}: {dto.Price:N0}원 (재고: {dto.Stock})");

// ─── Cursor 페이지네이션 ────────────────────────────
Console.WriteLine("\n--- Cursor Pagination (SearchByCursor) ---");
var cursorResult = await query
    .SearchByCursor(inStockAndAffordable, new CursorPageRequest(pageSize: 2), SortExpression.By("Price"))
    .Run().RunAsync();
var cursorPage = cursorResult.ThrowIfFail();

Console.WriteLine($"Items={cursorPage.Items.Count}, HasMore={cursorPage.HasMore}");
foreach (var dto in cursorPage.Items)
    Console.WriteLine($"  {dto.Name}: {dto.Price:N0}원");

// ─── Stream (비동기 열거) ───────────────────────────
Console.WriteLine("\n--- Stream (IAsyncEnumerable) ---");
var count = 0;
await foreach (var dto in query.Stream(inStockAndAffordable, SortExpression.By("Name")))
{
    Console.WriteLine($"  [{++count}] {dto.Name}: {dto.Price:N0}원");
}

Console.WriteLine("\nDone.");
