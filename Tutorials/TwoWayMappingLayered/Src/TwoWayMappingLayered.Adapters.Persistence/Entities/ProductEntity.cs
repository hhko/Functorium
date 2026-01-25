using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwoWayMappingLayered.Adapters.Persistence.Entities;

/// <summary>
/// Adapter 전용 영속성 모델
///
/// Two-Way Mapping 전략의 핵심:
/// - EF Core 어노테이션을 포함한 기술 의존성 모델
/// - Domain Core의 Product와 완전히 분리
/// - ProductMapper를 통해 양방향 변환
///
/// HappyCoders 문서 원문:
/// "In my experience, this variant is the most suitable."
/// (제 경험상, 이 방식이 가장 적합합니다.)
///
/// 장점:
/// - 명확한 아키텍처 경계 유지
/// - Core가 기술 의존성으로부터 완전히 자유로움
/// - 책임의 명확한 분리
/// </summary>
[Table("products")]
public class ProductEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }

    [Required]
    [MaxLength(3)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
