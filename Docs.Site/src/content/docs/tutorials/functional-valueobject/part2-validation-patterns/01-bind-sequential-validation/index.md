---
title: "Bind Sequential Validation"
---

## Overview

In Part 1, we covered how to ensure the validity of a single value object. However, in real applications, it is common to validate multiple fields simultaneously, and dependencies between fields often exist. For example, when validating an address, if the country code is "KR" the postal code must be 5 digits, and if it is "JP" it must be 7 digits. This chapter examines how to chain such dependent validation rules using the Bind pattern.

## Learning Objectives

- Understand the **sequential execution mechanism** where the Bind operator passes the result of the previous validation to the next validation.
- Implement **validation rules with Bind** that have dependencies between country and postal code.
- Leverage the **short-circuit behavior** that immediately stops at the first failure.

## Why Is This Needed?

When implementing value objects with complex domain rules, you quickly encounter three practical problems.

The dependency problem between validation rules surfaces first. In address validation, if the country code is "KR", the postal code must be a 5-digit number, and if it is "JP", it must be a 7-digit number. The result of the previous validation becomes a precondition for the next.

Unnecessary validation cost is also an issue. If the street name is empty, validating the city or postal code is meaningless. When the first condition fails, there is no need to check subsequent conditions.

The importance of validation order cannot be overlooked either. Basic format validation must occur first before complex business rule validation becomes meaningful.

**The Bind sequential validation pattern** resolves all three problems at once through a dependency chain.

## Core Concepts

### Bind Sequential Execution Mechanism

Bind passes the result of the previous validation as input to the next validation. Since each step must succeed before the next step executes, the chain immediately stops at the point of first failure.

The following code compares the approach of running all validations independently versus the Bind chaining approach.

```csharp
// Previous approach (problematic) - runs all validations independently
public static Validation<Error, Address> ValidateOld(string street, string city, string postalCode, string country)
{
    var streetResult = ValidateStreet(street);
    var cityResult = ValidateCity(city);
    var postalCodeResult = ValidatePostalCode(postalCode);
    var countryResult = ValidateCountry(country);
    // Inefficient because all validations run simultaneously
}

// Improved approach (current) - sequential execution via Bind
public static Validation<Error, (string, string, string, string)> Validate(string street, string city, string postalCode, string country) =>
    ValidateStreetFormat(street)
        .Bind(_ => ValidateCityFormat(city))
        .Bind(_ => ValidatePostalCodeFormat(postalCode))
        .Bind(_ => ValidateCountryAndPostalCodeMatch(country, postalCode))
        .Map(_ => (street, city, postalCode, country));
```

With this approach, if the first validation fails, it immediately stops, saving unnecessary validation costs.

### Dependency Validation Pattern

Business rules where a specific condition must be satisfied before proceeding to the next step can be naturally expressed with Bind.

```csharp
// Dependency validation between country and postal code
private static Validation<Error, string> ValidateCountryAndPostalCodeMatch(string country, string postalCode) =>
    (country, postalCode) switch
    {
        ("KR", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
        ("US", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
        ("JP", var code) when code.Length == 7 && code.All(char.IsDigit) => country,
        _ => Domain.CountryPostalCodeMismatch(country, postalCode)
    };
```

Using this pattern, you can express complex business rules clearly and in a type-safe manner.

## Practical Guidelines

### Expected Output
```
=== Dependent Validation Example ===
Executes dependent validation rules for the address value object sequentially.

--- Valid Korean Address ---
Street: 'Gangnam-daero 123'
City: 'Seoul'
Postal Code: '12345'
Country: 'KR'
Success: Address is valid.
   -> Complete address: Gangnam-daero 123, Seoul 12345, KR
   -> All dependent validation rules passed sequentially.

--- Empty Street ---
Street: ''
City: 'Seoul'
Postal Code: '12345'
Country: 'KR'
Failure:
   -> Error code: Domain.Address.StreetTooShort
   -> Current value: ''
```

### Key Implementation Points

There are three points to note during implementation. Construct a sequential execution chain by connecting each validation method with `.Bind()`. Leverage the short-circuit characteristic that immediately stops at the first failure to avoid unnecessary operations. Finally, compose the final result from the original parameters using `.Map()`.

## Project Description

### Project Structure
```
01-Bind-Sequential-Validation/
├── Program.cs              # Main entry point
├── ValueObjects/
│   └── Address.cs          # Address value object (Bind pattern implementation)
├── BindSequentialValidation.csproj
└── README.md               # Main document
```

### Core Code

The Address value object uses Bind to sequentially validate street, city, postal code, and country match.

```csharp
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    // Sequential validation implementation via Bind
    public static Validation<Error, (string Street, string City, string PostalCode, string Country)> Validate(
        string street, string city, string postalCode, string country) =>
        ValidateStreetFormat(street)
            .Bind(_ => ValidateCityFormat(city))
            .Bind(_ => ValidatePostalCodeFormat(postalCode))
            .Bind(_ => ValidateCountryAndPostalCodeMatch(country, postalCode))
            .Map(_ => (street, city, postalCode, country));

    // Dependency validation - business rules between country and postal code
    private static Validation<Error, string> ValidateCountryAndPostalCodeMatch(string country, string postalCode) =>
        (country, postalCode) switch
        {
            ("KR", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
            ("US", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
            ("JP", var code) when code.Length == 7 && code.All(char.IsDigit) => country,
            _ => Domain.CountryPostalCodeMismatch(country, postalCode)
        };
}
```

## Summary at a Glance

The following table compares the difference between the independent execution approach and the Bind sequential validation approach.

| Aspect | Previous approach | Bind Sequential Validation |
|------|-----------|----------------|
| **Execution method** | Runs all validations independently | Chains and executes sequentially |
| **Dependency handling** | Does not consider dependencies between rules | Previous results are passed to the next validation |
| **Performance** | Inefficient as all validations run | Short-circuits at the first failure |
| **business rule** | Simple individual validations | Can express complex dependency rules |

The following table summarizes the pros and cons of Bind sequential validation.

| Pros | Cons |
|------|------|
| Efficient by stopping immediately at the first failure | Validation order must be carefully designed |
| Clearly expresses complex business rules | Cannot run all validations simultaneously |
| Validation order matches business logic | Need to identify the cause when a middle step fails |

## FAQ

### Q1: What is the difference between Bind and Apply?
**A:** Bind provides sequential execution and short-circuiting, while Apply provides parallel execution and collects all errors. Bind is suitable for dependency validations where previous results must be passed to the next validation, while Apply is suitable for running mutually independent validation rules simultaneously.

### Q2: When should the Bind pattern be used?
**A:** Use it when validation rules have dependencies and must be executed in a specific order. A typical case is address validation where the street must be valid for city validation to be meaningful, and the city must be valid for postal code validation to be meaningful.

### Q3: Why is Map used?
**A:** It is used to ignore intermediate results in the Bind chain and compose the final result from the original parameters. Since the final object is created using the original input values rather than transformed values from the validation process, validation and object creation are cleanly separated.

However, Bind reports only one error at a time. How should we inform the user of all problems at once? The next chapter examines the Apply pattern, which runs independent validations in parallel.

---

-> [Chapter 2: Parallel Validation (Apply)](../02-Apply-Parallel-Validation/)
