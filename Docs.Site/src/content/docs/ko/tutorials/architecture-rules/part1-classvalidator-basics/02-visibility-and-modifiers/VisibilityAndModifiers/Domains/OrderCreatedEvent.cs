namespace VisibilityAndModifiers.Domains;

public sealed class OrderCreatedEvent : DomainEvent
{
    public string OrderNo { get; }

    private OrderCreatedEvent(string orderNo) => OrderNo = orderNo;

    public static OrderCreatedEvent Create(string orderNo) => new(orderNo);
}
