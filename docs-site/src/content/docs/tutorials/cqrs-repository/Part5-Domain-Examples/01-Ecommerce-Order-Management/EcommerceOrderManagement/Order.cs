using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace EcommerceOrderManagement;

/// <summary>
/// 주문 Aggregate Root.
/// 주문 생성, 상태 전이, 도메인 이벤트 발행을 담당합니다.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    // ─── Domain Events ──────────────────────────────────
    public sealed record OrderCreatedEvent(OrderId OrderId, string CustomerName) : DomainEvent;
    public sealed record OrderConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record OrderShippedEvent(OrderId OrderId) : DomainEvent;
    public sealed record OrderDeliveredEvent(OrderId OrderId) : DomainEvent;
    public sealed record OrderCancelledEvent(OrderId OrderId) : DomainEvent;

    // ─── Properties ─────────────────────────────────────
    public string CustomerName { get; private set; }
    public List<OrderLine> OrderLines { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    private Order(OrderId id, string customerName, List<OrderLine> orderLines)
    {
        Id = id;
        CustomerName = customerName;
        OrderLines = orderLines;
        TotalAmount = orderLines.Sum(l => l.LineTotal);
        Status = OrderStatus.Pending;
    }

    // ─── Factory ────────────────────────────────────────

    public static Fin<Order> Create(string customerName, List<OrderLine> orderLines)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return Error.New("CustomerName is required.");

        if (orderLines is null || orderLines.Count == 0)
            return Error.New("Order must have at least one order line.");

        var order = new Order(OrderId.New(), customerName, orderLines);
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerName));
        return Fin.Succ(order);
    }

    // ─── State Transitions ──────────────────────────────

    public Fin<Unit> Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Error.New($"Only Pending orders can be confirmed. Current: {Status}");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
        return unit;
    }

    public Fin<Unit> Ship()
    {
        if (Status != OrderStatus.Confirmed)
            return Error.New($"Only Confirmed orders can be shipped. Current: {Status}");

        Status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShippedEvent(Id));
        return unit;
    }

    public Fin<Unit> Deliver()
    {
        if (Status != OrderStatus.Shipped)
            return Error.New($"Only Shipped orders can be delivered. Current: {Status}");

        Status = OrderStatus.Delivered;
        AddDomainEvent(new OrderDeliveredEvent(Id));
        return unit;
    }

    public Fin<Unit> Cancel()
    {
        if (Status == OrderStatus.Delivered)
            return Error.New("Delivered orders cannot be cancelled.");

        if (Status == OrderStatus.Cancelled)
            return Error.New("Order is already cancelled.");

        Status = OrderStatus.Cancelled;
        AddDomainEvent(new OrderCancelledEvent(Id));
        return unit;
    }
}
