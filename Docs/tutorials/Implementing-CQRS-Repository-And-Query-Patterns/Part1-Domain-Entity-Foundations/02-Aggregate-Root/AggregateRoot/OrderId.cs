using Functorium.Domains.Entities;

namespace AggregateRoot;

public readonly record struct OrderId : IEntityId<OrderId>
{
    public Ulid Value { get; }

    private OrderId(Ulid value) => Value = value;

    public static OrderId New() => new(Ulid.NewUlid());
    public static OrderId Create(Ulid id) => new(id);
    public static OrderId Create(string id) => new(Ulid.Parse(id));

    public bool Equals(OrderId other) => Value == other.Value;
    public int CompareTo(OrderId other) => Value.CompareTo(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
