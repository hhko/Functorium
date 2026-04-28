using Functorium.Domains.Specifications;

namespace UsecasePatterns.Usecases;

public record SearchProductsQuery(string? Category, decimal? MinPrice, decimal? MaxPrice, bool? InStockOnly);

public class SearchProductsQueryHandler
{
    private readonly IProductRepository _repository;

    public SearchProductsQueryHandler(IProductRepository repository)
        => _repository = repository;

    public IEnumerable<Product> Handle(SearchProductsQuery query)
    {
        var spec = Specification<Product>.All;

        if (query.Category is not null)
            spec &= new Specifications.ProductCategorySpec(query.Category);
        if (query.MinPrice.HasValue && query.MaxPrice.HasValue)
            spec &= new Specifications.ProductPriceRangeSpec(query.MinPrice.Value, query.MaxPrice.Value);
        if (query.InStockOnly == true)
            spec &= new Specifications.ProductInStockSpec();

        return _repository.FindAll(spec);
    }
}
