using Functorium.Domains.Specifications;

namespace DynamicFilter;

public static class ProductFilterBuilder
{
    public static Specification<Product> Build(SearchProductsRequest request)
    {
        var spec = Specification<Product>.All;

        if (!string.IsNullOrWhiteSpace(request.Name))
            spec &= new Specifications.ProductNameContainsSpec(request.Name);
        if (!string.IsNullOrWhiteSpace(request.Category))
            spec &= new Specifications.ProductCategorySpec(request.Category);
        if (request.MinPrice.HasValue && request.MaxPrice.HasValue)
            spec &= new Specifications.ProductPriceRangeSpec(request.MinPrice.Value, request.MaxPrice.Value);
        if (request.InStockOnly)
            spec &= new Specifications.ProductInStockSpec();

        return spec;
    }
}
