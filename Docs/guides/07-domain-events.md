# 도메인 이벤트 (Domain Events)

이 문서는 Functorium 프레임워크에서 도메인 이벤트를 정의, 발행, 구독하는 방법을 설명합니다.

## 목차

- [요약](#요약)
- [왜 도메인 이벤트인가](#왜-도메인-이벤트인가)
- [도메인 이벤트란 무엇인가 (WHAT)](#도메인-이벤트란-무엇인가-what)
  - [IHasDomainEvents / IDomainEventDrain 패턴](#ihasdomainevents--idomaineventdrain-패턴)
- [이벤트 정의 (HOW)](#이벤트-정의-how)
- [이벤트 발행 (HOW)](#이벤트-발행-how)
- [이벤트 핸들러 구현 (HOW)](#이벤트-핸들러-구현-how)
- [테스트 패턴](#테스트-패턴)
- [체크리스트](#체크리스트)
- [향후 고급 패턴](#향후-고급-패턴)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

---

## 요약

### 주요 명령

```csharp
// 도메인 이벤트 정의 (Aggregate 내 중첩 record)
public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;

// 이벤트 발행 (AggregateRoot 내부)
AddDomainEvent(new CreatedEvent(Id, totalAmount));

// Event Handler 구현
public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>

// 핸들러 등록 (DI)
services.RegisterDomainEventHandlersFromAssembly(AssemblyReference.Assembly);
```

### 주요 절차

1. **이벤트 정의**: Aggregate Root 내부에 `sealed record`로 `DomainEvent` 상속, 과거형 이름 사용
2. **이벤트 발행**: 상태 변경 직후 `AddDomainEvent()` 호출
3. **이벤트 핸들러 작성**: `IDomainEventHandler<T>` 구현, `On{EventName}` 네이밍 패턴
4. **핸들러 등록**: `RegisterDomainEventHandlersFromAssembly`로 스캔 등록
5. **자동 처리**: `UsecaseTransactionPipeline`이 SaveChanges 후 이벤트 발행 자동 수행

### 주요 개념

| 개념 | 설명 |
|------|------|
| `IDomainEvent` | `INotification` 확장, `OccurredAt`, `EventId`, `CorrelationId`, `CausationId` 포함 |
| `DomainEvent` | 기반 abstract record, 시각/ID 자동 설정 |
| `IHasDomainEvents` | 읽기 전용 이벤트 조회 (public) |
| `IDomainEventDrain` | 이벤트 정리 인터페이스 (internal, 인프라 전용) |
| `UsecaseTransactionPipeline` | SaveChanges → 이벤트 발행 자동 처리 |
| 중첩 클래스 이벤트 | `Product.CreatedEvent` 형태로 소유권 명시 |

---

## 왜 도메인 이벤트인가

도메인 이벤트는 DDD(Domain-Driven Design)에서 **"도메인에서 발생한 중요한 사건"**을 명시적으로 표현하는 전술 패턴입니다.

### 도메인 이벤트가 해결하는 문제

**Aggregate 간 최종 일관성 (Eventual Consistency)**:
하나의 트랜잭션에서 하나의 Aggregate만 변경하고, 다른 Aggregate의 변경은 이벤트를 통해 비동기로 처리합니다. 이를 통해 Aggregate 경계를 깨뜨리지 않으면서도 도메인 간 협업이 가능합니다.

**관심사 분리**:
핵심 도메인 로직과 부수 효과(로깅, 알림, 외부 시스템 연동)를 분리합니다. 주문 생성 로직은 "주문을 만드는 것"에만 집중하고, 이메일 발송이나 재고 차감은 이벤트 핸들러에서 처리합니다.

**감사 추적 (Audit Trail)**:
도메인에서 무슨 일이 발생했는지 이벤트로 기록합니다. 각 이벤트는 발생 시각(`OccurredAt`)을 포함하므로 시간 순서대로 도메인의 변화를 추적할 수 있습니다.

**확장성**:
새로운 부수 효과가 필요할 때 기존 코드를 수정하지 않고 새 이벤트 핸들러를 추가하면 됩니다 (Open-Closed Principle).

---

## 도메인 이벤트란 무엇인가 (WHAT)

도메인 이벤트는 도메인에서 발생한 중요한 사건을 표현합니다. AggregateRoot에서만 발행할 수 있습니다.

### 도메인 이벤트의 특성

| 특성 | 설명 | 예시 |
|------|------|------|
| **과거형 (Past Tense)** | 이미 발생한 사실을 표현 | `CreatedEvent`, `ConfirmedEvent` |
| **불변 (Immutable)** | 한번 생성되면 변경 불가 | `sealed record`로 정의 |
| **시간 정보 포함** | 발생 시각을 기록 | `OccurredAt` 속성 |
| **이벤트 식별** | 고유 ID로 중복 방지 | `EventId` (Ulid) |
| **요청 추적** | 동일 요청의 이벤트 연결 | `CorrelationId` |
| **인과 관계** | 이벤트 간 원인-결과 추적 | `CausationId` |

### IDomainEvent / DomainEvent

**위치**: `Functorium.Domains.Events`

```csharp
// 인터페이스 — Mediator.INotification 확장으로 Pub/Sub 통합
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
    Ulid EventId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}

// 기반 record
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

**이벤트 추적성 (Traceability)**:
- `EventId`: 이벤트 고유 식별자. 중복 처리 방지(멱등성) 및 이벤트 추적에 사용됩니다.
- `CorrelationId`: 동일한 요청에서 발생한 이벤트를 그룹으로 추적합니다.
- `CausationId`: 이 이벤트를 발생시킨 이전 이벤트의 ID로, 이벤트 간 인과 관계를 추적합니다.

### CorrelationId 전파 흐름

`CorrelationId`는 하나의 요청에서 발생한 모든 도메인 이벤트를 연결하는 비즈니스 수준 식별자입니다:

```
HTTP Request
  → 미들웨어: CorrelationId 생성 또는 헤더에서 추출
    → Usecase 실행
      → Entity.AddDomainEvent(new CreatedEvent(...) { CorrelationId = correlationId })
        → Event Handler: 동일 CorrelationId로 이벤트 추적
```

| 식별자 | 수준 | 용도 |
|--------|------|------|
| `CorrelationId` | 비즈니스 | 동일 요청에서 발생한 이벤트 그룹핑 |
| OpenTelemetry `TraceId` | 인프라 | 분산 시스템 간 요청 추적 (span 기반) |

두 식별자는 독립적이지만 보완적입니다. `CorrelationId`로 비즈니스 흐름을 추적하고, `TraceId`로 인프라 성능을 분석합니다.

### 이벤트 명명 규칙

이벤트 이름은 과거형을 사용합니다:

| 도메인 행위 | 이벤트 이름 |
|------------|------------|
| 생성 | `CreatedEvent` |
| 확정 | `ConfirmedEvent` |
| 취소 | `CancelledEvent` |
| 배송 | `ShippedEvent` |

### Functorium 타입 계층

```
IDomainEvent : INotification (인터페이스)
├── OccurredAt (DateTimeOffset)
├── EventId (Ulid)
├── CorrelationId (string?)
└── CausationId (string?)
    │
    └── DomainEvent (abstract record)
        ├── 기본 생성자: OccurredAt, EventId 자동 설정
        ├── CorrelationId 생성자: 요청 추적 ID 지정
        ├── 전체 생성자: CorrelationId + CausationId 지정
        └── 사용자 정의 이벤트들이 상속
```

### IHasDomainEvents / IDomainEventDrain 패턴

AggregateRoot에서 도메인 이벤트를 관리하는 두 인터페이스가 분리되어 있습니다:

```csharp
// 도메인 계층의 읽기 전용 계약 — 이벤트 조회만 허용
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
}

// 인프라용 이벤트 정리 인터페이스 (internal)
internal interface IDomainEventDrain : IHasDomainEvents
{
    void ClearDomainEvents();
}
```

**설계 원칙**: 도메인 이벤트는 **불변의 사실(fact)입니다**. 도메인 계약(`IHasDomainEvents`)에서는 이벤트 삭제를 허용하지 않으며, 이벤트 정리는 인프라 관심사(`IDomainEventDrain`)로 분리합니다.

| 인터페이스 | 가시성 | 역할 |
|-----------|--------|------|
| `IHasDomainEvents` | `public` | 도메인 계층에서 이벤트 목록 조회 |
| `IDomainEventDrain` | `internal` | 이벤트 발행 후 정리 (인프라 전용) |

> **참고**: `IDomainEventDrain`은 `internal`이지만, `AggregateRoot<TId>.ClearDomainEvents()`는 `public`입니다. 이는 테스트 코드에서 `order.ClearDomainEvents()`를 직접 호출하여 이전 이벤트를 정리할 수 있도록 하기 위한 의도적인 설계입니다. 프로덕션 코드에서 `ClearDomainEvents()`는 인프라(Publisher)만 호출해야 합니다.

---

## 이벤트 정의 (HOW)

도메인 이벤트는 해당 Entity의 **중첩 클래스**로 정의합니다:

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Domain Events

    // 도메인 이벤트 (중첩 클래스)
    public sealed record CreatedEvent(OrderId OrderId, CustomerId CustomerId, Money TotalAmount) : DomainEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId, string Reason) : DomainEvent;

    #endregion

    // Entity 구현...
}
```

**장점**:
- 이벤트 소유권이 타입 시스템에서 명확 (`Order.CreatedEvent`)
- IntelliSense에서 `Order.`만 치면 관련 이벤트 모두 표시
- Entity 이름 중복 제거 (`OrderCreatedEvent` → `Order.CreatedEvent`)
- **Event Handler에서 이벤트 발행 주체 명시**: Handler가 `IDomainEventHandler<Product.CreatedEvent>`를 상속받으면, 코드를 읽는 것만으로 "Product Entity가 발행한 이벤트"임을 즉시 파악 가능

**사용 예시**:
```csharp
// Entity 내부에서 (짧게)
AddDomainEvent(new CreatedEvent(Id, customerId, totalAmount));

// 외부에서 (명시적)
public void Handle(Order.CreatedEvent @event) { ... }
```

---

## 이벤트 발행 (HOW)

### AggregateRoot에서 이벤트 수집

AggregateRoot 내에서 `AddDomainEvent()`를 사용하여 이벤트를 수집합니다.

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
    #region Domain Events

    public sealed record CreatedEvent(OrderId OrderId, Money TotalAmount) : DomainEvent;
    public sealed record ShippedEvent(OrderId OrderId, Address ShippingAddress) : DomainEvent;

    #endregion

    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    private Order(OrderId id, Money totalAmount) : base(id)
    {
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    // Create: 이미 검증된 Value Object를 직접 받음
    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        // 생성 이벤트 발행 (내부에서는 짧게)
        order.AddDomainEvent(new CreatedEvent(id, totalAmount));
        return order;
    }

    public sealed record InvalidStatus : DomainErrorType.Custom;

    public Fin<Unit> Ship(Address address)
    {
        if (Status != OrderStatus.Confirmed)
            return DomainError.For<Order>(
                new InvalidStatus(),
                Status.ToString(),
                "Order must be confirmed before shipping");

        Status = OrderStatus.Shipped;
        // 배송 이벤트 발행
        AddDomainEvent(new ShippedEvent(Id, address));
        return unit;
    }
}
```

### UsecaseTransactionPipeline 통합

`IDomainEvent`는 Mediator의 `INotification`을 확장하여 Pub/Sub 통합을 지원합니다.

**SaveChanges와 도메인 이벤트 발행은 `UsecaseTransactionPipeline`이 자동으로 처리합니다.** Usecase에서 `IUnitOfWork`나 `IDomainEventPublisher`를 직접 주입할 필요가 없습니다:

```csharp
internal sealed class Usecase(
    IProductRepository productRepository)   // Repository만 주입
    : ICommandUsecase<Request, Response>
{
    private readonly IProductRepository _productRepository = productRepository;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // ... 기존 검증 로직 ...

        FinT<IO, Response> usecase =
            from exists in _productRepository.ExistsByName(productName)
            from _ in guard(!exists, /* error */)
            from product in _productRepository.Create(newProduct)  // Repository가 IDomainEventCollector.Track() 자동 호출
            select new Response(...);
        // SaveChanges + 도메인 이벤트 발행은 UsecaseTransactionPipeline이 자동 처리

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### 파이프라인 발행 흐름

`UsecaseTransactionPipeline`은 Command Usecase에 대해 UoW 커밋과 도메인 이벤트 발행을 자동 처리합니다 (Query는 바이패스).

**이벤트 수집 시점**: Usecase 실행 중 Repository의 `Create`/`Update` 메서드가 `IDomainEventCollector.Track(aggregate)`를 호출하여 변경된 Aggregate를 추적 대상으로 등록합니다. Aggregate 내부에서는 `AddDomainEvent()`로 이벤트를 수집합니다.

**파이프라인 실행 순서**:

1. **Handler 실행** (`next()`) → 실패 시 커밋 안함, 실패 응답 즉시 반환
2. **`UoW.SaveChanges()`** → 트랜잭션 커밋. 실패 시 이벤트 발행 안함, 실패 응답 반환
3. **`IDomainEventPublisher.PublishTrackedEvents()`** → `IDomainEventCollector`에서 추적된 Aggregate의 이벤트를 수집, Mediator를 통해 발행, 발행 후 `ClearDomainEvents()` 호출

```
Usecase Handler 실행
  → Repository.Create(entity)  ─→ IDomainEventCollector.Track(entity)
  → entity.AddDomainEvent(...)  ─→ entity.DomainEvents에 이벤트 축적
  → Handler 완료 (성공)
    → UoW.SaveChanges()         ─→ DB 커밋
    → PublishTrackedEvents()    ─→ 추적된 Aggregate의 이벤트 발행 → ClearDomainEvents()
```

### 트랜잭션 고려사항

| 상황 | 동작 |
|------|------|
| 저장 성공, 이벤트 발행 성공 | 정상 처리 |
| 저장 실패 | 이벤트 발행 안 함 (파이프라인이 Fail 응답 반환) |
| 저장 성공, 이벤트 발행 실패 | 저장은 커밋됨, 성공 응답 유지 (eventual consistency) |

- 이벤트 발행은 `SaveChanges()` 성공 후에만 실행됩니다 (파이프라인 보장)
- 발행 실패 시 비즈니스 로직은 이미 커밋됨 (eventual consistency, 경고 로그 기록)
- 강한 일관성이 필요하면 Outbox 패턴을 고려하세요

> **참조**: 파이프라인 상세는 [11-usecases-and-cqrs.md §트랜잭션과 이벤트 발행](./11-usecases-and-cqrs.md#트랜잭션과-이벤트-발행-usecasetransactionpipeline)을 참조하세요.

---

## 이벤트 핸들러 구현 (HOW)

### Event Handler란?

Event Handler는 **Event-Driven Use Case**입니다. Command/Query Use Case와 동일하게 Application Layer에 속하지만, 트리거가 다릅니다:

| Use Case 유형 | 트리거 | 역할 |
|---------------|--------|------|
| Command | 외부 요청 (쓰기) | 상태 변경 |
| Query | 외부 요청 (읽기) | 데이터 조회 |
| **Event Handler** | 도메인 이벤트 | 부수 효과 수행 |

### 중첩 클래스 이벤트의 장점

도메인 이벤트가 Entity의 중첩 클래스로 정의되면(`Product.CreatedEvent`), Event Handler 선언만으로 **이벤트 발행 주체**가 명확해집니다:

```csharp
// Handler 선언만 보면 "Product가 발행한 CreatedEvent"임을 즉시 파악
public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>
```

| 비교 | 중첩 클래스 이벤트 | 독립 클래스 이벤트 |
|------|-------------------|-------------------|
| Handler 선언 | `IDomainEventHandler<Product.CreatedEvent>` | `IDomainEventHandler<ProductCreatedEvent>` |
| 발행 주체 파악 | **타입 시스템에서 명시** (`Product.`) | 네이밍 컨벤션에 의존 |
| IntelliSense | `Product.` 입력 시 관련 이벤트 목록 표시 | 전체 이벤트 중 검색 필요 |
| 응집도 | Entity와 이벤트가 함께 배치 | 이벤트가 별도 파일/폴더에 분산 |

### 네이밍 규칙

| 핸들러 유형 | 명명 패턴 | 예시 |
|------------|----------|------|
| Command/Query Handler | `{Command/Query}Handler` | `CreateProductHandler`, `GetProductHandler` |
| Domain Event Handler | `On{EventName}` | `OnProductCreated`, `OnOrderConfirmed` |

Domain Event Handler는 `On` 접두사만 사용합니다:
- `On` 접두사가 이미 이벤트 핸들러임을 나타내므로 `Handler` 접미사는 중복
- Command/Query Handler와 자연스럽게 구분됨
- 간결하고 가독성 향상

| 구분 | 패턴 | 예시 |
|------|------|------|
| 파일명 | `On{EventName}.cs` | `OnProductCreated.cs` |
| 클래스명 | `On{EventName}` | `OnProductCreated` |

### 폴더 위치

Event Handler는 관련 엔티티의 Usecases 폴더에 Command, Query와 함께 배치합니다:

```
Usecases/
└── Products/
    ├── CreateProductCommand.cs      # Command
    ├── GetProductByIdQuery.cs       # Query
    └── OnProductCreated.cs          # Event Handler
```

### 기본 구조

```csharp
using Functorium.Applications.Events;

namespace {프로젝트}.Application.Usecases.{엔티티};

/// <summary>
/// {이벤트} 핸들러 - {처리 내용 설명}
/// </summary>
public sealed class On{EventName} : IDomainEventHandler<{Entity}.{Event}>
{
    public On{EventName}(/* 의존성 주입 */)
    {
    }

    public ValueTask Handle({Entity}.{Event} notification, CancellationToken cancellationToken)
    {
        // 부수 효과 처리: 로깅, 알림, 외부 시스템 연동 등
        return ValueTask.CompletedTask;
    }
}
```

### 완전한 예제

```csharp
using Functorium.Applications.Events;
using LayeredArch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Product.CreatedEvent 핸들러 - 상품 생성 로깅.
/// </summary>
public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>
{
    private readonly ILogger<OnProductCreated> _logger;

    public OnProductCreated(ILogger<OnProductCreated> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.CreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Product created: {ProductId}, Name: {Name}, Price: {Price}",
            notification.ProductId,
            notification.Name,
            notification.Price);

        return ValueTask.CompletedTask;
    }
}
```

### 사용 시나리오

| 시나리오 | 설명 |
|----------|------|
| 로깅/감사 | 도메인 이벤트 기록 |
| 알림 발송 | 이메일, 푸시 알림 등 |
| 외부 시스템 연동 | 결제, 배송 시스템 호출 |
| 캐시 무효화 | 관련 캐시 갱신 |
| 검색 인덱스 업데이트 | Elasticsearch 등 동기화 |

### 핸들러 등록

> **주의**: `Mediator.SourceGenerator`는 해당 패키지가 참조된 프로젝트 내의 핸들러만 자동 등록합니다.
> 다른 어셈블리(예: Application 레이어)의 핸들러는 명시적으로 등록해야 합니다.

Scrutor를 사용하여 어셈블리에서 핸들러를 스캔하고 등록합니다:

```csharp
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
services.RegisterDomainEventPublisher();  // IDomainEventPublisher 등록

// Application 레이어의 도메인 이벤트 핸들러 등록
services.RegisterDomainEventHandlersFromAssembly(
    YourApp.Application.AssemblyReference.Assembly);
```

`RegisterDomainEventHandlersFromAssembly`는 Scrutor의 `Scan()` API를 사용하여 지정된 어셈블리에서 `IDomainEventHandler<T>` 구현체를 자동으로 검색하고 `INotificationHandler<T>`로 등록합니다.

---

## 테스트 패턴

### 이벤트 발행 검증

Entity의 상태 변경 후 `DomainEvents` 컬렉션에 올바른 이벤트가 추가되었는지 검증합니다:

```csharp
[Fact]
public void Create_ShouldRaise_CreatedEvent()
{
    // Arrange & Act
    var order = Order.Create(Money.Create(10000m).ThrowIfFail());

    // Assert
    order.DomainEvents.ShouldContain(e => e is Order.CreatedEvent);
}

[Fact]
public void Confirm_ShouldRaise_ConfirmedEvent()
{
    // Arrange
    var order = Order.Create(Money.Create(10000m).ThrowIfFail());
    order.ClearDomainEvents();  // 생성 이벤트 제거

    // Act
    var result = order.Confirm();

    // Assert
    result.IsSucc.ShouldBeTrue();
    order.DomainEvents.ShouldContain(e => e is Order.ConfirmedEvent);
}
```

### 이벤트 데이터 검증

이벤트에 올바른 데이터가 포함되어 있는지 검증합니다:

```csharp
[Fact]
public void Create_CreatedEvent_ShouldContainCorrectData()
{
    // Arrange & Act
    var amount = Money.Create(10000m).ThrowIfFail();
    var order = Order.Create(amount);

    // Assert
    var createdEvent = order.DomainEvents
        .OfType<Order.CreatedEvent>()
        .ShouldHaveSingleItem();

    createdEvent.OrderId.ShouldBe(order.Id);
    createdEvent.TotalAmount.ShouldBe(amount);
}
```

### 이벤트 핸들러 단위 테스트

Event Handler는 의존성을 모킹하여 단위 테스트합니다:

```csharp
[Fact]
public async Task Handle_ShouldLogProductCreation()
{
    // Arrange
    var logger = Substitute.For<ILogger<OnProductCreated>>();
    var handler = new OnProductCreated(logger);
    var @event = new Product.CreatedEvent(ProductId.New(), "Test Product", 1000m);

    // Act
    await handler.Handle(@event, CancellationToken.None);

    // Assert
    logger.ReceivedWithAnyArgs(1).LogInformation(default!);
}
```

---

## 체크리스트

### 이벤트 정의

- [ ] 이벤트 이름이 과거형인가? (`CreatedEvent`, `UpdatedEvent`)
- [ ] 이벤트가 Aggregate Root의 중첩 record로 정의되어 있는가?
- [ ] `DomainEvent` 기반 record를 상속하는가?
- [ ] 이벤트에 필요한 식별자(EntityId)가 포함되어 있는가?

### 이벤트 발행

- [ ] `AddDomainEvent()`가 상태 변경 직후 호출되는가?
- [ ] `UsecaseTransactionPipeline`이 자동 발행하도록 구성되었는가? (`UseAll()` 또는 `UseTransaction()` 등록 확인)

### 이벤트 핸들러

- [ ] Event Handler 이름이 `On{EventName}` 패턴을 따르는가?
- [ ] Event Handler가 Usecases 폴더에 Command/Query와 함께 배치되어 있는가?
- [ ] `IDomainEventHandler<T>`를 구현하는가?
- [ ] `RegisterDomainEventHandlersFromAssembly`로 핸들러가 등록되어 있는가?

---

## 향후 고급 패턴

서비스 성숙도가 높아질 때 필요한 고급 패턴입니다. 현재는 미구현이며, 필요 시 단계적으로 도입합니다.

- **Outbox 패턴**: DB 트랜잭션과 이벤트 발행의 원자성 보장
- **Event Versioning**: 이벤트 스키마 변경 시 하위 호환 전략
- **Saga / Process Manager**: 다중 Aggregate 간 장기 트랜잭션 조율
- **이벤트 재처리 전략**: 멱등성(Idempotency) 보장 패턴

---

## 트러블슈팅

### 도메인 이벤트가 핸들러에서 수신되지 않는다

**원인:** Event Handler가 DI 컨테이너에 등록되지 않았을 수 있습니다. `Mediator.SourceGenerator`는 해당 패키지가 참조된 프로젝트 내의 핸들러만 자동 등록합니다.

**해결:** Application 레이어 등 다른 어셈블리의 핸들러는 `RegisterDomainEventHandlersFromAssembly`로 명시적으로 등록하세요:
```csharp
services.RegisterDomainEventHandlersFromAssembly(
    YourApp.Application.AssemblyReference.Assembly);
```

### SaveChanges 성공 후 이벤트 발행이 실패한다

**원인:** `UsecaseTransactionPipeline`은 SaveChanges 성공 후 이벤트를 발행합니다. 핸들러에서 예외가 발생하면 이벤트 발행은 실패하지만 데이터는 이미 커밋된 상태입니다 (eventual consistency).

**해결:** 강한 일관성이 필요하면 Outbox 패턴을 도입하세요. 현재 구조에서는 핸들러 내부에서 예외를 적절히 처리하고, 경고 로그를 기록하는 것이 권장됩니다.

### 테스트에서 이전 이벤트가 Assert를 방해한다

**원인:** `Create()`에서 발행된 이벤트가 `DomainEvents` 컬렉션에 남아있어 후속 행위 검증을 방해합니다.

**해결:** 테스트의 Arrange 단계에서 `entity.ClearDomainEvents()`를 호출하여 이전 이벤트를 정리한 후 Act를 수행하세요:
```csharp
var order = Order.Create(...);
order.ClearDomainEvents();  // 생성 이벤트 제거
order.Confirm();            // 이후 행위 테스트
```

---

## FAQ

### Q1. 도메인 이벤트를 독립 클래스가 아닌 중첩 클래스로 정의하는 이유는?

중첩 클래스로 정의하면 `Product.CreatedEvent`처럼 타입 시스템에서 이벤트 소유권이 명확해집니다. IntelliSense에서 `Product.`만 입력하면 관련 이벤트가 모두 표시되고, Event Handler 선언만으로 발행 주체를 즉시 파악할 수 있습니다.

### Q2. CorrelationId와 OpenTelemetry TraceId의 차이는?

`CorrelationId`는 비즈니스 수준 식별자로 동일 요청에서 발생한 이벤트를 그룹핑합니다. `TraceId`는 인프라 수준 식별자로 분산 시스템 간 요청을 추적합니다. 두 식별자는 독립적이지만 보완적으로 사용됩니다.

### Q3. Event Handler에서 다른 Aggregate를 변경해도 되나요?

가능하지만, Event Handler에서 직접 다른 Aggregate를 변경하면 트랜잭션 경계가 모호해집니다. 다른 Aggregate의 변경이 필요하면 해당 Aggregate의 Command를 발행하거나, 별도 Usecase를 호출하는 방식을 권장합니다.

### Q4. Usecase에서 IUnitOfWork나 IDomainEventPublisher를 직접 주입해야 하나요?

아닙니다. `UsecaseTransactionPipeline`이 SaveChanges와 이벤트 발행을 자동으로 처리합니다. Usecase에서는 Repository만 주입하면 됩니다.

### Q5. 이벤트 핸들러의 실행 순서를 보장할 수 있나요?

Mediator의 기본 동작은 핸들러 실행 순서를 보장하지 않습니다. 순서가 중요한 경우 하나의 핸들러 내에서 순차적으로 처리하거나, Saga/Process Manager 패턴을 고려하세요.

---

## 참고 문서

- [06a-aggregate-design.md](./06a-aggregate-design.md) - Aggregate 설계, [06b-entity-aggregate-core.md](./06b-entity-aggregate-core.md) - Entity/Aggregate 핵심 패턴, [06c-entity-aggregate-advanced.md](./06c-entity-aggregate-advanced.md) - 고급 패턴
- [11-usecases-and-cqrs.md](./11-usecases-and-cqrs.md) - Use Case 구현
- [11-usecases-and-cqrs.md §트랜잭션과 이벤트 발행](./11-usecases-and-cqrs.md#트랜잭션과-이벤트-발행-usecasetransactionpipeline) - 파이프라인 자동 처리 패턴
- [13-adapters.md](./13-adapters.md) - UoW Adapter 구현
