using Cqrs03Functional.Demo.Domain.ValueObjects;
using Functorium.Domains.Entities;

namespace Cqrs03Functional.Demo.Domain;

/// <summary>
/// 상품 도메인 모델
/// Source Generator가 ProductId를 자동 생성합니다.
/// </summary>
[GenerateEntityId]
public sealed class Product : Entity<ProductId>
{
    public ProductName Name { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public Price Price { get; private set; } = null!;
    public StockQuantity StockQuantity { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() { }

    private Product(
        ProductId id,
        ProductName name,
        string description,
        Price price,
        StockQuantity stockQuantity,
        DateTime createdAt,
        DateTime? updatedAt = null) : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>
    /// 새 상품 생성
    /// </summary>
    public static Fin<Product> Create(
        ProductName name,
        string description,
        Price price,
        StockQuantity stockQuantity) =>
        Fin.Succ(new Product(
            ProductId.New(),
            name,
            description,
            price,
            stockQuantity,
            DateTime.UtcNow));

    /// <summary>
    /// 기존 상품 복원 (영속성 계층용)
    /// </summary>
    public static Fin<Product> CreateFromPersistence(
        ProductId id,
        ProductName name,
        string description,
        Price price,
        StockQuantity stockQuantity,
        DateTime createdAt,
        DateTime? updatedAt = null) =>
        Fin.Succ(new Product(id, name, description, price, stockQuantity, createdAt, updatedAt));

    /// <summary>
    /// 상품 정보 업데이트
    /// </summary>
    public Product Update(
        ProductName name,
        string description,
        Price price,
        StockQuantity stockQuantity) =>
        new(Id, name, description, price, stockQuantity, CreatedAt, DateTime.UtcNow);
}
