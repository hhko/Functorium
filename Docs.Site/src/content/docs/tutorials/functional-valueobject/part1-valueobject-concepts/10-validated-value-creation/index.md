---
title: "Validated Value Object Creation"
---

## Overview

When creating an Address, if Street, City, and PostalCode are already validated, is there any need to validate them again? In this chapter, we introduce the **CreateFromValidated method** to prevent unnecessary re-validation of already-validated values, and implement how the three methods -- Create, Validate, and CreateFromValidated -- each fulfill their respective roles in composite value objects.

## Learning Objectives

1. You can implement the CreateFromValidated method to directly create objects from already-validated values
2. You can combine validation results from multiple value objects using LanguageExt's tuple Apply pattern
3. You can choose among the three methods -- Create, Validate, and CreateFromValidated -- as appropriate for the situation to simultaneously secure performance and safety

## Why Is This Needed?

In the previous step `CreateValidateSeparation`, we separated the responsibilities of Create and Validate. However, new problems emerge when dealing with composite value objects.

When creating a composite object, if each component is already validated, it is an unnecessary operation for the Create method to perform validation again. Additionally, since validation results from multiple value objects need to be combined, safe composition that creates the composite object only when all validations succeed is required.

To address this, we introduce the three-method pattern of Create, Validate, and CreateFromValidated. By selecting the optimal creation method for the situation, you can simultaneously secure performance and safety.

## Core Concepts

### The CreateFromValidated Method (Validated Value Creation)

This creates objects directly from already-validated values. Since a value that has been validated once is used directly without re-validation, redundant validation costs are eliminated.

The following code shows the redundant validation problem of the previous approach and the improved approach.

```csharp
// Previous approach (problematic) - redundant validation occurs
public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue)
{
    // Re-validates each field (unnecessary duplication)
    var streetResult = Street.Create(streetValue);  // Validation performed
    var cityResult = City.Create(cityValue);        // Validation performed
    var postalCodeResult = PostalCode.Create(postalCodeValue); // Validation performed

    // Combines all results to create Address
    return CombineResults(streetResult, cityResult, postalCodeResult);
}

// Improved approach (current) - using CreateFromValidated
public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
    Validate(streetValue, cityValue, postalCodeValue)  // Validate only once
        .Map(validatedValues => new Address(
            validatedValues.Street,    // Already validated value
            validatedValues.City,      // Already validated value
            validatedValues.PostalCode // Already validated value
        ))
        .ToFin();

// CreateFromValidated: Direct creation without validation
public static Address CreateFromValidated(Street street, City city, PostalCode postalCode) =>
    new Address(street, city, postalCode);  // Create immediately without validation
```

### Composite Validation Composition

Using LanguageExt's tuple Apply pattern, you can perform validation on each component independently and create the composite object only when all validations succeed. If any one fails, all errors are collected and returned.

```csharp
// Composite validation composition - tuple Apply pattern
public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
    string streetValue, string cityValue, string postalCodeValue) =>
    (Street.Validate(streetValue), City.Validate(cityValue), PostalCode.Validate(postalCodeValue))
        .Apply((street, city, postalCode) =>
            (Street: Street.CreateFromValidated(street),       // Create directly from validated value
             City: City.CreateFromValidated(city),             // Create directly from validated value
             PostalCode: PostalCode.CreateFromValidated(postalCode))); // Create directly from validated value
```

### Three-Method Pattern

Select the most appropriate creation method based on the state of the input data.

- **Create**: Validate then create from raw data (general usage)
- **Validate**: Perform only validation without creating an object (when only validation is needed)
- **CreateFromValidated**: Create directly from already-validated values (performance optimization)

```csharp
// Usage examples of the three-method pattern
// 1. Create: Create from raw data
var address1 = Address.Create("123 Main St", "Seoul", "12345");

// 2. Validate: Perform only validation
var validation = Address.Validate("123 Main St", "Seoul", "12345");
if (validation.IsSucc) { /* Handle validation success */ }

// 3. CreateFromValidated: Create directly from already-validated values
var street = Street.CreateFromValidated("123 Main St");
var city = City.CreateFromValidated("Seoul");
var postalCode = PostalCode.CreateFromValidated("12345");
var address2 = Address.CreateFromValidated(street, city, postalCode);
```

## Practical Guidelines

### Expected Output
```
=== Three-Method Pattern for Composite Value Objects ===

1. Create: Validate then create

  Success case:
    Success: 123 Main St, Seoul 12345

  Failure cases:
    Failure: Street name cannot be empty
    Success: 123 Main St, Seoul 123

2. Validate: Validation method

  Validation success case:
    Validation succeeded: 123 Main St, Seoul 12345

  Validation failure case:
    Validation failed: Street name cannot be empty

3. CreateFromValidated: Create directly from already-validated value objects
  Created address: 123 Main St, Seoul 12345
```

### Key Implementation Points
1. **CreateFromValidated method implementation**: Designed with internal accessor so it cannot be used externally but can be utilized internally
2. **Composite validation composition**: Safe composite validation through functional composition using LanguageExt's tuple Apply pattern
3. **Three-method pattern**: Providing the optimal creation method for each situation to secure performance and flexibility

## Project Description

### Project Structure
```
ValidatedValueCreation/                    # Main project
├── Program.cs                            # Main entry file
├── ValueObjects/
│   ├── Address.cs                        # Composite address value object (three-method pattern)
│   ├── Street.cs                         # Street name value object
│   ├── City.cs                           # City name value object
│   └── PostalCode.cs                     # Postal code value object
├── ValidatedValueCreation.csproj         # Project file
└── README.md                            # Main documentation
```

### Core Code

#### Address.cs - Three-Method Pattern Implementation
```csharp
/// <summary>
/// Factory method for creating Address instances
/// Separates validation responsibility to adhere to single responsibility principle
/// </summary>
public static Fin<Address> Create(string streetValue, string cityValue, string postalCodeValue) =>
    Validate(streetValue, cityValue, postalCodeValue)
        .Map(validatedValues => new Address(
            validatedValues.Street,
            validatedValues.City,
            validatedValues.PostalCode))
        .ToFin();

/// <summary>
/// Internal method for creating Address instances from already-validated value objects
/// Used only externally (by parent); not used within its own Create
/// </summary>
public static Address CreateFromValidated(Street street, City city, PostalCode postalCode) =>
    new Address(street, city, postalCode);

/// <summary>
/// Validation responsibility - single responsibility principle
/// Combines validation of each component to perform composite validation
/// </summary>
public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(
    string streetValue, string cityValue, string postalCodeValue) =>
    (Street.Validate(streetValue), City.Validate(cityValue), PostalCode.Validate(postalCodeValue))
        .Apply((street, city, postalCode) =>
            (Street: Street.CreateFromValidated(street),
             City: City.CreateFromValidated(city),
             PostalCode: PostalCode.CreateFromValidated(postalCode)));
```

#### Program.cs - Three-Method Pattern Usage Examples
```csharp
// 1. Create: Validate then create
var successResult = Address.Create("123 Main St", "Seoul", "12345");
successResult.Match(
    Succ: address => Console.WriteLine($"    Success: {address}"),
    Fail: error => Console.WriteLine($"    Failure: {error}")
);

// 2. Validate: Perform only validation
var successValidation = Address.Validate("123 Main St", "Seoul", "12345");
successValidation.Match(
    Succ: validatedValues => Console.WriteLine($"    Validation succeeded: {validatedValues.Street}, {validatedValues.City} {validatedValues.PostalCode}"),
    Fail: error => Console.WriteLine($"    Validation failed: {error}")
);

// 3. CreateFromValidated: Create directly from already-validated values
var street = Street.CreateFromValidated("123 Main St");
var city = City.CreateFromValidated("Seoul");
var postalCode = PostalCode.CreateFromValidated("12345");
var address = Address.CreateFromValidated(street, city, postalCode);
```

## Summary at a Glance

The following table compares the differences between the previous approach (Create + Validate) and the current approach (three-method pattern).

| Aspect | Previous Approach | Current Approach |
|------|-----------|-----------|
| **Number of methods** | 2: Create, Validate | 3: Create, Validate, CreateFromValidated |
| **Redundant validation** | Re-validates each component when creating composite objects | Prevents redundant validation with CreateFromValidated |
| **Performance** | Validation performed in all cases | Optimized creation for each situation |
| **Composite validation** | Simple sequential validation | Safe composition through tuple Apply pattern |
| **Flexibility** | Limited creation methods | 3 optimized creation methods for each situation |

## FAQ

### Q1: When should the CreateFromValidated method be used?
**A**: Only when it is certain that the value has already been validated. Typical cases include values retrieved from a database (validated at storage time), creating child objects within a composite value object's Validate, and creating objects after validation is complete in business logic.

### Q2: Why does the Validate method use CreateFromValidated?
**A**: For performance optimization and type matching. When Validate succeeds, the return type must be a value object, but calling Create returns `Fin<T>`, requiring conversion back to `T`. CreateFromValidated takes an already-validated raw value and returns a value object directly, avoiding both unnecessary re-validation and type conversion.

### Q3: Why is CreateFromValidated internal?
**A**: To prevent external code from accidentally creating objects with unvalidated values. By restricting it with the `internal` accessor so it can only be used within the same assembly, domain integrity is guaranteed while still allowing performance optimization internally.

```csharp
// Correct usage: Used only within the Address internal assembly
public static Validation<Error, (Street Street, City City, PostalCode PostalCode)> Validate(...) =>
    (Street.Validate(streetValue), City.Validate(cityValue), PostalCode.Validate(postalCodeValue))
        .Apply((street, city, postalCode) =>
            (Street: Street.CreateFromValidated(street),              // Internal assembly only
             City: City.CreateFromValidated(city),                    // Internal assembly only
             PostalCode: PostalCode.CreateFromValidated(postalCode))); // Internal assembly only

// Incorrect usage: Direct usage from external assembly (compile error)
// var street = Street.CreateFromValidated("123 Main St"); // Inaccessible
```

With the three-method pattern, we secured both performance and safety for composite value objects. In the next chapter, we cover how to abstract these patterns into reusable framework types.

---

→ [Chapter 11: Framework Types](../11-ValueObject-Framework/)
