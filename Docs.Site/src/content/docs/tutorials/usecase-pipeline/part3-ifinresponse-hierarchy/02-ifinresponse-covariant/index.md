---
title: "IFinResponse Covariant Interface"
---

## Overview

The non-generic `IFinResponse` marker created in Section 1 can check success/failure, but does not provide **type information for the value**. Applying the covariance learned in Part 1, this section adds a **covariant interface** `IFinResponse<out A>` using the `out A` keyword. This enables safe assignment from derived types to base types.

```
IFinResponse                    ← Non-generic marker (Section 1)
└── IFinResponse<out A>         ← Covariant interface (this section)
```

## Learning Objectives

After completing this section, you will be able to:

1. Declare a covariant interface using the `out A` keyword
2. Explain why `IFinResponse<string>` can be assigned to `IFinResponse<object>` due to covariance
3. Understand the inheritance relationship between the non-generic marker and the generic covariant interface
4. Explain the flexibility that covariance provides in Pipelines

## Key Concepts

### 1. Covariant Interface: `out A`

The `out` keyword declares that the type parameter `A` is used only in **output positions**. This allows `IFinResponse<string>` to be assigned to `IFinResponse<object>`.

```csharp
public interface IFinResponse<out A> : IFinResponse
{
}
```

The empty body of `IFinResponse<out A>` is intentional. The role of this interface is not to expose value members, but to **declare covariance for the generic type parameter `A`**. The `IsSucc`/`IsFail` needed by Pipelines are already provided by the parent interface `IFinResponse`, and value access members (`Value`, `Match`, `Map`, etc.) are provided by the implementation `FinResponse<A>`. This is the result of separating interfaces by responsibility so that each level has a single concern.

### 2. Covariant Assignment

Since `string` is a subtype of `object`, covariance enables the following assignment:

```csharp
IFinResponse<string> stringResponse = CovariantResponse<string>.Succ("Hello");

// Covariance: IFinResponse<string> → IFinResponse<object> assignable
IFinResponse<object> objectResponse = stringResponse;
```

### 3. Also Assignable to Non-Generic Marker

Since `IFinResponse<out A>` inherits from `IFinResponse`, it can also be assigned to the non-generic marker type:

```csharp
IFinResponse<string> stringResponse = CovariantResponse<string>.Succ("Hello");

// IFinResponse<string> → IFinResponse assignable (inheritance)
IFinResponse nonGeneric = stringResponse;
nonGeneric.IsSucc; // true
```

### 4. Usage in Pipelines

Leveraging covariance allows Pipelines to handle responses more flexibly. For example, a logging Pipeline that accepts `IFinResponse<object>` can process all `IFinResponse<T>` responses.

```csharp
public void LogAnyResponse(IFinResponse<object> response)
{
    Console.WriteLine(response.IsSucc ? "Success" : "Fail");
}
```

## FAQ

### Q1: `IFinResponse<out A>` has no value-returning members -- does the covariant interface have any meaning?
**A**: While `IFinResponse<out A>` currently has no explicit value access members, it **conveys type information** through the type parameter `A`. The covariant declaration (`out`) enables assigning `IFinResponse<string>` to `IFinResponse<object>`, providing the foundation for Pipelines to handle various response types generically.

### Q2: Why are `IFinResponse` (non-generic) and `IFinResponse<out A>` (covariant) separated?
**A**: When a Pipeline only needs the **success/failure status**, the non-generic `IFinResponse` is sufficient. Introducing the type parameter `A` would cause unnecessary generic propagation. Separation allows each Pipeline to require only the **minimum type information** it needs.

### Q3: What practical difference does covariance make in Pipeline code?
**A**: Thanks to `IFinResponse<out A>`'s covariance, a Handler response returning `IFinResponse<ProductDto>` can be used directly in a logging utility that accepts `IFinResponse<object>`. Without covariance, explicit casting would be required each time, complicating the code.

## Project Structure

```
02-IFinResponse-Covariant/
├── FinResponseCovariant/
│   ├── FinResponseCovariant.csproj
│   ├── IFinResponse.cs
│   ├── CovariantResponse.cs
│   └── Program.cs
├── FinResponseCovariant.Tests.Unit/
│   ├── FinResponseCovariant.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseCovariantTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project FinResponseCovariant

# Run tests
dotnet test --project FinResponseCovariant.Tests.Unit
```

---

Now that Pipelines can read responses, it's time to tackle requirement R2. Using CRTP and `static abstract`, we design a factory interface that creates failure responses without reflection.

→ [Section 3.3: IFinResponseFactory CRTP Factory](../03-IFinResponseFactory-CRTP/)
