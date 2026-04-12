---
title: "ROP Reference"
---

## Overview

**Railway Oriented Programming (ROP)** is a functional error handling pattern proposed by Scott Wlaschin. Every function returns either a success (Success) or failure (Failure) track, and when a failure occurs, subsequent steps are skipped and the failure propagates along the failure track. Functorium's `FinResponse<A>` is a C# implementation of this ROP pattern.

---

## Railway Model

### Two Tracks

```
Success track:  ──── f1 ────── f2 ────── f3 ────── Result
                      │           │           │
Failure track:  ──────────────────────────────────── Error
```

Every function has two outputs:
- **Success (Succ)**: Passes the value to the next function
- **Failure (Fail)**: Skips remaining functions and propagates the error

### Switch Functions

Each function acts like a "switch (branching point)":

```
Input → [Function] → Success output (to next step)
                   └→ Failure output (to failure track)
```

---

## FinResponse\<A\>와 ROP

### 1. FinResponse Is a Two-Track Type

```csharp
// Success track
FinResponse<A>.Succ(value)    // contains value

// Failure track
FinResponse<A>.Fail(error)    // contains error
```

`FinResponse<A>` is always in one of these two states. It represents the two tracks as a Discriminated Union in C#.

### 2. Match Is Track Branching

```csharp
result.Match(
    Succ: value => $"Success: {value}",     // Success track handling
    Fail: error => $"Failure: {error}");    // Failure track handling
```

`Match` executes a different function depending on which track the value is currently on.

### 3. Map Transforms the Success Track

```csharp
FinResponse<int> length = name.Map(s => s.Length);
```

```
Success track:  ──── "hello" ──── [Map: s.Length] ──── 5
                                        │
Failure track:  ──── error ──────────────────────────── error (passed through)
```

`Map` transforms only the value on the success track. If on the failure track, the function is not executed and the error is passed through as-is.

### 4. Bind Connects Switches

```csharp
FinResponse<User> user = userId
    .Bind(id => FindUser(id))      // FindUser may fail
    .Bind(u => ValidateUser(u));   // ValidateUser may fail
```

```
Success track:  ──── id ──── [FindUser] ──── user ──── [ValidateUser] ──── validUser
                                  │                          │
Failure track:  ──────────────── error ──────────────────── error
```

`Bind` connects switch functions. If any step fails, subsequent steps are skipped.

### 5. Composing Railways with LINQ Syntax

Using C#'s LINQ query syntax, Bind chains can be written more readably:

```csharp
// LINQ syntax (easy to read)
var result =
    from request in Validate(input)
    from product in CreateProduct(request)
    from saved in SaveProduct(product)
    select saved;

// The above is equivalent to
var result = Validate(input)
    .Bind(request => CreateProduct(request))
    .Bind(product => SaveProduct(product));
```

This is possible because `FinResponse<A>` implements `Select` and `SelectMany`:

```csharp
public FinResponse<B> Select<B>(Func<A, B> f) => Map(f);

public FinResponse<C> SelectMany<B, C>(
    Func<A, FinResponse<B>> bind,
    Func<A, B, C> project) =>
    Bind(a => bind(a).Map(b => project(a, b)));
```

---

## Relationship Between Pipelines and ROP

Mediator Pipelines have a structure similar to ROP:

```
Request → [Validation] → [Logging] → [Tracing] → [Handler] → Response
                │            │           │            │
                └──── Fail ──┴─── Fail ──┴─── Fail ──┘
```

Each Pipeline acts like a switch:
- **Success**: Calls the next Pipeline (`next()`)
- **Failure**: Returns a failure response via `TResponse.CreateFail(error)` and skips subsequent Pipelines

---

## Fin\<T\> vs FinResponse\<A\>

Both types support ROP, but they differ critically in whether they can be used as Pipeline constraints.

| Item | Fin\<T\> (LanguageExt) | FinResponse\<A\> (Functorium) |
|------|----------------------|-------------------------------|
| **Type** | sealed struct | abstract record |
| **Pipeline constraint** | Not possible (sealed struct) | Possible (implements interfaces) |
| **ROP methods** | Match, Map, Bind | Match, Map, Bind |
| **LINQ support** | O | O |
| **Conversion** | - | `Fin<A>.ToFinResponse()` |
| **Factory** | `Fin.Succ`, `Fin.Fail` | `FinResponse.Succ`, `FinResponse.Fail` |

`Fin<T>` is used in the Repository layer, while `FinResponse<A>` is used in the Usecase/Pipeline layer. The `ToFinResponse()` extension method bridges these two layers.

---

## References

- [Railway Oriented Programming - Scott Wlaschin](https://fsharpforfunandprofit.com/rop/)
- [LanguageExt - Fin\<T\>](https://github.com/louthy/language-ext)

---

The following appendix compiles key terms related to generic variance, type systems, functional programming, and architecture used in this tutorial.

→ [Appendix D: Glossary](D-glossary.md)

