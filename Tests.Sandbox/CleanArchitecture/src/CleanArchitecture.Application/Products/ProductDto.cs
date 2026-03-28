namespace CleanArchitecture.Application.Products;

public record ProductDto(
    string Id,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive);
