---
title: "어댑터 코드 설계"
description: "4가지 IO 고급 패턴 코드 스니펫과 DI 등록 패턴"
---

[타입 설계 의사결정](../01-type-design-decisions/)에서 선택한 4가지 IO 패턴을 C# 코드로 구현합니다.

## 1. Timeout + Catch -- ModelHealthCheckService

10초 타임아웃 + 타임아웃 폴백 + 예외 변환 패턴입니다.

```csharp
[GenerateObservablePort]
public class ModelHealthCheckService : IModelHealthCheckService
{
    public sealed record HealthCheckFailed : AdapterErrorType.Custom;
    public sealed record HealthCheckTimedOut : AdapterErrorType.Custom;

    public virtual FinT<IO, HealthCheckResult> CheckHealth(
        ModelDeploymentId deploymentId)
    {
        var io = IO.liftAsync<HealthCheckResult>(async env =>
            {
                // 네트워크 지연 시뮬레이션 (50~300ms, 10%에서 12초)
                var delay = _random.Next(100) < 10
                    ? TimeSpan.FromSeconds(12)
                    : TimeSpan.FromMilliseconds(_random.Next(50, 300));
                await Task.Delay(delay, env.Token);

                var isHealthy = _random.Next(100) < 85;
                return new HealthCheckResult(
                    IsHealthy: isHealthy,
                    Status: isHealthy ? "Healthy" : "Degraded",
                    ErrorMessage: isHealthy
                        ? Option<string>.None
                        : Some("Model response latency exceeds threshold"),
                    CheckedAt: DateTimeOffset.UtcNow);
            })
            .Timeout(TimeSpan.FromSeconds(10))
            .Catch(
                e => e.Is(Errors.TimedOut),
                _ => IO.pure(new HealthCheckResult(
                    IsHealthy: false,
                    Status: "TimedOut",
                    ErrorMessage: Some("Health check timed out after 10 seconds"),
                    CheckedAt: DateTimeOffset.UtcNow)))
            .Catch(
                e => e.IsExceptional,
                e => IO.fail<HealthCheckResult>(
                    AdapterError.FromException<ModelHealthCheckService>(
                        new HealthCheckFailed(),
                        e.ToException())));

        return new FinT<IO, HealthCheckResult>(io.Map(Fin.Succ));
    }
}
```

**핵심 포인트:**
- `IO.liftAsync`: 비동기 작업을 IO 모나드로 리프팅
- `.Timeout(10s)`: IO 연산에 시간 제한 부여, 초과 시 `Errors.TimedOut` 발생
- `.Catch(e => e.Is(Errors.TimedOut), ...)`: 타임아웃을 오류가 아닌 폴백 결과로 변환
- `.Catch(e => e.IsExceptional, ...)`: 그 외 예외를 `AdapterError`로 변환
- Catch 순서가 중요: 구체적인 조건(TimedOut)을 먼저, 일반적인 조건(IsExceptional)을 나중에

## 2. Retry + Schedule -- ModelMonitoringService

지수 백오프 + 지터 + 최대 3회 재시도 패턴입니다.

```csharp
[GenerateObservablePort]
public class ModelMonitoringService : IModelMonitoringService
{
    public sealed record MonitoringFailed : AdapterErrorType.Custom;

    private static readonly Schedule RetrySchedule =
        Schedule.exponential(TimeSpan.FromMilliseconds(100))
        | Schedule.jitter(0.3)
        | Schedule.recurs(3)
        | Schedule.maxDelay(TimeSpan.FromSeconds(5));

    public virtual FinT<IO, DriftReport> GetDriftReport(
        ModelDeploymentId deploymentId)
    {
        var attemptCount = 0;

        var io = IO.liftAsync<DriftReport>(async env =>
            {
                Interlocked.Increment(ref attemptCount);

                await Task.Delay(
                    TimeSpan.FromMilliseconds(_random.Next(50, 200)),
                    env.Token);

                // 처음 두 번은 60% 실패, 이후 10% 실패
                var failRate = attemptCount <= 2 ? 60 : 10;
                if (_random.Next(100) < failRate)
                    throw new InvalidOperationException(
                        $"Monitoring service temporarily unavailable " +
                        $"(attempt {attemptCount})");

                var drift = (decimal)(_random.NextDouble() * 0.5);
                return new DriftReport(
                    CurrentDrift: drift,
                    Threshold: 0.3m,
                    IsDrifting: drift > 0.3m,
                    ReportedAt: DateTimeOffset.UtcNow);
            })
            .Retry(RetrySchedule)
            .Catch(
                e => e.IsExceptional,
                e => IO.fail<DriftReport>(
                    AdapterError.FromException<ModelMonitoringService>(
                        new MonitoringFailed(),
                        e.ToException())));

        return new FinT<IO, DriftReport>(io.Map(Fin.Succ));
    }
}
```

**핵심 포인트:**
- `Schedule` 합성: `|` 연산자로 여러 Schedule 정책을 합성
- `.Retry(RetrySchedule)`: Schedule에 따라 IO 연산을 자동 재시도
- `attemptCount`를 클로저로 캡처하여 시도 횟수를 추적
- 3회 재시도 후에도 실패하면 `.Catch`가 최종 오류를 변환

**Schedule 타임라인:**

```
시도 1 (실패, 60%) → 대기 ~100ms
시도 2 (실패, 60%) → 대기 ~200ms
시도 3 (실패, 60%) → 대기 ~400ms
시도 4 (성공, 90%) → 결과 반환
```

## 3. Fork + awaitAll -- ParallelComplianceCheckService

5개 독립 체크를 병렬로 Fork하고 awaitAll로 수집하는 패턴입니다.

```csharp
[GenerateObservablePort]
public class ParallelComplianceCheckService : IParallelComplianceCheckService
{
    public sealed record ComplianceCheckFailed : AdapterErrorType.Custom;

    private static readonly Seq<string> CriterionNames = Seq(
        "DataGovernance", "SecurityReview", "BiasAssessment",
        "TransparencyAudit", "HumanOversight");

    public virtual FinT<IO, ComplianceCheckReport> RunComplianceChecks(
        ModelDeploymentId deploymentId)
    {
        // 각 기준별 IO 체크를 Fork로 병렬 실행
        var forks = CriterionNames.Map(name =>
            CheckSingleCriterion(name).Fork());

        // awaitAll로 모든 Fork 결과를 수집
        var io = awaitAll(forks)
            .Map(results =>
            {
                var allPassed = results.ForAll(r => r.Passed);
                return new ComplianceCheckReport(
                    DeploymentId: deploymentId,
                    Results: results,
                    AllPassed: allPassed,
                    ReportedAt: DateTimeOffset.UtcNow);
            })
            .Catch(
                e => e.IsExceptional,
                e => IO.fail<ComplianceCheckReport>(
                    AdapterError.FromException<ParallelComplianceCheckService>(
                        new ComplianceCheckFailed(),
                        e.ToException())));

        return new FinT<IO, ComplianceCheckReport>(io.Map(Fin.Succ));
    }

    private static IO<ComplianceCriterionCheckResult> CheckSingleCriterion(
        string criterionName)
    {
        return IO.liftAsync<ComplianceCriterionCheckResult>(async env =>
        {
            // 각 기준별 독립적인 네트워크 지연 (100~500ms)
            await Task.Delay(
                TimeSpan.FromMilliseconds(_random.Next(100, 500)),
                env.Token);

            var passed = _random.Next(100) < 90;
            return new ComplianceCriterionCheckResult(
                CriterionName: criterionName,
                Passed: passed,
                Details: passed
                    ? $"{criterionName}: All requirements met"
                    : $"{criterionName}: Remediation required",
                CheckedAt: DateTimeOffset.UtcNow);
        });
    }
}
```

**핵심 포인트:**
- `.Fork()`: IO 연산을 별도 파이버에서 비동기 실행
- `awaitAll(forks)`: `Seq<IO<ForkIO<A>>>` 오버로드로 모든 Fork 결과를 `Seq<A>`로 수집
- `.Map(results => ...)`: 수집된 결과를 집계하여 보고서 생성
- 각 `CheckSingleCriterion`은 독립적이므로 안전하게 병렬화 가능

## 4. Bracket -- ModelRegistryService

세션 Acquire -> Use -> Release 리소스 수명 관리 패턴입니다.

```csharp
[GenerateObservablePort]
public class ModelRegistryService : IModelRegistryService
{
    public sealed record RegistryLookupFailed : AdapterErrorType.Custom;

    public virtual FinT<IO, ModelRegistryEntry> LookupModel(
        AIModelId modelId)
    {
        // Acquire: 레지스트리 세션 획득
        var acquireSession = IO.liftAsync<RegistrySession>(async env =>
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(_random.Next(50, 150)),
                env.Token);

            if (_random.Next(100) < 5)
                throw new InvalidOperationException(
                    "Failed to acquire registry session");

            return new RegistrySession(
                Guid.NewGuid().ToString("N")[..8]);
        });

        var io = acquireSession.Bracket(
            Use: session => IO.liftAsync<ModelRegistryEntry>(async env =>
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(_random.Next(100, 400)),
                    env.Token);

                return new ModelRegistryEntry(
                    ModelName: $"model-{modelId.ToString()[..8]}",
                    Version: "1.0.0",
                    Framework: "PyTorch",
                    Checksum: Guid.NewGuid().ToString("N"),
                    RegisteredAt: DateTimeOffset.UtcNow);
            }),
            Fin: session => IO.lift(() =>
            {
                // Release: 세션 해제 (성공/실패 무관)
                session.Dispose();
                return unit;
            }));

        var result = io.Catch(
            e => e.IsExceptional,
            e => IO.fail<ModelRegistryEntry>(
                AdapterError.FromException<ModelRegistryService>(
                    new RegistryLookupFailed(),
                    e.ToException())));

        return new FinT<IO, ModelRegistryEntry>(result.Map(Fin.Succ));
    }
}
```

**핵심 포인트:**
- `acquireSession.Bracket(Use: ..., Fin: ...)`: Acquire-Use-Release 패턴
- `Fin` 매개변수: `Use`가 성공하든 실패하든 항상 실행됨 (finally 역할)
- `session.Dispose()`: IDisposable 리소스 해제
- Bracket 외부의 `.Catch`: 전체 Bracket 실패 시 오류 변환

## Persistence 폴더 구조: Aggregate 중심 + CQRS Role

Persistence 프로젝트는 Aggregate를 1차 폴더로, CQRS Role(Repository/Query)을 2차 폴더로 구성합니다.

```
AiGovernance.Adapters.Persistence/
├── Models/
│   ├── AIModel.Model.cs                    # DB 모델
│   ├── AIModel.Configuration.cs            # EF 설정
│   ├── Repositories/
│   │   ├── AIModelRepositoryInMemory.cs
│   │   └── AIModelRepositoryEfCore.cs
│   └── Queries/
│       ├── AIModelQueryInMemory.cs
│       └── AIModelDetailQueryInMemory.cs
├── Deployments/
│   ├── Deployment.Model.cs
│   ├── Deployment.Configuration.cs
│   ├── Repositories/
│   │   ├── DeploymentRepositoryInMemory.cs
│   │   └── DeploymentRepositoryEfCore.cs
│   └── Queries/
│       ├── DeploymentQueryInMemory.cs
│       └── DeploymentDetailQueryInMemory.cs
├── Assessments/
│   ├── Assessment.Model.cs / Criterion.Model.cs
│   ├── Assessment.Configuration.cs / Criterion.Configuration.cs
│   └── Repositories/
│       ├── AssessmentRepositoryInMemory.cs
│       └── AssessmentRepositoryEfCore.cs
├── Incidents/
│   ├── Incident.Model.cs
│   ├── Incident.Configuration.cs
│   ├── Repositories/
│   │   ├── IncidentRepositoryInMemory.cs
│   │   └── IncidentRepositoryEfCore.cs
│   └── Queries/
│       └── IncidentQueryInMemory.cs
├── GovernanceDbContext.cs
├── UnitOfWorkInMemory.cs / UnitOfWorkEfCore.cs
└── Registrations/
    ├── AdapterPersistenceRegistration.cs
    └── PersistenceOptions.cs
```

이 구조의 장점:
- Aggregate별로 관련 파일이 응집되어 탐색이 용이하다
- InMemory/EfCore 변형이 같은 폴더에 있어 비교가 쉽다
- 새 Aggregate 추가 시 폴더 하나만 추가하면 된다

## DI 등록 패턴

### AdapterInfrastructureRegistration

```csharp
public static IServiceCollection RegisterAdapterInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    // Mediator + 도메인 이벤트 발행자
    services.AddMediator(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Scoped;
        options.NotificationPublisherType =
            typeof(ObservableDomainEventNotificationPublisher);
    });
    services.RegisterDomainEventPublisher();
    services.RegisterDomainEventHandlersFromAssembly(
        AiGovernance.Application.AssemblyReference.Assembly);

    // FluentValidation
    services.AddValidatorsFromAssembly(AssemblyReference.Assembly);
    services.AddValidatorsFromAssembly(
        AiGovernance.Application.AssemblyReference.Assembly);

    // OpenTelemetry + Pipeline
    services
        .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
        .ConfigurePipelines(pipelines => pipelines
            .UseObservability()
            .UseValidation()
            .UseException())
        .Build();

    // Domain Services
    services.AddScoped<RiskClassificationService>();
    services.AddScoped<DeploymentEligibilityService>();

    // 외부 서비스 (IO 고급 기능 데모)
    services.AddScoped<IModelHealthCheckService, ModelHealthCheckService>();
    services.AddScoped<IModelMonitoringService, ModelMonitoringService>();
    services.AddScoped<IParallelComplianceCheckService,
        ParallelComplianceCheckService>();
    services.AddScoped<IModelRegistryService, ModelRegistryService>();

    return services;
}
```

### AdapterPersistenceRegistration

```csharp
private static void RegisterInMemoryRepositories(IServiceCollection services)
{
    // Repository (Observable 래퍼)
    services.RegisterScopedObservablePort<IAIModelRepository,
        AIModelRepositoryInMemoryObservable>();
    services.RegisterScopedObservablePort<IDeploymentRepository,
        DeploymentRepositoryInMemoryObservable>();
    services.RegisterScopedObservablePort<IAssessmentRepository,
        AssessmentRepositoryInMemoryObservable>();
    services.RegisterScopedObservablePort<IIncidentRepository,
        IncidentRepositoryInMemoryObservable>();

    // UnitOfWork
    services.RegisterScopedObservablePort<IUnitOfWork,
        UnitOfWorkInMemoryObservable>();

    // Read Adapter
    services.RegisterScopedObservablePort<IAIModelQuery,
        AIModelQueryInMemoryObservable>();
    services.RegisterScopedObservablePort<IModelDetailQuery,
        ModelDetailQueryInMemoryObservable>();
    services.RegisterScopedObservablePort<IDeploymentQuery,
        DeploymentQueryInMemoryObservable>();
    services.RegisterScopedObservablePort<IDeploymentDetailQuery,
        DeploymentDetailQueryInMemoryObservable>();
    services.RegisterScopedObservablePort<IIncidentQuery,
        IncidentQueryInMemoryObservable>();
}
```

`RegisterScopedObservablePort`는 Source Generator가 생성한 Observable 래퍼를 인터페이스에 등록합니다. 이 래퍼는 각 메서드 호출에 자동으로 로깅, 메트릭, 트레이싱을 추가합니다.

## IO 패턴 -> FinT 변환 공통 패턴

모든 외부 서비스가 동일한 패턴으로 IO를 FinT로 변환합니다:

```csharp
// IO<A> → FinT<IO, A>
return new FinT<IO, A>(io.Map(Fin.Succ));
```

이 변환은 `IO<A>`를 `IO<Fin<A>>`로 매핑한 뒤, `FinT<IO, A>`로 래핑합니다. 이로써 Application Layer의 `FinT<IO, T>` LINQ 체인에 자연스럽게 합성됩니다.

[구현 결과](../03-implementation-results/)에서 전체 Adapter 프로젝트 구조와 엔드포인트 목록을 확인합니다.
