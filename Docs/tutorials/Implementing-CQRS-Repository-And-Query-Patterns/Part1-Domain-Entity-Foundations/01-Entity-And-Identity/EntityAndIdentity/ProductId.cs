using Functorium.Domains.Entities;

namespace EntityAndIdentity;

/// <summary>
/// Product Entity의 식별자.
/// [GenerateEntityId]가 자동 생성하는 코드를 수동으로 구현한 학습용 예제입니다.
/// </summary>
public readonly record struct ProductId : IEntityId<ProductId>
{
    public Ulid Value { get; }

    private ProductId(Ulid value) => Value = value;

    public static ProductId New() => new(Ulid.NewUlid());
    public static ProductId Create(Ulid id) => new(id);
    public static ProductId Create(string id) => new(Ulid.Parse(id));

    public bool Equals(ProductId other) => Value == other.Value;
    public int CompareTo(ProductId other) => Value.CompareTo(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
