---
title: "Invariance and Constraints"
---

## Overview

Having learned covariance and contravariance, a question arises: can variance be declared on all generic types? The answer is "no."

**Invariance** is the property where a generic type parameter **does not preserve** the inheritance relationship. A generic type declared without the `out` or `in` keyword is invariant.

```
Dog : Animal  (Dog is a subtype of Animal)
    ✗ Invariant (not assignable)
List<Dog> → List<Animal>  (compile error!)
```

## Learning Objectives

After completing this section, you will be able to:

1. Explain why `List<T>` is invariant
2. Understand why a sealed struct cannot be used as a `where` constraint
3. Know how to work around sealed struct limitations using interface constraints
4. Comprehensively compare the differences between covariance, contravariance, and invariance

## Key Concepts

### 1. List\<T\> Is Invariant

`List<T>` uses `T` in both input (Add) and output (indexer) positions, so neither `out` nor `in` can be declared.

```csharp
// Compile error! List<T> is invariant
// List<Animal> animals = new List<Dog>();

// But assignable through IEnumerable<out T> (covariant)
List<Dog> dogs = [new Dog("Buddy")];
IEnumerable<Animal> animals = dogs;  // OK
```

### 2. Constraint Limitations of sealed struct

LanguageExt's `Fin<T>` is a **sealed struct**. In C#, a sealed struct cannot be used as a `where` constraint.

The key thing to note in the following code is that the `where TResponse : Fin<T>` constraint causes a compile error:

```csharp
// This is not possible!
// where TResponse : Fin<T>  // Compile error!

// Fin<T> can only be used as a direct parameter type
public static string ProcessFin(Fin<string> fin) =>
    fin.Match(
        Succ: value => $"Success: {value}",
        Fail: error => $"Fail: {error}");
```

### 3. Working Around with Interface Constraints

The limitations of sealed struct constraints can be worked around with **interfaces**. Interfaces can be used as `where` constraints.

```csharp
public interface IResult
{
    bool IsSucc { get; }
    bool IsFail { get; }
}

// Interface constraints are possible
public static string ProcessResult<T>(T result) where T : IResult
{
    return result.IsSucc ? "Success" : "Fail";
}
```

## FAQ

### Q1: If you assign `List<T>` to `IEnumerable<T>` it becomes covariant -- isn't that sufficient?
**A**: When only reading is needed, `IEnumerable<out T>` is sufficient. However, in Pipelines, **factory method calls** (CreateFail) and **state reading** (IsSucc) through the response type are necessary. This requires designing dedicated interfaces to use as `where` constraints.

### Q2: Is the inability to use sealed struct as a `where` constraint a C# language limitation?
**A**: Yes. In C#, structs cannot be inherited, so constraints of the form `where T : SomeStruct` are not allowed. All structs are subject to this limitation regardless of whether they are sealed. This is the fundamental reason why `Fin<T>` cannot be used directly as a constraint.

### Q3: Is there a performance overhead when working around with interface constraints?
**A**: For records or classes implementing interfaces, there is a virtual method call (virtual dispatch) cost, but it is **negligible compared to reflection**. Additionally, due to JIT compiler optimizations (devirtualization), the actual performance difference is virtually nonexistent.

## Project Structure

```
03-Invariance-And-Constraints/
├── InvarianceAndConstraints/
│   ├── InvarianceAndConstraints.csproj
│   ├── InvarianceExamples.cs
│   └── Program.cs
├── InvarianceAndConstraints.Tests.Unit/
│   ├── InvarianceAndConstraints.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── InvarianceAndConstraintsTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project InvarianceAndConstraints

# Run tests
dotnet test --project InvarianceAndConstraints.Tests.Unit
```

---

By separating interfaces into read/write/factory, appropriate variance can be assigned to each. The next section covers the combination of ISP and variance, plus the CRTP factory pattern.

→ [Section 1.4: Interface Segregation and Variance](../04-Interface-Segregation-And-Variance/)
