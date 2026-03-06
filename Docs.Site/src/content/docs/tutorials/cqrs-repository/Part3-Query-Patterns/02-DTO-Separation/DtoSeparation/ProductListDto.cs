namespace DtoSeparation;

/// <summary>
/// Query DTO (목록 조회) — 리스트 뷰에 최적화된 프로젝션.
/// 목록에서 필요한 최소한의 필드만 포함합니다.
/// Description 같은 큰 필드는 제외하여 네트워크 비용을 절감합니다.
/// </summary>
public sealed record ProductListDto(
    string Id,
    string Name,
    decimal Price,
    string Category);
