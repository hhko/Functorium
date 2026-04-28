using Functorium.Domains.Events;

namespace DomainEventFlow;

public sealed record ProductPriceChangedEvent(
    string ProductId,
    decimal OldPrice,
    decimal NewPrice) : DomainEvent;
