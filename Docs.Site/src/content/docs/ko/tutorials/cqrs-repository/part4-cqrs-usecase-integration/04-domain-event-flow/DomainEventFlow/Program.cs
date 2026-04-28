using DomainEventFlow;

Console.WriteLine("=== Domain Event Flow ===\n");

// 1. Aggregate 생성 시 이벤트 발생
var product = Product.Create("노트북", 1_500_000m);
Console.WriteLine($"상품 생성: {product.Name}");
Console.WriteLine($"발생한 이벤트 수: {product.DomainEvents.Count}");

foreach (var e in product.DomainEvents)
    Console.WriteLine($"  - {e.GetType().Name} (EventId: {e.EventId})");

// 2. Collector가 Aggregate를 추적
var collector = new SimpleDomainEventCollector();
collector.Track(product);
Console.WriteLine($"\n추적 중인 Aggregate (이벤트 있음): {collector.GetTrackedAggregates().Count}개");

// 3. 가격 변경 시 추가 이벤트 발생
product.UpdatePrice(1_300_000m);
Console.WriteLine($"\n가격 변경 후 이벤트 수: {product.DomainEvents.Count}");
foreach (var e in product.DomainEvents)
    Console.WriteLine($"  - {e.GetType().Name}");

// 4. 이벤트 발행 후 ClearDomainEvents
Console.WriteLine($"\n이벤트 발행 후 정리...");
product.ClearDomainEvents();
Console.WriteLine($"남은 이벤트 수: {product.DomainEvents.Count}");
Console.WriteLine($"추적 중인 Aggregate (이벤트 있음): {collector.GetTrackedAggregates().Count}개");
