# Functorium

[![Build](https://github.com/hhko/Functorium/actions/workflows/build.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/build.yml) [![Publish](https://github.com/hhko/Functorium/actions/workflows/publish.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/publish.yml)

**[English](./README.md)** | **한국어**

> **Functorium**은 **`functor + dominium`** 에 **`fun`** 을 더한 이름으로, 도메인 주도 설계(DDD)와 함수형 아키텍처 원칙을 기반으로 **개발과 운영 사이의 구조적 단절을 해소**하기 위한 통합 .NET 프레임워크입니다.

- 배움은 셀렘이다.
- 배움은 겸손이다.
- 배움은 이타심이다.

**Functorium**은 단순한 설계 패턴 모음이 아니라, 요구사항 정의부터 운영 안정성 확보까지 일관된 철학으로 연결되는 구조적 접근 방식을 담고 있습니다.

## 누구를 위한 프레임워크인가

- **엔터프라이즈 DDD를 실천하는 .NET 팀** — 도메인 모델의 불변성과 합성 가능성을 코드 수준에서 보장하고 싶은 팀
- **개발과 운영 사이의 언어 격차를 해소하고 싶은 팀** — 도메인 개념이 코드, 로그, 메트릭, 추적 정보에 일관되게 반영되어야 하는 팀
- **함수형 DDD 아키텍처를 체계적으로 도입하려는 아키텍트** — LanguageExt + DDD + OpenTelemetry를 통합된 프레임워크로 사용하고 싶은 설계자

## 설계 동기

### 풀고자 하는 문제

1. **도메인 로직에 예외와 암묵적 사이드 이펙트가 섞여 있다** — 비즈니스 규칙의 성공과 실패가 예외로 처리되어, 흐름을 예측하기 어렵고 합성이 불가능합니다.
2. **개발 언어와 운영 언어가 분리되어 있다** — 기능 명세와 운영 요구가 서로 다른 체계로 관리되면서, 공통 언어가 정립되지 못하고 해석 차이가 누적됩니다.
3. **Observability가 사후 보완으로 추가된다** — 로그, 메트릭, 추적 정보가 구현 완료 후 별도로 부착되면서, 장애 발생 시 원인 분석에 필요한 맥락이 누락됩니다.

이는 단순히 프로세스의 문제가 아니라, **설계 철학과 구조의 문제**입니다.

Mediator, LanguageExt, FluentValidation, OpenTelemetry는 각각 훌륭합니다. 하지만 이들을 일관된 DDD 아키텍처로 통합하려면 에러 전파, 파이프라인 순서, 관측성 경계, 타입 제약에 대한 수백 가지 결정이 필요합니다. Functorium은 이 결정을 한 번, 일관되게 내립니다.

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

핵심 도메인 로직은 순수 함수로 구성됩니다. 입력이 동일하면 항상 동일한 출력을 반환하는 구조를 유지함으로써, 로직은 예측 가능하고 테스트하기 쉬운 형태가 됩니다. 사이드 이펙트(데이터베이스, 외부 API, 메시징, 파일 I/O)는 도메인 로직 바깥에서 처리됩니다. `IO` 모나드는 Timeout, Retry(지수 백오프), Fork(병렬 실행), Bracket(리소스 생명주기 관리) 등 고급 기능을 기본 제공하여, 외부 서비스 호출의 장애 내성을 타입 안전하게 구성할 수 있습니다.

**`Fin<T>`, `FinT<IO, T>`** — 예외 대신 명시적 결과 타입으로 오류를 처리합니다. Command 경로의 Repository는 `FinT<IO, T>`를 반환하여 사이드 이펙트를 명시적으로 표현합니다:

```csharp
// Command: IRepository — Aggregate Root 단위 CRUD, EF Core로 변경 추적과 트랜잭션 관리
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // 벌크 연산
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```

**CQRS** — 쓰기와 읽기를 구조적으로 분리하여 각 경로에 최적화된 데이터 접근 전략을 적용합니다. Command는 `IRepository` + EF Core로 Aggregate 일관성과 트랜잭션을 보장하고, Query는 `IQueryPort` + Dapper로 Aggregate 재구성 없이 DTO를 직접 프로젝션합니다. 두 경로 모두 `FinResponse<T>`로 결과를 통합합니다:

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

```csharp
// Query: IQueryPort — Aggregate 재구성 없이 DTO 직접 프로젝션, Dapper로 경량 SQL 매핑
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec, PageRequest page, SortExpression sort);

    FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec, CursorPageRequest cursor, SortExpression sort);

    IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec, SortExpression sort,
        CancellationToken cancellationToken = default);
}
```

| | Command (IRepository) | Query (IQueryPort) |
|------|----------------------|-------------------|
| **목적** | Aggregate Root 생명주기 관리 | 읽기 전용 DTO 프로젝션 |
| **구현** | EF Core — 변경 추적, 트랜잭션, 도메인 이벤트 | Dapper — 순수 SQL, 경량 매핑 |
| **Specification** | `PropertyMap` → EF Core LINQ 변환 | `DapperSpecTranslator` → SQL WHERE 변환 |
| **페이지네이션** | — | Offset/Limit, Cursor (keyset), Streaming |

### Observability by Design

운영 안정성은 배포 이후에 보완하는 것이 아니라, 설계 단계에서부터 고려됩니다. 모든 Command/Query의 요청부터 응답까지 Logging, Metrics, Tracing 3개 Pillar에 **동일한 필드가 일관되게 기록**됩니다. 개발자가 로그 코드를 직접 작성할 필요가 없습니다.

**IObservablePort** — 모든 외부 의존성이 관측 가능한 포트로 추상화됩니다:

```csharp
public interface IObservablePort
{
    string RequestCategory { get; }
}
```

**ctx.* 3-Pillar Enrichment** — Source Generator가 Request/Response/DomainEvent의 프로퍼티를 `ctx.{snake_case}` 필드로 자동 변환하여, Logging/Tracing/Metrics에 비즈니스 컨텍스트를 동시 전파합니다. `[CtxTarget(CtxPillar.All)]`로 Metrics 태그를 opt-in할 수 있습니다.

**`[GenerateObservablePort]`** — Source Generator가 Adapter에 대한 Observable wrapper를 자동 생성하여, OpenTelemetry 기반의 Tracing/Logging/Metrics를 투명하게 제공합니다:

```csharp
[GenerateObservablePort]  // → Observable{ClassName} 자동 생성 (예: ObservableOrderRepository)
public class OrderRepository : IRepository<Order, OrderId> { ... }
```

**에러 자동 분류** — 비즈니스 규칙 위반(`"재고 부족"`)은 `expected`, 시스템 장애(`NullReferenceException`)는 `exceptional`, 복합 검증 실패는 `aggregate`로 자동 분류됩니다. `error.type` 필드로 Seq/Grafana에서 비즈니스 오류와 시스템 장애를 분리 조회할 수 있습니다.

**Usecase Pipeline** — Command와 Query는 경로 특성에 따라 서로 다른 Pipeline 구성을 갖습니다:

> **Command (7단계):** CtxEnricher → Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler
>
> **Query (8단계):** CtxEnricher → Metrics → Tracing → Logging → Validation → Caching → Exception → Custom → Handler

| Pipeline | 역할 | 적용 |
|----------|------|------|
| `CtxEnricherPipeline` | 비즈니스 컨텍스트(ctx.*)를 Logging/Tracing/Metrics에 동시 전파 | 공통 |
| `UsecaseMetricsPipeline` | 유스케이스 메트릭 자동 수집 | 공통 |
| `UsecaseTracingPipeline` | 분산 추적 컨텍스트 전파 | 공통 |
| `UsecaseLoggingPipeline` | 구조화된 로그 자동 기록 | 공통 |
| `UsecaseValidationPipeline` | FluentValidation 기반 입력 검증 | 공통 |
| `UsecaseCachingPipeline` | ICacheable 요청 캐싱 | Query 전용 |
| `UsecaseExceptionPipeline` | 예외 → 구조화된 에러 변환 | 공통 |
| `UsecaseTransactionPipeline` | 트랜잭션 경계 + 도메인 이벤트 발행 | Command 전용 |

## 주요 핵심 기능

| 가치 | 제공 기능 |
|------|----------|
| **도메인 안전성** | Value Object 계층 (6 타입 + Union), Entity/AggregateRoot, Specification Pattern, 구조화된 에러 코드 |
| **함수형 합성** | `Fin<T>`/`FinT<IO,T>` Discriminated Union, LINQ 합성, Bind/Apply 검증, CQRS 경로별 최적화 |
| **IO 고급 기능** | Timeout, Retry(지수 백오프), Fork(병렬 실행), Bracket(리소스 생명주기 관리) |
| **자동화** | 5개 Source Generator, Usecase Pipeline (Command 7단계 / Query 8단계), 아키텍처 규칙 테스트 |
| **관측성** | 3-Pillar 자동 계측, ctx.* 비즈니스 컨텍스트 전파, 에러 자동 분류 (expected/exceptional/aggregate) |

## Quick Example

Always-valid Value Object — 예외 없이 타입 안전한 에러 코드로 검증하고, 합성 가능한 함수형 검증 파이프라인을 제공합니다:

```csharp
public sealed partial class Email : SimpleValueObject<string>
{
    public const int MaxLength = 320;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // 각 검증 조건이 실패하면 조건에 대응하는 에러 코드가 자동 생성됩니다.
    //   NotNull    → "DomainErrors.Email.Null"
    //   NotEmpty   → "DomainErrors.Email.Empty"
    //   MaxLength  → "DomainErrors.Email.TooLong"
    //   Matches    → "DomainErrors.Email.InvalidFormat"
    // 복합 Value Object는 Apply 패턴으로 복수 필드를 병렬 검증하여
    // 실패한 모든 에러를 한꺼번에 수집합니다.
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format");

    // ORM/Repository 복원용 — 이미 정규화된 데이터만 받는다
    public static Email CreateFromValidated(string value) => new(value);

    public static implicit operator string(Email email) => email.Value;
}
```

CQRS Command/Query 유스케이스 구현 예제는 [CQRS Repository 튜토리얼](./Docs.Site/src/content/docs/tutorials/cqrs-repository/index.md)에서 확인할 수 있습니다.

## 시작하기

```bash
# 핵심 도메인 모델링 — Value Object, Entity, AggregateRoot, Specification, 에러 체계
dotnet add package Functorium

# 인프라 어댑터 — OpenTelemetry, Serilog, EF Core, Dapper, Pipeline
dotnet add package Functorium.Adapters

# 코드 자동 생성 — [GenerateObservablePort], [GenerateEntityId], CtxEnricher
dotnet add package Functorium.SourceGenerators

# 테스트 유틸리티 — ArchUnitNET, xUnit 확장, 통합 테스트 픽스처
dotnet add package Functorium.Testing
```

**5분 빠른시작:** [Quickstart](./Docs.Site/src/content/docs/quickstart/index.mdx)에서 Value Object → AggregateRoot → Command Usecase를 5분 안에 만들어 보세요.

**첫 번째 튜토리얼:** [Functional ValueObject 튜토리얼](./Docs.Site/src/content/docs/tutorials/functional-valueobject/index.md)에서 Value Object를 깊이 학습하세요.

**전체 문서:** [https://hhko.github.io/Functorium](https://hhko.github.io/Functorium)

## 구조 개요

![](./Functorium.Architecture.png)

시스템은 세 가지 계층으로 구성됩니다. 도메인은 외부에 의존하지 않으며, 의존성은 항상 안쪽을 향합니다.

- **Domain Layer** — 순수 비즈니스 로직. Entity, AggregateRoot, Value Object, Specification, DomainError, Domain Event, Repository 포트(IRepository), IObservablePort. 외부 의존성 없이 순수 함수 기반으로 비즈니스 규칙을 표현합니다.
- **Application Layer** — 유스케이스 조립. CQRS(ICommandRequest, IQueryRequest), FinResponse, IQueryPort(읽기 전용 DTO 프로젝션), FluentValidation 확장, FinT LINQ 합성, Domain Event 발행, IUnitOfWork. 도메인 로직과 인프라를 연결하고 사이드 이펙트의 경계를 관리합니다.
- **Adapter Layer** — 인프라 구현. OpenTelemetry 구성, Usecase Pipeline (Command 7단계 / Query 8단계, CtxEnricher 포함), Observable 도메인 이벤트 발행, 구조화된 로거, DapperQueryAdapterBase, AdapterError, 5개 Source Generator([GenerateObservablePort], [GenerateEntityId], CtxEnricher, DomainEventCtxEnricher, [UnionType]). 도메인에 의존하지만, 도메인은 인프라에 의존하지 않습니다.

## Observability

Functorium은 OpenTelemetry 기반의 통합 관측성(Logging, Metrics, Tracing)을 제공합니다.

![](./Functorium.Observability.png)

### 세 가지 관측 경로

| 관측 경로 | 대상 | 메커니즘 | 기록 내용 |
|----------|------|---------|----------|
| **Usecase Pipeline** | 모든 Command/Query | Mediator `IPipelineBehavior` | request/response 필드 + ctx.* + 에러 분류 |
| **Observable Port** | Repository, QueryAdapter, ExternalService | `[GenerateObservablePort]` Source Generator | 동일한 request/response 필드 체계 |
| **DomainEvent** | Publisher + Handler | `ObservableDomainEventPublisher` | 이벤트 타입/수량 + 부분 실패 추적 |

Application 레이어(EventId 1001–1004)와 Adapter 레이어(EventId 2001–2004)가 **동일한 `request.*` / `response.*` / `error.*` 네이밍**을 사용하므로, 하나의 대시보드 쿼리로 전체 요청 흐름을 추적할 수 있습니다.

상세 사양과 가이드는 문서 사이트에서 확인할 수 있습니다:
- [Observability Specification](./Docs.Site/src/content/docs/spec/08-observability.md) — Field/Tag 구조, ctx.* 3-Pillar Enrichment, Meter/Instrument 사양
- [Logging Guide](./Docs.Site/src/content/docs/guides/observability/19-observability-logging.md) — 구조화된 로깅 상세 가이드
- [Metrics Guide](./Docs.Site/src/content/docs/guides/observability/20-observability-metrics.md) — 메트릭 수집 및 분석 가이드
- [Tracing Guide](./Docs.Site/src/content/docs/guides/observability/21-observability-tracing.md) — 분산 추적 상세 가이드

## 품질 전략

- 핵심 도메인 로직은 높은 수준의 **단위 테스트 커버리지를** 유지합니다.
- 사이드 이펙트 영역은 명시적으로 분리되어 **검증 가능한 구조를** 갖습니다.
- 배포 이전 단계에서 **Observability 검증이** 완료됩니다.
- 정의된 에러 코드와 복구 절차는 **문서화되고 검증**됩니다.
- 아키텍처 규칙은 ClassValidator/InterfaceValidator를 통해 **단위 테스트로 자동 검증**됩니다.

> 품질은 결과가 아니라 구조에서 비롯됩니다.

## 문서

**전체 문서 사이트:** [https://hhko.github.io/Functorium](https://hhko.github.io/Functorium)

### 튜토리얼

| 튜토리얼 | 주제 | 실습 |
|----------|------|------|
| [Implementing Functional ValueObject](./Docs.Site/src/content/docs/tutorials/functional-valueobject/index.md) | Value Object, 검증, 불변성 | 29개 |
| [Implementing Specification Pattern](./Docs.Site/src/content/docs/tutorials/specification-pattern/index.md) | Specification, Expression Tree | 18개 |
| [Implementing CQRS Repository And Query Patterns](./Docs.Site/src/content/docs/tutorials/cqrs-repository/index.md) | CQRS, Repository, Query 어댑터 | 22개 |
| [Designing TypeSafe Usecase Pipeline Constraints](./Docs.Site/src/content/docs/tutorials/usecase-pipeline/index.md) | 제네릭 변성, IFinResponse, Pipeline 제약 | 20개 |
| [Enforcing Architecture Rules with Testing](./Docs.Site/src/content/docs/tutorials/architecture-rules/index.md) | 아키텍처 규칙, ClassValidator | 16개 |
| [Automating ObservabilityCode with SourceGenerator](./Docs.Site/src/content/docs/tutorials/sourcegen-observability/index.md) | Source Generator, Observable wrapper | — |
| [Automating ReleaseNotes with ClaudeCode and .NET 10](./Docs.Site/src/content/docs/tutorials/release-notes-claude/index.md) | AI 자동화, 릴리스 노트 | — |

### 예제

| 예제 | 범위 | Aggregates | 핵심 패턴 |
|------|------|------------|-----------|
| [Designing with Types](./Docs.Site/src/content/docs/samples/designing-with-types/index.md) | Domain | 1 | VO, Union, Composite, Specification |
| [E-Commerce DDD](./Docs.Site/src/content/docs/samples/ecommerce-ddd/index.md) | Domain + Application | 5 | CQRS, EventHandler, DomainService, ApplyT |
| [AI Model Governance](./Docs.Site/src/content/docs/samples/ai-model-governance/index.md) | Domain + Application + Adapter | 4 | EfCore/Dapper/InMemory, FastEndpoints, IO.Retry/Timeout/Fork/Bracket |

## 패키지 구성

| 패키지 | 설명 |
|--------|------|
| `Functorium` | 핵심 도메인 모델링 — Value Object, Entity, AggregateRoot, Specification, 에러 체계 |
| `Functorium.Adapters` | 인프라 어댑터 — OpenTelemetry, Serilog, EF Core, Dapper, Pipeline |
| `Functorium.SourceGenerators` | 코드 자동 생성 — `[GenerateObservablePort]`, `[GenerateEntityId]`, `CtxEnricherGenerator` |
| `Functorium.Testing` | 테스트 유틸리티 — ArchUnitNET, xUnit 확장, 통합 테스트 픽스처 |

## AX (AI Transformation)

AX는 Functorium 기반 프로젝트의 개발과 운영을 AI가 안내하는 Claude Code 플러그인 시스템입니다. 2개 플러그인이 DDD 개발 워크플로와 릴리스 자동화를 각각 담당합니다.

### 설치

```bash
# 두 플러그인 동시 로드
claude --plugin-dir ./.claude/plugins/functorium-develop --plugin-dir ./.claude/plugins/release-note
```

### functorium-develop (v0.4.0) — DDD 개발 워크플로

PRD 작성부터 테스트까지, 7단계 워크플로를 8개 스킬 + 6개 전문 에이전트가 안내합니다.

```
project-spec → architecture-design → domain-develop → application-develop → adapter-develop → observability-develop → test-develop
```

| 스킬 | 역할 |
|------|------|
| `project-spec` | 프로젝트 요구사항 명세 (PRD, User Stories, 우선순위, 수락 기준) |
| `architecture-design` | 프로젝트 구조, 네이밍 규칙, 인프라 설계 |
| `domain-develop` | Domain Layer (VO, Aggregate, Spec, DomainService) |
| `application-develop` | Application Layer (CQRS, Usecase, Port) |
| `adapter-develop` | Adapter Layer (Repository, Endpoint, DI) |
| `observability-develop` | 관측성 전략 (KPI→메트릭 매핑, 대시보드, 알림, ctx.* 전파) |
| `test-develop` | 단위/통합/아키텍처/관측성 검증 테스트 |
| `domain-review` | DDD 코드 리뷰 (어느 시점에서든 사용 가능) |

### release-note (v1.0.0) — 릴리스 노트 자동화

C# 스크립트 기반 데이터 수집, Conventional Commits 분석, Breaking Changes 감지, 릴리스 노트 작성, 검증까지 5단계 워크플로를 1개 스킬 + 1개 에이전트가 자동화합니다.

```
환경 검증 → 데이터 수집 → 커밋 분석 → 릴리스 노트 작성 → 검증
```

| 원칙 | 설명 |
|------|------|
| 정확성 우선 | Uber 파일(`all-api-changes.txt`)에 없는 API는 문서화하지 않음 |
| 가치 전달 필수 | 모든 주요 기능에 "Why this matters" 섹션 포함 |
| Breaking Changes 자동 감지 | Git Diff 분석이 커밋 메시지 패턴보다 우선 |
| 추적성 | 모든 기능을 커밋 SHA로 추적 |

상세 문서: [AX (AI Transformation)](https://hhko.github.io/Functorium/ax/)

## 기술 스택

| 분류 | 주요 라이브러리 |
|------|----------------|
| 함수형 | LanguageExt.Core, Ulid, Ardalis.SmartEnum |
| 검증 | FluentValidation |
| 중재자 | Mediator (source-generated), Scrutor |
| 영속성 | EF Core, Dapper |
| 관측성 | OpenTelemetry, Serilog |
| 테스트 | xUnit v3, ArchUnitNET, Verify.Xunit, Shouldly |
