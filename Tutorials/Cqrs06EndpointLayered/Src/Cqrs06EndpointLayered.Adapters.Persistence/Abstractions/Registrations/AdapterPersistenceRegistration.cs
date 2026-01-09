using Cqrs06EndpointLayered.Adapters.Persistence.Repositories;
using Cqrs06EndpointLayered.Domains.Repositories;
using Functorium.Abstractions.Registrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs06EndpointLayered.Adapters.Persistence.Abstractions.Registrations;

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
