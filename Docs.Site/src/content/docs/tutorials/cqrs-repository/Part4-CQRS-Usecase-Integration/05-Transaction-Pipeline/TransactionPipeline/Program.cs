using TransactionPipeline;

Console.WriteLine("=== Transaction Pipeline ===\n");

var unitOfWork = new InMemoryUnitOfWork();
var eventCollector = new SimpleDomainEventCollector();

// 시나리오 1: 성공적인 Command Pipeline
Console.WriteLine("--- 시나리오 1: 성공 ---");
var product = Product.Create("노트북", 1_500_000m);
eventCollector.Track(product);

var result = await TransactionDemo.SimulateCommandPipeline(
    unitOfWork, eventCollector,
    handler: () => Task.FromResult(true));

Console.WriteLine($"Handler 성공: {result.HandlerSucceeded}");
Console.WriteLine($"SaveChanges 완료: {result.SavedChanges}");
Console.WriteLine($"트랜잭션 커밋: {result.Committed}");
Console.WriteLine($"발행된 이벤트: {result.PublishedEvents.Count}개");

// 시나리오 2: Handler 실패 → 트랜잭션 롤백
Console.WriteLine("\n--- 시나리오 2: Handler 실패 ---");
unitOfWork.Reset();

var failResult = await TransactionDemo.SimulateCommandPipeline(
    unitOfWork, eventCollector,
    handler: () => Task.FromResult(false));

Console.WriteLine($"Handler 성공: {failResult.HandlerSucceeded}");
Console.WriteLine($"SaveChanges 완료: {failResult.SavedChanges}");
Console.WriteLine($"트랜잭션 커밋: {failResult.Committed}");
Console.WriteLine($"발행된 이벤트: {failResult.PublishedEvents.Count}개");
