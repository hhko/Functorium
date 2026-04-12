---
title: "Apply with Internal Bind Validation"
---

## Overview

You want to validate username, email, and password simultaneously on a registration form. But username requires format validation followed by availability validation, email requires format validation followed by domain validation, and password requires strength validation followed by history validation. The fields are independent of each other, but each field internally requires sequential validation. This problem is resolved by a nested pattern that uses Apply for parallel processing on the outside and Bind for sequential processing inside each field.

## Learning Objectives

- Implement the **nested validation pattern** that uses Bind inside Apply to handle complex validation logic step by step.
- Apply **fine-grained per-field control** where each field implements its own complex validation logic independently.
- Achieve a **balance between performance and accuracy** by combining the advantages of parallel and sequential validation.

## Why Is This Needed?

In the previous step, we split independent and dependent information into 2 stages for validation. However, when each field requires complex multi-stage validation, a different approach is needed.

Username must pass format validation before availability validation, email must pass format validation before domain validation, and password must pass strength validation before history validation. The fields are independent of each other and can be validated in parallel, but within each field, format must be valid before business rule validation is meaningful, requiring sequential validation. Additionally, errors occurring simultaneously across multiple fields need to be collected, while distinguishing whether each field's error is a format error or a business rule violation.

**The Apply with internal Bind nested validation pattern** efficiently handles these complex per-field validation requirements.

## Core Concepts

### Nested Validation Structure

The outer Apply validates fields in parallel, and the Bind within each field performs step-by-step sequential validation.

The following code compares a simple processing approach with a nested validation approach.

```csharp
// Previous approach (problematic) - processes all validations simply
public static Validation<Error, MemberRegistration> ValidateOld(string username, string email, string password)
{
    // Handles complex validation of each field in a single method, making it complex
    var usernameResult = ValidateUsernameComplex(username);
    var emailResult = ValidateEmailComplex(email);
    var passwordResult = ValidatePasswordComplex(password);
    // Complex logic concentrated in a single method reduces readability
}

// Improved approach (current) - nested validation structure
public static Validation<Error, (string Username, string Email, string Password)> Validate(
    string username, string email, string password) =>
    // Outer Apply - validates 3 fields in parallel, each using Bind internally
    (ValidateUsername(username), ValidateEmail(email), ValidatePassword(password))
        .Apply((validUsername, validEmail, validPassword) =>
            (Username: validUsername, Email: validEmail, Password: validPassword))
        .As();
```

Since the outer Apply validates three fields simultaneously, if both username and email have format errors, both errors are collected together. At the same time, within each field, Bind skips business rule validation when format validation fails.

### Fine-Grained Per-Field Validation Control

Each field's validation is decomposed into two stages: format validation and business rule validation. Thanks to Bind, business rule validation runs only when the format is valid.

```csharp
// Username validation - uses Bind internally (2-stage validation)
private static Validation<Error, string> ValidateUsername(string username) =>
    ValidateUsernameFormat(username)
        .Bind(_ => ValidateUsernameAvailability(username));

// Email validation - uses Bind internally (2-stage validation)
private static Validation<Error, string> ValidateEmail(string email) =>
    ValidateEmailFormat(email)
        .Bind(_ => ValidateEmailDomain(email));

// Password validation - uses Bind internally (2-stage validation)
private static Validation<Error, string> ValidatePassword(string password) =>
    ValidatePasswordStrength(password)
        .Bind(_ => ValidatePasswordHistory(password));
```

The advantage of this structure is that each field's validation logic can be managed and tested independently.

## Practical Guidelines

### Expected Output
```
=== Nested Validation Example ===
An example of nested validation using Bind inside Apply for member registration information value object.

--- Valid Member Registration ---
Username: 'john_doe'
Email: 'john@example.com'
Password: 'SecurePass123'
 Success: Member registration information is valid.
   -> User: john_doe (john@example.com)
   -> All nested validation rules passed.

--- Username and Email Format Errors ---
Username: 'ab'
Email: 'invalid-email'
Password: 'SecurePass123'
 Failure:
   -> Total 2 validation failures:
     1. Error code: DomainErrors.MemberRegistration.UsernameTooShort
        Current value: 'ab'
     2. Error code: DomainErrors.MemberRegistration.EmailMissingAt
        Current value: 'invalid-email'
```

### Key Implementation Points

Three points to note during implementation. Validate 3 fields in parallel with outer Apply, perform 2-stage sequential validation with Bind inside each field, and collect errors occurring simultaneously across multiple fields with ManyErrors.

## Project Description

### Project Structure
```
04-Apply-Internal-Bind-Validation/
├── Program.cs              # Main entry point
├── ValueObjects/
│   └── MemberRegistration.cs # Member registration value object (nested validation pattern implementation)
├── ApplyInternalBindValidation.csproj
└── README.md               # Main document
```

### Core Code

The MemberRegistration value object validates fields in parallel with outer Apply and sequentially validates format and business rules with Bind inside each field.

```csharp
public sealed class MemberRegistration : ValueObject
{
    public string Username { get; }
    public string Email { get; }
    public string Password { get; }

    // Nested validation pattern implementation (Apply with internal Bind)
    public static Validation<Error, (string Username, string Email, string Password)> Validate(
        string username, string email, string password) =>
        // Outer Apply - validates 3 fields in parallel, each using Bind internally
        (ValidateUsername(username), ValidateEmail(email), ValidatePassword(password))
            .Apply((validUsername, validEmail, validPassword) =>
                (Username: validUsername, Email: validEmail, Password: validPassword))
            .As();

    // Username validation (independent) - uses Bind internally (2-stage validation)
    private static Validation<Error, string> ValidateUsername(string username) =>
        ValidateUsernameFormat(username)
            .Bind(_ => ValidateUsernameAvailability(username));

    // Email validation (independent) - uses Bind internally (2-stage validation)
    private static Validation<Error, string> ValidateEmail(string email) =>
        ValidateEmailFormat(email)
            .Bind(_ => ValidateEmailDomain(email));

    // Password validation (independent) - uses Bind internally (2-stage validation)
    private static Validation<Error, string> ValidatePassword(string password) =>
        ValidatePasswordStrength(password)
            .Bind(_ => ValidatePasswordHistory(password));

    // Detailed validation methods
    private static Validation<Error, string> ValidateUsernameFormat(string username) =>
        !string.IsNullOrWhiteSpace(username) && username.Length >= 3
            ? username
            : DomainErrors.UsernameTooShort(username);

    private static Validation<Error, string> ValidateUsernameAvailability(string username) =>
        !username.StartsWith("admin")
            ? username
            : DomainErrors.UsernameNotAvailable(username);
}
```

## Summary at a Glance

The following table compares the differences between Apply with internal Bind nested validation and existing patterns.

| Aspect | Apply Parallel Validation | Bind Sequential Validation | Apply with Internal Bind Nested Validation |
|------|----------------|----------------|-------------------------|
| **Outer structure** | Runs all validations in parallel | Runs all validations sequentially | Validates fields in parallel |
| **Inner structure** | Simple individual validations | Simple individual validations | Sequential validation inside each field |
| **Complexity** | Low | Low | High (complex logic per field) |
| **Performance** | Fast via parallel execution | Efficient via short-circuiting | Combination of parallel and sequential |

The following table summarizes the pros and cons of this pattern.

| Pros | Cons |
|------|------|
| Can implement complex validation logic per field | Validation structure becomes complex |
| Combines advantages of parallel and sequential validation | Debugging nested validation logic is difficult |
| Easy to add new fields or validation stages | Validation layers must be carefully designed |

## FAQ

### Q1: When should the nested validation pattern be used?
**A:** Use it when each field requires complex multi-stage validation. A typical case is when username needs format validation followed by availability validation, and email needs format validation followed by domain validation.

### Q2: What are the roles of outer Apply and inner Bind?
**A:** Outer Apply validates fields in parallel to optimize performance, while inner Bind processes each field's validation logic step by step. Multiple fields are processed simultaneously while each field is processed sequentially internally, securing both performance and accuracy.

### Q3: How is error handling done?
**A:** Errors occurring simultaneously across multiple fields are collected via the ManyErrors type. It is important to distinguish between format errors and business rule violations and provide clear feedback to the user.

In this chapter, we covered the outer Apply, inner Bind structure. So when would the reverse direction -- outer Bind to validate preconditions first, then inner Apply to validate components in parallel -- be useful? The next chapter examines this reverse nested pattern.

---

-> [Chapter 5: Bind with Internal Apply](../05-Bind-Internal-Apply-Validation/)
