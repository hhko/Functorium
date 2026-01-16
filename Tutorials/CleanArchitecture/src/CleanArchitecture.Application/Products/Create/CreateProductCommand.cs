using CleanArchitecture.Application.Abstractions;

namespace CleanArchitecture.Application.Products.Create;

public record CreateProductCommand(
    string Name,
    string Sku,
    decimal Price,
    string Currency) : ICommand<Guid>;
