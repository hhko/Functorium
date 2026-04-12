---
title: "IFinResponseFactory CRTP"
---

## Overview

**Requirement R2**: Pipelines must be able to create failure responses directly. In Sections 1 and 2, we enabled Pipelines to **read** responses, but what about cases where **failure responses must be created**, like in the Validation Pipeline? This section uses **CRTP (Curiously Recurring Template Pattern)** and C# 11's **static abstract** methods to design a factory interface that allows Pipelines to call `TResponse.CreateFail(error)` without reflection.

```
IFinResponseFactory<TSelf>      ← CRTP factory (this section)
├── static abstract CreateFail(Error error) → TSelf
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain what CRTP is and why it is needed
2. Define and implement C# 11 `static abstract` methods in interfaces
3. Write Pipeline constraints that call `TResponse.CreateFail(error)`
4. Understand the principle by which a CRTP factory eliminates reflection

## Key Concepts

### 1. CRTP (Curiously Recurring Template Pattern)

CRTP is a pattern where a type passes itself as its own type parameter. Since `TSelf` references the type itself, `CreateFail`'s return type is the exact implementing type.

```csharp
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
```

### 2. C# 11 static abstract Methods

`static abstract` **forces implementation of static methods** in interfaces. This enables calls in the form `T.Method()` in generic constraints.

```csharp
public record FactoryResponse<A> : IFinResponseFactory<FactoryResponse<A>>
{
    // static abstract implementation
    public static FactoryResponse<A> CreateFail(Error error) => new(error);
}
```

### 3. Usage in Pipelines

With the `where TResponse : IFinResponseFactory<TResponse>` constraint, Pipelines can **directly call** `TResponse.CreateFail(error)`. No reflection is needed.

The key thing to note is that thanks to the CRTP constraint, `TResponse.CreateFail` returns the exact implementing type.

```csharp
public static TResponse ValidateAndCreate<TResponse>(
    bool isValid,
    Func<TResponse> onSuccess,
    string errorMessage)
    where TResponse : IFinResponseFactory<TResponse>
{
    if (!isValid)
    {
        // static abstract call - no reflection!
        return TResponse.CreateFail(Error.New(errorMessage));
    }
    return onSuccess();
}
```

### 4. Why Is CRTP Needed?

With a regular interface, the return type of a `static abstract` method cannot be specified as **the type itself**. The CRTP `TSelf` constraint is needed so that `CreateFail` returns the exact implementing type.

```csharp
// Without CRTP: return type is ambiguous
public interface IFactory
{
    static abstract ??? CreateFail(Error error);  // Cannot specify return type
}

// With CRTP: return type is exact
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);  // TSelf = implementing type
}
```

## FAQ

### Q1: Can't a factory be defined with a regular interface without CRTP?
**A**: There is no way to specify the return type of a `static abstract` method as the type itself in a regular interface. If defined as `IFactory.CreateFail(Error)`, the return type would be `IFactory`, requiring downcasting to the implementing type. The CRTP `TSelf` constraint is needed so that `CreateFail` returns the **exact implementing type**.

### Q2: How was `static abstract` substituted before C# 11?
**A**: Before C# 11, without `static abstract`, implementing the factory pattern required either **injecting a separate factory class** via DI or calling static methods through reflection. The introduction of `static abstract` enabled defining factory contracts at the interface level, which is the key technology for eliminating reflection.

### Q3: How is `TResponse.CreateFail(error)` possible without reflection?
**A**: Thanks to the `where TResponse : IFinResponseFactory<TResponse>` constraint, the compiler **verifies at compile time** that `TResponse` has a `CreateFail` static method. The JIT compiler generates direct call code based on the concrete type, executing without reflection or virtual dispatch.

### Q4: Why is only `CreateFail` defined and not `CreateSucc` in the factory?
**A**: The cases where Pipelines **create** responses are mostly **failure responses** (Validation failure, exception). Success responses are returned directly by the Handler, so Pipelines don't need to create them. Following the principle of minimal interfaces, only `CreateFail`, which is actually needed, is defined.

## Project Structure

```
03-IFinResponseFactory-CRTP/
├── FinResponseFactoryCrtp/
│   ├── FinResponseFactoryCrtp.csproj
│   ├── IFinResponseFactory.cs
│   ├── FactoryResponse.cs
│   ├── ValidationPipelineExample.cs
│   └── Program.cs
├── FinResponseFactoryCrtp.Tests.Unit/
│   ├── FinResponseFactoryCrtp.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinResponseFactoryCrtpTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project FinResponseFactoryCrtp

# Run tests
dotnet test --project FinResponseFactoryCrtp.Tests.Unit
```

---

We can now create failure responses, but the content of the error is still unknown. The next section designs the `IFinResponseWithError` interface that enables error access only in the Fail case.

→ [Section 3.4: IFinResponseWithError Error Access](../04-IFinResponseWithError/)
