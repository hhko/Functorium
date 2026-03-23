using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Functorium.Adapters.Events;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Applications.Events;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Functorium.Adapters.Abstractions.Registrations;

/// <summary>
/// 도메인 이벤트 관련 서비스 등록 확장 메서드.
/// </summary>
/// <remarks>
/// <para>
/// Mediator와 도메인 이벤트를 함께 사용하려면 다음과 같이 등록합니다:
/// </para>
/// <code>
/// // 1. Mediator 등록 (Handler 관점 관찰 가능성 활성화)
/// services.AddMediator(options =>
/// {
///     options.ServiceLifetime = ServiceLifetime.Scoped;
///     options.NotificationPublisherType = typeof(ObservableDomainEventNotificationPublisher);
/// });
///
/// // 2. 도메인 이벤트 발행자 등록 (Publisher 관점 관찰 가능성 활성화)
/// services.RegisterDomainEventPublisher(enableObservability: true);
/// </code>
/// <para>
/// <b>참고:</b> NotificationPublisherType은 소스 생성기가 컴파일 타임에 분석하므로
/// typeof() 표현식을 직접 사용해야 합니다. 속성이나 변수는 사용할 수 없습니다.
/// </para>
/// </remarks>
public static class DomainEventRegistration
{
    /// <summary>
    /// 도메인 이벤트 발행자를 Scoped 서비스로 등록합니다.
    /// AddMediator() 호출 후 사용해야 합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="enableObservability">true이면 로깅, 추적이 자동으로 기록됩니다.</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection RegisterDomainEventPublisher(this IServiceCollection services)
    {
        services.TryAddScoped<DomainEventPublisher>();
        services.TryAddScoped<IDomainEventCollector, DomainEventCollector>();

        services.AddScoped<IDomainEventPublisher>(sp =>
        {
            var activitySource = sp.GetRequiredService<ActivitySource>();
            var inner = sp.GetRequiredService<DomainEventPublisher>();
            var collector = sp.GetRequiredService<IDomainEventCollector>();
            var logger = sp.GetRequiredService<ILogger<ObservableDomainEventPublisher>>();
            var meterFactory = sp.GetRequiredService<IMeterFactory>();
            var openTelemetryOptions = sp.GetRequiredService<IOptions<OpenTelemetryOptions>>();
            return new ObservableDomainEventPublisher(activitySource, inner, collector, logger, meterFactory, openTelemetryOptions);
        });

        return services;
    }

    /// <summary>
    /// 지정된 어셈블리에서 IDomainEventHandler 구현체를 Scrutor로 스캔하여 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assembly">스캔할 어셈블리</param>
    /// <param name="lifetime">서비스 생명주기 (기본값: Scoped)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection RegisterDomainEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)))
            .AsImplementedInterfaces()
            .WithLifetime(lifetime));

        return services;
    }

    /// <summary>
    /// 지정된 어셈블리에서 IDomainEventBatchHandler 구현체를 Scrutor로 스캔하여 등록합니다.
    /// 배치 핸들러는 Publisher에서 직접 호출되며 Mediator 라우팅을 사용하지 않습니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assembly">스캔할 어셈블리</param>
    /// <param name="lifetime">서비스 생명주기 (기본값: Scoped)</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection RegisterDomainEventBatchHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventBatchHandler<>)))
            .AsImplementedInterfaces()
            .WithLifetime(lifetime));

        return services;
    }

}
