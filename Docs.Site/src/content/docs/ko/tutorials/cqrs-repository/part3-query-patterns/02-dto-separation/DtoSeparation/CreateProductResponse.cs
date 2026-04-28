namespace DtoSeparation;

/// <summary>
/// Command DTO (출력) — Product 생성 응답.
/// 생성된 리소스의 ID와 최소한의 확인 정보만 반환합니다.
/// </summary>
public sealed record CreateProductResponse(
    string Id,
    string Name,
    DateTime CreatedAt);
