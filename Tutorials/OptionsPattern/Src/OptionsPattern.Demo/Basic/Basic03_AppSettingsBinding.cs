using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsPattern.Demo.Shared;

namespace OptionsPattern.Demo.Basic;

/// <summary>
/// Basic03: appsettings.json 바인딩
/// 
/// 학습 목표:
/// - BindConfiguration() 사용법
///   * BindConfiguration(sectionName)은 IConfiguration의 특정 섹션을 Options에 바인딩합니다
///   * SectionName 상수를 사용하여 섹션 이름을 지정하는 것이 일반적입니다
///   * appsettings.json의 구조가 Options 클래스의 속성과 일치해야 합니다
///   * 자동으로 속성 이름과 JSON 키를 매칭합니다 (대소문자 구분 없음)
/// - appsettings.json에서 설정 읽기
///   * ConfigurationBuilder를 사용하여 appsettings.json 파일을 로드합니다
///   * SetBasePath()로 기본 경로를 설정하고, AddJsonFile()로 JSON 파일을 추가합니다
///   * optional: false로 설정하면 파일이 없을 때 예외가 발생합니다
///   * reloadOnChange: true로 설정하면 파일 변경 시 자동으로 리로드됩니다
/// - 중첩 설정 구조 바인딩
///   * JSON의 중첩 객체는 Options 클래스의 중첩 속성으로 자동 바인딩됩니다
///   * 배열이나 리스트도 자동으로 바인딩됩니다
///   * 복잡한 구조도 타입이 일치하면 자동으로 매핑됩니다
/// - IConfiguration과 Options 패턴 통합
///   * IConfiguration은 유연하지만 약타입입니다 (string 키 사용)
///   * Options 패턴은 강타입이지만 IConfiguration과 통합하여 설정 파일을 읽을 수 있습니다
///   * BindConfiguration()을 통해 IConfiguration의 값을 Options 클래스로 안전하게 변환합니다
/// </summary>
public static class Basic03_AppSettingsBinding
{
    public static void Run()
    {
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("Basic03: AppSettings Binding");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine();

        // 1. IConfiguration 빌드
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // 2. 서비스 컬렉션에 IConfiguration 등록
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // 3. BindConfiguration()으로 appsettings.json 바인딩
        // SectionName 상수를 사용하여 섹션 지정
        services.AddOptions<SimpleOptions>()
            .BindConfiguration(SimpleOptions.SectionName);

        // 4. 다른 Options도 바인딩
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName);

        services.AddOptions<ApiClientOptions>()
            .BindConfiguration(ApiClientOptions.SectionName);

        var serviceProvider = services.BuildServiceProvider();

        // 5. Options 값 확인
        Console.WriteLine("SimpleOptions from appsettings.json:");
        var simpleOptions = serviceProvider.GetRequiredService<IOptions<SimpleOptions>>();
        OptionsViewer.PrintOptions(simpleOptions.Value, "SimpleOptions");

        Console.WriteLine("DatabaseOptions from appsettings.json:");
        var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>();
        OptionsViewer.PrintOptions(dbOptions.Value, "DatabaseOptions");

        Console.WriteLine("ApiClientOptions from appsettings.json:");
        var apiOptions = serviceProvider.GetRequiredService<IOptions<ApiClientOptions>>();
        OptionsViewer.PrintOptions(apiOptions.Value, "ApiClientOptions");

        Console.WriteLine("💡 Key Points:");
        Console.WriteLine("   - BindConfiguration()은 IConfiguration의 특정 섹션을 Options에 바인딩합니다");
        Console.WriteLine("   - SectionName 상수를 사용하여 섹션 이름을 지정합니다");
        Console.WriteLine("   - appsettings.json의 구조가 Options 클래스의 속성과 일치해야 합니다");
        Console.WriteLine("   - 중첩된 객체도 자동으로 바인딩됩니다");
        Console.WriteLine();
    }
}
