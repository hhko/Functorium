using HexagonalMapping.Strategy1.TwoWayMapping.Adapter.In.Rest;
using HexagonalMapping.Strategy1.TwoWayMapping.Adapter.Out.Persistence;
using HexagonalMapping.Strategy1.TwoWayMapping.Application.Port.In;
using HexagonalMapping.Strategy1.TwoWayMapping.Application.Port.Out;
using HexagonalMapping.Strategy1.TwoWayMapping.Application.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HexagonalMapping.Strategy1.TwoWayMapping.Bootstrap;

/// <summary>
/// DI 설정: Hexagonal Architecture의 Bootstrap 계층입니다.
/// 모든 의존성 주입을 설정하여 애플리케이션을 구성합니다.
///
/// 의존성 방향:
/// Adapter (In/Out) → Application (Port/Service) → Domain (Model)
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 애플리케이션 서비스를 등록합니다.
    /// </summary>
    public static IServiceCollection AddTwoWayMappingServices(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        // Infrastructure - DbContext
        services.AddDbContext<ProductDbContext>(options =>
        {
            configureDbContext?.Invoke(options);
        });

        // Output Port (Driven Adapter)
        services.AddScoped<IProductRepository, ProductRepository>();

        // Input Port (Use Case)
        services.AddScoped<IProductService, ProductService>();

        // Driving Adapter
        services.AddScoped<ProductController>();

        return services;
    }

    /// <summary>
    /// In-Memory 데이터베이스를 사용하여 서비스를 등록합니다.
    /// 테스트 및 데모 목적으로 사용합니다.
    /// </summary>
    public static IServiceCollection AddTwoWayMappingServicesWithInMemoryDb(
        this IServiceCollection services,
        string databaseName = "TwoWayMappingDb")
    {
        return services.AddTwoWayMappingServices(options =>
            options.UseInMemoryDatabase(databaseName));
    }
}
