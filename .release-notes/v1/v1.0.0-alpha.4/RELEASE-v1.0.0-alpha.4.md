# Functorium Release v1.0.0-alpha.4

**English** | **[한국어](https://github.com/hhko/Functorium/blob/v1.0.0-alpha.4/.release-notes/v1/v1.0.0-alpha.4/RELEASE-v1.0.0-alpha.4-KR.md)**

## Overview

Functorium v1.0.0-alpha.4 is the **error system redesign release**. The entire error type hierarchy has been renamed end-to-end to resolve naming overload that had accumulated across alpha.1-alpha.3, where six symbols all started with `ErrorCode*` and five symbols all ended with `*ErrorType`. After this release, every role in the error system has a unique, role-specific name: factories end in `Error`, classifiers end in `Kind`, and code prefixes use the bare layer name.

> **Pre-1.0 stability notice**: The 1.0.0-alpha line is still under active design. Further breaking changes are expected in subsequent alpha releases as additional structural issues are surfaced and resolved. Production adoption should wait until the 1.0.0 stable release.

Alongside the rename, the public API surface was tightened (the internal-only `ErrorCodeFactory` plus three prefix constants moved out of the public surface), and three new repository/query capabilities (`FindAllSatisfying`, `FindFirstSatisfying`, `Exists`/`Count` on `IQueryPort`, plus `ConcurrencyConflict` typed errors) round out the release.

**Key Features**:

- **Error System Rename (Breaking)**: `ErrorType` -> `ErrorKind`, `ErrorCodeFactory` -> internal `ErrorFactory`, `ErrorCodeExpected/Exceptional` -> `ExpectedError/ExceptionalError`, log field names changed (`ErrorType` -> `Kind`, `ErrorCodeId` -> `NumericCode`)
- **Error Code Prefix Shortened (Breaking)**: `"DomainErrors.X.Y"` -> `"Domain.X.Y"`, `"ApplicationErrors.X.Y"` -> `"Application.X.Y"`, `"AdapterErrors.X.Y"` -> `"Adapter.X.Y"` -- operational dashboards filtering on the legacy prefix must migrate
- **Architecture Test Contract Reorganization (Breaking)**: `IValueObject`/`IEntity` arch-test constants relocated into nested `ArchTestContract` static class
- **New Repository APIs**: `FindAllSatisfying` and `FindFirstSatisfying` added to `IRepository<TAggregate, TId>` for read-by-Specification scenarios
- **New Query APIs**: `IQueryPort<TEntity, TDto>` gains `Exists` and `Count` methods, mirroring the `IRepository` surface
- **Concurrency Conflict Typed Error**: `EfCoreRepositoryBase.Update` now detects `DbUpdateConcurrencyException` and converts it into a typed `AdapterErrorKind.ConcurrencyConflict` error
- **Pipeline Cancellation Fix**: `UsecaseExceptionPipeline` no longer swallows `OperationCanceledException`, allowing host frameworks (HTTP, gRPC) to recognize client disconnection
- **Validation Multi-Error Preservation**: `Validation<Error,T>` -> `Fin<T>` conversion now preserves all accumulated errors instead of keeping only the first

## Breaking Changes

### 1. `ErrorType` -> `ErrorKind` Across All Three Layers

The abstract base record and the three layer-specific records have been renamed. This affects every custom error definition in user code, every `For<T>(...)` factory call, and the virtual property used to derive error names.

**Before (v1.0.0-alpha.3)**:
```csharp
// Abstract base
public abstract partial record ErrorType
{
    public virtual string ErrorName { get; }
}

// Layer records
public abstract partial record DomainErrorType : ErrorType { ... }
public abstract partial record ApplicationErrorType : ErrorType { ... }
public abstract partial record AdapterErrorType : ErrorType { ... }

// Custom error
public sealed record InsufficientStock : DomainErrorType.Custom;

// Factory call
DomainError.For<Email>(new DomainErrorType.Empty(), value, "...");
```

**After (v1.0.0-alpha.4)**:
```csharp
// Abstract base
public abstract partial record ErrorKind
{
    public virtual string Name { get; }
}

// Layer records
public abstract partial record DomainErrorKind : ErrorKind { ... }
public abstract partial record ApplicationErrorKind : ErrorKind { ... }
public abstract partial record AdapterErrorKind : ErrorKind { ... }

// Custom error
public sealed record InsufficientStock : DomainErrorKind.Custom;

// Factory call
DomainError.For<Email>(new DomainErrorKind.Empty(), value, "...");
```

**Migration Guide**:
1. Replace all `ErrorType` -> `ErrorKind` occurrences (a global find-and-replace handles the bulk)
2. Replace `DomainErrorType` -> `DomainErrorKind`, `ApplicationErrorType` -> `ApplicationErrorKind`, `AdapterErrorType` -> `AdapterErrorKind`
3. Replace any explicit overrides of `ErrorName` with `Name`
4. Custom error records inheriting from `*ErrorType.Custom` must be updated to `*ErrorKind.Custom`

<!-- Related commit: b9396475 refactor(errors)!: ErrorType -> ErrorKind full rename -->

---

### 2. Error Code Prefix Values Shortened

The prefix portion of every emitted error code has been shortened to the bare layer name. The three `public const` prefix fields on `ErrorType` have been removed from the public API entirely (they now live in an internal `ErrorCodePrefixes` class).

**Before (v1.0.0-alpha.3)**:
```csharp
// Public constants on ErrorType
public abstract partial record ErrorType
{
    public const string DomainErrorsPrefix = "DomainErrors";
    public const string ApplicationErrorsPrefix = "ApplicationErrors";
    public const string AdapterErrorsPrefix = "AdapterErrors";
}

// Emitted error code
"DomainErrors.Email.Empty"
"ApplicationErrors.CreateProductCommand.AlreadyExists"
"AdapterErrors.ProductRepository.NotFound"
```

**After (v1.0.0-alpha.4)**:
```csharp
// All three public consts removed from the public surface
// Now internal-only:
//   internal static class ErrorCodePrefixes
//   {
//       public const string Domain = "Domain";
//       public const string Application = "Application";
//       public const string Adapter = "Adapter";
//   }

// Emitted error code
"Domain.Email.Empty"
"Application.CreateProductCommand.AlreadyExists"
"Adapter.ProductRepository.NotFound"
```

**Migration Guide**:
1. Update operational dashboards (Seq, Grafana, Elastic, Kibana) that filter on `DomainErrors.*`, `ApplicationErrors.*`, or `AdapterErrors.*` to filter on `Domain.*`, `Application.*`, `Adapter.*`
2. Remove any code referencing `ErrorType.DomainErrorsPrefix` / `.ApplicationErrorsPrefix` / `.AdapterErrorsPrefix` constants -- these are no longer accessible
3. Test code that asserts on hardcoded error code strings must be updated; the layer-specific assertion helpers (`ShouldBeDomainError`, `ShouldBeApplicationError`, `ShouldBeAdapterError`) compute the prefix automatically and need no change

<!-- Related commit: 21bc1f8b refactor(errors)!: layer prefix split into internal ErrorCodePrefixes + values shortened -->

---

### 3. `ErrorCodeFactory` Made Internal as `ErrorFactory`

The previously-public `ErrorCodeFactory` has been moved to `internal` and renamed `ErrorFactory`. Method names changed from `Create`/`CreateFromException` to `CreateExpected`/`CreateExceptional`. The `Format(...)` method was removed entirely. External consumers should use the layer factories (`DomainError`/`ApplicationError`/`AdapterError`) -- the same set that has been the recommended call site since alpha.1.

**Before (v1.0.0-alpha.3)**:
```csharp
// Public class
public static class ErrorCodeFactory
{
    public static Error Create(string errorCode, string currentValue, string message);
    public static Error Create<T>(string errorCode, T currentValue, string message)
        where T : notnull;
    public static Error Create<T1, T2>(...);
    public static Error Create<T1, T2, T3>(...);
    public static Error CreateFromException(string errorCode, Exception exception);
    public static string Format(params string[] parts);
}

// Direct usage (no longer possible)
var error = ErrorCodeFactory.Create("MyCustom.Code", value, "message");
```

**After (v1.0.0-alpha.4)**:
```csharp
// internal — not visible to external assemblies
internal static class ErrorFactory
{
    public static Error CreateExpected(string errorCode, string currentValue, string message);
    public static Error CreateExpected<T>(string errorCode, T currentValue, string message)
        where T : notnull;
    // ...
    public static Error CreateExceptional(string errorCode, Exception exception);
    // Format(...) removed
}

// Use the layer factory instead (no change from previous releases)
DomainError.For<Email>(new DomainErrorKind.Empty(), value, "message");
```

**Migration Guide**:
1. If you were calling `ErrorCodeFactory.Create(...)` directly, switch to `DomainError.For<T>(...)`, `ApplicationError.For<T>(...)`, or `AdapterError.For<T>(...)`
2. If you were calling `ErrorCodeFactory.CreateFromException(...)` to wrap an exception, the layer factories handle this through their exception-overload signatures
3. Remove any references to `ErrorCodeFactory.Format(...)` -- the helper is gone; format the parts at the call site instead

<!-- Related commits: 8c057417 refactor(errors)!: ErrorCodeFactory -> internal ErrorFactory; 049c0e26 refactor(errors): ErrorCodeFactory.Format removed -->

---

### 4. `ErrorCodeExpected`/`ErrorCodeExceptional` -> `ExpectedError`/`ExceptionalError`

The `Error` subclass records (and their companion Adapter destructurers and Testing assertions) have been renamed to make the `Expected` vs `Exceptional` distinction central.

**Before (v1.0.0-alpha.3)**:
```csharp
// Adapter Serilog destructurers
public class ErrorCodeExpectedDestructurer : IErrorDestructurer { ... }
public class ErrorCodeExpectedTDestructurer : IErrorDestructurer { ... }
public class ErrorCodeExceptionalDestructurer : IErrorDestructurer { ... }

// Testing assertions
public static class ErrorCodeAssertions
{
    public static void ShouldBeErrorCodeExpected(this Error error, string expectedErrorCode, string expectedCurrentValue);
    public static void ShouldBeErrorCodeExpected<T>(this Error error, string expectedErrorCode, T expectedCurrentValue);
    public static void ShouldBeErrorCodeExpected<T1, T2>(...);
    public static void ShouldBeErrorCodeExpected<T1, T2, T3>(...);
}

public static class ErrorCodeExceptionalAssertions
{
    public static void ShouldBeErrorCodeExceptional(this Error error, string expectedErrorCode);
    public static void ShouldBeErrorCodeExceptional<TException>(this Error error, string expectedErrorCode)
        where TException : Exception;
    // ...
}
```

**After (v1.0.0-alpha.4)**:
```csharp
// Adapter Serilog destructurers
public class ExpectedErrorDestructurer : IErrorDestructurer { ... }
public class ExpectedErrorTDestructurer : IErrorDestructurer { ... }
public class ExceptionalErrorDestructurer : IErrorDestructurer { ... }

// Testing assertions
public static class ExpectedErrorAssertions
{
    public static void ShouldBeExpectedError(this Error error, string expectedErrorCode, string expectedCurrentValue);
    public static void ShouldBeExpectedError<T>(this Error error, string expectedErrorCode, T expectedCurrentValue);
    public static void ShouldBeExpectedError<T1, T2>(...);
    public static void ShouldBeExpectedError<T1, T2, T3>(...);
}

public static class ExceptionalErrorAssertions
{
    public static void ShouldBeExceptionalError(this Error error, string expectedErrorCode);
    public static void ShouldBeExceptionalError<TException>(this Error error, string expectedErrorCode)
        where TException : Exception;
    // ...
}
```

**Migration Guide**:
1. Replace `ShouldBeErrorCodeExpected` -> `ShouldBeExpectedError` in all test code
2. Replace `ShouldBeErrorCodeExceptional` -> `ShouldBeExceptionalError`
3. If you registered the destructurers manually with Serilog, replace `ErrorCodeExpectedDestructurer` -> `ExpectedErrorDestructurer`, `ErrorCodeExceptionalDestructurer` -> `ExceptionalErrorDestructurer`
4. The `ShouldBeExpected()` / `ShouldBeExceptional()` discriminator extensions remain on `ExpectedErrorAssertions` (no change needed)

<!-- Related commit: 6627c7fc refactor(errors)!: ErrorCodeExpected -> ExpectedError; ErrorCodeExceptional -> ExceptionalError rename -->

---

### 5. `ErrorCodeFieldNames` -> `ErrorLogFieldNames` + Log Field Keys Renamed

The Serilog log field name registry has been renamed, and two of its values have changed. Operational queries that grep for these property names in log streams (Seq filters, Elastic queries, Loki LogQL) must be updated.

**Before (v1.0.0-alpha.3)**:
```csharp
// Internal class
internal static class ErrorCodeFieldNames
{
    public const string ErrorCode = "ErrorCode";
    public const string ErrorType = "ErrorType";        // record discriminator
    public const string ErrorCodeId = "ErrorCodeId";    // numeric error id
    // ...
}

// Emitted log fields
{
  "ErrorCode": "Domain.Email.Empty",
  "ErrorType": "Empty",
  "ErrorCodeId": 1042
}
```

**After (v1.0.0-alpha.4)**:
```csharp
// Internal class
internal static class ErrorLogFieldNames
{
    public const string ErrorCode = "ErrorCode";
    public const string Kind = "Kind";                  // renamed from ErrorType
    public const string NumericCode = "NumericCode";    // renamed from ErrorCodeId
    // ...
}

// Emitted log fields
{
  "ErrorCode": "Domain.Email.Empty",
  "Kind": "Empty",
  "NumericCode": 1042
}
```

**Migration Guide**:
1. Update Seq / Grafana / Elastic / Loki queries filtering on `@p.ErrorType` to use `@p.Kind`
2. Update queries on `@p.ErrorCodeId` to use `@p.NumericCode`
3. The `ErrorCode` field name is unchanged, so dashboards filtering only on `ErrorCode` need no update
4. The class itself is `internal` and has no direct external consumers

<!-- Related commit: 2bd7c215 refactor(errors)!: ErrorCodeFieldNames -> ErrorLogFieldNames + NumericCode/Kind rename -->

---

### 6. Architecture Test Contract Constants Relocated

`IValueObject` and `IEntity` previously exposed factory-method-name constants (`CreateMethodName`, `ValidateMethodName`, etc.) directly on the interface. These have been grouped into a nested `ArchTestContract` static class. The value of `NestedErrorsClassName` also changed from `"DomainErrors"` to `"Domain"` to align with the prefix rename in change #2.

**Before (v1.0.0-alpha.3)**:
```csharp
public interface IValueObject
{
    public const string CreateMethodName = "Create";
    public const string CreateFromValidatedMethodName = "CreateFromValidated";
    public const string ValidateMethodName = "Validate";
    public const string DomainErrorsNestedClassName = "DomainErrors";
}

public interface IEntity
{
    public const string CreateMethodName = "Create";
    public const string CreateFromValidatedMethodName = "CreateFromValidated";
}

// ArchUnit test usage
typeof(Email).GetMethod(IValueObject.CreateMethodName);
```

**After (v1.0.0-alpha.4)**:
```csharp
public interface IValueObject
{
    public static class ArchTestContract
    {
        public const string CreateMethodName = "Create";
        public const string CreateFromValidatedMethodName = "CreateFromValidated";
        public const string ValidateMethodName = "Validate";
        public const string NestedErrorsClassName = "Domain";    // value also changed: "DomainErrors" -> "Domain"
    }
}

public interface IEntity
{
    public static class ArchTestContract
    {
        public const string CreateMethodName = "Create";
        public const string CreateFromValidatedMethodName = "CreateFromValidated";
    }
}

// ArchUnit test usage
typeof(Email).GetMethod(IValueObject.ArchTestContract.CreateMethodName);
```

**Migration Guide**:
1. Add `.ArchTestContract` between the interface and the constant name: `IValueObject.CreateMethodName` -> `IValueObject.ArchTestContract.CreateMethodName`
2. The same applies to `IEntity.CreateMethodName`, `CreateFromValidatedMethodName`, `ValidateMethodName`
3. `IValueObject.DomainErrorsNestedClassName` -> `IValueObject.ArchTestContract.NestedErrorsClassName` (note both the class-relocation and the constant rename)
4. If your architecture tests asserted that nested error classes are named `"DomainErrors"`, update them to expect `"Domain"`

<!-- Related commit: 216bc689 refactor(arch-contract)!: IValueObject/IEntity arch-test constants moved into ArchTestContract nested class -->

## New Features

### Functorium Library

#### 1. `IRepository.FindAllSatisfying` and `FindFirstSatisfying`

`IRepository<TAggregate, TId>` gains two new read-by-Specification methods. `FindAllSatisfying` returns the full set of aggregates matching a Specification (loaded into memory), and `FindFirstSatisfying` returns the first matching aggregate (or `Option.None` if no match exists). These complement the existing Specification methods (`Exists`, `Count`, `DeleteBy`) added in alpha.3.

```csharp
// IRepository<TAggregate, TId> interface (verified API surface)
LanguageExt.FinT<LanguageExt.IO, LanguageExt.Seq<TAggregate>>
    FindAllSatisfying(Specification<TAggregate> spec);

LanguageExt.FinT<LanguageExt.IO, LanguageExt.Option<TAggregate>>
    FindFirstSatisfying(Specification<TAggregate> spec);

// Usage in an application service
var spec = new ActiveOrdersByCustomerSpec(customerId);

// Get all matching aggregates
FinT<IO, Seq<Order>> activeOrders = orderRepository.FindAllSatisfying(spec);

// Get only the first one (translated to SELECT TOP 1 / LIMIT 1)
FinT<IO, Option<Order>> firstActive = orderRepository.FindFirstSatisfying(spec);
```

**Why this matters:**
- Without these methods, finding aggregates that match a Specification required either calling a query port (which returns DTOs, not aggregates) or composing `GetByIds` after a separate `Exists`/`Count` query, which is two round trips
- `FindFirstSatisfying` translates to `SELECT TOP 1` / `LIMIT 1` in EF Core through `FirstOrDefaultAsync`, avoiding the cost of materializing a full collection when only one entity is needed
- The same `Specification<TAggregate>` instance can now drive `Exists`, `Count`, `DeleteBy`, `FindAllSatisfying`, and `FindFirstSatisfying` -- one specification, five read/write modes
- Both `EfCoreRepositoryBase` and `InMemoryRepositoryBase` provide the implementation, so test repositories and production repositories share the contract

<!-- Related commit: 94f636b6 feat(repository): IRepository.FindAllSatisfying/FindFirstSatisfying -->

---

#### 2. `IQueryPort` Gains `Exists` and `Count` Methods

`IQueryPort<TEntity, TDto>` -- the Application-layer port for read-only queries that return DTOs -- now includes `Exists` and `Count`, mirroring the surface that `IRepository` got in alpha.3.

```csharp
// IQueryPort<TEntity, TDto> interface (verified API surface)
public interface IQueryPort<TEntity, TDto> : IObservablePort, IQueryPort
{
    LanguageExt.FinT<LanguageExt.IO, int> Count(Specification<TEntity> spec);
    LanguageExt.FinT<LanguageExt.IO, bool> Exists(Specification<TEntity> spec);
    // ... existing Search, SearchByCursor, Stream methods unchanged
}

// Usage in a query handler
public sealed class CountActiveOrdersQueryHandler
{
    public FinT<IO, int> Handle(CountActiveOrdersQuery query)
    {
        var spec = new ActiveOrdersSpec(query.CustomerId);
        return _orderQueryPort.Count(spec);
    }
}
```

**Why this matters:**
- Before this release, the only way to count results from a query port was to call `Search` and read `PagedResult.TotalCount` -- which materialized the page on top of computing the count
- `Exists` translates to a `SELECT EXISTS (SELECT 1 FROM ...)` (in EF Core's case via `AnyAsync`), so checking "are there any matches" no longer requires loading data
- `Count` translates to a single `SELECT COUNT(*)`, so dashboards or pagination headers that need only the total can issue exactly one round trip
- `InMemoryQueryBase` provides matching implementations, so unit tests using in-memory query ports stay consistent with production behavior

<!-- Related commit: 56a62ac4 feat(query): IQueryPort.Exists/Count -->

---

### Functorium.Adapters Library

#### 3. `ConcurrencyConflict` Typed Error in `EfCoreRepositoryBase.Update`

`EfCoreRepositoryBase.Update(TAggregate)` now intercepts `DbUpdateConcurrencyException` (raised by EF Core when an optimistic-concurrency token mismatches) and converts it into a typed `AdapterErrorKind.ConcurrencyConflict` error. The Application layer receives a structured error with the entity ID instead of a raw exception. A protected helper `ConcurrencyConflictError(TId id)` is also exposed so subclasses can raise the same typed error from custom paths (e.g., explicit `UpdateBy` failure recovery).

```csharp
// New typed error records (verified API surface)

// Application layer
public abstract record ApplicationErrorKind
{
    public sealed record ConcurrencyConflict : ApplicationErrorKind;
    // ...
}

// Adapter layer
public abstract record AdapterErrorKind
{
    public sealed record ConcurrencyConflict : AdapterErrorKind;
    // ...
}

// EfCoreRepositoryBase helper
protected LanguageExt.Common.Error ConcurrencyConflictError(TId id);

// Usage example -- no caller change required, the error appears automatically
var result = await orderRepository.Update(modifiedOrder).RunAsync();

result.Match(
    Succ: order => /* updated normally */,
    Fail: error => error.Code switch
    {
        "Adapter.OrderRepository.ConcurrencyConflict"
            => /* retry with fresh aggregate, or surface to user */,
        _ => /* other failures */
    });
```

**Why this matters:**
- Previously, `DbUpdateConcurrencyException` propagated as a raw exception or generic adapter error, forcing callers to inspect exception types -- which couples application code to EF Core
- Concurrency conflicts are a routine business condition (two users edit the same record), not an exceptional fault; treating them as a typed error code makes the retry-or-surface decision explicit at the call site
- The error code `"Adapter.{RepositoryName}.ConcurrencyConflict"` is structured and queryable in dashboards, so operational metrics like "concurrency conflict rate per aggregate type" come for free
- Application-layer code can map it to `ApplicationErrorKind.ConcurrencyConflict` for HTTP responses (e.g., 409 Conflict) without exposing the Adapter layer to upper boundaries

<!-- Related commit: a371f83d feat(adapter): ConcurrencyConflict typed error + EfCoreRepositoryBase.Update detection -->

## Bug Fixes

### `UsecaseExceptionPipeline` No Longer Swallows Cancellation Exceptions

`UsecaseExceptionPipeline` is the framework's catch-all that converts unhandled exceptions into typed errors. Previously it caught `OperationCanceledException` along with everything else, which masked client disconnections and request timeouts from the host framework (ASP.NET Core, gRPC). The pipeline now lets `OperationCanceledException` (and `TaskCanceledException`, which inherits from it) propagate so the host can recognize cancelled operations and respond appropriately (e.g., HTTP 499, gRPC `CANCELLED` status).

```csharp
// Pipeline behavior (conceptual)
try
{
    return await next(request, ct);
}
catch (OperationCanceledException)
{
    throw;  // NEW: propagate, do not convert to typed error
}
catch (Exception ex)
{
    return ToApplicationError(ex);
}
```

<!-- Related commit: 15a84d47 fix(pipeline): UsecaseExceptionPipeline cancellation propagation -->

---

### `Validation -> Fin` Conversion Preserves All Errors

When converting `Validation<Error,T>` (which accumulates multiple errors) into `Fin<T>` (which carries one error), the framework previously kept only the first error in the sequence. Multi-error validation results -- the whole reason `Validation` exists -- were silently truncated at the conversion boundary. The conversion now preserves every accumulated error using `Error.Many`, so the entire pipeline (`UsecaseLoggingPipeline`, `UsecaseExceptionPipeline`, the test assertions) sees the full error set.

```csharp
// Before: only the first error survived
Validation<Error, Order> validation = ...; // contains 3 errors
Fin<Order> fin = validation.ToFin();        // fin.Error contains only 1 error

// After: all errors preserved through conversion
Fin<Order> fin = validation.ToFin();        // fin.Error is Error.Many(3 errors)
```

<!-- Related commit: a16107c3 fix(linq): Validation -> Fin conversion preserves all errors -->

## Other Changes

### `FinTLinqExtensions` IO-Only Validation Overloads Removed

Two specialized `SelectMany` overloads on `FinTLinqExtensions` -- the `FinT<IO, A> -> Validation<Error, B>` and `Validation<Error, A> -> FinT<IO, B>` IO-specific variants -- have been removed. The general-purpose overloads parameterized by any monad `M` cover the same scenarios. This is a minor public-surface contraction; if you used these specific overloads explicitly (rare), the generic version compiles in the same expression.

<!-- Related commit: d23503b3 refactor(linq): FinTLinqExtensions Validation IO-only overload removed -->

## API Changes Summary

### Renamed Types (Functorium)

```
Functorium.Abstractions.Errors
├── ErrorType                            -> ErrorKind
├── ErrorCodeFactory (public)            -> ErrorFactory (internal)
├── ErrorCodeExpected                    -> ExpectedError
└── ErrorCodeExceptional                 -> ExceptionalError

Functorium.Domains.Errors
└── DomainErrorType                      -> DomainErrorKind

Functorium.Applications.Errors
└── ApplicationErrorType                 -> ApplicationErrorKind

Functorium.Adapters.Errors
└── AdapterErrorType                     -> AdapterErrorKind
```

### Renamed Members and Constants

```
Functorium.Abstractions.Errors.ErrorKind
├── virtual ErrorName -> Name
└── DomainErrorsPrefix / ApplicationErrorsPrefix / AdapterErrorsPrefix    [Removed: now internal]

Functorium.Abstractions.Errors.ErrorFactory
├── Create(...) -> CreateExpected(...)
├── CreateFromException(...) -> CreateExceptional(...)
└── Format(...)                                                            [Removed]
```

### Renamed Adapter / Testing Companions

```
Functorium.Adapters.Abstractions.Errors.DestructuringPolicies
├── ErrorCodeExpectedDestructurer        -> ExpectedErrorDestructurer
├── ErrorCodeExpectedTDestructurer       -> ExpectedErrorTDestructurer
└── ErrorCodeExceptionalDestructurer     -> ExceptionalErrorDestructurer

Functorium.Testing.Errors
├── ErrorCodeAssertions                  -> ExpectedErrorAssertions
│   ├── ShouldBeErrorCodeExpected        -> ShouldBeExpectedError
│   └── ShouldBeErrorCodeExpected<T>     -> ShouldBeExpectedError<T>
└── ErrorCodeExceptionalAssertions       -> ExceptionalErrorAssertions
    ├── ShouldBeErrorCodeExceptional     -> ShouldBeExceptionalError
    └── ShouldBeErrorCodeExceptional<T>  -> ShouldBeExceptionalError<T>
```

### Architecture Test Contract Relocated (Functorium)

```
Functorium.Domains.ValueObjects.IValueObject
├── CreateMethodName                     -> ArchTestContract.CreateMethodName
├── CreateFromValidatedMethodName        -> ArchTestContract.CreateFromValidatedMethodName
├── ValidateMethodName                   -> ArchTestContract.ValidateMethodName
└── DomainErrorsNestedClassName          -> ArchTestContract.NestedErrorsClassName  [value: "DomainErrors" -> "Domain"]

Functorium.Domains.Entities.IEntity
├── CreateMethodName                     -> ArchTestContract.CreateMethodName
└── CreateFromValidatedMethodName        -> ArchTestContract.CreateFromValidatedMethodName
```

### New Members (Functorium)

```
Functorium.Domains.Repositories.IRepository<TAggregate, TId>
├── FindAllSatisfying(Specification<TAggregate>) -> FinT<IO, Seq<TAggregate>>      [New]
└── FindFirstSatisfying(Specification<TAggregate>) -> FinT<IO, Option<TAggregate>> [New]

Functorium.Applications.Queries.IQueryPort<TEntity, TDto>
├── Count(Specification<TEntity>) -> FinT<IO, int>                                 [New]
└── Exists(Specification<TEntity>) -> FinT<IO, bool>                               [New]
```

### New Members (Functorium.Adapters)

```
Functorium.Adapters.Repositories.EfCoreRepositoryBase<TAggregate, TId, TModel>
├── ConcurrencyConflictError(TId) -> Error                                [New: protected helper]
├── FindAllSatisfying(Specification<TAggregate>) -> FinT<IO, Seq<...>>    [New: virtual]
└── FindFirstSatisfying(Specification<TAggregate>) -> FinT<IO, Option<...>> [New: virtual]

Functorium.Adapters.Repositories.InMemoryRepositoryBase<TAggregate, TId>
├── FindAllSatisfying(Specification<TAggregate>) -> FinT<IO, Seq<...>>    [New: virtual]
└── FindFirstSatisfying(Specification<TAggregate>) -> FinT<IO, Option<...>> [New: virtual]

Functorium.Adapters.Repositories.InMemoryQueryBase<TEntity, TDto>
├── Count(Specification<TEntity>) -> FinT<IO, int>                        [New: virtual]
└── Exists(Specification<TEntity>) -> FinT<IO, bool>                      [New: virtual]
```

### New Error Kinds

```
Functorium.Applications.Errors.ApplicationErrorKind
└── ConcurrencyConflict                  [New]

Functorium.Adapters.Errors.AdapterErrorKind
└── ConcurrencyConflict                  [New]
```

## Installation

### NuGet Package Installation

```bash
# Functorium core library
dotnet add package Functorium --version 1.0.0-alpha.4

# Functorium.Adapters (Repository, Pipeline, Observability)
dotnet add package Functorium.Adapters --version 1.0.0-alpha.4

# Functorium.SourceGenerators (build-time code generation)
dotnet add package Functorium.SourceGenerators --version 1.0.0-alpha.4

# Functorium.Testing (test utilities, optional)
dotnet add package Functorium.Testing --version 1.0.0-alpha.4
```

### Required Dependencies

- .NET 10 or later
- LanguageExt.Core 5.x
- Microsoft.EntityFrameworkCore.Relational (required for `EfCoreRepositoryBase`)
