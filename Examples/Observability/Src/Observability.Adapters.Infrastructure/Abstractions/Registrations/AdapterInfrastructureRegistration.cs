using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Observability.Adapters.Infrastructure.Abstractions.Registrations;

public static class AdapterInfrastructureRegistration
{
    public static IServiceCollection RegisterAdapterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterOpenTelemetry(configuration);

        return services;
    }

    public static IApplicationBuilder UseAdapterInfrastructure(this IApplicationBuilder app)
    {
        return app;
    }
}

