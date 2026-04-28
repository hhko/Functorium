---
title: "Framework Type Selection Guide"
---
## Overview

A decision guide for choosing which framework type to use when implementing value objects.

---

## Decision Tree

```
Is it one of several variants? ─── Yes ──→ State transition needed? ─── Yes ──→ UnionValueObject<TSelf>
       │                                          │
       No                                         No ──→ UnionValueObject
       │
Is it a single value? ─── Yes ──→ Comparison needed? ─── Yes ──→ ComparableSimpleValueObject<T>
       │                                  │
       No                                 No ──→ SimpleValueObject<T>
       │
Is it an enum? ─── Yes ──→ SmartEnum + IValueObject
       │
       No
       │
Comparison needed? ─── Yes ──→ ComparableValueObject
       │
       No ──→ ValueObject
```

---

## Detailed Guide by Type

### 1. SimpleValueObject<T>

**When to use?**
- When wrapping a single value
- When comparison (sorting) is not needed
- The most common value object

**Examples**
```
- Email (string)
- ProductCode (string)
- Password (hashed string)
- UserId (GUID)
```

**Implementation Example**
```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            ValidationRules<Email>.NotNull(value)
                .ThenNotEmpty()
                .ThenMaxLength(255),
            v => new Email(v));
}
```

---

### 2. ComparableSimpleValueObject<T>

**When to use?**
- When wrapping a single value
- When sorting or comparison is needed
- When the inner value implements `IComparable<T>`

**Examples**
```
- Age (integer - age comparison)
- Quantity (integer - quantity comparison)
- Amount (decimal - monetary comparison)
- InterestRate (decimal - rate comparison)
- DateOfBirth (DateOnly - date comparison)
```

**Implementation Example**
```csharp
public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    public static Fin<Age> Create(int value)
    {
        if (value < 0 || value > 150)
            return DomainError.For<Age, int>(new OutOfRange("0", "150"), value, "Invalid age");
        return new Age(value);
    }
}
```

---

### 3. ValueObject (Composite)

**When to use?**
- Value object with multiple properties
- When comparison (sorting) is not needed
- When composite values are required

**Examples**
```
- Address (city, street, postal code)
- FullName (first name, last name)
- Coordinate (latitude, longitude)
- DateTimeRange (start, end)
```

**Implementation Example**
```csharp
public sealed class Address : ValueObject
{
    public string City { get; }
    public string Street { get; }
    public string PostalCode { get; }

    private Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return City;
        yield return Street;
        yield return PostalCode;
    }
}
```

---

### 4. ComparableValueObject (Comparable Composite)

**When to use?**
- Value object with multiple properties
- When sorting or comparison is needed
- When sorting by composite key is required

**Examples**
```
- Money (amount, currency - comparison within same currency)
- DateRange (start date, end date - sort by start date)
- ExchangeRate (base currency, quote currency, rate)
- TimeSlot (start time, end time)
```

**Implementation Example**
```csharp
public sealed class Money : ComparableValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Currency;
        yield return Amount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

### 5. SmartEnum + IValueObject (Type-Safe Enumeration)

**When to use?**
- A restricted set of values
- When each value has behavior or properties
- When state transition logic is needed

**Examples**
```
- OrderStatus (Pending, Confirmed, Shipping, Completed)
- PaymentMethod (Card, Cash, Bank Transfer)
- UserRole (Admin, User, Guest)
- TransactionType (Deposit, Withdrawal, Transfer)
```

**Implementation Example**
```csharp
public sealed class OrderStatus : SmartEnum<OrderStatus, string>, IValueObject
{
    public static readonly OrderStatus Pending = new("PENDING", "Pending", canCancel: true);
    public static readonly OrderStatus Shipped = new("SHIPPED", "Shipped", canCancel: false);

    public string DisplayName { get; }
    public bool CanCancel { get; }

    private OrderStatus(string value, string displayName, bool canCancel)
        : base(displayName, value)
    {
        DisplayName = displayName;
        CanCancel = canCancel;
    }
}
```

### 6. UnionValueObject (Pure Data Union)

**When to use?**
- When it is exactly one of several variants (cases)
- When exhaustive branching via pattern matching is needed
- When a closed type hierarchy is needed
- **Pure data union without state transitions**

**Examples**
```
- Shape (Circle | Rectangle | Triangle)
- PaymentMethod (CreditCard | BankTransfer | Cash)
- Result (Success | Failure)
```

**Implementation Example**
```csharp
public abstract record Shape : UnionValueObject
{
    public sealed record Circle(double Radius) : Shape;
    public sealed record Rectangle(double Width, double Height) : Shape;
    public sealed record Triangle(double Base, double Height) : Shape;

    public double Area => Match(
        circle: c => Math.PI * c.Radius * c.Radius,
        rectangle: r => r.Width * r.Height,
        triangle: t => 0.5 * t.Base * t.Height);
}
```

### 7. UnionValueObject&lt;TSelf&gt; (State Transition Union)

**When to use?**
- When it is exactly one of several variants and **state transitions are needed**
- When only valid transitions are allowed via `TransitionFrom` and invalid transitions are handled as `Fin<T>` failures
- When CRTP is needed to pass precise type information to `DomainError`

**Examples**
```
- OrderStatus (Pending → Confirmed → Shipped → Delivered)
- PaymentState (Initiated → Authorized → Captured → Refunded)
- ApprovalStatus (Draft → Submitted → Approved | Rejected)
```

**Implementation Example**
```csharp
public abstract record OrderStatus : UnionValueObject<OrderStatus>
{
    public sealed record Pending(string OrderId) : OrderStatus;
    public sealed record Confirmed(string OrderId, DateTime ConfirmedAt) : OrderStatus;
    private OrderStatus() { }

    public Fin<Confirmed> Confirm(DateTime confirmedAt) =>
        TransitionFrom<Pending, Confirmed>(
            p => new Confirmed(p.OrderId, confirmedAt));
}
```

---

## Quick Selection Table

A comparison of features supported by each type at a glance.

| Feature | SimpleValueObject | ComparableSimple | ValueObject | ComparableValue | SmartEnum | UnionValueObj | UnionValueObj&lt;TSelf&gt; |
|---------|:-----------------:|:----------------:|:-----------:|:---------------:|:---------:|:-------------:|:---------------------:|
| Single value | ✅ | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Composite value | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |
| DU (one of variants) | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Comparable | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Sortable | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Enumeration | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ |
| State transition | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ | ✅ |

---

## Common Mistakes

### 1. Unnecessary Comparability

```csharp
// ❌ Email does not need to be sorted
public sealed class Email : ComparableSimpleValueObject<string> { }

// ✅ Use a simple value object
public sealed class Email : SimpleValueObject<string> { }
```

### 2. Using a Composite Type for a Single Value

```csharp
// ❌ Unnecessarily complex
public sealed class ProductCode : ValueObject
{
    public string Value { get; }
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

// ✅ Keep it simple
public sealed class ProductCode : SimpleValueObject<string> { }
```

### 3. Not Using SmartEnum for Enumerations

```csharp
// ❌ Limitations of a plain enum
public enum OrderStatus { Pending, Shipped }

// ✅ SmartEnum with behavior
public sealed class OrderStatus : SmartEnum<OrderStatus, string>, IValueObject
{
    public bool CanCancel { get; }
    public Fin<OrderStatus> TransitionTo(OrderStatus next) { ... }
}
```

---

## Checklist

Verify the following when implementing a value object:

- [ ] Is the type declared as sealed?
- [ ] Does it use a factory method (Create) instead of a public constructor?
- [ ] Is the validation logic in the Create method?
- [ ] Does it have a Domain inner class?
- [ ] Is immutability guaranteed?
- [ ] Does it have implicit conversion operators where needed?
- [ ] Is ToString() appropriately overridden?

---

## Next Steps

Check the glossary.

→ [C. Glossary](C-glossary.md)
