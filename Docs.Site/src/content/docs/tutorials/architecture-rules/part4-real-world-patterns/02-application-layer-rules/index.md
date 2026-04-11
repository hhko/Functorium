---
title: "Application Layer Rules"
---

## Overview

What if a Command's `Request` is written as a regular class instead of a `record`? What if a `Response` has a public setter? The Mediator pipeline only reveals these structural problems at runtime. When Command/Query pattern rules are not followed, the entire pipeline becomes unstable.

In this chapter, we verify the application layer based on the Command/Query pattern using architecture tests. We **automatically enforce the pattern where each use case is composed of nested classes (Request, Response, Usecase)**.

> **"The pattern of bundling Request, Response, and Usecase into a single use case is powerful, but when someone breaks the structure, the pipeline breaks. Tests guard the structure."**

## Learning Objectives

### Core Learning Goals

1. **Defining structural rules for the Command/Query pattern**
   - Each use case nests Request, Response, and Usecase inside a single sealed class
   - A pattern that increases cohesion by bundling related types into a single unit

2. **Combining `RequireNestedClass` and `RequireRecord`**
   - Verify nested class existence with `RequireNestedClass`
   - Enforce that Request/Response are record types with `RequireRecord()`

3. **Verifying DTO class property rules**
   - Enforce `init`-only properties with `RequireNoPublicSetters()`
   - A pattern that ensures DTO immutability

### What You Will Verify Through Practice
- **CreateOrder (Command)**: sealed record Request/Response + sealed Usecase inside a sealed class
- **GetOrderById (Query)**: Verify the same nested structure
- **OrderDto**: Verify sealed, no public setters rules

## Domain Code Structure

### Command/Query Pattern

```
Applications/
├── ICommandUsecase.cs    # Command interface
├── IQueryUsecase.cs      # Query interface
├── CreateOrder.cs        # Command (nested: Request, Response, Usecase)
├── GetOrderById.cs       # Query (nested: Request, Response, Usecase)
└── Dtos/
    └── OrderDto.cs       # DTO
```

Each use case nests related types inside a single sealed class:

```csharp
public sealed class CreateOrder
{
    public sealed record Request(string CustomerName);
    public sealed record Response(Guid OrderId, bool Success);

    public sealed class Usecase : ICommandUsecase<Request>
    {
        public Task ExecuteAsync(Request request) => Task.CompletedTask;
    }
}
```

This pattern **bundles related types into a single unit** to increase cohesion.

## Test Code Walkthrough

### Nested Class Structure Verification

Select a specific class with `HaveName`, then verify its internal structure with `RequireNestedClass`:

```csharp
ArchRuleDefinition.Classes()
    .That()
    .HaveName("CreateOrder")
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNestedClass("Request", nested => nested
            .RequireSealed()
            .RequireRecord())
        .RequireNestedClass("Response", nested => nested
            .RequireSealed()
            .RequireRecord())
        .RequireNestedClass("Usecase", nested => nested
            .RequireSealed()),
        verbose: true)
    .ThrowIfAnyFailures("Command Structure Rule");
```

### DTO Property Rules

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DtoNamespace)
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNoPublicSetters(),
        verbose: true)
    .ThrowIfAnyFailures("DTO Rule");
```

**`RequireNoPublicSetters()`** enforces that DTOs have only `init`-only properties. Using `init` instead of `set` allows values to be set only during object initialization.

## Summary at a Glance

The following table compares the filter strategies and rules for each verification target in the application layer.

### Application Layer Verification Rules

| Target | Filter Strategy | Verification Rule | Core Intent |
|--------|----------------|-------------------|-------------|
| **Command/Query** | `HaveName` (specific class) | sealed, nested Request/Response/Usecase | Unify use case structure |
| **Request/Response** | Internal `RequireNestedClass` verification | sealed record | Ensure immutable DTOs |
| **Usecase** | `HaveNameEndingWith("Usecase")` | sealed, interface implementation | Pipeline compatibility |
| **DTO** | `ResideInNamespace(DtoNamespace)` | sealed, no public setters | Immutability of externally exposed data |

The following table shows the difference between `RequireRecord()` and `RequireImmutable()` in nested class verification.

### Record vs Immutable Verification Comparison

| Verification Method | What It Verifies | Suitable Target |
|--------------------|-----------------|-----------------|
| **`RequireRecord()`** | Whether it is a C# record type | Request, Response (concise DTOs) |
| **`RequireImmutable()`** | 6-dimension immutability | Domain objects (complex immutable classes) |

## Functorium Pre-Built Test Suites

Functorium provides architecture rules for domain/application layers as **pre-built abstract classes**. Simply inheriting in your project automatically applies the rules.

| Suite | Test Count | Verification Targets |
|-------|-----------|---------------------|
| `DomainArchitectureTestSuite` | 21 | AggregateRoot, Entity, ValueObject, DomainEvent, Specification, DomainService |
| `ApplicationArchitectureTestSuite` | 4 | Command/Query Validator, Usecase nested classes |

### Usage

Both Suites only require overriding `Architecture` and a namespace:

```csharp
public sealed class DomainArchTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader().LoadAssemblies(typeof(Order).Assembly).Build();

    protected override string DomainNamespace { get; } =
        typeof(Order).Namespace!;
}

public sealed class ApplicationArchTests : ApplicationArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader().LoadAssemblies(typeof(CreateOrderCommand).Assembly).Build();

    protected override string ApplicationNamespace { get; } =
        "MyApp.Application";
}
```

### ApplicationArchitectureTestSuite (4 tests)

`ApplicationArchitectureTestSuite` automatically verifies the Command/Query pattern structure:

1. **Command_ShouldHave_ValidatorNestedClass** -- If a Command has a Validator, it must be sealed + implement `AbstractValidator`
2. **Command_ShouldHave_UsecaseNestedClass** -- Command must have a Usecase, sealed + implement `ICommandUsecase`
3. **Query_ShouldHave_ValidatorNestedClass** -- If a Query has a Validator, it must be sealed + implement `AbstractValidator`
4. **Query_ShouldHave_UsecaseNestedClass** -- Query must have a Usecase, sealed + implement `IQueryUsecase`

`RequireImplementsGenericInterface("ICommandUsecase")` / `RequireImplementsGenericInterface("IQueryUsecase")` are used to verify generic interface implementation. `RequireNestedClassIfExists` is used for optional nested classes like Validators, while `RequireNestedClass` is used for required nested classes like Usecases.

For detailed usage, virtual property customization, and comparison with manual rules, see [4-05 Architecture Test Suites](../05-Architecture-Test-Suites/).

### Adding Custom Rules

After inheriting a Suite, you can freely define project-specific additional rules:

```csharp
public sealed class DomainArchTests : DomainArchitectureTestSuite
{
    // 21 Suite rules automatically inherited

    // Project-specific additional rule
    [Fact]
    public void Entity_ShouldNotDependOn_ExternalHttpClient()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoDependencyOn("HttpClient"),
                verbose: true)
            .ThrowIfAnyFailures("Entity No HttpClient Rule");
    }
}
```

## FAQ

### Q1: Why enforce Request/Response as records?
**A**: Records automatically provide value-based equality, `ToString()`, and deconstruction. For simple data transfer objects like Request/Response, records are the most suitable. Additionally, `sealed record` generates `init`-only properties by default, ensuring immutability.

### Q2: Can't the nested class pattern be separated into individual files instead?
**A**: Technically possible, but nesting as `CreateOrder.Request`, `CreateOrder.Response`, `CreateOrder.Usecase` keeps related types cohesive under a single namespace. A major advantage is that typing `CreateOrder.` in the IDE shows all related types.

### Q3: What is the difference between `RequireNoPublicSetters()` and `RequireImmutable()`?
**A**: `RequireNoPublicSetters()` only checks for the absence of public setters. It is suitable for DTOs that use `init` properties. `RequireImmutable()` verifies 6 dimensions including constructors, fields, mutable collections, and state-mutating methods, making it more suitable for domain objects.

### Q4: How do you verify interface implementation on a Usecase class?
**A**: Chain `RequireImplements()` like `RequireNestedClass("Usecase", nested => nested.RequireSealed().RequireImplements("ICommandUsecase"))` to enforce specific interface implementation.

---

By enforcing the Command/Query structure of the application layer through tests, the same pattern is automatically guaranteed every time a new use case is added. The next chapter examines how to verify the relationship between port interfaces and adapter implementations.

-> [Ch 3: Adapter Layer Rules](../03-Adapter-Layer-Rules/)
