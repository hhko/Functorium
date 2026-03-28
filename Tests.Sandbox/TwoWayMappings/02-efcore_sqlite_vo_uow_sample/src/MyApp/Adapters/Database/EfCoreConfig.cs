using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application;
using MyApp.Application.Ports;

namespace MyApp.Adapters.Database;

public static class EfCoreConfig
{
    public static IServiceCollection AddAppEfCoreSqlite(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("AppDb")
                 ?? throw new InvalidOperationException("ConnectionStrings:AppDb is missing.");

        services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));

        services.AddScoped<IPersistencePort, PersistenceAdapter>();
        services.AddScoped<IUnitOfWorkPort, EfUnitOfWork>();
        services.AddScoped<RegistrationService>();

        return services;
    }
}
