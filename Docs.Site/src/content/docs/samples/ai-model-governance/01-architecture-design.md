---
title: "Architecture Design"
description: "Project structure, reference direction, DI strategy, and observability pipeline for the AI Model Governance Platform"
---

## 1. Project Structure Diagram

```
samples/ai-model-governance/
├── AiModelGovernance.slnx                          # Solution file (8 projects)
├── Directory.Build.props                           # FunctoriumSrcRoot + common settings
├── Directory.Build.targets                         # Root inheritance blocking
├── domain/                                         # Domain layer docs (4)
├── application/                                    # Application layer docs (4)
├── adapter/                                        # Adapter layer docs (4)
├── observability/                                  # Observability docs (4)
├── Src/
│   ├── AiGovernance.Domain/                        # Domain Layer
│   │   ├── SharedModels/Services/                  # Domain Services (2 types)
│   │   └── AggregateRoots/
│   │       ├── Models/                             # AIModel + VOs(4) + Specs(2)
│   │       ├── Deployments/                        # ModelDeployment + VOs(4) + Specs(3)
│   │       ├── Assessments/                        # ComplianceAssessment + Entity(1) + VOs(3) + Specs(3)
│   │       └── Incidents/                          # ModelIncident + VOs(4) + Specs(4)
│   ├── AiGovernance.Application/                   # Application Layer
│   │   └── Usecases/
│   │       ├── Models/                             # Commands(2), Queries(2), Ports(2)
│   │       ├── Deployments/                        # Commands(4), Queries(2), Ports(4)
│   │       ├── Assessments/                        # Commands(1), Queries(1), EventHandlers(1)
│   │       └── Incidents/                          # Commands(1), Queries(2), Ports(1), EventHandlers(1)
│   ├── AiGovernance.Adapters.Infrastructure/       # Mediator, OpenTelemetry, External Services
│   │   ├── ExternalServices/                       # Advanced IO Features (4 types)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Persistence/          # InMemory, EfCore Repository/Query
│   │   ├── Models/                                 # Repository(2) + Query(2)
│   │   ├── Deployments/                            # Repository(2) + Query(2)
│   │   ├── Assessments/                            # Repository(2)
│   │   ├── Incidents/                              # Repository(2) + Query(1)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Presentation/         # FastEndpoints
│   │   ├── Endpoints/                              # HTTP API (15 types)
│   │   └── Registrations/
│   └── AiGovernance/                               # Host (Program.cs)
└── Tests/
    ├── AiGovernance.Tests.Unit/                    # Unit Tests
    └── AiGovernance.Tests.Integration/             # Integration Tests
```

---

## 2. Solution File

`AiModelGovernance.slnx` contains 8 projects.

```xml
<Solution>
  <Project Path="Src/AiGovernance.Domain/AiGovernance.Domain.csproj" />
  <Project Path="Src/AiGovernance.Application/AiGovernance.Application.csproj" />
  <Project Path="Src/AiGovernance.Adapters.Infrastructure/AiGovernance.Adapters.Infrastructure.csproj" />
  <Project Path="Src/AiGovernance.Adapters.Persistence/AiGovernance.Adapters.Persistence.csproj" />
  <Project Path="Src/AiGovernance.Adapters.Presentation/AiGovernance.Adapters.Presentation.csproj" />
  <Project Path="Src/AiGovernance/AiGovernance.csproj" />
  <Project Path="Tests/AiGovernance.Tests.Unit/AiGovernance.Tests.Unit.csproj" />
  <Project Path="Tests/AiGovernance.Tests.Integration/AiGovernance.Tests.Integration.csproj" />
</Solution>
```

| Project | Layer | Role |
|---------|-------|------|
| AiGovernance.Domain | Domain | Aggregate, VO, Specification, Domain Service, Domain Event |
| AiGovernance.Application | Application | Command/Query Usecase, Port Interfaces, Event Handler |
| AiGovernance.Adapters.Infrastructure | Adapter | Mediator, OpenTelemetry, Pipeline, External Service |
| AiGovernance.Adapters.Persistence | Adapter | Repository/Query Implementation (InMemory, EfCore) |
| AiGovernance.Adapters.Presentation | Adapter | FastEndpoints HTTP API |
| AiGovernance | Host | Program.cs, DI Assembly, appsettings.json |
| AiGovernance.Tests.Unit | Test | Unit Tests (VO, Aggregate, Domain Service, Architecture) |
| AiGovernance.Tests.Integration | Test | Integration Tests (HTTP Endpoint E2E) |

---

## 3. Project Reference Direction

```
AiGovernance (Host)
├── Adapters.Infrastructure → Functorium + Functorium.Adapters + Application
├── Adapters.Persistence    → Functorium.Adapters + Application + SourceGenerators
├── Adapters.Presentation   → Application
└── Application             → Domain
    Domain                  → Functorium + SourceGenerators
```

**Core Principle:** Dependencies flow only inward. Domain knows nothing about the outside, Application knows only port interfaces, and Adapters provide the implementations.

---

## 4. Naming Conventions

### 3-Dimensional Structure

| Dimension | Expression | Example |
|-----------|-----------|---------|
| Aggregate (What) | Primary Folder | `Models/`, `Deployments/`, `Assessments/`, `Incidents/` |
| CQRS Role (Read/Write) | Secondary Folder | `Repositories/`, `Queries/`, `Commands/`, `EventHandlers/` |
| Technology (How) | Class Suffix | `EfCore`, `InMemory`, `Dapper` |

### File Name Pattern: `{Subject}{Role}{Variant}`

| File Type | Pattern | Example |
|-----------|---------|---------|
| Repository | `{Aggregate}Repository{Variant}.cs` | `AIModelRepositoryInMemory.cs`, `AIModelRepositoryEfCore.cs` |
| Query | `{Aggregate}Query{Variant}.cs` | `AIModelQueryInMemory.cs`, `DeploymentDetailQueryInMemory.cs` |
| DB Model | `{Aggregate}.Model.cs` | `AIModel.Model.cs`, `Deployment.Model.cs` |
| EF Config | `{Aggregate}.Configuration.cs` | `AIModel.Configuration.cs` |
| UnitOfWork | `UnitOfWork{Variant}.cs` | `UnitOfWorkInMemory.cs`, `UnitOfWorkEfCore.cs` |

---

## 5. DI Registration Strategy

Three Registration classes independently register each Adapter's services.

| Registration Class | Registered Items |
|--------------------|-----------------|
| `AdapterPresentationRegistration` | FastEndpoints |
| `AdapterPersistenceRegistration` | Repository, Query, UnitOfWork (Observable wrappers) |
| `AdapterInfrastructureRegistration` | Mediator, FluentValidation, OpenTelemetry, Pipeline, Domain Service, External Service |

```csharp
// Host Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence(builder.Configuration)
    .RegisterAdapterInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseAdapterPresentation();
app.Run();

public partial class Program { }  // Integration Test support
```

---

## 6. Persistence Provider Switching

The `Persistence:Provider` value in `appsettings.json` switches between InMemory and Sqlite.

```json
{
  "Persistence": {
    "Provider": "InMemory",
    "ConnectionString": "Data Source=ai-governance.db"
  }
}
```

`AdapterPersistenceRegistration` branches based on the Provider value:

```csharp
switch (options.Provider)
{
    case "Sqlite":
        services.AddDbContext<GovernanceDbContext>(opt =>
            opt.UseSqlite(options.ConnectionString));
        RegisterSqliteRepositories(services);
        break;
    case "InMemory":
    default:
        RegisterInMemoryRepositories(services);
        break;
}
```

Since DI registration uses Observable wrappers, observability is automatically maintained even when switching providers:

```csharp
// InMemory
services.RegisterScopedObservablePort<IAIModelRepository, AIModelRepositoryInMemoryObservable>();
// Sqlite
services.RegisterScopedObservablePort<IAIModelRepository, AIModelRepositoryEfCoreObservable>();
```

---

## 7. Observability Pipeline

OpenTelemetry 3-Pillar observability is configured using `RegisterOpenTelemetry` + `ConfigurePipelines`.

```csharp
services
    .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()   // Batch-enable CtxEnricher, Metrics, Tracing, Logging
        .UseValidation()
        .UseException())
    .Build();
```

`UseObservability()` batch-enables 4 observability components (CtxEnricher, Metrics, Tracing, Logging). The remaining pipelines are registered via explicit opt-in:

| Order | Middleware | Role |
|-------|-----------|------|
| 1 | `UseObservability()` | Batch-enable CtxEnricher + Metrics + Tracing + Logging |
| 2 | `UseValidation()` | FluentValidation-based request validation |
| 3 | `UseException()` | Exception -> DomainError/AdapterError conversion |

```json
{
  "OpenTelemetry": {
    "ServiceName": "AiGovernance",
    "ServiceNamespace": "AiGovernance",
    "CollectorEndpoint": "http://localhost:18889",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": false
  }
}
```

---

## 8. Advanced IO Features

Four LanguageExt IO patterns used in external service integration.

| Pattern | Implementation Class | Purpose | Core Method |
|---------|---------------------|---------|-------------|
| **Timeout + Catch** | `ModelHealthCheckService` | Health check timeout handling | `IO.Timeout(10s)` -> `.Catch(TimedOut, fallback)` -> `.Catch(Exceptional, error)` |
| **Retry + Schedule** | `ModelMonitoringService` | Exponential backoff retry | `IO.Retry(exponential(100ms) \| jitter(0.3) \| recurs(3) \| maxDelay(5s))` |
| **Fork + awaitAll** | `ParallelComplianceCheckService` | Parallel compliance checks on 5 criteria | `forks.Map(io => io.Fork())` -> `awaitAll(forks)` |
| **Bracket** | `ModelRegistryService` | Resource lifecycle management (session) | `acquire.Bracket(Use: ..., Fin: ...)` |

All external services have observability automatically added via `[GenerateObservablePort]`, and are composed into the Application Layer's FinT LINQ chain through `IO<A>` -> `FinT<IO, A>` conversion.

---

## 9. Build/Test Commands

```bash
# Build
dotnet build Docs.Site/src/content/docs/samples/ai-model-governance/AiModelGovernance.slnx

# Test (268 tests)
dotnet test --solution Docs.Site/src/content/docs/samples/ai-model-governance/AiModelGovernance.slnx
```
