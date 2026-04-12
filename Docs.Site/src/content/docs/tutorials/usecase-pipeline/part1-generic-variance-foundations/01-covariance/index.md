---
title: "Covariance (out)"
---

## Overview

`Dog` is a subtype of `Animal`. So can `IAnimalShelter<Dog>` be assigned to `IAnimalShelter<Animal>`? The answer depends on the interface's **variance declaration**.

**Covariance** is the property where a generic type parameter preserves the inheritance relationship in the **same direction**. In C#, covariance is declared with the `out` keyword.

```
Dog : Animal  (Dog is a subtype of Animal)
    ↓ Covariance (same direction)
IAnimalShelter<Dog> → IAnimalShelter<Animal>  (assignable)
```

## Learning Objectives

After completing this section, you will be able to:

1. Declare a covariant interface using the `out` keyword
2. Explain why covariant assignment is possible (output position restriction)
3. Understand why `IEnumerable<out T>` is covariant
4. Explain how covariance is connected to read-only access

## Key Concepts

### 1. The `out` Keyword

`out T` declares to the compiler that the type parameter `T` is used only in **output positions**.

```csharp
public interface IAnimalShelter<out T> where T : Animal
{
    T GetAnimal(int index);       // OK: T is a return type (output position)
    IEnumerable<T> GetAll();      // OK: T is a return type (output position)
    // void Add(T animal);        // Compile error! T is a parameter (input position)
}
```

### 2. Covariant Assignment

If `Dog` is a subtype of `Animal`, `IAnimalShelter<Dog>` can be assigned to `IAnimalShelter<Animal>`.

```csharp
var dogShelter = new DogShelter();
dogShelter.Add(new Dog("Buddy", "Golden Retriever"));

// Covariance: IAnimalShelter<Dog> → IAnimalShelter<Animal> assignable
IAnimalShelter<Animal> animalShelter = dogShelter;
```

### 3. Covariant Interfaces in .NET

The most representative covariant interface in .NET is `IEnumerable<out T>`.

```csharp
IEnumerable<Dog> dogs = new List<Dog> { new("Buddy", "Golden Retriever") };
IEnumerable<Animal> animals = dogs;  // OK because IEnumerable<out T>
```

### 4. Why Is Covariance Useful?

Covariance guarantees **read-only** access. When declared with `out T`, values of type T can only be **retrieved**, so a collection of subtypes can be safely referenced as a supertype.

## FAQ

### Q1: Why can't `T` be used in input positions when the `out` keyword is applied?
**A**: `out T` is a promise that "this type parameter only produces values." If an input position like `void Add(T animal)` were allowed, a type safety violation could occur where a `Cat` is added to a `DogShelter` through an `IAnimalShelter<Animal>` reference. The compiler prevents this at the source.

### Q2: `IEnumerable<out T>` is covariant, but why isn't `List<T>` covariant?
**A**: Because `List<T>` uses T in input positions through methods like `Add(T item)`. When T is used in both input and output positions, neither `out` nor `in` can be declared, making it an **invariant** type. `IEnumerable<T>` is read-only, so `out` can be declared.

### Q3: How is covariance used in the Pipeline design of this tutorial?
**A**: The `IFinResponse<out A>` interface designed in Part 3 leverages covariance. Thanks to `out A`, `IFinResponse<string>` can be assigned to `IFinResponse<object>`, allowing Pipelines to handle various response types flexibly.

## Project Structure

```
01-Covariance/
├── Covariance/
│   ├── Covariance.csproj
│   ├── Animal.cs
│   ├── IAnimalShelter.cs
│   └── Program.cs
├── Covariance.Tests.Unit/
│   ├── Covariance.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── CovarianceTests.cs
└── README.md
```

## How to Run

```bash
# Run the program
dotnet run --project Covariance

# Run tests
dotnet test --project Covariance.Tests.Unit
```

---

If covariance is "retrieve-only," what variance applies in the opposite case of "receive-only"? The next section covers the `in` keyword and contravariance.

→ [Section 1.2: Contravariance (in)](../02-Contravariance/)
