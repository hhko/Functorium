---
title: "아키텍처 설계"
description: "AI 모델 거버넌스 플랫폼의 프로젝트 구조, 참조 방향, DI 전략, 관측성 파이프라인"
---

## 1. 프로젝트 구조도

```
samples/ai-model-governance/
├── AiModelGovernance.slnx                          # 솔루션 파일 (8 프로젝트)
├── Directory.Build.props                           # FunctoriumSrcRoot + 공통 설정
├── Directory.Build.targets                         # root 상속 차단
├── domain/                                         # 도메인 레이어 문서 (4개)
├── application/                                    # 애플리케이션 레이어 문서 (4개)
├── adapter/                                        # 어댑터 레이어 문서 (4개)
├── observability/                                  # 관측성 문서 (4개)
├── Src/
│   ├── AiGovernance.Domain/                        # Domain Layer
│   │   ├── SharedModels/Services/                  # Domain Services (2종)
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
│   │   ├── ExternalServices/                       # IO 고급 기능 (4종)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Persistence/          # InMemory, EfCore Repository/Query
│   │   ├── Models/                                 # Repository(2) + Query(2)
│   │   ├── Deployments/                            # Repository(2) + Query(2)
│   │   ├── Assessments/                            # Repository(2)
│   │   ├── Incidents/                              # Repository(2) + Query(1)
│   │   └── Registrations/
│   ├── AiGovernance.Adapters.Presentation/         # FastEndpoints
│   │   ├── Endpoints/                              # HTTP API (15종)
│   │   └── Registrations/
│   └── AiGovernance/                               # Host (Program.cs)
└── Tests/
    ├── AiGovernance.Tests.Unit/                    # 단위 테스트
    └── AiGovernance.Tests.Integration/             # 통합 테스트
```

---

## 2. 솔루션 파일

`AiModelGovernance.slnx`에 8개 프로젝트가 포함됩니다.

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

| 프로젝트 | 레이어 | 역할 |
|---------|--------|------|
| AiGovernance.Domain | Domain | Aggregate, VO, Specification, Domain Service, Domain Event |
| AiGovernance.Application | Application | Command/Query Usecase, Port 인터페이스, Event Handler |
| AiGovernance.Adapters.Infrastructure | Adapter | Mediator, OpenTelemetry, Pipeline, External Service |
| AiGovernance.Adapters.Persistence | Adapter | Repository/Query 구현 (InMemory, EfCore) |
| AiGovernance.Adapters.Presentation | Adapter | FastEndpoints HTTP API |
| AiGovernance | Host | Program.cs, DI 조립, appsettings.json |
| AiGovernance.Tests.Unit | Test | 단위 테스트 (VO, Aggregate, Domain Service, Architecture) |
| AiGovernance.Tests.Integration | Test | 통합 테스트 (HTTP Endpoint E2E) |

---

## 3. 프로젝트 참조 방향

```
AiGovernance (Host)
├── Adapters.Infrastructure → Functorium + Functorium.Adapters + Application
├── Adapters.Persistence    → Functorium.Adapters + Application + SourceGenerators
├── Adapters.Presentation   → Application
└── Application             → Domain
    Domain                  → Functorium + SourceGenerators
```

**핵심 원칙:** 의존성은 안쪽으로만 향한다. Domain은 외부에 대해 아무것도 모르고, Application은 포트 인터페이스만 알며, Adapter가 구현한다.

---

## 4. 네이밍 규칙

### 3차원 구조

| 차원 | 표현 수단 | 예시 |
|------|-----------|------|
| Aggregate (무엇) | 1차 폴더 | `Models/`, `Deployments/`, `Assessments/`, `Incidents/` |
| CQRS Role (읽기/쓰기) | 2차 폴더 | `Repositories/`, `Queries/`, `Commands/`, `EventHandlers/` |
| Technology (어떻게) | 클래스 접미사 | `EfCore`, `InMemory`, `Dapper` |

### 파일명 패턴: `{Subject}{Role}{Variant}`

| 파일 유형 | 패턴 | 예시 |
|-----------|------|------|
| Repository | `{Aggregate}Repository{Variant}.cs` | `AIModelRepositoryInMemory.cs`, `AIModelRepositoryEfCore.cs` |
| Query | `{Aggregate}Query{Variant}.cs` | `AIModelQueryInMemory.cs`, `DeploymentDetailQueryInMemory.cs` |
| DB 모델 | `{Aggregate}.Model.cs` | `AIModel.Model.cs`, `Deployment.Model.cs` |
| EF 설정 | `{Aggregate}.Configuration.cs` | `AIModel.Configuration.cs` |
| UnitOfWork | `UnitOfWork{Variant}.cs` | `UnitOfWorkInMemory.cs`, `UnitOfWorkEfCore.cs` |

---

## 5. DI 등록 전략

3개 Registration 클래스가 각 Adapter의 서비스를 독립적으로 등록합니다.

| Registration 클래스 | 등록 항목 |
|--------------------|----------|
| `AdapterPresentationRegistration` | FastEndpoints |
| `AdapterPersistenceRegistration` | Repository, Query, UnitOfWork (Observable 래퍼) |
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

public partial class Program { }  // Integration Test 지원
```

---

## 6. 영속성 Provider 전환

`appsettings.json`의 `Persistence:Provider` 값으로 InMemory/Sqlite를 전환합니다.

```json
{
  "Persistence": {
    "Provider": "InMemory",
    "ConnectionString": "Data Source=ai-governance.db"
  }
}
```

`AdapterPersistenceRegistration`에서 Provider 값에 따라 분기합니다:

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

Observable 래퍼를 통해 DI에 등록하므로, Provider를 교체해도 관측성은 자동으로 유지됩니다:

```csharp
// InMemory
services.RegisterScopedObservablePort<IAIModelRepository, AIModelRepositoryInMemoryObservable>();
// Sqlite
services.RegisterScopedObservablePort<IAIModelRepository, AIModelRepositoryEfCoreObservable>();
```

---

## 7. 관측성 파이프라인

OpenTelemetry 3-Pillar 관측성을 `RegisterOpenTelemetry` + `ConfigurePipelines`로 설정합니다.

```csharp
services
    .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()   // CtxEnricher, Metrics, Tracing, Logging 일괄 활성화
        .UseValidation()
        .UseException())
    .Build();
```

`UseObservability()`는 관측성 4종(CtxEnricher, Metrics, Tracing, Logging)을 일괄 활성화합니다. 나머지 파이프라인은 명시적 opt-in으로 등록합니다:

| 순서 | 미들웨어 | 역할 |
|------|---------|------|
| 1 | `UseObservability()` | CtxEnricher + Metrics + Tracing + Logging 일괄 활성화 |
| 2 | `UseValidation()` | FluentValidation 기반 요청 검증 |
| 3 | `UseException()` | 예외 -> DomainError/AdapterError 변환 |

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

## 8. IO 고급 기능

외부 서비스 통합에서 사용하는 4가지 LanguageExt IO 패턴입니다.

| 패턴 | 구현 클래스 | 용도 | 핵심 메서드 |
|------|------------|------|-----------|
| **Timeout + Catch** | `ModelHealthCheckService` | 헬스 체크 타임아웃 처리 | `IO.Timeout(10s)` -> `.Catch(TimedOut, fallback)` -> `.Catch(Exceptional, error)` |
| **Retry + Schedule** | `ModelMonitoringService` | 지수 백오프 재시도 | `IO.Retry(exponential(100ms) \| jitter(0.3) \| recurs(3) \| maxDelay(5s))` |
| **Fork + awaitAll** | `ParallelComplianceCheckService` | 5개 기준 병렬 체크 | `forks.Map(io => io.Fork())` -> `awaitAll(forks)` |
| **Bracket** | `ModelRegistryService` | 세션 리소스 수명 관리 | `acquire.Bracket(Use: ..., Fin: ...)` |

모든 외부 서비스는 `[GenerateObservablePort]`로 관측성이 자동 추가되고, `IO<A>` -> `FinT<IO, A>` 변환으로 Application Layer의 FinT LINQ 체인에 합성됩니다.

---

## 9. 빌드/테스트 명령어

```bash
# 빌드
dotnet build Docs.Site/src/content/docs/samples/ai-model-governance/AiModelGovernance.slnx

# 테스트 (268개)
dotnet test --solution Docs.Site/src/content/docs/samples/ai-model-governance/AiModelGovernance.slnx
```
