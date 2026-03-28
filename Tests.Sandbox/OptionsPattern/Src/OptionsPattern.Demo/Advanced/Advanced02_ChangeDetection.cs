using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Advanced;

/// <summary>
/// Advanced02: 변경 감지 콜백
/// 
/// 학습 목표:
/// - OnChange() 이벤트 사용법
///   * OnChange(Action<TOptions> callback) 메서드로 설정 변경 콜백을 등록합니다
///   * 콜백은 IDisposable을 반환하므로, 사용 후 Dispose()를 호출해야 합니다
///   * 여러 OnChange 콜백을 등록할 수 있으며, 모두 순차적으로 호출됩니다
///   * 콜백은 설정이 실제로 변경될 때만 호출됩니다 (값이 같으면 호출되지 않음)
/// - 변경 감지 시나리오
///   * appsettings.json 파일이 수정되면 IConfiguration이 변경을 감지합니다
///   * IOptionsMonitor<T>가 변경을 감지하고 OnChange 콜백을 호출합니다
///   * 콜백에서 새로운 Options 값을 받아 처리할 수 있습니다
///   * reloadOnChange: true로 설정되어 있어야 파일 변경을 감지할 수 있습니다
/// - 콜백에서 주의사항
///   * 콜백은 동기적으로 실행되므로, 무거운 작업을 하면 안 됩니다
///   * 비동기 작업이 필요하면 Task.Run()을 사용하거나 별도의 큐에 넣어야 합니다
///   * 콜백 내에서 예외가 발생하면 다른 콜백에 영향을 주지 않습니다
///   * 콜백은 등록된 순서대로 호출되지만, 실행 순서는 보장되지 않습니다
/// - 설정 변경 시 자동 처리
///   * 설정 변경 시 로깅, 알림, 캐시 무효화 등을 자동으로 처리할 수 있습니다
///   * CurrentValue가 자동으로 새로운 값으로 업데이트됩니다
///   * 다음 CurrentValue 호출부터 새로운 값이 반환됩니다
///   * 애플리케이션을 재시작하지 않고도 설정 변경을 반영할 수 있습니다
/// </summary>
public static class Advanced02_ChangeDetection
{
    private static int _changeCount = 0;

    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced02: Change Detection with OnChange()");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // 중요: reloadOnChange: true
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<SimpleOptions>()
            .BindConfiguration(SimpleOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();

        Console.WriteLine("Example 1: Registering OnChange Callback");
        Console.WriteLine("─".PadRight(80, '─'));

        // OnChange 콜백 등록
        IDisposable? changeToken = null;
        changeToken = monitor.OnChange(options =>
        {
            _changeCount++;
            Console.WriteLine($"  🔔 Options changed! (Change #{_changeCount})");
            Console.WriteLine($"     Name: {options.Name}");
            Console.WriteLine($"     Value: {options.Value}");
            Console.WriteLine($"     Enabled: {options.Enabled}");
            Console.WriteLine();
        });

        Console.WriteLine("✅ OnChange callback registered");
        Console.WriteLine($"   Current value: Name={monitor.CurrentValue.Name}, Value={monitor.CurrentValue.Value}");
        Console.WriteLine();

        Console.WriteLine("Example 2: Simulating Configuration Changes");
        Console.WriteLine("─".PadRight(80, '─'));
        Console.WriteLine("Note: In a real scenario, changes would come from:");
        Console.WriteLine("  - appsettings.json file modification");
        Console.WriteLine("  - Configuration reload (IConfiguration.GetReloadToken())");
        Console.WriteLine("  - External configuration source updates");
        Console.WriteLine();

        // 실제 파일 변경은 시뮬레이션하기 어려우므로
        // OnChange가 어떻게 동작하는지 설명
        Console.WriteLine("💡 How OnChange Works:");
        Console.WriteLine("   1. IConfiguration이 reloadOnChange: true로 설정되어야 합니다");
        Console.WriteLine("   2. 설정 파일이 변경되면 IConfiguration이 자동으로 리로드됩니다");
        Console.WriteLine("   3. IOptionsMonitor<T>가 변경을 감지하고 OnChange 콜백을 호출합니다");
        Console.WriteLine("   4. 콜백에서 새로운 Options 값을 받아 처리할 수 있습니다");
        Console.WriteLine();

        // 콜백 해제 (실제로는 애플리케이션 종료 시)
        changeToken?.Dispose();

        Console.WriteLine("Example 3: Multiple OnChange Callbacks");
        Console.WriteLine("─".PadRight(80, '─'));

        var monitor2 = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();
        int callback1Count = 0;
        int callback2Count = 0;

        var token1 = monitor2.OnChange(options =>
        {
            callback1Count++;
            Console.WriteLine($"  [Callback 1] Change detected: Name={options.Name}");
        });

        var token2 = monitor2.OnChange(options =>
        {
            callback2Count++;
            Console.WriteLine($"  [Callback 2] Change detected: Value={options.Value}");
        });

        Console.WriteLine("✅ Multiple callbacks registered");
        Console.WriteLine("   Both callbacks will be invoked when options change");
        Console.WriteLine();

        // 정리
        token1?.Dispose();
        token2?.Dispose();

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - OnChange()는 IDisposable을 반환하므로 해제해야 합니다");
        Console.WriteLine("   - 여러 OnChange 콜백을 등록할 수 있습니다");
        Console.WriteLine("   - 콜백은 설정이 실제로 변경될 때만 호출됩니다");
        Console.WriteLine("   - 콜백 내에서 예외가 발생하면 다른 콜백에 영향을 주지 않습니다");
        Console.WriteLine("   - 콜백에서 무거운 작업을 하면 안 됩니다 (비동기 처리 고려)");
        Console.WriteLine();
    }
}
