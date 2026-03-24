using Functorium.Applications.Observabilities;
using Functorium.Domains.Events;
using ObservabilityHost.Usecases;

namespace ObservabilityHost.DomainEvents;

/// <summary>
/// 주문 완료 도메인 이벤트.
/// PlaceOrderCommand 성공 후 발행되어 후속 처리(알림, 재고 등)를 트리거합니다.
/// </summary>
public sealed record OrderPlacedEvent(
    [CtxRoot] string CustomerId,
    string OrderId,
    int LineCount,
    decimal TotalAmount,
    string OperatorId) : DomainEvent, IOperatorContext;
