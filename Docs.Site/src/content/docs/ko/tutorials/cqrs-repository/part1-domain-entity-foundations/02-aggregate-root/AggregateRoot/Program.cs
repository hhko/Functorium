using AggregateRoot;

Console.WriteLine("=== Aggregate Root와 상태 전이 ===\n");

// 1. 주문 생성
var order = Order.Create("홍길동", 150_000m);
Console.WriteLine($"주문 생성: {order.Id}");
Console.WriteLine($"  고객: {order.CustomerName}");
Console.WriteLine($"  금액: {order.TotalAmount:N0}원");
Console.WriteLine($"  상태: {order.Status}");
Console.WriteLine();

// 2. 정상적인 상태 전이: Pending → Confirmed → Shipped → Delivered
var confirmResult = order.Confirm();
Console.WriteLine($"확인 결과: {(confirmResult.IsSucc ? "성공" : "실패")} → 상태: {order.Status}");

var shipResult = order.Ship();
Console.WriteLine($"배송 결과: {(shipResult.IsSucc ? "성공" : "실패")} → 상태: {order.Status}");

var deliverResult = order.Deliver();
Console.WriteLine($"배달 결과: {(deliverResult.IsSucc ? "성공" : "실패")} → 상태: {order.Status}");
Console.WriteLine();

// 3. 잘못된 상태 전이 시도
Console.WriteLine("--- 잘못된 상태 전이 시도 ---");
var cancelResult = order.Cancel();
cancelResult.Match(
    Succ: _ => Console.WriteLine("취소 성공"),
    Fail: err => Console.WriteLine($"취소 실패: {err.Message}"));

// 4. 취소 가능한 시나리오
Console.WriteLine();
var order2 = Order.Create("김철수", 50_000m);
order2.Confirm();
var cancelResult2 = order2.Cancel();
Console.WriteLine($"Confirmed 상태에서 취소: {(cancelResult2.IsSucc ? "성공" : "실패")} → 상태: {order2.Status}");
