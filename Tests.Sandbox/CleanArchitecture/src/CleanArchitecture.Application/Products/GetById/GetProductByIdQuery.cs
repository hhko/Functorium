using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Products.GetById;

public record GetProductByIdQuery(ProductId ProductId) : IQuery<ProductDto?>;
