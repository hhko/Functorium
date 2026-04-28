---
title: "Error System Specification"
---

Functorium's error system consists of **per-layer sealed record hierarchies** (`DomainErrorKind`, `ApplicationErrorKind`, `AdapterErrorKind`) and **per-layer factories** (`DomainError`, `ApplicationError`, `AdapterError`). Common creation logic for the factories is centralized in the internal `LayerErrorCore`, and common overrides for Expected errors are consolidated in `ExpectedErrorBase`. This specification defines the signatures, properties, and error code generation rules for public/internal types.

## Summary

### Key Types

#### Public Types

| Type | Namespace | Description |
|------|-------------|------|
| `ErrorKind` | `Functorium.Abstractions.Errors` | Abstract base record for all layer error types |
| `IHasErrorCode` | `Functorium.Abstractions.Errors` | Error code access interface |
| `ErrorFactory` | `Functorium.Abstractions.Errors` | Expected/Exceptional error creation factory |
| `DomainErrorKind` | `Functorium.Domains.Errors` | Domain error type sealed record hierarchy (10 categories) |
| `DomainError` | `Functorium.Domains.Errors` | Domain error creation factory |
| `ApplicationErrorKind` | `Functorium.Applications.Errors` | Application error type sealed record hierarchy (14 types) |
| `ApplicationError` | `Functorium.Applications.Errors` | Application error creation factory |
| `AdapterErrorKind` | `Functorium.Adapters.Errors` | Adapter error type sealed record hierarchy (20 types) |
| `AdapterError` | `Functorium.Adapters.Errors` | Adapter error creation factory |

#### Internal Types

| Type | Namespace | Description |
|------|-------------|------|
| `ExpectedErrorBase` | `Functorium.Abstractions.Errors` | Base class for common LanguageExt `Error` overrides of 4 Expected error types |
| `ExpectedError` (4 types) | `Functorium.Abstractions.Errors` | Expected errors -- responsible only for value storage, inheriting the rest from base |
| `ExceptionalError` | `Functorium.Abstractions.Errors` | Wraps Exception with error code |
| `LayerErrorCore` | `Functorium.Abstractions.Errors` | Common error code creation logic for all 3 layer factories |
| `ErrorAssertionCore` | `Functorium.Testing.Assertions.Errors` | Common validation logic for 3 layer Assertions |

### Error Code Format

All error codes follow this pattern:

```
{LayerPrefix}.{ContextName}.{ErrorName}
```

| Layer | Prefix | Example |
|--------|--------|------|
| Domain | `Domain` | `Domain.Email.Empty` |
| Application | `Application` | `Application.CreateProductCommand.AlreadyExists` |
| Adapter | `Adapter` | `Adapter.ProductRepository.NotFound` |

### Relationship Diagram

The diagram below shows the flow from the user code entry point (layer factory) through internal error record creation to the observability layer. It reflects the 1.0.0-alpha.4 redesign: the `ErrorKind` abstract base, internal `ErrorFactory`, `ExpectedError` / `ExceptionalError`, and shortened prefix values (`"Domain"` / `"Application"` / `"Adapter"`).

```
[Public API -- User Code Path]

  DomainError.For<Email>(new DomainErrorKind.Empty(), value, msg)
      |                        |
      | (static factory)       | (classification = Kind)
      |                        +--> ErrorKind (abstract base)
      |                                |
      |                                v
      |                             *ErrorKind (per-layer derivation)
      |                                |
      |                                v
      |                             Empty/Null/Custom/... (nested)
      v
  IHasErrorCode { string ErrorCode }   <-- implemented by all errors

                   | (internal delegation)
                   v

[Internal Implementation -- InternalsVisibleTo: Adapters, Testing]

  LayerErrorCore.Create<Email>(ErrorCodePrefixes.Domain, kind, ...)
      |                                 |
      |                                 | (3 internal constants)
      |                                 +--> "Domain" / "Application" / "Adapter"
      v
  ErrorFactory.CreateExpected(prefix, typeName, kind.Name, msg)
      | (exception path)
      +--> ErrorFactory.CreateExceptional(exception, ...)
      v
  ExpectedError / ExceptionalError : LanguageExt.Error
      * ErrorCode : string     <-- "Domain.Email.Empty"
      * NumericCode : int      <-- -1000 (default)
      v
  Serilog Destructurer
      --> ErrorLogFieldNames.{Kind, NumericCode, Message, Inner, ...}
```

> **Note**: The comprehensive redesign landed in 1.0.0-alpha.4 (`ErrorType` -> `ErrorKind`, `ErrorCodeFactory` -> internal `ErrorFactory`, shortened prefix values, etc.) is reflected throughout the body, diagram, and tables. See the release notes for the full migration guide from earlier versions.

---

## Error Code System

### ErrorKind (Abstract Base)

```csharp
namespace Functorium.Abstractions.Errors;

public abstract record ErrorKind
{
    public virtual string Name => GetType().Name;
}
```

**`ErrorKind`** is the common base for all per-layer error types (`DomainErrorKind`, `ApplicationErrorKind`, `AdapterErrorKind`).

| Member | Kind | Description |
|------|------|------|
| `Name` | `virtual string` | Last segment of the error code. Defaults to `GetType().Name` |

> **Layer prefix constants**: `"Domain"` / `"Application"` / `"Adapter"` prefix constants live in the internal `ErrorCodePrefixes` class (namespace `Functorium.Abstractions.Errors`, `internal`) and are not exposed on the public API. Consumers use them indirectly through layer factories such as `DomainError.For<T>(...)`.

### IHasErrorCode

```csharp
namespace Functorium.Abstractions.Errors;

public interface IHasErrorCode
{
    string ErrorCode { get; }
}
```

**`IHasErrorCode`** is an interface for type-safe error code access without reflection. `ExpectedError` and `ExceptionalError` implement this interface.

---

## ErrorCode Types

### ExpectedErrorBase (internal)

```csharp
internal abstract record ExpectedErrorBase(
    string ErrorCode,
    string ErrorMessage,
    int NumericCode = -1000,
    Option<Error> Inner = default) : Error, IHasErrorCode
```

**`ExpectedErrorBase`** is the common base class for the 4 Expected error types (`ExpectedError`, `<T>`, `<T1,T2>`, `<T1,T2,T3>`). It defines all 13 LanguageExt `Error` overrides in one place, eliminating duplication in derived types.

| Member | Kind | Description |
|------|------|------|
| `ErrorCode` | `string` | Error code in `"{Prefix}.{Context}.{ErrorName}"` format |
| `Message` | `override string` | Human-readable error message |
| `Code` | `override int` | Integer error code ID (default `-1000`) |
| `Inner` | `override Option<Error>` | Inner error (default `None`) |
| `ToString()` | `sealed override` | Returns `Message`. `sealed` prevents auto-generation by derived records |
| `ToErrorException()` | `override` | Returns `WrappedErrorExpectedException` |
| `IsExpected` | `bool` | Always `true` |
| `IsExceptional` | `bool` | Always `false` |

> **`sealed override ToString()`**: C# records auto-regenerate `ToString()` in derived classes. `sealed` blocks this to ensure all derived types consistently return `Message`.

### ExpectedError (internal)

```csharp
internal record ExpectedError(
    string ErrorCode,
    string ErrorCurrentValue,
    string ErrorMessage,
    int NumericCode = -1000,
    Option<Error> Inner = default)
    : ExpectedErrorBase(ErrorCode, ErrorMessage, NumericCode, Inner)
```

**`ExpectedError`** represents expected errors such as business rule violations. It inherits common members (`ErrorCode`, `Message`, `Code`, `Inner`, `ToString()`, `IsExpected`, `IsExceptional`) from `ExpectedErrorBase`, and derived types only add the value at error time (`ErrorCurrentValue`).

| Overload | Additional Properties | Description |
|----------|----------|------|
| `ExpectedError` | `ErrorCurrentValue: string` | String value |
| `ExpectedError<T>` | `ErrorCurrentValue: T` | Typed value (1) |
| `ExpectedError<T1, T2>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2` | Typed values (2) |
| `ExpectedError<T1, T2, T3>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2`, `ErrorCurrentValue3: T3` | Typed values (3) |

### ExceptionalError (internal)

```csharp
internal record ExceptionalError : Error, IHasErrorCode
{
    public ExceptionalError(string errorCode, Exception exception);
}
```

**`ExceptionalError`** wraps a system Exception with an error code. `IsExpected = false`, `IsExceptional = true`.

| Property | Type | Description |
|------|------|------|
| `ErrorCode` | `string` | Error code in `"{Prefix}.{Context}.{ErrorName}"` format |
| `Message` | `string` | Extracted from `exception.Message` |
| `Code` | `int` | Extracted from `exception.HResult` |
| `Inner` | `Option<Error>` | Recursively wraps `InnerException` if present |
| `IsExpected` | `bool` | Always `false` |
| `IsExceptional` | `bool` | Always `true` |

### ErrorFactory (internal)

```csharp
namespace Functorium.Abstractions.Errors;

internal static class ErrorFactory
{
    // Expected error creation
    public static Error CreateExpected(string errorCode, string errorCurrentValue, string errorMessage);
    public static Error CreateExpected<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull;
    public static Error CreateExpected<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull;
    public static Error CreateExpected<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
        where T1 : notnull where T2 : notnull where T3 : notnull;

    // Exceptional error creation
    public static Error CreateExceptional(string errorCode, Exception exception);
}
```

**`ErrorFactory`** is an internal static factory that creates `ExpectedError` and `ExceptionalError` instances. External consumers never call it directly; public per-layer factories delegate through it:

```
DomainError.For<T>(DomainErrorKind, ...)                    <- Layer type safety (public API)
  -> LayerErrorCore.Create<T>(ErrorCodePrefixes.Domain, kind, ...)   <- Common error code assembly (internal)
    -> ErrorFactory.CreateExpected(errorCode, ...)                    <- ExpectedError instance creation (internal)
```

`LayerErrorCore` assembles the error code string (`{Prefix}.{Context}.{Kind.Name}`), and `ErrorFactory` creates the final `Error` instance. `[AggressiveInlining]` is applied to all methods so the JIT eliminates delegation calls, resulting in no performance difference.

| Method | Return | Description |
|--------|------|------|
| `CreateExpected(...)` | `Error` | Creates Expected error (4 overloads) |
| `CreateExceptional(...)` | `Error` | Wraps Exception as Exceptional error |

---

## Domain ErrorKind Catalog

```csharp
namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorKind : ErrorKind;
```

**`DomainErrorKind`** is the sealed record hierarchy base for domain layer errors. Classified into 10 categories.

### Existence

| sealed record | Properties | Description |
|---------------|------|------|
| `NotFound` | (none) | Value not found |
| `AlreadyExists` | (none) | Value already exists |
| `Duplicate` | (none) | Duplicate value |
| `Mismatch` | (none) | Value mismatch (e.g., password confirmation) |

```csharp
public sealed record NotFound : DomainErrorKind;
public sealed record AlreadyExists : DomainErrorKind;
public sealed record Duplicate : DomainErrorKind;
public sealed record Mismatch : DomainErrorKind;
```

### Presence

| sealed record | Properties | Description |
|---------------|------|------|
| `Empty` | (none) | Value is empty (null, empty string, empty collection, etc.) |
| `Null` | (none) | Value is null |

```csharp
public sealed record Empty : DomainErrorKind;
public sealed record Null : DomainErrorKind;
```

### Format

| sealed record | Properties | Description |
|---------------|------|------|
| `InvalidFormat` | `string? Pattern` | Value format is invalid. `Pattern` is the expected format pattern |
| `NotUpperCase` | (none) | Value is not uppercase |
| `NotLowerCase` | (none) | Value is not lowercase |

```csharp
public sealed record InvalidFormat(string? Pattern = null) : DomainErrorKind;
public sealed record NotUpperCase : DomainErrorKind;
public sealed record NotLowerCase : DomainErrorKind;
```

### Length

| sealed record | Properties | Description |
|---------------|------|------|
| `TooShort` | `int MinLength` | Value is shorter than minimum length. Default `0` (unspecified) |
| `TooLong` | `int MaxLength` | Value exceeds maximum length. Default `int.MaxValue` (unspecified) |
| `WrongLength` | `int Expected` | Value length does not match expected. Default `0` (unspecified) |

```csharp
public sealed record TooShort(int MinLength = 0) : DomainErrorKind;
public sealed record TooLong(int MaxLength = int.MaxValue) : DomainErrorKind;
public sealed record WrongLength(int Expected = 0) : DomainErrorKind;
```

### Numeric

| sealed record | Properties | Description |
|---------------|------|------|
| `Zero` | (none) | Value is zero |
| `Negative` | (none) | Value is negative |
| `NotPositive` | (none) | Value is not positive (zero or negative) |
| `OutOfRange` | `string? Min`, `string? Max` | Value is outside allowed range |
| `BelowMinimum` | `string? Minimum` | Value is below minimum |
| `AboveMaximum` | `string? Maximum` | Value exceeds maximum |

```csharp
public sealed record Zero : DomainErrorKind;
public sealed record Negative : DomainErrorKind;
public sealed record NotPositive : DomainErrorKind;
public sealed record OutOfRange(string? Min = null, string? Max = null) : DomainErrorKind;
public sealed record BelowMinimum(string? Minimum = null) : DomainErrorKind;
public sealed record AboveMaximum(string? Maximum = null) : DomainErrorKind;
```

### DateTime

| sealed record | Properties | Description |
|---------------|------|------|
| `DefaultDate` | (none) | Date is default value (`DateTime.MinValue`) |
| `NotInPast` | (none) | Date should be in the past but is in the future |
| `NotInFuture` | (none) | Date should be in the future but is in the past |
| `TooLate` | `string? Boundary` | Date is after the boundary (should be before) |
| `TooEarly` | `string? Boundary` | Date is before the boundary (should be after) |

```csharp
public sealed record DefaultDate : DomainErrorKind;
public sealed record NotInPast : DomainErrorKind;
public sealed record NotInFuture : DomainErrorKind;
public sealed record TooLate(string? Boundary = null) : DomainErrorKind;
public sealed record TooEarly(string? Boundary = null) : DomainErrorKind;
```

### Range

| sealed record | Properties | Description |
|---------------|------|------|
| `RangeInverted` | `string? Min`, `string? Max` | Range is inverted (minimum greater than maximum) |
| `RangeEmpty` | `string? Value` | Range is empty (minimum equals maximum) |

```csharp
public sealed record RangeInverted(string? Min = null, string? Max = null) : DomainErrorKind;
public sealed record RangeEmpty(string? Value = null) : DomainErrorKind;
```

### State Transition

| sealed record | Properties | Description |
|---------------|------|------|
| `InvalidTransition` | `string? FromState`, `string? ToState` | Invalid state transition (e.g., `Paid` -> `Active`) |

```csharp
public sealed record InvalidTransition(string? FromState = null, string? ToState = null) : DomainErrorKind;
```

Records the pre- and post-transition states via `FromState` and `ToState`. Preserves transition information as structured data independent of the error message, enabling use in logging/monitoring.

### Custom

```csharp
public abstract record Custom : DomainErrorKind;
```

**`Custom`** is the base class for domain-specific errors that cannot be expressed with standard error types. Define derived sealed records to use.

```csharp
// Define as a nested record inside an Entity
public sealed record InsufficientStock : DomainErrorKind.Custom;

DomainError.For<Inventory>(new InsufficientStock(), currentStock, "Insufficient stock");
// Error code: Domain.Inventory.InsufficientStock
```

---

## Application ErrorKind Catalog

```csharp
namespace Functorium.Applications.Errors;

public abstract record ApplicationErrorKind : ErrorKind;
```

**`ApplicationErrorKind`** is the sealed record hierarchy base for application layer errors.

### Common

| sealed record | Properties | Description |
|---------------|------|------|
| `Empty` | (none) | Value is empty |
| `Null` | (none) | Value is null |
| `NotFound` | (none) | Value not found |
| `AlreadyExists` | (none) | Value already exists |
| `Duplicate` | (none) | Duplicate value |
| `InvalidState` | (none) | Invalid state |
| `Unauthorized` | (none) | Not authenticated |
| `Forbidden` | (none) | Access forbidden |

```csharp
public sealed record Empty : ApplicationErrorKind;
public sealed record Null : ApplicationErrorKind;
public sealed record NotFound : ApplicationErrorKind;
public sealed record AlreadyExists : ApplicationErrorKind;
public sealed record Duplicate : ApplicationErrorKind;
public sealed record InvalidState : ApplicationErrorKind;
public sealed record Unauthorized : ApplicationErrorKind;
public sealed record Forbidden : ApplicationErrorKind;
```

### Validation

| sealed record | Properties | Description |
|---------------|------|------|
| `ValidationFailed` | `string? PropertyName` | Validation failed. `PropertyName` is the failed property name |

```csharp
public sealed record ValidationFailed(string? PropertyName = null) : ApplicationErrorKind;
```

### Business Rules

| sealed record | Properties | Description |
|---------------|------|------|
| `BusinessRuleViolated` | `string? RuleName` | Business rule violated. `RuleName` is the violated rule name |
| `ConcurrencyConflict` | (none) | Concurrency conflict |
| `ResourceLocked` | `string? ResourceName` | Resource locked. `ResourceName` is the locked resource name |
| `OperationCancelled` | (none) | Operation cancelled |
| `InsufficientPermission` | `string? Permission` | Insufficient permission. `Permission` is the required permission |

```csharp
public sealed record BusinessRuleViolated(string? RuleName = null) : ApplicationErrorKind;
public sealed record ConcurrencyConflict : ApplicationErrorKind;
public sealed record ResourceLocked(string? ResourceName = null) : ApplicationErrorKind;
public sealed record OperationCancelled : ApplicationErrorKind;
public sealed record InsufficientPermission(string? Permission = null) : ApplicationErrorKind;
```

### Custom

```csharp
public abstract record Custom : ApplicationErrorKind;
```

```csharp
// Usage example
public sealed record CannotProcess : ApplicationErrorKind.Custom;
```

---

## Adapter ErrorKind Catalog

```csharp
namespace Functorium.Adapters.Errors;

public abstract record AdapterErrorKind : ErrorKind;
```

**`AdapterErrorKind`** is the sealed record hierarchy base for adapter layer errors.

### Common

| sealed record | Properties | Description |
|---------------|------|------|
| `Empty` | (none) | Value is empty |
| `Null` | (none) | Value is null |
| `NotFound` | (none) | Value not found |
| `PartialNotFound` | (none) | Some of the requested IDs were not found |
| `AlreadyExists` | (none) | Value already exists |
| `Duplicate` | (none) | Duplicate value |
| `InvalidState` | (none) | Invalid state |
| `NotConfigured` | (none) | Required configuration is missing |
| `NotSupported` | (none) | Unsupported operation |
| `Unauthorized` | (none) | Not authenticated |
| `Forbidden` | (none) | Access forbidden |

```csharp
public sealed record Empty : AdapterErrorKind;
public sealed record Null : AdapterErrorKind;
public sealed record NotFound : AdapterErrorKind;
public sealed record PartialNotFound : AdapterErrorKind;
public sealed record AlreadyExists : AdapterErrorKind;
public sealed record Duplicate : AdapterErrorKind;
public sealed record InvalidState : AdapterErrorKind;
public sealed record NotConfigured : AdapterErrorKind;
public sealed record NotSupported : AdapterErrorKind;
public sealed record Unauthorized : AdapterErrorKind;
public sealed record Forbidden : AdapterErrorKind;
```

### Pipeline

| sealed record | Properties | Description |
|---------------|------|------|
| `PipelineValidation` | `string? PropertyName` | Pipeline validation failed. `PropertyName` is the failed property name |
| `PipelineException` | (none) | Pipeline exception occurred |

```csharp
public sealed record PipelineValidation(string? PropertyName = null) : AdapterErrorKind;
public sealed record PipelineException : AdapterErrorKind;
```

### External Service

| sealed record | Properties | Description |
|---------------|------|------|
| `ExternalServiceUnavailable` | `string? ServiceName` | External service unavailable. `ServiceName` is the service name |
| `ConnectionFailed` | `string? Target` | Connection failed. `Target` is the connection target |
| `Timeout` | `TimeSpan? Duration` | Timeout. `Duration` is the timeout duration |

```csharp
public sealed record ExternalServiceUnavailable(string? ServiceName = null) : AdapterErrorKind;
public sealed record ConnectionFailed(string? Target = null) : AdapterErrorKind;
public sealed record Timeout(TimeSpan? Duration = null) : AdapterErrorKind;
```

### Data

| sealed record | Properties | Description |
|---------------|------|------|
| `Serialization` | `string? Format` | Serialization failed. `Format` is the serialization format |
| `Deserialization` | `string? Format` | Deserialization failed. `Format` is the deserialization format |
| `DataCorruption` | (none) | Data corruption |

```csharp
public sealed record Serialization(string? Format = null) : AdapterErrorKind;
public sealed record Deserialization(string? Format = null) : AdapterErrorKind;
public sealed record DataCorruption : AdapterErrorKind;
```

### Concurrency

| sealed record | Properties | Description |
|---------------|------|------|
| `ConcurrencyConflict` | (none) | Optimistic concurrency conflict detected during `Update`/`UpdateRange`. Returned when the Aggregate was modified by another operation after it was loaded. Distinguished from `NotFound`. Retry eligibility is the caller's responsibility. |

```csharp
public sealed record ConcurrencyConflict : AdapterErrorKind;
```

> Relationship with `ApplicationErrorKind.ConcurrencyConflict`:
> The Application-layer variant represents a **business-level conflict** raised by use-case logic, while the Adapter-layer variant is **detected by the persistence layer** (EF Core `RowVersion` / `DbUpdateConcurrencyException`, or `affected == 0` with existing ID).

### Custom

```csharp
public abstract record Custom : AdapterErrorKind;
```

```csharp
// Usage example
public sealed record RateLimited : AdapterErrorKind.Custom;
```

---

## Internal Architecture

### LayerErrorCore (internal)

```csharp
namespace Functorium.Abstractions.Errors;

internal static class LayerErrorCore
{
    internal static Error Create<TContext>(string prefix, ErrorKind errorType, string currentValue, string message);
    internal static Error Create<TContext, TValue>(string prefix, ErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error Create<TContext, T1, T2>(...) where T1 : notnull where T2 : notnull;
    internal static Error Create<TContext, T1, T2, T3>(...) where T1 : notnull where T2 : notnull where T3 : notnull;
    internal static Error Create(string prefix, Type contextType, ErrorKind errorType, string currentValue, string message);
    internal static Error ForContext(string prefix, string contextName, ErrorKind errorType, string currentValue, string message);
    internal static Error ForContext<TValue>(string prefix, string contextName, ErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error FromException<TContext>(string prefix, ErrorKind errorType, Exception exception);
}
```

**`LayerErrorCore`** is the common implementation for the 3 layer factories (`DomainError`, `ApplicationError`, `AdapterError`). It assembles the error code string `{prefix}.{typeof(TContext).Name}.{kind.Name}` and delegates to `ErrorFactory`.

**Design principle**: Public factories maintain per-layer type parameters (`DomainErrorKind`, `ApplicationErrorKind`, etc.) to ensure **compile-time safety**. `LayerErrorCore` receives the base type `ErrorKind` to **eliminate implementation duplication**. `[AggressiveInlining]` is applied to all methods so the JIT inlines delegation calls.

```csharp
// Compile-time safety example
DomainError.For<Email>(new DomainErrorKind.Empty(), ...)       // Compiles OK
DomainError.For<Email>(new AdapterErrorKind.Timeout(), ...)    // CS1503
```

### ErrorAssertionCore (internal)

```csharp
namespace Functorium.Testing.Assertions.Errors;

internal static class ErrorAssertionCore
{
    // Error -- ErrorCode validation, value validation (1~3), Exceptional validation
    internal static void ShouldBeError<TContext>(Error error, string prefix, string errorName);
    internal static void ShouldBeError<TContext, TValue>(Error error, string prefix, string errorName, TValue expectedValue);
    internal static void ShouldBeExceptionalError<TContext>(Error error, string prefix, string errorName);

    // Fin<T> -- failure state + ErrorCode validation
    internal static void ShouldBeFinError<TContext, T>(Fin<T> fin, string prefix, string errorName);

    // Validation<Error, T> -- error contains/only/multiple validation
    internal static void ShouldHaveError<TContext, T>(Validation<Error, T> validation, string prefix, string errorName);
    internal static void ShouldHaveOnlyError<TContext, T>(Validation<Error, T> validation, string prefix, string errorName);
    internal static void ShouldHaveErrors<TContext, T>(Validation<Error, T> validation, string prefix, params string[] errorNames);
}
```

**`ErrorAssertionCore`** is the common validation logic for the 3 layer Assertions (`DomainErrorAssertions`, `ApplicationErrorAssertions`, `AdapterErrorAssertions`). It provides error code assembly (`{prefix}.{typeof(TContext).Name}.{errorName}`), `ExpectedError<T>` type casting, and value comparison. Per-layer Assertions are thin wrappers that bind only the prefix and error type.

---

## Factory API

### DomainError

```csharp
namespace Functorium.Domains.Errors;

public static class DomainError
{
    public static Error For<TDomain>(DomainErrorKind errorType, string currentValue, string message);
    public static Error For<TDomain, TValue>(DomainErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TDomain, T1, T2>(DomainErrorKind errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TDomain, T1, T2, T3>(DomainErrorKind errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**Error code format:** `Domain.{typeof(TDomain).Name}.{kind.Name}`

| Overload | Value Parameters | Description |
|----------|-----------|------|
| `For<TDomain>(...)` | `string currentValue` | Default string value |
| `For<TDomain, TValue>(...)` | `TValue currentValue` | Generic single value |
| `For<TDomain, T1, T2>(...)` | `T1 value1, T2 value2` | Generic 2 values |
| `For<TDomain, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | Generic 3 values |

**Usage examples:**

```csharp
using static Functorium.Domains.Errors.DomainErrorKind;

// Basic usage
DomainError.For<Email>(new Empty(), "", "Email cannot be empty");
// Error code: Domain.Email.Empty

// Error type with properties
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password is too short");
// Error code: Domain.Password.TooShort

// State transition error
DomainError.For<Order>(new InvalidTransition(FromState: "Paid", ToState: "Active"), orderId, "Invalid state transition");
// Error code: Domain.Order.InvalidTransition

// Custom error
DomainError.For<Currency>(new Unsupported(), value, "Unsupported currency");
// Error code: Domain.Currency.Unsupported
```

### ApplicationError

```csharp
namespace Functorium.Applications.Errors;

public static class ApplicationError
{
    public static Error For<TUsecase>(ApplicationErrorKind errorType, string currentValue, string message);
    public static Error For<TUsecase, TValue>(ApplicationErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TUsecase, T1, T2>(ApplicationErrorKind errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TUsecase, T1, T2, T3>(ApplicationErrorKind errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**Error code format:** `Application.{typeof(TUsecase).Name}.{kind.Name}`

**Usage examples:**

```csharp
using static Functorium.Applications.Errors.ApplicationErrorKind;

ApplicationError.For<CreateProductCommand>(new AlreadyExists(), productId, "Already exists");
// Error code: Application.CreateProductCommand.AlreadyExists

ApplicationError.For<UpdateOrderCommand>(new ValidationFailed("Quantity"), value, "Quantity must be positive");
// Error code: Application.UpdateOrderCommand.ValidationFailed
```


### AdapterError

```csharp
namespace Functorium.Adapters.Errors;

public static class AdapterError
{
    public static Error For<TAdapter>(AdapterErrorKind errorType, string currentValue, string message);
    public static Error For(Type adapterType, AdapterErrorKind errorType, string currentValue, string message);
    public static Error For<TAdapter, TValue>(AdapterErrorKind errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TAdapter, T1, T2>(AdapterErrorKind errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TAdapter, T1, T2, T3>(AdapterErrorKind errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
    public static Error FromException<TAdapter>(AdapterErrorKind errorType, Exception exception);
}
```

**Error code format:** `Adapter.{typeof(TAdapter).Name}.{kind.Name}`

| Overload | Value Parameters | Description |
|----------|-----------|------|
| `For<TAdapter>(...)` | `string currentValue` | Default string value |
| `For(Type, ...)` | `string currentValue` | Specifies adapter via runtime Type (for `GetType()` in base classes) |
| `For<TAdapter, TValue>(...)` | `TValue currentValue` | Generic single value |
| `For<TAdapter, T1, T2>(...)` | `T1 value1, T2 value2` | Generic 2 values |
| `For<TAdapter, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | Generic 3 values |
| `FromException<TAdapter>(...)` | `Exception exception` | Wraps Exception as Exceptional error |

**Usage examples:**

```csharp
using static Functorium.Adapters.Errors.AdapterErrorKind;

// Expected error
AdapterError.For<ProductRepository>(new NotFound(), id, "Product not found");
// Error code: Adapter.ProductRepository.NotFound

// Pipeline error
AdapterError.For<UsecaseValidationPipeline>(new PipelineValidation("PropertyName"), value, "Validation failed");
// Error code: Adapter.UsecaseValidationPipeline.PipelineValidation

// Exception wrapping
AdapterError.FromException<UsecaseExceptionPipeline>(new PipelineException(), exception);
// Error code: Adapter.UsecaseExceptionPipeline.PipelineException (Exceptional)

// Runtime Type usage
AdapterError.For(GetType(), new ConnectionFailed("DB"), connectionString, "Connection failed");
// Error code: Adapter.{ActualTypeName}.ConnectionFailed
```

---

## Related Documents

| Document | Description |
|------|------|
| [Error System: Fundamentals and Naming](../guides/domain/08a-error-system) | Error handling principles, Fin patterns, naming rules R1~R8 |
| [Error System: Domain/Application/Event](../guides/domain/08b-error-system-domain-app) | Domain, Application, Event error detailed guide |
| [Error System: Adapter and Testing](../guides/domain/08c-error-system-adapter-testing) | Adapter error and test pattern guide |
| [Validation System Specification](../03-validation) | TypedValidation, ContextualValidation specification |
