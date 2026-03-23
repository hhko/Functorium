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
| 배타적 상태 타입 | `UnionValueObject` | PaymentInfo = CreditCard \| BankTransfer \| PayPal |

---

## 2. Naive 데이터 모델 -> Always-valid 모델 변환

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
    public sealed record AlreadyDeleted : DomainErrorType.Custom;

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
- **에러 추적**: `DomainErrors.ProductName.Empty` 같은 구조화된 에러 코드
- **이벤트 자동 발행**: 비즈니스 행위와 이벤트가 동기화

---

## 3. 결정 트리

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
    │       └── 배타적 타입 분기인가?
    │           │
    │           └── YES → UnionValueObject
    │               예: PaymentInfo = CreditCard | BankTransfer
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

## 4. 타입별 체크리스트

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
- [ ] 중첩 `sealed record : DomainErrorType.Custom` 에러 타입 정의
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
- [ ] 컨텍스트별 추가 검증 (VO 검증과 별도)
- [ ] `Create()` -> `Fin<TSelf>` 팩토리
- [ ] `CreateFromValidated()` ORM 복원용 팩토리
- [ ] 이벤트 직접 발행 금지 (부모 Aggregate에서만 발행)

### ExpressionSpecification 체크리스트

- [ ] `ExpressionSpecification<TEntity>` 상속
- [ ] `ToExpression()` 구현 (`Expression<Func<T, bool>>` 반환)
- [ ] Expression 내에서 VO는 원시 타입으로 변환하여 비교
- [ ] `Option<TId>` 기반 자기 제외 패턴 (업데이트 시)

### IDomainService 체크리스트

- [ ] `IDomainService` 인터페이스 구현
- [ ] `sealed class` 선언
- [ ] 에러 타입: 중첩 `sealed record : DomainErrorType.Custom`
- [ ] `Fin<Unit>` 또는 `Fin<T>` 반환
- [ ] Stateless: 가변 인스턴스 상태 없음
- [ ] 외부 I/O 없음 (기본) 또는 Repository 인터페이스 의존 (Evans Ch.9)
