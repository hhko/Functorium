using Functorium.Domains.Entities;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace AggregateRoot;

/// <summary>
/// 주문 Aggregate Root.
/// 상태 전이에 대한 비즈니스 불변 규칙을 보호합니다.
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
        return new Order(OrderId.New(), customerName, totalAmount);
    }

    public Fin<Unit> Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Error.New($"Pending 상태에서만 확인할 수 있습니다. 현재 상태: {Status}");

        Status = OrderStatus.Confirmed;
        return unit;
    }

    public Fin<Unit> Ship()
    {
        if (Status != OrderStatus.Confirmed)
            return Error.New($"Confirmed 상태에서만 배송할 수 있습니다. 현재 상태: {Status}");

        Status = OrderStatus.Shipped;
        return unit;
    }

    public Fin<Unit> Deliver()
    {
        if (Status != OrderStatus.Shipped)
            return Error.New($"Shipped 상태에서만 배달 완료할 수 있습니다. 현재 상태: {Status}");

        Status = OrderStatus.Delivered;
        return unit;
    }

    public Fin<Unit> Cancel()
    {
        if (Status == OrderStatus.Delivered)
            return Error.New("배달 완료된 주문은 취소할 수 없습니다.");

        if (Status == OrderStatus.Cancelled)
            return Error.New("이미 취소된 주문입니다.");

        Status = OrderStatus.Cancelled;
        return unit;
    }
}
