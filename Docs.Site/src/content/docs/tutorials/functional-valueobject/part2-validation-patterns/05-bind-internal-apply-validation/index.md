---
title: "Bind with Internal Apply Validation"
---

## Overview

Suppose you are validating a phone number. Only when the overall format is valid can the country code, area code, and local number be validated individually. If the format is wrong like "+86abc123def", parsing the components is meaningless. A reverse nested pattern is needed that first validates the precondition (format) with Bind, and upon passing, validates the components in parallel with Apply.

## Learning Objectives

- Implement the **reverse nested validation pattern** that uses Apply inside Bind to validate components in parallel after precondition validation.
- Design **precondition-based validation** where subsequent validations are meaningful only when specific conditions are met.
- Apply **component parallel validation** that validates each component of composite data simultaneously for performance optimization.

## Why Is This Needed?

In the previous step, we covered the outer Apply, inner Bind structure. However, when subsequent validations are meaningful only after a precondition is satisfied, the reverse approach is needed.

Phone number validation is a typical example. If the overall format is not valid, validating country code, area code, and local number individually is meaningless. On the other hand, if the format is valid, the three components are independent of each other and can be validated simultaneously. In this pattern, if the Bind stage fails, a single error occurs, and if the Apply stage fails, errors from multiple components are collected simultaneously.

**The Bind with internal Apply nested validation pattern** implements efficient precondition-based validation.

## Core Concepts

### Precondition-Based Validation

The precondition is validated first with Bind, and only when it passes are the components validated in parallel with Apply. If the precondition fails, subsequent validation itself is not executed.

The following code compares validation without preconditions versus precondition-based validation.

```csharp
// Previous approach (problematic) - runs all validations simultaneously
public static Validation<Error, PhoneNumber> ValidateOld(string phoneNumber)
{
    // Inefficient because all validations run without preconditions
    var countryResult = ValidateCountryCode(phoneNumber);
    var areaResult = ValidateAreaCode(phoneNumber);
    var localResult = ValidateLocalNumber(phoneNumber);
    // Unnecessary operations even when the format is not valid
}

// Improved approach (current) - precondition-based validation
public static Validation<Error, (string CountryCode, string AreaCode, string LocalNumber)> Validate(string phoneNumber) =>
    // Stage 1: Precondition validation (Bind) - validate phone number format first
    ValidatePhoneNumberFormat(phoneNumber)
        // Stage 2: Component parallel validation (Apply) - if format is valid, validate components in parallel
        .Bind(validFormat =>
            (ValidateCountryCode(validFormat), ValidateAreaCode(validFormat), ValidateLocalNumber(validFormat))
                .Apply((countryCode, areaCode, localNumber) => (countryCode, areaCode, localNumber))
                .As());
```

When the precondition is not satisfied, unnecessary subsequent validations are completely skipped.

### Component Parallel Validation

When the precondition passes, each component is independent and validated simultaneously with Apply. Country code, area code, and local number validations all run independently, and errors from failed components are all collected.

```csharp
// Precondition validation - must be executed first
private static Validation<Error, string> ValidatePhoneNumberFormat(string phoneNumber) =>
    !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10
        ? phoneNumber
        : Domain.PhoneNumberTooShort(phoneNumber);

// Component parallel validation - executed in parallel inside Apply
private static Validation<Error, string> ValidateCountryCode(string phoneNumber) =>
    phoneNumber.StartsWith("+82") || phoneNumber.StartsWith("+1")
        ? phoneNumber.Substring(0, 3)
        : Domain.CountryCodeUnsupported(phoneNumber);

private static Validation<Error, string> ValidateAreaCode(string phoneNumber) =>
    phoneNumber.Length >= 6 && phoneNumber.Substring(3, 3).All(char.IsDigit)
        ? phoneNumber.Substring(3, 3)
        : Domain.AreaCodeInvalid(phoneNumber);

private static Validation<Error, string> ValidateLocalNumber(string phoneNumber) =>
    phoneNumber.Length >= 10 && phoneNumber.Substring(6).All(char.IsDigit)
        ? phoneNumber.Substring(6)
        : Domain.LocalNumberInvalid(phoneNumber);
```

## Practical Guidelines

### Expected Output
```
=== Bind with Internal Apply Nested Validation Example ===
An example of nested validation using Apply inside Bind for the phone number value object.

--- Valid Korean Phone Number ---
Phone number: '+821012345678'
Success: Phone number is valid.
   -> Country code: +82
   -> Area code: 101
   -> Local number: 2345678
   -> All nested validation rules passed.

--- Phone Number Format Error ---
Phone number: '123'
Failure:
   -> Error code: Domain.PhoneNumber.PhoneNumberTooShort
   -> Current value: '123'

--- Multiple Errors (Failure at Apply Stage) ---
Phone number: '+86abc123def'
Failure:
   -> Total 3 validation failures:
     1. Error code: Domain.PhoneNumber.CountryCodeUnsupported
        Current value: '+86abc123def'
     2. Error code: Domain.PhoneNumber.AreaCodeInvalid
        Current value: '+86abc123def'
     3. Error code: Domain.PhoneNumber.LocalNumberInvalid
        Current value: '+86abc123def'
```

### Key Implementation Points

Three points to note during implementation. Validate the phone number format first with Bind to confirm the precondition, validate country code, area code, and local number simultaneously with Apply, and distinguish errors so the Bind stage returns a single error while the Apply stage returns ManyErrors.

## Project Description

### Project Structure
```
05-Bind-Internal-Apply-Validation/
├── Program.cs              # Main entry point
├── ValueObjects/
│   └── PhoneNumber.cs      # Phone number value object (reverse nested validation pattern implementation)
├── BindInternalApplyValidation.csproj
└── README.md               # Main document
```

### Core Code

The PhoneNumber value object validates the format first with Bind, then validates country code, area code, and local number in parallel with Apply.

```csharp
public sealed class PhoneNumber : ValueObject
{
    public string CountryCode { get; }
    public string AreaCode { get; }
    public string LocalNumber { get; }

    // Reverse nested validation pattern implementation (Apply inside Bind)
    public static Validation<Error, (string CountryCode, string AreaCode, string LocalNumber)> Validate(string phoneNumber) =>
        // Stage 1: Precondition validation (Bind) - validate phone number format first
        ValidatePhoneNumberFormat(phoneNumber)
            // Stage 2: Component parallel validation (Apply) - if format is valid, validate components in parallel
            .Bind(validFormat =>
                (ValidateCountryCode(validFormat), ValidateAreaCode(validFormat), ValidateLocalNumber(validFormat))
                    .Apply((countryCode, areaCode, localNumber) => (countryCode, areaCode, localNumber))
                    .As());

    // Precondition validation - must be executed first
    private static Validation<Error, string> ValidatePhoneNumberFormat(string phoneNumber) =>
        !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10
            ? phoneNumber
            : Domain.PhoneNumberTooShort(phoneNumber);

    // Component parallel validation - executed in parallel inside Apply
    private static Validation<Error, string> ValidateCountryCode(string phoneNumber) =>
        phoneNumber.StartsWith("+82") || phoneNumber.StartsWith("+1")
            ? phoneNumber.Substring(0, 3)
            : Domain.CountryCodeUnsupported(phoneNumber);

    private static Validation<Error, string> ValidateAreaCode(string phoneNumber) =>
        phoneNumber.Length >= 6 && phoneNumber.Substring(3, 3).All(char.IsDigit)
            ? phoneNumber.Substring(3, 3)
            : Domain.AreaCodeInvalid(phoneNumber);

    private static Validation<Error, string> ValidateLocalNumber(string phoneNumber) =>
        phoneNumber.Length >= 10 && phoneNumber.Substring(6).All(char.IsDigit)
            ? phoneNumber.Substring(6)
            : Domain.LocalNumberInvalid(phoneNumber);
}
```

## Summary at a Glance

The following table compares the differences between the two nested validation patterns.

| Aspect | Apply with Internal Bind Nested Validation | Bind with Internal Apply Nested Validation |
|------|-------------------------|-------------------------|
| **Outer structure** | Validates fields in parallel | Validates preconditions sequentially |
| **Inner structure** | Sequential validation inside each field | Validates components in parallel |
| **Applicable scenario** | Complex per-field validation | Precondition-based component validation |
| **Performance** | Per-field parallel processing | Component parallel processing after precondition |

The following table summarizes the pros and cons of this pattern.

| Pros | Cons |
|------|------|
| Skips unnecessary validation when precondition not met | Validation structure becomes complex |
| Parallel validation of components after precondition | Debugging nested validation logic is difficult |
| Logical validation flow based on preconditions | Preconditions must be carefully designed |

## FAQ

### Q1: When should the reverse nested validation pattern be used?
**A:** Use it when subsequent validations are meaningful only after a precondition is satisfied. A typical case is phone number validation where the overall format must be valid before country code, area code, and local number can be individually validated.

### Q2: What is the difference between precondition and component validation?
**A:** Precondition validation verifies the overall structure or format, while component validation examines each part in detail. When the precondition is not satisfied, there is no need to execute component validation.

### Q3: How do you determine the order of Bind and Apply?
**A:** Execute the precondition validation first with Bind, and if the result is successful, validate the components in parallel with Apply. Performing the precondition first and then running component validation in parallel based on its result avoids unnecessary operations.

In Part 2, we have learned all the combination patterns of Bind and Apply. In Part 3, we assemble these concepts into framework base classes from the Functorium framework to complete value object patterns ready for practical use.

---

-> [Chapter 6: Contextual Validation](../06-Contextual-Validation/)
