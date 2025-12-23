using Functorium.Adapters.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Observability.Adapters.Infrastructure.Abstractions.Options;

namespace Observability.Adapters.Infrastructure.Abstractions.Registrations;

public static class AdapterInfrastructureRegistration
{
    public static IServiceCollection RegisterAdapterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterObservability(configuration);

        // FTP Options 등록
        services.RegisterConfigureOptions<FtpOptions, FtpOptions.Validator>(
            FtpOptions.SectionName);

        return services;
    }

    public static IApplicationBuilder UseAdapterInfrastructure(this IApplicationBuilder app)
    {
        return app;
    }
}

