namespace DomainLayerRules.Domains;

public sealed class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    private OrderCreatedEvent(Guid orderId) => OrderId = orderId;
    public static OrderCreatedEvent Create(Guid orderId) => new(orderId);
}
