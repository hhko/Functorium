---
title: "Command Usecase"
---
## Overview

How do you execute the `FinT<IO, T>` returned by Repository and pass it to the API? In Part 3, we designed Repository to return a lazy `FinT`. But in an actual Usecase, this `FinT` must be executed and the result converted to `FinResponse<T>` suitable for HTTP responses. This chapter establishes the Command Usecase structure and builds the complete flow from Repository call to response return.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Define Command requests and handlers with **ICommandRequest / ICommandUsecase** interfaces
2. Compose Repository calls with **FinT\<IO, T\> LINQ syntax**
3. Convert Usecase results to HTTP-friendly format with **FinResponse\<T\>**
4. Cohesively group Request, Response, and Usecase in a single Command class with the **Nested class pattern**

---

## Core Concepts

### Command Usecase Structure

A Command Usecase bundles Request (input), Response (output), and Usecase (logic) in a single envelope class. Opening one file reveals the entire Command contract.

```
CreateProductCommand (envelope)
├── Request   - Input data (ICommandRequest<Response>)
├── Response  - Output data
└── Usecase   - Business logic (ICommandUsecase<Request, Response>)
```

### Mediator Interfaces

Request implements `ICommandRequest<Response>`, and Usecase implements `ICommandUsecase<Request, Response>`. The Handle signature returns `ValueTask<FinResponse<Response>>` and accepts `CancellationToken`.

```csharp
public sealed record Request(string Name, decimal Price) : ICommandRequest<Response>;

public sealed class Usecase(IProductRepository productRepository)
    : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
    {
        // ...
    }
}
```

### Execution Flow

When a Request arrives, the Usecase creates a domain object, saves it to the Repository, and converts the result to a Response. See what types each step handles.

```
Request -> Usecase.Handle(request, ct)
           ├── Product.Create()        (create domain object)
           ├── repository.Create()     (FinT<IO, Product>)
           ├── LINQ select             (Product -> Response conversion)
           ├── .Run().RunAsync()       (IO execution -> Fin<Response>)
           └── .ToFinResponse()        (Fin -> FinResponse conversion)
```

### FinT LINQ Composition

The `FinT<IO, T>` returned by Repository can be naturally composed with LINQ syntax.

```csharp
FinT<IO, Response> usecase =
    from created in productRepository.Create(product)
    select new Response(created.Id.ToString(), created.Name, ...);
```

`from ... in` is `FinT<IO, T>`'s monadic bind, and `select` transforms the result. If Repository returns `Fin.Fail`, subsequent operations are automatically skipped (Railway-oriented programming).

---

## Project Description

The files below constitute the complete structure of a Command Usecase.

| File | Description |
|------|-------------|
| `ProductId.cs` | Ulid-based Product identifier |
| `Product.cs` | Product entity inheriting AggregateRoot |
| `IProductRepository.cs` | Extension interface for IRepository<Product, ProductId> |
| `InMemoryProductRepository.cs` | InMemoryRepositoryBase-based implementation |
| `CreateProductCommand.cs` | Command Usecase pattern (Request, Response, Usecase) |
| `Program.cs` | Execution demo |

---

## Summary at a Glance

A summary of each concept's role in Command Usecase.

| Concept | Description |
|---------|-------------|
| `ICommandRequest<T>` | Command request marker (Mediator ICommand extension) |
| `ICommandUsecase<TCmd, T>` | Command handler (Mediator ICommandHandler extension) |
| `FinT<IO, T>` | Monadic type wrapping IO effect + success/failure |
| `FinResponse<T>` | HTTP response-suitable success/failure wrapper |
| `.ToFinResponse()` | `Fin<T>` -> `FinResponse<T>` conversion extension method |

---

## FAQ

### Q1: Why structure Usecase as nested classes?
**A**: Request, Response, and Usecase are cohesive within a single Command, making code navigation easy and allowing the entire Command contract to be grasped from a single file.

### Q2: What's the difference between FinT and Fin?
**A**: `FinT<IO, T>` is a lazy operation including IO effects. Executing with `.Run().RunAsync()` yields `Fin<T>` (immediate value).

### Q3: Why is ToFinResponse() needed?
**A**: `Fin<T>` is LanguageExt's internal type, while `FinResponse<T>` is Functorium's HTTP-friendly wrapper used in the Pipeline/API layer. It explicitly performs cross-layer conversion.

---

We've created the Command Usecase structure. But list queries need IQueryPort instead of Repository -- how does the Usecase structure change? In the next chapter, we'll examine Query Usecase design.

-> [Chapter 2: Query Usecase](../02-Query-Usecase/)
