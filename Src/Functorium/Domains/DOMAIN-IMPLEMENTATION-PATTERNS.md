# 도메인 구현 패턴 (Domain Implementation Patterns)

## 도메인 모델 설계 지침

요구사항으로부터 도메인 객체를 식별하고, Aggregate 경계를 설정하며, 도메인 로직을 올바른 위치에 배치하기 위한 의사결정 가이드.

### 도메인 객체 식별 — 요구사항에서 모델로

요구사항의 명사/동사로부터 Entity, Value Object, Aggregate Root를 구분하는 의사결정 트리:

```
고유 식별자가 필요한가?
├── 아니오 → Value Object (Money, Email, Quantity...)
└── 예 → Entity
         독립적으로 저장/조회되는가?
         ├── 예 → Aggregate Root (Customer, Product, Order...)
         └── 아니오 → 자식 Entity (Tag, OrderItem...)
```

**판별 기준 표**:

| 기준 | Value Object | 자식 Entity | Aggregate Root |
|------|-------------|------------|---------------|
| 고유 식별자 | 없음 | 있음 | 있음 |
| 동등성 | 값 기반 | ID 기반 | ID 기반 |
| 가변성 | 불변 | 가변 | 가변 |
| 독립 조회 | 불가 | 불가 (Root 통해) | 가능 |
| Repository | 없음 | 없음 | 있음 |
| 도메인 이벤트 | 발행 불가 | 발행 불가 | 발행 가능 |
| 생명주기 | 소유 Entity에 종속 | Root에 종속 | 독립적 |

**01-SingleHost 적용 사례**:

| 개념 | 분류 | 근거 |
|------|------|------|
| Customer | AR | 독립 생명주기, ID로 조회, Repository 소유 |
| Product | AR | 독립 생명주기, Tag(자식 Entity) 관리, Repository 소유 |
| Order | AR | 독립 생명주기, Cross-Aggregate 참조(ProductId), Repository 소유 |
| Inventory | AR | 독립 생명주기, IConcurrencyAware 동시성 제어, Repository 소유 |
| Tag | 공유 Entity | 자체 ID 보유, 여러 Product에서 공유, ITagRepository 소유 |
| Money | VO | 식별자 없음, 값으로 비교, 불변, 행위 메서드(Add/Subtract/Multiply) |
| Email | VO | 식별자 없음, 값으로 비교, 불변, 자기 검증(정규식) |
| Quantity | VO | 식별자 없음, 값으로 비교, 불변, 행위 메서드(Add/Subtract) |
| ShippingAddress | VO | 식별자 없음, 값으로 비교, 불변 |

> **참조**: `Docs/guides/06-entities-and-aggregates.md` §3

### Aggregate 경계 설계

Eric Evans + Vaughn Vernon의 Aggregate 설계 4원칙:

1. **불변식을 경계 내에서 보호하라** — 트랜잭션 경계 = Aggregate 경계
2. **작은 Aggregate를 설계하라** — 불변식 보호에 필요한 최소한의 데이터만 포함
3. **다른 Aggregate는 ID로만 참조하라** — 독립성, 느슨한 결합
4. **경계 밖은 최종 일관성을 사용하라** — 도메인 이벤트

**대/소 Aggregate 트레이드오프**:

| 관점 | 큰 Aggregate | 작은 Aggregate |
|------|-------------|---------------|
| 동시성 | 충돌 빈번 | 충돌 최소화 |
| 성능 | 전체 로드 필요 | 필요한 것만 로드 |
| 메모리 | 사용량 높음 | 사용량 낮음 |
| 트랜잭션 | 범위 넓음 | 범위 좁음 |

**Aggregate 경계 의사결정 트리**:

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

**분할 신호** — 다음 중 하나라도 해당하면 분할 검토:

| 신호 | 증상 | 예시 |
|------|------|------|
| 동시성 충돌 빈번 | `DbUpdateConcurrencyException` 반복 | 주문마다 Product 전체 락 |
| 변경 빈도 불균형 | 일부 속성만 고빈도 변경 | 카탈로그(저빈도) vs 재고(고빈도) |
| 불변식 독립성 | 속성 그룹 간 상호 의존 불변식 없음 | 가격 변경이 재고 규칙에 영향 없음 |

**적용 사례: Product → Product + Inventory 분할**

Before — 단일 Product Aggregate:
```
Product Aggregate
  ProductName, Description, Price  ← 저빈도 변경 (관리자)
  StockQuantity                    ← 고빈도 변경 (주문마다)
  → 주문 처리 시 Product 전체에 동시성 충돌 발생
```

After — 분할:
```
Product Aggregate (카탈로그)        Inventory Aggregate (재고)
  ProductName, Description, Price    ProductId (ID 참조)
  Tags (자식 Entity)                 StockQuantity
                                     RowVersion (IConcurrencyAware)
```

분할 근거:
- 카탈로그와 재고는 **불변식 독립** — 가격 변경이 재고 규칙에 영향 없음
- 재고는 주문마다 변경(고빈도), 카탈로그는 관리자만 변경(저빈도) — **변경 빈도 불균형**
- 분리 후 Inventory에만 `IConcurrencyAware`(RowVersion) 적용 — 재고 충돌만 감지

> **참조**: `Docs/guides/06-entities-and-aggregates.md` §1, §2, §4

### Rich Domain Model vs 빈혈 모델 (Anemic Model)

**빈혈 모델 감별 기준**:

| 특성 | 빈혈 모델 | Rich Domain Model |
|------|----------|-------------------|
| 속성 접근자 | public setter | private set |
| 행위 | getter/setter만 | 비즈니스 메서드 (Confirm, DeductStock) |
| 불변식 | 외부 서비스에서 검증 | Aggregate 내부에서 캡슐화 |
| 이벤트 | 없음 | 상태 변경 시 도메인 이벤트 발행 |

**01-SingleHost 적용 사례**:

| Aggregate | 행위 | 판단 |
|-----------|------|------|
| Order | Create + Confirm/Ship/Cancel/Deliver + 상태 전이 가드 | Rich Model |
| Inventory | Create + DeductStock/AddStock + 재고 부족 가드 | Rich Model |
| Product | Create + Update + AssignTag/UnassignTag | Rich Model |
| Customer | Create만 | 이 BC에서 참조 데이터 역할 — 행위 추가 불필요 (오버엔지니어링 주의) |

**핵심**: 모든 Aggregate가 반드시 풍부한 행위를 가져야 하는 것은 아니다. Bounded Context 내 역할에 따라 판단한다.

### 불변 조건 배치 전략 — 단일 vs 교차 Aggregate

의사결정 기준: **"이 규칙을 검증하려면 다른 Aggregate의 데이터가 필요한가?"**

```
다른 Aggregate 데이터가 필요한가?
├── NO → Aggregate 메서드 내부 (단일 Aggregate 불변 조건)
└── YES
    ├── 외부 I/O 없이 검증 가능한가?
    │   ├── YES → Domain Service
    │   └── NO → Usecase에서 조율
    └── 상태 변경이 여러 Aggregate에 걸치는가?
        ├── YES → Domain Event + 최종 일관성
        └── NO → Domain Service
```

**01-SingleHost 적용 사례**:

| 불변 조건 | 유형 | 위치 | 검증 대상 |
|-----------|------|------|-----------|
| 재고 차감 시 보유수량 ≥ 차감수량 | 단일 | `Inventory.DeductStock()` | 자기 자신의 `StockQuantity` |
| 주문 상태 전이 규칙 | 단일 | `Order.Confirm/Ship/Cancel/Deliver()` | 자기 자신의 `Status` |
| 주문 총액 ≤ 고객 신용한도 | 교차 | `OrderCreditCheckService` | Customer.CreditLimit + Order.TotalAmount |

공통 패턴: 두 유형 모두 `Fin<Unit>` 반환 (성공: `unit`, 실패: `DomainError`)

> **참조**: `Docs/guides/09-domain-services.md` §1

### 도메인 로직 배치 판단

```
로직이 단일 Aggregate에 속하는가?
├── YES → Entity 메서드 또는 Value Object
└── NO
    ├── 외부 I/O가 필요한가?
    │   ├── YES → Usecase에서 조율
    │   └── NO → Domain Service
    └── 여러 Aggregate의 상태를 변경하는가?
        ├── YES → Domain Event + 별도 Handler
        └── NO → Domain Service
```

| 배치 위치 | 기준 | 예시 |
|----------|------|------|
| **Entity 메서드** | 단일 Aggregate 내부 상태 변경 | `Inventory.DeductStock()` |
| **Value Object** | 값의 검증, 변환, 연산 | `Money.Add()` |
| **Domain Service** | 여러 Aggregate 참조, 순수 로직 | `OrderCreditCheckService.ValidateCreditLimit()` |
| **Usecase** | 조율, I/O 위임 | Repository 호출, Event 발행 |

**안티패턴 3종**:

| 안티패턴 | 증상 | 해결 |
|---------|------|------|
| Fat Usecase | 비즈니스 로직이 Usecase에 집중 | Entity 메서드 또는 Domain Service로 이동 |
| Anemic Domain Model | Entity가 getter/setter만 보유 | Entity에 비즈니스 메서드 추가 |
| Domain Service 남용 | 모든 로직을 Domain Service에 배치 | 단일 Aggregate 로직은 Entity 메서드로 |

> **참조**: `Docs/guides/09-domain-services.md` §1

---

## 엔티티 ID 체계

- 모든 엔티티/애그리거트에 `[GenerateEntityId]` 소스 생성기 적용
- **생성 타입**: `{Entity}Id` — `readonly partial record struct`, `IEntityId<T>` 구현
- **기반**: `Ulid` (시간순 정렬 가능한 128비트 식별자)
- **주요 멤버**:
  - `{Type}Id.New()` — 신규 ID 생성
  - `{Type}Id.Create(Ulid)` / `{Type}Id.Create(string)` — 기존 값으로 복원
  - `{Type}Id.Empty` — 빈 ID (Ulid.Empty)
  - 비교 연산자 (`<`, `>`, `<=`, `>=`), `IParsable<T>` 구현
- **영속성**: `ValueConverter<{Type}Id, string>` — DB에 문자열로 저장
- **적용 대상**: `ProductId`, `CustomerId`, `OrderId`, `InventoryId`, `TagId`

## 값 객체 구현 패턴

**기반 클래스 구분**:

| 기반 클래스 | 용도 | 적용 대상 |
|------------|------|-----------|
| `SimpleValueObject<T>` | 동등비교만 (`==`, `!=`) | 모든 string VO — CustomerName, Email, ProductName, ProductDescription, ShippingAddress, TagName |
| `ComparableSimpleValueObject<T>` | 비교연산자 포함 (`<`, `>`, `<=`, `>=`) | Money (`decimal`), Quantity (`int`) |

**팩토리 3종**:

| 메서드 | 반환 타입 | 용도 |
|--------|-----------|------|
| `Create(raw)` | `Fin<T>` | 외부 입력 검증 + 생성 (애플리케이션 계층) |
| `Validate(raw)` | `Validation<Error, T>` | 검증만 수행 (병렬 검증 수집용) |
| `CreateFromValidated(value)` | `T` (직접 반환) | 검증 우회 — ORM 복원 전용 |

**검증 체인** (`ValidationRules<TValueObject>`):

```
NotNull(value)
  .ThenNotEmpty()          // 빈 문자열 불가 (ProductDescription 제외)
  .ThenMaxLength(n)        // 최대 길이
  .ThenMatches(regex, msg) // 정규식 (Email만)
  .ThenNormalize(v => ...) // 정규화 (검증 아닌 변환)
```

**정규화 규칙**:
- 모든 string VO: `Trim()` 적용
- Email만 추가: `ToLowerInvariant()` — `v => v.Trim().ToLowerInvariant()`

**암묵 변환**: 모든 VO에 `implicit operator` — 기저 타입으로 자동 변환 (예: `Money → decimal`, `Email → string`)

**행위 메서드** (ComparableSimpleValueObject만 해당):
- `Money`: `Add(Money)`, `Subtract(Money)`, `Multiply(decimal)` — 새 인스턴스 반환
- `Quantity`: `Add(int)`, `Subtract(int)` — 새 인스턴스 반환, 음수 방지(`Math.Max(0, ...)`)

## 열거형 구현 패턴 (SmartEnum)

**기반 클래스 선택 기준**:

| 선택지 | 기반 클래스 | 용도 |
|--------|-----------|------|
| 고정된 선택지 + 값마다 고유 속성/동작 | `SmartEnum<T, TValue>` + `IValueObject` | Currency 등 |
| 고정된 선택지 + 상태 전이 로직 | `SimpleValueObject<string>` (Smart Enum 패턴) | OrderStatus 등 |
| 단순 플래그/상태 | C# `enum` | 충분한 경우 |

**SmartEnum 구현 패턴** (`SmartEnum<T, TValue>` + `IValueObject`):
- `IValueObject` 마커 구현 필수
- Create 패턴: `Validate(value).Map(FromValue).ToFin()`
- `CreateFromValidated`: `FromValue(value)` 직접 호출

**SimpleValueObject 기반 Smart Enum 패턴** (01-SingleHost `OrderStatus`):
- `SimpleValueObject<string>` 상속
- `static readonly` 인스턴스 + `HashMap<string, T>` 전체 목록
- 표준 팩토리 3종 (다른 VO와 동일 패턴):
  - `Create(string)` → `Fin<T>` — `Validate().ToFin()`
  - `Validate(string)` → `Validation<Error, T>` — HashMap 조회 기반 검증
  - `CreateFromValidated(string)` → `T` — HashMap 조회 (실패 시 throw, DB 복원용)
- 상태 전이: `HashMap<string, Seq<string>>` + `CanTransitionTo()` 메서드

**적용 대상**:

| 타입 | 기반 | 위치 |
|------|------|------|
| `OrderStatus` | `SimpleValueObject<string>` | 01-SingleHost Domain |
| `SortDirection` | `SmartEnum<SortDirection, string>` | Functorium Application |
| `OtlpCollectorProtocol` | `SmartEnum<OtlpCollectorProtocol>` | Functorium Adapter |

> **참조**: `Docs/guides/05-value-objects.md` §열거형 구현 패턴

## 애그리거트 구현 패턴

**기반 클래스**:
- `AggregateRoot<TId>` → `Entity<TId>` → `IEntity<TId>` (ID 기반 동등비교)
- `AggregateRoot<TId>`는 `IDomainEventDrain` 구현 — `AddDomainEvent()`, `ClearDomainEvents()`

**공통 인터페이스**:

| 인터페이스 | 속성 | 적용 대상 |
|-----------|------|-----------|
| `IAuditable` | `DateTime CreatedAt`, `Option<DateTime> UpdatedAt` | 모든 애그리거트 루트 (Customer, Product, Order, Inventory) |
| `IConcurrencyAware` | `byte[] RowVersion` | Inventory만 (높은 변경 빈도) |

**팩토리 이원화**:

| 메서드 | 용도 | 이벤트 발행 | 반환 |
|--------|------|------------|------|
| `Create(...)` | 신규 생성 (애플리케이션 계층) | O | `T` (직접 반환) |
| `CreateFromValidated(...)` | ORM 복원 (인프라 계층) | X | `T` (직접 반환) |

- `Create`는 이미 검증된 VO를 파라미터로 받음 (VO 자체의 `Create`에서 검증 완료)
- `CreateFromValidated`는 ID, VO, 감사 속성 등 전체 상태를 파라미터로 받아 복원

**캡슐화 규칙**:
- 모든 도메인 클래스(애그리거트, 엔티티, Specification)는 `sealed`
- 속성: `{ get; private set; }` — 행위 메서드를 통해서만 상태 변경
- 컬렉션: `private readonly List<T>` + `public IReadOnlyList<T>` 노출

## 리포지토리 인터페이스

**기본 계약** (`IRepository<TAggregate, TId>`):

| 메서드 | 시그니처 |
|--------|----------|
| Create | `FinT<IO, TAggregate>` |
| GetById | `FinT<IO, TAggregate>` |
| Update | `FinT<IO, TAggregate>` |
| Delete | `FinT<IO, Unit>` |

**AR별 확장 메서드**:

| 리포지토리 | 추가 메서드 |
|-----------|------------|
| `ICustomerRepository` | `Exists(Specification<Customer>)` → `FinT<IO, bool>` |
| `IProductRepository` | `Exists(Specification<Product>)` → `FinT<IO, bool>` |
| `IOrderRepository` | (없음) |
| `IInventoryRepository` | `GetByProductId(ProductId)` → `FinT<IO, Inventory>`, `Exists(Specification<Inventory>)` → `FinT<IO, bool>` |

## Specification 구현 패턴

- **기반**: `ExpressionSpecification<T>` → `Specification<T>` 상속
- **핵심 메서드**: `ToExpression()` → `Expression<Func<T, bool>>` — EF Core LINQ 변환용 Expression Tree
- **Null Object**: `Specification<T>.All` — 항상 `true` 반환 (AllSpecification)
- **조합 연산**: `And()`, `Or()`, `Not()` + 연산자 `&`, `|`, `!`
  - `&` 단축 평가: 한쪽이 `All`이면 다른 쪽 반환

**정의된 Specification 목록**:

| Specification | 대상 | 파라미터 | 조건 |
|--------------|------|----------|------|
| `CustomerEmailSpec` | Customer | `Email` | 이메일 정확 일치 |
| `ProductNameSpec` | Product | `ProductName` | 상품명 정확 일치 |
| `ProductNameUniqueSpec` | Product | `ProductName`, `Option<ProductId> excludeId` | 상품명 일치 + 자기 자신 제외 |
| `ProductPriceRangeSpec` | Product | `Money minPrice`, `Money maxPrice` | `minPrice ≤ Price ≤ maxPrice` |
| `InventoryLowStockSpec` | Inventory | `Quantity threshold` | `StockQuantity < threshold` |

## 함수형 타입 규칙

| 타입 | 용도 | 사용 위치 |
|------|------|-----------|
| `Fin<T>` | 동기 실패 가능 결과 | VO `Create()`, `DeductStock()`, `OrderCreditCheckService` |
| `Validation<Error, T>` | 병렬 검증 수집 (여러 오류 누적) | VO `Validate()` |
| `FinT<IO, T>` | 비동기 IO + 실패 가능 결과 | 리포지토리 인터페이스 전체 |
| `Option<T>` | null 대체 (값이 없을 수 있음) | `IAuditable.UpdatedAt`, `ProductNameUniqueSpec.ExcludeId` |
| `Seq<T>` | 불변 시퀀스 | `OrderCreditCheckService.ValidateCreditLimitWithExistingOrders(Seq<Order>)` |

## 도메인 에러 구현 패턴

### 에러 팩토리

`DomainError.For<T>(...)` 정적 메서드로 에러를 생성한다. 반환값은 `Error`이며, `Fin<T>`로 암시적 변환된다.

```csharp
// 기본 형태
DomainError.For<Email>(new Empty(), currentValue: "", message: "이메일은 비어있을 수 없습니다");

// 제네릭 값 타입
DomainError.For<Age, int>(new Negative(), currentValue: -5, message: "나이는 음수일 수 없습니다");
```

### 표준 에러 vs Custom 에러

**표준 에러 타입** (`DomainErrorType` 범주):

| 범주 | 대표 타입 |
|------|----------|
| Presence | `Empty`, `Null` |
| Length | `TooShort`, `TooLong`, `WrongLength` |
| Format | `InvalidFormat`, `NotUpperCase`, `NotLowerCase` |
| Numeric | `Zero`, `Negative`, `NotPositive`, `OutOfRange`, `BelowMinimum`, `AboveMaximum` |
| Existence | `NotFound`, `AlreadyExists`, `Duplicate`, `Mismatch` |

**Custom 에러** — 표준 타입으로 표현할 수 없는 도메인 특화 에러:

```csharp
public sealed record InsufficientStock : DomainErrorType.Custom;
```

### Custom 에러 정의 위치

Custom 에러는 해당 엔티티/VO 내부에 nested sealed record로 정의한다.

```csharp
public sealed class Inventory : AggregateRoot<InventoryId>
{
    public sealed record InsufficientStock : DomainErrorType.Custom;

    public Fin<Unit> DeductStock(Quantity quantity)
    {
        if ((int)quantity > (int)StockQuantity)
            return DomainError.For<Inventory, int>(
                new InsufficientStock(),
                currentValue: (int)StockQuantity,
                message: $"재고 부족. 현재: {(int)StockQuantity}, 요청: {(int)quantity}");
        // ...
    }
}
```

> **참조**: `Docs/guides/08-error-system.md` — 에러 네이밍 규칙(R1–R8), 레이어별 에러 타입 전체 목록, 테스트 어설션 패턴

## 도메인 이벤트

### 기반 속성

모든 도메인 이벤트는 `DomainEvent` abstract record를 상속하며, 다음 기반 속성을 가진다:

| 속성 | 타입 | 설명 |
|------|------|------|
| `OccurredAt` | `DateTimeOffset` | 이벤트 발생 시각 (자동: `DateTimeOffset.UtcNow`) |
| `EventId` | `Ulid` | 이벤트 고유 식별자 (자동: `Ulid.NewUlid()`) |
| `CorrelationId` | `string?` | 연관 요청 추적 ID (선택) |
| `CausationId` | `string?` | 원인 이벤트 추적 ID (선택) |

- 기본 생성자(`protected DomainEvent()`)는 `OccurredAt`과 `EventId`를 자동 설정
- 선택적 생성자로 `CorrelationId`, `CausationId` 지정 가능

### 정의 패턴

중첩 sealed record:
- 모든 도메인 이벤트는 소속 애그리거트/엔티티 내부에 중첩 `sealed record`로 정의
- `#region Domain Events` 영역으로 그룹핑
- 이름 규칙: `{BusinessConcept}Event` (예: `CreatedEvent`, `StockDeductedEvent`, `AssignedEvent`)

```csharp
public sealed class Product : AggregateRoot<ProductId>
{
    #region Domain Events
    public sealed record CreatedEvent(ProductId ProductId, ProductName Name, Money Price) : DomainEvent;
    public sealed record UpdatedEvent(ProductId ProductId, ProductName Name, Money OldPrice, Money NewPrice) : DomainEvent;
    #endregion
}
```

### 파라미터 규칙

- 첫 번째 파라미터: 소속 애그리거트/엔티티 ID
- 나머지: 비즈니스적으로 유의미한 값 객체 (변경 전/후 포함 가능)

### 발행 규칙

- `Create()` + 상태 변경 메서드에서 `AddDomainEvent()` 호출
- `CreateFromValidated()`에서는 **발행하지 않음** (ORM 복원 시 중복 방지)

### 공유 엔티티 이벤트

- 비-애그리거트 엔티티(예: `Tag`)도 도메인 이벤트 정의 가능
- 다른 애그리거트(예: `Product.AddTag()`)가 해당 이벤트를 발행할 수 있음

### 이벤트 목록 (01-SingleHost)

| 이벤트 | 파라미터 |
|--------|----------|
| `Customer.CreatedEvent` | `CustomerId`, `CustomerName`, `Email` |
| `Product.CreatedEvent` | `ProductId`, `ProductName`, `Money` |
| `Product.UpdatedEvent` | `ProductId`, `ProductName`, `Money OldPrice`, `Money NewPrice` |
| `Order.CreatedEvent` | `OrderId`, `ProductId`, `Quantity`, `Money TotalAmount` |
| `Inventory.CreatedEvent` | `InventoryId`, `ProductId`, `Quantity StockQuantity` |
| `Inventory.StockDeductedEvent` | `InventoryId`, `ProductId`, `Quantity` |
| `Inventory.StockAddedEvent` | `InventoryId`, `ProductId`, `Quantity` |
| `Tag.AssignedEvent` | `TagId`, `TagName` |
| `Tag.RemovedEvent` | `TagId` |

## 폴더 구성도

호스트 프로젝트의 도메인 계층은 다음과 같은 폴더 구조를 따른다.

**참조 예시** (01-SingleHost `LayeredArch.Domain/`):

```
LayeredArch.Domain/
├── AggregateRoots/
│   ├── Customers/
│   │   ├── Customer.cs
│   │   ├── ICustomerRepository.cs
│   │   ├── Specifications/
│   │   │   └── CustomerEmailSpec.cs
│   │   └── ValueObjects/
│   │       ├── CustomerName.cs
│   │       └── Email.cs
│   ├── Inventories/
│   │   ├── Inventory.cs
│   │   ├── IInventoryRepository.cs
│   │   └── Specifications/
│   │       └── InventoryLowStockSpec.cs
│   ├── Orders/
│   │   ├── Order.cs
│   │   ├── IOrderRepository.cs
│   │   └── ValueObjects/
│   │       └── ShippingAddress.cs
│   └── Products/
│       ├── Product.cs
│       ├── IProductRepository.cs
│       ├── Specifications/
│       │   ├── ProductNameSpec.cs
│       │   ├── ProductNameUniqueSpec.cs
│       │   └── ProductPriceRangeSpec.cs
│       └── ValueObjects/
│           ├── ProductDescription.cs
│           └── ProductName.cs
├── SharedModels/
│   ├── Entities/
│   │   ├── Tag.cs
│   │   └── ValueObjects/
│   │       └── TagName.cs
│   ├── Services/
│   │   └── OrderCreditCheckService.cs
│   └── ValueObjects/
│       ├── Money.cs
│       └── Quantity.cs
├── DOMAIN-GLOSSARY.md
├── Using.cs
└── AssemblyReference.cs
```

**구조 요약**:
- `AggregateRoots/{Aggregate}/` — 애그리거트 루트, 리포지토리 인터페이스, 하위 `Specifications/`와 `ValueObjects/`
- `SharedModels/` — 여러 애그리거트가 공유하는 `Entities/`, `Services/`, `ValueObjects/`
- 루트 — `DOMAIN-GLOSSARY.md`, `Using.cs`, `AssemblyReference.cs`

## 이름 규칙

01-SingleHost에서 관찰된 명명 패턴이다.

### 디렉토리

| 디렉토리 | 규칙 | 예시 |
|-----------|------|------|
| 애그리거트 루트 | `AggregateRoots/{Aggregate복수형}/` | `AggregateRoots/Customers/` |
| 스펙 | `AggregateRoots/{Aggregate복수형}/Specifications/` | `Specifications/` |
| 값 객체 | `AggregateRoots/{Aggregate복수형}/ValueObjects/` | `ValueObjects/` |
| 공유 모델 | `SharedModels/{유형복수형}/` | `SharedModels/Entities/`, `SharedModels/ValueObjects/` |
| 도메인 서비스 | `SharedModels/Services/` | `SharedModels/Services/` |

### 파일 및 클래스

| 대상 | 규칙 | 예시 |
|------|------|------|
| 애그리거트 루트 | `{Aggregate}.cs` | `Customer.cs`, `Order.cs` |
| 리포지토리 인터페이스 | `I{Aggregate}Repository.cs` | `ICustomerRepository.cs` |
| Specification | `{Concept}{Criteria}Spec.cs` | `ProductPriceRangeSpec.cs`, `InventoryLowStockSpec.cs` |
| 값 객체 | `{Name}.cs` | `Money.cs`, `Email.cs`, `CustomerName.cs` |
| 공유 엔티티 | `{Name}.cs` | `Tag.cs` |
| 도메인 서비스 | `{BusinessRule}Service.cs` | `OrderCreditCheckService.cs` |
