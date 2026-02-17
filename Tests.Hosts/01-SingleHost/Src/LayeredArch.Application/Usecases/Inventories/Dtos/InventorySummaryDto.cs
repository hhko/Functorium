namespace LayeredArch.Application.Usecases.Inventories.Dtos;

public sealed record InventorySummaryDto(
    string InventoryId,
    string ProductId,
    int StockQuantity);
