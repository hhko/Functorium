using LanguageExt;
using LanguageExt.Common;

namespace QueryUsecase;

public sealed class InMemoryProductQuery : IProductQuery
{
    private readonly List<Product> _products;

    public InMemoryProductQuery(List<Product> products)
    {
        _products = products;
    }

    public FinT<IO, ProductDto> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            var product = _products.FirstOrDefault(p => p.Id.Equals(id));
            if (product is null)
                return Fin.Fail<ProductDto>(Error.New($"Product not found: {id}"));

            return Fin.Succ(ToDto(product));
        });
    }

    public FinT<IO, List<ProductDto>> SearchByName(string keyword)
    {
        return IO.lift(() =>
        {
            var results = _products
                .Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Select(ToDto)
                .ToList();

            return Fin.Succ(results);
        });
    }

    private static ProductDto ToDto(Product p)
        => new(p.Id.ToString(), p.Name, p.Price, p.IsActive);
}
