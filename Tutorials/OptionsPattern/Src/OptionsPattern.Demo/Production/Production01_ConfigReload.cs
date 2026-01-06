using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Production;

/// <summary>
/// Production01: 설정 파일 변경 감지 및 자동 리로드
/// 
/// 학습 목표:
/// - appsettings.json 파일 변경 감지
///   * reloadOnChange: true로 설정하여 파일 변경을 자동으로 감지합니다
///   * IConfiguration이 파일 시스템 이벤트를 모니터링합니다
///   * 파일이 변경되면 자동으로 설정을 리로드합니다
///   * 프로덕션에서는 외부 설정 서비스 사용을 권장하지만, 개발/테스트 환경에서 유용합니다
/// - IOptionsMonitor<T>.OnChange() 콜백 구현
///   * 여러 Options에 대해 OnChange 콜백을 등록할 수 있습니다
///   * 콜백에서 변경된 설정 값을 로깅하거나 알림을 보낼 수 있습니다
///   * IDisposable을 반환하므로 사용 후 정리해야 합니다
///   * 실제 프로덕션 환경에서 설정 변경을 추적하는 데 사용됩니다
/// - 실시간 설정 업데이트 시뮬레이션
///   * 파일을 수정하여 설정 변경을 시뮬레이션합니다
///   * OnChange 콜백이 호출되는 것을 확인할 수 있습니다
///   * CurrentValue가 자동으로 새로운 값으로 업데이트됩니다
///   * 애플리케이션을 재시작하지 않고도 설정 변경을 반영할 수 있습니다
/// - 파일 감시(FileSystemWatcher) 통합
///   * IConfiguration이 내부적으로 FileSystemWatcher를 사용합니다
///   * 파일 변경 이벤트를 감지하여 자동으로 리로드합니다
///   * 개발 환경에서는 편리하지만, 프로덕션에서는 성능과 보안 문제가 있을 수 있습니다
///   * 프로덕션에서는 Azure App Configuration, AWS Systems Manager 등 사용을 권장합니다
/// - 변경 사항 로깅 및 알림
///   * 설정 변경 시 로그를 기록하여 감사(audit) 목적으로 사용할 수 있습니다
///   * 중요한 설정 변경 시 알림을 보내어 운영팀에 통지할 수 있습니다
///   * 변경 이력을 추적하여 문제 발생 시 원인 분석에 도움이 됩니다
///   * ILogger를 사용하여 구조화된 로깅을 구현합니다
/// </summary>
public static class Production01_ConfigReload
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Production01: Configuration Reload with File Watching");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // 실행 파일이 있는 디렉토리에서 appsettings.json 찾기
        var currentDir = AppContext.BaseDirectory;
        var appSettingsPath = Path.Combine(currentDir, "appsettings.json");
        
        if (!File.Exists(appSettingsPath))
        {
            // 빌드 출력 디렉토리가 아닌 경우 프로젝트 디렉토리에서 찾기
            var projectDir = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
            appSettingsPath = Path.Combine(projectDir, "Src", "OptionsPattern.Demo", "appsettings.json");
        }
        var originalContent = File.ReadAllText(appSettingsPath);

        try
        {
            // reloadOnChange: true로 설정
            var basePath = Path.GetDirectoryName(appSettingsPath) ?? Directory.GetCurrentDirectory();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddConsole());

            services.AddOptions<ApiClientOptions>()
                .BindConfiguration(ApiClientOptions.SectionName);

            services.AddOptions<DatabaseOptions>()
                .BindConfiguration(DatabaseOptions.SectionName);

            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Production01_ConfigReload");

            var apiMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ApiClientOptions>>();
            var dbMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<DatabaseOptions>>();

            Console.WriteLine("Initial Configuration:");
            Console.WriteLine("─".PadRight(80, '─'));
            OptionsViewer.PrintOptions(apiMonitor.CurrentValue, "ApiClientOptions");
            OptionsViewer.PrintOptions(dbMonitor.CurrentValue, "DatabaseOptions");
            Console.WriteLine();

            // OnChange 콜백 등록
            Console.WriteLine("Registering Change Detection Callbacks...");
            Console.WriteLine("─".PadRight(80, '─'));

            var apiChangeToken = apiMonitor.OnChange(options =>
            {
                logger.LogInformation("🔔 ApiClientOptions changed!");
                logger.LogInformation("   BaseUrl: {BaseUrl}", options.BaseUrl);
                logger.LogInformation("   TimeoutSeconds: {TimeoutSeconds}", options.TimeoutSeconds);
                logger.LogInformation("   MaxRetries: {MaxRetries}", options.MaxRetries);
            });

            var dbChangeToken = dbMonitor.OnChange(options =>
            {
                logger.LogInformation("🔔 DatabaseOptions changed!");
                logger.LogInformation("   ConnectionTimeout: {Timeout}", options.ConnectionTimeout);
                logger.LogInformation("   RetryCount: {Retries}", options.RetryCount);
                logger.LogInformation("   MaxPoolSize: {PoolSize}", options.MaxPoolSize);
            });

            Console.WriteLine("✅ Change detection callbacks registered");
            Console.WriteLine();

            // 설정 변경 시뮬레이션
            Console.WriteLine("Simulating Configuration Changes:");
            Console.WriteLine("─".PadRight(80, '─'));
            Console.WriteLine("Change 1: Updating ApiClientOptions...");
            Console.WriteLine();

            // appsettings.json 수정
            var modifiedContent1 = originalContent.Replace(
                "\"TimeoutSeconds\": 30",
                "\"TimeoutSeconds\": 60"
            ).Replace(
                "\"MaxRetries\": 3",
                "\"MaxRetries\": 5"
            );

            File.WriteAllText(appSettingsPath, modifiedContent1);
            Thread.Sleep(500); // 파일 시스템 이벤트 처리 대기

            Console.WriteLine("After Change 1:");
            OptionsViewer.PrintOptions(apiMonitor.CurrentValue, "ApiClientOptions");
            Console.WriteLine();

            Console.WriteLine("Change 2: Updating DatabaseOptions...");
            Console.WriteLine();

            var modifiedContent2 = modifiedContent1.Replace(
                "\"ConnectionTimeout\": 30",
                "\"ConnectionTimeout\": 60"
            ).Replace(
                "\"RetryCount\": 3",
                "\"RetryCount\": 5"
            );

            File.WriteAllText(appSettingsPath, modifiedContent2);
            Thread.Sleep(500); // 파일 시스템 이벤트 처리 대기

            Console.WriteLine("After Change 2:");
            OptionsViewer.PrintOptions(dbMonitor.CurrentValue, "DatabaseOptions");
            Console.WriteLine();

            // 원본으로 복원
            Console.WriteLine("Restoring original configuration...");
            File.WriteAllText(appSettingsPath, originalContent);
            Thread.Sleep(500);

            Console.WriteLine("After Restore:");
            OptionsViewer.PrintOptions(apiMonitor.CurrentValue, "ApiClientOptions");
            OptionsViewer.PrintOptions(dbMonitor.CurrentValue, "DatabaseOptions");
            Console.WriteLine();

            // 정리
            apiChangeToken?.Dispose();
            dbChangeToken?.Dispose();

            Console.WriteLine("💡 Production Best Practices:");
            Console.WriteLine("   1. Use IOptionsMonitor<T> for settings that may change at runtime");
            Console.WriteLine("   2. Register OnChange callbacks to react to configuration changes");
            Console.WriteLine("   3. Log configuration changes for audit purposes");
            Console.WriteLine("   4. Handle exceptions in OnChange callbacks gracefully");
            Console.WriteLine("   5. Consider using external configuration sources (Azure App Configuration, etc.)");
            Console.WriteLine("   6. Use FileSystemWatcher only for development/testing scenarios");
            Console.WriteLine("   7. In production, prefer configuration services with built-in change detection");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine();
        }
        finally
        {
            // 원본 파일 복원
            try
            {
                File.WriteAllText(appSettingsPath, originalContent);
            }
            catch
            {
                // 무시
            }
        }
    }
}
