using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Functorium.Abstractions.Observabilities;

namespace Functorium.Abstractions.Registrations;

// # ObservablePortRegistration 메서드 명명 규칙
// ## 기본 패턴
// `Register{Lifetime}ObservablePort` - 단일 인터페이스를 하나의 구현체로 등록
//
// ## For 접미사 패턴
// `Register{Lifetime}ObservablePortFor` - 여러 인터페이스를 하나의 구현체로 등록
//
// - For 접미사가 없을 때
//   - `Register{Lifetime}ObservablePort<TService>`: 단일 인터페이스 등록
// - For 접미사가 있을 때
//   - `Register{Lifetime}ObservablePortFor<T1, T2, TImpl>`: 2개 인터페이스 → 1개 구현체
//   - `Register{Lifetime}ObservablePortFor<T1, T2, T3, TImpl>`: 3개 인터페이스 → 1개 구현체
//   - `Register{Lifetime}ObservablePortFor<TImpl>(params Type[])`: N개 인터페이스 → 1개 구현체
//
// ## For 접미사 의미
//  "해당 인터페이스들을 위해(For) 단일 구현체를 등록한다"는 의미로,
//  동일한 구현체 인스턴스를 여러 서비스 인터페이스로 해결(resolve)할 수 있게 합니다.
public static class ObservablePortRegistration
{
    // - RegisterScopedObservablePort
    // - RegisterTransientObservablePort
    // - RegisterSingletonObservablePort
    //
    // - RegisterScopedObservablePortFor
    // - RegisterTransientObservablePortFor
    // - RegisterSingletonObservablePortFor

    // For 접미사가 없을 때: 단일 인터페이스 등록

    /// <summary>
    /// Scoped 서비스를 등록합니다.
    /// 구현 타입의 생성자에 ActivitySource, ILogger, IMeterFactory를 주입합니다.
    /// </summary>
    /// <typeparam name="TService">등록할 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스 타입</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterScopedObservablePort&lt;IRepositoryIO, RepositoryIoObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterScopedObservablePort<TService, TImplementation>(
        this IServiceCollection services)
            where TService : class, IObservablePort
            where TImplementation : class, TService
    {
        return services.AddScoped<TService>(sp => ActivatorUtilities.CreateInstance<TImplementation>(sp));
    }

    /// <summary>
    /// Transient 서비스를 등록합니다.
    /// 구현 타입의 생성자에 ActivitySource, ILogger, IMeterFactory를 주입합니다.
    /// </summary>
    /// <typeparam name="TService">등록할 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스 타입</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterTransientObservablePort&lt;IRepositoryIO, RepositoryIoObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterTransientObservablePort<TService, TImplementation>(
        this IServiceCollection services)
            where TService : class, IObservablePort
            where TImplementation : class, TService
    {
        return services.AddTransient<TService>(sp => ActivatorUtilities.CreateInstance<TImplementation>(sp));
    }

    /// <summary>
    /// Singleton 서비스를 등록합니다.
    /// 구현 타입의 생성자에 ActivitySource, ILogger, IMeterFactory를 주입합니다.
    /// </summary>
    /// <typeparam name="TService">등록할 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스 타입</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterSingletonObservablePort&lt;IMessagePublisher, MessagePublisherObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterSingletonObservablePort<TService, TImplementation>(
        this IServiceCollection services)
            where TService : class, IObservablePort
            where TImplementation : class, TService
    {
        return services.AddSingleton<TService>(sp => ActivatorUtilities.CreateInstance<TImplementation>(sp));
    }

    // For 접미사가 있을 때: 복수 인터페이스 등록

    /// <summary>
    /// Scoped 서비스를 등록하고, 동일한 구현체를 두 개의 서비스 인터페이스로 등록합니다.
    /// HTTP 요청당 1개의 인스턴스가 생성되며, 모든 인터페이스가 동일한 인스턴스를 공유합니다.
    /// </summary>
    /// <typeparam name="TService1">등록할 첫 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService2">등록할 두 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterScopedObservablePortFor&lt;IService1, IService2, MyObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterScopedObservablePortFor<TService1, TService2, TImplementation>(
        this IServiceCollection services)
            where TService1 : class, IObservablePort
            where TService2 : class, IObservablePort
            where TImplementation : class, TService1, TService2
    {
        // 구현체 등록
        services.AddScoped<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 인터페이스가 동일한 구현체 인스턴스를 참조
        services.AddScoped<TService1>(sp => sp.GetRequiredService<TImplementation>());
        services.AddScoped<TService2>(sp => sp.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    /// Scoped 서비스를 등록하고, 동일한 구현체를 세 개의 서비스 인터페이스로 등록합니다.
    /// HTTP 요청당 1개의 인스턴스가 생성되며, 모든 인터페이스가 동일한 인스턴스를 공유합니다.
    /// </summary>
    /// <typeparam name="TService1">등록할 첫 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService2">등록할 두 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService3">등록할 세 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterScopedObservablePortFor&lt;IService1, IService2, IService3, MyObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterScopedObservablePortFor<TService1, TService2, TService3, TImplementation>(
        this IServiceCollection services)
            where TService1 : class, IObservablePort
            where TService2 : class, IObservablePort
            where TService3 : class, IObservablePort
            where TImplementation : class, TService1, TService2, TService3
    {
        // 구현체 등록
        services.AddScoped<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 인터페이스가 동일한 구현체 인스턴스를 참조
        services.AddScoped<TService1>(sp => sp.GetRequiredService<TImplementation>());
        services.AddScoped<TService2>(sp => sp.GetRequiredService<TImplementation>());
        services.AddScoped<TService3>(sp => sp.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    /// Scoped 서비스를 등록하고, 동일한 구현체를 여러 개의 서비스 인터페이스로 등록합니다 (4개 이상 지원).
    /// HTTP 요청당 1개의 인스턴스가 생성되며, 모든 인터페이스가 동일한 인스턴스를 공유합니다.
    /// </summary>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="serviceTypes">등록할 서비스 인터페이스 타입 배열</param>
    /// <returns>서비스 컬렉션</returns>
    /// <exception cref="ArgumentException">서비스 타입이 IObservablePort를 구현하지 않거나, 구현 클래스가 서비스 타입을 구현하지 않을 때</exception>
    /// <example>
    /// <code>
    /// services.RegisterScopedObservablePortFor&lt;MyObservable&gt;(
    ///     typeof(IService1), typeof(IService2), typeof(IService3), typeof(IService4));
    /// </code>
    /// </example>
    public static IServiceCollection RegisterScopedObservablePortFor<TImplementation>(
        this IServiceCollection services,
        params Type[] serviceTypes)
            where TImplementation : class, IObservablePort
    {
        // 런타임 검증
        Type implementationType = typeof(TImplementation);
        foreach (Type serviceType in serviceTypes)
        {
            if (!typeof(IObservablePort).IsAssignableFrom(serviceType))
            {
                throw new ArgumentException(
                    $"Service type '{serviceType.Name}' must implement IObservablePort interface.",
                    nameof(serviceTypes));
            }

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException(
                    $"Implementation type '{implementationType.Name}' must implement service interface '{serviceType.Name}'.",
                    nameof(serviceTypes));
            }
        }

        // 구현체 등록
        services.AddScoped<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 서비스 타입 등록
        foreach (Type serviceType in serviceTypes)
        {
            services.AddScoped(serviceType, sp => sp.GetRequiredService<TImplementation>());
        }

        return services;
    }

    /// <summary>
    /// Transient 서비스를 등록하고, 동일한 구현체를 두 개의 서비스 인터페이스로 등록합니다.
    /// 요청될 때마다 새로운 인스턴스가 생성됩니다.
    /// </summary>
    /// <typeparam name="TService1">등록할 첫 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService2">등록할 두 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterTransientObservablePortFor&lt;IService1, IService2, MyObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterTransientObservablePortFor<TService1, TService2, TImplementation>(
        this IServiceCollection services)
            where TService1 : class, IObservablePort
            where TService2 : class, IObservablePort
            where TImplementation : class, TService1, TService2
    {
        // 구현체 등록
        services.AddTransient<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 인터페이스가 동일한 구현체 인스턴스를 참조
        services.AddTransient<TService1>(sp => sp.GetRequiredService<TImplementation>());
        services.AddTransient<TService2>(sp => sp.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    /// Transient 서비스를 등록하고, 동일한 구현체를 세 개의 서비스 인터페이스로 등록합니다.
    /// 요청될 때마다 새로운 인스턴스가 생성됩니다.
    /// </summary>
    /// <typeparam name="TService1">등록할 첫 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService2">등록할 두 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService3">등록할 세 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterTransientObservablePortFor&lt;IService1, IService2, IService3, MyObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterTransientObservablePortFor<TService1, TService2, TService3, TImplementation>(
        this IServiceCollection services)
            where TService1 : class, IObservablePort
            where TService2 : class, IObservablePort
            where TService3 : class, IObservablePort
            where TImplementation : class, TService1, TService2, TService3
    {
        // 구현체 등록
        services.AddTransient<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 인터페이스가 동일한 구현체 인스턴스를 참조
        services.AddTransient<TService1>(sp => sp.GetRequiredService<TImplementation>());
        services.AddTransient<TService2>(sp => sp.GetRequiredService<TImplementation>());
        services.AddTransient<TService3>(sp => sp.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    /// Transient 서비스를 등록하고, 동일한 구현체를 여러 개의 서비스 인터페이스로 등록합니다 (4개 이상 지원).
    /// 요청될 때마다 새로운 인스턴스가 생성됩니다.
    /// </summary>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="serviceTypes">등록할 서비스 인터페이스 타입 배열</param>
    /// <returns>서비스 컬렉션</returns>
    /// <exception cref="ArgumentException">서비스 타입이 IObservablePort를 구현하지 않거나, 구현 클래스가 서비스 타입을 구현하지 않을 때</exception>
    /// <example>
    /// <code>
    /// services.RegisterTransientObservablePortFor&lt;MyObservable&gt;(
    ///     typeof(IService1), typeof(IService2), typeof(IService3), typeof(IService4));
    /// </code>
    /// </example>
    public static IServiceCollection RegisterTransientObservablePortFor<TImplementation>(
        this IServiceCollection services,
        params Type[] serviceTypes)
            where TImplementation : class, IObservablePort
    {
        // 런타임 검증
        Type implementationType = typeof(TImplementation);
        foreach (Type serviceType in serviceTypes)
        {
            if (!typeof(IObservablePort).IsAssignableFrom(serviceType))
            {
                throw new ArgumentException(
                    $"Service type '{serviceType.Name}' must implement IObservablePort interface.",
                    nameof(serviceTypes));
            }

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException(
                    $"Implementation type '{implementationType.Name}' must implement service interface '{serviceType.Name}'.",
                    nameof(serviceTypes));
            }
        }

        // 구현체 등록
        services.AddTransient<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 서비스 타입 등록
        foreach (Type serviceType in serviceTypes)
        {
            services.AddTransient(serviceType, sp => sp.GetRequiredService<TImplementation>());
        }

        return services;
    }

    /// <summary>
    /// Singleton 서비스를 등록하고, 동일한 구현체를 두 개의 서비스 인터페이스로 등록합니다.
    /// 애플리케이션 전체에서 1개의 인스턴스가 생성되며, 모든 인터페이스가 동일한 인스턴스를 공유합니다.
    /// </summary>
    /// <typeparam name="TService1">등록할 첫 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService2">등록할 두 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterSingletonObservablePortFor&lt;IService1, IService2, MyObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterSingletonObservablePortFor<TService1, TService2, TImplementation>(
        this IServiceCollection services)
            where TService1 : class, IObservablePort
            where TService2 : class, IObservablePort
            where TImplementation : class, TService1, TService2
    {
        // 구현체 등록
        services.AddSingleton<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 인터페이스가 동일한 구현체 인스턴스를 참조
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImplementation>());
        services.AddSingleton<TService2>(sp => sp.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    /// Singleton 서비스를 등록하고, 동일한 구현체를 세 개의 서비스 인터페이스로 등록합니다.
    /// 애플리케이션 전체에서 1개의 인스턴스가 생성되며, 모든 인터페이스가 동일한 인스턴스를 공유합니다.
    /// </summary>
    /// <typeparam name="TService1">등록할 첫 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService2">등록할 두 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TService3">등록할 세 번째 서비스 인터페이스</typeparam>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    /// <example>
    /// <code>
    /// services.RegisterSingletonObservablePortFor&lt;IService1, IService2, IService3, MyObservable&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection RegisterSingletonObservablePortFor<TService1, TService2, TService3, TImplementation>(
        this IServiceCollection services)
            where TService1 : class, IObservablePort
            where TService2 : class, IObservablePort
            where TService3 : class, IObservablePort
            where TImplementation : class, TService1, TService2, TService3
    {
        // 구현체 등록
        services.AddSingleton<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 인터페이스가 동일한 구현체 인스턴스를 참조
        services.AddSingleton<TService1>(sp => sp.GetRequiredService<TImplementation>());
        services.AddSingleton<TService2>(sp => sp.GetRequiredService<TImplementation>());
        services.AddSingleton<TService3>(sp => sp.GetRequiredService<TImplementation>());

        return services;
    }

    /// <summary>
    /// Singleton 서비스를 등록하고, 동일한 구현체를 여러 개의 서비스 인터페이스로 등록합니다 (4개 이상 지원).
    /// 애플리케이션 전체에서 1개의 인스턴스가 생성되며, 모든 인터페이스가 동일한 인스턴스를 공유합니다.
    /// </summary>
    /// <typeparam name="TImplementation">구현 클래스</typeparam>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="serviceTypes">등록할 서비스 인터페이스 타입 배열</param>
    /// <returns>서비스 컬렉션</returns>
    /// <exception cref="ArgumentException">서비스 타입이 IObservablePort를 구현하지 않거나, 구현 클래스가 서비스 타입을 구현하지 않을 때</exception>
    /// <example>
    /// <code>
    /// services.RegisterSingletonObservablePortFor&lt;MyObservable&gt;(
    ///     typeof(IService1), typeof(IService2), typeof(IService3), typeof(IService4));
    /// </code>
    /// </example>
    public static IServiceCollection RegisterSingletonObservablePortFor<TImplementation>(
        this IServiceCollection services,
        params Type[] serviceTypes)
            where TImplementation : class, IObservablePort
    {
        // 런타임 검증
        Type implementationType = typeof(TImplementation);
        foreach (Type serviceType in serviceTypes)
        {
            if (!typeof(IObservablePort).IsAssignableFrom(serviceType))
            {
                throw new ArgumentException(
                    $"Service type '{serviceType.Name}' must implement IObservablePort interface.",
                    nameof(serviceTypes));
            }

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException(
                    $"Implementation type '{implementationType.Name}' must implement service interface '{serviceType.Name}'.",
                    nameof(serviceTypes));
            }
        }

        // 구현체 등록
        services.AddSingleton<TImplementation>(sp =>
            ActivatorUtilities.CreateInstance<TImplementation>(sp));

        // 각 서비스 타입 등록
        foreach (Type serviceType in serviceTypes)
        {
            services.AddSingleton(serviceType, sp => sp.GetRequiredService<TImplementation>());
        }

        return services;
    }
}
