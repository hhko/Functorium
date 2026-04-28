using Functorium.Domains.Entities;

namespace EcommerceOrderManagement;

/// <summary>
/// 주문 항목 자식 Entity.
/// Aggregate Root(Order)를 통해서만 접근합니다.
/// </summary>
public sealed class OrderLine : Entity<OrderLineId>
{
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => Quantity * UnitPrice;

    private OrderLine(OrderLineId id, string productName, int quantity, decimal unitPrice)
    {
        Id = id;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderLine Create(string productName, int quantity, decimal unitPrice)
    {
        return new OrderLine(OrderLineId.New(), productName, quantity, unitPrice);
    }
}
