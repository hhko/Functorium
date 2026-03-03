// ---------------------------------------------------------
// Chapter 6: InMemory Repository
// ---------------------------------------------------------
// InMemoryRepositoryBase<TAggregate, TId>는 ConcurrentDictionary
// 기반의 IRepository 구현체입니다.
//
// 서브클래스는 Store 프로퍼티만 제공하면 8개 CRUD가 동작합니다.
// IDomainEventCollector를 통해 도메인 이벤트를 수집합니다.
//
// FinT<IO, T> 결과를 실행하려면:
//   var result = await operation.Run().RunAsync();
//   result.IsSucc / result.IsFail
// ---------------------------------------------------------

using Functorium.Applications.Events;
using Functorium.Domains.Events;
using InMemoryRepository;
using LanguageExt;

Console.WriteLine("=== Chapter 6: InMemory Repository ===");
Console.WriteLine();

// NoOp 이벤트 수집기 생성
var eventCollector = new NoOpDomainEventCollector();
var repository = new InMemoryProductRepository(eventCollector);

// 1. Create
var product = new Product(ProductId.New(), "Keyboard", 49_900m);
var createResult = await repository.Create(product).Run().RunAsync();
Console.WriteLine($"Create: IsSucc={createResult.IsSucc}");

// 2. GetById
var getResult = await repository.GetById(product.Id).Run().RunAsync();
Console.WriteLine($"GetById: IsSucc={getResult.IsSucc}, Name={getResult.ThrowIfFail().Name}");

// 3. Update
product.UpdatePrice(39_900m);
var updateResult = await repository.Update(product).Run().RunAsync();
Console.WriteLine($"Update: IsSucc={updateResult.IsSucc}, Price={updateResult.ThrowIfFail().Price:N0}");

// 4. Delete
var deleteResult = await repository.Delete(product.Id).Run().RunAsync();
Console.WriteLine($"Delete: IsSucc={deleteResult.IsSucc}, Affected={deleteResult.ThrowIfFail()}");

// 5. GetById after delete (should fail)
var notFoundResult = await repository.GetById(product.Id).Run().RunAsync();
Console.WriteLine($"GetById after delete: IsFail={notFoundResult.IsFail}");

Console.WriteLine();
Console.WriteLine("Done.");

// ---------------------------------------------------------
// NoOp 이벤트 수집기 (데모용)
// ---------------------------------------------------------
internal sealed class NoOpDomainEventCollector : IDomainEventCollector
{
    public void Track(IHasDomainEvents aggregate) { }
    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates) { }
    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates() => [];
}
