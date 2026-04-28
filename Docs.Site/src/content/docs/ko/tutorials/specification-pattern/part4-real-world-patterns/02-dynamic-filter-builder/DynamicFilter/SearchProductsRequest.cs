namespace DynamicFilter;

public record SearchProductsRequest(
    string? Name = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool InStockOnly = false);
