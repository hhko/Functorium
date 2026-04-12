---
title: "Adapter Code Design"
description: "Code snippets for 4 advanced IO patterns and DI registration patterns"
---

Implements the 4 IO patterns selected in [Type Design Decisions](../01-type-design-decisions/) in C# code.

## 1. Timeout + Catch -- ModelHealthCheckService

10-second timeout + timeout fallback + exception conversion pattern.

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
                // Network latency simulation (50~300ms, 12s in 10% of cases)
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

**Key points:**
- `IO.liftAsync`: Lifts an async operation into the IO monad
- `.Timeout(10s)`: Imposes a time limit on the IO operation, raises `Errors.TimedOut` on exceeding
- `.Catch(e => e.Is(Errors.TimedOut), ...)`: Converts timeout to a fallback result instead of an error
- `.Catch(e => e.IsExceptional, ...)`: Converts other exceptions to `AdapterError`
- Catch order matters: specific conditions (TimedOut) first, general conditions (IsExceptional) last

## 2. Retry + Schedule -- ModelMonitoringService

Exponential backoff + jitter + max 3 retries pattern.

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

                // First two attempts fail 60%, subsequent 10%
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

**Key points:**
- `Schedule` composition: Composes multiple Schedule policies with the `|` operator
- `.Retry(RetrySchedule)`: Automatically retries the IO operation according to the Schedule
- Captures `attemptCount` via closure to track attempt count
- After 3 retries, `.Catch` converts the final error if still failing

**Schedule timeline:**

```
Attempt 1 (fail, 60%) -> wait ~100ms
Attempt 2 (fail, 60%) -> wait ~200ms
Attempt 3 (fail, 60%) -> wait ~400ms
Attempt 4 (success, 90%) -> return result
```

## 3. Fork + awaitAll -- ParallelComplianceCheckService

Pattern that Forks 5 independent checks in parallel and collects them with awaitAll.

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
        // Fork each criterion IO check for parallel execution
        var forks = CriterionNames.Map(name =>
            CheckSingleCriterion(name).Fork());

        // Collect all Fork results with awaitAll
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
            // Independent network latency per criterion (100~500ms)
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

**Key points:**
- `.Fork()`: Executes the IO operation asynchronously in a separate fiber
- `awaitAll(forks)`: `Seq<IO<ForkIO<A>>>` overload that collects all Fork results into `Seq<A>`
- `.Map(results => ...)`: Aggregates collected results to generate a report
- Each `CheckSingleCriterion` is independent, so safe to parallelize

## 4. Bracket -- ModelRegistryService

Session Acquire -> Use -> Release resource lifecycle management pattern.

```csharp
[GenerateObservablePort]
public class ModelRegistryService : IModelRegistryService
{
    public sealed record RegistryLookupFailed : AdapterErrorType.Custom;

    public virtual FinT<IO, ModelRegistryEntry> LookupModel(
        AIModelId modelId)
    {
        // Acquire: Acquire registry session
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
                // Release: Release session (regardless of success/failure)
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

**Key points:**
- `acquireSession.Bracket(Use: ..., Fin: ...)`: Acquire-Use-Release pattern
- `Fin` parameter: Always executes whether `Use` succeeds or fails (acts as finally)
- `session.Dispose()`: IDisposable resource release
- `.Catch` outside Bracket: Error conversion on overall Bracket failure

## Persistence Folder Structure: Aggregate-centric + CQRS Role

The Persistence project uses Aggregate as the primary folder and CQRS Role (Repository/Query) as the secondary folder.

```
AiGovernance.Adapters.Persistence/
├── Models/
│   ├── AIModel.Model.cs                    # DB model
│   ├── AIModel.Configuration.cs            # EF configuration
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

Advantages of this structure:
- Related files are cohesive per Aggregate, making navigation easy
- InMemory/EfCore variants are in the same folder, making comparison easy
- Adding a new Aggregate only requires adding one folder

## DI Registration Patterns

### AdapterInfrastructureRegistration

```csharp
public static IServiceCollection RegisterAdapterInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    // Mediator + domain event publisher
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

    // External services (IO advanced features demo)
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
    // Repository (Observable wrappers)
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

`RegisterScopedObservablePort` registers the Source Generator-generated Observable wrapper to the interface. This wrapper automatically adds logging, metrics, and tracing to each method call.

## IO Pattern -> FinT Conversion Common Pattern

All external services convert IO to FinT using the same pattern:

```csharp
// IO<A> -> FinT<IO, A>
return new FinT<IO, A>(io.Map(Fin.Succ));
```

This conversion maps `IO<A>` to `IO<Fin<A>>`, then wraps it as `FinT<IO, A>`. This enables natural composition into the Application Layer's `FinT<IO, T>` LINQ chain.

See the complete Adapter project structure and endpoint list in [Implementation Results](./03-implementation-results/).
