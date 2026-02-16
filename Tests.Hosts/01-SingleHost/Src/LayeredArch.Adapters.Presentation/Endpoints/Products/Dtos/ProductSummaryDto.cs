namespace LayeredArch.Adapters.Presentation.Endpoints.Products.Dtos;

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price,
    int StockQuantity);
