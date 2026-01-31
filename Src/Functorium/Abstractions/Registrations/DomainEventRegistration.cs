using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Microsoft.Extensions.DependencyInjection;

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
}
