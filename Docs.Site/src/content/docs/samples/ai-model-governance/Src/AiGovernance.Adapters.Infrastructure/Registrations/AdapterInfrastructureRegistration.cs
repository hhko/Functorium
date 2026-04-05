using Functorium.Adapters.Abstractions.Registrations;
using Functorium.Adapters.Observabilities.Events;
using AiGovernance.Adapters.Infrastructure.ExternalServices;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Domain.SharedModels.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiGovernance.Adapters.Infrastructure.Registrations;

public static class AdapterInfrastructureRegistration
{
    public static IServiceCollection RegisterAdapterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // =================================================================
        // Mediator 및 도메인 이벤트 발행자 등록
        // NotificationPublisherType: Handler 관점 관찰 가능성 활성화
        // RegisterDomainEventPublisher: Publisher 관점 관찰 가능성 활성화
        // =================================================================
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
        });
        services.RegisterDomainEventPublisher();
        services.RegisterDomainEventHandlersFromAssembly(AiGovernance.Application.AssemblyReference.Assembly);

        // =================================================================
        // FluentValidation 등록 - 어셈블리에서 모든 Validator 자동 등록
        // =================================================================
        services.AddValidatorsFromAssembly(AssemblyReference.Assembly);
        services.AddValidatorsFromAssembly(AiGovernance.Application.AssemblyReference.Assembly);

        // =================================================================
        // OpenTelemetry 및 파이프라인 설정
        // =================================================================
        services
            .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
            .ConfigurePipelines(pipelines => pipelines
                .UseObservability()
                .UseValidation()
                .UseException()
                .UseTransaction())
            .Build();

        // =================================================================
        // 도메인 서비스 등록
        // =================================================================
        services.AddScoped<RiskClassificationService>();
        services.AddScoped<DeploymentEligibilityService>();

        // =================================================================
        // 외부 서비스 등록 (IO Advanced Features 데모)
        // =================================================================
        // Timeout + Catch 패턴
        services.AddScoped<IModelHealthCheckService, ModelHealthCheckService>();

        // Retry + Schedule 패턴
        services.AddScoped<IModelMonitoringService, ModelMonitoringService>();

        // Fork + awaitAll 패턴
        services.AddScoped<IParallelComplianceCheckService, ParallelComplianceCheckService>();

        // Bracket 패턴
        services.AddScoped<IModelRegistryService, ModelRegistryService>();

        return services;
    }

    public static IApplicationBuilder UseAdapterInfrastructure(this IApplicationBuilder app)
    {
        return app;
    }
}
