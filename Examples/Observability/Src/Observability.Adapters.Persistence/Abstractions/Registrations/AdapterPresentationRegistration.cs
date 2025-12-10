using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Observability.Adapters.Persistence.Abstractions.Registrations;

public static class AdapterPresentationRegistration
{
    public static IServiceCollection RegisterAdapterPersistence(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseAdapterPersistence(this IApplicationBuilder app)
    {
        return app;
    }
}
