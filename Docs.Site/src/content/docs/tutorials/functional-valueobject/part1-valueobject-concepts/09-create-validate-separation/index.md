---
title: "Create/Validate Separation"
---

## Overview

What if you only want to check whether user input is valid, but the existing `Create` method performs validation and object creation simultaneously? In situations where only validation is needed, you still have to pay the unnecessary cost of object creation. In this chapter, we explicitly separate the responsibilities of Create and Validate, apply the single responsibility principle, and secure reusability and testability of validation logic.

## Learning Objectives

1. You can implement Create and Validate methods separately so that each handles only one responsibility
2. You can call the Validate method independently to perform validation without creating an object
3. You can implement safe value creation through a functional pipeline using LanguageExt's Validation and Fin

## Why Is This Needed?

In the previous step `ValueComparability`, we implemented value object comparability to provide equality and sorting capabilities. However, when validation and creation are bundled in a single method, problems arise in complex business logic.

When validation logic is included in the Create method, unnecessary object creation costs are incurred even in situations that only require validation (e.g., form validity checks, API request validation, batch data filtering). Additionally, when validation logic and creation logic are intermingled, it becomes difficult to test each independently.

By separating Create and Validate, you can secure reusability of validation logic, adherence to the single responsibility principle, and testability all at once.

## Core Concepts

### Validation Responsibility Separation

Validation logic is extracted into an independent method to make it reusable. The Validate method returns only the validation result, and the Create method delegates validation to Validate and then creates the object through functional composition.

The following code shows the difference between the previous approach and the improved approach.

```csharp
// Previous approach (problematic) - creates object even when only validation is needed
public static Fin<Denominator> Create(int value)
{
    if (value == 0)  // Validation logic is tied to Create
        return Error.New("Zero is not allowed");

    return new Denominator(value);  // Unnecessary object creation
}

// Improved approach (current) - validation responsibility separation
public static Validation<Error, int> Validate(int value) =>
    value == 0
        ? Error.New("Zero is not allowed")
        : value;

public static Fin<Denominator> Create(int value) =>
    Validate(value)  // Delegate validation responsibility to Validate
        .Map(validNumber => new Denominator(validNumber))
        .ToFin();
```

### Functional Composition for Safe Creation

Using LanguageExt's Validation and Fin, you can implement type-safe error handling instead of exceptions when validation fails. This is a pipeline that transforms the validation result with `Map` and converts the final type with `ToFin()`.

```csharp
// Safe creation through functional composition
public static Fin<Denominator> Create(int value) =>
    Validate(value)                    // Step 1: Validation
        .Map(validNumber => new Denominator(validNumber))  // Step 2: Transformation
        .ToFin();                      // Step 3: Type conversion
```

Each step is clearly separated making debugging easy, and errors are automatically propagated on failure.

## Practical Guidelines

### Expected Output
```
=== Create and Validate Separation Through Single Responsibility Principle ===

=== 1. Key Improvement: Create and Validate Responsibility Separation ===
Validation responsibility separation: Calling only the Validate method
  Validation succeeded: 5
Creation responsibility separation: Calling the Create method
  Creation succeeded: 5

=== 2. Independent Usage of the Validate Method ===
Using only the validation responsibility:
  1 -> Validation passed: 1
  5 -> Validation passed: 5
  10 -> Validation passed: 10
  0 -> Validation failed: Zero is not allowed
  -3 -> Validation passed: -3
```

### Key Implementation Points
1. **Validate method implementation**: Implemented as a pure function handling only validation logic to secure reusability
2. **Create method refactoring**: Delegates validation responsibility by calling the Validate method, and creates objects safely through functional composition
3. **Independent validation usage**: Optimizes performance by calling only the Validate method in situations where only validation is needed

## Project Description

### Project Structure
```
CreateValidateSeparation/                 # Main project
├── Program.cs                           # Main entry file
├── MathOperations.cs                    # Math operations class
├── ValueObjects/
│   └── Denominator.cs                   # Denominator value object (Create/Validate separation)
├── CreateValidateSeparation.csproj      # Project file
└── README.md                           # Main documentation
```

### Core Code

#### Denominator.cs - Validation and Creation Responsibility Separation
```csharp
/// <summary>
/// Validation responsibility - single responsibility principle
/// Separate method handling only validation logic
/// </summary>
public static Validation<Error, int> Validate(int value) =>
    value == 0
        ? Error.New("Zero is not allowed")
        : value;

/// <summary>
/// Factory method for creating Denominator instances
/// Separates validation responsibility to adhere to single responsibility principle
/// </summary>
public static Fin<Denominator> Create(int value) =>
    Validate(value)
        .Map(validNumber => new Denominator(validNumber))
        .ToFin();
```

#### Program.cs - Usage Examples
```csharp
// Validation responsibility separation: Calling only the Validate method
var validationResult = Denominator.Validate(5);
validationResult.Match(
    Succ: value => Console.WriteLine($"  Validation succeeded: {value}"),
    Fail: error => Console.WriteLine($"  Validation failed: {error}")
);

// Creation responsibility separation: Calling the Create method
var creationResult = Denominator.Create(5);
creationResult.Match(
    Succ: denominator => Console.WriteLine($"  Creation succeeded: {denominator}"),
    Fail: error => Console.WriteLine($"  Creation failed: {error}")
);
```

## Summary at a Glance

The following table compares the differences between the previous approach and the current approach.

| Aspect | Previous Approach | Current Approach |
|------|-----------|-----------|
| **Validation logic** | Embedded inside the Create method | Separated into the Validate method |
| **Reusability** | Object creation required even when only validation is needed | Can call only the Validate method |
| **Responsibility separation** | Create handles both validation and creation | Each method handles only one responsibility |
| **Testability** | Unnecessary object creation when testing validation logic | Validation logic can be tested independently |
| **Performance** | Object creation cost even when only validation is needed | Cost optimized when only validation is needed |

## FAQ

### Q1: Are there actual cases where only the Validate method is used?
**A**: Yes, situations where only validity is checked without creating an object are common, such as request data validation in web APIs, form validity checks, and data filtering in batch processing.

```csharp
// When only user input validation is needed in a web API
[HttpPost]
public IActionResult Divide([FromBody] DivisionRequest request)
{
    // Perform only validation without object creation for performance optimization
    var denominatorValidation = Denominator.Validate(request.Denominator);
    if (denominatorValidation.IsFail)
    {
        return BadRequest("Denominator cannot be zero");
    }

    // Perform actual operation only after validation passes
    var result = request.Numerator / request.Denominator;
    return Ok(result);
}
```

### Q2: What is the difference between Validation<Error, int> and `Fin<Denominator>`?
**A**: `Validation<Error, int>` represents a validation result and returns the original value (int) on success. `Fin<Denominator>` represents an object creation result and returns the created object (Denominator) on success. Validate operates at the validation level, while Create operates at the creation level.

```csharp
// Validate: Performs only validation, returns original value (validation level)
var validation = Denominator.Validate(5);
validation.Match(
    Succ: value => Console.WriteLine($"Validated value: {value}"), // int type
    Fail: error => Console.WriteLine($"Validation failed: {error}")
);

// Create: Creates object after validation, returns created object (creation level)
var creation = Denominator.Create(5);
creation.Match(
    Succ: denominator => Console.WriteLine($"Created object: {denominator}"), // Denominator type
    Fail: error => Console.WriteLine($"Creation failed: {error}")
);
```

### Q3: What do the Map and ToFin() methods do?
**A**: `Map` transforms the value of the success case to another type (int -> Denominator). The failure case is propagated as-is. `ToFin()` converts a Validation type to a Fin type. Both types represent success/failure but provide different APIs, so they are converted as appropriate for the situation.

```csharp
public static Fin<Denominator> Create(int value) =>
    Validate(value)                                         // Validation<Error, int>
        .Map(validNumber => new Denominator(validNumber))   // Validation<Error, Denominator>
        .ToFin();                                           // Fin<Denominator>

// Result at each step (success case):
// Step 1: Validate(5) -> Success(5)
// Step 2: Map(...) -> Success(new Denominator(5))
// Step 3: ToFin() -> Succ(new Denominator(5))

// Result at each step (failure case):
// Step 1: Validate(0) -> Failure(Error("Zero is not allowed"))
// Step 2: Map(...) -> Failure(Error("Zero is not allowed")) (auto-propagated)
// Step 3: ToFin() -> Fail(Error("Zero is not allowed")) (auto-propagated)
```

By separating Create and Validate, we secured validation reusability for a single value object. In the next chapter, we cover the CreateFromValidated pattern for creating composite objects directly from already-validated values.

---

→ [Chapter 10: Validated Value Creation](../10-Validated-Value-Creation/)
