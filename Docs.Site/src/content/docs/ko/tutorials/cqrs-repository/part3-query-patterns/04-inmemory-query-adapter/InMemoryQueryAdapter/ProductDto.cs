namespace InMemoryQueryAdapter;

/// <summary>
/// 읽기 전용 Product 프로젝션.
/// </summary>
public sealed record ProductDto(
    string Id,
    string Name,
    decimal Price,
    int Stock,
    string Category);
