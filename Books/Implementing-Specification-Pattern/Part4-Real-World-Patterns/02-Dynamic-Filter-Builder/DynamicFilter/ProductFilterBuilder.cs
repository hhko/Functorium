using Functorium.Domains.Specifications;

namespace DynamicFilter;

public static class ProductFilterBuilder
{
    public static Specification<Product> Build(SearchProductsRequest request)
    {
        var spec = Specification<Product>.All;

        if (!string.IsNullOrWhiteSpace(request.Name))
            spec &= new Specifications.NameContainsSpec(request.Name);
        if (!string.IsNullOrWhiteSpace(request.Category))
            spec &= new Specifications.CategorySpec(request.Category);
        if (request.MinPrice.HasValue && request.MaxPrice.HasValue)
            spec &= new Specifications.PriceRangeSpec(request.MinPrice.Value, request.MaxPrice.Value);
        if (request.InStockOnly)
            spec &= new Specifications.InStockSpec();

        return spec;
    }
}
