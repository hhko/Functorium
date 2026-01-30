using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Products.UpdatePrice;

public record UpdatePriceCommand(
    ProductId ProductId,
    decimal NewPrice,
    string Currency) : ICommand<bool>;
