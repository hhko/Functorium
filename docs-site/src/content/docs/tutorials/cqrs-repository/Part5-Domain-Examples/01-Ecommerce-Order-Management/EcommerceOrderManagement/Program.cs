using EcommerceOrderManagement;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;

Console.WriteLine("=== Chapter 19: E-commerce Order Management ===\n");

// 1. Repository 준비
var eventCollector = new NoOpDomainEventCollector();
var repository = new InMemoryOrderRepository(eventCollector);

// 2. 주문 생성
var lines = new List<OrderLine>
{
    OrderLine.Create("노트북", 1, 1_500_000m),
    OrderLine.Create("마우스", 2, 35_000m),
};

var order = Order.Create("홍길동", lines).ThrowIfFail();
Console.WriteLine($"주문 생성: {order.Id}");
Console.WriteLine($"  고객: {order.CustomerName}");
Console.WriteLine($"  총액: {order.TotalAmount:N0}원");
Console.WriteLine($"  상태: {order.Status}");
Console.WriteLine($"  항목 수: {order.OrderLines.Count}");
Console.WriteLine();

// 3. Repository에 저장
var saved = await repository.Create(order).Run().RunAsync();
Console.WriteLine($"저장 완료: IsSucc={saved.IsSucc}");

// 4. 상태 전이: Pending -> Confirmed -> Shipped -> Delivered
order.Confirm().ThrowIfFail();
Console.WriteLine($"확인 완료: {order.Status}");

order.Ship().ThrowIfFail();
Console.WriteLine($"배송 완료: {order.Status}");

order.Deliver().ThrowIfFail();
Console.WriteLine($"배달 완료: {order.Status}");
Console.WriteLine();

// 5. 배달 완료 후 취소 시도 (실패)
var cancelResult = order.Cancel();
Console.WriteLine($"배달 완료 주문 취소 시도: IsFail={cancelResult.IsFail}");
Console.WriteLine();

// 6. 도메인 이벤트 확인
Console.WriteLine($"발생한 도메인 이벤트 ({order.DomainEvents.Count}개):");
foreach (var @event in order.DomainEvents)
{
    Console.WriteLine($"  - {@event.GetType().Name}");
}

Console.WriteLine("\nDone.");

// ---------------------------------------------------------
// NoOp 이벤트 수집기 (데모용)
// ---------------------------------------------------------
internal sealed class NoOpDomainEventCollector : IDomainEventCollector
{
    public void Track(IHasDomainEvents aggregate) { }
    public void TrackRange(IEnumerable<IHasDomainEvents> aggregates) { }
    public IReadOnlyList<IHasDomainEvents> GetTrackedAggregates() => [];
}
