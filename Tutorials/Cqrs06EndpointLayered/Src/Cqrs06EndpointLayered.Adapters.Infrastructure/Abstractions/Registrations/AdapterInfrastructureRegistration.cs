using FluentValidation;
using Functorium.Abstractions.Registrations;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Cqrs06EndpointLayered.Adapters.Infrastructure.Abstractions.Registrations;

public static class AdapterInfrastructureRegistration
{
    public static IServiceCollection RegisterAdapterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // =================================================================
        // Mediator 등록 (Scoped - WebApi에서 요청당 Scope 생성)
        // Mediator.SourceGenerator가 이 어셈블리에서 AddMediator() 생성
        // =================================================================
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);

        // =================================================================
        // MeterFactory 등록 (UsecaseMetricsPipeline에 필요)
        // =================================================================
        services.AddMetrics();

        // =================================================================
        // FluentValidation 등록 - 어셈블리에서 모든 Validator 자동 등록
        // =================================================================
        services.AddValidatorsFromAssembly(AssemblyReference.Assembly);
        services.AddValidatorsFromAssembly(Cqrs06EndpointLayered.Applications.AssemblyReference.Assembly);

        // =================================================================
        // OpenTelemetry 및 파이프라인 설정
        // =================================================================
        services
            .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
            .ConfigureTracing(tracing => tracing.Configure(b => b.AddConsoleExporter()))
            .ConfigureMetrics(metrics => metrics.Configure(b => b.AddConsoleExporter()))
            .ConfigurePipelines()
            .Build();

        return services;
    }

    public static IApplicationBuilder UseAdapterInfrastructure(this IApplicationBuilder app)
    {
        return app;
    }
}
