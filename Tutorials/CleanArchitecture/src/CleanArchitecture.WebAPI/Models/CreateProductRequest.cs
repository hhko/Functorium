namespace CleanArchitecture.WebAPI.Models;

public record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    string Currency);
