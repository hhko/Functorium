using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application;
using MyApp.Application.Ports;

namespace MyApp.Adapters.Database;

public static class EfCoreConfig
{
    public static IServiceCollection AddAppEfCore(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(connectionString)); // UseNpgsql / UseSqlite 등으로 교체 가능

        services.AddScoped<IPersistencePort, PersistenceAdapter>();
        services.AddScoped<IUnitOfWorkPort, EfUnitOfWork>();
        services.AddScoped<RegistrationService>();

        return services;
    }
}
