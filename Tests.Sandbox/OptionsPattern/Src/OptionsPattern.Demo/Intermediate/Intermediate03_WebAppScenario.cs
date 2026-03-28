using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Intermediate;

/// <summary>
/// Intermediate03: 웹 애플리케이션 시나리오
/// 
/// 학습 목표:
/// - 컨트롤러/서비스에서 IOptionsSnapshot<T> 사용
///   * 생성자 주입을 통해 IOptionsSnapshot<T>를 주입받습니다
///   * 각 HTTP 요청마다 새로운 스냅샷이 생성되므로, 요청별로 일관된 설정 값을 보장합니다
///   * 여러 서비스에서 같은 IOptionsSnapshot<T>를 주입받아도 같은 요청 내에서는 동일한 값을 가집니다
///   * 웹 애플리케이션에서 가장 일반적으로 사용되는 패턴입니다
/// - 요청별 다른 설정 값 처리
///   * 각 요청은 독립적인 스코프를 가지므로, 서로 다른 설정 값을 가질 수 있습니다
///   * 요청 처리 중간에 설정이 변경되면, 현재 요청에는 영향을 주지 않습니다
///   * 다음 요청부터 새로운 설정 값이 적용됩니다
///   * 요청별로 일관된 설정 값을 보장할 수 있어 안정적입니다
/// - 실제 웹 애플리케이션 패턴
///   * UserService, ProductService, OrderService 등 여러 서비스에서 Options를 사용합니다
///   * 각 서비스는 IOptionsSnapshot<T>를 주입받아 사용합니다
///   * 같은 요청 내의 모든 서비스는 동일한 설정 스냅샷을 공유합니다
///   * 실제 프로덕션 환경에서 사용되는 패턴을 시뮬레이션합니다
/// </summary>
public static class Intermediate03_WebAppScenario
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Intermediate03: Web Application Scenario");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName);
        services.AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.SectionName);

        // 웹 애플리케이션의 서비스들
        services.AddScoped<UserService>();
        services.AddScoped<ProductService>();
        services.AddScoped<OrderService>();

        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("Simulating Web Application Requests:");
        Console.WriteLine("─".PadRight(80, '─'));

        // 여러 사용자 요청 시뮬레이션
        var userIds = new[] { "user-1", "user-2", "user-3" };

        foreach (var userId in userIds)
        {
            Console.WriteLine($"\nProcessing request for {userId}:");
            using var scope = serviceProvider.CreateScope();
            
            // 각 요청마다 새로운 스코프가 생성되므로
            // IOptionsSnapshot<T>는 해당 요청의 스냅샷을 반환합니다
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            var productService = scope.ServiceProvider.GetRequiredService<ProductService>();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            userService.GetUser(userId);
            productService.GetProducts();
            orderService.CreateOrder(userId);
        }

        Console.WriteLine();

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - 웹 애플리케이션에서 IOptionsSnapshot<T>는 각 HTTP 요청마다 새로운 스냅샷을 제공합니다");
        Console.WriteLine("   - 같은 요청 내에서는 동일한 스냅샷 인스턴스를 공유합니다");
        Console.WriteLine("   - 요청 처리 중 설정이 변경되면 다음 요청부터 반영됩니다");
        Console.WriteLine("   - 컨트롤러, 서비스 등에서 IOptionsSnapshot<T>를 주입받아 사용합니다");
        Console.WriteLine();
    }

    // 웹 애플리케이션의 서비스들 (시뮬레이션)
    private sealed class UserService
    {
        private readonly IOptionsSnapshot<DatabaseOptions> _dbOptions;

        public UserService(IOptionsSnapshot<DatabaseOptions> dbOptions)
        {
            _dbOptions = dbOptions;
        }

        public void GetUser(string userId)
        {
            var options = _dbOptions.Value;
            Console.WriteLine($"  [UserService] Getting user {userId}");
            Console.WriteLine($"    Using DB: {options.ConnectionString[..20]}...");
            Console.WriteLine($"    Timeout: {options.ConnectionTimeout}s");
        }
    }

    private sealed class ProductService
    {
        private readonly IOptionsSnapshot<CacheOptions> _cacheOptions;

        public ProductService(IOptionsSnapshot<CacheOptions> cacheOptions)
        {
            _cacheOptions = cacheOptions;
        }

        public void GetProducts()
        {
            var options = _cacheOptions.Value;
            Console.WriteLine($"  [ProductService] Getting products");
            Console.WriteLine($"    Cache Type: {options.CacheType}");
            Console.WriteLine($"    Expiration: {options.DefaultExpirationMinutes} minutes");
        }
    }

    private sealed class OrderService
    {
        private readonly IOptionsSnapshot<DatabaseOptions> _dbOptions;
        private readonly IOptionsSnapshot<CacheOptions> _cacheOptions;

        public OrderService(
            IOptionsSnapshot<DatabaseOptions> dbOptions,
            IOptionsSnapshot<CacheOptions> cacheOptions)
        {
            _dbOptions = dbOptions;
            _cacheOptions = cacheOptions;
        }

        public void CreateOrder(string userId)
        {
            var dbOptions = _dbOptions.Value;
            var cacheOptions = _cacheOptions.Value;
            Console.WriteLine($"  [OrderService] Creating order for {userId}");
            Console.WriteLine($"    DB Retry Count: {dbOptions.RetryCount}");
            Console.WriteLine($"    Cache Max Size: {cacheOptions.MaxSize}");
        }
    }
}
