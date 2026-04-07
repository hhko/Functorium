using System.Data;
using LayeredArch.Adapters.Persistence.Abstractions.Options;
using LayeredArch.Adapters.Persistence.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Customers.Queries;
using LayeredArch.Adapters.Persistence.Repositories.Customers.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Inventories.Queries;
using LayeredArch.Adapters.Persistence.Repositories.Inventories.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Orders.Queries;
using LayeredArch.Adapters.Persistence.Repositories.Orders.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Products.Queries;
using LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Tags.Repositories;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Abstractions.Options;
using Functorium.Applications.Persistence;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Application.Usecases.Inventories.Ports;
using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Application.Usecases.Products.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace LayeredArch.Adapters.Persistence.Abstractions.Registrations;

public static class AdapterPersistenceRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options 등록
        services.RegisterConfigureOptions<PersistenceOptions, PersistenceOptions.Validator>(
            PersistenceOptions.SectionName);

        var options = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();

        switch (options.Provider)
        {
            case "Sqlite":
                services.AddDbContext<LayeredArchDbContext>(opt =>
                    opt.UseSqlite(options.ConnectionString));
                RegisterSqliteRepositories(services);
                RegisterDapperQueries(services, options.ConnectionString);
                break;

            case "InMemory":
            default:
                RegisterInMemoryRepositories(services);
                break;
        }

        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<PersistenceOptions>>().Value;

        if (options.Provider == "Sqlite")
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LayeredArchDbContext>();
            dbContext.Database.EnsureCreated();
        }

        return app;
    }

    private static void RegisterInMemoryRepositories(IServiceCollection services)
    {
        // Repository 등록 (Source Generator가 생성한 Pipeline 버전 사용)
        services.RegisterScopedObservablePort<IProductRepository, ProductRepositoryInMemoryObservable>();
        services.RegisterScopedObservablePort<IInventoryRepository, InventoryRepositoryInMemoryObservable>();
        services.RegisterScopedObservablePort<IOrderRepository, OrderRepositoryInMemoryObservable>();
        services.RegisterScopedObservablePort<ICustomerRepository, CustomerRepositoryInMemoryObservable>();
        services.RegisterScopedObservablePort<ITagRepository, TagRepositoryInMemoryObservable>();

        // UnitOfWork 등록
        services.RegisterScopedObservablePort<IUnitOfWork, UnitOfWorkInMemoryObservable>();

        // 공유 Port 등록
        services.AddScoped<ProductRepositoryInMemory>();
        services.RegisterScopedObservablePort<IProductCatalog, ProductCatalogInMemoryObservable>();

        // Read Adapter 등록
        services.RegisterScopedObservablePort<IProductQuery, ProductQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IProductDetailQuery, ProductDetailQueryInMemoryObservable>();
        services.AddScoped<InventoryRepositoryInMemory>();
        services.RegisterScopedObservablePort<IInventoryQuery, InventoryQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IProductWithStockQuery, ProductWithStockQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IProductWithOptionalStockQuery, ProductWithOptionalStockQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<ICustomerDetailQuery, CustomerDetailQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<ICustomerOrderSummaryQuery, CustomerOrderSummaryQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<ICustomerOrdersQuery, CustomerOrdersQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IOrderDetailQuery, OrderDetailQueryInMemoryObservable>();
        services.RegisterScopedObservablePort<IOrderWithProductsQuery, OrderWithProductsQueryInMemoryObservable>();
    }

    private static void RegisterSqliteRepositories(IServiceCollection services)
    {
        // Repository 등록 (Source Generator가 생성한 Pipeline 버전 사용)
        services.RegisterScopedObservablePort<IProductRepository, ProductRepositoryEfCoreObservable>();
        services.RegisterScopedObservablePort<IInventoryRepository, InventoryRepositoryEfCoreObservable>();
        services.RegisterScopedObservablePort<IOrderRepository, OrderRepositoryEfCoreObservable>();
        services.RegisterScopedObservablePort<ICustomerRepository, CustomerRepositoryEfCoreObservable>();
        services.RegisterScopedObservablePort<ITagRepository, TagRepositoryEfCoreObservable>();

        // UnitOfWork 등록
        services.RegisterScopedObservablePort<IUnitOfWork, UnitOfWorkEfCoreObservable>();

        // 공유 Port 등록
        services.RegisterScopedObservablePort<IProductCatalog, ProductCatalogEfCoreObservable>();
    }

    private static void RegisterDapperQueries(
        IServiceCollection services, string connectionString)
    {
        services.AddScoped<IDbConnection>(_ =>
        {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        });

        services.RegisterScopedObservablePort<IProductQuery, ProductQueryDapperObservable>();
        services.RegisterScopedObservablePort<IProductWithStockQuery, ProductWithStockQueryDapperObservable>();
        services.RegisterScopedObservablePort<IProductWithOptionalStockQuery, ProductWithOptionalStockQueryDapperObservable>();
        services.RegisterScopedObservablePort<IInventoryQuery, InventoryQueryDapperObservable>();
        services.RegisterScopedObservablePort<ICustomerOrderSummaryQuery, CustomerOrderSummaryQueryDapperObservable>();
        services.RegisterScopedObservablePort<ICustomerOrdersQuery, CustomerOrdersQueryDapperObservable>();
        services.RegisterScopedObservablePort<IOrderWithProductsQuery, OrderWithProductsQueryDapperObservable>();
    }
}
