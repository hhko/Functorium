using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Functorium.Testing.Arrangements.ScheduledJobs;

/// <summary>
/// Quartz Job 통합 테스트를 위한 제네릭 Fixture
/// WebApplicationFactory를 사용하여 전체 DI 설정을 재사용합니다.
///
/// 설정 파일 로드 순서:
/// 1. TProgram 프로젝트의 appsettings.json (기본 설정)
/// 2. 테스트 프로젝트의 appsettings.json (출력 디렉토리에 복사됨, 기존 설정 덮어씀)
/// </summary>
/// <typeparam name="TProgram">테스트할 애플리케이션의 Program 클래스</typeparam>
public class QuartzTestFixture<TProgram> : IAsyncLifetime where TProgram : class
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

    public JobCompletionListener JobListener { get; } = new();

    public IScheduler Scheduler { get; private set; } = null!;

    public virtual async ValueTask InitializeAsync()
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

        // 앱 시작 (QuartzHostedService 자동 시작)
        _ = _factory.CreateClient();

        // 스케줄러 획득
        var schedulerFactory = _factory.Services.GetRequiredService<ISchedulerFactory>();
        Scheduler = await schedulerFactory.GetScheduler();

        // JobListener 등록
        Scheduler.ListenerManager.AddJobListener(JobListener);
    }

    /// <summary>
    /// 테스트 프로젝트 경로를 반환합니다.
    /// bin/Debug/net10.0 에서 실행되므로 3단계 상위 디렉토리가 프로젝트 경로입니다.
    /// 파생 클래스에서 오버라이드하여 다른 경로를 지정할 수 있습니다.
    /// </summary>
    protected virtual string GetTestProjectPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        // /bin/Debug/net1.0: ./../../..
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

    /// <summary>
    /// 지정된 Job을 즉시 1회 실행하고 완료를 대기합니다.
    /// Job 이름과 그룹은 TJob 타입에서 자동으로 추출됩니다.
    /// </summary>
    /// <typeparam name="TJob">실행할 Job 타입</typeparam>
    /// <param name="timeout">최대 대기 시간</param>
    /// <returns>Job 실행 결과</returns>
    public Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(TimeSpan timeout) where TJob : IJob
    {
        var jobType = typeof(TJob);
        var jobName = jobType.Name;
        var jobGroup = jobType.Namespace?.Split('.').LastOrDefault() ?? "Default";

        return ExecuteJobOnceAsync<TJob>(jobName, jobGroup, timeout);
    }

    /// <summary>
    /// 지정된 Job을 즉시 1회 실행하고 완료를 대기합니다.
    /// </summary>
    /// <typeparam name="TJob">실행할 Job 타입</typeparam>
    /// <param name="jobName">Job 이름</param>
    /// <param name="jobGroup">Job 그룹</param>
    /// <param name="timeout">최대 대기 시간</param>
    /// <returns>Job 실행 결과</returns>
    public async Task<JobExecutionResult> ExecuteJobOnceAsync<TJob>(
        string jobName,
        string jobGroup,
        TimeSpan timeout) where TJob : IJob
    {
        // 상태 초기화
        JobListener.Reset();

        // 테스트용 고유 이름 생성
        var testJobName = $"{jobName}-Test-{Guid.NewGuid():N}";

        // Job 정의
        var jobDetail = JobBuilder.Create<TJob>()
            .WithIdentity(testJobName, jobGroup)
            .Build();

        // SimpleTrigger로 즉시 1회 실행
        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{testJobName}-Trigger", jobGroup)
            .StartNow()
            .WithSimpleSchedule(x => x.WithRepeatCount(0))
            .Build();

        // Job 스케줄링
        await Scheduler.ScheduleJob(jobDetail, trigger);

        // 완료 대기
        return await JobListener.WaitForJobCompletionAsync(testJobName, timeout);
    }

    public virtual async ValueTask DisposeAsync()
    {
        // 스케줄러 종료
        if (Scheduler is { IsShutdown: false })
        {
            await Scheduler.Shutdown(waitForJobsToComplete: true);
        }

        // WebApplicationFactory 정리
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}
