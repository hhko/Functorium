namespace DomainLayerRules.Domains;

public sealed record OrderCreatedEvent(Guid OrderId) : DomainEvent
{
    public static OrderCreatedEvent Create(Guid orderId) => new(orderId);
}
