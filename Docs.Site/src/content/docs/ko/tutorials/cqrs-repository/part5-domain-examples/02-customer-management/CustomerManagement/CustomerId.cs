using Functorium.Domains.Entities;

namespace CustomerManagement;

public readonly record struct CustomerId : IEntityId<CustomerId>
{
    public Ulid Value { get; }

    private CustomerId(Ulid value) => Value = value;

    public static CustomerId New() => new(Ulid.NewUlid());
    public static CustomerId Create(Ulid id) => new(id);
    public static CustomerId Create(string id) => new(Ulid.Parse(id));

    public bool Equals(CustomerId other) => Value == other.Value;
    public int CompareTo(CustomerId other) => Value.CompareTo(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static CustomerId Parse(string s, IFormatProvider? provider) => Create(s);
    public static bool TryParse(string? s, IFormatProvider? provider, out CustomerId result)
    {
        if (s is null || !Ulid.TryParse(s, out var ulid)) { result = default; return false; }
        result = Create(ulid);
        return true;
    }
}
