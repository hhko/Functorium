using LayeredArch.Domain.AggregateRoots.Customers;

namespace LayeredArch.Domain.AggregateRoots.Orders;

/// <summary>
/// 고객과 관련된 주문 도메인 이벤트 인터페이스.
/// 비-root 인터페이스로 선언되어 ctx.customer_event.{field} 형식의
/// 인터페이스 스코프 로그 필드를 생성합니다.
/// </summary>
public interface ICustomerEvent { CustomerId CustomerId { get; } }
