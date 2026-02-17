namespace LayeredArch.Application.Usecases.Products.Dtos;

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price);
