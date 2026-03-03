namespace DtoSeparation;

/// <summary>
/// Command DTO (입력) — 새 Product 생성 요청.
/// 클라이언트가 보내는 데이터만 포함합니다.
/// Id, CreatedAt 등 서버가 생성하는 필드는 포함하지 않습니다.
/// </summary>
public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category);
