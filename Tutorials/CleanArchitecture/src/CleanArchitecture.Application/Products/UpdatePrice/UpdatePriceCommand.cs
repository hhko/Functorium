using CleanArchitecture.Application.Abstractions;

namespace CleanArchitecture.Application.Products.UpdatePrice;

public record UpdatePriceCommand(
    Guid ProductId,
    decimal NewPrice,
    string Currency) : ICommand<bool>;
