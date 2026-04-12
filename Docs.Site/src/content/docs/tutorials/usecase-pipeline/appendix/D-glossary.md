---
title: "Glossary"
---

## Overview

This section compiles the key terms used in this tutorial.

---

## Generic Variance

### Covariance

The property where a generic type parameter preserves the inheritance relationship in the **same direction**. In C#, it is declared with the `out` keyword. If `Dog : Animal`, then `IEnumerable<Dog>` can be assigned to `IEnumerable<Animal>`. It can only be used in **output positions** (return types).

### Contravariance

The property where a generic type parameter preserves the inheritance relationship in the **opposite direction**. In C#, it is declared with the `in` keyword. If `Dog : Animal`, then `Action<Animal>` can be assigned to `Action<Dog>`. It can only be used in **input positions** (parameters).

### Invariance

The property where a generic type parameter does not preserve the inheritance relationship. `List<Dog>` cannot be assigned to `List<Animal>`. A type parameter declared without `in` or `out` is invariant.

---

## Type System

### CRTP (Curiously Recurring Template Pattern)

A pattern where a type passes itself as its own type parameter. Declared as `IFinResponseFactory<TSelf> where TSelf : IFinResponseFactory<TSelf>`, it allows defining methods on an interface that return the implementing type.

### static abstract

A feature introduced in C# 11. It declares static methods in interfaces and forces implementing types to provide implementations. In generic constraints, it enables calls like `TSelf.CreateFail(error)`, allowing factory methods to be used without reflection.

### Discriminated Union

A type where one type represents exactly one of several fixed cases. `FinResponse<A>` is a Discriminated Union with two cases: `Succ` and `Fail`. F# supports this at the language level, while in C# it is implemented using a sealed record hierarchy.

### sealed struct

A value type that cannot be inherited. Since `Fin<T>` is a sealed struct, it cannot be used as an interface constraint (`where T : Fin<T>`). This is the core reason why the `FinResponse<A>` wrapper was designed.

---

## Functional Programming

### Monad

A pattern that wraps a value (`unit/return`) and applies a function to the wrapped value (`bind/flatMap`). `FinResponse<A>` supports monadic composition through its `Bind` method.

### Railway Oriented Programming (ROP)

An error handling pattern where every function returns two tracks (success/failure) and subsequent steps are skipped on failure. The `Map`/`Bind` chains of `FinResponse<A>` implement this pattern.

### Match (Pattern Matching)

An operation that executes a different function for each case of a Discriminated Union. `response.Match(Succ: ..., Fail: ...)` branches on success/failure.

### Map (Mapping)

An operation that transforms the inner value while preserving the context (success/failure). `response.Map(x => x.ToString())` transforms the value on success and passes through the error on failure.

### Bind

An operation that extracts the value and creates a new context. `response.Bind(x => FindUser(x))` calls `FindUser` on success (which may fail), and passes through the error on failure.

---

## Architecture

### Pipeline

A middleware chain that runs before/after a request reaches the Handler. There are 7 built-in Pipelines (Metrics, Tracing, Logging, Validation, Caching, Exception, Transaction) plus a Custom Pipeline slot (8 slots total), allowing user-defined Pipelines to be added through the Custom Pipeline slot.

### Mediator

An intermediary between requestors and handlers. It routes requests to the appropriate Handler and automatically applies Pipelines. Functorium uses the [Mediator](https://github.com/martinothamar/Mediator) library.

### Pipeline Behavior

An interface that implements Pipelines in Mediator. By implementing `IPipelineBehavior<TMessage, TResponse>`, logic is inserted before/after request handling.

### CQRS (Command Query Responsibility Segregation)

A pattern that separates the responsibilities of commands (writes) and queries (reads). `ICommandRequest<T>` handles state changes, while `IQueryRequest<T>` handles data retrieval.

### Usecase

A unit representing a single business operation. It includes Request, Response, Validator, and Handler, cohesively organized into a single class using the Nested class pattern.

---

## Interfaces

### IFinResponse

A non-generic marker interface providing `IsSucc`/`IsFail` properties. Used in Pipelines to read the response status.

### IFinResponse\<out A\>

A covariant generic interface. It inherits from `IFinResponse` and supports covariance via `out A`.

### IFinResponseFactory\<TSelf\>

A CRTP factory interface. It defines the `static abstract TSelf CreateFail(Error error)` method. Used in Pipelines to create failure responses without reflection.

### IFinResponseWithError

An interface providing the `Error` property. Implemented only in `FinResponse<A>.Fail`, and used in the Logging Pipeline to access error messages.

### ICommandRequest\<TSuccess\>

A Command request interface. Inherits from `ICommand<FinResponse<TSuccess>>`.

### IQueryRequest\<TSuccess\>

A Query request interface. Inherits from `IQuery<FinResponse<TSuccess>>`.

### ICacheable

An interface representing a cacheable request. It defines `CacheKey` and `Duration` properties. When a Query Request implements this, the Caching Pipeline automatically applies caching.

---

The following appendix compiles reference materials that are helpful for learning the concepts covered in this tutorial, including C# generic variance, static abstract, LanguageExt, Mediator, ROP, CQRS, and more.

→ [Appendix E: References](E-references.md)
