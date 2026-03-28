using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Advanced;

/// <summary>
/// Advanced01: IOptionsMonitor<T> 기본 사용
/// 
/// 학습 목표:
/// - IOptionsMonitor<T> vs IOptionsSnapshot<T> 차이 이해
///   * IOptionsMonitor<T>는 Singleton 라이프사이클을 가지며, 설정 변경을 감지할 수 있습니다
///   * IOptionsSnapshot<T>는 Scoped 라이프사이클을 가지며, 각 요청마다 새로운 스냅샷을 제공합니다
///   * IOptionsMonitor<T>는 백그라운드 서비스나 Singleton 서비스에서 사용하기 적합합니다
///   * IOptionsSnapshot<T>는 웹 애플리케이션의 각 HTTP 요청에서 사용하기 적합합니다
/// - CurrentValue 속성 사용
///   * CurrentValue는 항상 최신 설정 값을 반환하는 읽기 전용 속성입니다
///   * 설정이 변경되면 CurrentValue를 통해 즉시 변경된 값을 가져올 수 있습니다
///   * 여러 번 호출해도 같은 인스턴스를 반환합니다 (설정이 변경되지 않는 한)
/// - Singleton 라이프사이클 이해
///   * IOptionsMonitor<T>는 애플리케이션 전체에서 단일 인스턴스로 존재합니다
///   * 여러 서비스에서 동일한 IOptionsMonitor<T> 인스턴스를 공유합니다
///   * 메모리 효율적이며, 설정 변경 감지 기능을 제공합니다
/// - 실시간 설정 값 접근
///   * CurrentValue를 통해 언제든지 최신 설정 값을 가져올 수 있습니다
///   * 설정 파일이 변경되면 자동으로 새로운 값이 반영됩니다 (reloadOnChange: true일 때)
///   * OnChange 콜백을 등록하여 설정 변경을 감지할 수 있습니다
/// </summary>
public static class Advanced01_OptionsMonitor
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced01: Options Monitor (IOptionsMonitor<T>)");
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

        // IOptionsMonitor<T>는 Singleton으로 등록됩니다
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();

        Console.WriteLine("Example 1: Using CurrentValue");
        Console.WriteLine("─".PadRight(80, '─'));
        Console.WriteLine("CurrentValue (always gets the latest value):");
        var currentValue = monitor.CurrentValue;
        OptionsViewer.PrintOptions(currentValue, "CurrentValue");

        // 여러 번 호출해도 같은 인스턴스
        var currentValue2 = monitor.CurrentValue;
        Console.WriteLine($"Same instance? {ReferenceEquals(currentValue, currentValue2)}");
        Console.WriteLine();

        // IOptions<T>와 비교
        Console.WriteLine("Example 2: IOptions<T> vs IOptionsMonitor<T>");
        Console.WriteLine("─".PadRight(80, '─'));

        var options = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();
        Console.WriteLine("IOptions<T>.Value:");
        Console.WriteLine($"  Name: {options.Value.Name}");
        Console.WriteLine($"  Value: {options.Value.Value}");

        Console.WriteLine("IOptionsMonitor<T>.CurrentValue:");
        Console.WriteLine($"  Name: {monitor.CurrentValue.Name}");
        Console.WriteLine($"  Value: {monitor.CurrentValue.Value}");

        Console.WriteLine();

        // IOptionsSnapshot<T>와 비교
        Console.WriteLine("Example 3: IOptionsSnapshot<T> vs IOptionsMonitor<T>");
        Console.WriteLine("─".PadRight(80, '─'));

        using (var scope = serviceProvider.CreateScope())
        {
            var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SimpleOptions>>();
            Console.WriteLine("IOptionsSnapshot<T>.Value (Scoped):");
            Console.WriteLine($"  Name: {snapshot.Value.Name}");
            Console.WriteLine($"  Value: {snapshot.Value.Value}");
        }

        Console.WriteLine("IOptionsMonitor<T>.CurrentValue (Singleton):");
        Console.WriteLine($"  Name: {monitor.CurrentValue.Name}");
        Console.WriteLine($"  Value: {monitor.CurrentValue.Value}");

        Console.WriteLine();

        // 서비스에서 사용하는 예제
        Console.WriteLine("Example 4: Using IOptionsMonitor<T> in a Singleton Service");
        Console.WriteLine("─".PadRight(80, '─'));

        services.AddSingleton<BackgroundService>();
        var serviceProvider2 = services.BuildServiceProvider();

        var bgService = serviceProvider2.GetRequiredService<BackgroundService>();
        bgService.DoWork();

        Console.WriteLine();

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - IOptionsMonitor<T>는 Singleton 라이프사이클을 가집니다");
        Console.WriteLine("   - CurrentValue 속성으로 항상 최신 설정 값을 가져올 수 있습니다");
        Console.WriteLine("   - IOptionsSnapshot<T>는 Scoped이지만, IOptionsMonitor<T>는 Singleton입니다");
        Console.WriteLine("   - 설정 변경 감지 기능(OnChange)을 제공합니다 (다음 예제에서 다룸)");
        Console.WriteLine("   - 백그라운드 서비스나 Singleton 서비스에서 사용하기 적합합니다");
        Console.WriteLine();
    }

    private sealed class BackgroundService
    {
        private readonly IOptionsMonitor<SimpleOptions> _optionsMonitor;

        public BackgroundService(IOptionsMonitor<SimpleOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        public void DoWork()
        {
            // CurrentValue로 항상 최신 값을 가져옵니다
            var options = _optionsMonitor.CurrentValue;
            Console.WriteLine($"  [BackgroundService] Doing work:");
            Console.WriteLine($"    Name: {options.Name}");
            Console.WriteLine($"    Value: {options.Value}");
            Console.WriteLine($"    Enabled: {options.Enabled}");
        }
    }
}
