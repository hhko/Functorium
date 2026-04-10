using Functorium.Domains.Entities;

namespace InMemoryQueryAdapter;

public sealed class Order : AggregateRoot<OrderId>
{
    public ProductId ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalAmount { get; private set; }

    private Order() : base() { ProductId = default; }

    public Order(OrderId id, ProductId productId, int quantity, decimal totalAmount) : base(id)
    {
        ProductId = productId;
        Quantity = quantity;
        TotalAmount = totalAmount;
    }

    public static Order Create(ProductId productId, int quantity, decimal unitPrice)
        => new(OrderId.New(), productId, quantity, quantity * unitPrice);
}
