using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Intermediate;

/// <summary>
/// Intermediate02: Scoped 서비스와 함께 사용
/// 
/// 학습 목표:
/// - HTTP 요청 시나리오 시뮬레이션
///   * 각 HTTP 요청을 하나의 스코프로 시뮬레이션합니다
///   * 스코프가 생성될 때 IOptionsSnapshot<T>가 해당 시점의 설정 값을 캡처합니다
///   * 같은 요청 내에서는 일관된 설정 값을 보장합니다
///   * 여러 요청이 동시에 처리되어도 각각 독립적인 설정 값을 가집니다
/// - 요청 중간에 설정 변경 시나리오
///   * 요청 처리 중간에 설정 파일이 변경되면, 현재 요청에는 영향을 주지 않습니다
///   * 다음 요청부터 새로운 설정 값이 적용됩니다
///   * IOptionsSnapshot<T>는 스코프 생성 시점의 값을 캡처하므로 안정적입니다
///   * 설정 변경이 즉시 반영되지 않을 수 있으므로, 중요한 설정은 재시작을 고려해야 합니다
/// - IOptionsSnapshot<T>의 실시간 반영
///   * "실시간"은 각 요청마다 최신 설정 값을 가져온다는 의미입니다
///   * 요청 처리 중간에 변경된 설정은 다음 요청부터 반영됩니다
///   * reloadOnChange: true로 설정하면 파일 변경 시 자동으로 리로드됩니다
///   * IOptions<T>와 달리 각 요청마다 새로운 스냅샷을 제공하므로 더 유연합니다
/// </summary>
public static class Intermediate02_ScopedOptions
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Intermediate02: Scoped Options in Request Scenario");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // 동적으로 변경 가능한 설정 시뮬레이션
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // reloadOnChange: true
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<ApiClientOptions>()
            .BindConfiguration(ApiClientOptions.SectionName);

        // Scoped 서비스 등록
        services.AddScoped<ApiClientService>();

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("Simulating HTTP Requests:");
        Console.WriteLine("─".PadRight(80, '─'));

        // 여러 요청 시뮬레이션
        for (int i = 1; i <= 3; i++)
        {
            Console.WriteLine($"\nRequest {i}:");
            using var scope = serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ApiClientService>();
            
            // 각 요청마다 IOptionsSnapshot<T>는 새로운 스냅샷을 가져옵니다
            service.MakeApiCall($"Request-{i}");
        }

        Console.WriteLine();

        // IOptions<T>와 비교
        Console.WriteLine("Comparison: IOptions<T> vs IOptionsSnapshot<T>");
        Console.WriteLine("─".PadRight(80, '─'));

        var options = serviceProvider.GetRequiredService<IOptions<ApiClientOptions>>();
        Console.WriteLine("IOptions<T> (same instance across requests):");
        Console.WriteLine($"  BaseUrl: {options.Value.BaseUrl}");
        Console.WriteLine($"  TimeoutSeconds: {options.Value.TimeoutSeconds}");

        using (var scope = serviceProvider.CreateScope())
        {
            var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ApiClientOptions>>();
            Console.WriteLine("IOptionsSnapshot<T> (new snapshot per request):");
            Console.WriteLine($"  BaseUrl: {snapshot.Value.BaseUrl}");
            Console.WriteLine($"  TimeoutSeconds: {snapshot.Value.TimeoutSeconds}");
        }

        Console.WriteLine();

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - IOptionsSnapshot<T>는 각 스코프(요청)마다 새로운 스냅샷을 생성합니다");
        Console.WriteLine("   - 웹 애플리케이션에서 요청 처리 중 설정이 변경되면 다음 요청부터 반영됩니다");
        Console.WriteLine("   - IOptions<T>는 Singleton이므로 설정 변경이 즉시 반영되지 않을 수 있습니다");
        Console.WriteLine("   - Scoped 서비스와 함께 사용하면 요청별로 일관된 설정 값을 보장할 수 있습니다");
        Console.WriteLine();
    }

    private sealed class ApiClientService
    {
        private readonly IOptionsSnapshot<ApiClientOptions> _optionsSnapshot;

        public ApiClientService(IOptionsSnapshot<ApiClientOptions> optionsSnapshot)
        {
            _optionsSnapshot = optionsSnapshot;
        }

        public void MakeApiCall(string requestId)
        {
            var options = _optionsSnapshot.Value;
            Console.WriteLine($"  [{requestId}] Calling API:");
            Console.WriteLine($"    BaseUrl: {options.BaseUrl}");
            Console.WriteLine($"    Timeout: {options.TimeoutSeconds}s");
            Console.WriteLine($"    MaxRetries: {options.MaxRetries}");
        }
    }
}
