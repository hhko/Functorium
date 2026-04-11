---
title: "도메인 코드 설계"
---

[비즈니스 요구사항](../00-business-requirements/)에서 자연어로 정의한 규칙을, [타입 설계 의사결정](../01-type-design-decisions/)에서 불변식으로 분류하고 타입 전략을 도출했습니다. 이 문서에서는 그 전략을 C#과 Functorium DDD 빌딩 블록으로 매핑하고, 각 패턴의 구체적인 코드 구현을 살펴봅니다.

## 설계 의사결정 → C# 구현 매핑

다음 표는 설계 의사결정과 구현 패턴의 1:1 매핑입니다. 이후 섹션에서 각 패턴을 코드로 살펴봅니다.

| 설계 의사결정 | Functorium 타입 | 적용 예 | 보장 효과 |
|---|---|---|---|
| 단일 값 검증 + 불변 + 정규화 | `SimpleValueObject<T>` + `Validate` 체인 | CustomerName, Email, ProductName, TagName | 생성 시 검증, Trim/ToLower 정규화, 빈 문자열 차단 |
| 비교 가능한 단일 값 + 산술 연산 | `ComparableSimpleValueObject<T>` | Money, Quantity | 크기 비교(`>`, `<`), 산술(Add, Subtract), 합산(Sum) |
| Smart Enum + 상태 전이 규칙 | `SimpleValueObject<string>` + `HashMap` 전이 맵 | OrderStatus | 허용된 전이만 가능, 잘못된 전이 시 오류 반환 |
| Aggregate Root 이중 팩토리 | `AggregateRoot<TId>` + `Create`/`CreateFromValidated` | Customer, Product, Order, Inventory, Tag | 도메인 생성(검증+이벤트)과 ORM 복원(검증 없음) 분리 |
| 자식 엔티티 + 컬렉션 관리 | `Entity<TId>` + private `List` + `IReadOnlyList` 노출 | OrderLine (Order의 자식) | 외부에서 컬렉션 직접 수정 불가 |
| 교차 Aggregate 비즈니스 규칙 | `IDomainService` | OrderCreditCheckService | Customer와 Order 간 신용 한도 검증 |
| 쿼리 가능한 도메인 사양 | `ExpressionSpecification<T>` | ProductNameUniqueSpec, CustomerEmailSpec | Expression Tree 기반 EF Core SQL 자동 번역 |
| 영속성 추상화 | `IRepository<T, TId>` + 커스텀 메서드 | ICustomerRepository | Specification 기반 존재 여부 확인(Exists) |
| 도메인 이벤트 + 도메인 오류 | 중첩 `sealed record : DomainEvent` / `DomainErrorType.Custom` | Customer.CreatedEvent, Order.EmptyOrderLines | Aggregate 내부에 이벤트/오류 타입 응집 |
| Soft Delete + 가드 | `ISoftDeletableWithUser` + `DeletedAt.IsSome` 가드 | Product.Update() | 삭제된 Aggregate에 대한 변경 차단 |

## 패턴별 코드 스니펫

### 1. SimpleValueObject + Validate 체인

각 값 객체는 `SimpleValueObject<T>`를 상속하고, `Validate` 메서드에서 검증 규칙을 체이닝합니다. private 생성자로 `new`를 차단하고, `Create` 팩토리만 노출합니다.

**CustomerName** -- 문자열 길이 검증 + Trim 정규화:

```csharp
public sealed class CustomerName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private CustomerName(string value) : base(value) { }

    public static Fin<CustomerName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new CustomerName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<CustomerName>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static CustomerName CreateFromValidated(string value) => new(value);

    public static implicit operator string(CustomerName name) => name.Value;
}
```

**Email** -- 정규식 매칭 + ToLowerInvariant 정규화:

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

모든 단일 값 VO가 동일한 4가지 구성 요소를 따릅니다:

| 구성 요소 | 역할 |
|---|---|
| `Create(string?)` → `Fin<T>` | 외부 입력을 검증하여 VO 생성, 실패 시 `Fin.Fail` |
| `Validate(string?)` → `Validation<Error, string>` | Application Layer에서 applicative 합성 시 사용 |
| `CreateFromValidated(string)` | ORM 복원용, 이미 검증된 값을 직접 생성 |
| `implicit operator` | VO → 원시 타입 변환 지원 |

이 4가지 구성 요소의 핵심은 **생성 경로의 분리입니다.** 외부 입력은 반드시 `Create`를 통해 검증을 거치고, ORM 복원은 `CreateFromValidated`로 검증을 생략합니다. 두 경로 모두 private 생성자로 보호되어, 검증을 우회한 인스턴스 생성이 불가능합니다.

### 2. ComparableSimpleValueObject + 산술 연산

`ComparableSimpleValueObject<T>`는 크기 비교 연산(`>`, `<`, `>=`, `<=`)을 지원하며, 도메인 산술 연산을 캡슐화합니다.

**Money** -- 양수만 허용, 합산의 항등원(Zero) 제공:

```csharp
public sealed class Money : ComparableSimpleValueObject<decimal>
{
    /// <summary>
    /// 합산 연산의 항등원 (Identity element for addition)
    /// </summary>
    public static readonly Money Zero = new(0m);

    private Money(decimal value) : base(value) { }

    public static Fin<Money> Create(decimal value) =>
        CreateFromValidation(Validate(value), v => new Money(v));

    public static Validation<Error, decimal> Validate(decimal value) =>
        ValidationRules<Money>
            .Positive(value);

    public static Money CreateFromValidated(decimal value) => new(value);

    public static implicit operator decimal(Money money) => money.Value;

    public Money Add(Money other) => new(Value + other.Value);
    public Fin<Money> Subtract(Money other) => Create(Value - other.Value);
    public Money Multiply(decimal factor) => new(Value * factor);

    public static Money Sum(IEnumerable<Money> values) =>
        values.Aggregate(Zero, (acc, m) => acc.Add(m));
}
```

`Add`는 항상 성공(`Money` 반환)하지만, `Subtract`는 결과가 음수일 수 있으므로 `Fin<Money>`를 반환합니다. `Sum`은 `Zero` 항등원과 `Aggregate`로 LINQ 합산을 지원합니다.

**Quantity** -- 0 이상, clamp 방식 뺄셈:

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

`Subtract`는 `Math.Max(0, ...)`로 결과를 0 이상으로 clamp합니다. Money와 달리 오류 대신 하한 제한을 적용하는데, 재고 차감 시 음수가 되면 Aggregate(`Inventory`)에서 `InsufficientStock` 오류로 처리하기 때문입니다.

Money와 Quantity의 설계 차이는 도메인 의미의 차이를 반영합니다. Money의 `Subtract`가 `Fin<Money>`를 반환하는 이유는 금액 뺄셈의 결과가 음수일 수 있고, 이는 도메인에서 유효하지 않은 상태이기 때문입니다. 반면 Quantity의 `Subtract`는 0으로 clamp하고, 실제 재고 부족 검증은 Aggregate 수준에서 수행합니다.

단일 값의 유효성과 산술 연산을 보장했다면, 다음으로 열거형 값과 상태 전이 규칙을 타입으로 표현해야 합니다.

### 3. Smart Enum -- OrderStatus + 전이 규칙

`OrderStatus`는 `SimpleValueObject<string>`을 활용한 Smart Enum 패턴입니다. `HashMap`으로 허용된 전이 규칙을 선언하고, `CanTransitionTo`/`TransitionTo`로 상태 전이를 제어합니다.

```csharp
public sealed class OrderStatus : SimpleValueObject<string>
{
    #region Error Types

    public sealed record InvalidValue : DomainErrorType.Custom;

    #endregion

    public static readonly OrderStatus Pending = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    private static readonly HashMap<string, OrderStatus> All = HashMap(
        ("Pending", Pending),
        ("Confirmed", Confirmed),
        ("Shipped", Shipped),
        ("Delivered", Delivered),
        ("Cancelled", Cancelled));

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

전이 규칙 요약:

| 현재 상태 | 허용 전이 대상 |
|---|---|
| Pending | Confirmed, Cancelled |
| Confirmed | Shipped, Cancelled |
| Shipped | Delivered |
| Delivered | (터미널 상태) |
| Cancelled | (터미널 상태) |

`AllowedTransitions`에 없는 상태(Delivered, Cancelled)는 `CanTransitionTo`가 항상 `false`를 반환하여 터미널 상태로 동작합니다.

값 객체의 생성과 검증 패턴을 정의했다면, 이제 이 값 객체들을 조합하는 Aggregate Root의 생성 전략을 살펴봅니다.

### 4. AggregateRoot 이중 팩토리 + 가드

모든 Aggregate Root는 이중 팩토리 패턴을 사용합니다:

| 팩토리 | 용도 | 검증 | 이벤트 |
|---|---|---|---|
| `Create(VO...)` | 도메인 생성 | 이미 검증된 VO 수신 | 도메인 이벤트 발행 |
| `CreateFromValidated(id, VO..., audit...)` | ORM/Repository 복원 | 없음 (DB 데이터 신뢰) | 없음 |

**Customer.Create()** -- VO를 받아 생성 + 이벤트 발행:

```csharp
public static Customer Create(
    CustomerName name,
    Email email,
    Money creditLimit)
{
    var customer = new Customer(CustomerId.New(), name, email, creditLimit);
    customer.AddDomainEvent(new CreatedEvent(customer.Id, name, email));
    return customer;
}
```

**Customer.CreateFromValidated()** -- ORM 복원용:

```csharp
public static Customer CreateFromValidated(
    CustomerId id,
    CustomerName name,
    Email email,
    Money creditLimit,
    DateTime createdAt,
    Option<DateTime> updatedAt)
{
    return new Customer(id, name, email, creditLimit)
    {
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

**Product.Update()** -- Soft Delete 가드:

삭제된 Aggregate에 대한 변경을 차단하는 가드 패턴입니다. `DeletedAt.IsSome`이면 `AlreadyDeleted` 오류를 반환합니다.

```csharp
public Fin<Product> Update(
    ProductName name,
    ProductDescription description,
    Money price)
{
    if (DeletedAt.IsSome)
        return DomainError.For<Product>(
            new AlreadyDeleted(),
            Id.ToString(),
            "Cannot update a deleted product");

    var oldPrice = Price;

    Name = name;
    Description = description;
    Price = price;
    UpdatedAt = DateTime.UtcNow;

    AddDomainEvent(new UpdatedEvent(Id, name, oldPrice, price));

    return this;
}
```

**Product.Delete()** -- 멱등 삭제:

이미 삭제된 상태에서 다시 Delete()를 호출해도 오류 없이 `this`를 반환합니다.

```csharp
public Product Delete(string deletedBy)
{
    if (DeletedAt.IsSome)
        return this;

    DeletedAt = DateTime.UtcNow;
    DeletedBy = deletedBy;
    AddDomainEvent(new DeletedEvent(Id, deletedBy));
    return this;
}
```

### 5. Entity 자식 엔티티 (OrderLine)

`OrderLine`은 `Entity<OrderLineId>`를 상속하는 자식 엔티티로, Order Aggregate 경계 내에서만 존재합니다.

**OrderLine.Create()** -- Quantity > 0 검증 + LineTotal 자동 계산:

```csharp
public static Fin<OrderLine> Create(ProductId productId, Quantity quantity, Money unitPrice)
{
    if ((int)quantity <= 0)
        return DomainError.For<OrderLine, int>(
            new InvalidQuantity(),
            currentValue: quantity,
            message: "Order line quantity must be greater than 0");

    var lineTotal = unitPrice.Multiply(quantity);
    return new OrderLine(OrderLineId.New(), productId, quantity, unitPrice, lineTotal);
}
```

`Quantity` VO는 0 이상을 허용하지만, 주문 라인 컨텍스트에서는 0 수량이 무의미하므로 Entity 수준에서 추가 검증합니다. `LineTotal`은 `Money.Multiply`로 자동 계산되어 일관성을 보장합니다.

**Order.Create()** -- 빈 주문 라인 차단 + TotalAmount 자동 계산:

```csharp
public static Fin<Order> Create(
    CustomerId customerId,
    IEnumerable<OrderLine> orderLines,
    ShippingAddress shippingAddress)
{
    var lines = orderLines.ToList();
    if (lines.Count == 0)
        return DomainError.For<Order, int>(
            new EmptyOrderLines(),
            currentValue: 0,
            message: "Order must contain at least one order line");

    var totalAmount = Money.CreateFromValidated(lines.Sum(l => (decimal)l.LineTotal));
    var order = new Order(OrderId.New(), customerId, lines, totalAmount, shippingAddress);

    var lineInfos = Seq(lines.Select(l => new OrderLineInfo(l.ProductId, l.Quantity, l.UnitPrice, l.LineTotal)));
    order.AddDomainEvent(new CreatedEvent(order.Id, customerId, lineInfos, totalAmount));
    return order;
}
```

Order는 private `List<OrderLine>`으로 자식 엔티티를 관리하고, 외부에는 `IReadOnlyList`만 노출합니다:

```csharp
private readonly List<OrderLine> _orderLines = [];
public IReadOnlyList<OrderLine> OrderLines => _orderLines.AsReadOnly();
```

Aggregate 내부의 소유 관계를 정의했다면, 이제 Aggregate 간 교차 검증을 위한 쿼리 패턴을 살펴봅니다.

### 6. ExpressionSpecification

`ExpressionSpecification<T>`는 `Expression<Func<T, bool>>`을 반환하여 EF Core에서 SQL로 자동 번역됩니다.

**ProductNameUniqueSpec** -- 자기 자신 제외 옵션:

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

`Option<ProductId> ExcludeId`로 업데이트 시 자기 자신을 제외한 중복 검사를 지원합니다. 생성 시에는 `default`(None)를 사용하고, 업데이트 시에는 현재 상품 ID를 전달합니다.

**CustomerEmailSpec** -- 이메일 일치 검색:

```csharp
public sealed class CustomerEmailSpec : ExpressionSpecification<Customer>
{
    public Email Email { get; }

    public CustomerEmailSpec(Email email)
    {
        Email = email;
    }

    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string emailStr = Email;
        return customer => (string)customer.Email == emailStr;
    }
}
```

VO의 `implicit operator`를 활용하여 Expression Tree 내에서 문자열 비교로 변환합니다. 이 변환은 Expression 변수 캡처(`string emailStr = Email`)를 통해 이루어지며, EF Core가 SQL `WHERE` 절로 번역할 수 있습니다.

### 7. IDomainService

`OrderCreditCheckService`는 Customer와 Order 간의 교차 Aggregate 비즈니스 규칙을 구현합니다. 상태 없는 서비스로, Application Layer가 필요한 데이터를 조회한 뒤 최소 데이터만 전달합니다.

```csharp
public sealed class OrderCreditCheckService : IDomainService
{
    #region Error Types

    public sealed record CreditLimitExceeded : DomainErrorType.Custom;

    #endregion

    /// <summary>
    /// 주문 금액이 고객의 신용 한도 내에 있는지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateCreditLimit(Customer customer, Money orderAmount)
    {
        if (orderAmount > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"Order amount {(decimal)orderAmount} exceeds customer credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }

    /// <summary>
    /// 기존 주문들과 신규 주문을 합산하여 신용 한도 내에 있는지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateCreditLimitWithExistingOrders(
        Customer customer,
        Seq<Order> existingOrders,
        Money newOrderAmount)
    {
        var totalExisting = Money.Sum(existingOrders.Map(o => o.TotalAmount));
        var totalWithNew = totalExisting.Add(newOrderAmount);

        if (totalWithNew > customer.CreditLimit)
            return DomainError.For<OrderCreditCheckService>(
                new CreditLimitExceeded(),
                customer.Id.ToString(),
                $"Total order amount {(decimal)totalWithNew} (existing: {(decimal)totalExisting} + new: {(decimal)newOrderAmount}) exceeds customer credit limit {(decimal)customer.CreditLimit}");

        return unit;
    }
}
```

`ValidateCreditLimitWithExistingOrders`는 `Money.Sum`으로 기존 주문 합계를 계산하고, `Money.Add`로 신규 주문을 합산합니다. `ComparableSimpleValueObject`의 비교 연산(`>`)으로 한도 초과를 판별합니다. 오류 타입 `CreditLimitExceeded`는 서비스 내부에 중첩 정의되어 오류 출처를 명확히 합니다.

Domain Service의 핵심은 **상태 없는 순수 로직이라는** 점입니다. Application Layer가 필요한 데이터를 모두 조회하여 전달하면, Domain Service는 오직 비즈니스 규칙만 실행합니다. 이 분리 덕분에 Domain Service는 Mock 없이 순수 단위 테스트가 가능합니다.

### 8. IRepository 포트

`IRepository<T, TId>` 기본 CRUD에 커스텀 메서드를 추가하여 Specification 기반 조회를 지원합니다.

```csharp
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    /// <summary>
    /// Specification 기반 존재 여부 확인.
    /// </summary>
    FinT<IO, bool> Exists(Specification<Customer> spec);
}
```

`FinT<IO, bool>`는 IO 효과와 실패 가능성을 타입으로 표현합니다. Application Layer에서 `CustomerEmailSpec`을 전달하면 Repository 구현체가 EF Core를 통해 SQL로 변환합니다.

### 9. DomainEvent + DomainError 중첩 레코드

도메인 이벤트와 오류 타입을 Aggregate 내부에 `sealed record`로 중첩 정의합니다. 이벤트/오류가 어떤 Aggregate에서 발생하는지 네임스페이스 수준에서 명확해집니다.

**Customer의 도메인 이벤트:**

```csharp
public sealed class Customer : AggregateRoot<CustomerId>, IAuditable
{
    #region Domain Events

    public sealed record CreatedEvent(
        CustomerId CustomerId,
        CustomerName Name,
        Email Email) : DomainEvent;

    public sealed record CreditLimitUpdatedEvent(
        CustomerId CustomerId,
        Money OldCreditLimit,
        Money NewCreditLimit) : DomainEvent;

    public sealed record EmailChangedEvent(
        CustomerId CustomerId,
        Email OldEmail,
        Email NewEmail) : DomainEvent;

    #endregion
    // ...
}
```

**Order의 도메인 오류:**

```csharp
public sealed class Order : AggregateRoot<OrderId>, IAuditable
{
    #region Error Types

    public sealed record EmptyOrderLines : DomainErrorType.Custom;
    public sealed record InvalidOrderStatusTransition : DomainErrorType.Custom;

    #endregion
    // ...
}
```

이벤트는 변경 전/후 값을 포함하여(예: `OldPrice`, `NewPrice`) 이벤트 소비자가 변경 내용을 파악할 수 있게 합니다. 오류 타입은 `DomainErrorType.Custom`을 상속하며, `DomainError.For<T>()`로 오류를 생성할 때 Aggregate 타입 정보가 자동으로 포함됩니다.

## Failable vs Idempotent 반환 타입

도메인 메서드의 반환 타입은 실패 가능성에 따라 결정됩니다. `Fin<T>`는 실패할 수 있는 연산에, 자기 자신 타입(T)은 항상 성공하는 연산에 사용합니다. 반환 타입만으로 호출자가 오류 처리 필요 여부를 판단할 수 있습니다.

| 메서드 | 반환 타입 | 분류 | 이유 |
|---|---|---|---|
| `Customer.Create()` | `Customer` | 항상 성공 | 이미 검증된 VO만 수신 |
| `Customer.UpdateCreditLimit()` | `Customer` | 멱등 | 항상 성공, fluent chaining |
| `Customer.ChangeEmail()` | `Customer` | 멱등 | 항상 성공, fluent chaining |
| `Product.Create()` | `Product` | 항상 성공 | 이미 검증된 VO만 수신 |
| `Product.Update()` | `Fin<Product>` | 실패 가능 | 삭제된 상품 변경 시 `AlreadyDeleted` |
| `Product.Delete()` | `Product` | 멱등 | 이미 삭제된 상태에서 재호출 허용 |
| `Product.Restore()` | `Product` | 멱등 | 이미 복원된 상태에서 재호출 허용 |
| `Product.AssignTag()` | `Product` | 멱등 | 이미 할당된 태그 재할당 허용 |
| `Product.UnassignTag()` | `Product` | 멱등 | 이미 해제된 태그 재해제 허용 |
| `Order.Create()` | `Fin<Order>` | 실패 가능 | 빈 주문 라인 시 `EmptyOrderLines` |
| `Order.Confirm()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 시 `InvalidOrderStatusTransition` |
| `Order.Ship()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 시 `InvalidOrderStatusTransition` |
| `Order.Deliver()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 시 `InvalidOrderStatusTransition` |
| `Order.Cancel()` | `Fin<Unit>` | 실패 가능 | 잘못된 상태 전이 시 `InvalidOrderStatusTransition` |
| `OrderLine.Create()` | `Fin<OrderLine>` | 실패 가능 | 수량 0 이하 시 `InvalidQuantity` |
| `Inventory.DeductStock()` | `Fin<Unit>` | 실패 가능 | 재고 부족 시 `InsufficientStock` |
| `Inventory.AddStock()` | `Inventory` | 항상 성공 | 재고 추가는 항상 유효 |
| `Tag.Create()` | `Tag` | 항상 성공 | 이미 검증된 VO만 수신 |
| `Tag.Rename()` | `Tag` | 멱등 | 항상 성공, fluent chaining |

설계 원칙: **실패할 수 있는 메서드는 `Fin<T>`로,** 항상 성공하거나 멱등한 메서드는 자기 자신 타입을 반환합니다. 반환 타입만 보고 호출자가 오류 처리 필요 여부를 판단할 수 있습니다.

## Naive → 최종 타입 추적표

| Naive 필드 | 단일 값 VO | 소속 Aggregate/Entity | 최종 위치 |
|---|---|---|---|
| `string Name` (고객) | CustomerName | Customer | `Customer.Name` |
| `string Email` (고객) | Email | Customer | `Customer.Email` |
| `decimal CreditLimit` | Money | Customer | `Customer.CreditLimit` |
| `string Name` (상품) | ProductName | Product | `Product.Name` |
| `string Description` | ProductDescription | Product | `Product.Description` |
| `decimal Price` | Money | Product | `Product.Price` |
| `string Name` (태그) | TagName | Tag | `Tag.Name` |
| `List<string> TagIds` | `List<TagId>` | Product | `Product.TagIds` |
| `string Status` | OrderStatus (Smart Enum) | Order | `Order.Status` |
| `string ShippingAddress` | ShippingAddress (복합 VO) | Order | `Order.ShippingAddress` |
| `decimal TotalAmount` | Money (자동 계산) | Order | `Order.TotalAmount` |
| `string ProductId` (주문 라인) | ProductId (교차 참조) | OrderLine | `OrderLine.ProductId` |
| `int Quantity` | Quantity | OrderLine, Inventory | `OrderLine.Quantity`, `Inventory.StockQuantity` |
| `decimal UnitPrice` | Money | OrderLine | `OrderLine.UnitPrice` |
| `decimal LineTotal` | Money (자동 계산) | OrderLine | `OrderLine.LineTotal` |
| `int StockQuantity` | Quantity | Inventory | `Inventory.StockQuantity` |
| `string CustomerId` (주문) | CustomerId (교차 참조) | Order | `Order.CustomerId` |
| `string ProductId` (재고) | ProductId (교차 참조) | Inventory | `Inventory.ProductId` |

이 표는 원시 타입이 어떻게 도메인 의미를 가진 타입으로 변환되었는지 추적합니다. `string`이 `CustomerName`, `Email`, `ProductName` 등으로 분화되면서, 컴파일러가 서로 다른 문자열을 혼동하는 것을 방지합니다. `decimal`은 `Money`로, `int`는 `Quantity`로 감싸져 음수 금액이나 잘못된 수량이 존재할 수 없게 됩니다.

원시 타입(`string`, `decimal`, `int`)이 모두 도메인 의미를 가진 Value Object로 변환되었습니다. 교차 Aggregate 참조는 ID 값 객체(`CustomerId`, `ProductId`, `TagId`)로 표현하여 Aggregate 경계를 유지합니다. 자동 계산 필드(`TotalAmount`, `LineTotal`)는 생성 시점에 VO 산술 연산으로 계산되어 일관성을 보장합니다.

[구현 결과](./03-implementation-results/)에서 이 타입 구조가 비즈니스 시나리오를 어떻게 보장하는지 확인합니다.
