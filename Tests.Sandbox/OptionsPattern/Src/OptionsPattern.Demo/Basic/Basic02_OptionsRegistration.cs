using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Basic;

/// <summary>
/// Basic02: Options 등록 방법
/// 
/// 학습 목표:
/// - AddOptions<T>() 사용법
///   * AddOptions<T>()는 OptionsBuilder<T>를 반환하여 체이닝 방식으로 설정을 구성할 수 있습니다
///   * IOptions<T>, IOptionsSnapshot<T>, IOptionsMonitor<T>를 모두 등록합니다
///   * 기본적으로 Singleton으로 등록되며, 설정 값은 애플리케이션 시작 시 한 번만 로드됩니다
/// - Configure<T>() 패턴의 다양한 방법
///   * 인라인 람다: Configure(options => { ... }) - 간단한 설정에 적합
///   * 외부 함수: Configure(ConfigureOptions) - 재사용 가능한 설정 로직
///   * 여러 Configure 호출: 체이닝하여 순차적으로 설정 적용
///   * PostConfigure<T>(): 모든 Configure 이후에 실행되는 후처리
/// - 여러 등록 방법 비교
///   * Configure<T>()는 여러 번 호출 가능하며, 순서대로 실행됩니다
///   * 마지막 Configure의 값이 최종 설정 값이 됩니다 (덮어쓰기)
///   * PostConfigure<T>()는 모든 Configure 이후에 실행되어 최종 검증이나 변환에 사용됩니다
/// </summary>
public static class Basic02_OptionsRegistration
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic02: Options Registration Methods");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // 방법 1: Configure<T>()로 직접 설정
        Console.WriteLine("Method 1: Configure<T>() with inline configuration");
        Console.WriteLine("─".PadRight(80, '─'));
        {
            var services = new ServiceCollection();
            services.AddOptions<SimpleOptions>()
                .Configure(options =>
                {
                    options.Name = "Method1";
                    options.Value = 1;
                    options.Enabled = true;
                });

            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<SimpleOptions>>();
            OptionsViewer.PrintOptions(options.Value, "Method 1");
        }

        // 방법 2: Configure<T>()로 외부 함수 사용
        Console.WriteLine("Method 2: Configure<T>() with external function");
        Console.WriteLine("─".PadRight(80, '─'));
        {
            var services = new ServiceCollection();
            services.AddOptions<SimpleOptions>()
                .Configure(ConfigureSimpleOptions);

            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<SimpleOptions>>();
            OptionsViewer.PrintOptions(options.Value, "Method 2");
        }

        // 방법 3: 여러 Configure 호출 (체이닝)
        Console.WriteLine("Method 3: Multiple Configure calls (chaining)");
        Console.WriteLine("─".PadRight(80, '─'));
        {
            var services = new ServiceCollection();
            services.AddOptions<SimpleOptions>()
                .Configure(options => options.Name = "Method3")
                .Configure(options => options.Value = 3)
                .Configure(options => options.Enabled = true);

            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<SimpleOptions>>();
            OptionsViewer.PrintOptions(options.Value, "Method 3");
        }

        // 방법 4: PostConfigure 사용 (다른 Configure 이후 실행)
        Console.WriteLine("Method 4: PostConfigure (runs after Configure)");
        Console.WriteLine("─".PadRight(80, '─'));
        {
            var services = new ServiceCollection();
            services.AddOptions<SimpleOptions>()
                .Configure(options =>
                {
                    options.Name = "Method4";
                    options.Value = 4;
                })
                .PostConfigure(options =>
                {
                    // PostConfigure는 다른 Configure 이후에 실행됩니다
                    options.Enabled = options.Value > 0;
                });

            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<SimpleOptions>>();
            OptionsViewer.PrintOptions(options.Value, "Method 4");
        }

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - Configure<T>()는 여러 번 호출 가능하며 순서대로 실행됩니다");
        Console.WriteLine("   - PostConfigure<T>()는 모든 Configure 이후에 실행됩니다");
        Console.WriteLine("   - 외부 함수를 사용하면 재사용 가능한 설정 로직을 만들 수 있습니다");
        Console.WriteLine();
    }

    private static void ConfigureSimpleOptions(SimpleOptions options)
    {
        options.Name = "Method2";
        options.Value = 2;
        options.Enabled = true;
    }
}
