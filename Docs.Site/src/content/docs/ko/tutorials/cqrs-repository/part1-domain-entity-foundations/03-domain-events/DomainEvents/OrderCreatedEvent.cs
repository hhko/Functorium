using Functorium.Domains.Events;

namespace DomainEvents;

public sealed record OrderCreatedEvent(
    OrderId OrderId,
    string CustomerName,
    decimal TotalAmount) : DomainEvent;
