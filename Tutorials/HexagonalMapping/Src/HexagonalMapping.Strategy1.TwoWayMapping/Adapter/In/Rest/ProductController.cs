using HexagonalMapping.Domain.Model;
using HexagonalMapping.Strategy1.TwoWayMapping.Application.Port.In;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Adapter.In.Rest;

/// <summary>
/// REST Controller (Driving Adapter): 입력 포트(IProductService)를 통해 Use Case를 호출합니다.
///
/// Hexagonal Architecture 데이터 흐름:
/// HTTP Request → Controller → IProductService(입력 포트) → ProductService(Use Case)
///
/// 책임:
/// - HTTP 요청/응답 처리
/// - DTO ↔ Domain 변환 (Two-Way Mapping)
/// - Use Case 호출
/// </summary>
public class ProductController
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Use Case 호출
        var products = await _productService.GetAllProductsAsync(cancellationToken);

        // Domain → DTO 매핑 (응답용)
        return ProductDtoMapper.ToDtoList(products);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Use Case 호출
        var product = await _productService.GetProductAsync(ProductId.From(id), cancellationToken);

        // Domain → DTO 매핑 (응답용)
        return product is null ? null : ProductDtoMapper.ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        // Use Case 호출 (Request의 데이터를 전달)
        var product = await _productService.CreateProductAsync(
            request.Name,
            request.Price,
            request.Currency,
            cancellationToken);

        // Domain → DTO 매핑 (응답용)
        return ProductDtoMapper.ToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Price.HasValue)
            return null;

        // Use Case 호출
        var product = await _productService.UpdateProductPriceAsync(
            ProductId.From(id),
            request.Price.Value,
            request.Currency ?? "USD",
            cancellationToken);

        // Domain → DTO 매핑 (응답용)
        return product is null ? null : ProductDtoMapper.ToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Use Case 호출
        return await _productService.DeleteProductAsync(ProductId.From(id), cancellationToken);
    }
}
