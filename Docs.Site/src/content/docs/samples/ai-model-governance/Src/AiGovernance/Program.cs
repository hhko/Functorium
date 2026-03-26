using AiGovernance.Adapters.Infrastructure.Registrations;
using AiGovernance.Adapters.Persistence.Registrations;
using AiGovernance.Adapters.Presentation.Registrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence(builder.Configuration)
    .RegisterAdapterInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseAdapterPresentation();

app.Run();

// Integration Test를 위한 partial class
public partial class Program { }
