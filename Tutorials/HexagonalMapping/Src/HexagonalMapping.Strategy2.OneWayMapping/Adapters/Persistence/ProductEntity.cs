using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HexagonalMapping.Strategy2.OneWayMapping.Domain;

namespace HexagonalMapping.Strategy2.OneWayMapping.Adapters.Persistence;

/// <summary>
/// Adapter 모델: IProductModel 인터페이스를 구현합니다.
/// 인터페이스를 구현하므로 Domain으로 직접 전달될 수 있습니다.
/// 단, 기술 어노테이션은 여전히 Adapter에만 존재합니다.
/// </summary>
[Table("products")]
public class ProductEntity : IProductModel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// IProductModel로부터 엔티티 생성
    /// </summary>
    public static ProductEntity FromModel(IProductModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Price = model.Price,
        Currency = model.Currency,
        CreatedAt = DateTime.UtcNow
    };
}
