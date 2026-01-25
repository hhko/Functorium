namespace HexagonalMapping.Domain.Entities;

/// <summary>
/// 도메인 엔티티: 기술 의존성이 없는 순수 비즈니스 객체입니다.
/// Hexagonal Architecture에서 Core의 중심에 위치합니다.
/// </summary>
public sealed class Product
{
    public ProductId Id { get; }
    public string Name { get; private set; }
    public Money Price { get; private set; }

    private Product(ProductId id, string name, Money price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public static Product Create(string name, decimal amount, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Product(
            ProductId.New(),
            name,
            Money.Create(amount, currency));
    }

    public static Product Reconstitute(Guid id, string name, decimal amount, string currency)
    {
        return new Product(
            ProductId.From(id),
            name,
            Money.Create(amount, currency));
    }

    public void UpdatePrice(decimal amount, string currency)
    {
        Price = Money.Create(amount, currency);
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName;
    }
}
