using LanguageExt;
using UnionValueObject.ValueObjects;

// Shape 예제
Shape circle = new Shape.Circle(5.0);
Shape rectangle = new Shape.Rectangle(4.0, 6.0);
Shape triangle = new Shape.Triangle(3.0, 4.0);

Console.WriteLine($"Circle: Area={circle.Area:F2}, Perimeter={circle.Perimeter:F2}");
Console.WriteLine($"Rectangle: Area={rectangle.Area:F2}, Perimeter={rectangle.Perimeter:F2}");
Console.WriteLine($"Triangle: Area={triangle.Area:F2}, Perimeter={triangle.Perimeter:F2}");

// PaymentMethod 예제
PaymentMethod card = new PaymentMethod.CreditCard("1234-5678-9012-3456", "12/25");
PaymentMethod transfer = new PaymentMethod.BankTransfer("110-123-456789", "KB");
PaymentMethod cash = new PaymentMethod.Cash();

decimal orderAmount = 50000m;
Console.WriteLine($"{card.DisplayName}: 수수료 {card.CalculateFee(orderAmount):N0}원");
Console.WriteLine($"{transfer.DisplayName}: 수수료 {transfer.CalculateFee(orderAmount):N0}원");
Console.WriteLine($"{cash.DisplayName}: 수수료 {cash.CalculateFee(orderAmount):N0}원");

// OrderStatus 예제 (Functorium UnionValueObject<TSelf> 상태 전이)
OrderStatus order = new OrderStatus.Pending("ORD-001");
Console.WriteLine($"현재 상태: {order}");

var confirmResult = order.Confirm(DateTime.Now);
confirmResult.Match(
    Succ: confirmed => Console.WriteLine($"전이 성공: {confirmed}"),
    Fail: error => Console.WriteLine($"전이 실패: {error}"));

// 이미 Confirmed 상태에서 다시 Confirm 시도 → 실패
OrderStatus alreadyConfirmed = new OrderStatus.Confirmed("ORD-002", DateTime.Now);
var failResult = alreadyConfirmed.Confirm(DateTime.Now);
failResult.Match(
    Succ: confirmed => Console.WriteLine($"전이 성공: {confirmed}"),
    Fail: error => Console.WriteLine($"전이 실패: {error}"));
