using Functorium.Domains.Events;

namespace MediatorEvents.Demo.Domain;

/// <summary>
/// 주문 Entity
/// </summary>
public sealed class Order
{
    public string Id { get; }
    public string CustomerName { get; }
    public decimal TotalAmount { get; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; }

    private readonly List<IDomainEvent> _domainEvents = [];

    private Order(string id, string customerName, decimal totalAmount, DateTime createdAt)
    {
        Id = id;
        CustomerName = customerName;
        TotalAmount = totalAmount;
        Status = OrderStatus.Created;
        CreatedAt = createdAt;
    }

    public static Order Create(string customerName, decimal totalAmount)
    {
        var order = new Order(
            Ulid.NewUlid().ToString(),
            customerName,
            totalAmount,
            DateTime.UtcNow);

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, order.CustomerName, order.TotalAmount));

        return order;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Created)
            throw new InvalidOperationException($"Cannot ship order in status: {Status}");

        Status = OrderStatus.Shipped;
        AddDomainEvent(new OrderShippedEvent(Id, DateTime.UtcNow));
    }

    public void Complete()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot complete order in status: {Status}");

        Status = OrderStatus.Completed;
        AddDomainEvent(new OrderCompletedEvent(Id));
    }

    private void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> GetDomainEvents() =>
        _domainEvents.AsReadOnly();

    public void ClearDomainEvents() =>
        _domainEvents.Clear();
}

public enum OrderStatus
{
    Created,
    Shipped,
    Completed
}

/// <summary>
/// 주문 생성 이벤트
/// </summary>
public sealed record OrderCreatedEvent(
    string OrderId,
    string CustomerName,
    decimal TotalAmount) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public Ulid EventId { get; } = Ulid.NewUlid();
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}

/// <summary>
/// 주문 배송 이벤트
/// </summary>
public sealed record OrderShippedEvent(
    string OrderId,
    DateTime ShippedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public Ulid EventId { get; } = Ulid.NewUlid();
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}

/// <summary>
/// 주문 완료 이벤트
/// </summary>
public sealed record OrderCompletedEvent(
    string OrderId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public Ulid EventId { get; } = Ulid.NewUlid();
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}
