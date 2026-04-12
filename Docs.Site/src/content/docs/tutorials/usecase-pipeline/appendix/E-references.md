---
title: "References"
---

## Overview

This section compiles reference materials that are helpful for learning the concepts covered in this tutorial.

---

## C# Language and .NET Documentation

### Generic Variance

- [Covariance and Contravariance in Generics - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/generics/covariance-and-contravariance)
- [Covariance and Contravariance (C# Programming Guide)](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/covariance-contravariance/)
- [out (generic modifier) - C# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/out-generic-modifier)
- [in (generic modifier) - C# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/in-generic-modifier)

### static abstract Members

- [Static abstract members in interfaces - C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11#generic-math-support)
- [Tutorial: Explore static virtual members in interfaces](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members)

### Record Types

- [Records - C# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
- [Use record types - C# Tutorial](https://learn.microsoft.com/en-us/dotnet/csharp/tutorials/records)

---

## Libraries

### LanguageExt

A functional programming library for C#. It provides monadic types such as `Fin<T>`, `Option<T>`, `Either<L, R>`, and more.

- [GitHub - louthy/language-ext](https://github.com/louthy/language-ext)
- [LanguageExt Documentation](https://louthy.github.io/language-ext/)

### Mediator

A high-performance .NET Mediator pattern library. It routes requests without reflection, based on Source Generators.

- [GitHub - martinothamar/Mediator](https://github.com/martinothamar/Mediator)

---

## Functional Programming

### Railway Oriented Programming

- [Railway Oriented Programming - Scott Wlaschin](https://fsharpforfunandprofit.com/rop/)
- [Against Railway-Oriented Programming - Scott Wlaschin](https://fsharpforfunandprofit.com/posts/against-railway-oriented-programming/)

### Functional C#

- [Functional Programming in C# - Enrico Buonanno (Manning)](https://www.manning.com/books/functional-programming-in-c-sharp-second-edition)

---

## Design Patterns

### CQRS

- [CQRS Pattern - Microsoft Learn](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Martin Fowler - CQRS](https://martinfowler.com/bliki/CQRS.html)

### Mediator Pattern

- [Mediator Pattern - Refactoring Guru](https://refactoring.guru/design-patterns/mediator)
- [Pipeline Pattern / Chain of Responsibility](https://refactoring.guru/design-patterns/chain-of-responsibility)

### CRTP (Curiously Recurring Template Pattern)

- [CRTP in C# - Wikipedia](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern)

---

## Functorium Source Files

The following is a list of key Functorium source files covered in this tutorial.

### IFinResponse Interface Hierarchy

| File | Description |
|------|------|
| `Src/Functorium/Applications/Usecases/IFinResponse.cs` | Interface definitions (IFinResponse, IFinResponseFactory, etc.) |
| `Src/Functorium/Applications/Usecases/IFinResponse.Impl.cs` | FinResponse\<A\> record (Succ/Fail, Match/Map/Bind) |
| `Src/Functorium/Applications/Usecases/IFinResponse.Factory.cs` | FinResponse static factory class |
| `Src/Functorium/Applications/Usecases/IFinResponse.FinConversions.cs` | Fin\<A\> → FinResponse\<A\> conversion extension methods |

### Command/Query Interfaces

| File | Description |
|------|------|
| `Src/Functorium/Applications/Usecases/ICommandRequest.cs` | ICommandRequest\<TSuccess\>, ICommandUsecase |
| `Src/Functorium/Applications/Usecases/IQueryRequest.cs` | IQueryRequest\<TSuccess\>, IQueryUsecase |
| `Src/Functorium/Applications/Usecases/ICacheable.cs` | ICacheable interface |

### Pipeline Implementations

All files are located in the `Src/Functorium.Adapters/Observabilities/Pipelines/` directory.

| File | Description |
|------|------|
| `UsecasePipelineBase.cs` | Common helper base class (CQRS type identification, handler name extraction) |
| `UsecaseMetricsPipeline.cs` | Metrics Pipeline (Read + Create) |
| `UsecaseTracingPipeline.cs` | Tracing Pipeline (Read + Create) |
| `UsecaseLoggingPipeline.cs` | Logging Pipeline (Read + Create) |
| `UsecaseValidationPipeline.cs` | Validation Pipeline (CreateFail) |
| `UsecaseCachingPipeline.cs` | Caching Pipeline (Read + Create, Query only) |
| `UsecaseExceptionPipeline.cs` | Exception Pipeline (CreateFail) |
| `UsecaseTransactionPipeline.cs` | Transaction Pipeline (Read + Create, Command only) |
| `ICustomUsecasePipeline.cs` | Custom Pipeline marker interface (for Scrutor auto-discovery) |
| `UsecaseMetricCustomPipelineBase.cs` | Custom Metric Pipeline base class |
| `UsecaseTracingCustomPipelineBase.cs` | Custom Tracing Pipeline base class |
| `IUsecaseCtxEnricher.cs` | Log custom property Enricher interface |

---

## Related Tutorials

| Tutorial | Location | Description |
|------|------|------|
| Separating Commands and Queries with the CQRS Pattern | `Docs.Site/src/content/docs/tutorials/cqrs-repository/` | From CQRS pattern basics to Usecase integration |
