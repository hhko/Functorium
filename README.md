# Functorium

[![Build](https://github.com/hhko/Functorium/actions/workflows/build.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/build.yml) [![Publish](https://github.com/hhko/Functorium/actions/workflows/publish.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/publish.yml)

> **Functorium**은 **`functor + dominium`** 에 **`fun`** 을 더한 이름으로, 도메인 주도 설계(DDD)와 함수형 아키텍처 원칙을 기반으로 **개발과 운영 사이의 구조적 단절을 해소**하기 위한 .NET 아키텍처 프레임워크입니다.

단순한 설계 패턴 모음이 아니라, 요구사항 정의부터 운영 안정성 확보까지 일관된 철학으로 연결되는 구조적 접근 방식을 담고 있습니다.

## 왜 이 프레임워크가 필요한가

많은 조직에서 요구사항은 개발 관점과 운영 관점으로 분리되어 정의됩니다. 기능 구현을 중심으로 작성된 명세와 운영 안정성을 고려한 요구가 서로 다른 체계로 관리되면서, **공통 언어는 정립되지 못하고 내부 아키텍처 기준 역시 명확히 수립되지 않는** 경우가 많습니다.

그 결과, 기능은 완성되었으나 운영 안정성이 확보되지 않거나, 안정성을 보완하는 과정에서 비용과 시간이 반복적으로 증가하는 구조가 형성됩니다. 이는 단순히 프로세스의 문제가 아니라, **설계 철학과 구조의 문제**입니다.

본 프레임워크는 요구사항, 설계, 구현, 운영을 하나의 도메인 중심 구조로 통합함으로써 이 단절을 제거하고자 합니다.

## 이 프레임워크가 지향하는 목표

1. **단일 도메인 언어(Ubiquitous Language) 통합** — Bounded Context를 명확히 정의하고, 도메인 개념을 중심으로 시스템을 재구성합니다. 개발자와 운영자, 비즈니스 이해관계자가 동일한 용어 체계를 공유하여 해석의 차이를 줄입니다.
2. **Observability 내재화** — 로그, 메트릭, 추적 정보는 구현 이후 추가되는 부가 요소가 아니라, 도메인 흐름과 함께 설계되어야 할 핵심 구조입니다. 장애 발생 시 원인 분석과 복구가 가능한 시스템을 만듭니다.
3. **함수형 아키텍처 기반 도메인 로직** — 순수 함수 중심의 설계를 통해 로직은 예측 가능하고 검증 가능한 형태를 갖춥니다. 사이드 이펙트는 명시적으로 분리되며, 테스트 가능한 구조가 기본 전제가 됩니다.

## 설계 철학

### 도메인 중심 설계

모든 핵심 비즈니스 로직은 도메인 모델 안에 위치하며, 엔티티와 값 객체, 애그리게이트, 도메인 서비스는 명확한 책임을 가집니다. 공통 언어는 단순한 용어 정리가 아니라, 코드와 문서, 운영 지표에까지 반영되어야 하는 일관된 개념 체계입니다.

**Value Object** — 값 기반 동등성과 불변성을 보장합니다:

```csharp
public abstract class AbstractValueObject : IValueObject, IEquatable<AbstractValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    // 값 기반 동등성, 캐시된 해시코드, ORM 프록시 처리
}
```

**Entity / AggregateRoot** — Ulid 기반 ID와 도메인 이벤트 관리를 제공합니다:

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>
    where T : struct, IEntityId<T>
{
    Ulid Value { get; }
    static abstract T New();
    static abstract T Create(Ulid id);
    static abstract T Create(string id);
}

public abstract class AggregateRoot<TId> : Entity<TId>, IDomainEventDrain
    where TId : struct, IEntityId<TId>
{
    protected void AddDomainEvent(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}
```

**DomainError** — 구조화된 에러 코드로 복구 가능성을 확보합니다:

```csharp
// 에러 코드 자동 생성: "DomainErrors.Email.Empty"
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");

// 에러 코드 자동 생성: "DomainErrors.Password.TooShort"
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
```

**Domain Event** — Mediator 기반 Pub/Sub과 이벤트 추적을 통합합니다:

```csharp
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
    Ulid EventId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}
```

### 함수형 아키텍처 전환

핵심 도메인 로직은 순수 함수로 구성됩니다. 입력이 동일하면 항상 동일한 출력을 반환하는 구조를 유지함으로써, 로직은 예측 가능하고 테스트하기 쉬운 형태가 됩니다. 사이드 이펙트(데이터베이스, 외부 API, 메시징, 파일 I/O)는 도메인 로직 바깥에서 처리됩니다.

**`Fin<T>`, `FinT<IO, T>`** — 예외 대신 명시적 결과 타입으로 오류를 처리합니다:

```csharp
// Repository는 FinT<IO, T>를 반환하여 사이드 이펙트를 명시적으로 표현
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, Unit> Delete(TId id);
}
```

**CQRS** — Command/Query를 명확히 분리하고, `FinResponse<T>`로 결과를 통합합니다:

```csharp
// Command
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>> { }
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess> { }

// Query
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>> { }
public interface IQueryUsecase<in TQuery, TSuccess>
    : IQueryHandler<TQuery, FinResponse<TSuccess>>
    where TQuery : IQueryRequest<TSuccess> { }
```

### Observability by Design

운영 안정성은 배포 이후에 보완하는 것이 아니라, 설계 단계에서부터 고려됩니다. 구조화된 로그, 핵심 비즈니스 메트릭, 분산 추적 정보가 도메인 흐름과 함께 정의됩니다.

**IObservablePort** — 모든 외부 의존성이 관측 가능한 포트로 추상화됩니다:

```csharp
public interface IObservablePort
{
    string RequestCategory { get; }
}
```

**`[GenerateObservablePort]`** — Source Generator가 Adapter에 대한 Observable wrapper를 자동 생성하여, OpenTelemetry 기반의 Tracing/Logging/Metrics를 투명하게 제공합니다:

```csharp
[GenerateObservablePort]  // → Observable{ClassName} 자동 생성
public class OrderRepository : IRepository<Order, OrderId> { ... }
```

**Usecase Pipeline** — 모든 유스케이스는 파이프라인을 통해 횡단 관심사를 처리합니다:

| Pipeline | 역할 |
|----------|------|
| `UsecaseExceptionPipeline` | 예외 → 구조화된 에러 변환 |
| `UsecaseValidationPipeline` | FluentValidation 기반 입력 검증 |
| `UsecaseLoggingPipeline` | 구조화된 로그 자동 기록 |
| `UsecaseMetricsPipeline` | 유스케이스 메트릭 자동 수집 |
| `UsecaseTracingPipeline` | 분산 추적 컨텍스트 전파 |
| `UsecaseTransactionPipeline` | 트랜잭션 경계 관리 |

## 구조 개요

![](./Functorium.Architecture.png)

시스템은 세 가지 계층으로 구성됩니다. 도메인은 외부에 의존하지 않으며, 의존성은 항상 안쪽을 향합니다.

### Domain Layer — 순수 비즈니스 로직

외부 의존성 없이, 순수 함수 기반으로 비즈니스 규칙을 표현합니다.

```
Domains/
├── Entities/           # Entity, AggregateRoot, IEntityId
├── ValueObjects/       # AbstractValueObject, Validations
├── Errors/             # DomainError, DomainErrorType
├── Events/             # IDomainEvent, DomainEvent
├── Repositories/       # IRepository (포트 정의)
├── Specifications/     # Specification Pattern
├── Services/           # IDomainService
└── Observabilities/    # IObservablePort
```

### Application Layer — 유스케이스 조립

도메인 로직과 인프라를 연결하고, 사이드 이펙트의 경계를 관리합니다.

```
Applications/
├── Cqrs/               # ICommandRequest, IQueryRequest, FinResponse
├── Events/             # IDomainEventHandler, IDomainEventPublisher
├── Errors/             # ApplicationError, EventError
├── Validations/        # FluentValidation 확장
├── Linq/               # FinT LINQ 확장
├── Queries/            # PagedResult, SortExpression
└── Persistence/        # IUnitOfWork
```

### Adapter Layer — 인프라 구현

데이터 저장소, 외부 시스템 연동, 관측성 구현을 담당합니다. 도메인에 의존하지만, 도메인은 인프라에 의존하지 않습니다.

```
Adapters/
├── Observabilities/
│   ├── Builders/       # OpenTelemetry 구성
│   ├── Pipelines/      # Usecase Pipeline (Logging, Metrics, Tracing, ...)
│   ├── Events/         # Observable 도메인 이벤트 발행
│   ├── Loggers/        # 구조화된 로거 확장
│   └── Naming/         # Observability 네이밍 규칙
├── Events/             # DomainEventCollector, DomainEventPublisher
├── Repositories/       # DapperQueryAdapterBase
├── Errors/             # AdapterError
├── Options/            # 옵션 설정
└── SourceGenerators/   # [GenerateObservablePort]
```

## 품질 전략

- 핵심 도메인 로직은 높은 수준의 **단위 테스트 커버리지**를 유지합니다.
- 사이드 이펙트 영역은 명시적으로 분리되어 **검증 가능한 구조**를 갖습니다.
- 배포 이전 단계에서 **Observability 검증**이 완료됩니다.
- 정의된 에러 코드와 복구 절차는 **문서화되고 검증**됩니다.

> 품질은 결과가 아니라 구조에서 비롯됩니다.

## 기대 효과

- 개발과 운영이 **동일한 도메인 모델** 기반으로 협업합니다.
- **변경 영향 범위**가 예측 가능해지고, 기능 완성도와 안정성이 동시에 향상됩니다.
- 반복적인 장애 대응과 유지보수 비용이 **구조적으로 감소**합니다.
- 기술 도입이 목적이 아니라, 도메인 중심 사고를 기반으로 한 **조직적 역량 강화**가 핵심 성과입니다.

## Book

- [Architecture](./Docs/architecture-is/README.md)
- [Automating Release Notes with Claude Code and .NET 10](./Books/Automating-ReleaseNotes-with-ClaudeCode-and-.NET10/README.md)
- [Automating Observability Code with SourceGenerator](./Books/Automating-ObservabilityCode-with-SourceGenerator/README.md)
- [Implementing Functional ValueObject](./Books/Implementing-Functional-ValueObject/README.md)
- [Implementing Specification Pattern](./Books/Implementing-Specification-Pattern/README.md)

## Observability

Functorium은 OpenTelemetry 기반의 통합 관측성(Logging, Metrics, Tracing)을 제공합니다.
모든 관측성 필드는 OpenTelemetry 시맨틱 규칙과의 일관성을 위해 `snake_case + dot` 표기법을 사용합니다.

![](./Functorium.Observability.png)

- **사양**: [Observability Specification](./Docs/guides/18-observability-spec.md) — Field/Tag 구조, Meter/Instrument 사양, 메시지 템플릿
- **Logging 매뉴얼**: [Logging Guide](./Docs/guides/19-observability-logging.md) — 구조화된 로깅 상세 가이드
- **Metrics 매뉴얼**: [Metrics Guide](./Docs/guides/20-observability-metrics.md) — 메트릭 수집 및 분석 가이드
- **Tracing 매뉴얼**: [Tracing Guide](./Docs/guides/21-observability-tracing.md) — 분산 추적 상세 가이드
