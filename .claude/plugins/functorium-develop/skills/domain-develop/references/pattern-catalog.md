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
            .ThenMaxLength(MaxLength)
            .ThenNormalize(v => v.Trim());

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
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format")
            .ThenNormalize(v => v.Trim().ToLowerInvariant());

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
예: `DomainErrors.ProductName.Empty`, `DomainErrors.Email.InvalidFormat`

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
    public sealed record InvalidValue : DomainErrorType.Custom;
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
    public sealed record AlreadyDeleted : DomainErrorType.Custom;
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
    public sealed record EmptyOrderLines : DomainErrorType.Custom;
    public sealed record InvalidOrderStatusTransition : DomainErrorType.Custom;
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
| 중첩 `sealed record : DomainErrorType.Custom` | Aggregate 소속 에러 타입 |
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

### OrderLine

```csharp
[GenerateEntityId]
public sealed class OrderLine : Entity<OrderLineId>
{
    #region Error Types
    public sealed record InvalidQuantity : DomainErrorType.Custom;
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

### 핵심 포인트

- 자식 Entity도 `[GenerateEntityId]`로 ID 자동 생성
- VO의 불변식(`Quantity >= 0`)과 컨텍스트 불변식(`OrderLine은 quantity > 0`) 분리
- 이벤트를 직접 발행하지 않음 (부모 Aggregate Root만 발행)

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
    spec &= new ProductNameSpec(ProductName.Create(request.Name).ThrowIfFail());

if (request.MinPrice > 0 && request.MaxPrice > 0)
    spec &= new ProductPriceRangeSpec(
        Money.Create(request.MinPrice).ThrowIfFail(),
        Money.Create(request.MaxPrice).ThrowIfFail());
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
    public sealed record CreditLimitExceeded : DomainErrorType.Custom;
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

## 8. Repository Port — Aggregate 단위 영속화

Domain Layer에 위치하는 Repository 인터페이스(Port)입니다.

### IProductRepository

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// Specification 기반 존재 여부 확인
    FinT<IO, bool> Exists(Specification<Product> spec);

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
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```

### 핵심 포인트

- `AggregateRoot<TId>` 제약으로 Aggregate 단위 영속화를 컴파일 타임에 강제
- `FinT<IO, T>` 반환으로 모나드 합성 지원 (LINQ 쿼리 표현식)
- `IObservablePort` 상속으로 OpenTelemetry 관측 자동 지원
- 확장 메서드는 도메인별 Repository 인터페이스에 추가

---

## 9. Domain Service 벌크 패턴

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

## 10. IRepository 벌크 메서드

```csharp
public interface IRepository<TAggregate, TId> : IObservablePort
{
    // 단건
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // 벌크
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```
