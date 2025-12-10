using Observability.Adapters.Infrastructure.Abstractions.Registrations;

WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.GetCurrentDirectory()
});

builder.Services
    .RegisterAdapterInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

app.UseAdapterInfrastructure();

await app.RunAsync();

public partial class Program { }