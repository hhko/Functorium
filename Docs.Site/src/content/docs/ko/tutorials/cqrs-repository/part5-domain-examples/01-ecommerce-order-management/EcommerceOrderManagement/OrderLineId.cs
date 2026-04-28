using Functorium.Domains.Entities;

namespace EcommerceOrderManagement;

public readonly record struct OrderLineId : IEntityId<OrderLineId>
{
    public Ulid Value { get; }

    private OrderLineId(Ulid value) => Value = value;

    public static OrderLineId New() => new(Ulid.NewUlid());
    public static OrderLineId Create(Ulid id) => new(id);
    public static OrderLineId Create(string id) => new(Ulid.Parse(id));

    public bool Equals(OrderLineId other) => Value == other.Value;
    public int CompareTo(OrderLineId other) => Value.CompareTo(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static OrderLineId Parse(string s, IFormatProvider? provider) => Create(s);
    public static bool TryParse(string? s, IFormatProvider? provider, out OrderLineId result)
    {
        if (s is null || !Ulid.TryParse(s, out var ulid)) { result = default; return false; }
        result = Create(ulid);
        return true;
    }
}
