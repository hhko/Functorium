using CleanArchitecture.Application.Abstractions;

namespace CleanArchitecture.Application.Products.GetAll;

public record GetAllProductsQuery(bool OnlyActive = true) : IQuery<IEnumerable<ProductDto>>;
