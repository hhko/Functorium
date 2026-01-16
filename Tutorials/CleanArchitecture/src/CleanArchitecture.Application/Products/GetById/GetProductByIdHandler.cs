using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.Products.GetById;

public class GetProductByIdHandler : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> HandleAsync(GetProductByIdQuery query, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(query.ProductId, ct);

        if (product is null)
            return null;

        return new ProductDto(
            product.Id,
            product.Name,
            product.Sku,
            product.Price.Amount,
            product.Price.Currency,
            product.StockQuantity,
            product.IsActive);
    }
}
