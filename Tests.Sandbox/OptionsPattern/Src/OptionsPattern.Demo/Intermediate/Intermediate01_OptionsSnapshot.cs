using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Intermediate;

/// <summary>
/// Intermediate01: IOptionsSnapshot<T> 기본 사용
/// 
/// 학습 목표:
/// - IOptionsSnapshot<T> vs IOptions<T> 차이 이해
///   * IOptions<T>는 Singleton으로, 애플리케이션 전체에서 동일한 인스턴스를 공유합니다
///   * IOptionsSnapshot<T>는 Scoped로, 각 요청(스코프)마다 새로운 스냅샷을 제공합니다
///   * IOptions<T>는 설정이 변경되어도 자동으로 갱신되지 않습니다
///   * IOptionsSnapshot<T>는 요청 시점의 최신 설정 값을 캡처합니다
/// - Scoped 라이프사이클 이해
///   * Scoped 서비스는 각 스코프(HTTP 요청 등)마다 새로운 인스턴스가 생성됩니다
///   * 같은 스코프 내에서는 동일한 인스턴스를 공유합니다
///   * 스코프가 종료되면 인스턴스도 함께 해제됩니다
///   * 웹 애플리케이션에서 각 HTTP 요청이 하나의 스코프입니다
/// - 요청별 설정 갱신 동작
///   * 각 요청마다 새로운 IOptionsSnapshot<T>가 생성되므로, 최신 설정 값을 반영합니다
///   * 요청 처리 중간에 설정이 변경되면, 다음 요청부터 새로운 값이 적용됩니다
///   * 같은 요청 내에서는 일관된 설정 값을 보장합니다
///   * 설정 변경이 즉시 반영되지 않을 수 있으므로 주의가 필요합니다
/// - IOptionsSnapshot<T>의 Value 속성 사용
///   * Value 속성은 해당 스코프의 설정 스냅샷을 반환합니다
///   * 같은 스코프 내에서는 항상 같은 인스턴스를 반환합니다
///   * 스코프가 생성될 때의 설정 값을 캡처하므로, 스코프 내에서는 변경되지 않습니다
///   * 웹 애플리케이션에서 요청별로 다른 설정 값을 보장할 수 있습니다
/// </summary>
public static class Intermediate01_OptionsSnapshot
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Intermediate01: Options Snapshot (IOptionsSnapshot<T>)");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SimpleOptions>()
            .BindConfiguration(SimpleOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        // IOptionsSnapshot<T>는 Scoped 서비스이므로
        // 각 스코프마다 새로운 스냅샷을 가져옵니다
        Console.WriteLine("Example 1: Multiple Scopes with IOptionsSnapshot<T>");
        Console.WriteLine("─".PadRight(80, '─'));

        // 스코프 1
        using (var scope1 = serviceProvider.CreateScope())
        {
            var snapshot1 = scope1.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
            Console.WriteLine("Scope 1:");
            Console.WriteLine($"  Name: {snapshot1.Value.Name}");
            Console.WriteLine($"  Value: {snapshot1.Value.Value}");
        }

        // 스코프 2 (새로운 스코프)
        using (var scope2 = serviceProvider.CreateScope())
        {
            var snapshot2 = scope2.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
            Console.WriteLine("Scope 2:");
            Console.WriteLine($"  Name: {snapshot2.Value.Name}");
            Console.WriteLine($"  Value: {snapshot2.Value.Value}");
        }

        Console.WriteLine();

        // IOptions<T>와 비교
        Console.WriteLine("Example 2: IOptions<T> vs IOptionsSnapshot<T>");
        Console.WriteLine("─".PadRight(80, '─'));

        var options = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();
        Console.WriteLine("IOptions<T> (Singleton):");
        Console.WriteLine($"  Name: {options.Value.Name}");
        Console.WriteLine($"  Value: {options.Value.Value}");

        using (var scope = serviceProvider.CreateScope())
        {
            var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
            Console.WriteLine("IOptionsSnapshot<T> (Scoped):");
            Console.WriteLine($"  Name: {snapshot.Value.Name}");
            Console.WriteLine($"  Value: {snapshot.Value.Value}");
        }

        Console.WriteLine();

        // 서비스에서 사용하는 예제
        Console.WriteLine("Example 3: Using IOptionsSnapshot<T> in a Service");
        Console.WriteLine("─".PadRight(80, '─'));

        services.AddScoped<ExampleService>();
        var serviceProvider2 = services.BuildServiceProvider();

        // 여러 스코프에서 서비스 사용
        for (int i = 1; i <= 3; i++)
        {
            using var scope = serviceProvider2.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ExampleService>();
            service.DoSomething($"Request-{i}");
        }

        Console.WriteLine();

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - IOptionsSnapshot<T>는 Scoped 라이프사이클을 가집니다");
        Console.WriteLine("   - 각 HTTP 요청(또는 스코프)마다 새로운 스냅샷을 가져옵니다");
        Console.WriteLine("   - IOptions<T>는 Singleton이므로 애플리케이션 전체에서 동일한 인스턴스를 공유합니다");
        Console.WriteLine("   - 웹 애플리케이션에서 요청 중간에 설정이 변경되면 IOptionsSnapshot<T>가 최신 값을 반영합니다");
        Console.WriteLine();
    }

    private sealed class ExampleService
    {
        private readonly IOptionsSnapshot<SimpleOptions> _optionsSnapshot;

        public ExampleService(IOptionsSnapshot<SimpleOptions> optionsSnapshot)
        {
            _optionsSnapshot = optionsSnapshot;
        }

        public void DoSomething(string requestId)
        {
            var options = _optionsSnapshot.Value;
            Console.WriteLine($"  [{requestId}] Using options: Name={options.Name}, Value={options.Value}");
        }
    }
}
