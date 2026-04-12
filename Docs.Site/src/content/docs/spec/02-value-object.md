---
title: "Value Object Specification"
---

Defines the public API for Value Object types provided by the Functorium framework. For design intent and hands-on guidance, see the [Value Object Guide](../guides/domain/05a-value-objects). This document covers each type's signature, contract, and behavioral rules.

## Summary

### Key Types

| Type | Namespace | Description |
|------|-------------|------|
| `IValueObject` | `Functorium.Domains.ValueObjects` | Value Object marker interface |
| `AbstractValueObject` | `Functorium.Domains.ValueObjects` | Value-based equality base class |
| `ValueObject` | `Functorium.Domains.ValueObjects` | Composite Value Object base class |
| `SimpleValueObject<T>` | `Functorium.Domains.ValueObjects` | Single-value wrapper base class |
| `ComparableValueObject` | `Functorium.Domains.ValueObjects` | Comparable composite Value Object base class |
| `ComparableSimpleValueObject<T>` | `Functorium.Domains.ValueObjects` | Comparable single-value wrapper base class |
| `IUnionValueObject` | `Functorium.Domains.ValueObjects.Unions` | Union Value Object marker interface |
| `UnionValueObject` | `Functorium.Domains.ValueObjects.Unions` | Discriminated Union base record |
| `UnionValueObject<TSelf>` | `Functorium.Domains.ValueObjects.Unions` | State transition-supporting Union record |
| `[UnionType]` | `Functorium.Domains.ValueObjects.Unions` | Match/Switch source generator trigger |
| `UnreachableCaseException` | `Functorium.Domains.ValueObjects.Unions` | Unreachable case exception |

### Key Concepts

| Concept | Description |
|------|------|
| Value-based equality | Equality determined by components returned from `GetEqualityComponents()` |
| Factory method pattern | Object creation flow: `Create()` -> `Validate()` -> `CreateFromValidation()` |
| Hash code caching | Cached after first computation, O(1) subsequent returns |
| Proxy type transparency | Transparently handles ORM proxies (Castle, NHibernate, EF Core) |
| CRTP state transition | Type-safe transitions in `UnionValueObject<TSelf>` using Curiously Recurring Template Pattern |

---

## Class Hierarchy

```
IValueObject (marker interface)
├── AbstractValueObject (abstract class, equality)
│   └── ValueObject (abstract class, CreateFromValidation)
│       ├── SimpleValueObject<T> (abstract class, single value)
│       └── ComparableValueObject (abstract class, IComparable)
│           └── ComparableSimpleValueObject<T> (abstract class, single comparable value)
│
IUnionValueObject : IValueObject (marker interface)
└── UnionValueObject (abstract record)
    └── UnionValueObject<TSelf> (abstract record, state transition)
```

The Value Object hierarchy branches into two directions. The **class-based hierarchy** has `AbstractValueObject` as its root, providing equality and factory methods. The **record-based hierarchy** has `UnionValueObject` as its root, providing the Discriminated Union pattern.

---

## IValueObject

A marker interface indicating a Value Object. Used by source generators and architecture rule validation to identify Value Object types.

```csharp
public interface IValueObject
{
    const string CreateMethodName = "Create";
    const string CreateFromValidatedMethodName = "CreateFromValidated";
    const string ValidateMethodName = "Validate";
    const string DomainErrorsNestedClassName = "DomainErrors";
}
```

### Constants

| Constant | Value | Purpose |
|------|----|------|
| `CreateMethodName` | `"Create"` | Factory method name convention |
| `CreateFromValidatedMethodName` | `"CreateFromValidated"` | Factory method name convention for pre-validated values |
| `ValidateMethodName` | `"Validate"` | Validation-only method name convention |
| `DomainErrorsNestedClassName` | `"DomainErrors"` | Nested error class name convention |

---

## AbstractValueObject

Root abstract class for all class-based Value Objects. Provides value-based equality comparison, hash code caching, and ORM proxy transparency.

```csharp
[Serializable]
public abstract class AbstractValueObject
    : IValueObject
    , IEquatable<AbstractValueObject>
```

### Abstract Members

| Member | Signature | Description |
|------|---------|------|
| `GetEqualityComponents()` | `protected abstract IEnumerable<object> GetEqualityComponents()` | Returns components used for equality comparison |

### Public Members

| Member | Signature | Description |
|------|---------|------|
| `Equals(object?)` | `public override bool Equals(object? obj)` | Value-based equality comparison |
| `Equals(AbstractValueObject?)` | `public bool Equals(AbstractValueObject? other)` | Type-safe equality comparison (`IEquatable<T>`) |
| `GetHashCode()` | `public override int GetHashCode()` | Returns cached hash code |
| `operator ==` | `public static bool operator ==(AbstractValueObject?, AbstractValueObject?)` | Equality operator |
| `operator !=` | `public static bool operator !=(AbstractValueObject?, AbstractValueObject?)` | Inequality operator |

### Protected Members

| Member | Signature | Description |
|------|---------|------|
| `GetUnproxiedType(object)` | `protected static Type GetUnproxiedType(object obj)` | Strips ORM proxy type and returns the actual type |

### Equality Contract

1. **Component comparison**: Compares sequences returned by `GetEqualityComponents()` using `SequenceEqual`.
2. **Type match required**: Comparison targets must be the same type (after proxy removal) to be considered equal.
3. **Array content comparison**: Internal `ValueObjectEqualityComparer` compares arrays element by element. Not suitable for large arrays (100KB+).
4. **Hash code caching**: Computed and cached on first `GetHashCode()` call. Safe because Value Objects are immutable.
5. **Proxy transparency**: Automatically detects proxy types from Castle.Proxies, NHibernate.Proxy, and EF Core Proxies namespaces, substituting `BaseType`.

---

## ValueObject

Base class for composite Value Objects (Value Objects with multiple fields). Provides the `CreateFromValidation` factory method template.

```csharp
[Serializable]
public abstract class ValueObject : AbstractValueObject
```

### Static Methods

| Method | Signature | Description |
|--------|---------|------|
| `CreateFromValidation<TValueObject, TValue>` | `public static Fin<TValueObject> CreateFromValidation<TValueObject, TValue>(Validation<Error, TValue> validation, Func<TValue, TValueObject> factory) where TValueObject : ValueObject` | Converts a Validation result to Fin and creates a Value Object |

### Usage Example

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Fin<Money> Create(decimal amount, string currency)
    {
        var validation = (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => new Money(a, c));
        return CreateFromValidation<Money, Money>(validation, x => x);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

---

## SimpleValueObject\<T\>

Base class for Value Objects wrapping a single value. Provides a `Value` property, explicit conversion operator, and simplified `CreateFromValidation`.

```csharp
[Serializable]
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
```

### Protected Members

| Member | Signature | Description |
|------|---------|------|
| `Value` | `protected T Value { get; }` | The wrapped value |
| Constructor | `protected SimpleValueObject(T value)` | Throws `ArgumentNullException` if `null` is passed |

### Public Members

| Member | Signature | Description |
|------|---------|------|
| `ToString()` | `public override string ToString()` | Returns `Value.ToString()` |
| `explicit operator T` | `public static explicit operator T(SimpleValueObject<T>? valueObject)` | Explicit conversion (`InvalidOperationException` if `null`) |
| `operator ==` | `public static bool operator ==(SimpleValueObject<T>?, SimpleValueObject<T>?)` | Equality operator |
| `operator !=` | `public static bool operator !=(SimpleValueObject<T>?, SimpleValueObject<T>?)` | Inequality operator |

### Static Methods

| Method | Signature | Description |
|--------|---------|------|
| `CreateFromValidation<TValueObject>` | `public static Fin<TValueObject> CreateFromValidation<TValueObject>(Validation<Error, T> validation, Func<T, TValueObject> factory) where TValueObject : SimpleValueObject<T>` | Converts single-value Validation to Fin |

### Equality Sealing

`Equals(object?)` and `GetHashCode()` are declared as `sealed override`, preventing derived classes from overriding them. Equality logic must be defined through `GetEqualityComponents()`.

### Usage Example

```csharp
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Validation<Error, string> Validate(string value) =>
        ValidationRules<Email>.NotEmpty(value)
            .ThenMatches(EmailRegex())
            .ThenMaxLength(254);

    public static Fin<Email> Create(string value) =>
        CreateFromValidation(Validate(value), v => new Email(v));
}
```

---

## ComparableValueObject

Base class for comparable composite Value Objects. Implements `IComparable<ComparableValueObject>` to support sorting operators (`<`, `<=`, `>`, `>=`).

```csharp
[Serializable]
public abstract class ComparableValueObject : ValueObject, IComparable<ComparableValueObject>
```

### Abstract Members

| Member | Signature | Description |
|------|---------|------|
| `GetComparableEqualityComponents()` | `protected abstract IEnumerable<IComparable> GetComparableEqualityComponents()` | Returns `IComparable` components used for comparison |

### Public Members

| Member | Signature | Description |
|------|---------|------|
| `CompareTo(ComparableValueObject?)` | `public virtual int CompareTo(ComparableValueObject? other)` | Compares components in order |
| `operator <` | `public static bool operator <(ComparableValueObject?, ComparableValueObject?)` | Less than comparison |
| `operator <=` | `public static bool operator <=(ComparableValueObject?, ComparableValueObject?)` | Less than or equal comparison |
| `operator >` | `public static bool operator >(ComparableValueObject?, ComparableValueObject?)` | Greater than comparison |
| `operator >=` | `public static bool operator >=(ComparableValueObject?, ComparableValueObject?)` | Greater than or equal comparison |

### Comparison Contract

1. **Sequential component comparison**: Compares the sequence returned by `GetComparableEqualityComponents()` from front to back, determining the result at the first differing element.
2. **Null handling**: Returns `1` (this is greater) if `other` is `null`.
3. **Type mismatch**: Returns string comparison result of type names if types differ.
4. **Equality delegation**: `GetEqualityComponents()` wraps `GetComparableEqualityComponents()`, so equality and comparison use the same components.

---

## ComparableSimpleValueObject\<T\>

Base class for Value Objects wrapping a single comparable value. `T` must implement `IComparable`.

```csharp
[Serializable]
public abstract class ComparableSimpleValueObject<T> : ComparableValueObject
    where T : notnull, IComparable
```

### Protected Members

| Member | Signature | Description |
|------|---------|------|
| `Value` | `protected T Value { get; }` | The wrapped value |
| Constructor | `protected ComparableSimpleValueObject(T value)` | Throws `ArgumentNullException` if `null` is passed |

### Public Members

| Member | Signature | Description |
|------|---------|------|
| `ToString()` | `public override string ToString()` | Returns `Value.ToString()` |
| `explicit operator T` | `public static explicit operator T(ComparableSimpleValueObject<T>? valueObject)` | Explicit conversion (`InvalidOperationException` if `null`) |
| `operator ==` | `public static bool operator ==(ComparableSimpleValueObject<T>?, ComparableSimpleValueObject<T>?)` | Equality operator |
| `operator !=` | `public static bool operator !=(ComparableSimpleValueObject<T>?, ComparableSimpleValueObject<T>?)` | Inequality operator |

### Static Methods

| Method | Signature | Description |
|--------|---------|------|
| `CreateFromValidation<TValueObject>` | `public static Fin<TValueObject> CreateFromValidation<TValueObject>(Validation<Error, T> validation, Func<T, TValueObject> factory) where TValueObject : ComparableSimpleValueObject<T>` | Converts single comparable value Validation to Fin |

### Equality Sealing

Same as `SimpleValueObject<T>`, `Equals(object?)` and `GetHashCode()` are declared as `sealed override`.

### Usage Example

```csharp
public sealed class Priority : ComparableSimpleValueObject<int>
{
    private Priority(int value) : base(value) { }

    public static Fin<Priority> Create(int value) =>
        CreateFromValidation(
            ValidationRules<Priority>.GreaterThanOrEqual(value, 1)
                .ThenLessThanOrEqual(10),
            v => new Priority(v));
}

// Using comparison operators
Priority high = Priority.Create(9).ThrowIfFail();
Priority low = Priority.Create(1).ThrowIfFail();
bool result = high > low; // true
```

---

## Factory Method Pattern

All Value Objects follow the same creation flow.

### Create/Validate Separation

```
Create(rawValue)
  └── Validate(rawValue) → Validation<Error, T>
       └── CreateFromValidation(validation, factory) → Fin<TValueObject>
```

| Method | Return Type | Responsibility |
|--------|----------|------|
| `Validate()` | `Validation<Error, T>` | Performs validation logic only. Supports error accumulation. Reusable in Application Layer |
| `Create()` | `Fin<TValueObject>` | Calls `Validate()` then creates object via `CreateFromValidation()` |
| `CreateFromValidation()` | `Fin<TValueObject>` | Converts `Validation` to `Fin` and applies factory function |

### CreateFromValidation Variants

| Base Class | Signature | Constraint |
|------------|---------|----------|
| `ValueObject` | `CreateFromValidation<TValueObject, TValue>(Validation<Error, TValue>, Func<TValue, TValueObject>)` | `TValueObject : ValueObject` |
| `SimpleValueObject<T>` | `CreateFromValidation<TValueObject>(Validation<Error, T>, Func<T, TValueObject>)` | `TValueObject : SimpleValueObject<T>` |
| `ComparableSimpleValueObject<T>` | `CreateFromValidation<TValueObject>(Validation<Error, T>, Func<T, TValueObject>)` | `TValueObject : ComparableSimpleValueObject<T>` |

`SimpleValueObject<T>` and `ComparableSimpleValueObject<T>` have simplified `CreateFromValidation` with a single type parameter (`TValueObject`). The `ValueObject` version takes an additional `TValue` to accommodate various validation value types for composite Value Objects.

---

## Union Value Objects

Implements the Discriminated Union pattern using records. This is a separate record hierarchy from the class-based Value Object hierarchy.

### IUnionValueObject

Marker interface for Union Value Objects. Inherits `IValueObject`.

```csharp
public interface IUnionValueObject : IValueObject;
```

### UnionValueObject

Base abstract record for pure data unions (no state transitions).

```csharp
[Serializable]
public abstract record UnionValueObject : IUnionValueObject;
```

### UnionValueObject\<TSelf\>

Union record supporting state transitions via CRTP (Curiously Recurring Template Pattern).

```csharp
[Serializable]
public abstract record UnionValueObject<TSelf> : UnionValueObject
    where TSelf : UnionValueObject<TSelf>
```

#### Protected Members

| Member | Signature | Description |
|------|---------|------|
| `TransitionFrom<TSource, TTarget>` | `protected Fin<TTarget> TransitionFrom<TSource, TTarget>(Func<TSource, TTarget> transition, string? message = null) where TTarget : notnull` | Type-safe state transition. Applies transition function if `this` is `TSource`, otherwise returns `InvalidTransition` error |

#### Transition Failure Error

On transition failure, returns `DomainError.For<TSelf>(new DomainErrorType.InvalidTransition(FromState, ToState), ...)`. `FromState` is the current case's type name, and **`ToState`** is the target case's type name.

### [UnionType] Attribute

When applied to an `abstract partial record`, the source generator automatically generates pattern matching methods.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UnionTypeAttribute : Attribute;
```

#### Generated Members

The source generator analyzes internal `sealed record` cases and generates the following members.

| Generated Member | Signature | Description |
|----------|---------|------|
| `Match<TResult>` | `TResult Match<TResult>(Func<Case1, TResult> case1, ...)` | Receives a function for every case and returns a result (exhaustive) |
| `Switch` | `void Switch(Action<Case1> case1, ...)` | Executes an action for every case (exhaustive) |
| `Is{CaseName}` | `bool Is{CaseName}` | Property checking whether it is a specific case |
| `As{CaseName}()` | `{CaseName}? As{CaseName}()` | Safe cast to a specific case (nullable) |

#### Generator Conditions

- The target type must be an `abstract partial record`.
- At least one `sealed record` case must directly inherit from it.
- No code is generated if there are no cases.

### UnreachableCaseException

Exception thrown in the default branch of generated `Match`/`Switch`. Will never be reached at runtime if the sealed hierarchy is complete.

```csharp
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
```

### Usage Example

```csharp
[UnionType]
public abstract partial record OrderStatus : UnionValueObject<OrderStatus>
{
    public sealed record Pending : OrderStatus;
    public sealed record Confirmed(DateTime ConfirmedAt) : OrderStatus;
    public sealed record Shipped(string TrackingNumber) : OrderStatus;
    public sealed record Cancelled(string Reason) : OrderStatus;

    // State transition method
    public Fin<Confirmed> Confirm(DateTime confirmedAt) =>
        TransitionFrom<Pending, Confirmed>(_ => new Confirmed(confirmedAt));
}

// Using auto-generated members from the source generator
OrderStatus status = new OrderStatus.Pending();

// Match - exhaustive pattern matching
string label = status.Match(
    pending:   _ => "Pending",
    confirmed: c => $"Confirmed ({c.ConfirmedAt:d})",
    shipped:   s => $"Shipped ({s.TrackingNumber})",
    cancelled: c => $"Cancelled ({c.Reason})");

// Is property
bool isPending = status.IsPending; // true

// As method
OrderStatus.Pending? asPending = status.AsPending(); // non-null
```

---

## Related Documents

| Document | Description |
|------|------|
| [Value Object Guide](../guides/domain/05a-value-objects) | Value Object design principles, base class selection criteria, Create/Validate separation pattern |
| [Value Object Validation Guide](../guides/domain/05b-value-objects-validation) | Enumeration patterns, Application validation, FluentValidation integration |
| [Union Value Object Guide](../guides/domain/05c-union-value-objects) | Discriminated Union design, state transitions, source generator usage |
| [Validation System Specification](./03-validation) | `TypedValidation`, `ContextualValidation`, `ValidationRules<T>` API |
| [Error System Specification](./04-error-system) | `DomainErrorType.InvalidTransition`, error factory API |
| [Source Generators Specification](./10-source-generators) | `UnionTypeGenerator` detailed behavior, generated code format |
