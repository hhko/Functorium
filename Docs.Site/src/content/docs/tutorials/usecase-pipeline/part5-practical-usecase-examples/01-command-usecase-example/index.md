---
title: "Command Usecase Example"
---

## Overview

How do Pipelines and FinResponse work in an actual Command Usecase? This section builds a **complete Command Usecase implementation example** using Functorium's `ICommandRequest<TSuccess>` interface. The Nested class pattern is used to cohesively organize Request, Response, Validator, and Handler within a single class, and `FinResponse<T>` is used for type-safe success/failure handling.

```
Command Usecase structure:

CreateProductCommand (top-level class)
├── Request   : ICommandRequest<Response>           ← Request definition
├── Response                                        ← Response definition
├── Validator                                       ← Validation
└── Handler   : ICommandUsecase<Request, Response>  ← Business logic
```

## Learning Objectives

After completing this section, you will be able to:

1. Understand the roles of `ICommandRequest<TSuccess>` and `ICommandUsecase<TCommand, TSuccess>` interfaces and their Pipeline connection
2. Cohesively organize Request/Response/Validator/Handler using the Nested class pattern
3. Use `FinResponse<T>`'s implicit conversions to concisely return success/failure
4. Separate the Validator from the Handler for independent testing

## Key Concepts

### 1. ICommandRequest Interface

`ICommandRequest<TSuccess>` inherits from Mediator's `ICommand<FinResponse<TSuccess>>`. This ensures that the Request automatically passes through Pipelines.

```csharp
// Functorium definition
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>> { }
```

When a Request record implements `ICommandRequest<Response>`, the Mediator Pipeline recognizes the request as a Command and applies Transaction Pipeline, etc.

The Handler implements `ICommandUsecase<TCommand, TSuccess>`. This interface inherits from `ICommandHandler<TCommand, FinResponse<TSuccess>>`, so when a Handler implements it, Mediator automatically registers it in the Pipeline chain:

```csharp
// Functorium definition
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess> { }
```

### 2. Nested Class Pattern

All types related to a single Usecase are nested inside a top-level class.

```csharp
public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);
    public static class Validator { ... }
    public sealed class Handler : ICommandUsecase<Request, Response> { ... }
}
```

Benefits of this pattern:
- **Cohesion**: Related types are gathered in one file for easy navigation.
- **Naming conflict prevention**: Accessed via full path like `CreateProductCommand.Request`.
- **Intent expression**: Command/Query distinction is clear from the class name alone.

### 3. Result Handling via FinResponse

The Handler returns `ValueTask<FinResponse<Response>>`. This is because `ICommandUsecase` requires an async signature. The Validator's result is chained with `Bind` to connect validation and business logic in Railway fashion:

```csharp
public ValueTask<FinResponse<Response>> Handle(Request command, CancellationToken cancellationToken)
{
    var result = Validator.Validate(command)
        .Bind(req =>
        {
            var productId = Guid.NewGuid().ToString("N")[..8];
            return FinResponse.Succ(new Response(productId, req.Name, req.Price));
        });

    return new ValueTask<FinResponse<Response>>(result);
}
```

Using `Bind` eliminates the need for `if (validated.IsFail)` branching -- validation failure is automatically propagated.

### 4. Validator Separation

The Validator is defined as a static class so it can be tested independently from the Handler. The Validator returns `FinResponse<Request>` to deliver validation results in Railway fashion.

```csharp
public static class Validator
{
    public static FinResponse<Request> Validate(Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error.New("Name is required");

        if (request.Price <= 0)
            return Error.New("Price must be positive");

        return request;  // Implicit conversion
    }
}
```

## FAQ

### Q1: Can Request, Response, Validator, and Handler be split into separate files in the Nested class pattern?
**A**: Using `partial class`, each nested type can be defined in a separate file. However, when a single Usecase fits in one file, keeping nested types together makes **navigation and understanding easier**, so keeping them in one file is recommended when nested types are small.

### Q2: `ICommandRequest<TSuccess>` has `TSuccess` as `Response`, so why not use `FinResponse<Response>` directly?
**A**: Since `ICommandRequest<TSuccess>` internally inherits `ICommand<FinResponse<TSuccess>>`, specifying only `TSuccess` **automatically determines** `FinResponse<Response>`. This eliminates the need to explicitly write `FinResponse` wrapping in Usecase code.

### Q3: Why does the Validator return `FinResponse<Request>`?
**A**: When the Validator returns `FinResponse<Request>`, it passes the original Request on validation success and returns a failure response containing `Error` on failure. This enables **Railway-Oriented Programming** style natural chaining of validation results to the Handler.

### Q4: How does the `return Error.New("...")` form work through implicit conversion?
**A**: An `implicit operator` is defined on `FinResponse<A>`, so `Error` type values are automatically converted to `FinResponse<A>.Fail(error)`. Similarly, `A` type values are converted to `FinResponse<A>.Succ(value)`. These implicit conversions significantly reduce boilerplate.

## Project Structure

```
01-Command-Usecase-Example/
├── CommandUsecaseExample/
│   ├── CommandUsecaseExample.csproj
│   ├── CreateProductCommand.cs
│   └── Program.cs
├── CommandUsecaseExample.Tests.Unit/
│   ├── CommandUsecaseExample.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── CreateProductCommandTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project CommandUsecaseExample

# Run tests
dotnet test --project CommandUsecaseExample.Tests.Unit
```

---

How does a read-only Query Usecase differ from a Command? The next section implements a Query Usecase with `IQueryRequest` and caching optimization via `ICacheable`.

→ [Section 5.2: Query Usecase Complete Example](../02-Query-Usecase-Example/)
