using CleanArchitecture.Application.Abstractions;
using CleanArchitecture.Application.Products;
using CleanArchitecture.Application.Products.Create;
using CleanArchitecture.Application.Products.GetAll;
using CleanArchitecture.Application.Products.GetById;
using CleanArchitecture.Application.Products.UpdatePrice;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Commands
        services.AddScoped<ICommandHandler<CreateProductCommand, Guid>, CreateProductHandler>();
        services.AddScoped<ICommandHandler<UpdatePriceCommand, bool>, UpdatePriceHandler>();

        // Queries
        services.AddScoped<IQueryHandler<GetProductByIdQuery, ProductDto?>, GetProductByIdHandler>();
        services.AddScoped<IQueryHandler<GetAllProductsQuery, IEnumerable<ProductDto>>, GetAllProductsHandler>();

        return services;
    }
}
