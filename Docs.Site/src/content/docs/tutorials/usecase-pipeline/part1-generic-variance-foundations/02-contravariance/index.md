---
title: "Contravariance (in)"
---

## Overview

In Section 1, we learned about covariance where values can only be "retrieved" using the `out` keyword. But what about types that only **receive** values?

**Contravariance** is the property where a generic type parameter preserves the inheritance relationship in the **opposite direction**. In C#, contravariance is declared with the `in` keyword.

```
Dog : Animal  (Dog is a subtype of Animal)
    ↓ Contravariance (opposite direction)
IAnimalHandler<Animal> → IAnimalHandler<Dog>  (assignable)
```

## Learning Objectives

After completing this section, you will be able to:

1. Declare a contravariant interface using the `in` keyword
2. Explain why contravariant assignment is possible (input position restriction)
3. Distinguish between the directional differences of covariance and contravariance
4. Understand contravariance in types like `Action<in T>` and `IComparer<in T>`

## Key Concepts

### 1. The `in` Keyword

`in T` declares to the compiler that the type parameter `T` is used only in **input positions**.

```csharp
public interface IAnimalHandler<in T> where T : Animal
{
    void Handle(T animal);         // OK: T is a parameter (input position)
    // T GetResult();              // Compile error! T is a return type (output position)
}
```

### 2. Contravariant Assignment

If `Dog` is a subtype of `Animal`, `IAnimalHandler<Animal>` can be assigned to `IAnimalHandler<Dog>`. The direction is **reversed**.

```csharp
var animalHandler = new AnimalHandler();

// Contravariance: IAnimalHandler<Animal> → IAnimalHandler<Dog> assignable
IAnimalHandler<Dog> dogHandler = animalHandler;
dogHandler.Handle(new Dog("Buddy", "Golden Retriever"));
```

This is possible because `AnimalHandler` can handle all `Animal`s, so it can naturally handle a `Dog` as well.

### 3. Contravariant Types in .NET

Here are representative contravariant types in .NET. All declare input-only usage with the `in` keyword:

| Type | Declaration | Description |
|------|------|------|
| `Action<in T>` | `in T` | Delegate that only receives input |
| `IComparer<in T>` | `in T` | Receives comparison targets as input |
| `IEqualityComparer<in T>` | `in T` | Receives equality comparison targets as input |

```csharp
Action<Animal> animalAction = a => Console.WriteLine(a.Name);
Action<Dog> dogAction = animalAction;  // Action<in T> contravariance
dogAction(new Dog("Buddy", "Golden Retriever"));
```

### 4. Handler Substitution Principle

The practical meaning of contravariance is **handler substitution**. A more general (supertype) handler can substitute for a more specific (subtype) handler.

```
AnimalHandler can handle all Animals
    → Dog is also an Animal, so AnimalHandler can handle Dog
    → IAnimalHandler<Animal> can substitute for IAnimalHandler<Dog>
```

## FAQ

### Q1: Why can a supertype handler substitute for a subtype handler in contravariance?
**A**: `AnimalHandler` can handle all `Animal`s, so it can naturally handle a `Dog`. `in T` is a promise that "T is only received," so a supertype handler that accepts a wider range can safely substitute for a subtype-specific handler.

### Q2: I can't intuitively understand why the directions of covariance and contravariance are opposite.
**A**: **Output (retrieve)** is safe from subtype to supertype. Taking a `Dog` out and storing it in an `Animal` variable is always safe. **Input (receive)** is safe from supertype to subtype. Passing a `Dog` to a handler that handles all `Animal`s is always safe. The directions are opposite because the type safety conditions for input and output are exactly reversed.

### Q3: Is contravariance directly used in the Pipeline design?
**A**: The `in` keyword is not directly used in this tutorial's Pipeline design. However, the concept of contravariance is key to understanding **why interfaces need to be separated into read/write**. This knowledge is applied in Section 4's Interface Segregation Principle (ISP).

## Project Structure

```
02-Contravariance/
├── Contravariance/
│   ├── Contravariance.csproj
│   ├── Animal.cs
│   ├── IAnimalHandler.cs
│   └── Program.cs
├── Contravariance.Tests.Unit/
│   ├── Contravariance.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── ContravarianceTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project Contravariance

# Run tests
dotnet test --project Contravariance.Tests.Unit
```

---

What happens with types that can declare neither `out` nor `in`? The next section examines `List<T>`'s invariance and the constraint limitations of sealed structs.

→ [Section 1.3: Invariance and Constraints](../03-Invariance-And-Constraints/)
