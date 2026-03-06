namespace DapperQueryAdapter;

/// <summary>
/// 읽기 전용 Product 프로젝션.
/// Dapper가 SQL 결과를 매핑하는 대상 타입입니다.
/// </summary>
public sealed record ProductDto(
    string Id,
    string Name,
    decimal Price,
    int Stock,
    string Category);
