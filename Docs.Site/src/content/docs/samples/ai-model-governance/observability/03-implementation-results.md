---
title: "관측성 구현 결과"
description: "Observable Port, CtxEnricher, 관측성 파이프라인, IO 패턴 현황"
---

## Observable Port 현황

`[GenerateObservablePort]` Source Generator가 생성한 Observable 래퍼 목록입니다.

### Repository Observable (InMemory)

| 원본 클래스 | Observable 래퍼 | 포트 |
|-----------|----------------|------|
| AIModelRepositoryInMemory | AIModelRepositoryInMemoryObservable | IAIModelRepository |
| DeploymentRepositoryInMemory | DeploymentRepositoryInMemoryObservable | IDeploymentRepository |
| AssessmentRepositoryInMemory | AssessmentRepositoryInMemoryObservable | IAssessmentRepository |
| IncidentRepositoryInMemory | IncidentRepositoryInMemoryObservable | IIncidentRepository |
| UnitOfWorkInMemory | UnitOfWorkInMemoryObservable | IUnitOfWork |

### Repository Observable (EfCore)

| 원본 클래스 | Observable 래퍼 | 포트 |
|-----------|----------------|------|
| AIModelRepositoryEfCore | AIModelRepositoryEfCoreObservable | IAIModelRepository |
| DeploymentRepositoryEfCore | DeploymentRepositoryEfCoreObservable | IDeploymentRepository |
| AssessmentRepositoryEfCore | AssessmentRepositoryEfCoreObservable | IAssessmentRepository |
| IncidentRepositoryEfCore | IncidentRepositoryEfCoreObservable | IIncidentRepository |
| UnitOfWorkEfCore | UnitOfWorkEfCoreObservable | IUnitOfWork |

### Query Observable (InMemory)

| 원본 클래스 | Observable 래퍼 | 포트 |
|-----------|----------------|------|
| AIModelQueryInMemory | AIModelQueryInMemoryObservable | IAIModelQuery |
| AIModelDetailQueryInMemory | AIModelDetailQueryInMemoryObservable | IModelDetailQuery |
| DeploymentQueryInMemory | DeploymentQueryInMemoryObservable | IDeploymentQuery |
| DeploymentDetailQueryInMemory | DeploymentDetailQueryInMemoryObservable | IDeploymentDetailQuery |
| IncidentQueryInMemory | IncidentQueryInMemoryObservable | IIncidentQuery |

### External Service Observable

| 원본 클래스 | IO 패턴 | 포트 |
|-----------|---------|------|
| ModelHealthCheckService | Timeout + Catch | IModelHealthCheckService |
| ModelMonitoringService | Retry + Schedule | IModelMonitoringService |
| ParallelComplianceCheckService | Fork + awaitAll | IParallelComplianceCheckService |
| ModelRegistryService | Bracket | IModelRegistryService |

**Observable Port 합계: 19개** (InMemory Repository 5 + EfCore Repository 5 + Query 5 + External Service 4)

---

## CtxEnricher 현황

Functorium Pipeline의 `UseCtxEnricher()` 미들웨어가 Command/Query Request와 DomainEvent의 프로퍼티를 자동으로 ctx.* 필드로 전파합니다.

### Command Request ctx.* 전파

| Command | ctx.* 필드 | Pillar |
|---------|-----------|--------|
| RegisterModelCommand | `ctx.register_model_command.request.name` | Default(L+T) |
| | `ctx.register_model_command.request.version` | Default(L+T) |
| | `ctx.register_model_command.request.purpose` | Logging |
| CreateDeploymentCommand | `ctx.create_deployment_command.request.model_id` | Default(L+T) |
| | `ctx.create_deployment_command.request.environment` | All(L+T+M) |
| | `ctx.create_deployment_command.request.drift_threshold` | Default+MetricsValue |
| ReportIncidentCommand | `ctx.report_incident_command.request.severity` | All(L+T+M) |
| | `ctx.report_incident_command.request.description` | Logging |

### DomainEvent ctx.* 전파

| Event | ctx.* 필드 | Pillar |
|-------|-----------|--------|
| AIModel.RiskClassifiedEvent | `ctx.risk_classified_event.model_id` | Default(L+T) |
| | `ctx.risk_classified_event.new_risk_tier` | Default(L+T) |
| ModelIncident.ReportedEvent | `ctx.reported_event.severity` | All(L+T+M) |
| | `ctx.reported_event.deployment_id` | Default(L+T) |

---

## 관측성 파이프라인 구성 현황

```csharp
services
    .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()                          // CtxEnricher + Metrics + Tracing + Logging 일괄 활성화
        .UseValidation()
        .UseException())
    .Build();
```

### UseObservability() 미들웨어 순서

`UseObservability()`는 관측성 4종을 일괄 활성화합니다. 나머지 파이프라인은 명시적 opt-in으로 등록합니다:

| 순서 | 미들웨어 | 수집 대상 | 출력 |
|------|---------|----------|------|
| 1 | `UseObservability()` → Metrics | Counter(requests, responses), Histogram(duration) | Prometheus / OTLP |
| 2 | `UseObservability()` → Tracing | Activity Span (진입/종료/태그) | Jaeger / OTLP |
| 3 | `UseObservability()` → CtxEnricher | Request/Response/Event 프로퍼티 | LogContext + Activity.SetTag + MetricsTagContext |
| 4 | `UseObservability()` → Logging | 구조화 로그 (Serilog) | Console / OTLP |
| 5 | `UseValidation()` | FluentValidation 기반 요청 검증 | Validation Error |
| 6 | `UseException()` | 예외 -> DomainError/AdapterError 변환 | error.type, error.code 태그 |

### 이벤트 발행 관측성

```csharp
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.NotificationPublisherType =
        typeof(ObservableDomainEventNotificationPublisher);
});
services.RegisterDomainEventPublisher();
services.RegisterDomainEventHandlersFromAssembly(
    AiGovernance.Application.AssemblyReference.Assembly);
```

`ObservableDomainEventNotificationPublisher`는 DomainEvent 발행 시 자동으로 메트릭, 트레이싱, 로깅을 수행합니다. EventHandler 관측성은 Mediator의 `NotificationPublisherType`으로 제공됩니다.

---

## External Service IO 패턴 현황

4종 외부 서비스의 IO 패턴과 관측성 통합 현황입니다.

### ModelHealthCheckService (Timeout + Catch)

```
IO.liftAsync → .Timeout(10s) → .Catch(TimedOut → fallback) → .Catch(Exceptional → error)
                                                              ↓
                              [GenerateObservablePort] → Observable 래퍼 자동 생성
                                                              ↓
                              adapter.external_service.requests (Counter)
                              adapter.external_service.duration (Histogram)
                              adapter.external_service.responses (Counter + error.type)
```

### ModelMonitoringService (Retry + Schedule)

```
IO.liftAsync → .Retry(exponential|jitter|recurs|maxDelay) → .Catch(Exceptional → error)
                                                              ↓
                              Observable 래퍼: 최종 결과만 관측
                              재시도 시도 횟수는 내부 로깅으로 추적
```

### ParallelComplianceCheckService (Fork + awaitAll)

```
CriterionNames.Map(name => CheckSingle(name).Fork()) → awaitAll(forks) → .Map(aggregate)
                                                              ↓
                              Observable 래퍼: 전체 병렬 체크 결과를 하나의 호출로 관측
                              개별 기준 체크는 Fork 내부에서 실행 (개별 관측 안함)
```

### ModelRegistryService (Bracket)

```
acquireSession.Bracket(Use: lookupModel, Fin: releaseSession) → .Catch(Exceptional → error)
                                                              ↓
                              Observable 래퍼: Bracket 전체를 하나의 호출로 관측
                              Acquire/Use/Release 세부 단계는 내부 로깅으로 추적
```

---

## 수치 요약

| 항목 | 수량 |
|------|------|
| Observable Port (InMemory Repository) | 5 |
| Observable Port (EfCore Repository) | 5 |
| Observable Port (Query) | 5 |
| Observable Port (External Service) | 4 |
| **Observable Port 합계** | **19** |
| Pipeline 미들웨어 | 5 (Metrics, Tracing, CtxEnricher, Logging, Exception) |
| DomainEvent 관측성 | ObservableDomainEventNotificationPublisher |
| ctx.* MetricsTag 필드 | 2 (Environment, Severity) |
| ctx.* MetricsValue 필드 | 1 (DriftThreshold) |
| ctx.* Default(L+T) 필드 | 8+ (ModelId, DeploymentId, Name, Version 등) |
| ctx.* Logging only 필드 | 2 (Purpose, Description) |
