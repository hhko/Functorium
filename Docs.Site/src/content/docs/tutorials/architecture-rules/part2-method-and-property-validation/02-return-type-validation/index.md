---
title: "Return Type Validation"
---

## Overview

What happens when someone returns `Email` instead of `Fin<Email>` from `Email.Create`? Compilation passes without issues. The caller expects `IsSucc`/`IsFail` branching, but the code actually throws exceptions, causing failures only at runtime. In this chapter, you will learn how to verify method return types with architecture tests -- enforcing the use of functional result types like **`Fin<T>`** or confirming that factory methods return their own class type.

> **"When you enforce return type rules through tests, you can catch breaks in functional error handling patterns before code review."**

## Learning Objectives

### Core Learning Goals
1. **Open generic return type verification**
   - Match all closed generics like `Fin<Email>`, `Fin<PhoneNumber>` with `RequireReturnType(typeof(Fin<>))`
   - Open generics work through prefix comparison

2. **Self-type return verification**
   - Confirm factory methods return the declaring class type with `RequireReturnTypeOfDeclaringClass()`
   - Suitable for factory patterns that combine already-validated values

3. **Name-based return type verification**
   - Flexibly verify using only part of the type name with `RequireReturnTypeContaining("Fin")`
   - Apply rules without requiring an exact type reference

### What You Will Verify Through Practice
- **Email.Create / PhoneNumber.Create**: Enforce `Fin<T>` return type
- **Customer.CreateFromValidated**: Verify self-type (`Customer`) return
- The difference between open generic matching and string-based matching

## Domain Code

### Email / PhoneNumber Classes

The `Create` method returns `Fin<T>` to safely express creation failure.

```csharp
public sealed class Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value)
        => string.IsNullOrWhiteSpace(value) || !value.Contains('@')
            ? Fin.Fail<Email>(Error.New("Invalid email"))
            : Fin.Succ(new Email(value));
}
```

### Customer Class

`CreateFromValidated` receives already-validated values and directly returns its own type (`Customer`).

```csharp
public sealed class Customer
{
    public string Name { get; }
    public Email Email { get; }

    private Customer(string name, Email email)
    {
        Name = name;
        Email = email;
    }

    public static Customer CreateFromValidated(string name, Email email)
        => new(name, email);
}
```

## Test Code

### Open Generic Return Type Verification

Passing `typeof(Fin<>)` matches all closed generic types like `Fin<Email>`, `Fin<PhoneNumber>`.

```csharp
[Fact]
public void CreateMethods_ShouldReturn_FinOpenGeneric()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireReturnType(typeof(Fin<>))),
            verbose: true)
        .ThrowIfAnyFailures("Fin Return Type Rule");
}
```

### Self-Type Return Verification

`RequireReturnTypeOfDeclaringClass()` verifies that a method's return type matches the declaring class.

```csharp
[Fact]
public void CreateFromValidated_ShouldReturn_DeclaringClass()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Customer")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("CreateFromValidated", m => m
                .RequireReturnTypeOfDeclaringClass()),
            verbose: true)
        .ThrowIfAnyFailures("Factory Return Type Rule");
}
```

### Return Type Name Contains Verification

`RequireReturnTypeContaining` verifies whether the full name of the return type contains a specified string.

```csharp
[Fact]
public void CreateMethods_ShouldReturn_TypeContainingFin()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireReturnTypeContaining("Fin")),
            verbose: true)
        .ThrowIfAnyFailures("Fin Return Type Containing Rule");
}
```

## Summary at a Glance

The following table summarizes the return type verification APIs, their matching approaches, and use scenarios.

### Return Type Verification API Summary
| API | Matching Approach | Use Scenario |
|-----|-------------------|--------------|
| `RequireReturnType(typeof(Fin<>))` | Open generic prefix comparison | Verify generic family: `Fin<Email>`, `Fin<PhoneNumber>`, etc. |
| `RequireReturnType(typeof(string))` | Exact type matching | When a specific type must be returned |
| `RequireReturnTypeOfDeclaringClass()` | Same as declaring class | Factory pattern returning self type |
| `RequireReturnTypeContaining("Fin")` | Type name string contains | Flexible verification without type reference |

### Comparison of Three Verification Approaches
| Aspect | Open Generic | Self Type | String Contains |
|--------|-------------|-----------|-----------------|
| **Precision** | High (type system based) | High (declaring class based) | Moderate (string matching) |
| **Flexibility** | Matches entire generic family | Auto-matches per class | Most flexible |
| **Best suited for** | `Fin<T>`, `Result<T>`, etc. | Builder/factory patterns | When type reference is difficult |

## FAQ

### Q1: How does matching work when passing an open generic typeof(Fin<>) to RequireReturnType?
**A**: Open generics work through prefix comparison. The FullName prefix of `typeof(Fin<>)` is compared against the FullName of the actual return type, matching all closed generic variants like `Fin<Email>`, `Fin<PhoneNumber>`, etc.

### Q2: Does RequireReturnTypeOfDeclaringClass consider inheritance relationships?
**A**: No. It only verifies whether the return type is exactly the same as the declaring class. If a method of the `Customer` class returns `Customer`, it passes, but if it returns a subtype of `Customer`, it fails. This is suitable for enforcing that factory methods return exactly their own type.

### Q3: When should RequireReturnTypeContaining be used?
**A**: It is useful when it is difficult to directly reference the target type's assembly, or when just the type name convention is sufficient. For example, when verifying a generic type from an external library and referencing it with `typeof()` is cumbersome, you can use `RequireReturnTypeContaining` for string-based matching. However, be cautious of false positives since the string "Fin" could be contained in other type names.

### Q4: Can the same approach be used for result types other than Fin<T> (such as Result<T>)?
**A**: Yes, you can apply it identically like `RequireReturnType(typeof(Result<>))` for any generic type. Whatever result type your project uses, it can be consistently enforced through open generic matching.

---

With return type verification, you can now ensure consistency of functional result patterns. The next chapter covers verifying method **parameter counts and types** to prevent arbitrary changes to factory method signatures.

-> [Ch 3: Parameter Verification](../03-Parameter-Validation/)
