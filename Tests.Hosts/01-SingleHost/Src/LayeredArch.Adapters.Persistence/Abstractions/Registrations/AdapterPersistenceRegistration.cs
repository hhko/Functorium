using System.Data;
using LayeredArch.Adapters.Persistence.Abstractions.Options;
using LayeredArch.Adapters.Persistence.Repositories.Dapper;
using LayeredArch.Adapters.Persistence.Repositories.EfCore;
using LayeredArch.Adapters.Persistence.Repositories.InMemory;
using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Tags;
using Functorium.Abstractions.Registrations;
using Functorium.Adapters.Options;
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
                RegisterDapperQueryAdapters(services, options.ConnectionString);
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
        services.RegisterScopedObservablePort<IProductRepository, InMemoryProductRepositoryObservable>();
        services.RegisterScopedObservablePort<IInventoryRepository, InMemoryInventoryRepositoryObservable>();
        services.RegisterScopedObservablePort<IOrderRepository, InMemoryOrderRepositoryObservable>();
        services.RegisterScopedObservablePort<ICustomerRepository, InMemoryCustomerRepositoryObservable>();
        services.RegisterScopedObservablePort<ITagRepository, InMemoryTagRepositoryObservable>();

        // UnitOfWork 등록
        services.RegisterScopedObservablePort<IUnitOfWork, InMemoryUnitOfWorkObservable>();

        // 공유 Port 등록
        services.AddScoped<InMemoryProductRepository>();
        services.RegisterScopedObservablePort<IProductCatalog, InMemoryProductCatalogObservable>();

        // Read Adapter 등록
        services.RegisterScopedObservablePort<IProductQuery, InMemoryProductQueryAdapterObservable>();
        services.RegisterScopedObservablePort<IProductDetailQuery, InMemoryProductDetailQueryAdapterObservable>();
        services.AddScoped<InMemoryInventoryRepository>();
        services.RegisterScopedObservablePort<IInventoryQuery, InMemoryInventoryQueryAdapterObservable>();
        services.RegisterScopedObservablePort<IProductWithStockQuery, InMemoryProductWithStockQueryAdapterObservable>();
        services.RegisterScopedObservablePort<ICustomerDetailQuery, InMemoryCustomerDetailQueryAdapterObservable>();
        services.RegisterScopedObservablePort<IOrderDetailQuery, InMemoryOrderDetailQueryAdapterObservable>();
    }

    private static void RegisterSqliteRepositories(IServiceCollection services)
    {
        // Repository 등록 (Source Generator가 생성한 Pipeline 버전 사용)
        services.RegisterScopedObservablePort<IProductRepository, EfCoreProductRepositoryObservable>();
        services.RegisterScopedObservablePort<IInventoryRepository, EfCoreInventoryRepositoryObservable>();
        services.RegisterScopedObservablePort<IOrderRepository, EfCoreOrderRepositoryObservable>();
        services.RegisterScopedObservablePort<ICustomerRepository, EfCoreCustomerRepositoryObservable>();
        services.RegisterScopedObservablePort<ITagRepository, EfCoreTagRepositoryObservable>();

        // UnitOfWork 등록
        services.RegisterScopedObservablePort<IUnitOfWork, EfCoreUnitOfWorkObservable>();

        // 공유 Port 등록
        services.RegisterScopedObservablePort<IProductCatalog, EfCoreProductCatalogObservable>();
    }

    private static void RegisterDapperQueryAdapters(
        IServiceCollection services, string connectionString)
    {
        services.AddScoped<IDbConnection>(_ =>
        {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        });

        services.RegisterScopedObservablePort<IProductQuery, DapperProductQueryAdapterObservable>();
        services.RegisterScopedObservablePort<IProductWithStockQuery, DapperProductWithStockQueryAdapterObservable>();
        services.RegisterScopedObservablePort<IInventoryQuery, DapperInventoryQueryAdapterObservable>();
    }
}
