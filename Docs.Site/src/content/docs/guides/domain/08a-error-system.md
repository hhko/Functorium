---
title: "Error System: Fundamentals and Naming"
---

This document covers the fundamental principles of error handling, the Fin pattern, and error naming rules. For Domain/Application/Event errors, see [08b-error-system-domain-app.md](../08b-error-system-domain-app); for Adapter errors and test patterns, see [08c-error-system-adapter-testing.md](../08c-error-system-adapter-testing).

## Introduction

"How should the return type be designed when an Entity method can fail?"
"What are the benefits of using result types instead of throwing exceptions?"
"Are there rules for naming error codes consistently?"

Error handling is a core concern that covers various failure scenarios from domain rule violations to external system failures. This document covers Functorium's `Fin<T>` pattern, per-layer error factories, and error naming rules (R1-R8).

### What You Will Learn

Through this document, you will learn:

1. **Exception vs Result type differences** -- Why explicit error handling is preferred
2. **`Fin<T>` and implicit conversion** -- Pattern for returning errors concisely
3. **Per-layer error factories** -- Usage of `DomainError`, `ApplicationError`, `AdapterError`
4. **Error naming rules R1-R8** -- Flowchart for writing consistent error codes

### Prerequisites

A basic understanding of the following concepts is required to understand this document:

- Basic understanding of LanguageExt's `Fin<T>` type
- [Value Object implementation guide](../05a-value-objects) -- Usage of `Validation<Error, T>` in Value Objects

> Functorium makes failures explicit in the type system using `Fin<T>` and `Validation<Error, T>` instead of exceptions. Per-layer error factories (`DomainError`, `ApplicationError`, `AdapterError`) distinguish error origins, and R1-R8 naming rules ensure consistency of error codes.

## Summary

### Key Commands

```csharp
// Error return (implicit conversion recommended)
return DomainError.For<Email>(new Empty(), "", "Email cannot be empty");
return ApplicationError.For<CreateProductCommand>(new AlreadyExists(), code, "Already exists");
return AdapterError.For<ProductRepository>(new NotFound(), id, "Not found");

// Exception wrapping
return AdapterError.FromException<MyAdapter>(new ConnectionFailed("DB"), exception);

// Success return
return Fin.Succ(product);
```

### Key Procedures

1. Follow the error naming rules flowchart (R1-R8) in order to select the appropriate rule
2. Check if it can be expressed with standard error types; if not, define a `Custom` sealed record
3. Use the factory matching the layer (`DomainError`, `ApplicationError`, `AdapterError`)
4. Return directly via implicit conversion instead of wrapping with `Fin.Fail<T>()`

### Key Concepts

| Concept | Description |
|------|------|
| `Fin<T>` | Single error return. Used in Entity methods and Usecases |
| `Validation<Error, T>` | Multiple error accumulation. Used in Value Object validation |
| Per-layer error factories | Distinguish error origin with `DomainError`, `ApplicationError`, `AdapterError` |
| Implicit conversion | Automatic `Error -> Fin<T>` conversion. No `Fin.Fail<T>(error)` wrapping needed |
| Naming rules R1-R8 | Self-evident state (R1) -> Criteria comparison (R2) -> Unmet expectation (R3) -> ... -> Operation failure (R8) |

---

## Why Explicit Error Handling

### Exception vs Result Type

Traditional exception-based error handling has implicit control flow, and method signatures do not reveal which errors a method may return. Result types (`Fin<T>`, `Validation<Error, T>`) make success and failure explicit in the type system, forcing callers to handle both cases.

### Railway Oriented Programming

Railway Oriented Programming (ROP) uses two rails as a metaphor for success and failure tracks. Each step automatically transitions to the next step on success, or to the error track on failure. `Fin<T>`'s `Bind`/`Map` and LINQ query syntax naturally support this pattern.

### Role of Errors in DDD

In Domain-Driven Design, errors are not mere exceptions but **explicit representations of domain rule violations.** Value Object invariant violations, Entity state transition constraints, and Aggregate business rules are all expressed as typed errors.

### Functorium's Approach

Functorium leverages LanguageExt's `Fin<T>` and `Validation<Error, T>`:

- **`Fin<T>`**: Operations that return a single error (Entity methods, Usecases, etc.)
- **`Validation<Error, T>`**: Validation that accumulates multiple errors (Value Object creation, etc.)
- **Per-layer error factories**: Clearly distinguish error origins with `DomainError`, `ApplicationError`, `AdapterError`
- **Type-safe error codes**: Use `DomainErrorType`, `ApplicationErrorType`, `AdapterErrorType` instead of strings

Now that we understand the need for explicit error handling, let us examine the specific patterns for returning errors in Functorium.

---

## Fin and Error Return Patterns

### Fin<T> Overview

`Fin<T>` is a type provided by LanguageExt that represents success/failure:

```csharp
// Success
Fin<Product> success = product;           // Implicit conversion
Fin<Product> success = Fin.Succ(product); // Explicit

// Failure
Fin<Product> failure = error;             // Implicit conversion (recommended)
Fin<Product> failure = Fin.Fail<Product>(error); // Explicit (unnecessary)
```

### Leveraging Implicit Conversion (Recommended)

LanguageExt provides `Error -> Fin<T>` implicit conversion. **`Fin.Fail<T>(error)` wrapping is unnecessary.**

```csharp
// ❌ Previous approach (verbose)
return Fin.Fail<Money>(AdapterError.For<MyAdapter>(
    new NotFound(), context, "Resource not found"));

// ✅ Recommended approach (implicit conversion)
return AdapterError.For<MyAdapter>(
    new NotFound(), context, "Resource not found");
```

### Per-Layer Error Return Patterns

```csharp
// Domain Layer - Entity method
// Error type definition: public sealed record InsufficientStock : DomainErrorType.Custom;
public Fin<Unit> DeductStock(Quantity quantity)
{
    if ((int)quantity > (int)StockQuantity)
        return DomainError.For<Product, int>(
            new InsufficientStock(),
            currentValue: (int)StockQuantity,
            message: $"Insufficient stock. Current: {(int)StockQuantity}, Requested: {(int)quantity}");

    StockQuantity = Quantity.Create((int)StockQuantity - (int)quantity).ThrowIfFail();
    return unit;
}

// Application Layer - Usecase
public async ValueTask<FinResponse<Response>> Handle(Request request, ...)
{
    if (await _repository.ExistsAsync(request.ProductCode))
        return ApplicationError.For<CreateProductCommand>(
            new AlreadyExists(),
            request.ProductCode,
            "Product code already exists");

    // Success processing...
}

// Adapter Layer - Repository
public virtual FinT<IO, Product> GetById(ProductId id)
{
    return IO.lift(() =>
    {
        if (_products.TryGetValue(id, out Product? product))
            return Fin.Succ(product);

        return AdapterError.For<InMemoryProductRepository>(
            new NotFound(),
            id.ToString(),
            $"Product ID '{id}' not found");
    });
}
```

### FinT<IO, T> Pattern

The `FinT<IO, T>` pattern used with asynchronous IO operations:

```csharp
// Synchronous operation
return IO.lift(() =>
{
    if (condition)
        return Fin.Succ(result);
    return AdapterError.For<MyAdapter>(new NotFound(), context, "message");
});

// Asynchronous operation
// Error type definition: public sealed record HttpError : AdapterErrorType.Custom;
return IO.liftAsync(async () =>
{
    try
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return AdapterError.For<MyAdapter>(
                new HttpError(),
                response.StatusCode.ToString(),
                "API call failed");

        var result = await response.Content.ReadFromJsonAsync<T>();
        return Fin.Succ(result!);
    }
    catch (HttpRequestException ex)
    {
        return AdapterError.FromException<MyAdapter>(
            new ConnectionFailed("ExternalApi"),
            ex);
    }
});
```

### Notes on Returning Success

When returning success values, `Fin.Succ(value)` is still used:

```csharp
// ✅ Success return
return Fin.Succ(product);
return Fin.Succ(unit);  // Unit type

// ❌ Unit does not have implicit conversion (Unit is not an Error)
return unit;  // Compile error or type inference failure
```

### Exception Handling Pattern

Use the `FromException` method when converting exceptions to `Error`:

```csharp
catch (HttpRequestException ex)
{
    return AdapterError.FromException<ExternalPricingApiService>(
        new ConnectionFailed("ExternalPricingApi"),
        ex);
}
// Error type definitions:
// public sealed record OperationCancelled : AdapterErrorType.Custom;
// public sealed record UnexpectedException : AdapterErrorType.Custom;
catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
{
    return AdapterError.For<ExternalPricingApiService>(
        new OperationCancelled(),
        productCode,
        "Request was cancelled");
}
catch (TaskCanceledException ex)
{
    return AdapterError.FromException<ExternalPricingApiService>(
        new Timeout(TimeSpan.FromSeconds(30)),
        ex);
}
catch (Exception ex)
{
    return AdapterError.FromException<ExternalPricingApiService>(
        new UnexpectedException(),
        ex);
}
```

Now that we have learned error return patterns, the next important thing is naming error codes consistently.

---

## Error Naming Rules

### Quick Reference: Naming Rules Summary

When defining a new error type, first find the appropriate rule in this table. Apply rules in order starting from R1.

| Rule | Condition | Pattern | Example |
|------|----------|------|------|
| **R1** | Self-evident problematic state | State as-is | `Empty`, `Null`, `Negative`, `Duplicate` |
| **R2** | Comparison against criteria | `Too-` / `Below-` / `Above-` / `OutOf-` | `TooShort`, `BelowMinimum`, `OutOfRange` |
| **R3** | Unmet expected condition | `Not-` + expectation | `NotPositive`, `NotUpperCase`, `NotFound` |
| **R4** | Already occurred state | `Already-` + state | `AlreadyExists` |
| **R5** | Format/structure issue | `Invalid-` + target | `InvalidFormat`, `InvalidState` |
| **R6** | Two values mismatch | `Mismatch` / `Wrong-` | `Mismatch`, `WrongLength` |
| **R7** | Authorization/authentication issue | State as-is | `Unauthorized`, `Forbidden` |
| **R8** | Operation/process issue | Past participle + noun | `ValidationFailed`, `OperationCancelled` |

### Detailed Rule Description

#### R1: Self-Evident State -> State As-Is

**Applicable Condition**: Cases where it is clearly a "problem" by itself

```csharp
// ✅ Correct
new Empty()      // Being empty → problem
new Null()       // Being null → problem
new Negative()   // Being negative → problem
new Duplicate()  // Being duplicate → problem

// ❌ Incorrect
new NotFilled()  // Empty is sufficient
new IsNull()     // Null is sufficient
```

#### R2: Comparison Against Criteria -> Comparison Expression

**Applicable Condition**: Cases where comparison against criteria such as min/max/range is needed

```csharp
// ✅ Correct
new TooShort(MinLength: 8)        // Below minimum length
new TooLong(MaxLength: 100)       // Exceeds maximum length
new BelowMinimum(Minimum: "0")    // Below minimum value
new AboveMaximum(Maximum: "100")  // Above maximum value
new OutOfRange(Min: "1", Max: "10") // Outside range

// ❌ Incorrect
new Short()      // Criteria unclear
new Long()       // Criteria unclear
```

| Prefix | Meaning | Use Case |
|--------|------|----------|
| `Too-` | Excessive/insufficient | Relative comparison of length, size, etc. |
| `Below-` | Less than | Minimum criteria not met |
| `Above-` | Greater than | Maximum criteria exceeded |
| `OutOf-` | Out of range | Outside allowed range |

#### R3: Unmet Expected Condition -> Not + Expectation

**Applicable Condition**: Cases where "should be X but is not" needs to be expressed

```csharp
// ✅ Correct
new NotPositive()   // Should be positive (0 is also an error)
new NotUpperCase()  // Should be uppercase
new NotLowerCase()  // Should be lowercase
new NotFound()      // Should exist

// ❌ Incorrect
new Lowercase()     // Ambiguous meaning
new Missing()       // NotFound is more clear
```

**R1 vs R3 distinction:**

| Situation | Applied Rule | Reason |
|------|----------|------|
| `Negative` | R1 | "Being negative" is clearly a problem |
| `NotPositive` | R3 | "Should be positive" but also needs to include 0 |
| `Empty` | R1 | "Being empty" is clearly a problem |
| `NotUpperCase` | R3 | Needs to explicitly state "should be uppercase" for clarity |

#### R4: Already Occurred State -> Already + State

**Applicable Condition**: A state that has already occurred and cannot be undone

```csharp
// ✅ Correct
new AlreadyExists()  // Already exists

// ❌ Incorrect
new Exists()         // Weak meaning without "already"
```

#### R5: Format/Structure/State Issue -> Invalid + Target

**Applicable Condition**: Cases where the format, structure, or state of a value is invalid

```csharp
// ✅ Correct
new InvalidFormat(Pattern: @"^\d{3}-\d{4}$")
new InvalidState()

// ❌ Incorrect
new InvalidLength()  // Use WrongLength (R6)
new InvalidValue()   // Too abstract
```

**Caution**: The `Invalid-` prefix is only used for format/structure/state issues.

#### R6: Two Values Mismatch -> Mismatch or Wrong

**Applicable Condition**: Cases where two values should match but do not

```csharp
// ✅ Correct
new Mismatch()                    // General mismatch
new WrongLength(Expected: 10)     // Exact length mismatch

// ❌ Incorrect
new NotMatching()    // Mismatch is more concise
new LengthMismatch() // WrongLength is more clear
```

| Pattern | Use Case |
|------|----------|
| `Mismatch` | Two value comparison (password confirmation, etc.) |
| `Wrong-` | Mismatch with expected exact value |

#### R7: Authorization/Authentication Issue -> State As-Is

**Applicable Condition**: Authentication/authorization related issues

```csharp
// ✅ Correct (matches HTTP status codes)
new Unauthorized()   // 401: Authentication required
new Forbidden()      // 403: Access denied

// ❌ Incorrect
new NotAuthenticated()  // Unauthorized is standard
new AccessDenied()      // Forbidden is standard
```

#### R8: Operation/Process Issue -> Past Participle + Noun

**Applicable Condition**: Issues that occur during operation or process execution

```csharp
// ✅ Correct
new ValidationFailed(PropertyName: "Email")
new OperationCancelled()
new BusinessRuleViolated(RuleName: "MaxOrderLimit")
new ConcurrencyConflict()

// ❌ Incorrect
new FailedValidation()  // Word order mismatch
new CancelledOperation() // OperationCancelled is standard
```

### Rule Application Flowchart

When defining a new error type, apply rules in the following order:

```
1. Is the state itself a problem?
   ├─ Yes → R1 (Empty, Null, Negative, Duplicate)
   └─ No ↓

2. Does it need comparison against criteria?
   ├─ Yes → R2 (TooShort, BelowMinimum, OutOfRange)
   └─ No ↓

3. Is it "should be X but is not"?
   ├─ Yes → R3 (NotPositive, NotUpperCase, NotFound)
   └─ No ↓

4. Is it an already occurred state?
   ├─ Yes → R4 (AlreadyExists)
   └─ No ↓

5. Is it a format/structure/state issue?
   ├─ Yes → R5 (InvalidFormat, InvalidState)
   └─ No ↓

6. Is it a two values mismatch?
   ├─ Yes → R6 (Mismatch, WrongLength)
   └─ No ↓

7. Is it an authorization/authentication issue?
   ├─ Yes → R7 (Unauthorized, Forbidden)
   └─ No ↓

8. Is it an operation/process failure?
   ├─ Yes → R8 (ValidationFailed, OperationCancelled)
   └─ No → Use Custom
```

### Custom -> Standard Error Promotion Criteria

When a `Custom` error at the end of the flowchart is used repeatedly throughout the project, consider promoting it to a standard error type (R1-R8):

> 1. **Used in 3 or more different locations** with the same Custom error
> 2. **Reuse meaning is clear** (established as a domain concept)
> 3. Can be **naturally mapped** to existing naming rules (R1-R8)
> 4. **Stability confirmed** (meaning is no longer changing)

When all 4 conditions are met, add it to the standard `DomainErrorType` / `ApplicationErrorType` / `AdapterErrorType`.

---

## Troubleshooting

### `Fin<Unit>` type inference failure when returning `unit`
**Cause:** `return unit;` is of type `Unit`, not `Error`, so implicit conversion to `Fin<T>` does not work. Implicit conversion only supports the `Error -> Fin<T>` direction.
**Solution:** Always explicitly use `return Fin.Succ(unit);` for success returns. Value types (like `Product`) support implicit conversion, but `Unit` is an exception.

### Ambiguous distinction between R1 and R3 in error naming
**Cause:** There are cases that look similar, like `Negative` (R1) and `NotPositive` (R3).
**Solution:** R1 is for "states that are clearly problematic by themselves" (e.g., `Empty`, `Null`, `Negative`), while R3 is for "negations that only make sense with an expected condition" (e.g., `NotPositive` includes 0, `NotUpperCase`). Follow the flowchart from top to bottom in order, and the first matching rule is the correct answer.

### `Fin.Fail` needed for error return inside `FinT<IO, T>`
**Cause:** Inside `IO.lift(() => { ... })` blocks, the return type is `Fin<T>`, so implicit conversion works normally for error returns. However, `Fin.Succ(value)` is needed for success returns.
**Solution:** Inside `IO.lift` blocks, use implicit conversion for errors and `Fin.Succ(value)` for success.

---

## FAQ

**Q: What is the difference between `Fin<T>` and Exception?**

`Fin<T>` represents predictable failures (business rule violations, validation failures, etc.) as types, forcing callers to handle them. Exceptions are used only for exceptional, unrecoverable situations such as network failures or out-of-memory conditions.

**Q: When I am unsure which rule among R1-R8 to apply for error code naming?**

Follow the [Rule Application Flowchart](#rule-application-flowchart) from top to bottom in order. The first matching rule is the most specific, so apply that one.

**Q: When should Custom errors be created?**

Use Custom when the error is domain-specific and cannot be expressed with standard error types from R1-R8. Examples: `InsufficientStock`, `HttpError`, etc. If later used repeatedly in 3 or more locations, consider promotion per the [Custom -> Standard Error Promotion Criteria](#custom---standard-error-promotion-criteria).

---

## Appendix: ErrorCodeFactory API

### File Structure

```
Src/Functorium/Abstractions/Errors/
├── ErrorCodeExpected.cs              # Expected error type (4 variants)
├── ErrorCodeExpectedBase.cs          # Expected error common base class (13 overrides unified)
├── ErrorCodeExceptional.cs           # Exceptional error type
├── ErrorCodeFactory.cs               # ErrorCodeExpected/Exceptional instance creation
├── ErrorCodeFieldNames.cs            # Serilog structured field name constants
├── ErrorType.cs                      # Error prefix constants (DomainErrorsPrefix, etc.)
├── IHasErrorCode.cs                  # Error code access interface
└── LayerErrorCore.cs                 # Per-layer factory common error code generation logic

Src/Functorium.Testing/Assertions/Errors/
├── DomainErrorAssertions.cs          # Domain error validation (thin wrapper)
├── ApplicationErrorAssertions.cs     # Application error validation (thin wrapper)
├── AdapterErrorAssertions.cs         # Adapter error validation (thin wrapper)
├── ErrorAssertionCore.cs             # Per-layer Assertion common validation logic
├── ErrorAssertionHelpers.cs          # Shared utilities
├── ErrorCodeAssertions.cs            # General-purpose error code validation
└── ErrorCodeExceptionalAssertions.cs # Exceptional error validation

Src/Functorium.Adapters/Abstractions/Errors/
└── DestructuringPolicies/            # Serilog destructuring policies
    ├── IErrorDestructurer.cs
    ├── ErrorsDestructuringPolicy.cs
    └── ErrorTypes/
        ├── ErrorCodeExpectedDestructurer.cs
        ├── ErrorCodeExpectedTDestructurer.cs
        ├── ErrorCodeExceptionalDestructurer.cs
        ├── ExceptionalDestructurer.cs    # LanguageExt Exceptional destructuring
        ├── ExpectedDestructurer.cs       # LanguageExt Expected destructuring
        └── ManyErrorsDestructurer.cs
```

### Error Type Hierarchy

Functorium's error types extend LanguageExt's `Error` to form the following hierarchy.

```
Error (LanguageExt.Common)
├── ErrorCodeExpectedBase             - Expected error common base (13 overrides unified)
│   ├── ErrorCodeExpected             - Domain/business error (string value)
│   ├── ErrorCodeExpected<T>          - Domain/business error (1 typed value)
│   ├── ErrorCodeExpected<T1, T2>     - Domain/business error (2 typed values)
│   └── ErrorCodeExpected<T1, T2, T3> - Domain/business error (3 typed values)
├── ErrorCodeExceptional              - Exception wrapper
└── ManyErrors                        - Multiple error collection
```

`ErrorCodeExpectedBase` defines common members including `ErrorCode`, `Message`, `Code`, `Inner` properties and `sealed override ToString() => Message`, `IsExpected = true`, `IsExceptional = false`. The 4 derived types only add `ErrorCurrentValue`-related properties. `sealed override ToString()` prevents C# records from auto-regenerating `ToString()` in derived classes, ensuring all Expected errors consistently return `Message`.

All `ErrorCodeExpected` variants and `ErrorCodeExceptional` are **internal records** implementing the `IHasErrorCode` interface.

| Type | `IsExpected` | `IsExceptional` | Access Modifier |
|------|:---:|:---:|:---:|
| `ErrorCodeExpectedBase` | `true` | `false` | internal (abstract) |
| `ErrorCodeExpected` | `true` | `false` | internal |
| `ErrorCodeExpected<T>` | `true` | `false` | internal |
| `ErrorCodeExpected<T1, T2>` | `true` | `false` | internal |
| `ErrorCodeExpected<T1, T2, T3>` | `true` | `false` | internal |
| `ErrorCodeExceptional` | `false` | `true` | internal |

> **Note**: The 3-value overload `DomainError.For<TDomain, T1, T2, T3>()` is also supported. For detailed signatures and usage examples, see [Error System: Domain/Application Errors](../08b-error-system-domain-app).

### Error Creation Flow

Per-layer factories (`DomainError`, `ApplicationError`, `EventError`, `AdapterError`) create errors through a 2-stage internal delegation.

```
DomainError.For<Email>(new Empty(), value, msg)     ← Public API (DomainErrorType enforced)
  → LayerErrorCore.Create<Email>(prefix, errorType, value, msg)
      ← Received as ErrorType(base) → Assemble error code: "DomainErrors.Email.Empty"
    → ErrorCodeFactory.Create(errorCode, value, msg)
        ← Create ErrorCodeExpected instance
```

`LayerErrorCore` is the shared implementation for the 4 factories, assembling the error code string `{prefix}.{typeof(TContext).Name}.{errorType.ErrorName}`. The public factories maintain per-layer type parameters (`DomainErrorType`, `ApplicationErrorType`, etc.) to **prevent incorrect layer error usage at compile time.** All methods have `[AggressiveInlining]` applied so the JIT inlines the delegation calls, achieving the same performance as direct calls.

### ErrorCodeFactory API

`ErrorCodeFactory` is a **static class** located in `Abstractions/Errors/ErrorCodeFactory.cs` that directly creates `ErrorCodeExpected` and `ErrorCodeExceptional` instances.

```csharp
public static class ErrorCodeFactory
{
    // Expected error (string value) → ErrorCodeExpected
    public static Error Create(string errorCode, string errorCurrentValue, string errorMessage);

    // Expected error (typed value) → ErrorCodeExpected<T>
    public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
        where T : notnull;

    // Expected error (2 typed values) → ErrorCodeExpected<T1, T2>
    public static Error Create<T1, T2>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, string errorMessage)
        where T1 : notnull where T2 : notnull;

    // Expected error (3 typed values) → ErrorCodeExpected<T1, T2, T3>
    public static Error Create<T1, T2, T3>(string errorCode, T1 errorCurrentValue1, T2 errorCurrentValue2, T3 errorCurrentValue3, string errorMessage)
        where T1 : notnull where T2 : notnull where T3 : notnull;

    // Exceptional error → ErrorCodeExceptional
    public static Error CreateFromException(string errorCode, Exception exception);

    // Error code format → string.Join('.', parts)
    public static string Format(params string[] parts);
}
```

### Usage Examples

```csharp
// Expected error (string value)
Error error = ErrorCodeFactory.Create(
    "DomainErrors.User.NotFound", "user123", "User not found");

// Expected error (typed value)
Error error = ErrorCodeFactory.Create(
    "DomainErrors.Sensor.TemperatureOutOfRange", 150, "Temperature out of range");

// Expected error (2 typed values)
Error error = ErrorCodeFactory.Create(
    "DomainErrors.Range.InvalidBounds", 100, 50, "Minimum is greater than maximum");

// Exceptional error
Error error = ErrorCodeFactory.CreateFromException(
    "ApplicationErrors.Database.ConnectionFailed", exception);

// Error code format
string code = ErrorCodeFactory.Format("DomainErrors", "User", "NotFound");
// Result: "DomainErrors.User.NotFound"
```

### Serilog Destructuring

Registering `ErrorsDestructuringPolicy` enables error objects to be logged as structured JSON.

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.With<ErrorsDestructuringPolicy>()
    .CreateLogger();
```

**Field mapping:**

| Field | Expected | Expected&lt;T&gt; | Exceptional | ManyErrors |
|-------|:---:|:---:|:---:|:---:|
| ErrorType | O | O | O | O |
| ErrorCode | O | O | O | X |
| ErrorCodeId | O | O | O | O |
| ErrorCurrentValue | O | O | X | X |
| Message | O | O | O | X |
| Count | X | X | X | O |
| Errors | X | X | X | O |
| ExceptionDetails | X | X | O | X |

---

## Reference Documents

- [08b-error-system-domain-app.md](../08b-error-system-domain-app) - Domain/Application error definitions and testing
- [08c-error-system-adapter-testing.md](../08c-error-system-adapter-testing) - Adapter errors, Custom errors, testing best practices
