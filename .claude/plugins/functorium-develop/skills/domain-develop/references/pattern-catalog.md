# Functorium DDD 패턴 카탈로그

Functorium 프레임워크의 핵심 DDD 패턴과 코드 예제를 정리합니다.
모든 예제는 `Tests.Hosts/01-SingleHost` 프로젝트 기반입니다.

---

## 1. SimpleValueObject\<T\> — 단일값 불변식

단일 원시 값을 래핑하여 Always-valid를 보장하는 값 객체입니다.

### 기본 패턴: ProductName

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
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static ProductName CreateFromValidated(string value) => new(value);

    public static implicit operator string(ProductName productName) => productName.Value;
}
```

### 정규식 검증 패턴: Email

```csharp
public sealed partial class Email : SimpleValueObject<string>
{
    public const int MaxLength = 320;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format");

    public static Email CreateFromValidated(string value) => new(value);

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
```

### 핵심 구조

| 구성 요소 | 역할 |
|-----------|------|
| `private 생성자` | 외부 직접 생성 방지 |
| `Create()` | 검증 + 생성 팩토리 (`Fin<T>` 반환) |
| `Validate()` | `Validation<Error, T>` 반환 (Apply 병합용) |
| `CreateFromValidated()` | ORM 복원용 (검증 생략) |
| `implicit operator` | 원시 타입으로 암묵적 변환 |

### ValidationRules 체인

`ValidationRules<TValueObject>` 정적 클래스에서 시작하는 fluent 체인:

```
.NotNull(value)         → null 검사
.ThenNotEmpty()         → 빈 문자열 검사
.ThenMaxLength(n)       → 최대 길이 검사
.ThenMatches(regex, msg) → 정규식 패턴 검사
.ThenNormalize(func)    → 정규화 변환
```

에러 코드는 `DomainErrors.{TypeName}.{RuleName}` 형식으로 자동 생성됩니다.
예: `Domain.ProductName.Empty`, `Domain.Email.InvalidFormat`

---

## 2. ComparableSimpleValueObject\<T\> — 비교/연산 가능 값 객체

`IComparable`을 구현하는 값을 래핑합니다. 비교 연산자(`<`, `>`, `<=`, `>=`)를 지원합니다.

### Money (양수 금액)

```csharp
public sealed class Money : ComparableSimpleValueObject<decimal>
{
    /// 합산 연산의 항등원
    public static readonly Money Zero = new(0m);

    private Money(decimal value) : base(value) { }

    public static Fin<Money> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Money(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Money>
            .Positive(value);

    public static Money CreateFromValidated(decimal value) => new(value);

    public static implicit operator decimal(Money money) => money.Value;

    // 산술 연산
    public Money Add(Money other) => new(Value + other.Value);
    public Fin<Money> Subtract(Money other) => Create(Value - other.Value);
    public Money Multiply(decimal factor) => new(Value * factor);

    // 집합 연산
    public static Money Sum(IEnumerable<Money> values) =>
        values.Aggregate(Zero, (acc, m) => acc.Add(m));
}
```

### Quantity (0 이상 정수)

```csharp
public sealed class Quantity : ComparableSimpleValueObject<int>
{
    private Quantity(int value) : base(value) { }

    public static Fin<Quantity> Create(int value) =>
        CreateFromValidation(Validate(value), v => new Quantity(v));

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<Quantity>
            .NonNegative(value);

    public static Quantity CreateFromValidated(int value) => new(value);

    public static implicit operator int(Quantity quantity) => quantity.Value;

    public Quantity Add(int amount) => new(Value + amount);
    public Quantity Subtract(int amount) => new(Math.Max(0, Value - amount));
}
```

### 핵심 포인트

- `Subtract`가 `Fin<T>`를 반환하면 음수 방지를 컴파일 타임에 강제
- `Zero` 상수는 항등원 패턴 (집합 연산의 시작점)
- 비교 연산은 `ComparableValueObject` 기본 클래스에서 자동 제공

---

## 3. SmartEnum — 열거형 상태 + 상태 전이

`SimpleValueObject<string>`을 확장하여 유한 상태 집합과 전이 규칙을 구현합니다.

### OrderStatus

```csharp
public sealed class OrderStatus : SimpleValueObject<string>
{
    #region Error Types
    public sealed record InvalidValue : DomainErrorKind.Custom;
    #endregion

    // 열거형 인스턴스
    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    // 전체 목록 (이름 → 인스턴스 매핑)
    private static readonly HashMap<string, OrderStatus> All = HashMap(
        ("Pending", Pending),
        ("Confirmed", Confirmed),
        ("Shipped", Shipped),
        ("Delivered", Delivered),
        ("Cancelled", Cancelled));

    // 상태 전이 규칙
    private static readonly HashMap<string, Seq<string>> AllowedTransitions = HashMap(
        ("Pending", Seq("Confirmed", "Cancelled")),
        ("Confirmed", Seq("Shipped", "Cancelled")),
        ("Shipped", Seq("Delivered")));

    private OrderStatus(string value) : base(value) { }

    public static Fin<OrderStatus> Create(string value) =>
        Validate(value).ToFin();

    public static Validation<Error, OrderStatus> Validate(string value) =>
        All.Find(value)
            .ToValidation(DomainError.For<OrderStatus>(
                new InvalidValue(),
                currentValue: value,
                message: $"Invalid order status: '{value}'"));

    public static OrderStatus CreateFromValidated(string value) =>
        All.Find(value)
            .IfNone(() => throw new InvalidOperationException(
                $"Invalid order status for CreateFromValidated: '{value}'"));

    public bool CanTransitionTo(OrderStatus target) =>
        AllowedTransitions.Find(Value)
            .Map(allowed => allowed.Any(v => v == target.Value))
            .IfNone(false);

    public static implicit operator string(OrderStatus status) => status.Value;
}
```

### 핵심 구조

| 구성 요소 | 역할 |
|-----------|------|
| `static readonly` 인스턴스 | 유한 상태 집합 정의 |
| `HashMap<string, T> All` | 이름 기반 조회 |
| `HashMap<string, Seq<string>> AllowedTransitions` | 상태 전이 규칙 |
| `CanTransitionTo()` | 전이 가능 여부 확인 |

---

## 4. AggregateRoot\<TId\> — 생명주기 관리 엔티티

Aggregate Root는 트랜잭션 일관성 경계이며 도메인 이벤트의 발행 주체입니다.

### Product (기본 Aggregate)

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId>, IAuditable, ISoftDeletableWithUser
{
    #region Error Types
    public sealed record AlreadyDeleted : DomainErrorKind.Custom;
    #endregion

    #region Domain Events
    public sealed record CreatedEvent(ProductId ProductId, ProductName Name, Money Price) : DomainEvent;
    public sealed record UpdatedEvent(ProductId ProductId, ProductName Name, Money OldPrice, Money NewPrice) : DomainEvent;
    public sealed record DeletedEvent(ProductId ProductId, string DeletedBy) : DomainEvent;
    public sealed record RestoredEvent(ProductId ProductId) : DomainEvent;
    #endregion

    // Value Object 속성
    public ProductName Name { get; private set; }
    public ProductDescription Description { get; private set; }
    public Money Price { get; private set; }

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // SoftDelete 속성
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }

    // private 생성자: 이미 검증된 VO를 받음
    private Product(ProductId id, ProductName name, ProductDescription description, Money price)
        : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        CreatedAt = DateTime.UtcNow;
    }

    /// Create: 검증된 VO를 받아 생성 + 이벤트 발행
    public static Product Create(ProductName name, ProductDescription description, Money price)
    {
        var product = new Product(ProductId.New(), name, description, price);
        product.AddDomainEvent(new CreatedEvent(product.Id, name, price));
        return product;
    }

    /// CreateFromValidated: ORM 복원용
    public static Product CreateFromValidated(
        ProductId id, ProductName name, ProductDescription description, Money price,
        IEnumerable<TagId> tagIds, DateTime createdAt, Option<DateTime> updatedAt,
        Option<DateTime> deletedAt, Option<string> deletedBy)
    {
        var product = new Product(id, name, description, price)
        {
            CreatedAt = createdAt, UpdatedAt = updatedAt,
            DeletedAt = deletedAt, DeletedBy = deletedBy
        };
        product._tagIds.AddRange(tagIds);
        return product;
    }

    /// 업데이트: 삭제 상태 검증 → Fin<T> 반환
    public Fin<Product> Update(ProductName name, ProductDescription description, Money price)
    {
        if (DeletedAt.IsSome)
            return DomainError.For<Product>(
                new AlreadyDeleted(), Id.ToString(),
                "Cannot update a deleted product");

        var oldPrice = Price;
        Name = name; Description = description; Price = price;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UpdatedEvent(Id, name, oldPrice, price));
        return this;
    }

    /// 삭제: 멱등성 보장
    public Product Delete(string deletedBy)
    {
        if (DeletedAt.IsSome) return this;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        AddDomainEvent(new DeletedEvent(Id, deletedBy));
        return this;
    }
}
```

### Order (상태 전이 포함 Aggregate)

```csharp
[GenerateEntityId]
public sealed class Order : AggregateRoot<OrderId>, IAuditable
{
    #region Error Types
    public sealed record EmptyOrderLines : DomainErrorKind.Custom;
    public sealed record InvalidOrderStatusTransition : DomainErrorKind.Custom;
    #endregion

    #region Domain Events
    public sealed record CreatedEvent(
        OrderId OrderId, CustomerId CustomerId,
        Seq<OrderLineInfo> OrderLines, Money TotalAmount) : DomainEvent, ICustomerEvent;
    public sealed record ConfirmedEvent(OrderId OrderId) : DomainEvent;
    public sealed record CancelledEvent(OrderId OrderId) : DomainEvent;
    #endregion

    public CustomerId CustomerId { get; private set; }
    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    // Create: 불변 조건 검증 (최소 1개 라인) → Fin<Order>
    public static Fin<Order> Create(
        CustomerId customerId, IEnumerable<OrderLine> orderLines, ShippingAddress shippingAddress)
    {
        var lines = orderLines.ToList();
        if (lines.Count == 0)
            return DomainError.For<Order, int>(
                new EmptyOrderLines(), currentValue: 0,
                message: "Order must contain at least one order line");

        var totalAmount = Money.CreateFromValidated(lines.Sum(l => (decimal)l.LineTotal));
        var order = new Order(OrderId.New(), customerId, lines, totalAmount, shippingAddress);
        order.AddDomainEvent(new CreatedEvent(order.Id, customerId, /*...*/, totalAmount));
        return order;
    }

    // 상태 전이: SmartEnum과 결합
    public Fin<Unit> Confirm() => TransitionTo(OrderStatus.Confirmed, new ConfirmedEvent(Id));

    private Fin<Unit> TransitionTo(OrderStatus target, DomainEvent domainEvent)
    {
        if (!Status.CanTransitionTo(target))
            return DomainError.For<Order, string, string>(
                new InvalidOrderStatusTransition(),
                value1: Status, value2: target,
                message: $"Cannot transition from '{Status}' to '{target}'");

        Status = target;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(domainEvent);
        return unit;
    }
}
```

### 핵심 구조

| 구성 요소 | 역할 |
|-----------|------|
| `[GenerateEntityId]` | `{Entity}Id` 구조체 자동 생성 (Ulid 기반) |
| `private 생성자` | 직접 생성 방지 |
| `Create()` | 팩토리 + 이벤트 발행 (비즈니스 생성) |
| `CreateFromValidated()` | ORM 복원용 (검증/이벤트 없음) |
| 중첩 `sealed record : DomainEvent` | Aggregate 소속 이벤트 |
| 중첩 `sealed record : DomainErrorKind.Custom` | Aggregate 소속 에러 타입 |
| `IAuditable` | `CreatedAt`, `UpdatedAt` |
| `ISoftDeletableWithUser` | `DeletedAt`, `DeletedBy` |
| `IConcurrencyAware` | `RowVersion` (낙관적 동시성) |

### 이벤트 이름 규칙

- 과거형: `CreatedEvent`, `UpdatedEvent`, `DeletedEvent`
- Aggregate 이름 접두사 생략 (중첩 record이므로 `Product.CreatedEvent`로 참조)

### DomainError.For 사용법

```csharp
// 단일값 에러
DomainError.For<Product>(new AlreadyDeleted(), Id.ToString(), "message");

// 제네릭 값 에러
DomainError.For<OrderLine, int>(new InvalidQuantity(), currentValue: quantity, message: "message");

// 두 값 에러 (상태 전이 등)
DomainError.For<Order, string, string>(
    new InvalidOrderStatusTransition(), value1: Status, value2: target, message: "message");
```

---

## 5. Entity\<TId\> — 자식 엔티티

Aggregate 내부의 자식 엔티티입니다. 독립적으로 영속화되지 않으며 부모 Aggregate를 통해서만 접근합니다.

### 선택 기준: Entity vs VO vs Aggregate

| 기준 | Value Object | Entity | Aggregate Root |
|------|-------------|--------|---------------|
| 식별자 | 없음 (값으로 비교) | **있음 (ID로 구별)** | 있음 + 트랜잭션 경계 |
| 생명주기 | 부모에 종속, 교체 방식 | **부모에 종속, 개별 추가/삭제** | 독립적 |
| 영속화 | 부모와 함께 | **부모를 통해서만** | Repository 통해 독립적 |
| 이벤트 발행 | 불가 | **불가 (부모만 발행)** | 가능 |
| 사용 시점 | 개별 추적 불필요 | **개별 항목 추가/삭제/조회 필요** | 독립적 CRUD 필요 |

**Entity를 선택하는 핵심 조건:** 컬렉션 내 개별 항목을 ID로 식별하여 추가/삭제/수정해야 할 때.

### 5-1. 검증 + 계산 Entity: OrderLine

컨텍스트별 추가 검증과 파생 값(LineTotal)을 가지는 패턴입니다.

```csharp
[GenerateEntityId]
public sealed class OrderLine : Entity<OrderLineId>
{
    #region Error Types
    public sealed record InvalidQuantity : DomainErrorKind.Custom;
    #endregion

    public ProductId ProductId { get; private set; }
    public Quantity Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal { get; private set; }

    private OrderLine(OrderLineId id, ProductId productId, Quantity quantity,
        Money unitPrice, Money lineTotal) : base(id)
    {
        ProductId = productId; Quantity = quantity;
        UnitPrice = unitPrice; LineTotal = lineTotal;
    }

    /// Create: 컨텍스트별 추가 검증 (수량 > 0)
    public static Fin<OrderLine> Create(ProductId productId, Quantity quantity, Money unitPrice)
    {
        if ((int)quantity <= 0)
            return DomainError.For<OrderLine, int>(
                new InvalidQuantity(), currentValue: quantity,
                message: "Order line quantity must be greater than 0");

        var lineTotal = unitPrice.Multiply(quantity);
        return new OrderLine(OrderLineId.New(), productId, quantity, unitPrice, lineTotal);
    }

    public static OrderLine CreateFromValidated(
        OrderLineId id, ProductId productId, Quantity quantity,
        Money unitPrice, Money lineTotal)
    {
        return new OrderLine(id, productId, quantity, unitPrice, lineTotal);
    }
}
```

### 5-2. 행위 Entity: AssessmentCriterion

자체 상태 변경 메서드를 가지는 패턴입니다. 이벤트는 발행하지 않고, 부모 Aggregate가 대신 발행합니다.

```csharp
[GenerateEntityId]
public sealed class AssessmentCriterion : Entity<AssessmentCriterionId>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Option<CriterionResult> Result { get; private set; }
    public Option<string> Notes { get; private set; }
    public Option<DateTime> EvaluatedAt { get; private set; }

    private AssessmentCriterion(AssessmentCriterionId id, string name, string description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    public static AssessmentCriterion Create(string name, string description) =>
        new(AssessmentCriterionId.New(), name, description);

    public static AssessmentCriterion CreateFromValidated(
        AssessmentCriterionId id, string name, string description,
        Option<CriterionResult> result, Option<string> notes, Option<DateTime> evaluatedAt)
        => new(id, name, description) { Result = result, Notes = notes, EvaluatedAt = evaluatedAt };

    /// Entity 자체 행위: 이벤트 발행 없이 상태만 변경
    public AssessmentCriterion Evaluate(CriterionResult result, Option<string> notes)
    {
        Result = result;
        Notes = notes;
        EvaluatedAt = DateTime.UtcNow;
        return this;
    }
}
```

### 5-3. 불변 Entity: ContactNote

생성 후 변경 불가한 Entity입니다. 삭제만 가능하므로 ID가 필요합니다 (VO와의 차이).

```csharp
[GenerateEntityId]
public sealed class ContactNote : Entity<ContactNoteId>
{
    public NoteContent Content { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ContactNote(ContactNoteId id, NoteContent content, DateTime createdAt) : base(id)
    {
        Content = content;
        CreatedAt = createdAt;
    }

    public static ContactNote Create(NoteContent content, DateTime createdAt)
        => new(ContactNoteId.New(), content, createdAt);

    public static ContactNote CreateFromValidated(
        ContactNoteId id, NoteContent content, DateTime createdAt)
        => new(id, content, createdAt);
}
```

### 5-4. Aggregate 내 컬렉션 관리 패턴

부모 Aggregate에서 자식 Entity 컬렉션을 관리하는 표준 패턴입니다.

```csharp
public sealed class Order : AggregateRoot<OrderId>
{
    // 1. private 가변 컬렉션
    private readonly List<OrderLine> _orderLines = [];

    // 2. IReadOnlyList로 외부 노출 (불변 보장)
    public IReadOnlyList<OrderLine> OrderLines => _orderLines;

    // 3. 추가: Aggregate가 검증 후 컬렉션에 추가
    public Fin<Order> AddOrderLine(ProductId productId, Quantity quantity, Money unitPrice)
    {
        return OrderLine.Create(productId, quantity, unitPrice)
            .Map(line =>
            {
                _orderLines.Add(line);
                AddDomainEvent(new OrderLineAddedEvent(Id, line.Id));
                return this;
            });
    }

    // 4. 삭제: ID로 개별 항목 제거
    public Fin<Order> RemoveOrderLine(OrderLineId lineId)
    {
        var line = _orderLines.Find(l => l.Id == lineId);
        if (line is null)
            return DomainError.For<Order, string>(
                new OrderLineNotFound(), lineId.ToString(), "Order line not found");

        _orderLines.Remove(line);
        AddDomainEvent(new OrderLineRemovedEvent(Id, lineId));
        return this;
    }

    // 5. ORM 복원: CreateFromValidated에서 컬렉션 재구성
    public static Order CreateFromValidated(
        OrderId id, ..., IReadOnlyList<OrderLine> orderLines)
    {
        var order = new Order(id, ...);
        order._orderLines.AddRange(orderLines);
        return order;
    }
}
```

### 핵심 포인트

- `private List<T>` + `IReadOnlyList<T>` — 외부에서 컬렉션 직접 수정 불가
- Entity 추가/삭제는 **반드시 부모 Aggregate 메서드를 통해** 수행
- 이벤트는 **부모 Aggregate만 발행** (Entity는 `AddDomainEvent` 없음)
- Entity 자체 행위 메서드는 `TSelf` 또는 `void` 반환 (이벤트 발행 없이 상태만 변경)
- ORM 복원 시 `CreateFromValidated`에서 부모가 컬렉션을 재구성
- VO의 불변식(예: `Quantity >= 0`)과 Entity 컨텍스트 불변식(예: `OrderLine은 quantity > 0`)은 분리

---

## 6. ExpressionSpecification\<T\> — 조건부 쿼리

Expression Tree 기반 Specification으로 EF Core SQL 자동 번역을 지원합니다.

### ProductNameUniqueSpec

```csharp
public sealed class ProductNameUniqueSpec : ExpressionSpecification<Product>
{
    public ProductName Name { get; }
    public Option<ProductId> ExcludeId { get; }

    public ProductNameUniqueSpec(ProductName name, Option<ProductId> excludeId = default)
    {
        Name = name;
        ExcludeId = excludeId;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = Name;
        string? excludeIdStr = ExcludeId.Match<string?>(id => id.ToString(), () => null);
        return product => (string)product.Name == nameStr &&
                          (excludeIdStr == null || product.Id.ToString() != excludeIdStr);
    }
}
```

### Specification 조합

```csharp
// And 조합
var spec = Specification<Product>.All;

if (request.Name.Length > 0)
    spec &= new ProductNameSpec(ProductName.Create(request.Name).Unwrap());

if (request.MinPrice > 0 && request.MaxPrice > 0)
    spec &= new ProductPriceRangeSpec(
        Money.Create(request.MinPrice).Unwrap(),
        Money.Create(request.MaxPrice).Unwrap());
```

### 핵심 포인트

- `ToExpression()`만 구현하면 `IsSatisfiedBy()`는 자동 제공 (컴파일 + 캐싱)
- `Specification<T>.All`은 항상 true인 기본 Specification
- `&=` (And), `|=` (Or), `!` (Not) 연산자 지원

---

## 7. IDomainService — 교차 Aggregate 규칙

여러 Aggregate에 걸친 비즈니스 규칙을 표현합니다.
Evans Blue Book Ch.9: Stateless 원칙을 따릅니다.

### OrderCreditCheckService

```csharp
public sealed class OrderCreditCheckService : IDomainService
{
    #region Error Types
    public sealed record CreditLimitExceeded : DomainErrorKind.Custom;
    #endregion

    /// 단건 검증
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount)
    {
        if (orderAmount > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"Order amount {(decimal)orderAmount} exceeds credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }

    /// 기존 주문 합산 검증
    public Fin<Unit> ValidateCreditLimitWithExistingOrders(
        Customer customer, Seq<Order> existingOrders, Money newOrderAmount)
    {
        var totalExisting = Money.Sum(existingOrders.Map(o => o.TotalAmount));
        var totalWithNew = totalExisting.Add(newOrderAmount);

        if (totalWithNew > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"Total {(decimal)totalWithNew} exceeds credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }
}
```

### 핵심 원칙

- Stateless: 호출 간 가변 상태 없음
- 순수 함수 패턴: 외부 I/O 없음 (기본)
- Evans Ch.9 패턴: Repository 인터페이스 의존 허용 (대규모 교차 데이터 시)
- `Fin<T>` 반환으로 실패를 명시적으로 표현
- `IObservablePort` 의존성 없음

---

## 8. UnionValueObject — 배타적 상태 조합

구조적으로 무효 상태를 제거하는 Discriminated Union 패턴입니다.
enum/SmartEnum은 모든 케이스가 같은 데이터를 가지지만, Union은 **케이스별로 다른 데이터**를 가집니다.

### 선택 기준: enum vs SmartEnum vs UnionValueObject

| 기준 | enum / SmartEnum | UnionValueObject |
|------|-----------------|-----------------|
| 케이스별 데이터 | 동일 (없거나 공통) | **케이스별 다름** |
| 예시 | OrderStatus(Pending, Confirmed) | ContactInfo(EmailOnly, PostalOnly, EmailAndPostal) |
| 무효 상태 | 런타임 검증으로 방지 | **컴파일 타임에 제거** |
| 상태 전이 | AllowedTransitions HashMap | `TransitionFrom<TSource, TTarget>()` |

### 8-1. 순수 데이터 Union: ContactInfo

케이스별로 다른 데이터를 가지며, 상태 전이가 없는 패턴입니다.

```csharp
[UnionType]
public abstract partial record ContactInfo : UnionValueObject
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
    public sealed record EmailAndPostal(EmailVerificationState EmailState, PostalAddress Address) : ContactInfo;

    private ContactInfo() { }
}
```

**핵심:**
- `abstract partial record` + `[UnionType]` 어트리뷰트
- `: UnionValueObject` (순수 데이터, 상태 전이 없음)
- `private` 생성자로 외부 확장 차단
- `sealed record` 케이스들만 허용
- Source Generator가 `Match`, `Switch`, `IsEmailOnly`, `AsEmailOnly()` 등 자동 생성

### 8-2. 상태 전이 Union: EmailVerificationState

단방향/양방향 상태 전이를 타입 안전하게 강제하는 패턴입니다.

```csharp
[UnionType]
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState>
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;

    private EmailVerificationState() { }

    /// Unverified → Verified 전이. Verified 상태에서는 실패를 반환합니다.
    public Fin<Verified> Verify(DateTime verifiedAt) =>
        TransitionFrom<Unverified, Verified>(
            u => new Verified(u.Email, verifiedAt));
}
```

**핵심:**
- `: UnionValueObject<EmailVerificationState>` (CRTP — 상태 전이 지원)
- `TransitionFrom<TSource, TTarget>(converter)` — 현재 상태가 `TSource`면 변환, 아니면 `Fin.Fail(InvalidTransition)` 반환
- 전이 실패 시 자동으로 `DomainErrorKind.InvalidTransition(FromState, ToState)` 에러 생성

### 8-3. Aggregate에서 Union 사용

```csharp
public sealed class Contact : AggregateRoot<ContactId>
{
    public ContactInfo ContactInfo { get; private set; }

    // 프로젝션 속성: Specification 쿼리를 위해 union 내부 값을 평탄화
    public string? EmailValue { get; private set; }

    public Fin<Contact> UpdateContactInfo(ContactInfo newInfo)
    {
        ContactInfo = newInfo;
        // 프로젝션 속성 동기화
        EmailValue = newInfo.Match(
            email => email.EmailState.Match(u => (string)u.Email, v => (string)v.Email),
            postal => null,
            both => both.EmailState.Match(u => (string)u.Email, v => (string)v.Email));
        AddDomainEvent(new ContactInfoUpdatedEvent(Id, newInfo));
        return this;
    }
}
```

**프로젝션 속성 패턴:**
- Union 내부 값을 Aggregate 루트 레벨로 평탄화
- EF Core/Dapper Specification 쿼리에서 직접 필터링 가능
- Union 변경 시 반드시 동기화

### 8-4. Implementation Checklist

- [ ] `abstract partial record` 선언
- [ ] `[UnionType]` 어트리뷰트 추가
- [ ] 베이스 클래스 선택: `UnionValueObject` (순수 데이터) 또는 `UnionValueObject<TSelf>` (상태 전이)
- [ ] `private` 생성자로 외부 확장 차단
- [ ] 모든 케이스를 `sealed record`로 정의
- [ ] 상태 전이가 있으면 `TransitionFrom<TSource, TTarget>()` 사용
- [ ] Aggregate에서 사용 시 프로젝션 속성 검토 (쿼리 필요 여부)
- [ ] Match/Switch 호출 시 모든 케이스 처리 (exhaustive)

---

## 9. Repository Port — Aggregate 단위 영속화

Domain Layer에 위치하는 Repository 인터페이스(Port)입니다.

### IProductRepository

```csharp
// Exists/Count/DeleteBy는 IRepository에서 상속 — 서브 인터페이스에 선언 불필요
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// 삭제된 상품을 포함하여 ID로 조회
    FinT<IO, Product> GetByIdIncludingDeleted(ProductId id);
}
```

### IRepository\<TAggregate, TId\> 기본 메서드

```csharp
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    // ── Write: Single ──
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // ── Write: Batch ──
    FinT<IO, int> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);

    // ── Read ──
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);

    // ── Specification ──
    FinT<IO, bool> Exists(Specification<TAggregate> spec);
    FinT<IO, int> Count(Specification<TAggregate> spec);
    FinT<IO, int> DeleteBy(Specification<TAggregate> spec);
}
```

### 핵심 포인트

- `AggregateRoot<TId>` 제약으로 Aggregate 단위 영속화를 컴파일 타임에 강제
- `FinT<IO, T>` 반환으로 모나드 합성 지원 (LINQ 쿼리 표현식)
- `IObservablePort` 상속으로 OpenTelemetry 관측 자동 지원
- Batch 쓰기 메서드는 영향 받은 건수(`int`)를 반환 (호출자가 이미 aggregate 보유)
- Specification 메서드는 `PropertyMap` 설정 시 사용 가능
- 도메인별 확장 메서드는 `IProductRepository` 등 서브 인터페이스에 추가

---

## 10. Domain Service 벌크 패턴

여러 Aggregate를 조율하는 벌크 연산은 Domain Service가 소유합니다.

```csharp
public static class ProductBulkOperations
{
    public sealed record BulkDeletedEvent(Seq<ProductId> DeletedIds, string DeletedBy) : DomainEvent;

    public static (Seq<Product> Deleted, BulkDeletedEvent Event) BulkDelete(
        IReadOnlyList<Product> products, string deletedBy)
    {
        foreach (var product in products)
        {
            product.Delete(deletedBy);
            product.ClearDomainEvents();  // 개별 이벤트 정리
        }
        var ids = toSeq(products.Select(p => p.Id));
        return (toSeq(products), new BulkDeletedEvent(ids, deletedBy));
    }
}
```

**핵심 원칙:**
- 각 Aggregate의 상태 변경은 도메인 메서드로 수행 (Delete, Create 등)
- 개별 이벤트는 `ClearDomainEvents()`로 정리
- Domain Service가 단일 벌크 이벤트를 생성
- Use Case에서 `eventCollector.TrackEvent(bulkResult.Event)` 호출

---

## 11. IRepository 벌크 메서드

```csharp
public interface IRepository<TAggregate, TId> : IObservablePort
{
    // ── Write: Single ──
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // ── Write: Batch ──
    FinT<IO, int> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);

    // ── Read ──
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);

    // ── Specification ──
    FinT<IO, bool> Exists(Specification<TAggregate> spec);
    FinT<IO, int> Count(Specification<TAggregate> spec);
    FinT<IO, int> DeleteBy(Specification<TAggregate> spec);
}
```

---

## 12. Application Layer — ApplyT 패턴 + Validator 역할

### 검증 파이프라인의 4가지 역할

| 역할 | 책임 | DDD 근거 |
|------|------|----------|
| `Validate()` | 도메인 지식 컨테이너 (정규화 + 구조적 검증) | Evans: 불변식은 값 객체가 캡슐화 |
| `Create()` | 권위적 팩토리 (Always-Valid 보증) | Khorikov: always-valid는 가장 기본적인 원칙 |
| Handler + ApplyT | 도메인 검증(=VO 생성) + 유스케이스 오케스트레이션 | Khorikov: 핸들러가 VO를 생성하는 것이 THE 검증 |
| Presentation Validator | 선택적 UX 편의 기능 | Microsoft: UI 검증은 UX, 도메인 검증은 정확성 |

### Presentation Validator (선택적 UX 편의)

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // Validate()를 재사용하여 통과/실패만 확인 — 정규화된 결과는 폐기
        RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
        RuleFor(x => x.Description).MustSatisfyValidation(ProductDescription.Validate);
        RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);
        RuleFor(x => x.StockQuantity).MustSatisfyValidation(Quantity.Validate);
    }
}
```

### Handler + ApplyT (도메인 검증의 권위적 지점)

```csharp
public sealed class Usecase(
    IProductRepository productRepository,
    IInventoryRepository inventoryRepository)
    : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
        FinT<IO, Response> usecase =
            from vos in (
                ProductName.Create(request.Name),
                ProductDescription.Create(request.Description),
                Money.Create(request.Price),
                Quantity.Create(request.StockQuantity)
            ).ApplyT((name, desc, price, qty) => (Name: name, Desc: desc, Price: price, Qty: qty))
            let product = Product.Create(vos.Name, vos.Desc, vos.Price)
            from exists in productRepository.Exists(new ProductNameUniqueSpec(vos.Name))
            from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                new AlreadyExists(), request.Name,
                $"Product name already exists: '{request.Name}'"))
            from createdProduct in productRepository.Create(product)
            from createdInventory in inventoryRepository.Create(
                Inventory.Create(createdProduct.Id, vos.Qty))
            select new Response(
                createdProduct.Id.ToString(),
                createdProduct.Name,
                createdProduct.Description,
                createdProduct.Price,
                createdInventory.StockQuantity,
                createdProduct.CreatedAt);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### ApplyT가 필요한 이유

- Presentation Validator가 `Validate()` 호출 → **통과/실패만 확인, 정규화된 데이터는 폐기**
- Handler는 정규화된 VO가 필요 → `Create()` 호출로 **VO 생성 + 정규화 + 도메인 검증**
- **이것은 "재검증"이 아니다** — 핸들러가 VO를 생성하는 것 자체가 도메인 검증이다 (Khorikov)
- ApplyT는 다중 `Create()` 결과를 applicative하게 합성하여 모든 에러를 병렬 수집 + FinT 리프팅

### ApplyT vs Unwrap 선택 기준

| 기준 | Unwrap | ApplyT |
|------|--------|--------|
| VO 개수 | 1~2개 | 3개 이상 |
| 에러 처리 | 첫 에러에서 즉시 반환 | 모든 에러를 병렬 수집 |
| 코드 스타일 | 명령형 (`var x = ...`) | 선언형 (LINQ `from`) |
| 학습 곡선 | 낮음 | 높음 (모나드 트랜스포머) |
| 적합한 상황 | 간단한 Command, 내부 서비스 | 사용자 입력 폼, 복잡한 검증 |

**판단 기준:** VO가 1~2개이고 에러를 병렬 수집할 필요가 없으면 Unwrap이 더 간결합니다.
VO가 3개 이상이거나 사용자에게 모든 검증 오류를 한 번에 보여줘야 하면 ApplyT를 사용합니다.
