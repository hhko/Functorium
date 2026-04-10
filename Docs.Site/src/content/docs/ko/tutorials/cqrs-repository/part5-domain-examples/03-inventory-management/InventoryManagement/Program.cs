using Functorium.Applications.Events;
using Functorium.Applications.Queries;
using Functorium.Domains.Events;
using InventoryManagement;
using LanguageExt;

Console.WriteLine("=== Chapter 21: Inventory Management ===\n");

// 1. Repository & Query 준비
var eventCollector = new NoOpDomainEventCollector();
var repository = new InMemoryProductRepository(eventCollector);

// 2. 상품 등록
var products = new[]
{
    Product.Create("노트북", 1_500_000m, 10).ThrowIfFail(),
    Product.Create("키보드", 89_000m, 50).ThrowIfFail(),
    Product.Create("마우스", 35_000m, 100).ThrowIfFail(),
    Product.Create("모니터", 450_000m, 20).ThrowIfFail(),
};

foreach (var p in products)
    await repository.Create(p).Run().RunAsync();
Console.WriteLine($"상품 {products.Length}개 등록 완료\n");

// 3. Soft Delete
products[2].Delete().ThrowIfFail(); // 마우스 삭제
await repository.Update(products[2]).Run().RunAsync();
Console.WriteLine($"'{products[2].Name}' 소프트 삭제: IsDeleted={products[2].IsDeleted}");

// 4. ActiveProductSpec으로 활성 상품만 조회
var query = new InMemoryProductQuery(repository.GetStore());
var activeSpec = new ActiveProductSpec();

var result = await query
    .Search(activeSpec, new PageRequest(1, 10), SortExpression.By("Name"))
    .Run().RunAsync();
var paged = result.ThrowIfFail();

Console.WriteLine($"\n활성 상품 ({paged.TotalCount}개):");
foreach (var dto in paged.Items)
    Console.WriteLine($"  - {dto.Name}: {dto.Price:N0}원 (재고: {dto.Stock})");

// 5. Cursor 기반 페이지네이션
Console.WriteLine("\n--- Cursor 페이지네이션 ---");
var cursorResult = await query
    .SearchByCursor(activeSpec, new CursorPageRequest(pageSize: 2), SortExpression.By("Name"))
    .Run().RunAsync();
var cursorPage = cursorResult.ThrowIfFail();

Console.WriteLine($"첫 페이지 ({cursorPage.Items.Count}개, HasMore={cursorPage.HasMore}):");
foreach (var dto in cursorPage.Items)
    Console.WriteLine($"  - {dto.Name}");

if (cursorPage.HasMore && cursorPage.NextCursor is not null)
{
    var nextResult = await query
        .SearchByCursor(activeSpec, new CursorPageRequest(after: cursorPage.NextCursor, pageSize: 2), SortExpression.By("Name"))
        .Run().RunAsync();
    var nextPage = nextResult.ThrowIfFail();
    Console.WriteLine($"다음 페이지 ({nextPage.Items.Count}개):");
    foreach (var dto in nextPage.Items)
        Console.WriteLine($"  - {dto.Name}");
}

// 6. Restore
products[2].Restore().ThrowIfFail();
Console.WriteLine($"\n'{products[2].Name}' 복원: IsDeleted={products[2].IsDeleted}");

Console.WriteLine("\nDone.");

// ---------------------------------------------------------
internal sealed class NoOpDomainEventCollector : IDomainEventCollector
{
    public void Track(IHasDomainEvents aggregate) { }
    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates) { }
    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates() => [];
    public void TrackEvent(IDomainEvent domainEvent) { }
    public IReadOnlyList<IDomainEvent> GetDirectlyTrackedEvents() => [];
}
