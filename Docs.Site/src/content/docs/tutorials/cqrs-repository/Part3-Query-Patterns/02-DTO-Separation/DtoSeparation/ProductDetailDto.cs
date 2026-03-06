namespace DtoSeparation;

/// <summary>
/// Query DTO (상세 조회) — 디테일 뷰에 최적화된 프로젝션.
/// 단일 상품 조회 시 모든 필드를 포함합니다.
/// ProductListDto보다 더 많은 정보를 담고 있습니다.
/// </summary>
public sealed record ProductDetailDto(
    string Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
