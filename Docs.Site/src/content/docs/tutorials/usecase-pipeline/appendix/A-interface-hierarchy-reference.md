---
title: "IFinResponse Hierarchy Reference"
---

## Overview

This appendix is a reference document that provides a complete overview of Functorium's IFinResponse interface hierarchy at a glance. It describes in detail the role of each interface, its constraints, and how it is implemented in `FinResponse<A>`.

---

## Interface Hierarchy Diagram

```
IFinResponse                              Non-generic marker (IsSucc/IsFail)
├── IFinResponse<out A>                   Covariant interface (read-only)
│
IFinResponseFactory<TSelf>                CRTP factory (CreateFail)
│
IFinResponseWithError                     Error access (Error property)
│
FinResponse<A>                            Discriminated Union
├── : IFinResponse<A>                     Covariant interface implementation
├── : IFinResponseFactory<FinResponse<A>> CRTP factory implementation
│
├── sealed record Succ(A Value)           Success case
│
└── sealed record Fail(Error Error)       Failure case
    └── : IFinResponseWithError           Error access only in Fail
```

---

## Interface Details

### 1. IFinResponse (Non-Generic Marker)

```csharp
public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}
```

| Item | Description |
|------|------|
| **Role** | Minimal interface for reading success/failure status in Pipelines |
| **Variance** | None (non-generic) |
| **Used by Pipelines** | Logging, Tracing, Metrics, Transaction, Caching |
| **Used as constraint** | `where TResponse : IFinResponse` |

### 2. IFinResponse\<out A\> (Covariant Interface)

```csharp
public interface IFinResponse<out A> : IFinResponse
{
}
```

| Item | Description |
|------|------|
| **Role** | Generic extension supporting covariance |
| **Variance** | Covariant (`out A`) |
| **Inheritance** | Inherits from `IFinResponse` |
| **Meaning** | `FinResponse<Dog>` can be referenced as `IFinResponse<Animal>` |

### 3. IFinResponseFactory\<TSelf\> (CRTP Factory)

```csharp
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
```

| Item | Description |
|------|------|
| **Role** | Factory for creating failure responses in Pipelines |
| **Pattern** | CRTP (Curiously Recurring Template Pattern) |
| **Key method** | `static abstract TSelf CreateFail(Error error)` |
| **Used by Pipelines** | Validation, Exception (Create-Only), and all Read+Create Pipelines |
| **Used as constraint** | `where TResponse : IFinResponseFactory<TResponse>` |

### 4. IFinResponseWithError (Error Access)

```csharp
public interface IFinResponseWithError
{
    Error Error { get; }
}
```

| Item | Description |
|------|------|
| **Role** | Access Error information on failure |
| **Implementation** | Implemented only in `FinResponse<A>.Fail` |
| **Usage** | `if (response is IFinResponseWithError fail) { ... fail.Error ... }` |
| **Used by Pipelines** | Logging Pipeline (recording error messages) |

---

## FinResponse\<A\> Implementation Details

### Abstract Record

```csharp
public abstract record FinResponse<A> : IFinResponse<A>, IFinResponseFactory<FinResponse<A>>
```

### Succ Case

```csharp
public sealed record Succ(A Value) : FinResponse<A>
{
    public override bool IsSucc => true;
    public override bool IsFail => false;
}
```

### Fail Case

```csharp
public sealed record Fail(Error Error) : FinResponse<A>, IFinResponseWithError
{
    public override bool IsSucc => false;
    public override bool IsFail => true;
    Error IFinResponseWithError.Error => Error;
}
```

### Key Methods

| Method | Signature | Description |
|--------|----------|------|
| `Match` | `B Match<B>(Func<A, B> Succ, Func<Error, B> Fail)` | Branch on success/failure |
| `Match` (void) | `void Match(Action<A> Succ, Action<Error> Fail)` | Execute branch (no return) |
| `Map` | `FinResponse<B> Map<B>(Func<A, B> f)` | Transform success value |
| `MapFail` | `FinResponse<A> MapFail(Func<Error, Error> f)` | Transform failure error |
| `BiMap` | `FinResponse<B> BiMap<B>(Func<A, B> Succ, Func<Error, Error> Fail)` | Transform both success/failure |
| `Bind` | `FinResponse<B> Bind<B>(Func<A, FinResponse<B>> f)` | Monadic bind |
| `BiBind` | `FinResponse<B> BiBind<B>(Func<A, FinResponse<B>> Succ, Func<Error, FinResponse<B>> Fail)` | Bidirectional monadic bind |
| `BindFail` | `FinResponse<A> BindFail(Func<Error, FinResponse<A>> Fail)` | Failure track bind |
| `Select` | `FinResponse<B> Select<B>(Func<A, B> f)` | LINQ select support |
| `SelectMany` | `FinResponse<C> SelectMany<B, C>(...)` | LINQ from/select support |
| `ThrowIfFail` | `A ThrowIfFail()` | Throws on failure, returns value on success |
| `IfFail` (value) | `A IfFail(A alternative)` | Returns alternative value on failure |
| `IfFail` (Func) | `A IfFail(Func<Error, A> Fail)` | Generates alternative value via function on failure |
| `IfFail` (Action) | `void IfFail(Action<Error> Fail)` | Executes side effect on failure |
| `IfSucc` | `void IfSucc(Action<A> Succ)` | Executes side effect on success |
| `CreateFail` | `static FinResponse<A> CreateFail(Error error)` | CRTP factory implementation |

### Implicit Conversion Operators

```csharp
// Value → FinResponse (Succ)
public static implicit operator FinResponse<A>(A value) => new Succ(value);

// Error → FinResponse (Fail)
public static implicit operator FinResponse<A>(Error error) => new Fail(error);
```

### Boolean and Choice Operators

```csharp
// Boolean operators — supports if/else pattern matching
public static bool operator true(FinResponse<A> ma) => ma.IsSucc;
public static bool operator false(FinResponse<A> ma) => ma.IsFail;

// Choice operator — selects the first successful value
public static FinResponse<A> operator |(FinResponse<A> lhs, FinResponse<A> rhs) =>
    lhs.IsSucc ? lhs : rhs;
```

### Static Factories (FinResponse Class)

```csharp
public static class FinResponse
{
    public static FinResponse<A> Succ<A>(A value) => new FinResponse<A>.Succ(value);
    public static FinResponse<A> Succ<A>() where A : new() => new FinResponse<A>.Succ(new A());
    public static FinResponse<A> Fail<A>(Error error) => new FinResponse<A>.Fail(error);
}
```

---

## Pipeline Constraint Matrix

```
Pipeline                    TResponse Constraint                             Capability
──────────────────────────  ───────────────────────────────────────────────  ────────────
Metrics Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Tracing Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Logging Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Validation Pipeline         IFinResponseFactory<TResponse>                   CreateFail
Caching Pipeline            IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Exception Pipeline          IFinResponseFactory<TResponse>                   CreateFail
Transaction Pipeline        IFinResponse, IFinResponseFactory<TResponse>     Read + Create
Custom Pipeline             (User-defined)                                   Varies
```

---

The following appendix provides a comparative analysis of the interface constraint approach versus alternative approaches such as reflection, dynamic, and Source Generators.

→ [Appendix B: Pipeline Constraints vs Alternatives](B-constraint-vs-alternatives.md)

