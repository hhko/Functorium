using Functorium.Domains.Events;

namespace DomainEventFlow;

public sealed record ProductCreatedEvent(
    string ProductId,
    string Name,
    decimal Price) : DomainEvent;
