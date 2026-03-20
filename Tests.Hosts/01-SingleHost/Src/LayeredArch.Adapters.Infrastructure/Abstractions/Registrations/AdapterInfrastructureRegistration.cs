using Functorium.Adapters.Abstractions.Registrations;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Usecases;
using LayeredArch.Application.Usecases.Orders.Commands;
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
            .ConfigurePipelines(pipelines => pipelines
                .UseAll()
                .AddCustomPipelinesFromAssembly(AssemblyReference.Assembly))
            .Build();

        // Log Enricher (별도 등록 — ICustomUsecasePipeline이 아니므로 Scrutor 스캔 대상 아님)
        services.AddScoped<
            IUsecaseLogEnricher<CreateOrderCommand.Request, FinResponse<CreateOrderCommand.Response>>,
            CreateOrderCommandRequestLogEnricher>();

        return services;
    }

    public static IApplicationBuilder UseAdapterInfrastructure(this IApplicationBuilder app)
    {
        return app;
    }
}
