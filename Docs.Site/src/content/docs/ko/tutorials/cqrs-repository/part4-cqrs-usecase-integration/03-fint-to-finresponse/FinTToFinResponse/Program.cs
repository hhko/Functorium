using FinTToFinResponse;
using LanguageExt;

Console.WriteLine("=== FinT to FinResponse 합성 패턴 ===\n");

var eventCollector = new NoOpDomainEventCollector();
var repository = new InMemoryProductRepository(eventCollector);

// 패턴 1: 단일 연산
Console.WriteLine("--- 패턴 1: 단일 연산 (from...select) ---");
var createResult = await CompositionExamples.SimpleCreate(repository, "노트북", 1_500_000m);
createResult.Match(
    Succ: r => Console.WriteLine($"생성: {r.Name} ({r.Price:N0}원)"),
    Fail: e => Console.WriteLine($"실패: {e}"));

// 패턴 2: 순차 연산
Console.WriteLine("\n--- 패턴 2: 순차 연산 (from...from...select) ---");
var product = Product.Create("마우스", 25_000m);
await repository.Create(product).Run().RunAsync();
var updateResult = await CompositionExamples.ChainedUpdate(repository, product.Id, 30_000m);
updateResult.Match(
    Succ: r => Console.WriteLine($"가격 변경: {r.OldPrice:N0}원 → {r.NewPrice:N0}원"),
    Fail: e => Console.WriteLine($"실패: {e}"));

// 패턴 3: guard 조건부 중단
Console.WriteLine("\n--- 패턴 3: guard 조건부 중단 ---");
var inactive = Product.Create("단종 상품", 10_000m).Deactivate();
await repository.Create(inactive).Run().RunAsync();
var guardResult = await CompositionExamples.GuardedUpdate(repository, inactive.Id, 15_000m);
guardResult.Match(
    Succ: r => Console.WriteLine($"가격 변경: {r.NewPrice:N0}원"),
    Fail: e => Console.WriteLine($"실패 (예상됨): {e}"));
