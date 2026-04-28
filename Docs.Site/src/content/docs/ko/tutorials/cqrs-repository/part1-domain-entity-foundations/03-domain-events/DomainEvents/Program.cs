using DomainEvents;

Console.WriteLine("=== Domain Events ===\n");

// 1. 주문 생성 시 이벤트 발행
var order = Order.Create("홍길동", 150_000m);
Console.WriteLine($"주문 생성: {order.Id}");
Console.WriteLine($"  발행된 이벤트 수: {order.DomainEvents.Count}");

foreach (var evt in order.DomainEvents)
{
    Console.WriteLine($"  - {evt.GetType().Name} (EventId: {evt.EventId}, OccurredAt: {evt.OccurredAt:HH:mm:ss.fff})");
}
Console.WriteLine();

// 2. 주문 확인 시 이벤트 추가 발행
order.Confirm();
Console.WriteLine($"주문 확인 후 이벤트 수: {order.DomainEvents.Count}");

foreach (var evt in order.DomainEvents)
{
    Console.WriteLine($"  - {evt.GetType().Name} (EventId: {evt.EventId})");
}
Console.WriteLine();

// 3. 이벤트 정리 (인프라에서 발행 후 호출)
order.ClearDomainEvents();
Console.WriteLine($"ClearDomainEvents() 후 이벤트 수: {order.DomainEvents.Count}");
