using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.Products.GetAll;

public class GetAllProductsHandler : IQueryHandler<GetAllProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetAllProductsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IEnumerable<ProductDto>> HandleAsync(GetAllProductsQuery query, CancellationToken ct = default)
    {
        var products = query.OnlyActive
            ? await _productRepository.GetActiveAsync(ct)
            : await _productRepository.GetAllAsync(ct);

        return products.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Sku,
            p.Price.Amount,
            p.Price.Currency,
            p.StockQuantity,
            p.IsActive));
    }
}
