using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Functorium.Testing.Arrangements.Controllers;

/// <summary>
/// HTTP Controller 통합 테스트를 위한 제네릭 Fixture
/// WebApplicationFactory를 사용하여 전체 DI 설정을 재사용합니다.
///
/// 설정 파일 로드 순서:
/// 1. TProgram 프로젝트의 appsettings.json (기본 설정)
/// 2. 테스트 프로젝트의 appsettings.json (출력 디렉토리에 복사됨, 기존 설정 덮어씀)
/// </summary>
/// <typeparam name="TProgram">테스트할 애플리케이션의 Program 클래스</typeparam>
public class ControllerTestFixture<TProgram> : IAsyncLifetime where TProgram : class
{
    private WebApplicationFactory<TProgram>? _factory;

    /// <summary>
    /// 사용할 환경 이름 (기본값: Test)
    /// 파생 클래스에서 override하여 다른 환경을 사용할 수 있습니다.
    /// appsettings.{EnvironmentName}.json 파일이 로드됩니다.
    /// </summary>
    protected virtual string EnvironmentName => "Test";

    public IServiceProvider Services => _factory?.Services
        ?? throw new InvalidOperationException("Fixture not initialized");

    public HttpClient Client { get; private set; } = null!;

    public virtual ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<TProgram>()
            .WithWebHostBuilder(builder =>
            {
                // 환경 설정 (appsettings.{Environment}.json 자동 로드)
                builder.UseEnvironment(EnvironmentName);

                // 테스트 프로젝트의 ContentRoot 설정 (appsettings 파일 위치)
                builder.UseContentRoot(GetTestProjectPath());

                // 추가 설정 적용
                ConfigureWebHost(builder);
            });

        // 앱 시작
        Client = _factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 테스트 프로젝트 경로를 반환합니다.
    /// bin/Debug/net10.0 에서 실행되므로 3단계 상위 디렉토리가 프로젝트 경로입니다.
    /// 파생 클래스에서 오버라이드하여 다른 경로를 지정할 수 있습니다.
    /// </summary>
    protected virtual string GetTestProjectPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var projectPath = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", ".."));
        return projectPath;
    }

    /// <summary>
    /// WebHost 추가 설정을 위한 확장 포인트
    /// 파생 클래스에서 오버라이드하여 추가 설정을 적용할 수 있습니다.
    /// </summary>
    protected virtual void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 기본 구현은 비어있음 - 파생 클래스에서 오버라이드
    }

    public virtual async ValueTask DisposeAsync()
    {
        // HttpClient 정리
        Client?.Dispose();

        // WebApplicationFactory 정리
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}
