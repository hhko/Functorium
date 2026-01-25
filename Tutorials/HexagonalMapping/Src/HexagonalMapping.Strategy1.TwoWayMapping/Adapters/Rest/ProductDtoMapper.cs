using HexagonalMapping.Domain.Entities;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Adapters.Rest;

/// <summary>
/// REST DTO 매퍼: Domain ↔ DTO 변환을 담당합니다.
/// API 계층의 표현을 도메인 모델과 분리합니다.
/// </summary>
public static class ProductDtoMapper
{
    /// <summary>
    /// Domain → DTO (응답용)
    /// </summary>
    public static ProductDto ToDto(Product product) => new()
    {
        Id = product.Id.Value,
        Name = product.Name,
        Price = product.Price.Amount,
        Currency = product.Price.Currency
    };

    /// <summary>
    /// Request → Domain (생성용)
    /// </summary>
    public static Product ToDomain(CreateProductRequest request) =>
        Product.Create(request.Name, request.Price, request.Currency);

    /// <summary>
    /// 컬렉션 변환
    /// </summary>
    public static IReadOnlyList<ProductDto> ToDtoList(IEnumerable<Product> products) =>
        products.Select(ToDto).ToList();
}
