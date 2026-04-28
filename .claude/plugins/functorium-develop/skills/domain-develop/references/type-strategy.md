# 불변식 분류 -> Functorium 타입 매핑 가이드

비즈니스 규칙의 불변식을 분석하여 올바른 Functorium 타입으로 매핑하는 전략입니다.

---

## 1. 불변식 분류 기준표

| 불변식 유형 | Functorium 패턴 | 예시 |
|-------------|----------------|------|
| 단일값 + 형식 규칙 | `SimpleValueObject<string>` | ProductName, Email, PhoneNumber |
| 단일값 + 크기/범위 비교 | `ComparableSimpleValueObject<T>` | Money(decimal), Quantity(int), Age(int) |
| 복합값 (2개 이상 필드) | `ValueObject` (직접 상속) | Address(Street, City, ZipCode), DateRange(Start, End) |
| 유한 상태 집합 | SmartEnum (`SimpleValueObject<string>` + `HashMap`) | OrderStatus, PaymentMethod |
| 유한 상태 + 전이 규칙 | SmartEnum + `AllowedTransitions` | OrderStatus(Pending->Confirmed->Shipped) |
| 식별 가능 + 생명주기 | `AggregateRoot<TId>` | Product, Order, Customer |
| 식별 가능 + 부모 종속 | `Entity<TId>` | OrderLine, CartItem |
| 조건부 필터/쿼리 | `ExpressionSpecification<T>` | ProductNameUniqueSpec, PriceRangeSpec |
| 교차 Aggregate 규칙 | `IDomainService` | OrderCreditCheckService |
| 배타적 상태 조합 | `UnionValueObject` + `[UnionType]` | ContactInfo = EmailOnly \| PostalOnly \| EmailAndPostal |
| 배타적 상태 전이 | `UnionValueObject<TSelf>` + `TransitionFrom` | EmailVerificationState = Unverified \| Verified |

---

## 2. 배타적 상태 타입: enum vs SmartEnum vs UnionValueObject

### 2-1. 선택 결정 트리

```
케이스별로 다른 데이터를 가지는가?
├── No → 상태 전이 규칙이 있는가?
│   ├── No → SmartEnum (HashMap 기반)
│   └── Yes → SmartEnum + AllowedTransitions
└── Yes → 상태 전이가 필요한가?
    ├── No → UnionValueObject + [UnionType]
    └── Yes → UnionValueObject<TSelf> + TransitionFrom
```

### 2-2. 비교표

| 기준 | SmartEnum | UnionValueObject | UnionValueObject\<TSelf\> |
|------|----------|-----------------|-------------------------|
| 케이스별 데이터 | 동일 (없거나 공통 속성) | **케이스별 다름** | **케이스별 다름** |
| 상태 전이 | AllowedTransitions HashMap | 없음 | TransitionFrom 메서드 |
| 무효 상태 방지 | 런타임 검증 | **컴파일 타임** | **컴파일 타임** |
| 패턴 매칭 | switch/if | **Match/Switch (exhaustive)** | **Match/Switch (exhaustive)** |
| EF Core 저장 | int/string 직렬화 | 프로젝션 속성 필요 | 프로젝션 속성 필요 |
| 예시 | OrderStatus, Genre | ContactInfo, PaymentInfo | EmailVerificationState |

### 2-3. 코드 예시

**SmartEnum (공통 데이터):**
```csharp
// 모든 케이스가 같은 구조 (Value, Name)
public sealed class OrderStatus : SimpleValueObject<string>
{
    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    // AllowedTransitions으로 전이 규칙
}
```

**UnionValueObject (케이스별 다른 데이터):**
```csharp
// EmailOnly는 EmailState만, PostalOnly는 Address만
[UnionType]
public abstract partial record ContactInfo : UnionValueObject
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
    public sealed record EmailAndPostal(EmailVerificationState EmailState, PostalAddress Address) : ContactInfo;
    private ContactInfo() { }
}
```

**UnionValueObject\<TSelf\> (타입 안전 전이):**
```csharp
// Unverified → Verified 단방향 전이
[UnionType]
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState>
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;
    private EmailVerificationState() { }

    public Fin<Verified> Verify(DateTime verifiedAt) =>
        TransitionFrom<Unverified, Verified>(u => new Verified(u.Email, verifiedAt));
}
```

---

## 3. Naive 데이터 모델 -> Always-valid 모델 변환

### Before: Naive 모델 (원시 타입)

```csharp
// Primitive Obsession: 비즈니스 규칙이 코드에 없음
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }        // null? 빈 문자열? 길이 제한?
    public string Description { get; set; }
    public decimal Price { get; set; }       // 음수? 0?
    public string Status { get; set; }       // 아무 문자열이나 가능
}
```

문제점:
- **null/빈 문자열** 허용 -> 런타임 NullReferenceException
- **음수 가격** 허용 -> 비즈니스 규칙 위반이 DB까지 전파
- **임의 상태 문자열** -> "Pendign" 오타도 컴파일 통과
- **Guid ID** -> 생성 시점 순서 보장 없음

### After: Always-valid 모델 (Functorium)

```csharp
[GenerateEntityId]  // Ulid 기반 ID 자동 생성
public sealed class Product : AggregateRoot<ProductId>, IAuditable
{
    // 에러 타입 정의
    public sealed record AlreadyDeleted : DomainErrorKind.Custom;

    // 이벤트 정의
    public sealed record CreatedEvent(ProductId ProductId, ProductName Name, Money Price) : DomainEvent;

    // Value Object 속성 (Always-valid 보장)
    public ProductName Name { get; private set; }           // null/빈/100자 초과 불가
    public ProductDescription Description { get; private set; }
    public Money Price { get; private set; }                // 0 이하 불가

    private Product(ProductId id, ProductName name, ProductDescription description, Money price)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }

    // 팩토리: 이벤트 발행 포함
    public static Product Create(ProductName name, ProductDescription description, Money price)
    {
        var product = new Product(ProductId.New(), name, description, price);
        product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
        return product;
    }
}
```

변환 효과:
- **컴파일 타임 안전성**: 잘못된 값 생성 불가
- **불변식 중앙화**: 검증 로직이 VO 내부에 단 한 곳
- **에러 추적**: `Domain.ProductName.Empty` 같은 구조화된 에러 코드
- **이벤트 자동 발행**: 비즈니스 행위와 이벤트가 동기화

---

## 4. 결정 트리

비즈니스 개념을 Functorium 타입으로 매핑할 때 다음 순서로 판단합니다:

```
이 개념은 식별자(Identity)가 필요한가?
│
├── YES → 생명주기를 독립적으로 관리하는가?
│   │
│   ├── YES → AggregateRoot<TId>
│   │   - 트랜잭션 경계의 루트
│   │   - 도메인 이벤트 발행 주체
│   │   - Repository를 통한 영속화
│   │   예: Product, Order, Customer, Inventory
│   │
│   └── NO → Entity<TId>
│       - 부모 Aggregate에 종속
│       - 부모를 통해서만 접근/영속화
│       - 이벤트 직접 발행 불가
│       예: OrderLine, CartItem
│
└── NO → 값으로만 비교하는가? (동등성 = 값 동등성)
    │
    ├── 단일 값을 래핑하는가?
    │   │
    │   ├── 크기 비교/산술 연산이 필요한가?
    │   │   │
    │   │   ├── YES → ComparableSimpleValueObject<T>
    │   │   │   예: Money, Quantity, Age, Weight
    │   │   │
    │   │   └── NO → SimpleValueObject<T>
    │   │       예: ProductName, Email, PhoneNumber
    │   │
    │   └── 유한 상태 집합인가?
    │       │
    │       ├── 상태 전이 규칙이 있는가?
    │       │   │
    │       │   ├── YES → SmartEnum + AllowedTransitions
    │       │   │   예: OrderStatus, TaskStatus
    │       │   │
    │       │   └── NO → SmartEnum (HashMap만)
    │       │       예: PaymentMethod, Currency
    │       │
    │       └── 케이스별로 다른 데이터를 가지는가?
    │           │
    │           ├── YES + 상태 전이 필요 → UnionValueObject<TSelf> + TransitionFrom
    │           │   예: EmailVerificationState = Unverified | Verified
    │           │
    │           └── YES + 전이 불필요 → UnionValueObject + [UnionType]
    │               예: ContactInfo = EmailOnly | PostalOnly | EmailAndPostal
    │
    ├── 복합 값 (2개 이상 필드)인가?
    │   │
    │   └── ValueObject (직접 상속)
    │       예: Address, DateRange, GpsCoordinate
    │
    ├── 조건 표현식인가?
    │   │
    │   └── ExpressionSpecification<T>
    │       예: ProductNameUniqueSpec, PriceRangeSpec
    │
    └── 여러 Aggregate를 참조하는 규칙인가?
        │
        └── IDomainService
            예: OrderCreditCheckService, PricingService
```

---

## 5. 타입별 체크리스트

### SimpleValueObject 체크리스트

- [ ] `private 생성자(T value) : base(value)` 정의
- [ ] `Create(T? value)` -> `Fin<TSelf>` 팩토리 정의
- [ ] `Validate(T? value)` -> `Validation<Error, T>` 정의
- [ ] `CreateFromValidated(T value)` -> `TSelf` 정의
- [ ] `implicit operator T` 정의
- [ ] `ValidationRules<TSelf>` 체인으로 검증 규칙 구성
- [ ] 상수: `MaxLength`, `MinLength` 등 필요 시 정의

### AggregateRoot 체크리스트

- [ ] `[GenerateEntityId]` 속성 적용
- [ ] `sealed class` 선언
- [ ] 중첩 `sealed record : DomainErrorKind.Custom` 에러 타입 정의
- [ ] 중첩 `sealed record : DomainEvent` 이벤트 정의
- [ ] VO 속성: `{ get; private set; }` 패턴
- [ ] `private 생성자` 정의
- [ ] `Create()` 팩토리 + `AddDomainEvent()` 호출
- [ ] `CreateFromValidated()` ORM 복원용 팩토리
- [ ] 상태 변경 메서드: `Fin<T>` 또는 `Fin<Unit>` 반환
- [ ] 멱등성 보장 (이미 적용된 상태면 무시)
- [ ] 적절한 마커 인터페이스: `IAuditable`, `ISoftDeletable`, `IConcurrencyAware`

### Entity (자식) 체크리스트

- [ ] `[GenerateEntityId]` 속성 적용
- [ ] `sealed class` 선언
- [ ] `private` 생성자로 외부 생성 차단
- [ ] `Create()` → `Fin<TSelf>` 또는 `TSelf` 팩토리 (컨텍스트별 검증 포함)
- [ ] `CreateFromValidated()` ORM 복원용 팩토리
- [ ] 컨텍스트별 추가 검증은 VO 검증과 분리 (예: VO는 `Quantity >= 0`, Entity는 `quantity > 0`)
- [ ] 이벤트 직접 발행 금지 (부모 Aggregate에서만 `AddDomainEvent`)
- [ ] 부모 Aggregate에 `private List<T>` + `IReadOnlyList<T>` 노출
- [ ] 부모 Aggregate에 `Add/Remove` 메서드 정의 (Entity 직접 추가/삭제 금지)
- [ ] Entity 자체 행위 메서드는 `TSelf` 또는 `void` 반환 (이벤트 없이 상태만 변경)
- [ ] ORM 복원 시 부모 `CreateFromValidated`에서 컬렉션 재구성

### UnionValueObject 체크리스트

- [ ] `abstract partial record` 선언
- [ ] `[UnionType]` 어트리뷰트 추가
- [ ] 베이스 클래스 선택: `UnionValueObject` (순수 데이터) 또는 `UnionValueObject<TSelf>` (상태 전이)
- [ ] `private` 생성자로 외부 확장 차단
- [ ] 모든 케이스를 `sealed record`로 정의
- [ ] 상태 전이 시 `TransitionFrom<TSource, TTarget>(converter)` 사용
- [ ] Aggregate 연동 시 프로젝션 속성 검토 (Spec 쿼리용 평탄화)
- [ ] Match/Switch 사용 시 모든 케이스 exhaustive 처리

### ExpressionSpecification 체크리스트

- [ ] `ExpressionSpecification<TEntity>` 상속
- [ ] `ToExpression()` 구현 (`Expression<Func<T, bool>>` 반환)
- [ ] Expression 내에서 VO는 원시 타입으로 변환하여 비교
- [ ] `Option<TId>` 기반 자기 제외 패턴 (업데이트 시)

### IDomainService 체크리스트

- [ ] `IDomainService` 인터페이스 구현
- [ ] `sealed class` 선언
- [ ] 에러 타입: 중첩 `sealed record : DomainErrorKind.Custom`
- [ ] `Fin<Unit>` 또는 `Fin<T>` 반환
- [ ] Stateless: 가변 인스턴스 상태 없음
- [ ] 외부 I/O 없음 (기본) 또는 Repository 인터페이스 의존 (Evans Ch.9)
