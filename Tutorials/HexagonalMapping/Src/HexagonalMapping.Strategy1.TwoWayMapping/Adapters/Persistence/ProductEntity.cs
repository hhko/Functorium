using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Adapters.Persistence;

/// <summary>
/// Adapter 전용 모델: 영속성 기술 어노테이션을 포함합니다.
/// Domain Core의 Product와 분리되어 아키텍처 경계를 유지합니다.
/// </summary>
[Table("products")]
public class ProductEntity
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

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
