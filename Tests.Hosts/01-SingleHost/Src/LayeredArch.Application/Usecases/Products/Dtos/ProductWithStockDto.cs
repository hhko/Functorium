namespace LayeredArch.Application.Usecases.Products.Dtos;

public sealed record ProductWithStockDto(
    string ProductId,
    string Name,
    decimal Price,
    int StockQuantity);
