namespace HexagonalMapping.Domain.Entities;

/// <summary>
/// 강타입 식별자 (Strongly-typed ID)
/// </summary>
public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
    public static ProductId From(Guid id) => new(id);
    public static ProductId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
}
