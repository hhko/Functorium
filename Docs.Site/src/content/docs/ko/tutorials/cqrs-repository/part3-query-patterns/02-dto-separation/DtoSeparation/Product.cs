using Functorium.Domains.Entities;

namespace DtoSeparation;

/// <summary>
/// Product Aggregate Root.
/// 도메인 로직(비즈니스 규칙)을 포함하는 도메인 엔터티입니다.
/// 이 엔터티는 직접 클라이언트에 반환되지 않습니다.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() : base()
    {
        Name = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
    }

    public Product(ProductId id, string name, string description, decimal price, int stock, string category)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        Category = category;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>가격 변경 도메인 로직</summary>
    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0) throw new InvalidOperationException("Price cannot be negative.");
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>재고 차감 도메인 로직</summary>
    public void DecreaseStock(int quantity)
    {
        if (quantity > Stock) throw new InvalidOperationException("Insufficient stock.");
        Stock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
