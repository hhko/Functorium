---
title: "Interface Segregation and Variance"
---

## Overview

In Section 3, we confirmed that sealed struct limitations can be worked around with interfaces. However, if a single interface includes both reading and writing, variance cannot be declared. The solution is the **Interface Segregation Principle (ISP).**

Combining ISP with variance allows assigning appropriate variance to each interface. Read interfaces get `out` (covariant), write interfaces get `in` (contravariant), and factory interfaces get CRTP (Curiously Recurring Template Pattern).

```
IReadable<out T>     → Covariant (read-only)
IWritable<in T>      → Contravariant (write-only)
IFactory<TSelf>      → CRTP (static abstract factory)
```

## Learning Objectives

After completing this section, you will be able to:

1. Apply ISP to separate interfaces into read/write/factory
2. Assign appropriate variance (out/in) to separated interfaces
3. Implement a `static abstract` factory using the CRTP pattern
4. Write generic factory methods using `where T : IFactory<T>` constraints
5. Understand how this pattern connects to the IFinResponse hierarchy design

## Key Concepts

### 1. Read Interface - Covariant (out)

A read-only interface declares covariance with the `out` keyword.

```csharp
public interface IReadable<out T>
{
    T Value { get; }
    bool IsValid { get; }
}
```

### 2. Write Interface - Contravariant (in)

A write-only interface declares contravariance with the `in` keyword.

```csharp
public interface IWritable<in T>
{
    void Write(T value);
}
```

### 3. Factory Interface - CRTP

A CRTP factory pattern leveraging C# 11's `static abstract` members. It passes the implementing type itself as the type parameter to precisely specify the factory method's return type.

```csharp
public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
{
    static abstract TSelf Create(string value);
    static abstract TSelf CreateEmpty();
}
```

### 4. Interface Composition

Multiple interfaces can be composed to constrain only the needed capabilities.

```csharp
// Read+Write = Invariant (implements both interfaces)
public interface IReadWrite<T> : IReadable<T>, IWritable<T>;
```

### 5. Why This Pattern Matters

This pattern is the foundation for the **IFinResponse hierarchy** designed in later sections. Check how each interface maps:

| This Section's Interface | IFinResponse Hierarchy | Role |
|-------------------|-------------------|------|
| `IReadable<out T>` | `IFinResponse<out A>` | Covariant read access |
| `IFactory<TSelf>` | `IFinResponseFactory<TSelf>` | CRTP factory (CreateFail) |

## FAQ

### Q1: Why separate interfaces into read/write/factory instead of combining them into one?
**A**: If a single interface includes both reading and writing, neither `out` (covariant) nor `in` (contravariant) can be declared, making it **invariant**. Separation allows assigning appropriate variance to each interface, and Pipelines can use only the interfaces they need as constraints, adhering to the **principle of least privilege**.

### Q2: What does the `where TSelf : IFactory<TSelf>` constraint in the CRTP pattern guarantee?
**A**: This constraint guarantees that `TSelf` must be a type that implements `IFactory<TSelf>`. This ensures that the `static abstract` factory method's return type is **the exact type of the implementor**, allowing correct-type instances to be created without runtime casting.

### Q3: How does the pattern learned in this section correspond to the IFinResponse hierarchy?
**A**: `IReadable<out T>` corresponds to `IFinResponse<out A>`, and `IFactory<TSelf>` corresponds to `IFinResponseFactory<TSelf>`. Covariance is applied to the read interface for flexible type assignment, and CRTP is applied to the factory interface for reflection-free response creation.

### Q4: What happens to variance when composing separated interfaces like `IReadWrite<T>`?
**A**: When `IReadWrite<T>` inherits both `IReadable<T>` and `IWritable<T>`, T is used in both input and output positions, making it **invariant**. The composition interface loses variance, but in Pipelines, only individual capability interfaces are constrained, so variance is maintained.

## Project Structure

```
04-Interface-Segregation-And-Variance/
├── InterfaceSegregationAndVariance/
│   ├── InterfaceSegregationAndVariance.csproj
│   ├── Interfaces.cs
│   └── Program.cs
├── InterfaceSegregationAndVariance.Tests.Unit/
│   ├── InterfaceSegregationAndVariance.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── InterfaceSegregationTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project InterfaceSegregationAndVariance

# Run tests
dotnet test --project InterfaceSegregationAndVariance.Tests.Unit
```

---

Applying the variance fundamentals established in Part 1 to Mediator Pipelines. The next section examines the structure of `IPipelineBehavior` and how `where` constraints determine Pipeline scope.

→ [Section 2.1: Mediator Pipeline Behavior Structure](../../Part2-Problem-Definition/01-Mediator-Pipeline-Structure/)
