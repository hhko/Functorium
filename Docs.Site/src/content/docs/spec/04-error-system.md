---
title: "Error System Specification"
---

Functorium's error system consists of **per-layer sealed record hierarchies** (`DomainErrorType`, `ApplicationErrorType`, `AdapterErrorType`) and **per-layer factories** (`DomainError`, `ApplicationError`, `EventError`, `AdapterError`). Common creation logic for the factories is centralized in the internal `LayerErrorCore`, and common overrides for Expected errors are consolidated in `ErrorCodeExpectedBase`. This specification defines the signatures, properties, and error code generation rules for public/internal types.

## Summary

### Key Types

#### Public Types

| Type | Namespace | Description |
|------|-------------|------|
| `ErrorType` | `Functorium.Abstractions.Errors` | Abstract base record for all layer error types |
| `IHasErrorCode` | `Functorium.Abstractions.Errors` | Error code access interface |
| `ErrorCodeFactory` | `Functorium.Abstractions.Errors` | Expected/Exceptional error creation factory |
| `DomainErrorType` | `Functorium.Domains.Errors` | Domain error type sealed record hierarchy (10 categories) |
| `DomainError` | `Functorium.Domains.Errors` | Domain error creation factory |
| `ApplicationErrorType` | `Functorium.Applications.Errors` | Application error type sealed record hierarchy (14 types) |
| `ApplicationError` | `Functorium.Applications.Errors` | Application error creation factory |
| `EventErrorType` | `Functorium.Applications.Errors` | Event error type sealed record hierarchy (4 types) |
| `EventError` | `Functorium.Applications.Errors` | Event error creation factory |
| `AdapterErrorType` | `Functorium.Adapters.Errors` | Adapter error type sealed record hierarchy (20 types) |
| `AdapterError` | `Functorium.Adapters.Errors` | Adapter error creation factory |

#### Internal Types

| Type | Namespace | Description |
|------|-------------|------|
| `ErrorCodeExpectedBase` | `Functorium.Abstractions.Errors` | Base class for common LanguageExt `Error` overrides of 4 Expected error types |
| `ErrorCodeExpected` (4 types) | `Functorium.Abstractions.Errors` | Expected errors -- responsible only for value storage, inheriting the rest from base |
| `ErrorCodeExceptional` | `Functorium.Abstractions.Errors` | Wraps Exception with error code |
| `LayerErrorCore` | `Functorium.Abstractions.Errors` | Common error code creation logic for all 4 layer factories |
| `ErrorAssertionCore` | `Functorium.Testing.Assertions.Errors` | Common validation logic for 3 layer Assertions |

### Error Code Format

All error codes follow this pattern:

```
{LayerPrefix}.{ContextName}.{ErrorName}
```

| Layer | Prefix | Example |
|--------|--------|------|
| Domain | `DomainErrors` | `DomainErrors.Email.Empty` |
| Application | `ApplicationErrors` | `ApplicationErrors.CreateProductCommand.AlreadyExists` |
| Adapter | `AdapterErrors` | `AdapterErrors.ProductRepository.NotFound` |

### Relationship Diagram (1.0.0-alpha.4 Target State)

The diagram below shows the flow from the user code entry point (layer factory), through internal error record creation, to the observability layer. It reflects the **upcoming 1.0.0-alpha.4 naming** (ErrorType -> ErrorKind, ErrorCodeFactory -> ErrorFactory, etc.) and the shortened prefix values ("Domain" / "Application" / "Adapter").

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

> **Note**: The type signatures and property descriptions in the rest of this document reflect the current (1.0.0-alpha.3) code. The diagram above is a roadmap preview of the target state after the comprehensive redesign; the names it uses (`ErrorKind`, `ErrorFactory`, `ExpectedError`, shortened prefixes, etc.) take effect in 1.0.0-alpha.4. See the release notes for full change details.

---

## Error Code System

### ErrorType (Abstract Base)

```csharp
namespace Functorium.Abstractions.Errors;

public abstract record ErrorType
{
    public const string DomainErrorsPrefix = "DomainErrors";
    public const string ApplicationErrorsPrefix = "ApplicationErrors";
    public const string AdapterErrorsPrefix = "AdapterErrors";

    public virtual string ErrorName => GetType().Name;
}
```

**`ErrorType`** is the common base for all per-layer error types (`DomainErrorType`, `ApplicationErrorType`, `AdapterErrorType`).

| Member | Kind | Description |
|------|------|------|
| `DomainErrorsPrefix` | `const string` | Domain error code prefix `"DomainErrors"` |
| `ApplicationErrorsPrefix` | `const string` | Application error code prefix `"ApplicationErrors"` |
| `AdapterErrorsPrefix` | `const string` | Adapter error code prefix `"AdapterErrors"` |
| `ErrorName` | `virtual string` | Last segment of the error code. Defaults to `GetType().Name` |

### IHasErrorCode

```csharp
namespace Functorium.Abstractions.Errors;

public interface IHasErrorCode
{
    string ErrorCode { get; }
}
```

**`IHasErrorCode`** is an interface for type-safe error code access without reflection. `ErrorCodeExpected` and `ErrorCodeExceptional` implement this interface.

---

## ErrorCode Types

### ErrorCodeExpectedBase (internal)

```csharp
internal abstract record ErrorCodeExpectedBase(
    string ErrorCode,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default) : Error, IHasErrorCode
```

**`ErrorCodeExpectedBase`** is the common base class for the 4 Expected error types (`ErrorCodeExpected`, `<T>`, `<T1,T2>`, `<T1,T2,T3>`). It defines all 13 LanguageExt `Error` overrides in one place, eliminating duplication in derived types.

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

### ErrorCodeExpected (internal)

```csharp
internal record ErrorCodeExpected(
    string ErrorCode,
    string ErrorCurrentValue,
    string ErrorMessage,
    int ErrorCodeId = -1000,
    Option<Error> Inner = default)
    : ErrorCodeExpectedBase(ErrorCode, ErrorMessage, ErrorCodeId, Inner)
```

**`ErrorCodeExpected`** represents expected errors such as business rule violations. It inherits common members (`ErrorCode`, `Message`, `Code`, `Inner`, `ToString()`, `IsExpected`, `IsExceptional`) from `ErrorCodeExpectedBase`, and derived types only add the value at error time (`ErrorCurrentValue`).

| Overload | Additional Properties | Description |
|----------|----------|------|
| `ErrorCodeExpected` | `ErrorCurrentValue: string` | String value |
| `ErrorCodeExpected<T>` | `ErrorCurrentValue: T` | Typed value (1) |
| `ErrorCodeExpected<T1, T2>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2` | Typed values (2) |
| `ErrorCodeExpected<T1, T2, T3>` | `ErrorCurrentValue1: T1`, `ErrorCurrentValue2: T2`, `ErrorCurrentValue3: T3` | Typed values (3) |

### ErrorCodeExceptional (internal)

```csharp
internal record ErrorCodeExceptional : Error, IHasErrorCode
{
    public ErrorCodeExceptional(string errorCode, Exception exception);
}
```

**`ErrorCodeExceptional`** wraps a system Exception with an error code. `IsExpected = false`, `IsExceptional = true`.

| Property | Type | Description |
|------|------|------|
| `ErrorCode` | `string` | Error code in `"{Prefix}.{Context}.{ErrorName}"` format |
| `Message` | `string` | Extracted from `exception.Message` |
| `Code` | `int` | Extracted from `exception.HResult` |
| `Inner` | `Option<Error>` | Recursively wraps `InnerException` if present |
| `IsExpected` | `bool` | Always `false` |
| `IsExceptional` | `bool` | Always `true` |

### ErrorCodeFactory

```csharp
namespace Functorium.Abstractions.Errors;

public static class ErrorCodeFactory
{
    // Expected error creation
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage);
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull;
    public static Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull;
    public static Error Create<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
        where T1 : notnull where T2 : notnull where T3 : notnull;

    // Exceptional error creation
    public static Error CreateFromException(string errorCode, Exception exception);

    // Error code composition
    public static string Format(params string[] parts);
}
```

**`ErrorCodeFactory`** is a static factory that creates `ErrorCodeExpected` and `ErrorCodeExceptional` instances. Per-layer factories create errors through the following flow:

```
DomainError.For<T>(DomainErrorType, ...)        <- Layer type safety (public API)
  -> LayerErrorCore.Create<T>(prefix, ErrorType, ...)  <- Common error code assembly (internal)
    -> ErrorCodeFactory.Create(errorCode, ...)          <- ErrorCodeExpected instance creation (internal)
```

`LayerErrorCore` assembles the error code string (`{Prefix}.{Context}.{ErrorName}`), and `ErrorCodeFactory` creates the final `Error` instance. `[AggressiveInlining]` is applied to all methods so the JIT eliminates delegation calls, resulting in no performance difference.

| Method | Return | Description |
|--------|------|------|
| `Create(...)` | `Error` | Creates Expected error (4 overloads) |
| `CreateFromException(...)` | `Error` | Wraps Exception as Exceptional error |
| `Format(...)` | `string` | Joins string array with `'.'` to generate error code |

---

## Domain ErrorType Catalog

```csharp
namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType : ErrorType;
```

**`DomainErrorType`** is the sealed record hierarchy base for domain layer errors. Classified into 10 categories.

### Existence

| sealed record | Properties | Description |
|---------------|------|------|
| `NotFound` | (none) | Value not found |
| `AlreadyExists` | (none) | Value already exists |
| `Duplicate` | (none) | Duplicate value |
| `Mismatch` | (none) | Value mismatch (e.g., password confirmation) |

```csharp
public sealed record NotFound : DomainErrorType;
public sealed record AlreadyExists : DomainErrorType;
public sealed record Duplicate : DomainErrorType;
public sealed record Mismatch : DomainErrorType;
```

### Presence

| sealed record | Properties | Description |
|---------------|------|------|
| `Empty` | (none) | Value is empty (null, empty string, empty collection, etc.) |
| `Null` | (none) | Value is null |

```csharp
public sealed record Empty : DomainErrorType;
public sealed record Null : DomainErrorType;
```

### Format

| sealed record | Properties | Description |
|---------------|------|------|
| `InvalidFormat` | `string? Pattern` | Value format is invalid. `Pattern` is the expected format pattern |
| `NotUpperCase` | (none) | Value is not uppercase |
| `NotLowerCase` | (none) | Value is not lowercase |

```csharp
public sealed record InvalidFormat(string? Pattern = null) : DomainErrorType;
public sealed record NotUpperCase : DomainErrorType;
public sealed record NotLowerCase : DomainErrorType;
```

### Length

| sealed record | Properties | Description |
|---------------|------|------|
| `TooShort` | `int MinLength` | Value is shorter than minimum length. Default `0` (unspecified) |
| `TooLong` | `int MaxLength` | Value exceeds maximum length. Default `int.MaxValue` (unspecified) |
| `WrongLength` | `int Expected` | Value length does not match expected. Default `0` (unspecified) |

```csharp
public sealed record TooShort(int MinLength = 0) : DomainErrorType;
public sealed record TooLong(int MaxLength = int.MaxValue) : DomainErrorType;
public sealed record WrongLength(int Expected = 0) : DomainErrorType;
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
public sealed record Zero : DomainErrorType;
public sealed record Negative : DomainErrorType;
public sealed record NotPositive : DomainErrorType;
public sealed record OutOfRange(string? Min = null, string? Max = null) : DomainErrorType;
public sealed record BelowMinimum(string? Minimum = null) : DomainErrorType;
public sealed record AboveMaximum(string? Maximum = null) : DomainErrorType;
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
public sealed record DefaultDate : DomainErrorType;
public sealed record NotInPast : DomainErrorType;
public sealed record NotInFuture : DomainErrorType;
public sealed record TooLate(string? Boundary = null) : DomainErrorType;
public sealed record TooEarly(string? Boundary = null) : DomainErrorType;
```

### Range

| sealed record | Properties | Description |
|---------------|------|------|
| `RangeInverted` | `string? Min`, `string? Max` | Range is inverted (minimum greater than maximum) |
| `RangeEmpty` | `string? Value` | Range is empty (minimum equals maximum) |

```csharp
public sealed record RangeInverted(string? Min = null, string? Max = null) : DomainErrorType;
public sealed record RangeEmpty(string? Value = null) : DomainErrorType;
```

### State Transition

| sealed record | Properties | Description |
|---------------|------|------|
| `InvalidTransition` | `string? FromState`, `string? ToState` | Invalid state transition (e.g., `Paid` -> `Active`) |

```csharp
public sealed record InvalidTransition(string? FromState = null, string? ToState = null) : DomainErrorType;
```

Records the pre- and post-transition states via `FromState` and `ToState`. Preserves transition information as structured data independent of the error message, enabling use in logging/monitoring.

### Custom

```csharp
public abstract record Custom : DomainErrorType;
```

**`Custom`** is the base class for domain-specific errors that cannot be expressed with standard error types. Define derived sealed records to use.

```csharp
// Define as a nested record inside an Entity
public sealed record InsufficientStock : DomainErrorType.Custom;

DomainError.For<Inventory>(new InsufficientStock(), currentStock, "Insufficient stock");
// Error code: DomainErrors.Inventory.InsufficientStock
```

---

## Application ErrorType Catalog

```csharp
namespace Functorium.Applications.Errors;

public abstract record ApplicationErrorType : ErrorType;
```

**`ApplicationErrorType`** is the sealed record hierarchy base for application layer errors.

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
public sealed record Empty : ApplicationErrorType;
public sealed record Null : ApplicationErrorType;
public sealed record NotFound : ApplicationErrorType;
public sealed record AlreadyExists : ApplicationErrorType;
public sealed record Duplicate : ApplicationErrorType;
public sealed record InvalidState : ApplicationErrorType;
public sealed record Unauthorized : ApplicationErrorType;
public sealed record Forbidden : ApplicationErrorType;
```

### Validation

| sealed record | Properties | Description |
|---------------|------|------|
| `ValidationFailed` | `string? PropertyName` | Validation failed. `PropertyName` is the failed property name |

```csharp
public sealed record ValidationFailed(string? PropertyName = null) : ApplicationErrorType;
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
public sealed record BusinessRuleViolated(string? RuleName = null) : ApplicationErrorType;
public sealed record ConcurrencyConflict : ApplicationErrorType;
public sealed record ResourceLocked(string? ResourceName = null) : ApplicationErrorType;
public sealed record OperationCancelled : ApplicationErrorType;
public sealed record InsufficientPermission(string? Permission = null) : ApplicationErrorType;
```

### Custom

```csharp
public abstract record Custom : ApplicationErrorType;
```

```csharp
// Usage example
public sealed record CannotProcess : ApplicationErrorType.Custom;
```

### EventErrorType

```csharp
namespace Functorium.Applications.Errors;

public abstract record EventErrorType : ErrorType;
```

**`EventErrorType`** is the error type for domain event publishing/handling errors.

| sealed record | Properties | Description |
|---------------|------|------|
| `PublishFailed` | (none) | Event publish failed |
| `HandlerFailed` | (none) | Event handler execution failed |
| `InvalidEventType` | (none) | Event type is invalid |
| `PublishCancelled` | (none) | Event publish cancelled |

```csharp
public sealed record PublishFailed : EventErrorType;
public sealed record HandlerFailed : EventErrorType;
public sealed record InvalidEventType : EventErrorType;
public sealed record PublishCancelled : EventErrorType;
```

**Custom extension:**

```csharp
public abstract record Custom : EventErrorType;

// Usage example
public sealed record RetryExhausted : EventErrorType.Custom;
```

---

## Adapter ErrorType Catalog

```csharp
namespace Functorium.Adapters.Errors;

public abstract record AdapterErrorType : ErrorType;
```

**`AdapterErrorType`** is the sealed record hierarchy base for adapter layer errors.

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
public sealed record Empty : AdapterErrorType;
public sealed record Null : AdapterErrorType;
public sealed record NotFound : AdapterErrorType;
public sealed record PartialNotFound : AdapterErrorType;
public sealed record AlreadyExists : AdapterErrorType;
public sealed record Duplicate : AdapterErrorType;
public sealed record InvalidState : AdapterErrorType;
public sealed record NotConfigured : AdapterErrorType;
public sealed record NotSupported : AdapterErrorType;
public sealed record Unauthorized : AdapterErrorType;
public sealed record Forbidden : AdapterErrorType;
```

### Pipeline

| sealed record | Properties | Description |
|---------------|------|------|
| `PipelineValidation` | `string? PropertyName` | Pipeline validation failed. `PropertyName` is the failed property name |
| `PipelineException` | (none) | Pipeline exception occurred |

```csharp
public sealed record PipelineValidation(string? PropertyName = null) : AdapterErrorType;
public sealed record PipelineException : AdapterErrorType;
```

### External Service

| sealed record | Properties | Description |
|---------------|------|------|
| `ExternalServiceUnavailable` | `string? ServiceName` | External service unavailable. `ServiceName` is the service name |
| `ConnectionFailed` | `string? Target` | Connection failed. `Target` is the connection target |
| `Timeout` | `TimeSpan? Duration` | Timeout. `Duration` is the timeout duration |

```csharp
public sealed record ExternalServiceUnavailable(string? ServiceName = null) : AdapterErrorType;
public sealed record ConnectionFailed(string? Target = null) : AdapterErrorType;
public sealed record Timeout(TimeSpan? Duration = null) : AdapterErrorType;
```

### Data

| sealed record | Properties | Description |
|---------------|------|------|
| `Serialization` | `string? Format` | Serialization failed. `Format` is the serialization format |
| `Deserialization` | `string? Format` | Deserialization failed. `Format` is the deserialization format |
| `DataCorruption` | (none) | Data corruption |

```csharp
public sealed record Serialization(string? Format = null) : AdapterErrorType;
public sealed record Deserialization(string? Format = null) : AdapterErrorType;
public sealed record DataCorruption : AdapterErrorType;
```

### Concurrency

| sealed record | Properties | Description |
|---------------|------|------|
| `ConcurrencyConflict` | (none) | Optimistic concurrency conflict detected during `Update`/`UpdateRange`. Returned when the Aggregate was modified by another operation after it was loaded. Distinguished from `NotFound`. Retry eligibility is the caller's responsibility. |

```csharp
public sealed record ConcurrencyConflict : AdapterErrorType;
```

> Relationship with `ApplicationErrorType.ConcurrencyConflict`:
> The Application-layer variant represents a **business-level conflict** raised by use-case logic, while the Adapter-layer variant is **detected by the persistence layer** (EF Core `RowVersion` / `DbUpdateConcurrencyException`, or `affected == 0` with existing ID).

### Custom

```csharp
public abstract record Custom : AdapterErrorType;
```

```csharp
// Usage example
public sealed record RateLimited : AdapterErrorType.Custom;
```

---

## Internal Architecture

### LayerErrorCore (internal)

```csharp
namespace Functorium.Abstractions.Errors;

internal static class LayerErrorCore
{
    internal static Error Create<TContext>(string prefix, ErrorType errorType, string currentValue, string message);
    internal static Error Create<TContext, TValue>(string prefix, ErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error Create<TContext, T1, T2>(...) where T1 : notnull where T2 : notnull;
    internal static Error Create<TContext, T1, T2, T3>(...) where T1 : notnull where T2 : notnull where T3 : notnull;
    internal static Error Create(string prefix, Type contextType, ErrorType errorType, string currentValue, string message);
    internal static Error ForContext(string prefix, string contextName, ErrorType errorType, string currentValue, string message);
    internal static Error ForContext<TValue>(string prefix, string contextName, ErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    internal static Error FromException<TContext>(string prefix, ErrorType errorType, Exception exception);
}
```

**`LayerErrorCore`** is the common implementation for the 4 layer factories (`DomainError`, `ApplicationError`, `EventError`, `AdapterError`). It assembles the error code string `{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}` and delegates to `ErrorCodeFactory`.

**Design principle**: Public factories maintain per-layer type parameters (`DomainErrorType`, `ApplicationErrorType`, etc.) to ensure **compile-time safety**. `LayerErrorCore` receives the base type `ErrorType` to **eliminate implementation duplication**. `[AggressiveInlining]` is applied to all methods so the JIT inlines delegation calls.

```csharp
// Compile-time safety example
DomainError.For<Email>(new DomainErrorType.Empty(), ...)       // Compiles OK
DomainError.For<Email>(new AdapterErrorType.Timeout(), ...)    // CS1503
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

**`ErrorAssertionCore`** is the common validation logic for the 3 layer Assertions (`DomainErrorAssertions`, `ApplicationErrorAssertions`, `AdapterErrorAssertions`). It provides error code assembly (`{prefix}.{typeof(TContext).Name}.{errorName}`), `ErrorCodeExpected<T>` type casting, and value comparison. Per-layer Assertions are thin wrappers that bind only the prefix and error type.

---

## Factory API

### DomainError

```csharp
namespace Functorium.Domains.Errors;

public static class DomainError
{
    public static Error For<TDomain>(DomainErrorType errorType, string currentValue, string message);
    public static Error For<TDomain, TValue>(DomainErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TDomain, T1, T2>(DomainErrorType errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TDomain, T1, T2, T3>(DomainErrorType errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**Error code format:** `DomainErrors.{typeof(TDomain).Name}.{errorType.ErrorName}`

| Overload | Value Parameters | Description |
|----------|-----------|------|
| `For<TDomain>(...)` | `string currentValue` | Default string value |
| `For<TDomain, TValue>(...)` | `TValue currentValue` | Generic single value |
| `For<TDomain, T1, T2>(...)` | `T1 value1, T2 value2` | Generic 2 values |
| `For<TDomain, T1, T2, T3>(...)` | `T1 value1, T2 value2, T3 value3` | Generic 3 values |

**Usage examples:**

```csharp
using static Functorium.Domains.Errors.DomainErrorType;

// Basic usage
DomainError.For<Email>(new Empty(), "", "Email cannot be empty");
// Error code: DomainErrors.Email.Empty

// Error type with properties
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password is too short");
// Error code: DomainErrors.Password.TooShort

// State transition error
DomainError.For<Order>(new InvalidTransition(FromState: "Paid", ToState: "Active"), orderId, "Invalid state transition");
// Error code: DomainErrors.Order.InvalidTransition

// Custom error
DomainError.For<Currency>(new Unsupported(), value, "Unsupported currency");
// Error code: DomainErrors.Currency.Unsupported
```

### ApplicationError

```csharp
namespace Functorium.Applications.Errors;

public static class ApplicationError
{
    public static Error For<TUsecase>(ApplicationErrorType errorType, string currentValue, string message);
    public static Error For<TUsecase, TValue>(ApplicationErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TUsecase, T1, T2>(ApplicationErrorType errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TUsecase, T1, T2, T3>(ApplicationErrorType errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
}
```

**Error code format:** `ApplicationErrors.{typeof(TUsecase).Name}.{errorType.ErrorName}`

**Usage examples:**

```csharp
using static Functorium.Applications.Errors.ApplicationErrorType;

ApplicationError.For<CreateProductCommand>(new AlreadyExists(), productId, "Already exists");
// Error code: ApplicationErrors.CreateProductCommand.AlreadyExists

ApplicationError.For<UpdateOrderCommand>(new ValidationFailed("Quantity"), value, "Quantity must be positive");
// Error code: ApplicationErrors.UpdateOrderCommand.ValidationFailed
```

### EventError

```csharp
namespace Functorium.Applications.Errors;

public static class EventError
{
    public static Error For<TPublisher>(EventErrorType errorType, string currentValue, string message);
    public static Error For<TPublisher, TValue>(EventErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error FromException<TPublisher>(Exception exception);
    public static Error FromException<TPublisher>(EventErrorType errorType, Exception exception);
}
```

**Error code format:** `ApplicationErrors.{typeof(TPublisher).Name}.{errorType.ErrorName}`

**`EventError`** shares the `ApplicationErrors` prefix and represents event publishing/handling failures.

| Method | Description |
|--------|------|
| `For<TPublisher>(...)` | Creates Expected error |
| `For<TPublisher, TValue>(...)` | Creates Expected error with generic value |
| `FromException<TPublisher>(exception)` | Wraps exception as `PublishFailed` type Exceptional error |
| `FromException<TPublisher>(errorType, exception)` | Wraps exception as specified type Exceptional error |

**Usage examples:**

```csharp
using static Functorium.Applications.Errors.EventErrorType;

EventError.For<DomainEventPublisher>(new PublishFailed(), eventType, "Event publishing failed");
// Error code: ApplicationErrors.DomainEventPublisher.PublishFailed

EventError.FromException<DomainEventPublisher>(exception);
// Error code: ApplicationErrors.DomainEventPublisher.PublishFailed (Exceptional)
```

### AdapterError

```csharp
namespace Functorium.Adapters.Errors;

public static class AdapterError
{
    public static Error For<TAdapter>(AdapterErrorType errorType, string currentValue, string message);
    public static Error For(Type adapterType, AdapterErrorType errorType, string currentValue, string message);
    public static Error For<TAdapter, TValue>(AdapterErrorType errorType, TValue currentValue, string message)
        where TValue : notnull;
    public static Error For<TAdapter, T1, T2>(AdapterErrorType errorType, T1 value1, T2 value2, string message)
        where T1 : notnull where T2 : notnull;
    public static Error For<TAdapter, T1, T2, T3>(AdapterErrorType errorType, T1 value1, T2 value2, T3 value3, string message)
        where T1 : notnull where T2 : notnull where T3 : notnull;
    public static Error FromException<TAdapter>(AdapterErrorType errorType, Exception exception);
}
```

**Error code format:** `AdapterErrors.{typeof(TAdapter).Name}.{errorType.ErrorName}`

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
using static Functorium.Adapters.Errors.AdapterErrorType;

// Expected error
AdapterError.For<ProductRepository>(new NotFound(), id, "Product not found");
// Error code: AdapterErrors.ProductRepository.NotFound

// Pipeline error
AdapterError.For<UsecaseValidationPipeline>(new PipelineValidation("PropertyName"), value, "Validation failed");
// Error code: AdapterErrors.UsecaseValidationPipeline.PipelineValidation

// Exception wrapping
AdapterError.FromException<UsecaseExceptionPipeline>(new PipelineException(), exception);
// Error code: AdapterErrors.UsecaseExceptionPipeline.PipelineException (Exceptional)

// Runtime Type usage
AdapterError.For(GetType(), new ConnectionFailed("DB"), connectionString, "Connection failed");
// Error code: AdapterErrors.{ActualTypeName}.ConnectionFailed
```

---

## Related Documents

| Document | Description |
|------|------|
| [Error System: Fundamentals and Naming](../guides/domain/08a-error-system) | Error handling principles, Fin patterns, naming rules R1~R8 |
| [Error System: Domain/Application/Event](../guides/domain/08b-error-system-domain-app) | Domain, Application, Event error detailed guide |
| [Error System: Adapter and Testing](../guides/domain/08c-error-system-adapter-testing) | Adapter error and test pattern guide |
| [Validation System Specification](../03-validation) | TypedValidation, ContextualValidation specification |
