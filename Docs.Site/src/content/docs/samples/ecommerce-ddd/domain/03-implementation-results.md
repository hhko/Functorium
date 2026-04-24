---
title: "Domain Implementation Results"
---

We prove the scenarios defined in the [business requirements](../00-business-requirements/) by executing them with the type structure implemented in the [code design](../02-code-design/). Normal scenarios (1-7) verify how types represent valid states, and rejection scenarios (8-12) verify how they block invalid states.

## Normal Scenario Verification

### Scenario 1: Customer Creation

**Business Rule:** A customer has a name, email, and credit limit, and a `CreatedEvent` is published upon creation.

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

`CustomerName`, `Email`, and `Money` are each validated Value Objects. `Create` receives already validated VOs to construct the Aggregate and publishes a `CreatedEvent`. The ID is auto-generated based on Ulid.

### Scenario 2: Product Creation

**Business Rule:** A product has a name, description, and price, and a `CreatedEvent` is published upon creation.

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

`Product.Create` receives three VOs (`ProductName`, `ProductDescription`, `Money`) to construct the Aggregate. The product implements `ISoftDeletableWithUser` to support soft delete.

### Scenario 3: Order Creation

**Business Rule:** An order must contain at least 1 order line, and `TotalAmount` is automatically calculated as the sum of all lines.

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

`Order.Create` returns `Fin<Order>`. `TotalAmount` is auto-calculated as the sum of individual `OrderLine.LineTotal` (`Quantity * UnitPrice`), and the status is immediately set to `Pending` upon creation. `CustomerId` is a cross-Aggregate reference that holds the Customer Aggregate's ID as a value.

### Scenario 4: Order Status Transition Chain

**Business Rule:** Order status transitions in the order `Pending -> Confirmed -> Shipped -> Delivered`, with domain events published for each transition.

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
    sut.Confirm(); // Pending -> Confirmed

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

`OrderStatus` defines allowed transition rules via `CanTransitionTo`. `Confirm()`, `Ship()`, `Deliver()` internally call `TransitionTo`, which verifies whether the transition from the current state to the target state is allowed, then changes the state and publishes an event.

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

State transition rules are encapsulated inside the `OrderStatus` Value Object, structurally preventing the Aggregate from transitioning to an invalid state.

### Scenario 5: Product Soft Delete + Restore

**Business Rule:** When a product is soft deleted, the deleter and timestamp are recorded. When restored, the deletion information is cleared. Both delete and restore are idempotent.

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
    sut.Delete("other@test.com"); // Second delete attempt

    // Assert
    sut.DomainEvents.ShouldBeEmpty();              // No events added
    sut.DeletedBy.ShouldBe(Some("admin@test.com")); // Original deleter preserved
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
    sut.Restore(); // Restore attempt on non-deleted state

    // Assert
    sut.DomainEvents.ShouldBeEmpty(); // No events added
}
```

`DeletedAt` and `DeletedBy` are expressed as `Option<T>`. On deletion they transition to `Some(value)`, on restoration to `None`. Calling delete (restore) again on an already deleted (restored) state does not add events, guaranteeing idempotency.

### Scenario 6: Inventory Deduction/Addition

**Business Rule:** Inventory supports deduction and addition, with domain events published for each operation.

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

`Inventory` is a dedicated inventory management Aggregate separated from `Product`. It supports optimistic concurrency control (`IConcurrencyAware`) for high-frequency changes (per-order `DeductStock`). `DeductStock` returns `Fin<Unit>` to explicitly express failure on insufficient stock.

### Scenario 7: Credit Limit Validation (Pass)

**Business Rule:** If the order amount is within the customer's credit limit, the order is allowed. The sum of existing orders is also considered.

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

`OrderCreditCheckService` is a cross-Aggregate validation service implementing `IDomainService`. It compares the `Customer`'s credit limit with the `Order`'s amount to validate business rules that cannot be performed within a single Aggregate. `ValidateCreditLimitWithExistingOrders` validates the cumulative amount including existing order totals.

Having verified how valid states are represented in normal scenarios, we now verify how invalid states are blocked in rejection scenarios. The true value of the domain type system lies not in 'representing allowed states' but in 'making disallowed states structurally impossible.'

## Rejection Scenario Verification

### Scenario 8: Empty Order Lines

**Business Rule:** Orders without order lines cannot be created.

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

`Order.Create` returns a `Fin.Fail` containing an `EmptyOrderLines` error when the order line list is empty. The `Fin<Order>` return type forces the caller to handle failure, structurally preventing the creation of empty orders.

### Scenario 9: Invalid Status Transition

**Business Rule:** Disallowed state transitions are rejected. (e.g., `Pending -> Shipped`, `Delivered -> Cancelled`)

```csharp
[Fact]
public void Ship_ShouldFail_WhenPending()
{
    // Arrange
    var sut = CreateSampleOrder(); // Status = Pending

    // Act
    var actual = sut.Ship(); // Attempting Pending -> Shipped

    // Assert
    actual.IsFail.ShouldBeTrue();
}

[Fact]
public void Deliver_ShouldFail_WhenCancelled()
{
    // Arrange
    var sut = CreateSampleOrder();
    sut.Cancel(); // Pending -> Cancelled

    // Act
    var actual = sut.Deliver(); // Attempting Cancelled -> Delivered

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
    sut.Deliver(); // Shipped -> Delivered

    // Act
    var actual = sut.Cancel(); // Attempting Delivered -> Cancelled

    // Assert
    actual.IsFail.ShouldBeTrue();
}
```

Inside `TransitionTo`, when `OrderStatus.CanTransitionTo` detects a disallowed transition, it returns an `InvalidOrderStatusTransition` error. Ship is only possible after Confirmed, and Deliver is only possible after Shipped.

### Scenario 10: Modifying a Deleted Product

**Business Rule:** Soft-deleted products cannot be modified. After restoration, modification becomes possible.

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
    sut.Restore(); // Restore

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

The `Update` method checks `DeletedAt.IsSome` in its first guard. In the deleted state, it returns an `AlreadyDeleted` error to block state changes. After `Restore()`, `DeletedAt` is reset to `None`, making modification possible again.

### Scenario 11: Insufficient Stock

**Business Rule:** More than the current stock cannot be deducted.

```csharp
[Fact]
public void DeductStock_ShouldFail_WhenInsufficientStock()
{
    // Arrange
    var sut = CreateSampleInventory(2); // Stock: 2

    // Act
    var result = sut.DeductStock(Quantity.Create(5).ThrowIfFail()); // Attempting to deduct 5

    // Assert
    result.IsFail.ShouldBeTrue();
}
```

`DeductStock` returns an `InsufficientStock` error when the requested quantity exceeds the current stock (`StockQuantity`). The `Fin<Unit>` return type forces the caller to handle insufficient stock situations.

### Scenario 12: Credit Limit Exceeded

**Business Rule:** If the order amount exceeds the customer's credit limit, the order is rejected. The same applies when summing existing orders.

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
        CreateSampleOrder(unitPrice: 2000m)); // Existing total: 4000
    var newOrderAmount = Money.Create(2000m).ThrowIfFail(); // 4000 + 2000 = 6000 > 5000

    // Act
    var actual = _sut.ValidateCreditLimitWithExistingOrders(customer, existingOrders, newOrderAmount);

    // Assert
    actual.IsFail.ShouldBeTrue();
}
```

`OrderCreditCheckService` provides two validations: single order (`ValidateCreditLimit`) and cumulative orders (`ValidateCreditLimitWithExistingOrders`). In both cases, a `CreditLimitExceeded` error is returned when the limit is exceeded.

## Error Type Matrix

Domain errors are defined as nested records within each Aggregate or Domain Service. The reason for using per-type sealed records instead of a centralized ErrorCode enum is that the type system guarantees error origin and classification, and `DomainError.For<T>()` auto-generates error codes.

Each rejection scenario structurally identifies failure causes through `sealed record` error types. `DomainError.For<TDomain>()` auto-generates error codes in the format `Domain.{TypeName}.{ErrorName}`, enabling pattern matching without string comparison.

| Aggregate/Service | Error Type | Trigger Condition | Return Type |
|-------------------|-----------|-------------------|------------|
| `Order` | `EmptyOrderLines` | Order lines are empty | `Fin<Order>` |
| `Order` | `InvalidOrderStatusTransition` | Disallowed state transition | `Fin<Unit>` |
| `OrderLine` | `InvalidQuantity` | Order line quantity validation failure | `Fin<OrderLine>` |
| `OrderStatus` | `InvalidValue` | Invalid status string | `Fin<OrderStatus>` |
| `Product` | `AlreadyDeleted` | Attempting to modify deleted product | `Fin<Product>` |
| `Inventory` | `InsufficientStock` | Deduction during insufficient stock | `Fin<Unit>` |
| `OrderCreditCheckService` | `CreditLimitExceeded` | Order amount exceeds credit limit | `Fin<Unit>` |

## Scenario Coverage Matrix

| Requirement | Scenario | Result | Verification Method |
|-------------|---------|--------|---------------------|
| Customer creation and property setting | 1. Customer creation | `CreatedEvent` published, properties set | `ShouldContain`, `ShouldBe` |
| Product creation | 2. Product creation | `CreatedEvent` published | `ShouldContain` |
| Automatic order amount calculation | 3. Order creation | `TotalAmount = Sum(LineTotal)` | `ShouldBe(400m)` |
| Order status transition rules | 4. Status transition chain | Pending -> Confirmed -> Shipped -> Delivered | `IsSucc`, `ShouldBe(OrderStatus.*)` |
| Product lifecycle management + idempotency | 5. Soft delete/restore | Delete/restore + idempotency guaranteed | `IsSome`/`IsNone`, `ShouldBeEmpty` |
| Inventory management | 6. Inventory deduction/addition | Quantity change + event publication | `ShouldBe(7)`, `ShouldContain` |
| Cross-Aggregate validation | 7. Credit limit pass | Order within limit allowed | `IsSucc` |
| Order line invariants | 8. Empty order lines | `EmptyOrderLines` error | `IsFail` |
| State transition invariants | 9. Invalid transition | `InvalidOrderStatusTransition` error | `IsFail` |
| Deleted state behavior blocking | 10. Modifying deleted product | `AlreadyDeleted` error | `IsFail` |
| Inventory invariants | 11. Insufficient stock | `InsufficientStock` error | `IsFail` |
| Credit limit rules | 12. Credit limit exceeded | `CreditLimitExceeded` error | `IsFail` |

Having verified the behavior of the domain model through individual scenarios, we now provide a comprehensive summary of how DDD tactical patterns and functional types collaborate in this example to guarantee business rules.

## Role of DDD Tactical Patterns

Eric Evans' DDD building blocks determine "which rules to guarantee where."

| Pattern | Application | Role | Guarantee Effect |
|---------|------------|------|------------------|
| Value Object | `Money`, `Quantity`, `OrderStatus`, `CustomerName`, `Email`, `ShippingAddress`, etc. | Guarantees validity and immutability of single values | Blocks the very existence of invalid values |
| Aggregate Root | `Customer`, `Product`, `Order`, `Inventory`, `Tag` | Invariant boundary and single entry point for consistency | All state changes occur only through the Aggregate |
| Entity | `OrderLine` | Identifiable object within the Aggregate | Lifecycle dependent on Aggregate, preventing orphan entities |
| Domain Event | `CreatedEvent`, `ConfirmedEvent`, `StockDeductedEvent`, etc. | Tracking and propagation of state changes | All changes explicitly recorded |
| Domain Service | `OrderCreditCheckService` | Cross-Aggregate business rule validation | Rules that cannot be resolved by a single Aggregate separated into a dedicated service |
| Cross-Aggregate Reference | `Order.CustomerId`, `Inventory.ProductId` | Loose ID-based connection between Aggregates | References maintained without violating Aggregate boundaries |

While DDD tactical patterns determine 'which rules to guarantee where,' the functional type system provides 'how to delegate rule verification to the compiler.'

## Role of the Functional Type System

Functorium's functional types provide "how to delegate rule verification to the compiler."

| Type | Application | Effect |
|------|------------|--------|
| `Fin<T>` | `Order.Create`, `DeductStock`, `ValidateCreditLimit`, etc. | Expresses failure via return type instead of exceptions, forcing callers to handle failures |
| `Option<T>` | `DeletedAt`, `DeletedBy`, `UpdatedAt` | Type-safe optional value expression instead of null, clearly distinguishing presence/absence of deleted state via `IsSome`/`IsNone` |
| `DomainErrorKind.Custom` | `EmptyOrderLines`, `InsufficientStock`, `CreditLimitExceeded`, etc. | Structurally identifies failure causes via `sealed record` instead of string messages, auto-generates error codes |
| `DomainError.For<T>()` | All error creation | Auto-generates error codes in `Domain.{TypeName}.{ErrorName}` format, enabling precise branching via pattern matching |
| Value Object Factory | `Money.Create`, `Quantity.Create`, `OrderStatus.Create` | Validation complete at creation time, no need to re-verify validity in subsequent domain logic |
| `CreateFromValidated` | ORM restoration for all Aggregates | Skips validation and event publication, trusts already persisted data to separate domain creation from ORM restoration |

## Architecture Tests

The structural rules of the domain model are automatically verified using Functorium's `ArchitectureRules` framework. 6 test classes guarantee the structural consistency of DDD building blocks.

| Test Class | Verification Target | Key Rules |
|------------|---------------------|-----------|
| `ValueObjectArchitectureRuleTests` | Value Object (Money, Quantity, CustomerName, Email, etc.) | public sealed, immutability, `Create`/`Validate` factory |
| `EntityArchitectureRuleTests` | 5 AggregateRoots + OrderLine (Entity) | public sealed, `Create`/`CreateFromValidated`, `[GenerateEntityId]`, private constructor |
| `DomainEventArchitectureRuleTests` | 19 Domain Events | sealed record, `Event` suffix |
| `DomainServiceArchitectureRuleTests` | OrderCreditCheckService | public sealed, stateless, `Fin` return, no IObservablePort dependency, not a record |
| `SpecificationArchitectureRuleTests` | 6 Specifications | public sealed, `Specification<>` inheritance, residing in domain layer |
