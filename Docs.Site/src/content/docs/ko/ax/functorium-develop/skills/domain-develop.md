---
title: "Domain Develop"
description: "도메인 모델 설계와 구현 (VO, Aggregate, Event, Spec, Service)"
---

> project-spec -> architecture-design -> **domain-develop** -> application-develop -> adapter-develop -> observability-develop -> test-develop

## 선행 조건

- `project-spec` 스킬에서 생성한 `00-project-spec.md`가 있으면 자동으로 읽어 Aggregate 후보와 비즈니스 규칙을 확인합니다.
- `architecture-design` 스킬에서 생성한 `01-architecture-design.md`가 있으면 읽어 폴더 구조와 네이밍 규칙을 확인합니다.
- 선행 문서가 없으면 사용자에게 직접 질문합니다.

## 배경

DDD 전술적 설계에 따라 도메인 코드를 작성할 때, 반복되는 패턴이 많습니다. Value Object의 `Create`/`Validate`/`CreateFromValidated` 삼중 팩토리, Aggregate Root의 이벤트 발행, 커맨드 메서드의 `Fin<Unit>` 반환 패턴 등은 빌딩블록마다 동일한 구조를 따릅니다.

`/domain-develop` 스킬은 이 반복을 자동화합니다. 자연어로 도메인 요구사항을 전달하면, Functorium 프레임워크 패턴에 맞는 코드, 단위 테스트, 문서를 5단계로 생성합니다.

## 스킬 개요

### 5단계 프로세스

| 단계 | 작업 | 산출물 |
|------|------|--------|
| 1 | 요구사항 분석 | 도메인 모델 분석표, 폴더 구조 |
| 2 | 코드 생성 | VO, Entity, Aggregate, Event, Error, Spec, Service |
| 3 | 단위 테스트 생성 | T1_T2_T3 명명 규칙, Shouldly 검증 |
| 4 | 빌드/테스트 검증 | `dotnet build` + `dotnet test` 통과 |
| 5 | 문서 생성 (선택) | 마크다운 설계 문서 |

### 지원하는 빌딩블록

| 빌딩블록 | 기반 클래스 | 설명 |
|----------|-----------|------|
| Simple Value Object | `SimpleValueObject<T>` | 단일 원시 값 래핑 |
| Composite Value Object | `ValueObject` | 여러 VO 조합 |
| Union Value Object | `UnionValueObject` / `UnionValueObject<TSelf>` | 허용 상태 조합, 상태 전이 |
| Entity | `Entity<TId>` | Aggregate 내 자식 엔티티 |
| Aggregate Root | `AggregateRoot<TId>` | 트랜잭션 경계 |
| Domain Event | `DomainEvent` | 상태 변경 알림 |
| Domain Error | `DomainErrorKind.Custom` | 비즈니스 규칙 위반 |
| Specification | `ExpressionSpecification<T>` | 조회/검색 조건 |
| Domain Service | `IDomainService` | 교차 Aggregate 순수 로직 |
| Repository | `IRepository<T, TId>` | 영속화 인터페이스 |

### 핵심 API 패턴

| 패턴 | 사용법 |
|------|--------|
| `Fin<T>` 성공/실패 | `result.IsSucc`, `result.IsFail` |
| 값 추출 | `result.ThrowIfFail()` |
| 성공 반환 | `unit` (`using static LanguageExt.Prelude;`) |
| 실패 반환 | `DomainError.For<T>(new ErrorRecord(), id, message)` |
| EntityId 생성 | `{Type}Id.New()` (Ulid 기반) |
| 검증 병렬 합성 | `(Fin1, Fin2).Apply((v1, v2) => ...)` |
| 상태 전이 | `TransitionFrom<TFrom, TTo>(mapper)` |

### Entity 컬렉션 관리

Aggregate 내부에 Child Entity 컬렉션을 관리할 때, `IReadOnlyList<T>` 공개 + `List<T>` 내부 패턴을 사용합니다.

```csharp
private readonly List<OrderItem> _items = new();
public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

public Fin<Unit> AddItem(OrderItem item)
{
    _items.Add(item);
    AddDomainEvent(new ItemAddedEvent(Id, item.Id));
    return unit;
}

public Fin<Unit> RemoveItem(OrderItemId itemId)
{
    var item = _items.FirstOrDefault(i => i.Id == itemId);
    if (item is null) return unit; // 멱등성: 없는 항목 제거는 성공
    _items.Remove(item);
    AddDomainEvent(new ItemRemovedEvent(Id, itemId));
    return unit;
}
```

### CtxEnricher 어트리뷰트

Application 레이어의 Request/Response DTO에 관측성 전파 어트리뷰트를 적용합니다. 이 어트리뷰트는 `observability-develop` 스킬에서 설계한 ctx.* 전파 전략을 코드에 반영합니다.

| 어트리뷰트 | 용도 | 예시 |
|-----------|------|------|
| `[CtxRoot]` | ctx.{field} 루트 레벨 승격 | `[CtxRoot] string CustomerId` |
| `[CtxTarget(CtxPillar.All)]` | 특정 Pillar에 전파 | `[CtxTarget(CtxPillar.All)] string CustomerTier` |
| `[CtxIgnore]` | 모든 Pillar에서 제외 | `[CtxIgnore] string InternalMemo` |

## 사용 방법

### 기본 호출

```text
/domain-develop Product aggregate with ProductName (max 100 chars), ProductPrice (positive decimal)
```

### 대화형 모드

인자 없이 `/domain-develop`만 호출하면, 스킬이 대화형으로 요구사항을 수집합니다.

### 실행 흐름

1. **분석 결과 제시** — Aggregate, Value Object, Event, Error 등 식별 결과를 표로 보여줍니다
2. **사용자 확인** — 분석 결과를 확인한 후 코드 생성으로 진행합니다
3. **코드 + 테스트 생성** — 빌딩블록별로 코드와 테스트를 생성합니다
4. **빌드/테스트 검증** — `dotnet build`와 `dotnet test`를 실행하여 통과를 확인합니다

## 예제 1: 기초 — Aggregate와 Value Object

가장 기본적인 Aggregate 패턴입니다. 단일 원시 값을 감싸는 Simple Value Object 두 개와, 이를 속성으로 가지는 Aggregate Root를 생성합니다. Functorium 도메인 개발의 출발점이 되는 구조입니다.

### 프롬프트

```text
/domain-develop Product aggregate with ProductName (max 100 chars), ProductPrice (positive decimal).
Create, UpdateName, UpdatePrice 커맨드 메서드
```

### 기대 결과

| 빌딩블록 | 타입 | 설명 |
|----------|------|------|
| Simple VO | `ProductName` | `SimpleValueObject<string>`, 100자 제한 |
| Simple VO | `ProductPrice` | `SimpleValueObject<decimal>`, 양수 검증 |
| Aggregate | `Product` | `AggregateRoot<ProductId>`, 3개 커맨드 메서드 |
| Domain Event | `CreatedEvent`, `NameUpdatedEvent`, `PriceUpdatedEvent` | 상태 변경 알림 |
| 단위 테스트 | 약 36개 | VO 검증 + Aggregate 커맨드 + FinApply |

### 핵심 스니펫

**Simple Value Object** — 단일 원시 값 래핑, `Create`/`Validate`/`CreateFromValidated` 삼중 팩토리:

```csharp
public sealed class ProductName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private ProductName(string value) : base(value) { }

    public static Fin<ProductName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ProductName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ProductName>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.Trim());
}
```

**커맨드 메서드** — `Fin<Unit>` 반환, 상태 변경 + 이벤트 발행:

```csharp
public Fin<Unit> UpdateName(ProductName newName, DateTime now)
{
    Name = newName;
    UpdatedAt = now;
    AddDomainEvent(new NameUpdatedEvent(Id, newName));
    return unit;
}
```

## 예제 2: 중급 — Union 타입과 Child Entity

예제 1에 세 가지 패턴을 추가합니다. 상태 전이를 타입으로 인코딩하는 Union Value Object, 여러 VO를 조합하는 Composite Value Object, Aggregate 내부에 소속되는 Child Entity입니다. 이 세 패턴이 결합되면, "Pending 주문만 확인 가능"이나 "배송 주소는 4개 필드가 모두 유효해야 생성 가능" 같은 규칙을 컴파일 타임에 보장할 수 있습니다.

### 프롬프트

```text
/domain-develop Order aggregate with OrderStatus union
(Pending → Confirmed → Shipped → Cancelled 상태 전이),
ShippingAddress composite VO (Street, City, State, ZipCode),
OrderItem child entity (ProductName, Quantity, UnitPrice).
Confirm과 Ship 커맨드 메서드
```

### 기대 결과

| 빌딩블록 | 타입 | 설명 |
|----------|------|------|
| Union VO | `OrderStatus` | `UnionValueObject<OrderStatus>`, 4개 상태, 전이 메서드 |
| Composite VO | `ShippingAddress` | `ValueObject`, 4개 하위 VO 조합 |
| Child Entity | `OrderItem` | `Entity<OrderItemId>`, 부모 Aggregate 통해 관리 |
| Aggregate | `Order` | `AggregateRoot<OrderId>`, 상태 전이 기반 커맨드 |

### 핵심 스니펫

**Union Value Object 상태 전이** — `TransitionFrom`으로 허용된 전이만 성공, 나머지는 `InvalidTransition` 에러 반환:

```csharp
[UnionType]
public abstract partial record OrderStatus : UnionValueObject<OrderStatus>
{
    public sealed record Pending(DateTime CreatedAt) : OrderStatus;
    public sealed record Confirmed(DateTime ConfirmedAt) : OrderStatus;
    public sealed record Shipped(DateTime ShippedAt) : OrderStatus;
    public sealed record Cancelled(DateTime CancelledAt) : OrderStatus;

    private OrderStatus() { }

    public Fin<Confirmed> Confirm(DateTime now) =>
        TransitionFrom<Pending, Confirmed>(
            _ => new Confirmed(now));

    public Fin<Shipped> Ship(DateTime now) =>
        TransitionFrom<Confirmed, Shipped>(
            _ => new Shipped(now));
}
```

**Composite Value Object** — 하위 VO의 `Validate`를 `.Apply()`로 병렬 합성하여 에러를 누적 수집:

```csharp
public sealed class ShippingAddress : ValueObject
{
    public Street Street { get; }
    public City City { get; }
    public State State { get; }
    public ZipCode ZipCode { get; }

    private ShippingAddress(Street street, City city, State state, ZipCode zipCode)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
    }

    public static Validation<Error, (string, string, string, string)> Validate(
        string? street, string? city, string? state, string? zipCode) =>
        (Street.Validate(street), City.Validate(city),
         State.Validate(state), ZipCode.Validate(zipCode))
            .Apply((s, c, st, z) => (s, c, st, z));

    public static Fin<ShippingAddress> Create(
        string? street, string? city, string? state, string? zipCode) =>
        CreateFromValidation<ShippingAddress, (string, string, string, string)>(
            Validate(street, city, state, zipCode),
            v => new ShippingAddress(
                Street.CreateFromValidated(v.Item1),
                City.CreateFromValidated(v.Item2),
                State.CreateFromValidated(v.Item3),
                ZipCode.CreateFromValidated(v.Item4)));
}
```

**Child Entity** — `Entity<TId>` 상속, 이벤트 발행 없이 부모 Aggregate를 통해 관리:

```csharp
[GenerateEntityId]
public sealed class OrderItem : Entity<OrderItemId>
{
    public ProductName ProductName { get; }
    public Quantity Quantity { get; private set; }
    public UnitPrice UnitPrice { get; }

    private OrderItem(OrderItemId id, ProductName name, Quantity qty, UnitPrice price)
        : base(id)
    {
        ProductName = name;
        Quantity = qty;
        UnitPrice = price;
    }

    public static OrderItem Create(ProductName name, Quantity qty, UnitPrice price) =>
        new(OrderItemId.New(), name, qty, price);
}
```

## 예제 3: 고급 — Domain Service와 Specification

Aggregate 하나로 해결할 수 없는 로직이 등장합니다. Specification은 조회 조건을 객체로 캡슐화하고, Domain Service는 여러 Aggregate에 걸친 순수 로직을 담습니다. 이 두 패턴은 Aggregate의 경계를 존중하면서 교차 관심사를 처리하는 방법입니다.

### 프롬프트

```text
/domain-develop Inventory aggregate with StockQuantity VO (non-negative int),
LowStockThreshold VO (positive int).
InventoryLowStockSpec specification (재고가 임계값 이하),
InventoryTransferService domain service (source → target 재고 이동).
Restock, Transfer 커맨드 메서드
```

### 기대 결과

| 빌딩블록 | 타입 | 설명 |
|----------|------|------|
| Simple VO | `StockQuantity` | `SimpleValueObject<int>`, 0 이상 |
| Simple VO | `LowStockThreshold` | `SimpleValueObject<int>`, 양수 |
| Specification | `InventoryLowStockSpec` | `ExpressionSpecification<Inventory>`, 재고 ≤ 임계값 |
| Domain Service | `InventoryTransferService` | `IDomainService`, 재고 부족 시 실패 반환 |
| Aggregate | `Inventory` | `AggregateRoot<InventoryId>`, Restock/Deduct 커맨드 |

### 핵심 스니펫

**Specification** — VO를 primitive로 변환하여 Expression 클로저에 캡처, EF Core 쿼리 변환 가능:

```csharp
public sealed class InventoryLowStockSpec : ExpressionSpecification<Inventory>
{
    public LowStockThreshold Threshold { get; }

    public InventoryLowStockSpec(LowStockThreshold threshold) =>
        Threshold = threshold;

    public override Expression<Func<Inventory, bool>> ToExpression()
    {
        int thresholdValue = Threshold;
        return inventory => inventory.StockQuantity <= thresholdValue;
    }
}
```

**Domain Service** — 상태 없는 순수 함수, 교차 Aggregate 로직을 `Fin<Unit>`으로 표현:

```csharp
public sealed class InventoryTransferService : IDomainService
{
    public sealed record InsufficientStock : DomainErrorKind.Custom;

    public Fin<Unit> Transfer(
        Inventory source, Inventory target, StockQuantity amount, DateTime now)
    {
        if (source.StockQuantity < amount)
            return DomainError.For<InventoryTransferService>(
                new InsufficientStock(),
                source.Id.ToString(),
                $"재고 부족: 현재 {(int)source.StockQuantity}, 요청 {(int)amount}");

        source.Deduct(amount, now);
        target.Restock(amount, now);
        return unit;
    }
}
```

## 예제 4: 실전 — 연락처 도메인 전체 구현

앞의 세 예제에서 다룬 모든 패턴이 하나의 도메인에 결합됩니다. [타입으로 도메인 설계하기](../samples/designing-with-types/) 예제의 비즈니스 요구사항을 그대로 프롬프트로 전달하여, 전체 도메인 모델을 자동 생성합니다. 9개 Value Object, 2개 Union, 1개 Child Entity, 1개 Aggregate, 1개 Domain Service, 1개 Specification — 그리고 114개 단위 테스트까지 한 번의 스킬 호출로 만들어냅니다.

### 프롬프트

```text
/domain-develop Contact aggregate 연락처 관리 도메인을 구현해줘.

## 연락처 정보 구성
- 개인 이름: 이름(First Name, 필수), 성(Last Name, 필수), 중간 이니셜(Middle Initial, 선택)
- 이메일 주소: 표준 이메일 형식
- 우편 주소: 주소(Address), 도시(City), 주 코드(State, 2자리 대문자), 우편번호(Zip, 5자리 숫자)
- 메모: 자유 형식 텍스트, 500자 이하

## 비즈니스 규칙
1. 데이터 유효성: 이름/성 50자 이하, 이메일 표준 형식, 주 코드 2자리 대문자, 우편번호 5자리 숫자
2. 연락 수단: 최소 하나 필수 (이메일만 / 우편 주소만 / 둘 다). 연락 수단 없는 연락처는 존재 불가
3. 이메일 인증: 미인증 → 인증 단방향 전이. 인증 시점 기록. 이미 인증된 이메일 재인증 불가
4. 연락처 수명: 이름 변경, 논리 삭제(삭제자+시점), 복원 가능. 삭제된 연락처 수정 불가. 삭제/복원 멱등
5. 메모 관리: 추가/제거 가능, 삭제된 연락처 불가, 존재하지 않는 메모 제거 멱등
6. 이메일 고유성: 중복 불가, 자기 자신 제외

## 존재불가 상태
- 이메일 없는데 인증된 상태
- 연락 수단 없는 연락처
- 인증 → 미인증 역전
- 삭제된 연락처에서 행위 수행
- 동일 이메일 중복 연락처
```

### 기대 결과

이 프롬프트는 [타입으로 도메인 설계하기](../samples/designing-with-types/) 예제와 동일한 도메인 모델을 생성합니다:

| 빌딩블록 | 타입 | 설명 |
|----------|------|------|
| Simple VO (9개) | `FirstName`, `LastName`, `MiddleInitial`, `EmailAddress`, `Street`, `City`, `StateCode`, `ZipCode`, `NoteContent` | 원시 값 검증 |
| Composite VO (2개) | `PersonalName`, `PostalAddress` | VO 조합 |
| Union VO (2개) | `ContactInfo` (이메일만/우편주소만/둘다), `EmailStatus` (미인증/인증) | 허용 상태 조합, 단방향 전이 |
| Child Entity | `ContactNote` | 메모 관리 |
| Aggregate | `Contact` | Create, UpdateName, VerifyEmail, SoftDelete, Restore, AddNote, RemoveNote |
| Domain Service | `ContactEmailUniquenessService` | 이메일 고유성 검증 |
| Specification | `ContactByEmailSpec` | 이메일 기반 조회 |
| 단위 테스트 | 약 114개 | 비즈니스 규칙 6개 그룹 + 시나리오 10개 전체 검증 |

### 왜 이 예제가 중요한가

이 예제는 단순한 CRUD가 아닙니다. "연락 수단 없는 연락처는 존재할 수 없다"는 규칙은 Union 타입으로, "인증은 단방향"이라는 규칙은 상태 전이로, "삭제된 연락처 수정 불가"는 Aggregate 가드 조건으로 각각 타입 시스템에 인코딩됩니다. 잘못된 상태가 컴파일 타임에 차단되는 것을 확인하려면, [타입으로 도메인 설계하기](../samples/designing-with-types/) 예제의 전체 설계 과정을 참조하십시오.

## 참고 자료

### 워크플로

- [워크플로](../workflow/) -- 7단계 전체 흐름
- [Project Spec 스킬](../project-spec/) -- 이전 단계: PRD 작성
- [Architecture Design 스킬](../architecture-design/) -- 이전 단계: 프로젝트 구조 설계
- [Application Develop 스킬](../application-develop/) -- 다음 단계: 유스케이스 구현

### 프레임워크 가이드

- [DDD 전술적 설계 개요](../guides/domain/04-ddd-tactical-overview/)
- [값 객체](../guides/domain/05a-value-objects/)
- [값 객체 검증](../guides/domain/05b-value-objects-validation/)
- [Union 값 객체](../guides/domain/05c-union-value-objects/)
- [Aggregate 설계](../guides/domain/06a-aggregate-design/)
- [Entity/Aggregate 핵심](../guides/domain/06b-entity-aggregate-core/)
- [도메인 이벤트](../guides/domain/07-domain-events/)
- [에러 시스템](../guides/domain/08a-error-system/)
- [Specification](../guides/domain/10-specifications/)
- [도메인 서비스](../guides/domain/09-domain-services/)
- [단위 테스트](../guides/testing/15a-unit-testing/)

### 실전 예제

- [타입으로 도메인 설계하기](../samples/designing-with-types/) -- 연락처 도메인 전체 설계/구현 과정
