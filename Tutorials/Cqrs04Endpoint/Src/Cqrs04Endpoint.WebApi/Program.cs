using System.Reflection;
using Cqrs04Endpoint.WebApi.Domain;
using Cqrs04Endpoint.WebApi.Infrastructure;
using FastEndpoints;
using FluentValidation;
using Functorium.Abstractions.Registrations;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// =================================================================
// FastEndpoints 등록
// =================================================================
builder.Services.AddFastEndpoints();

// =================================================================
// Configuration 설정
// =================================================================
IConfiguration configuration = builder.Configuration;

// =================================================================
// MeterFactory 등록 (UsecaseMetricsPipeline에 필요)
// =================================================================
builder.Services.AddMetrics();

// =================================================================
// Mediator 등록 (Scoped - WebApi에서 요청당 Scope 생성)
// =================================================================
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

// =================================================================
// FluentValidation 등록 - 어셈블리에서 모든 Validator 자동 등록
// =================================================================
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// =================================================================
// OpenTelemetry 및 파이프라인 설정
// =================================================================
builder.Services
    .RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigureTracing(tracing => tracing.Configure(b => b.AddConsoleExporter()))
    .ConfigureMetrics(metrics => metrics.Configure(b => b.AddConsoleExporter()))
    .ConfigurePipelines()
    .Build();

// =================================================================
// Repository 등록
// =================================================================
builder.Services.RegisterScopedAdapterPipeline<IProductRepository, InMemoryProductRepositoryPipeline>();

// =================================================================
// App 빌드 및 미들웨어 설정
// =================================================================
var app = builder.Build();

app.UseFastEndpoints(c =>
{
    c.Serializer.Options.PropertyNamingPolicy = null; // PascalCase 유지
});

app.Run();

// 테스트를 위한 partial class 노출
public partial class Program { }
