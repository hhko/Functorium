namespace HexagonalMapping.Strategy2.OneWayMapping.Model;

/// <summary>
/// 도메인 엔티티: IProductModel 인터페이스를 구현합니다.
/// 비즈니스 로직은 인터페이스에 포함되지 않는 별도 메서드로 구현됩니다.
/// </summary>
public sealed class Product : IProductModel
{
    // IProductModel 구현
    public Guid Id { get; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; }

    private Product(Guid id, string name, decimal price, string currency)
    {
        Id = id;
        Name = name;
        Price = price;
        Currency = currency;
    }

    public static Product Create(string name, decimal price, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        return new Product(Guid.NewGuid(), name, price, currency.ToUpperInvariant());
    }

    /// <summary>
    /// 인터페이스를 통한 재구성: Adapter가 인터페이스를 구현하므로
    /// Adapter → Domain 방향으로는 매핑이 필요합니다.
    /// </summary>
    public static Product FromModel(IProductModel model)
    {
        return new Product(model.Id, model.Name, model.Price, model.Currency);
    }

    // 비즈니스 로직 (인터페이스에 포함되지 않음)
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newPrice));
        Price = newPrice;
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName;
    }

    public string FormattedPrice => $"{Price:N2} {Currency}";
}
