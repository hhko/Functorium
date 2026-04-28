# 도메인 구현 패턴 — 빠른 참조

> 이 문서는 도메인 구현의 **빠른 참조 체크리스트**입니다.
> 상세 설명은 각 섹션의 참조 링크를 확인하세요.

## 도메인 객체 식별 트리

```
고유 식별자가 필요한가?
├── 아니오 → Value Object (Money, Email, Quantity...)
└── 예 → Entity
         독립적으로 저장/조회되는가?
         ├── 예 → Aggregate Root (Customer, Product, Order...)
         └── 아니오 → 자식 Entity (Tag, OrderItem...)
```

| 기준 | Value Object | 자식 Entity | Aggregate Root |
|------|-------------|------------|---------------|
| 고유 식별자 | 없음 | 있음 | 있음 |
| 동등성 | 값 기반 | ID 기반 | ID 기반 |
| 가변성 | 불변 | 가변 | 가변 |
| 독립 조회 | 불가 | 불가 (Root 통해) | 가능 |
| Repository | 없음 | 없음 | 있음 |
| 도메인 이벤트 | 발행 불가 | 발행 불가 | 발행 가능 |
| 생명주기 | 소유 Entity에 종속 | Root에 종속 | 독립적 |

> 참조: Docs/guides/06a-entities-and-aggregates-design.md

## Aggregate 경계 — 분할 신호

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

> 참조: Docs/guides/06a-entities-and-aggregates-design.md

## Rich vs Anemic 비교표

| 특성 | 빈혈 모델 | Rich Domain Model |
|------|----------|-------------------|
| 속성 접근자 | public setter | private set |
| 행위 | getter/setter만 | 비즈니스 메서드 (Confirm, DeductStock) |
| 불변식 | 외부 서비스에서 검증 | Aggregate 내부에서 캡슐화 |
| 이벤트 | 없음 | 상태 변경 시 도메인 이벤트 발행 |

> 참조: Docs/guides/06b-entity-aggregate-core.md

## 불변 조건 배치 트리 + 안티패턴

**불변 조건 배치**:

```
다른 Aggregate 데이터가 필요한가?
├── NO → Aggregate 메서드 내부 (단일 Aggregate 불변 조건)
└── YES
    ├── 외부 I/O 없이 검증 가능한가?
    │   ├── YES → 순수 Domain Service
    │   └── NO → Usecase가 데이터를 로드할 수 있는 규모인가?
    │       ├── YES → 순수 Domain Service (Usecase가 데이터 전달)
    │       └── NO → Repository 사용 Domain Service (Evans Ch.9)
    └── 상태 변경이 여러 Aggregate에 걸치는가?
        ├── YES → Domain Event + 최종 일관성
        └── NO → Domain Service
```

**도메인 로직 배치**:

```
로직이 단일 Aggregate에 속하는가?
├── YES → Entity 메서드 또는 Value Object
└── NO
    ├── 외부 I/O가 필요한가?
    │   ├── Usecase가 데이터를 로드할 수 있는 규모인가?
    │   │   ├── YES → 순수 Domain Service (Usecase가 데이터 전달)
    │   │   └── NO → Repository 사용 Domain Service (Evans Ch.9)
    │   └── I/O 불필요 → 순수 Domain Service
    └── 여러 Aggregate의 상태를 변경하는가?
        ├── YES → Domain Event + 별도 Handler
        └── NO → Domain Service
```

| 배치 위치 | 기준 | 예시 |
|----------|------|------|
| **Entity 메서드** | 단일 Aggregate 내부 상태 변경 | `Inventory.DeductStock()` |
| **Value Object** | 값의 검증, 변환, 연산 | `Money.Add()` |
| **Domain Service (순수)** | 여러 Aggregate 참조, Usecase가 데이터 로드 가능 | `OrderCreditCheckService.ValidateCreditLimit()` |
| **Domain Service (Repository)** | 여러 Aggregate 참조, 대규모 교차 데이터 (Evans Ch.9) | `ContactEmailCheckService.ValidateEmailUnique()` |
| **Usecase** | 조율, I/O 위임 | Repository 호출, Event 발행 |

**안티패턴 3종**:

| 안티패턴 | 증상 | 해결 |
|---------|------|------|
| Fat Usecase | 비즈니스 로직이 Usecase에 집중 | Entity 메서드 또는 Domain Service로 이동 |
| Anemic Domain Model | Entity가 getter/setter만 보유 | Entity에 비즈니스 메서드 추가 |
| Domain Service 남용 | 모든 로직을 Domain Service에 배치 | 단일 Aggregate 로직은 Entity 메서드로 |

> 참조: Docs/guides/09-domain-services.md

## 엔티티 ID 체계

| 항목 | 설명 |
|------|------|
| 소스 생성기 | `[GenerateEntityId]` 적용 |
| 생성 타입 | `{Entity}Id` — `readonly partial record struct`, `IEntityId<T>` 구현 |
| 기반 | `Ulid` (시간순 정렬 가능한 128비트 식별자) |
| `New()` | 신규 ID 생성 |
| `Create(Ulid)` / `Create(string)` | 기존 값으로 복원 |
| `Empty` | 빈 ID (`Ulid.Empty`) |
| 비교 | `<`, `>`, `<=`, `>=`, `IParsable<T>` 구현 |
| 영속성 | `ValueConverter<{Type}Id, string>` — DB에 문자열로 저장 |

> 참조: Docs/guides/06b-entity-aggregate-core.md

## 값 객체 — 기반 클래스 + 팩토리 3종

| 기반 클래스 | 용도 | 적용 대상 |
|------------|------|-----------|
| `SimpleValueObject<T>` | 동등비교만 (`==`, `!=`) | 모든 string VO — CustomerName, Email, ProductName, ProductDescription, ShippingAddress, TagName |
| `ComparableSimpleValueObject<T>` | 비교연산자 포함 (`<`, `>`, `<=`, `>=`) | Money (`decimal`), Quantity (`int`) |

| 메서드 | 반환 타입 | 용도 |
|--------|-----------|------|
| `Create(raw)` | `Fin<T>` | 외부 입력 검증 + 생성 (애플리케이션 계층) |
| `Validate(raw)` | `Validation<Error, T>` | 검증만 수행 (병렬 검증 수집용) |
| `CreateFromValidated(value)` | `T` (직접 반환) | 검증 우회 — ORM 복원 전용 |

> 참조: Docs/guides/05a-value-objects.md

## 열거형 — 선택 기준

| 선택지 | 기반 클래스 | 용도 |
|--------|-----------|------|
| 고정된 선택지 + 값마다 고유 속성/동작 | `SmartEnum<T, TValue>` + `IValueObject` | Currency 등 |
| 고정된 선택지 + 상태 전이 로직 | `SimpleValueObject<string>` (Smart Enum 패턴) | OrderStatus 등 |
| 단순 플래그/상태 | C# `enum` | 충분한 경우 |

> 참조: Docs/guides/05a-value-objects.md

## 애그리거트 — 팩토리 이원화 + 캡슐화

**팩토리 이원화**:

| 메서드 | 용도 | 이벤트 발행 | 반환 |
|--------|------|------------|------|
| `Create(...)` | 신규 생성 (애플리케이션 계층) | O | `T` (직접 반환) |
| `CreateFromValidated(...)` | ORM 복원 (인프라 계층) | X | `T` (직접 반환) |

**캡슐화 규칙**:
- 모든 도메인 클래스(애그리거트, 엔티티, Specification)는 `sealed`
- 속성: `{ get; private set; }` — 행위 메서드를 통해서만 상태 변경
- 컬렉션: `private readonly List<T>` + `public IReadOnlyList<T>` 노출

> 참조: Docs/guides/06b-entity-aggregate-core.md

## 함수형 타입 규칙표

| 타입 | 용도 | 사용 위치 |
|------|------|-----------|
| `Fin<T>` | 동기 실패 가능 결과 | VO `Create()`, `DeductStock()`, `OrderCreditCheckService` |
| `Validation<Error, T>` | 병렬 검증 수집 (여러 오류 누적) | VO `Validate()` |
| `FinT<IO, T>` | 비동기 IO + 실패 가능 결과 | 리포지토리 인터페이스 전체 |
| `Option<T>` | null 대체 (값이 없을 수 있음) | `IAuditable.UpdatedAt`, `ProductNameUniqueSpec.ExcludeId` |
| `Seq<T>` | 불변 시퀀스 | `OrderCreditCheckService.ValidateCreditLimitWithExistingOrders(Seq<Order>)` |

## 에러 — 범주 + Custom 패턴

**표준 에러 타입** (`DomainErrorKind` 범주):

| 범주 | 대표 타입 |
|------|----------|
| Presence | `Empty`, `Null` |
| Length | `TooShort`, `TooLong`, `WrongLength` |
| Format | `InvalidFormat`, `NotUpperCase`, `NotLowerCase` |
| Numeric | `Zero`, `Negative`, `NotPositive`, `OutOfRange`, `BelowMinimum`, `AboveMaximum` |
| Existence | `NotFound`, `AlreadyExists`, `Duplicate`, `Mismatch` |

**Custom 에러** — 해당 엔티티/VO 내부에 nested sealed record로 정의:

```csharp
public sealed class Inventory : AggregateRoot<InventoryId>
{
    public sealed record InsufficientStock : DomainErrorKind.Custom;

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

> 참조: Docs/guides/08a-error-system-overview.md, Docs/guides/08b-error-system-domain-app.md

## 이벤트 — 기반 속성 + 발행 규칙

| 속성 | 타입 | 설명 |
|------|------|------|
| `OccurredAt` | `DateTimeOffset` | 이벤트 발생 시각 (자동: `DateTimeOffset.UtcNow`) |
| `EventId` | `Ulid` | 이벤트 고유 식별자 (자동: `Ulid.NewUlid()`) |
| `CorrelationId` | `string?` | 연관 요청 추적 ID (선택) |
| `CausationId` | `string?` | 원인 이벤트 추적 ID (선택) |

**발행 규칙**:
- `Create()` + 상태 변경 메서드에서 `AddDomainEvent()` 호출
- `CreateFromValidated()`에서는 **발행하지 않음** (ORM 복원 시 중복 방지)

> 참조: Docs/guides/07-domain-events.md

---

> 실제 구현 예시: `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/` 참조
