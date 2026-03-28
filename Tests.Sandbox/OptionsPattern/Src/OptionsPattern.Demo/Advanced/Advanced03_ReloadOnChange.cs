using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Advanced;

/// <summary>
/// Advanced03: 자동 리로드 설정
/// 
/// 학습 목표:
/// - AddOptions().BindConfiguration() 패턴
///   * AddOptions<T>()로 Options를 등록하고, BindConfiguration()으로 설정 파일을 바인딩합니다
///   * 이 패턴은 가장 일반적인 Options 등록 방법입니다
///   * IConfiguration의 변경을 자동으로 Options에 반영합니다
///   * reloadOnChange: true와 함께 사용하면 파일 변경 시 자동으로 리로드됩니다
/// - IConfiguration.GetReloadToken() 사용
///   * ReloadToken은 설정 변경을 감지하는 토큰입니다
///   * ActiveChangeCallbacks 속성으로 콜백 활성화 여부를 확인할 수 있습니다
///   * HasChanged 속성으로 변경 여부를 확인할 수 있습니다
///   * IOptionsMonitor<T>가 내부적으로 이 토큰을 사용하여 변경을 감지합니다
/// - 파일 변경 감지 및 자동 리로드
///   * reloadOnChange: true로 설정하면 파일 시스템 이벤트를 모니터링합니다
///   * 파일이 변경되면 자동으로 IConfiguration을 리로드합니다
///   * IOptionsMonitor<T>가 변경을 감지하고 OnChange 콜백을 호출합니다
///   * CurrentValue가 자동으로 새로운 값으로 업데이트됩니다
/// - reloadOnChange 옵션 이해
///   * reloadOnChange: true - 파일 변경 시 자동 리로드 (프로덕션에서 주의 필요)
///   * reloadOnChange: false - 파일 변경을 감지하지 않음 (기본값, 성능 최적화)
///   * 개발 환경에서는 true로 설정하여 편의성을 높일 수 있습니다
///   * 프로덕션에서는 외부 설정 서비스(Azure App Configuration 등) 사용을 권장합니다
/// </summary>
public static class Advanced03_ReloadOnChange
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Advanced03: Reload on Change Configuration");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // reloadOnChange: true로 설정
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // 중요!
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // BindConfiguration으로 바인딩하면 자동으로 변경 감지가 가능합니다
        services.AddOptions<SimpleOptions>()
            .BindConfiguration(SimpleOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleOptions>>();

        Console.WriteLine("Example 1: Configuration Reload Token");
        Console.WriteLine("─".PadRight(80, '─'));

        // IConfiguration의 ReloadToken을 확인
        var reloadToken = configuration.GetReloadToken();
        Console.WriteLine($"ReloadToken Active: {reloadToken.ActiveChangeCallbacks}");
        Console.WriteLine($"HasChanged: {reloadToken.HasChanged}");
        Console.WriteLine();

        Console.WriteLine("Example 2: Current Value");
        Console.WriteLine("─".PadRight(80, '─'));
        var currentValue = monitor.CurrentValue;
        OptionsViewer.PrintOptions(currentValue, "Current SimpleOptions");

        // OnChange 콜백 등록
        Console.WriteLine("Example 3: Registering OnChange Callback");
        Console.WriteLine("─".PadRight(80, '─'));

        var changeToken = monitor.OnChange(options =>
        {
            Console.WriteLine("  🔔 Configuration reloaded!");
            OptionsViewer.PrintOptions(options, "Updated SimpleOptions");
        });

        Console.WriteLine("✅ OnChange callback registered");
        Console.WriteLine("   If appsettings.json is modified, the callback will be invoked");
        Console.WriteLine();

        Console.WriteLine("Example 4: Manual Reload Simulation");
        Console.WriteLine("─".PadRight(80, '─'));
        Console.WriteLine("In a real application:");
        Console.WriteLine("  1. Modify appsettings.json file");
        Console.WriteLine("  2. IConfiguration detects the change (if reloadOnChange: true)");
        Console.WriteLine("  3. IOptionsMonitor<T> triggers OnChange callbacks");
        Console.WriteLine("  4. CurrentValue returns the new values");
        Console.WriteLine();

        // 정리
        changeToken?.Dispose();

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - reloadOnChange: true를 설정하면 파일 변경을 자동으로 감지합니다");
        Console.WriteLine("   - BindConfiguration()은 IConfiguration의 변경을 Options에 자동으로 반영합니다");
        Console.WriteLine("   - IOptionsMonitor<T>.OnChange()로 변경 사항을 감지할 수 있습니다");
        Console.WriteLine("   - CurrentValue는 항상 최신 설정 값을 반환합니다");
        Console.WriteLine("   - 프로덕션에서는 파일 변경 감지 대신 외부 설정 소스를 사용하는 것이 일반적입니다");
        Console.WriteLine();
    }
}
