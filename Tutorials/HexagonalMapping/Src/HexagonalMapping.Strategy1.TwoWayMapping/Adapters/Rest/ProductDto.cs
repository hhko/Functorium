using System.Text.Json.Serialization;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Adapters.Rest;

/// <summary>
/// REST API용 DTO: 외부 클라이언트에게 노출되는 데이터 구조입니다.
/// 내부 도메인 모델과 분리되어 API 계약의 안정성을 보장합니다.
/// </summary>
public record ProductDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "USD";

    [JsonPropertyName("formattedPrice")]
    public string FormattedPrice => $"{Price:N2} {Currency}";
}

public record CreateProductRequest
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("price")]
    public required decimal Price { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "USD";
}

public record UpdateProductRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("price")]
    public decimal? Price { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
}
