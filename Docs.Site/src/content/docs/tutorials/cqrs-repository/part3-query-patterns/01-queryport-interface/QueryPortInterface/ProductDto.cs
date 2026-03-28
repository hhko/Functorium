namespace QueryPortInterface;

/// <summary>
/// 읽기 전용 Product DTO.
/// IQueryPort의 TDto 타입 파라미터로 사용됩니다.
/// 도메인 엔터티와 달리, 클라이언트가 필요로 하는 필드만 포함합니다.
/// </summary>
public sealed record ProductDto(
    string Id,
    string Name,
    decimal Price,
    int Stock,
    string Category);
