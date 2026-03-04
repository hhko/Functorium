# Functorium

[![Build](https://github.com/hhko/Functorium/actions/workflows/build.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/build.yml) [![Publish](https://github.com/hhko/Functorium/actions/workflows/publish.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/publish.yml)

> **Functorium**은 **`functor + dominium`** 에 **`fun`** 을 더한 이름으로, 도메인 주도 설계(DDD)와 함수형 아키텍처 원칙을 기반으로 **개발과 운영 사이의 구조적 단절을 해소**하기 위한 .NET 아키텍처 프레임워크입니다.

단순한 설계 패턴 모음이 아니라, 요구사항 정의부터 운영 안정성 확보까지 일관된 철학으로 연결되는 구조적 접근 방식을 담고 있습니다.

## 주요 핵심 기능

### 타입 안전한 함수형 도메인 모델링

Value Object와 Entity를 함수형 팩토리 패턴으로 생성하여 도메인 불변식을 생성 시점에 보장하고, 예외 대신 `FinResponse<T>` Discriminated Union으로 유스케이스 결과를 명시적으로 처리합니다. `FinT<IO, T>` Monad Transformer로 사이드 이펙트 경계를 타입 수준에서 추적하고, 구조화된 에러 코드와 Specification 합성, 함수형 검증을 통해 도메인 로직의 정확성과 합성 가능성을 동시에 확보합니다.

| 기능 | 설명 |
|------|------|
| Value Object 계층 | AbstractValueObject → SimpleValueObject\<T\> → ComparableValueObject. 값 기반 동등성, Always-valid 팩토리 |
| Entity / AggregateRoot | Ulid 기반 타입 안전한 ID(IEntityId\<T\>). AggregateRoot에서 도메인 이벤트 수집·발행 |
| FinResponse\<T\> Discriminated Union | sealed record 기반 Success/Failure. 모든 유스케이스의 명시적 결과 처리. LINQ 합성 지원 |
| FinT\<IO, T\> Monad Transformer | 사이드 이펙트를 타입으로 추적. Repository 반환 타입으로 순수/비순수 경계 명시 |
| 구조화된 에러 코드 시스템 | `DomainErrors.Email.Empty` 형태 자동 생성. 8가지 명명 규칙(R1–R8) |
| Specification Pattern | &/\|/! 연산자 합성. Expression Tree → SQL 변환. PropertyMap 브릿지 |
| 함수형 검증 (Bind + Apply) | 순차(Bind) vs 병렬(Apply) 검증. Always-valid Value Object |

### 자동 코드 생성 (Source Generator)

Source Generator가 컴파일 시점에 Domain과 Adapter 계층 간 브릿지 코드를 자동 생성합니다. 포트 구현체에 관측성 래퍼를 씌우고, 도메인 ID 타입에 영속성 변환기를 생성하여 계층 간 연결 보일러플레이트를 제거합니다.

| 기능 | 설명 |
|------|------|
| \[GenerateObservablePort\] | Adapter 포트 구현체에 OpenTelemetry Tracing/Logging/Metrics Observable wrapper 자동 생성 |
| \[GenerateEntityId\] | Ulid 기반 EntityId + EF Core ValueConverter/ValueComparer 자동 생성. Domain ↔ Adapter 영속성 브릿지 |

### 아키텍처 품질 자동화

모든 유스케이스에 횡단 관심사 Pipeline이 자동 적용되고, CQRS 기반으로 읽기/쓰기 경로가 분리됩니다. 타입 시스템과 단위 테스트가 Pipeline 제약 조건, 불변성·가시성·상속 규칙을 코드 수준에서 강제하여 설계 의도가 지속적으로 보존됩니다.

| 기능 | 설명 |
|------|------|
| Usecase 횡단 관심사 Pipeline | Metrics → Tracing → Logging → Validation → Caching → Exception → Transaction 순서로 자동 적용 |
| 타입 안전한 Pipeline 제약 | IFinResponse 인터페이스 계층 + CRTP 팩토리. 리플렉션 없이 Pipeline별 최소 제약 |
| CQRS 읽기/쓰기 분리 | Command(IRepository + EF Core) vs Query(IQueryPort + Dapper). Cursor 페이지네이션 |
| 아키텍처 규칙 테스트 | ClassValidator/InterfaceValidator/MethodValidator. 불변성·가시성·상속 규칙을 단위 테스트로 강제 |

**Quick Example** — Domain Value Object 생성 → CQRS Usecase:

```csharp
// Domain: Always-valid Value Object — Fin<T>로 검증 결과 반환
// 검증 실패 시 조건별 DomainError 자동 생성:
//   "DomainErrors.Email.Null", "DomainErrors.Email.Empty",
//   "DomainErrors.Email.TooLong", "DomainErrors.Email.InvalidFormat"
public sealed partial class Email : SimpleValueObject<string>
{
    public const int MaxLength = 320;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // 각 검증 조건이 실패하면 조건에 대응하는 에러 코드가 자동 생성됩니다.
    //   NotNull  → "DomainErrors.Email.Null"
    //   NotEmpty → "DomainErrors.Email.Empty"
    //   MaxLength→ "DomainErrors.Email.TooLong"
    //   Matches  → "DomainErrors.Email.InvalidFormat"
    // 복합 Value Object는 Apply 패턴으로 복수 필드를 병렬 검증하여
    // 실패한 모든 에러를 한꺼번에 수집합니다.
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format")
            .ThenNormalize(v => v.Trim().ToLowerInvariant());
}
```

```csharp
// ── Command 경로 ──────────────────────────────────────────────
// ICustomerRepository : IRepository<Customer, CustomerId>
//   → Domain 계층에 정의된 포트. Aggregate Root 단위로 영속화
//   → 반환 타입: FinT<IO, Customer> (도메인 객체)
//   → 구현: EF Core (변경 추적, 트랜잭션)

using static Functorium.Applications.Errors.ApplicationErrorType;

public sealed class CreateCustomerCommand
{
    public sealed record Request(string Name, string Email, decimal CreditLimit)
        : ICommandRequest<Response>;
    public sealed record Response(string CustomerId, string Name, string Email);

    public sealed class Usecase(ICustomerRepository repository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken token)
        {
            FinT<IO, Response> usecase =
                from exists  in repository.Exists(new CustomerEmailSpec(email))
                from _       in guard(!exists, ApplicationError.For<CreateCustomerCommand>(
                    new AlreadyExists(),                    // ApplicationErrorType sealed record
                    request.Email,                          // 현재 값
                    $"Email already exists: '{request.Email}'"))
                from customer in repository.Create(newCustomer)
                select new Response(
                    customer.Id.ToString(), customer.Name, customer.Email);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}

// ── Query 경로 ────────────────────────────────────────────────
// ICustomerDetailQuery : IQueryPort
//   → Application 계층에 정의된 포트. Aggregate 재구성 없이 DTO로 직접 프로젝션
//   → 반환 타입: FinT<IO, CustomerDetailDto> (읽기 전용 DTO)
//   → 구현: Dapper (경량 SQL 매핑)

public sealed class GetCustomerByIdQuery
{
    public sealed record Request(string CustomerId) : IQueryRequest<Response>;
    public sealed record Response(string CustomerId, string Name, string Email);

    public sealed class Usecase(ICustomerDetailQuery query)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken token)
        {
            FinT<IO, Response> usecase =
                from dto in query.GetById(CustomerId.Create(request.CustomerId))
                select new Response(dto.CustomerId, dto.Name, dto.Email);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}
```

각 계층은 독립적인 에러 코드 체계를 가집니다:
- **Domain**: `DomainError.For<Email>(new Empty(), ...)` → `"DomainErrors.Email.Empty"`
- **Application**: `ApplicationError.For<CreateCustomerCommand>(new AlreadyExists(), ...)` → `"ApplicationErrors.CreateCustomerCommand.AlreadyExists"`
- **Adapter**: `AdapterError.For<OrderRepository>(new NotFound(), ...)` → `"AdapterErrors.OrderRepository.NotFound"`

Adapter 계층은 인프라 오류(DB 조회 실패, 외부 서비스 장애, 파이프라인 예외 등)를 구조화된 에러로 표현합니다.
`NotFound`, `Timeout`, `ConnectionFailed`, `PipelineException` 등 인프라 특성에 맞는 에러 타입이 제공되며, `FromException`으로 예외를 에러 코드로 래핑할 수도 있습니다.

모든 에러는 구조화된 로그로 출력되어 Observability 파이프라인과 연계됩니다:

```json
// 단일 에러 — Domain 검증 실패
{
  "error.code": "DomainErrors.Email.Empty",
  "error.type": "expected",
  "@error": {
    "ErrorCode": "DomainErrors.Email.Empty",
    "ErrorCurrentValue": "",
    "Message": "Email cannot be empty"
  }
}

// 복수 에러 — Apply 패턴으로 여러 조건 실패를 한꺼번에 수집
{
  "error.code": "DomainErrors.Email.Empty",
  "error.type": "aggregate",
  "@error": {
    "Errors": [
      { "ErrorCode": "DomainErrors.Email.Empty", "ErrorCurrentValue": "", "Message": "Email cannot be empty" },
      { "ErrorCode": "DomainErrors.Password.TooShort", "ErrorCurrentValue": "ab", "Message": "Password too short" }
    ]
  }
}
```

## 설계 동기

### 풀고자 하는 문제

1. **도메인 로직에 예외와 암묵적 사이드 이펙트가 섞여 있다** — 비즈니스 규칙의 성공과 실패가 예외로 처리되어, 흐름을 예측하기 어렵고 합성이 불가능합니다.
2. **개발 언어와 운영 언어가 분리되어 있다** — 기능 명세와 운영 요구가 서로 다른 체계로 관리되면서, 공통 언어가 정립되지 못하고 해석 차이가 누적됩니다.
3. **Observability가 사후 보완으로 추가된다** — 로그, 메트릭, 추적 정보가 구현 완료 후 별도로 부착되면서, 장애 발생 시 원인 분석에 필요한 맥락이 누락됩니다.

이는 단순히 프로세스의 문제가 아니라, **설계 철학과 구조의 문제**입니다.

### 문제 해결 방향

1. **함수형 아키텍처로 도메인 로직을 순수하게 유지한다** — `Fin<T>`, `FinT<IO, T>`로 결과와 사이드 이펙트를 타입 수준에서 명시하고, `from ... in ... select` LINQ 합성으로 예외 없이 도메인 흐름을 조립합니다.
2. **단일 도메인 언어(Ubiquitous Language)로 통합한다** — Bounded Context를 명확히 정의하고, 도메인 개념이 코드·문서·운영 지표에 일관되게 반영되는 구조를 만듭니다.
3. **Observability를 설계 단계부터 내재화한다** — OpenTelemetry 기반 Logging, Metrics, Tracing이 유스케이스 파이프라인에 자동 적용되어, 도메인 흐름과 관측 정보가 함께 설계됩니다.

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
| `UsecaseCachingPipeline` | ICacheable 요청 캐싱. 캐시 히트 시 DB 라운드트립 없이 즉시 반환 |
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
- 아키텍처 규칙은 ClassValidator/InterfaceValidator를 통해 **단위 테스트로 자동 검증**됩니다.

> 품질은 결과가 아니라 구조에서 비롯됩니다.

## 기대 효과

- 개발과 운영이 **동일한 도메인 모델** 기반으로 협업합니다.
- **변경 영향 범위**가 예측 가능해지고, 기능 완성도와 안정성이 동시에 향상됩니다.
- 반복적인 장애 대응과 유지보수 비용이 **구조적으로 감소**합니다.
- 기술 도입이 목적이 아니라, 도메인 중심 사고를 기반으로 한 **조직적 역량 강화**가 핵심 성과입니다.

## 시작하기

```bash
dotnet add package Functorium
dotnet add package Functorium.SourceGenerators
dotnet add package Functorium.Testing
```

## 튜토리얼

- [Architecture](./Docs/architecture-is/README.md)

| 튜토리얼 | 주제 | 실습 |
|----------|------|------|
| [Implementing Functional ValueObject](./Docs/tutorials/Implementing-Functional-ValueObject/README.md) | Value Object, 검증, 불변성 | 29개 |
| [Implementing Specification Pattern](./Docs/tutorials/Implementing-Specification-Pattern/README.md) | Specification, Expression Tree | 18개 |
| [Implementing CQRS Repository And Query Patterns](./Docs/tutorials/Implementing-CQRS-Repository-And-Query-Patterns/README.md) | CQRS, Repository, Query 어댑터 | 22개 |
| [Designing TypeSafe Usecase Pipeline Constraints](./Docs/tutorials/Designing-TypeSafe-Usecase-Pipeline-Constraints/README.md) | 제네릭 변성, IFinResponse, Pipeline 제약 | 20개 |
| [Enforcing Architecture Rules with Testing](./Docs/tutorials/Enforcing-Architecture-Rules-with-Testing/README.md) | 아키텍처 규칙, ClassValidator | 16개 |
| [Automating ObservabilityCode with SourceGenerator](./Docs/tutorials/Automating-ObservabilityCode-with-SourceGenerator/README.md) | Source Generator, Observable wrapper | — |
| [Automating ReleaseNotes with ClaudeCode and .NET 10](./Docs/tutorials/Automating-ReleaseNotes-with-ClaudeCode-and-.NET10/README.md) | AI 자동화, 릴리스 노트 | — |

## Observability

Functorium은 OpenTelemetry 기반의 통합 관측성(Logging, Metrics, Tracing)을 제공합니다.
모든 관측성 필드는 OpenTelemetry 시맨틱 규칙과의 일관성을 위해 `snake_case + dot` 표기법을 사용합니다.

![](./Functorium.Observability.png)

- **사양**: [Observability Specification](./Docs/guides/18a-observability-spec.md) — Field/Tag 구조, Meter/Instrument 사양, 메시지 템플릿
- **Logging 매뉴얼**: [Logging Guide](./Docs/guides/19-observability-logging.md) — 구조화된 로깅 상세 가이드
- **Metrics 매뉴얼**: [Metrics Guide](./Docs/guides/20-observability-metrics.md) — 메트릭 수집 및 분석 가이드
- **Tracing 매뉴얼**: [Tracing Guide](./Docs/guides/21-observability-tracing.md) — 분산 추적 상세 가이드
