using Functorium.Abstractions.Registrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TwoWayMappingLayered.Adapters.Persistence.Repositories;
using TwoWayMappingLayered.Domains.Repositories;

namespace TwoWayMappingLayered.Adapters.Persistence.Abstractions.Registrations;

public static class AdapterPersistenceRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(this IServiceCollection services)
    {
        // Repository 등록 (Source Generator가 생성한 Pipeline 버전 사용)
        services.RegisterScopedPortObservable<IProductRepository, InMemoryProductRepositoryObservable>();

        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(this IApplicationBuilder app)
    {
        return app;
    }
}
