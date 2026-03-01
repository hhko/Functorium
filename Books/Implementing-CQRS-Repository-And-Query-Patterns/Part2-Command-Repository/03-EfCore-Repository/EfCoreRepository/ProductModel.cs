namespace EfCoreRepository;

/// <summary>
/// Product의 EF Core 퍼시스턴스 모델.
/// Domain Model(Product)과 분리하여 DB 스키마와 도메인 로직을 독립적으로 관리합니다.
/// </summary>
public sealed class ProductModel
{
    /// <summary>Ulid를 string으로 저장 (DB 호환성)</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Domain의 decimal → DB의 decimal 직접 매핑</summary>
    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}
