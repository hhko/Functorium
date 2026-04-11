---
title: "Why You Should Read This Tutorial"
---
## Overview

Are you checking every call site to see if `string email` is empty or `int denominator` is zero? If you forget a check, a runtime exception fires; even if you never forget, the same `if` statements proliferate throughout your code.

This tutorial covers how to implement **value objects whose types themselves guarantee validity** using functional programming principles. Starting from a basic division function and progressing to a complete framework pattern, you will work through **29 hands-on projects** step by step.

---

## Target Audience

The following table recommends a learning scope based on your experience level.

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers who know basic C# syntax and want to get started with functional programming | Part 1 (Chapters 1-6) |
| **Intermediate** | Developers who understand functional concepts and want practical application | All of Parts 1-3 |
| **Advanced** | Developers interested in framework design and architecture | Parts 4-5 + Appendices |

---

## Prerequisites

The following knowledge is needed to effectively learn from this tutorial:

### Required
- Understanding of basic C# syntax (classes, interfaces, generics)
- Fundamentals of object-oriented programming
- Experience running .NET projects

### Recommended (Nice to Have)
- Basic LINQ syntax
- Unit testing experience
- Fundamentals of Domain-Driven Design (DDD)

---

## Expected Outcomes

Upon completing this tutorial, you will be able to:

### 1. Write safe code using explicit result types instead of exceptions

```csharp
// ❌ Exception-based - problematic approach
public User CreateUser(string email, int age)
{
    if (string.IsNullOrEmpty(email))
        throw new ArgumentException("Email is required.");
    return new User(email, age);
}

// ✅ Success-driven - recommended approach
public Fin<User> CreateUser(string email, int age)
{
    return
        from validEmail in Email.Create(email)
        from validAge in Age.Create(age)
        select new User(validEmail, validAge);
}
```

### 2. Express domain rules as types to validate at compile time

```csharp
// ❌ Runtime validation - late discovery
public int Divide(int numerator, int denominator)
{
    if (denominator == 0)
        throw new ArgumentException("Cannot divide by zero");
    return numerator / denominator;
}

// ✅ Compile-time guarantee - early discovery
public int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value; // No validation needed!
}
```

### 3. Implement flexible validation logic using Bind/Apply patterns

```csharp
// Bind pattern - sequential validation
var result = ValidateEmail(email)
    .Bind(_ => ValidatePassword(password))
    .Bind(_ => ValidateName(name));

// Apply pattern - parallel validation (collects all errors)
var result = (ValidateEmail(email), ValidatePassword(password), ValidateName(name))
    .Apply((e, p, n) => new User(e, p, n));
```

### 4. Develop production-ready value objects using the Functorium framework

```csharp
public sealed partial class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMaxLength(320)
            .ThenMatches(EmailRegex(), "Invalid email format");

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
```

---

## Three Core Perspectives

This tutorial integrates three perspectives: Success-Driven Development, Functional Programming, and DDD Value Objects.

| Perspective | Core Principle | Application in This Tutorial |
|-------------|---------------|------------------------------|
| **Success-Driven Development** | Design around the success path | Using `Fin<T>`, `Validation<Error, T>` |
| **Functional Programming** | Pure functions, immutability, composition | Monad chaining, LINQ expressions |
| **DDD Value Objects** | Typing domain concepts | `ValueObject` framework types |

---

## Learning Path

The following diagram shows the recommended learning scope by level.

```
Beginner (Part 1: Chapters 1-6)
├── Exceptions vs Domain Types
├── Defensive Programming
├── Fin<T>, Validation<Error, T>
└── Always-Valid Value Objects

Intermediate (Part 1: Chapters 7-14 + Parts 2-3)
├── Value Equality, Comparability
├── Bind/Apply Validation Patterns
└── Framework Type Usage

Advanced (Parts 4-5 + Appendices)
├── ORM, CQRS Integration
├── Domain-Specific Practical Examples
└── Architecture Tests
```

## FAQ

### Q1: Can I follow along without any functional programming experience?
**A**: Yes. This tutorial is designed so you can start with just basic C# syntax knowledge. In Part 1, you first observe the limitations of `int` and `string`, then gradually introduce functional types like `Fin<T>` and `Validation<Error, T>`.

### Q2: What is the difference between `Fin<T>` and `Validation<Error, T>`?
**A**: `Fin<T>` is a final result type representing success or failure, while `Validation<Error, T>` is a type that can collect all validation errors. Use `Fin<T>` when you only need a single error; use `Validation<Error, T>` when you want to collect multiple errors simultaneously.

### Q3: Do I have to complete all 29 projects?
**A**: No. Refer to the recommended learning scope in the target audience table. Beginners should cover Part 1 (Chapters 1-6), intermediate learners Parts 1-3, and advanced learners Parts 4-5. Completing only up to your needed level is perfectly sufficient.

---

## Next Steps

In the next chapter, we take a detailed look at **Success-Driven Development**, the core concept that makes this kind of code possible. We compare how the exception-centric approach differs from the success-centric approach.

→ [0.2 What Is Success-Driven Development?](02-success-driven-development.md)
