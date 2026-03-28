using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Basic;

/// <summary>
/// Basic01: IOptions<T> 기본 사용법
/// 
/// 학습 목표:
/// - IOptions<T>가 무엇인지 이해
///   * IOptions<T>는 .NET의 Options 패턴을 구현하는 기본 인터페이스입니다
///   * 강타입 설정 관리를 위한 래퍼 인터페이스로, 설정 값을 안전하게 접근할 수 있게 합니다
///   * Singleton 라이프사이클을 가지며, 애플리케이션 시작 시 한 번만 생성됩니다
/// - Options 클래스를 DI 컨테이너에 등록
///   * AddOptions<T>() 메서드를 사용하여 Options 클래스를 DI 컨테이너에 등록합니다
///   * Configure<T>() 메서드로 코드에서 직접 설정 값을 지정할 수 있습니다
///   * 여러 Configure 호출을 체이닝하여 순차적으로 설정을 적용할 수 있습니다
/// - IOptions<T>를 주입받아 사용
///   * 생성자 주입을 통해 IOptions<T>를 서비스에 주입받을 수 있습니다
///   * GetRequiredService<IOptions<T>>()를 사용하여 직접 가져올 수도 있습니다
///   * DI 컨테이너가 자동으로 IOptions<T> 인스턴스를 생성하고 관리합니다
/// - Value 속성으로 설정 값 접근
///   * Value 속성을 통해 실제 설정 값(TOptions)에 접근할 수 있습니다
///   * Value는 읽기 전용이며, 설정이 변경되어도 자동으로 갱신되지 않습니다
///   * 애플리케이션 시작 시 한 번만 로드되므로, 런타임에 변경되는 설정에는 부적합합니다
/// </summary>
public static class Basic01_SimpleOptions
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic01: Simple Options (IOptions<T>)");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // 1. 서비스 컬렉션 생성
        var services = new ServiceCollection();

        // 2. Options 등록
        // AddOptions<T>()는 IOptions<T>를 등록합니다
        services.AddOptions<SimpleOptions>()
            .Configure(options =>
            {
                // 코드에서 직접 설정 값 지정
                options.Name = "MyApplication";
                options.Value = 100;
                options.Enabled = true;
            });

        // 3. 서비스 프로바이더 빌드
        var serviceProvider = services.BuildServiceProvider();

        // 4. IOptions<T> 주입받기
        var options = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();

        // 5. Value 속성으로 설정 값 접근
        Console.WriteLine("Options Values:");
        Console.WriteLine($"  Name: {options.Value.Name}");
        Console.WriteLine($"  Value: {options.Value.Value}");
        Console.WriteLine($"  Enabled: {options.Value.Enabled}");
        Console.WriteLine();

        // 6. OptionsViewer를 사용한 출력
        OptionsViewer.PrintOptions(options.Value, "SimpleOptions");

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - IOptions<T>는 Singleton으로 등록됩니다");
        Console.WriteLine("   - 애플리케이션 시작 시 한 번만 생성되고 변경되지 않습니다");
        Console.WriteLine("   - Value 속성을 통해 설정 값에 접근합니다");
        Console.WriteLine("   - Configure<T>()로 코드에서 직접 설정할 수 있습니다");
        Console.WriteLine();
    }
}
