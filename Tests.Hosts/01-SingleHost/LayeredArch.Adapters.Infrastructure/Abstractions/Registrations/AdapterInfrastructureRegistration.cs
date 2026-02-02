using Functorium.Abstractions.Registrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace LayeredArch.Adapters.Infrastructure.Abstractions.Registrations;

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
        // 도메인 이벤트 발행자 등록 (Publisher 관점 Observability 활성화)
        // =================================================================
        services.RegisterDomainEventPublisher(enableObservability: true);

        // =================================================================
        // Application 레이어의 도메인 이벤트 핸들러 등록
        // Mediator.SourceGenerator는 해당 패키지가 참조된 프로젝트 내의
        // 핸들러만 자동 등록하므로, 다른 어셈블리의 핸들러는 명시적 등록 필요
        // =================================================================
        services.RegisterDomainEventHandlersFromAssembly(
            LayeredArch.Application.AssemblyReference.Assembly);

        // =================================================================
        // Handler 관점 Observability 활성화
        // =================================================================
        services.RegisterObservableDomainEventNotificationPublisher();

        // =================================================================
        // MeterFactory 등록 (UsecaseMetricsPipeline에 필요)
        // =================================================================
        services.AddMetrics();

        // =================================================================
        // FluentValidation 등록 - 어셈블리에서 모든 Validator 자동 등록
        // =================================================================
        services.AddValidatorsFromAssembly(AssemblyReference.Assembly);
        services.AddValidatorsFromAssembly(LayeredArch.Application.AssemblyReference.Assembly);

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
