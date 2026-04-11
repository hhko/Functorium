---
title: "도메인 구현 결과"
---

[비즈니스 요구사항](../00-business-requirements/)에서 정의한 시나리오를, [코드 설계](../02-code-design/)에서 구현한 타입 구조로 실행하여 증명합니다. 정상 시나리오(1~7)는 타입이 올바른 상태를 어떻게 표현하는지, 거부 시나리오(8~12)는 잘못된 상태를 어떻게 차단하는지 확인합니다.

## 정상 시나리오 검증

### 시나리오 1: 고객 생성

**비즈니스 규칙:** 고객은 이름, 이메일, 신용한도를 가지며, 생성 시 `CreatedEvent`가 발행됩니다.

```csharp
[Fact]
public void Create_ShouldPublishCreatedEvent()
{
    // Arrange
    var name = CustomerName.Create("John").ThrowIfFail();
    var email = Email.Create("john@example.com").ThrowIfFail();
    var creditLimit = Money.Create(5000m).ThrowIfFail();

    // Act
    var sut = Customer.Create(name, email, creditLimit);

    // Assert
    sut.Id.ShouldNotBe(default);
    sut.DomainEvents.ShouldContain(e => e is Customer.CreatedEvent);
}

[Fact]
public void Create_ShouldSetProperties()
{
    // Arrange
    var name = CustomerName.Create("John").ThrowIfFail();
    var email = Email.Create("john@example.com").ThrowIfFail();
    var creditLimit = Money.Create(5000m).ThrowIfFail();

    // Act
    var sut = Customer.Create(name, email, creditLimit);

    // Assert
    ((string)sut.Name).ShouldBe("John");
    ((string)sut.Email).ShouldBe("john@example.com");
    ((decimal)sut.CreditLimit).ShouldBe(5000m);
}
```

`CustomerName`, `Email`, `Money`는 각각 검증된 Value Object입니다. `Create`는 이미 검증된 VO를 받아 Aggregate를 구성하고, `CreatedEvent`를 발행합니다. ID는 Ulid 기반으로 자동 생성됩니다.

### 시나리오 2: 상품 생성

**비즈니스 규칙:** 상품은 이름, 설명, 가격을 가지며, 생성 시 `CreatedEvent`가 발행됩니다.

```csharp
[Fact]
public void Create_ShouldPublishCreatedEvent()
{
    // Act
    var sut = Product.Create(
        ProductName.Create("Test Product").ThrowIfFail(),
        ProductDescription.Create("Test Description").ThrowIfFail(),
        Money.Create(100m).ThrowIfFail());

    // Assert
    sut.Id.ShouldNotBe(default);
    sut.DomainEvents.ShouldContain(e => e is Product.CreatedEvent);
}

[Fact]
public void Create_ShouldSetProperties()
{
    // Act
    var sut = Product.Create(
        ProductName.Create("Laptop").ThrowIfFail(),
        ProductDescription.Create("Good laptop").ThrowIfFail(),
        Money.Create(1500m).ThrowIfFail());

    // Assert
    ((string)sut.Name).ShouldBe("Laptop");
    ((string)sut.Description).ShouldBe("Good laptop");
    ((decimal)sut.Price).ShouldBe(1500m);
}
```

`Product.Create`는 세 가지 VO(`ProductName`, `ProductDescription`, `Money`)를 받아 Aggregate를 구성합니다. 상품은 `ISoftDeletableWithUser`를 구현하여 논리 삭제를 지원합니다.

### 시나리오 3: 주문 생성

**비즈니스 규칙:** 주문은 최소 1개 이상의 주문 라인을 포함해야 하며, `TotalAmount`는 모든 라인의 합계로 자동 계산됩니다.

```csharp
[Fact]
public void Create_ShouldCalculateTotalAmount()
{
    // Arrange
    var line1 = OrderLine.Create(
        ProductId.New(),
        Quantity.Create(3).ThrowIfFail(),
        Money.Create(100m).ThrowIfFail()).ThrowIfFail();
    var line2 = OrderLine.Create(
        ProductId.New(),
        Quantity.Create(2).ThrowIfFail(),
        Money.Create(50m).ThrowIfFail()).ThrowIfFail();

    // Act
    var sut = Order.Create(
        CustomerId.New(),
        [line1, line2],
        ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

    // Assert
    ((decimal)sut.TotalAmount).ShouldBe(400m); // (3 * 100) + (2 * 50) = 400
}

[Fact]
public void Create_ShouldPublishCreatedEvent()
{
    // Arrange
    var customerId = CustomerId.New();
    var line = OrderLine.Create(
        ProductId.New(),
        Quantity.Create(2).ThrowIfFail(),
        Money.Create(50m).ThrowIfFail()).ThrowIfFail();

    // Act
    var sut = Order.Create(customerId, [line],
        ShippingAddress.Create("Seoul, Korea").ThrowIfFail()).ThrowIfFail();

    // Assert
    sut.Id.ShouldNotBe(default);
    var createdEvent = sut.DomainEvents.OfType<Order.CreatedEvent>().ShouldHaveSingleItem();
    createdEvent.CustomerId.ShouldBe(customerId);
    createdEvent.OrderLines.Count.ShouldBe(1);
}
```

`Order.Create`는 `Fin<Order>`를 반환합니다. `TotalAmount`는 개별 `OrderLine.LineTotal`(`Quantity * UnitPrice`)의 합계로 자동 계산되며, 생성 즉시 `Pending` 상태가 됩니다. `CustomerId`는 교차 Aggregate 참조로, Customer Aggregate의 ID를 값으로 보유합니다.

### 시나리오 4: 주문 상태 전이 체인

**비즈니스 규칙:** 주문 상태는 `Pending -> Confirmed -> Shipped -> Delivered` 순서로 전이되며, 각 전이마다 도메인 이벤트가 발행됩니다.

```csharp
[Fact]
public void Confirm_ShouldTransitionFromPending()
{
    // Arrange
    var sut = CreateSampleOrder(); // Status = Pending

    // Act
    var actual = sut.Confirm();

    // Assert
    actual.IsSucc.ShouldBeTrue();
    sut.Status.ShouldBe(OrderStatus.Confirmed);
}

[Fact]
public void Ship_ShouldTransitionFromConfirmed()
{
    // Arrange
    var sut = CreateSampleOrder();
    sut.Confirm(); // Pending → Confirmed

    // Act
    var actual = sut.Ship();

    // Assert
    actual.IsSucc.ShouldBeTrue();
    sut.Status.ShouldBe(OrderStatus.Shipped);
}

[Fact]
public void Confirm_ShouldPublishConfirmedEvent()
{
    // Arrange
    var sut = CreateSampleOrder();
    sut.ClearDomainEvents();

    // Act
    sut.Confirm();

    // Assert
    sut.DomainEvents.OfType<Order.ConfirmedEvent>().ShouldHaveSingleItem();
}
```

`OrderStatus`는 허용된 전이 규칙을 `CanTransitionTo`로 정의합니다. `Confirm()`, `Ship()`, `Deliver()`는 내부에서 `TransitionTo`를 호출하여 현재 상태에서 대상 상태로의 전이 가능 여부를 검증한 뒤, 상태를 변경하고 이벤트를 발행합니다.

```csharp
[Theory]
[InlineData("Pending", "Confirmed", true)]
[InlineData("Pending", "Cancelled", true)]
[InlineData("Confirmed", "Shipped", true)]
[InlineData("Confirmed", "Cancelled", true)]
[InlineData("Shipped", "Delivered", true)]
[InlineData("Pending", "Shipped", false)]
[InlineData("Confirmed", "Pending", false)]
[InlineData("Shipped", "Cancelled", false)]
[InlineData("Delivered", "Cancelled", false)]
public void CanTransitionTo_ShouldReturnExpected(string from, string to, bool expected)
{
    var fromStatus = OrderStatus.CreateFromValidated(from);
    var toStatus = OrderStatus.CreateFromValidated(to);

    var actual = fromStatus.CanTransitionTo(toStatus);

    actual.ShouldBe(expected);
}
```

상태 전이 규칙은 `OrderStatus` Value Object 내부에 캡슐화되어, Aggregate가 유효하지 않은 상태로 전이하는 것을 구조적으로 방지합니다.

### 시나리오 5: 상품 논리 삭제 + 복원

**비즈니스 규칙:** 상품을 논리 삭제하면 삭제자와 시점이 기록됩니다. 복원하면 삭제 정보가 초기화됩니다. 삭제와 복원 모두 멱등합니다.

```csharp
[Fact]
public void Delete_ShouldSetDeletedAtAndDeletedBy()
{
    // Arrange
    var sut = CreateSampleProduct();

    // Act
    sut.Delete("admin@test.com");

    // Assert
    sut.DeletedAt.IsSome.ShouldBeTrue();
    sut.DeletedBy.ShouldBe(Some("admin@test.com"));
}

[Fact]
public void Delete_ShouldBeIdempotent_WhenAlreadyDeleted()
{
    // Arrange
    var sut = CreateSampleProduct();
    sut.Delete("admin@test.com");
    sut.ClearDomainEvents();

    // Act
    sut.Delete("other@test.com"); // 두 번째 삭제 시도

    // Assert
    sut.DomainEvents.ShouldBeEmpty();              // 이벤트 추가 없음
    sut.DeletedBy.ShouldBe(Some("admin@test.com")); // 최초 삭제자 유지
}

[Fact]
public void Restore_ShouldClearDeletedAtAndDeletedBy()
{
    // Arrange
    var sut = CreateSampleProduct();
    sut.Delete("admin@test.com");

    // Act
    sut.Restore();

    // Assert
    sut.DeletedAt.IsNone.ShouldBeTrue();
    sut.DeletedBy.IsNone.ShouldBeTrue();
}

[Fact]
public void Restore_ShouldBeIdempotent_WhenNotDeleted()
{
    // Arrange
    var sut = CreateSampleProduct();
    sut.ClearDomainEvents();

    // Act
    sut.Restore(); // 삭제되지 않은 상태에서 복원 시도

    // Assert
    sut.DomainEvents.ShouldBeEmpty(); // 이벤트 추가 없음
}
```

`DeletedAt`과 `DeletedBy`는 `Option<T>`로 표현됩니다. 삭제 시 `Some(value)`, 복원 시 `None`으로 전환됩니다. 이미 삭제(복원)된 상태에서 다시 삭제(복원)해도 이벤트가 추가되지 않아, 멱등성이 보장됩니다.

### 시나리오 6: 재고 차감/추가

**비즈니스 규칙:** 재고는 차감과 추가가 가능하며, 각 작업마다 도메인 이벤트가 발행됩니다.

```csharp
[Fact]
public void DeductStock_ShouldSucceed_WhenSufficientStock()
{
    // Arrange
    var sut = CreateSampleInventory(10);
    sut.ClearDomainEvents();

    // Act
    var result = sut.DeductStock(Quantity.Create(3).ThrowIfFail());

    // Assert
    result.IsSucc.ShouldBeTrue();
    ((int)sut.StockQuantity).ShouldBe(7);
    sut.DomainEvents.ShouldContain(e => e is Inventory.StockDeductedEvent);
}

[Fact]
public void AddStock_ShouldIncreaseQuantity()
{
    // Arrange
    var sut = CreateSampleInventory(10);
    sut.ClearDomainEvents();

    // Act
    sut.AddStock(Quantity.Create(5).ThrowIfFail());

    // Assert
    ((int)sut.StockQuantity).ShouldBe(15);
    sut.DomainEvents.ShouldContain(e => e is Inventory.StockAddedEvent);
}
```

`Inventory`는 `Product`에서 분리된 재고 관리 전용 Aggregate입니다. 고빈도 변경(주문마다 `DeductStock`)에 대한 낙관적 동시성 제어(`IConcurrencyAware`)를 지원합니다. `DeductStock`은 `Fin<Unit>`을 반환하여 재고 부족 시 실패를 명시적으로 표현합니다.

### 시나리오 7: 신용한도 검증 (통과)

**비즈니스 규칙:** 주문 금액이 고객의 신용 한도 이내이면 주문이 허용됩니다. 기존 주문 합산도 고려합니다.

```csharp
[Fact]
public void ValidateCreditLimit_ReturnsSuccess_WhenAmountWithinLimit()
{
    // Arrange
    var customer = CreateSampleCustomer(creditLimit: 5000m);
    var orderAmount = Money.Create(3000m).ThrowIfFail();

    // Act
    var actual = _sut.ValidateCreditLimit(customer, orderAmount);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}

[Fact]
public void ValidateCreditLimit_ReturnsSuccess_WhenAmountEqualsLimit()
{
    // Arrange
    var customer = CreateSampleCustomer(creditLimit: 5000m);
    var orderAmount = Money.Create(5000m).ThrowIfFail();

    // Act
    var actual = _sut.ValidateCreditLimit(customer, orderAmount);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}

[Fact]
public void ValidateCreditLimitWithExistingOrders_ReturnsSuccess_WhenTotalWithinLimit()
{
    // Arrange
    var customer = CreateSampleCustomer(creditLimit: 5000m);
    var existingOrders = Seq(
        CreateSampleOrder(unitPrice: 1000m),
        CreateSampleOrder(unitPrice: 1500m));
    var newOrderAmount = Money.Create(2000m).ThrowIfFail();

    // Act
    var actual = _sut.ValidateCreditLimitWithExistingOrders(customer, existingOrders, newOrderAmount);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

`OrderCreditCheckService`는 `IDomainService`를 구현하는 교차 Aggregate 검증 서비스입니다. `Customer`의 신용한도와 `Order`의 금액을 비교하여, 단일 Aggregate에서는 수행할 수 없는 비즈니스 규칙을 검증합니다. `ValidateCreditLimitWithExistingOrders`는 기존 주문 합계를 포함한 누적 금액을 검증합니다.

정상 시나리오에서 올바른 상태가 어떻게 표현되는지 확인했습니다. 이제 거부 시나리오에서 잘못된 상태가 어떻게 차단되는지 검증합니다. 도메인 타입 시스템의 진정한 가치는 '허용된 상태를 표현하는 것'보다 '허용되지 않은 상태를 구조적으로 불가능하게 만드는 것'에 있습니다.

## 거부 시나리오 검증

### 시나리오 8: 빈 주문라인

**비즈니스 규칙:** 주문 라인이 없는 주문은 생성할 수 없습니다.

```csharp
[Fact]
public void Create_ShouldFail_WhenOrderLinesEmpty()
{
    // Act
    var actual = Order.Create(
        CustomerId.New(),
        [],
        ShippingAddress.Create("Seoul, Korea").ThrowIfFail());

    // Assert
    actual.IsFail.ShouldBeTrue();
}
```

`Order.Create`는 주문 라인 목록이 비어 있으면 `EmptyOrderLines` 에러를 포함한 `Fin.Fail`을 반환합니다. `Fin<Order>` 반환 타입이 호출자에게 실패 처리를 강제하므로, 빈 주문이 생성되는 것을 구조적으로 방지합니다.

### 시나리오 9: 잘못된 상태 전이

**비즈니스 규칙:** 허용되지 않은 상태 전이는 거부됩니다. (예: `Pending -> Shipped`, `Delivered -> Cancelled`)

```csharp
[Fact]
public void Ship_ShouldFail_WhenPending()
{
    // Arrange
    var sut = CreateSampleOrder(); // Status = Pending

    // Act
    var actual = sut.Ship(); // Pending → Shipped 시도

    // Assert
    actual.IsFail.ShouldBeTrue();
}

[Fact]
public void Deliver_ShouldFail_WhenCancelled()
{
    // Arrange
    var sut = CreateSampleOrder();
    sut.Cancel(); // Pending → Cancelled

    // Act
    var actual = sut.Deliver(); // Cancelled → Delivered 시도

    // Assert
    actual.IsFail.ShouldBeTrue();
}

[Fact]
public void Cancel_ShouldFail_WhenDelivered()
{
    // Arrange
    var sut = CreateSampleOrder();
    sut.Confirm();
    sut.Ship();
    sut.Deliver(); // Shipped → Delivered

    // Act
    var actual = sut.Cancel(); // Delivered → Cancelled 시도

    // Assert
    actual.IsFail.ShouldBeTrue();
}
```

`TransitionTo` 내부에서 `OrderStatus.CanTransitionTo`를 호출하여 허용되지 않은 전이를 감지하면, `InvalidOrderStatusTransition` 에러를 반환합니다. 배송(`Ship`)은 반드시 확인(`Confirmed`) 이후에만, 배달 완료(`Deliver`)는 반드시 배송(`Shipped`) 이후에만 가능합니다.

### 시나리오 10: 삭제된 상품 수정

**비즈니스 규칙:** 논리 삭제된 상품은 수정할 수 없습니다. 복원 후에는 수정이 가능합니다.

```csharp
[Fact]
public void Update_ReturnsFail_WhenProductIsDeleted()
{
    // Arrange
    var sut = CreateSampleProduct();
    sut.Delete("admin@test.com");

    var newName = ProductName.Create("New Name").ThrowIfFail();
    var newDescription = ProductDescription.Create("New Desc").ThrowIfFail();
    var newPrice = Money.Create(200m).ThrowIfFail();

    // Act
    var actual = sut.Update(newName, newDescription, newPrice);

    // Assert
    actual.IsFail.ShouldBeTrue();
}

[Fact]
public void Restore_ShouldAllowUpdate_AfterDeleteAndRestore()
{
    // Arrange
    var sut = CreateSampleProduct();
    sut.Delete("admin@test.com");
    sut.Restore(); // 복원

    var newName = ProductName.Create("Restored Name").ThrowIfFail();
    var newDescription = ProductDescription.Create("Restored Desc").ThrowIfFail();
    var newPrice = Money.Create(300m).ThrowIfFail();

    // Act
    var actual = sut.Update(newName, newDescription, newPrice);

    // Assert
    actual.IsSucc.ShouldBeTrue();
    ((string)sut.Name).ShouldBe("Restored Name");
}
```

`Update` 메서드는 첫 번째 가드에서 `DeletedAt.IsSome`을 확인합니다. 삭제된 상태에서는 `AlreadyDeleted` 에러를 반환하여 상태 변경을 차단합니다. `Restore()` 이후에는 `DeletedAt`이 `None`으로 초기화되므로, 다시 수정이 가능해집니다.

### 시나리오 11: 재고 부족

**비즈니스 규칙:** 현재 재고보다 많은 수량을 차감할 수 없습니다.

```csharp
[Fact]
public void DeductStock_ShouldFail_WhenInsufficientStock()
{
    // Arrange
    var sut = CreateSampleInventory(2); // 재고 2개

    // Act
    var result = sut.DeductStock(Quantity.Create(5).ThrowIfFail()); // 5개 차감 시도

    // Assert
    result.IsFail.ShouldBeTrue();
}
```

`DeductStock`은 요청 수량(`quantity`)이 현재 재고(`StockQuantity`)를 초과하면 `InsufficientStock` 에러를 반환합니다. `Fin<Unit>` 반환 타입이 호출자에게 재고 부족 상황에 대한 처리를 강제합니다.

### 시나리오 12: 신용한도 초과

**비즈니스 규칙:** 주문 금액이 고객의 신용 한도를 초과하면 주문이 거부됩니다. 기존 주문 합산 시에도 동일합니다.

```csharp
[Fact]
public void ValidateCreditLimit_ReturnsFail_WhenAmountExceedsLimit()
{
    // Arrange
    var customer = CreateSampleCustomer(creditLimit: 5000m);
    var orderAmount = Money.Create(6000m).ThrowIfFail();

    // Act
    var actual = _sut.ValidateCreditLimit(customer, orderAmount);

    // Assert
    actual.IsFail.ShouldBeTrue();
}

[Fact]
public void ValidateCreditLimitWithExistingOrders_ReturnsFail_WhenTotalExceedsLimit()
{
    // Arrange
    var customer = CreateSampleCustomer(creditLimit: 5000m);
    var existingOrders = Seq(
        CreateSampleOrder(unitPrice: 2000m),
        CreateSampleOrder(unitPrice: 2000m)); // 기존 합계 4000
    var newOrderAmount = Money.Create(2000m).ThrowIfFail(); // 4000 + 2000 = 6000 > 5000

    // Act
    var actual = _sut.ValidateCreditLimitWithExistingOrders(customer, existingOrders, newOrderAmount);

    // Assert
    actual.IsFail.ShouldBeTrue();
}
```

`OrderCreditCheckService`는 단일 주문(`ValidateCreditLimit`)과 누적 주문(`ValidateCreditLimitWithExistingOrders`) 두 가지 검증을 제공합니다. 두 경우 모두 한도 초과 시 `CreditLimitExceeded` 에러를 반환합니다.

## 에러 타입 매트릭스

도메인 에러는 각 Aggregate 또는 Domain Service 내부에 중첩 레코드로 정의됩니다. 중앙 ErrorCode enum 대신 타입별 sealed record를 사용하는 이유는, 에러의 출처와 분류를 타입 시스템으로 보장하고 `DomainError.For<T>()`가 에러 코드를 자동 생성하기 때문입니다.

각 거부 시나리오는 `sealed record` 에러 타입을 통해 실패 원인을 구조적으로 식별합니다. `DomainError.For<TDomain>()`이 에러 코드를 `DomainErrors.{타입명}.{에러명}` 형식으로 자동 생성하므로, 문자열 비교 없이 에러를 패턴 매칭할 수 있습니다.

| Aggregate/서비스 | 에러 타입 | 발생 조건 | 반환 타입 |
|-----------------|----------|----------|----------|
| `Order` | `EmptyOrderLines` | 주문 라인이 비어 있음 | `Fin<Order>` |
| `Order` | `InvalidOrderStatusTransition` | 허용되지 않은 상태 전이 | `Fin<Unit>` |
| `OrderLine` | `InvalidQuantity` | 주문 라인 수량 유효성 실패 | `Fin<OrderLine>` |
| `OrderStatus` | `InvalidValue` | 유효하지 않은 상태 문자열 | `Fin<OrderStatus>` |
| `Product` | `AlreadyDeleted` | 삭제된 상품 수정 시도 | `Fin<Product>` |
| `Inventory` | `InsufficientStock` | 재고 부족 상태에서 차감 | `Fin<Unit>` |
| `OrderCreditCheckService` | `CreditLimitExceeded` | 주문 금액이 신용한도 초과 | `Fin<Unit>` |

## 시나리오 커버리지 매트릭스

| 요구사항 | 시나리오 | 결과 | 검증 방법 |
|---------|---------|------|----------|
| 고객 생성 및 속성 설정 | 1. 고객 생성 | `CreatedEvent` 발행, 속성 설정 | `ShouldContain`, `ShouldBe` |
| 상품 생성 | 2. 상품 생성 | `CreatedEvent` 발행 | `ShouldContain` |
| 주문 금액 자동 계산 | 3. 주문 생성 | `TotalAmount = Sum(LineTotal)` | `ShouldBe(400m)` |
| 주문 상태 전이 규칙 | 4. 상태 전이 체인 | Pending -> Confirmed -> Shipped -> Delivered | `IsSucc`, `ShouldBe(OrderStatus.*)` |
| 상품 수명 관리 + 멱등성 | 5. 논리 삭제/복원 | 삭제/복원 + 멱등 보장 | `IsSome`/`IsNone`, `ShouldBeEmpty` |
| 재고 관리 | 6. 재고 차감/추가 | 수량 변경 + 이벤트 발행 | `ShouldBe(7)`, `ShouldContain` |
| 교차 Aggregate 검증 | 7. 신용한도 통과 | 한도 이내 주문 허용 | `IsSucc` |
| 주문 라인 불변식 | 8. 빈 주문라인 | `EmptyOrderLines` 에러 | `IsFail` |
| 상태 전이 불변식 | 9. 잘못된 전이 | `InvalidOrderStatusTransition` 에러 | `IsFail` |
| 삭제 상태 행위 차단 | 10. 삭제된 상품 수정 | `AlreadyDeleted` 에러 | `IsFail` |
| 재고 불변식 | 11. 재고 부족 | `InsufficientStock` 에러 | `IsFail` |
| 신용한도 규칙 | 12. 신용한도 초과 | `CreditLimitExceeded` 에러 | `IsFail` |

지금까지 개별 시나리오를 통해 도메인 모델의 동작을 검증했습니다. 이제 이 예제에서 DDD 전술적 패턴과 함수형 타입이 어떻게 협력하여 비즈니스 규칙을 보장하는지 종합적으로 정리합니다.

## DDD 전술적 패턴의 역할

Eric Evans의 DDD 빌딩 블록은 "어떤 규칙을 어디서 보장할 것인가"를 결정합니다.

| 패턴 | 적용 | 역할 | 보장 효과 |
|-----|------|------|----------|
| Value Object | `Money`, `Quantity`, `OrderStatus`, `CustomerName`, `Email`, `ShippingAddress` 등 | 단일 값의 유효성과 불변성 보장 | 잘못된 값의 존재 자체를 차단 |
| Aggregate Root | `Customer`, `Product`, `Order`, `Inventory`, `Tag` | 불변식 경계와 일관성의 단일 진입점 | 모든 상태 변경이 Aggregate를 통해서만 발생 |
| Entity | `OrderLine` | Aggregate 내부의 식별 가능한 객체 | 수명 주기가 Aggregate에 종속되어 고아 엔티티 방지 |
| Domain Event | `CreatedEvent`, `ConfirmedEvent`, `StockDeductedEvent` 등 | 상태 변경의 추적과 전파 | 모든 변경을 명시적으로 기록 |
| Domain Service | `OrderCreditCheckService` | 교차 Aggregate 비즈니스 규칙 검증 | 단일 Aggregate로 해결할 수 없는 규칙을 별도 서비스로 분리 |
| 교차 Aggregate 참조 | `Order.CustomerId`, `Inventory.ProductId` | Aggregate 간 ID 기반 느슨한 연결 | Aggregate 경계를 침범하지 않고 참조 유지 |

DDD 전술적 패턴이 '어떤 규칙을 어디서 보장할 것인가'를 결정한다면, 함수형 타입 시스템은 '어떻게 컴파일러에게 규칙 검증을 위임할 것인가'를 제공합니다.

## 함수형 타입 시스템의 역할

Functorium의 함수형 타입은 "어떻게 컴파일러에게 규칙 검증을 위임할 것인가"를 제공합니다.

| 타입 | 적용 | 효과 |
|-----|------|------|
| `Fin<T>` | `Order.Create`, `DeductStock`, `ValidateCreditLimit` 등 | 예외 대신 반환 타입으로 실패를 표현, 호출자가 실패 처리를 강제받음 |
| `Option<T>` | `DeletedAt`, `DeletedBy`, `UpdatedAt` | null 대신 타입 안전한 선택적 값 표현, 삭제 상태의 유무를 `IsSome`/`IsNone`으로 명확히 구분 |
| `DomainErrorType.Custom` | `EmptyOrderLines`, `InsufficientStock`, `CreditLimitExceeded` 등 | 문자열 메시지 대신 `sealed record`로 실패 원인을 구조적으로 식별, 에러 코드 자동 생성 |
| `DomainError.For<T>()` | 모든 에러 생성 | `DomainErrors.{타입명}.{에러명}` 형식의 에러 코드 자동 생성, 패턴 매칭으로 정확한 분기 처리 |
| Value Object 팩토리 | `Money.Create`, `Quantity.Create`, `OrderStatus.Create` | 생성 시점에 검증 완료, 이후 도메인 로직에서 유효성 재확인 불필요 |
| `CreateFromValidated` | 모든 Aggregate의 ORM 복원 | 검증과 이벤트 발행 생략, 이미 영속화된 데이터를 신뢰하여 도메인 생성과 ORM 복원을 분리 |

## 아키텍처 테스트

도메인 모델의 구조적 규칙을 Functorium의 `ArchitectureRules` 프레임워크로 자동 검증합니다. 6개 테스트 클래스가 DDD 빌딩 블록의 구조적 일관성을 보장합니다.

| 테스트 클래스 | 검증 대상 | 핵심 규칙 |
|-------------|----------|----------|
| `ValueObjectArchitectureRuleTests` | Value Object (Money, Quantity, CustomerName, Email 등) | public sealed, 불변성, `Create`/`Validate` 팩토리 |
| `EntityArchitectureRuleTests` | 5개 AggregateRoot + OrderLine (Entity) | public sealed, `Create`/`CreateFromValidated`, `[GenerateEntityId]`, private 생성자 |
| `DomainEventArchitectureRuleTests` | 19개 Domain Event | sealed record, `Event` 접미사 |
| `DomainServiceArchitectureRuleTests` | OrderCreditCheckService | public sealed, stateless, `Fin` 반환, IObservablePort 미의존, record 아님 |
| `SpecificationArchitectureRuleTests` | 6개 Specification | public sealed, `Specification<>` 상속, 도메인 레이어 거주 |
