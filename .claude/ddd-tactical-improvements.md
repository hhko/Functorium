# DDD 전술적 설계 개선 사항

이 문서는 Eric Evans의 DDD 전술적 설계 패턴과 Functorium 현재 구현 간의 갭을 분석하고, 개선 방향을 제시합니다.

## 목차

### Part 1: 완전 구현된 패턴
- [1. Value Objects](#1-value-objects-)
- [2. Entities](#2-entities-)
- [3. Aggregate Roots](#3-aggregate-roots-)
- [4. Domain Events](#4-domain-events-)

### Part 2: 구현 완료 패턴 (이전 미구현)
- [5. Repository Pattern](#5-repository-pattern-)
- [6. Domain Services](#6-domain-services-)
- [7. Specifications](#7-specifications-)
- [8. Factories](#8-factories-)

### Part 3: 부분 구현 / 개선 필요
- [9. Integration Events vs Domain Events](#9-integration-events-vs-domain-events-)
- [10. Outbox Pattern](#10-outbox-pattern-)
- [11. Unit of Work](#11-unit-of-work-)
- [12. Domain Event Versioning](#12-domain-event-versioning-)
- [13. DTO Mapping Strategy](#13-dto-mapping-strategy-) ✅
- [14. Resilience Patterns](#14-resilience-patterns-)

### Part 4: 전략적 개선
- [15. 도메인 로직 배치 전략](#15-도메인-로직-배치-전략)
- [16. Aggregate 설계 가이드 강화](#16-aggregate-설계-가이드-강화)
- [17. 로드맵 및 우선순위](#17-로드맵-및-우선순위)

---

# Part 1: 완전 구현된 패턴

---

## 1. Value Objects ✅

### 현재 구현

Functorium은 DDD Value Object 패턴을 완전히 구현합니다.

**기반 클래스 계층:**

```
IValueObject
└── AbstractValueObject (값 기반 동등성, ==, !=)
    └── ValueObject
        ├── SimpleValueObject<T> (단일 값 래핑)
        └── ComparableValueObject (비교 연산자)
            └── ComparableSimpleValueObject<T>
```

**검증 시스템:**

| 검증 방식 | 클래스 | 용도 |
|-----------|--------|------|
| Typed | `ValidationRules<T>` | 타입별 플루언트 검증 체인 |
| Contextual | `ContextualValidation` | 컨텍스트 기반 검증 |

**구현 패턴:**

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>.NotEmpty(value ?? "")
            .ThenMatches(EmailRegex())
            .ThenMaxLength(254)
            .ThenNormalize(v => v.Trim().ToLowerInvariant());
}
```

**DDD 원칙 충족:**
- 불변성: `private` 생성자 + `sealed class`
- 자기 검증: `Create` 팩토리 메서드에서 검증
- 값 동등성: `AbstractValueObject`가 `Equals`/`GetHashCode` 구현
- 명시적 오류: `Fin<T>`, `Validation<Error, T>` 결과 타입 사용

**참조:** [05-value-objects.md](./05-value-objects.md)

---

## 2. Entities ✅

### 현재 구현

**기반 클래스 계층:**

```
IEntity<TId>
└── Entity<TId> (ID 기반 동등성, ORM 프록시 지원)
    └── AggregateRoot<TId> (도메인 이벤트 컬렉션)
```

**Entity ID 시스템:**

```
IEntityId<T> (Ulid 기반)
├── static T New()          — 새 ID 생성
├── static T Create(Ulid)   — Ulid에서 생성
└── static T Create(string) — 문자열에서 생성
```

- `[GenerateEntityId]` 소스 생성기로 ID 타입 자동 생성
- Ulid 기반으로 시간순 정렬 가능, DB 인덱스에 유리

**구현 패턴:**

```csharp
[GenerateEntityId]
public sealed class Customer : AggregateRoot<CustomerId>, IAuditable
{
    public CustomerName Name { get; private set; }
    public Email Email { get; private set; }

    private Customer(CustomerId id, CustomerName name, Email email) : base(id)
    {
        Name = name;
        Email = email;
    }

    public static Customer Create(CustomerName name, Email email)
    {
        var customer = new Customer(CustomerId.New(), name, email);
        customer.AddDomainEvent(new CreatedEvent(customer.Id, name, email));
        return customer;
    }
}
```

**DDD 원칙 충족:**
- ID 기반 동등성: `Entity<TId>`가 구현
- 가변 상태 보호: `private set` 속성
- 부가 인터페이스: `IAuditable`, `ISoftDeletable` 제공

**참조:** [06-entities-and-aggregates.md](./06-entities-and-aggregates.md)

---

## 3. Aggregate Roots ✅

### 현재 구현

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IDomainEventDrain
    where TId : struct, IEntityId<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**DDD 원칙 충족:**
- 도메인 이벤트 컬렉션 관리
- 트랜잭션 경계로서의 Aggregate (설계 가이드 제공)
- 불변식 보호 (Aggregate 내부에서만 상태 변경)
- Cross-Aggregate 관계는 ID 참조만 사용

**참조:** [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) Part 1

---

## 4. Domain Events ✅

### 현재 구현

**이벤트 정의:**

```csharp
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
    Ulid EventId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}

public abstract record DomainEvent(
    DateTimeOffset OccurredAt,
    Ulid EventId,
    string? CorrelationId,
    string? CausationId) : IDomainEvent
{
    protected DomainEvent() : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), null, null) { }
    protected DomainEvent(string? correlationId) : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, null) { }
    protected DomainEvent(string? correlationId, string? causationId) : this(DateTimeOffset.UtcNow, Ulid.NewUlid(), correlationId, causationId) { }
}
```

**발행/구독 시스템:**

| 컴포넌트 | 인터페이스 | 구현체 | 위치 |
|---------|-----------|--------|------|
| Publisher | `IDomainEventPublisher` | `DomainEventPublisher` | Adapter Layer |
| Observable Publisher | — | `ObservableDomainEventPublisher` | Adapter Layer |
| Handler | `IDomainEventHandler<T>` | 사용자 구현 | Application Layer |

**발행 메서드:**

```csharp
public interface IDomainEventPublisher
{
    FinT<IO, Unit> Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
    FinT<IO, Unit> PublishEvents<TId>(AggregateRoot<TId> aggregateRoot) where TId : struct, IEntityId<TId>;
    FinT<IO, Seq<DomainEventResult>> PublishEventsWithResult<TId>(AggregateRoot<TId> aggregateRoot) where TId : struct, IEntityId<TId>;
}
```

**DDD 원칙 충족:**
- Aggregate Root에서만 이벤트 발행
- 중첩 record로 이벤트 정의 (소속 명확)
- 관찰 가능성 자동 통합 (Observable wrapper)

**참조:** [07-domain-events.md](./07-domain-events.md)

---

# Part 2: 구현 완료 패턴 (이전 미구현)

---

## 5. Repository Pattern ✅

### 구현 완료

`Functorium.Domains.Repositories` 네임스페이스에 공통 `IRepository<TAggregate, TId>` 인터페이스를 제공합니다.

```csharp
// Functorium.Domains.Repositories
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

**핵심 설계 결정:**
- `AggregateRoot<TId>` 제약으로 **컴파일 타임에 Aggregate Root 단위 Repository를 강제**
- 풀 CRUD (`Create` + `GetById` + `Update` + `Delete`) 표준 제공
- `IObservablePort`를 전이적으로 상속하여 기존 소스 생성기 및 DI 등록과 호환

**사용 예:**

```csharp
// 빈 인터페이스 — CRUD는 IRepository에서 상속
public interface IOrderRepository : IRepository<Order, OrderId> { }

// 도메인 전용 메서드만 추가 선언
public interface IProductRepository : IRepository<Product, ProductId>
{
    FinT<IO, Option<Product>> GetByName(ProductName name);
    FinT<IO, Seq<Product>> GetAll();
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
}
```

**참조:** `Src/Functorium/Domains/Repositories/IRepository.cs`

---

## 6. Domain Services ✅

### 구현 완료

`Functorium.Domains.Services` 네임스페이스에 `IDomainService` 마커 인터페이스를 제공합니다.

```csharp
// Functorium.Domains.Services
public interface IDomainService { }
```

**핵심 설계 결정:**
- 빈 마커 인터페이스 (Domain Service는 각자 고유한 메서드를 가짐)
- `IObservablePort` 상속 없음 (Domain Service는 순수 도메인 로직, Port/Adapter가 아님)
- 아키텍처 테스트에서 `is IDomainService`로 검증 가능

**사용 예: 주문 신용 한도 검증 서비스**

```csharp
// Domain Layer - 교차 Aggregate 순수 도메인 로직
public sealed class OrderCreditCheckService : IDomainService
{
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount)
    {
        if (orderAmount > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new Custom("CreditLimitExceeded"),
                customer.Id.ToString(),
                $"주문 금액이 고객 신용 한도를 초과합니다");

        return unit;
    }
}
```

```csharp
// Application Layer - Usecase에서 Domain Service 사용
FinT<IO, Response> usecase =
    from customer in _customerRepository.GetById(customerId)
    from unitPrice in _productCatalog.GetPrice(productId)
    from _2 in _creditCheckService.ValidateCreditLimit(customer, unitPrice.Multiply(quantity))
    from order in _orderRepository.Create(Order.Create(...))
    select new Response(...);
```

**배치 규칙:**
- Domain Service는 Domain Layer에 배치
- 순수 함수로 구현 (외부 I/O 없음)
- `IObservablePort` 의존 없음 (Port/Adapter 사용 시 Usecase에서 조율)
- `Fin<T>` 반환값은 `FinT<IO, T>` LINQ 체인에서 자동 리프팅

**참조:** `Src/Functorium/Domains/Services/IDomainService.cs`, `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/Services/OrderCreditCheckService.cs`

---

## 7. Specifications ✅

### 구현 완료

`Functorium.Domains.Specifications` 네임스페이스에 `Specification<T>` 추상 기반 클래스를 제공합니다.

```csharp
// Functorium.Domains.Specifications
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T entity);

    public Specification<T> And(Specification<T> other);
    public Specification<T> Or(Specification<T> other);
    public Specification<T> Not();

    // 연산자 오버로드: &, |, !
}
```

**핵심 설계 결정:**
- `Expression<Func<T, bool>>` 대신 `bool IsSatisfiedBy(T)` 메서드 사용 (도메인 순수성 유지)
- EfCore SQL 최적화는 Adapter에서 `switch` pattern-match로 처리
- Specification의 파라미터를 public 프로퍼티로 노출하여 pattern-match 접근 가능
- `And`/`Or`/`Not` 조합 및 `&`/`|`/`!` 연산자 오버로드 지원
- 조합 클래스(`AndSpecification<T>`, `OrSpecification<T>`, `NotSpecification<T>`)는 `internal sealed`

**구현된 실전 예제:**

| Specification | 용도 | Aggregate |
|--------------|------|-----------|
| `ProductNameUniqueSpec` | 상품명 중복 확인 (자기 제외 옵션) | Product |
| `ProductPriceRangeSpec` | 가격 범위 필터 | Product |
| `ProductLowStockSpec` | 재고 부족 필터 | Product |
| `CustomerEmailSpec` | 이메일 중복 확인 | Customer |

**Repository 통합 패턴:**

```csharp
// Port (Domain Layer)
FinT<IO, bool> Exists(Specification<Product> spec);
FinT<IO, Seq<Product>> FindAll(Specification<Product> spec);

// InMemory: IsSatisfiedBy() 직접 사용
// EfCore: switch pattern-match SQL 최적화 + IsSatisfiedBy() 폴백
```

**참조:** [10-specifications.md](./10-specifications.md), `Src/Functorium/Domains/Specifications/Specification.cs`

---

## 8. Factories ✅

### 구현 완료

Functorium은 별도의 Factory 클래스 대신 **정적 팩토리 메서드 패턴**을 채택합니다.

| 패턴 | 위치 | 용도 |
|------|------|------|
| `Create()` | Aggregate/VO 내부 정적 메서드 | 새 Aggregate 생성 (ID 자동 발급, 이벤트 발행) |
| `CreateFromValidated()` | Aggregate/VO 내부 정적 메서드 | ORM/Repository에서 기존 데이터 복원 (검증 스킵) |
| Apply 패턴 | Usecase 내부 private 메서드 | 병렬 VO 검증 후 Aggregate 생성 |
| Port 조율 패턴 | Usecase LINQ 체인 | 교차 Aggregate 데이터 조회 후 생성 |

**참조:** [06-entities-and-aggregates.md §8](./06-entities-and-aggregates.md) — Factory 패턴 전체 가이드 (패턴 상세, 적용 사례, EFCore 통합, 설계 가이드라인)

---

# Part 3: 부분 구현 / 개선 필요

---

## 9. Integration Events vs Domain Events ⚠️

### 현재 상태

모든 이벤트가 `IDomainEvent`로 통합되어 있습니다. 내부(In-Process) 이벤트와 외부(Cross-Service) 이벤트의 구분이 없습니다.

```csharp
// 현재: 단일 이벤트 타입 (추적 속성 포함)
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
    Ulid EventId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}
```

### 문제점

1. **이벤트 경계 불명확**: 같은 프로세스 내 Aggregate 간 통신과 마이크로서비스 간 통신이 구분되지 않음
2. **스키마 노출 위험**: Domain Event의 내부 구조가 외부 서비스에 그대로 노출될 수 있음
3. **전송 메커니즘 차이 미반영**: 내부 이벤트는 Mediator, 외부 이벤트는 Message Broker 사용

### 개선 방향

```csharp
// 내부 이벤트 (현재 유지)
public interface IDomainEvent { ... }

// 외부 이벤트 (제안)
public interface IIntegrationEvent
{
    string EventId { get; }
    DateTimeOffset OccurredAt { get; }
    string EventType { get; }  // 직렬화/역직렬화용 식별자
}
```

**발행 흐름:**

```
Aggregate → Domain Event → Domain Event Handler
                              ↓
                        Integration Event 변환
                              ↓
                        Message Broker (RabbitMQ 등)
                              ↓
                        외부 서비스
```

### 우선순위

**높음** — 다중 호스트 시나리오에서 필수입니다.

**TODO.md 연계:**
- L150: `내부/외부 도메인 이벤트 구분?`
- L355: `Event: Internal(Mediator) vs External(RabbitMQ)` (alpha.4)

---

## 10. Outbox Pattern ⚠️

### 현재 상태

트랜잭션 보장 이벤트 발행이 **구현되어 있지 않습니다**. 도메인 이벤트는 Mediator를 통해 In-Process로 즉시 발행됩니다.

### 문제점

1. **이벤트 유실 가능**: DB 트랜잭션 커밋 후 이벤트 발행 실패 시 이벤트가 유실됨
2. **트랜잭션 불일치**: Aggregate 저장과 이벤트 발행이 원자적이지 않음
3. **재시도 불가**: 실패한 이벤트를 다시 발행할 메커니즘이 없음

### 개선 방향

```
┌─────────────────────────────────────┐
│           트랜잭션 경계              │
│                                     │
│  1. Aggregate 저장                  │
│  2. Outbox 테이블에 이벤트 저장      │
│                                     │
└─────────────────────────────────────┘
           ↓ (비동기)
┌─────────────────────────────────────┐
│  3. Outbox Processor가 이벤트 읽기  │
│  4. Message Broker로 발행           │
│  5. Outbox 레코드 처리 완료 표시     │
└─────────────────────────────────────┘
```

**필요 컴포넌트:**

| 컴포넌트 | 역할 |
|---------|------|
| `OutboxMessage` | 이벤트 직렬화 엔티티 |
| `IOutboxRepository` | Outbox CRUD |
| `OutboxProcessor` | Background Service로 Outbox 폴링/발행 |

### 우선순위

**중간** — Integration Event 구분 이후에 구현합니다. 단일 호스트에서는 불필요합니다.

**TODO.md 연계:**
- L300: `Outbox` (alpha.7)
- L377~378: `복수 호스트 예제`, `Outbox`

---

## 11. Unit of Work ✅

### 구현 완료

`Functorium.Applications.Persistence` 네임스페이스에 `IUnitOfWork` 인터페이스를 제공하며, EF Core 및 InMemory 구현체가 구현되어 있습니다.

**인터페이스:**

```csharp
// Functorium.Applications.Persistence
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
}
```

**구현체:**

| 구현체 | 위치 | 용도 |
|--------|------|------|
| `EfCoreUnitOfWork` | Adapters.Persistence | EF Core `DbContext.SaveChangesAsync()` 호출, `DbUpdateException` 처리 |
| `InMemoryUnitOfWork` | Adapters.Persistence | 테스트/개발용, 즉시 성공 반환 |

**Usecase 사용 패턴:**

Command Handler는 Repository만 호출하며, SaveChanges와 이벤트 발행은 `UsecaseTransactionPipeline`이 자동 처리합니다:

```csharp
FinT<IO, Response> usecase =
    from product in _productRepository.Create(newProduct)  // Repository가 IDomainEventCollector.Track() 자동 호출
    select new Response(product.Id.ToString());
// SaveChanges + 도메인 이벤트 발행은 UsecaseTransactionPipeline이 자동 처리
```

> **이전 패턴과의 차이**: 이전에는 Usecase에서 `_unitOfWork.SaveChanges()` → `_eventPublisher.PublishEvents()`를 LINQ 체인에서 직접 호출했으나, 파이프라인 도입으로 이 책임이 `UsecaseTransactionPipeline`으로 이전되었습니다. 자세한 내용은 [11-usecases-and-cqrs.md §트랜잭션과 이벤트 발행](./11-usecases-and-cqrs.md#트랜잭션과-이벤트-발행-usecasetransactionpipeline)을 참조하세요.

**핵심 설계 결정:**
- `IObservablePort` 상속으로 `[GenerateObservablePort]` 소스 생성기 및 DI 등록 호환
- EF Core 구현체에서 `DbUpdateConcurrencyException` → `ConcurrencyConflict`, `DbUpdateException` → `DatabaseUpdateFailed` 에러 변환
- DI 등록 시 Strategy 패턴으로 InMemory/EfCore 전환 가능

**참조:** `Src/Functorium/Applications/Persistence/IUnitOfWork.cs`, `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreUnitOfWork.cs`

---

## 12. Domain Event Versioning ⚠️

### 현재 상태

도메인 이벤트 스키마 진화에 대한 지원이 **없습니다**. 이벤트는 단순 record로 정의되며 버전 정보가 없습니다.

```csharp
// 현재: 버전 없는 이벤트
public sealed record CreatedEvent(
    ProductId ProductId,
    ProductName Name,
    Money Price) : DomainEvent;
```

### 문제점

1. **스키마 변경 시 역직렬화 실패**: 필드 추가/삭제 시 기존 이벤트를 읽을 수 없음
2. **이벤트 소싱 적용 불가**: 이벤트 히스토리에서 Aggregate를 재구성하려면 버전별 마이그레이션 필요
3. **호환성 관리 불가**: 소비자가 어떤 버전의 이벤트를 처리할 수 있는지 알 수 없음

### 개선 방향

```csharp
// 제안: 이벤트 메타데이터 확장
public abstract record DomainEvent(
    DateTimeOffset OccurredAt,
    Ulid EventId,
    string? CorrelationId,
    string? CausationId) : IDomainEvent
{
    public virtual int Version => 1;
}

// 사용 예: 버전 2 이벤트
public sealed record CreatedEventV2(
    ProductId ProductId,
    ProductName Name,
    Money Price,
    string? Description) : DomainEvent
{
    public override int Version => 2;
}
```

### 우선순위

**낮음** — Integration Event 구분 및 외부 Message Broker 도입 이후에 의미가 있습니다.

---

## 13. DTO Mapping Strategy ✅

### 현재 상태

레이어별 DTO 전략이 **구현되었습니다**. 각 레이어가 자체 DTO를 소유하며, 수동 매핑으로 레이어 경계를 명확히 분리합니다.

**구현된 레이어별 DTO 역할:**

| 레이어 | DTO 유형 | 역할 |
|--------|---------|------|
| Presentation | `Endpoint.Response` record | 외부 인터페이스 계약 (API 응답) |
| Application | `Command/Query.Response` record | Usecase 입출력 |
| Persistence | `XxxModel` POCO class | DB 스키마 매핑 (primitive 타입) |

**구현된 변환 패턴:**

```
API Request → Endpoint.Request → Usecase.Request → Domain Entity → PersistenceModel
API Response ← Endpoint.Response ← Usecase.Response ← Domain Entity ← PersistenceModel
```

**매핑 방식:**

| 시나리오 | 방식 |
|---------|------|
| Usecase Response → Endpoint Response | `FinResponse<A>.Map<B>()` |
| Domain Entity ↔ Persistence Model | 수동 매핑 (확장 메서드: `ToModel()` / `ToDomain()`) |
| Application DTO 공유 | `Usecases/Xxx/Dtos/` 네임스페이스 |

**주요 구현 사항:**

1. **Presentation Layer**: 모든 Endpoint에 `new sealed record Response(...)` 추가, `result.Map(r => new Response(...))` 패턴
2. **Application Layer**: 중복 `ProductDto` → 공유 `ProductSummaryDto`로 통합
3. **Persistence Layer**: `ProductModel`, `CustomerModel`, `OrderModel`, `TagModel` POCO 도입, `HasConversion()` 제거
4. **Domain 순수성**: 도메인 엔티티에서 ORM용 파라미터 없는 생성자 제거
5. **Mapper 라운드트립 테스트**: `Domain → Model → Domain` 검증 테스트 추가

**상세 리뷰**: [dto-strategy-review.md](./dto-strategy-review.md) — Eric Evans DDD & Hexagonal Architecture 관점의 종합 리뷰 (전체 평가: 우수)

---

## 14. Resilience Patterns ⚠️

### 현재 상태

외부 서비스 호출에 대한 복원력 패턴이 **구현되어 있지 않습니다**. Polly 통합이 없습니다.

### 문제점

1. **외부 서비스 장애 전파**: API 호출 실패가 전체 시스템에 영향
2. **재시도 로직 부재**: 일시적 오류에 대한 재시도가 Adapter마다 수동 구현 필요
3. **서킷 브레이커 없음**: 장애 서비스에 대한 반복 호출 차단 불가

### 개선 방향

```csharp
// Adapter에서 Polly 통합 예시
[GenerateObservablePort]
public sealed class ExternalPricingApiService : IExternalPricingService
{
    private readonly HttpClient _httpClient;  // Polly 정책 적용된 HttpClient

    public FinT<IO, Money> GetPrice(ProductId productId) =>
        IO.liftAsync(async () =>
        {
            // HttpClient에 Polly ResilienceHandler가 설정됨
            var response = await _httpClient.GetAsync($"/api/prices/{productId}");
            // ...
        });
}
```

**Polly 통합 위치:**

| 패턴 | 적용 레이어 | 방식 |
|------|-----------|------|
| Retry | Adapter | `HttpClientFactory` + Polly |
| Circuit Breaker | Adapter | `HttpClientFactory` + Polly |
| Timeout | Adapter | `HttpClientFactory` + Polly |
| Bulkhead | Adapter | `HttpClientFactory` + Polly |

### 우선순위

**중간** — 외부 서비스 Adapter 구현과 함께 도입합니다.

**TODO.md 연계:**
- L27: `io + polly: timeout, retry, 서킷브레이커, ...`

---

# Part 4: 전략적 개선

---

## 15. 도메인 로직 배치 전략

도메인 로직을 어디에 배치할지는 DDD에서 가장 중요한 설계 결정 중 하나입니다.

### 배치 기준

| 배치 위치 | 기준 | 예시 |
|----------|------|------|
| **Entity 메서드** | 단일 Aggregate 내부 상태 변경 | `Order.AddItem()`, `Product.DeductStock()` |
| **Value Object** | 값의 검증, 변환, 연산 | `Money.Add()`, `Email.Validate()` |
| **Domain Service** | 여러 Aggregate 참조하는 순수 도메인 로직 | `PricingService.CalculateDiscount(order, customer)` |
| **Usecase** | 조율(orchestration), 부수 효과 위임 | Repository 호출, Event 발행, 트랜잭션 관리 |

### 판단 플로우차트

```
로직이 단일 Aggregate에 속하는가?
├── YES → Entity 메서드 (상태 변경) 또는 Value Object (값 연산)
└── NO
    ├── 외부 I/O가 필요한가?
    │   ├── YES → Usecase에서 조율
    │   └── NO → Domain Service (순수 도메인 로직)
    └── 여러 Aggregate의 상태를 변경하는가?
        ├── YES → Domain Event + 별도 Handler
        └── NO → Domain Service
```

### 안티패턴

| 안티패턴 | 문제 | 해결 |
|---------|------|------|
| **Fat Usecase** | 비즈니스 로직이 Usecase에 집중 | Entity 메서드 또는 Domain Service로 이동 |
| **Anemic Domain Model** | Entity가 getter/setter만 보유 | Entity에 비즈니스 메서드 추가 |
| **Domain Service 남용** | 모든 로직을 Domain Service에 배치 | 단일 Aggregate 로직은 Entity 메서드로 |

### 현재 개선 필요 사항

TODO.md L535에서 확인된 사례:

```csharp
// 현재: Usecase에 위치한 도메인 규칙
from exists in _productRepository.ExistsByName(request.Name)
from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
```

**분석:** `제품명 중복 검사`는 비즈니스 규칙이지만, Repository 조회가 필요하므로 Usecase에서 조율하는 것이 적절합니다. 다만, 에러 타입은 `DomainError`가 아닌 `ApplicationError`를 사용해야 합니다 (외부 I/O 의존).

---

## 16. Aggregate 설계 가이드 강화

### 현재 가이드 범위

[06-entities-and-aggregates.md](./06-entities-and-aggregates.md)에서 Aggregate 설계 원칙을 다루고 있으나, 실전 의사결정을 돕는 구체적 사례가 추가로 필요합니다.

### 추가 권장 콘텐츠

#### A. Aggregate 경계 의사결정 트리

```
함께 변경되어야 하는 데이터인가?
├── YES → 같은 Aggregate
│   └── 트랜잭션 일관성이 필수인가?
│       ├── YES → 같은 Aggregate (강한 일관성)
│       └── NO → 최종 일관성 → Domain Event
└── NO → 별도 Aggregate
    └── 참조가 필요한가?
        ├── YES → ID 참조만 사용
        └── NO → 완전 독립
```

#### B. Aggregate 크기 가이드

| 규모 | Entity 수 | 적합 시나리오 |
|------|----------|-------------|
| 소형 | 1~2 | 대부분의 경우 (Product, Customer) |
| 중형 | 3~5 | 주문 + 주문항목 (Order + OrderItem) |
| 대형 | 5+ | 주의: 분리를 검토해야 함 |

#### C. 실전 사례

**사례 1: 주문 시스템**
```
Order (Aggregate Root)
├── OrderItem (자식 Entity) — 주문과 함께 생성/삭제
├── ShippingAddress (Value Object)
└── OrderStatus (Value Object)

Customer (별도 Aggregate) — Order에서 CustomerId로만 참조
Product (별도 Aggregate) — OrderItem에서 ProductId로만 참조
```

**사례 2: 게시판 시스템**
```
Post (Aggregate Root)
├── PostContent (Value Object)
└── Tag 참조 (TagId 컬렉션)

Comment (별도 Aggregate) — PostId로 참조, 독립적 생명주기
Tag (별도 Aggregate) — 여러 Post에서 공유
```

---

## 17. 로드맵 및 우선순위

### alpha 버전별 구현 계획

| 개선 항목 | 우선순위 | 목표 버전 | 의존성 | TODO.md 참조 |
|----------|---------|----------|--------|-------------|
| DTO Mapping Strategy | 높음 | alpha.2 | ✅ 구현됨 | L112~116 |
| IRepository 공통 인터페이스 | 높음 | alpha.4 | EFCore 통합 | L354 |
| Unit of Work | ✅ 완료 | — | EFCore 통합 | L365 |
| Integration Events | 높음 | alpha.4 | — | L150, L355 |
| IDomainService 마커 | ✅ 완료 | alpha.3 | — | L535 |
| Resilience Patterns (Polly) | 중간 | alpha.4 | 외부 Adapter | L27 |
| Outbox Pattern | 중간 | alpha.7 | Integration Events, UoW | L300, L378 |
| Specification Pattern | ✅ 완료 | — | — | — |
| Factory 패턴 | ✅ 완료 | — | — | — |
| Domain Event Versioning | 낮음 | alpha.7+ | Integration Events | — |

### 구현 순서 의존 관계

```
DTO Mapping Strategy ──────────────────────────────────────────┐
                                                               │
IDomainService 마커 ───────────────────────────────────────────┤
                                                               │
IRepository ──→ Unit of Work ──→ EFCore 통합 ──→ Specification │
                                                               │
Integration Events ──→ Outbox Pattern ──→ Wolverine           │
                   └──→ Event Versioning                       │
                                                               │
Resilience (Polly) ──→ 외부 API Adapter                        │
                                                               │
                                                       가이드 문서
```

### DDD 전술적 패턴 커버리지 요약

| Eric Evans DDD 패턴 | Functorium 상태 | 문서 |
|--------------------|----------------|------|
| Value Object | ✅ 완전 구현 | [05-value-objects.md](./05-value-objects.md) |
| Entity | ✅ 완전 구현 | [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) |
| Aggregate | ✅ 완전 구현 | [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) |
| Domain Event | ✅ 완전 구현 | [07-domain-events.md](./07-domain-events.md) |
| Repository | ✅ 공통 인터페이스 구현 | 본 문서 §5 |
| Domain Service | ✅ 마커 인터페이스 구현 | 본 문서 §6 |
| Specification | ✅ 완전 구현 | [10-specifications.md](./10-specifications.md) |
| Factory | ✅ 정적 팩토리 메서드 패턴 구현 | 본 문서 §8 |
| Application Service | ✅ CQRS Usecase | [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) |
| Integration Event | ⚠️ 내부/외부 구분 없음 | 본 문서 §9 |
| Unit of Work | ✅ 완전 구현 | 본 문서 §11 |

---

## 참고 문서

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) — Aggregate 설계 원칙
- [07-domain-events.md](./07-domain-events.md) — 도메인 이벤트
- [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) — Usecase/CQRS
- [12-ports.md](./12-ports.md) — Port 아키텍처
- TODO.md — 프로젝트 로드맵
