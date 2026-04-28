using Functorium.Domains.Entities;

namespace DomainEvents;

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

    public static OrderId Parse(string s, IFormatProvider? provider) => Create(s);
    public static bool TryParse(string? s, IFormatProvider? provider, out OrderId result)
    {
        if (s is null || !Ulid.TryParse(s, out var ulid)) { result = default; return false; }
        result = Create(ulid);
        return true;
    }
}
