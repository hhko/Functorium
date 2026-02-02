using System.Reflection;
using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Functorium.Abstractions.Registrations;

// # DomainEventRegistration 메서드 명명 규칙
// ## 기본 패턴
// `RegisterDomainEvent{Component}` - 도메인 이벤트 관련 서비스 등록
//
// ## 컴포넌트
// - Publisher: 도메인 이벤트 발행자 (IDomainEventPublisher)
// - HandlersFromAssembly: Scrutor로 어셈블리 스캔하여 핸들러 자동 등록
// - NotificationPublisher: Mediator INotificationPublisher 데코레이터
//
// ## Observability 활성화
// - RegisterDomainEventPublisher(enableObservability: true): Publisher 관점 로깅/추적
// - RegisterObservableDomainEventNotificationPublisher(): Handler 관점 로깅/추적

/// <summary>
/// 도메인 이벤트 관련 서비스 등록 확장 메서드.
/// </summary>
public static class DomainEventRegistration
{
    /// <summary>
    /// 도메인 이벤트 발행자를 Scoped 서비스로 등록합니다.
    /// AddMediator() 호출 후 사용해야 합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="enableObservability">true이면 로깅, 추적이 자동으로 기록됩니다.</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// // 기본 등록 (Observability 비활성화)
    /// services.RegisterDomainEventPublisher();
    ///
    /// // Observability 활성화 (로깅, 추적 자동 기록)
    /// services.RegisterDomainEventPublisher(enableObservability: true);
    /// </code>
    /// </example>
    public static IServiceCollection RegisterDomainEventPublisher(
        this IServiceCollection services,
        bool enableObservability = false)
    {
        services.TryAddScoped<DomainEventPublisher>();

        if (enableObservability)
        {
            services.AddScoped<IDomainEventPublisher>(sp =>
            {
                var inner = sp.GetRequiredService<DomainEventPublisher>();
                var logger = sp.GetRequiredService<ILogger<ObservableDomainEventPublisher>>();
                return new ObservableDomainEventPublisher(inner, logger);
            });
        }
        else
        {
            services.TryAddScoped<IDomainEventPublisher>(sp =>
                sp.GetRequiredService<DomainEventPublisher>());
        }

        return services;
    }

    /// <summary>
    /// 지정된 어셈블리에서 IDomainEventHandler 구현체를 Scrutor로 스캔하여 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="assembly">스캔할 어셈블리</param>
    /// <param name="lifetime">서비스 생명주기 (기본값: Scoped)</param>
    /// <returns>서비스 컬렉션</returns>
    /// <exception cref="ArgumentNullException">assembly가 null일 때</exception>
    /// <example>
    /// <code>
    /// // Application 어셈블리에서 핸들러 스캔 (기본 Scoped)
    /// services.RegisterDomainEventHandlersFromAssembly(Application.AssemblyReference.Assembly);
    ///
    /// // Singleton으로 등록
    /// services.RegisterDomainEventHandlersFromAssembly(
    ///     Application.AssemblyReference.Assembly,
    ///     ServiceLifetime.Singleton);
    /// </code>
    /// </example>
    public static IServiceCollection RegisterDomainEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)))
            .AsImplementedInterfaces()
            .WithLifetime(lifetime));

        return services;
    }

    /// <summary>
    /// 관찰성이 통합된 도메인 이벤트 Notification Publisher를 등록합니다.
    /// 도메인 이벤트 핸들러(IDomainEventHandler) 실행 시 Handler 관점의 로깅과 추적이 자동으로 기록됩니다.
    /// AddMediator() 호출 후 사용해야 합니다.
    /// </summary>
    /// <remarks>
    /// 이 메서드는 기존 INotificationPublisher를 데코레이터 패턴으로 감쌉니다.
    /// ObservableDomainEventPublisher(Publisher 관점)와 함께 사용하면 전체 도메인 이벤트 발행 과정에 대한
    /// 완전한 관찰 가능성을 제공합니다.
    /// </remarks>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// // 완전한 도메인 이벤트 관찰 가능성 구성
    /// services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
    /// services.RegisterDomainEventPublisher(enableObservability: true);
    /// services.RegisterDomainEventHandlersFromAssembly(Application.AssemblyReference.Assembly);
    /// services.RegisterObservableDomainEventNotificationPublisher();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterObservableDomainEventNotificationPublisher(
        this IServiceCollection services)
    {
        // 기존 INotificationPublisher 등록을 교체 (데코레이터 패턴)
        services.Decorate<INotificationPublisher>((inner, sp) =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new ObservableDomainEventNotificationPublisher(inner, loggerFactory);
        });

        return services;
    }
}
