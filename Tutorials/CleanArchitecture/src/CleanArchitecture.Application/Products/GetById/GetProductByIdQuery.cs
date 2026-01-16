using CleanArchitecture.Application.Abstractions;

namespace CleanArchitecture.Application.Products.GetById;

public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto?>;
