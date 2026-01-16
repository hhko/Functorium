namespace CleanArchitecture.Application.Products;

public record ProductDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsActive);
