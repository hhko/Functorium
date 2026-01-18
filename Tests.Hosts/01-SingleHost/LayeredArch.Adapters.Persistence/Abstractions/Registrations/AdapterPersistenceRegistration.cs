using LayeredArch.Adapters.Persistence.Repositories;
using LayeredArch.Domain.Repositories;
using Functorium.Abstractions.Registrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LayeredArch.Adapters.Persistence.Abstractions.Registrations;

public static class AdapterPersistenceRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(this IServiceCollection services)
    {
        // Repository 등록 (Source Generator가 생성한 Pipeline 버전 사용)
        services.RegisterScopedAdapterPipeline<IProductRepository, InMemoryProductRepositoryPipeline>();

        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(this IApplicationBuilder app)
    {
        return app;
    }
}
