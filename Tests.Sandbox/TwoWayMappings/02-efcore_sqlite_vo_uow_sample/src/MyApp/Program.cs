using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyApp.Adapters.Database;
using MyApp.Application;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddAppEfCoreSqlite(ctx.Configuration);
    })
    .Build();

// Apply migrations automatically (optional for sample; remove for production if you prefer explicit deployment)
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Demo: register a user twice to show unique-constraint based duplicate handling
using (var scope = host.Services.CreateScope())
{
    var svc = scope.ServiceProvider.GetRequiredService<RegistrationService>();

    try
    {
        var u1 = await svc.RegisterAsync("Test.User+demo@example.com", "Tester");
        Console.WriteLine($"Registered: {u1.Id} / {u1.Email.Value} / {u1.DisplayName}");

        // Duplicate (same normalized email)
        var u2 = await svc.RegisterAsync("test.user+demo@EXAMPLE.com", "Tester2");
        Console.WriteLine($"Registered: {u2.Id} / {u2.Email.Value} / {u2.DisplayName}");
    }
    catch (UserAlreadyExistsException ex)
    {
        Console.WriteLine($"Duplicate detected (mapped): {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex}");
    }
}

await host.StopAsync();
