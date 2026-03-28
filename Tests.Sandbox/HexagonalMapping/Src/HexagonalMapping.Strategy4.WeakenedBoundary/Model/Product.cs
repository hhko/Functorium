using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HexagonalMapping.Strategy4.WeakenedBoundary.Model;

/// <summary>
/// ❌ Anti-pattern: Domain 엔티티에 기술 어노테이션이 포함되어 있습니다.
///
/// 이 방식의 문제점:
/// 1. Domain Core가 Infrastructure 기술(EF Core)에 의존
/// 2. Hexagonal Architecture의 핵심 원칙 위반
/// 3. "깨진 창문 이론" - 한 번 경계가 무너지면 계속 악화됨
///
/// 경고: 이 코드는 학습 목적으로만 사용하세요.
/// 실제 프로젝트에서는 절대 이 방식을 사용하지 마세요!
/// </summary>
[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(200)]
    [Column("product_name")]
    public string Name { get; private set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; private set; }

    [Required]
    [MaxLength(3)]
    [Column("currency")]
    public string Currency { get; private set; } = "USD";

    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; private set; }

    // EF Core를 위한 파라미터 없는 생성자 (또 다른 기술 요구사항)
    private Product() { }

    private Product(Guid id, string name, decimal price, string currency)
    {
        Id = id;
        Name = name;
        Price = price;
        Currency = currency;
        CreatedAt = DateTime.UtcNow;
    }

    public static Product Create(string name, decimal price, string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (price < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        return new Product(Guid.NewGuid(), name, price, currency.ToUpperInvariant());
    }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative", nameof(newPrice));
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FormattedPrice => $"{Price:N2} {Currency}";
}
