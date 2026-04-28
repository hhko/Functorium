using Functorium.Domains.Events;

namespace DomainEvents;

public sealed record OrderConfirmedEvent(OrderId OrderId) : DomainEvent;
