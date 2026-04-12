---
title: "Composite Value Object"
---

> `ValueObject`

## Overview

When handling an email address as a single string, the validation logic for the local part (`user`) and domain part (`example.com`) gets mixed up, making it difficult to reuse each part independently. A Composite Value Object combines smaller value objects to structurally represent such complex domain concepts.

## Learning Objectives

1. Express complex domain concepts by combining multiple value objects.
2. Implement hierarchical validation logic using LINQ Expressions.
3. Override `GetEqualityComponents()` to define composite equality.
4. Modularize smaller value objects for reuse in other contexts.

## Why Is This Needed?

In the previous step, `04-ComparableValueObject-Primitive`, we combined multiple primitive types directly to represent composite data. However, more complex domain concepts arise in real applications.

For cases like email addresses where the local part and domain part each have different rules, they must be handled as a single unit. Format validation, split validation, local part validation, and domain validation must proceed hierarchically, and the email local part or domain should be independently reusable elsewhere.

A Composite Value Object meets these requirements. It defines `EmailLocalPart` and `EmailDomain` as independent value objects and combines them to create a higher-level concept called `Email`. Each component has its own validation logic, and the overall `Email` determines equality based on the combination of its components.

## Core Concepts

### Value Object Composition

Two smaller value objects, `EmailLocalPart` and `EmailDomain`, are combined to create a larger concept called `Email`. Each part exists as an independent value object, but together they form the larger concept of email.

```csharp
// Individual value objects
EmailLocalPart localPart = EmailLocalPart.Create("user");
EmailDomain domain = EmailDomain.Create("example.com");

// Composite value object
Email email = Email.Create("user@example.com");
```

This composition greatly enhances code modularity and reusability. The smaller value objects can be reused in other contexts.

### Hierarchical Validation Logic

Composite value objects have multi-level validation logic. Email validation proceeds hierarchically through format validation, split validation, local part validation, and domain validation.

Using the `from-in` chain of LINQ Expressions, these validation stages can be expressed declaratively.

```csharp
// Hierarchical validation
public static Validation<Error, (EmailLocalPart, EmailDomain)> Validate(string email) =>
    from validEmail in ValidateEmailFormat(email)        // 1. Format validation
    from validParts in ValidateEmailParts(validEmail)     // 2. Split validation
    select validParts;                                     // Combine results
```

If any stage fails, subsequent stages are not executed, allowing systematic implementation of complex business rules.

### Composite Equality

The equality of a composite value object is determined by comprehensively evaluating the equality of all components. Email address equality holds when both local part and domain part are equal.

All elements returned from `GetEqualityComponents()` must be pairwise equal for two objects to be identical.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return LocalPart;  // Compare local part
    yield return Domain;     // Compare domain part
}
```

## Practical Guidelines

### Expected Output
```
=== 5. Non-Comparable Composite Value Object - ValueObject ===
Parent class: ValueObject
Example: Email (email address) - EmailLocalPart + EmailDomain composition

Features:
   Value object with complex validation logic
   Provides equality comparison only
   Expresses more complex domain concepts by combining multiple value objects
   EmailLocalPart + EmailDomain = Email

Success Cases:
   Email: user@example.com
     - LocalPart: user
     - Domain: example.com

   Email: user@example.com
     - LocalPart: user
     - Domain: example.com

   Email: admin@test.org
     - LocalPart: admin
     - Domain: test.org

Equality Comparison:
   user@example.com == user@example.com = True
   user@example.com == admin@test.org = False

Hash Code:
   user@example.com.GetHashCode() = -1711187277
   user@example.com.GetHashCode() = -1711187277
   Same value hash codes equal? True

Failure Cases:
   Email("invalid-email"): InvalidEmailFormat
   Email("@example.com"): EmptyOrOutOfRange
   Email("user@"): EmptyOrInvalidFormat

Composite value object characteristics:
   - EmailLocalPart and EmailDomain are each independent value objects
   - Email expresses a more complex domain concept by combining these two value objects
   - Each component has its own validation logic
   - The overall Email determines equality through the combination of components

Demo completed successfully!
```

### Key Implementation Points

The following four are the core of composite value object implementation.

| Point | Description |
|--------|------|
| **Hierarchical value object structure** | EmailLocalPart + EmailDomain -> Email |
| **LINQ Expression hierarchical validation** | Composite validation via from-in chaining |
| **Composite GetEqualityComponents() implementation** | Defines equality across multiple components |
| **Modularity** | Ensures reusability of smaller value objects |

## Project Description

### Project Structure
```
05-ValueObject-Composite/
├── Program.cs                    # Main entry point
├── ValueObjectComposite.csproj  # Project file
├── ValueObjects/
│   ├── Email.cs                 # Composite email value object
│   ├── EmailLocalPart.cs        # Email local part value object
│   └── EmailDomain.cs           # Email domain value object
└── README.md                    # Project document
```

### Core Code

`EmailLocalPart` represents the email local part as an independent value object.

**EmailLocalPart.cs - basic value object**
```csharp
public sealed class EmailLocalPart : SimpleValueObject<string>
{
    private EmailLocalPart(string value) : base(value) { }

    public static Fin<EmailLocalPart> Create(string value) =>
        CreateFromValidation(Validate(value), v => new EmailLocalPart(v));

    public static EmailLocalPart CreateFromValidated(string validatedValue) =>
        new(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 1 && value.Length <= 64
            ? value
            : DomainError.For<EmailLocalPart>(new DomainErrorType.WrongLength(), value,
                $"Email local part is empty or out of range. Must be 1-64 characters. Current value: '{value}'");

    public override string ToString() => Value;
}
```

`Email` combines `EmailLocalPart` and `EmailDomain` to provide composite equality and hierarchical validation.

**Email.cs - composite value object**
```csharp
public sealed class Email : ValueObject
{
    public EmailLocalPart LocalPart { get; }
    public EmailDomain Domain { get; }

    private Email(EmailLocalPart localPart, EmailDomain domain)
    {
        LocalPart = localPart;
        Domain = domain;
    }

    // Hierarchical validation
    public static Validation<Error, (EmailLocalPart, EmailDomain)> Validate(string email) =>
        from validEmail in ValidateEmailFormat(email)      // 1. Format validation
        from validParts in ValidateEmailParts(validEmail)   // 2. Split validation
        select validParts;                                   // Combine results

    // Composite equality
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LocalPart;
        yield return Domain;
    }
}
```

**Program.cs - composite value object demo**
```csharp
// Create composite value objects
var email1 = Email.Create("user@example.com");
var email2 = Email.Create("user@example.com");

// Equality comparison
var e1 = email1.Match(Succ: x => x, Fail: _ => default!);
var e2 = email2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {e1} == {e2} = {e1 == e2}");
```

## Summary at a Glance

Compares the difference between direct primitive composition and value object composition approaches.

### Comparison Table
| Aspect | ValueObject-Primitive | ValueObject-Composite |
|------|----------------------|---------------------|
| **Components** | Primitive types used directly | Value object composition |
| **Validation complexity** | Single-stage validation | Hierarchical validation |
| **Reusability** | Limited | High (component reuse) |
| **Modularity** | Low | High |
| **Maintainability** | Average | High |

### Pros and Cons
| Pros | Cons |
|------|------|
| **High modularity** | Implementation complexity increases |
| **Component reuse** | Complex hierarchy structure |
| **Improved maintainability** | Learning curve exists |
| **Domain expressiveness** | Performance overhead |

## FAQ

### Q1: What is the difference between a composite value object and a regular class?
**A**: A composite value object enforces immutability and value-based equality of its components. It cannot be changed after creation, and explicitly defines how equality comparison works through `GetEqualityComponents()`. Regular classes have no such constraints and can freely change components, but do not guarantee equality and immutability.

### Q2: Why is hierarchical validation used?
**A**: Because complex business rules can be implemented clearly step by step. In email validation, first checking the basic format and then validating the validity of each part makes debugging and maintenance easier. Each validation stage can be independently tested and reused.

### Q3: How does GetEqualityComponents() implement composite equality?
**A**: `GetEqualityComponents()` sequentially returns all components of the composite value object. For two composite value objects to be identical, all returned elements must be pairwise equal. In the case of email, both the local part and domain part must be equal to be treated as the same email.

So far we have examined composite value objects that only support equality. The next chapter covers how to add sorting and comparison functionality to composite value objects by inheriting from `ComparableValueObject`.

---

-> [Chapter 6: ComparableValueObject (Composite)](../06-ComparableValueObject-Composite/)
