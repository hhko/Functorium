---
title: "Apply Parallel Validation"
---

## Overview

Suppose a user enters email, password, name, and age all incorrectly on a registration form. Using the Bind pattern, only the email error is reported and execution stops. The user fixes the email, submits again, and this time receives the password error... this process must be repeated four times. The Apply pattern runs all validations simultaneously and collects all errors at once.

## Learning Objectives

- Understand the **Apply operator's parallel execution mechanism** that runs mutually independent validation rules simultaneously.
- Implement the **error collection pattern** that collects all validation failures at once and provides complete feedback to the user.
- Leverage the **ManyErrors type** to handle multiple errors in a structured manner.

## Why Is This Needed?

In the previous step, Bind sequential validation, we learned how to run dependent validation rules sequentially. However, when validating mutually independent information, a different approach is needed.

From a user experience perspective, showing all problems at once when multiple fields have invalid values is much more efficient. There is no reason to stop password or name validation just because the email format is wrong. Running independent validations simultaneously can reduce total validation time, and collecting all validation failures at once allows providing structured error information.

**The Apply parallel validation pattern** satisfies all these requirements by running independent validation rules in parallel.

## Core Concepts

### Apply Parallel Execution Mechanism

Apply runs validation rules that have no dependencies on each other simultaneously. Even if one fails, the remaining validations continue, and all failed results are collected.

The following code shows the limitations of the sequential execution approach.

```csharp
// Previous approach (problematic) - sequential execution is inefficient
public static Validation<Error, UserRegistration> ValidateOld(string email, string password, string name, string ageInput)
{
    var emailResult = ValidateEmail(email);
    if (emailResult.IsFail) return emailResult; // Short-circuit skips other validations

    var passwordResult = ValidatePassword(password);
    if (passwordResult.IsFail) return passwordResult; // Short-circuit skips other validations
    // User cannot identify all problems at once
}
```

### Apply Implementation Methods Comparison

There are two ways to implement Apply parallel validation in LanguageExt.

#### Method 1: Tuple-based Apply (Recommended)

This approach groups multiple Validations into a tuple and calls Apply at once.

```csharp
public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
    string email, string password, string name, string ageInput) =>
    (ValidateEmailFormat(email), ValidatePasswordStrength(password), ValidateNameFormat(name), ValidateAgeFormat(ageInput))
        .Apply((validEmail, validPassword, validName, validAge) =>
            (Email: validEmail, Password: validPassword, Name: validName, Age: validAge))
        .As();
```

It is concise and intuitive, and since the number of validations is clearly visible, it is recommended for most situations.

#### Method 2: fun-based Individual Apply

This approach uses the `fun` function to chain individual Apply calls using Currying.

```csharp
using static LanguageExt.Prelude;

public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
    string email, string password, string name, string ageInput) =>
    fun((string e, string p, string n, int a) => (Email: e, Password: p, Name: n, Age: a))
        .Map(f => Success<Error, Func<string, string, string, int, (string, string, string, int)>>(f))
        .Apply(ValidateEmailFormat(email))
        .Apply(ValidatePasswordStrength(password))
        .Apply(ValidateNameFormat(name))
        .Apply(ValidateAgeFormat(ageInput));
```

Or it can be written more concisely using `Pure`.

```csharp
public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
    string email, string password, string name, string ageInput) =>
    Pure<Validation<Error>, Func<string, string, string, int, (string, string, string, int)>>(
        fun((string e, string p, string n, int a) => (Email: e, Password: p, Name: n, Age: a)))
        .Apply(ValidateEmailFormat(email))
        .Apply(ValidatePasswordStrength(password))
        .Apply(ValidateNameFormat(name))
        .Apply(ValidateAgeFormat(ageInput));
```

This approach provides flexibility through step-by-step application via Currying and is useful when dynamically adjusting the number of validations.

#### Comparison of the Two Methods

The following table compares the characteristics of the two Apply implementation methods.

| Aspect | Tuple-based Apply | fun-based Individual Apply |
|------|----------------|---------------------|
| **Code conciseness** | Concise and intuitive | Relatively verbose |
| **Type inference** | Automatic inference | `fun` supports type inference |
| **Flexibility** | Fixed number of validations | Dynamic number possible |
| **When to use** | Most cases | Advanced composition, dynamic parameters |
| **Learning curve** | Low | Requires understanding of Currying |

> **Recommendation**: Use **tuple-based Apply** for general cases. Consider fun-based individual Apply when you need to dynamically combine validations or deeply leverage functional programming patterns.

### Error Collection and ManyErrors Handling

Apply collects all validation failures into a `ManyErrors` type. The following code shows how to iterate through collected errors and display them to the user.

```csharp
// Handling multiple errors via ManyErrors
if (error is ManyErrors manyErrors)
{
    Console.WriteLine($"   -> Total {manyErrors.Errors.Count} validation failures:");
    for (int i = 0; i < manyErrors.Errors.Count; i++)
    {
        var individualError = manyErrors.Errors[i];
        if (individualError is ExpectedError errorCodeExpected)
        {
            Console.WriteLine($"     {i + 1}. Error code: {errorCodeExpected.ErrorCode}");
            Console.WriteLine($"        Current value: '{errorCodeExpected.ErrorCurrentValue}'");
        }
    }
}
```

## Practical Guidelines

### Expected Output
```
=== Independent Validation Example ===
Runs all validation rules for the user registration value object in parallel.

--- Valid User Registration ---
Email: 'newuser@example.com'
Password: 'newpass123'
Name: 'John Doe'
Age: '25'
Success: User registration is valid.
   -> Registered user: John Doe (newuser@example.com)
   -> All independent validation rules passed.

--- All Validations Fail Simultaneously (Core of Apply) ---
Email: ''
Password: 'short'
Name: 'A'
Age: 'abc'
Failure:
   -> Total 4 validation failures:
     1. Error code: Domain.UserRegistration.EmailMissingAt
        Current value: ''
     2. Error code: Domain.UserRegistration.PasswordTooShort
        Current value: 'short'
     3. Error code: Domain.UserRegistration.NameTooShort
        Current value: 'A'
     4. Error code: Domain.UserRegistration.AgeNotNumeric
        Current value: 'abc'
```

### Key Implementation Points

Three points to note during implementation. Group multiple validations into a tuple and run them in parallel with Apply, collect all validation failures using ManyErrors, and display all problems to the user at once.

## Project Description

### Project Structure
```
02-Apply-Parallel-Validation/
├── Program.cs              # Main entry point
├── ValueObjects/
│   └── UserRegistration.cs # User registration value object (Apply pattern implementation)
├── ApplyParallelValidation.csproj
└── README.md               # Main document
```

### Core Code

The UserRegistration value object uses Apply to validate email, password, name, and age simultaneously.

```csharp
public sealed class UserRegistration : ValueObject
{
    public string Email { get; }
    public string Password { get; }
    public string Name { get; }
    public int Age { get; }

    // Parallel validation implementation via Apply
    public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
        string email, string password, string name, string ageInput) =>
        // Run core validation rules in parallel (independent validations)
        (ValidateEmailFormat(email), ValidatePasswordStrength(password), ValidateNameFormat(name), ValidateAgeFormat(ageInput))
            .Apply((validEmail, validPassword, validName, validAge) =>
                (Email: validEmail, Password: validPassword, Name: validName, Age: validAge))
            .As();

    // Independent validation methods
    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".")
            ? email
            : DomainError.For<UserRegistration>(new EmailMissingAt(), email, "Email is missing @ sign");

    private static Validation<Error, string> ValidatePasswordStrength(string password) =>
        password.Length >= 8
            ? password
            : DomainError.For<UserRegistration>(new PasswordTooShort(), password, "Password is too short");
}
```

## Summary at a Glance

The following table compares the difference between Bind sequential validation and Apply parallel validation.

| Aspect | Bind Sequential Validation | Apply Parallel Validation |
|------|----------------|----------------|
| **Execution method** | Chains and executes sequentially | Runs all validations simultaneously |
| **error handling** | Short-circuits at the first failure | Collects all failures and returns them |
| **Performance** | Efficient via short-circuiting | Fast via parallel execution |
| **User experience** | Only one problem identified at a time | All problems identified at once |

The following table summarizes the pros and cons of Apply parallel validation.

| Pros | Cons |
|------|------|
| All problems identified at once | Error handling is complex |
| Fast validation via parallel execution | All errors must be kept in memory |
| Collects all validation failures | Validation rules must be independent |

## FAQ

### Q1: When should I choose Apply vs Bind?
**A:** Check the dependencies between validation rules. If they are mutually independent, use Apply. If previous results affect the next validation, use Bind.

### Q2: How is ManyErrors handled?
**A:** Check the ManyErrors type, then iterate through the multiple errors and handle each one. It is good practice to check whether each is an ExpectedError type and display the error code and current value.

### Q3: How do you handle the case where all validations fail?
**A:** ManyErrors collects all failures and provides complete feedback to the user. By clearly distinguishing and displaying each error, the user can identify and fix all problems at once.

So far we have examined Bind (sequential) and Apply (parallel) independently. However, in real domains, independent fields and dependent fields coexist in a single object. The next chapter resolves these complex validation requirements by combining Apply and Bind.

---

-> [Chapter 3: Apply and Bind Combined](../03-Apply-Bind-Combined-Validation/)
