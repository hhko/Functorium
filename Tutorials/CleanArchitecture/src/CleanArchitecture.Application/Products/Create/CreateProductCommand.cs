using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Products.Create;

public record CreateProductCommand(
    string Name,
    string Sku,
    decimal Price,
    string Currency) : ICommand<ProductId>;
