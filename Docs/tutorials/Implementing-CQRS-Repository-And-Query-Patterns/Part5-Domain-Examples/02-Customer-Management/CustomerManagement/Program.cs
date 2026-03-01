using CustomerManagement;
using Functorium.Applications.Events;
using Functorium.Applications.Queries;
using Functorium.Domains.Events;
using Functorium.Domains.Specifications;
using LanguageExt;

Console.WriteLine("=== Chapter 20: Customer Management ===\n");

// 1. Repository & Query 준비
var eventCollector = new NoOpDomainEventCollector();
var repository = new InMemoryCustomerRepository(eventCollector);

// 2. 고객 생성
var customers = new[]
{
    Customer.Create("김철수", "kim@example.com", 1_000_000m).ThrowIfFail(),
    Customer.Create("이영희", "lee@example.com", 500_000m).ThrowIfFail(),
    Customer.Create("박민수", "park@example.com", 2_000_000m).ThrowIfFail(),
    Customer.Create("김지영", "kimjy@example.com", 750_000m).ThrowIfFail(),
};

foreach (var customer in customers)
    await repository.Create(customer).Run().RunAsync();

Console.WriteLine($"고객 {customers.Length}명 등록 완료\n");

// 3. Specification으로 이메일 중복 확인
var emailExists = await repository
    .Exists(new CustomerEmailSpec("kim@example.com"))
    .Run().RunAsync();
Console.WriteLine($"kim@example.com 존재 여부: {emailExists.ThrowIfFail()}");

// 4. Query로 이름 검색 (Specification 조합)
var query = new InMemoryCustomerQuery(
    GetStore(repository));

var nameSpec = new CustomerNameSpec("김");
var searchResult = await query
    .Search(nameSpec, new PageRequest(1, 10), SortExpression.By("Name"))
    .Run().RunAsync();
var paged = searchResult.ThrowIfFail();
Console.WriteLine($"\n'김' 검색 결과: {paged.TotalCount}명");
foreach (var dto in paged.Items)
    Console.WriteLine($"  - {dto.Name} ({dto.Email})");

// 5. 동적 필터 빌더
var filter = Specification<Customer>.All;
filter = filter & new CustomerNameSpec("김");

var dynamicResult = await query
    .Search(filter, new PageRequest(1, 10), SortExpression.By("Name"))
    .Run().RunAsync();
Console.WriteLine($"\n동적 필터 결과: {dynamicResult.ThrowIfFail().TotalCount}명");

Console.WriteLine("\nDone.");

// ---------------------------------------------------------
// 헬퍼
// ---------------------------------------------------------
static System.Collections.Concurrent.ConcurrentDictionary<CustomerId, Customer> GetStore(
    InMemoryCustomerRepository repo)
{
    // 테스트/데모용: 리플렉션으로 내부 Store 접근
    var field = typeof(InMemoryCustomerRepository)
        .GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
    return (System.Collections.Concurrent.ConcurrentDictionary<CustomerId, Customer>)field.GetValue(repo)!;
}

internal sealed class NoOpDomainEventCollector : IDomainEventCollector
{
    public void Track(IHasDomainEvents aggregate) { }
    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates) { }
    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates() => [];
}
