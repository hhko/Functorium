using Functorium.Domains.Entities;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace DomainEvents;

public enum OrderStatus
{
    Pending,
    Confirmed
}

/// <summary>
/// 도메인 이벤트를 발행하는 주문 Aggregate Root.
/// 상태 변경 시 AddDomainEvent()로 이벤트를 등록합니다.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    public string CustomerName { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    private Order(OrderId id, string customerName, decimal totalAmount)
    {
        Id = id;
        CustomerName = customerName;
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    public static Order Create(string customerName, decimal totalAmount)
    {
        var order = new Order(OrderId.New(), customerName, totalAmount);
        order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerName, totalAmount));
        return order;
    }

    public Fin<Unit> Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Error.New($"Pending 상태에서만 확인할 수 있습니다. 현재 상태: {Status}");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
        return unit;
    }
}
