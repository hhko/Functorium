using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Functorium.Abstractions.Registrations;

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
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection RegisterDomainEventPublisher(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        return services;
    }

    /// <summary>
    /// 관찰성이 통합된 도메인 이벤트 발행자를 Scoped 서비스로 등록합니다.
    /// 로깅, 추적, 메트릭이 자동으로 기록됩니다.
    /// AddMediator() 호출 후 사용해야 합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection RegisterObservableDomainEventPublisher(this IServiceCollection services)
    {
        // 내부 구현체를 키 기반으로 등록 (데코레이터 패턴)
        services.TryAddScoped<DomainEventPublisher>();

        services.AddScoped<IDomainEventPublisher>(sp =>
        {
            var inner = sp.GetRequiredService<DomainEventPublisher>();
            var logger = sp.GetRequiredService<ILogger<ObservableDomainEventPublisher>>();
            return new ObservableDomainEventPublisher(inner, logger);
        });

        return services;
    }
}
