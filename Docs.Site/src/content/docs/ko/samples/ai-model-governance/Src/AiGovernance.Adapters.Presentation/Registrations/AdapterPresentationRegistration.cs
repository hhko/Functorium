using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AiGovernance.Adapters.Presentation.Registrations;

public static class AdapterPresentationRegistration
{
    public static IServiceCollection RegisterAdapterPresentation(this IServiceCollection services)
    {
        // FastEndpoints 등록
        services.AddFastEndpoints();

        return services;
    }

    public static IApplicationBuilder UseAdapterPresentation(this IApplicationBuilder app)
    {
        app.UseFastEndpoints(c =>
        {
            c.Serializer.Options.PropertyNamingPolicy = null; // PascalCase 유지
        });

        return app;
    }
}
