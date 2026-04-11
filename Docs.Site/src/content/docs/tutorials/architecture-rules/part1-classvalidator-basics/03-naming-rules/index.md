---
title: "Naming Rules"
---
## Overview

The team agreed that service classes use the `Service` suffix and event classes use the `Event` suffix. But one day someone created a class called `OrderSvc`, and someone else created an event called `ProductChanged`. It might get caught in code review, but if it does not, consistency gradually breaks down. In this chapter, you will learn how to prevent such inconsistencies at the source by automating **naming rules** with architecture tests.

> **"Naming rules determine the readability and navigability of code. Without automated verification, rules gradually fade over time."**

## Learning Objectives

### Core Learning Goals
1. **Suffix rules**
   - `RequireNameEndsWith("Service")`: Enforce service class naming
   - `RequireNameEndsWith("Event")`: Enforce event class naming
   - `RequireNameEndsWith("Dto")`: Enforce DTO class naming
2. **Prefix rules**
   - `RequireNameStartsWith("I")`: Enforce interface naming convention
3. **Regular expression pattern rules**
   - `RequireNameMatching(".*Repository$")`: Verify complex naming patterns
4. **Interface verification**
   - Verify interface targets with `ValidateAllInterfaces`

### What You Will Verify Through Practice
- Apply suffix rules by namespace (Service, Event, Dto)
- Verify interface prefix with `ValidateAllInterfaces`
- Compound naming rules using regular expression patterns

## Project Structure

```
03-Naming-Rules/
├── NamingRules/                              # Main project
│   ├── Domains/
│   │   ├── OrderService.cs
│   │   ├── ProductService.cs
│   │   ├── OrderCreatedEvent.cs
│   │   └── ProductUpdatedEvent.cs
│   ├── Dtos/
│   │   ├── OrderDto.cs
│   │   └── ProductDto.cs
│   ├── Repositories/
│   │   ├── IOrderRepository.cs
│   │   └── IProductRepository.cs
│   ├── Program.cs
│   └── NamingRules.csproj
├── NamingRules.Tests.Unit/                   # Test project
│   ├── ArchitectureTests.cs
│   ├── NamingRules.Tests.Unit.csproj
│   └── xunit.runner.json
└── README.md
```

## Test Code Walkthrough

### Suffix Verification (RequireNameEndsWith)

```csharp
[Fact]
public void ServiceClasses_ShouldEndWith_Service()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveNameEndingWith("Service")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNameEndsWith("Service"),
            verbose: true)
        .ThrowIfAnyFailures("Service Naming Rule");
}
```

After filtering targets with `HaveNameEndingWith("Service")`, the rule is verified with `RequireNameEndsWith("Service")`. This pattern can be used for rules like "all classes in the Service namespace must end with Service".

### DTO Naming Verification

```csharp
[Fact]
public void DtoClasses_ShouldEndWith_Dto()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DtoNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNameEndsWith("Dto"),
            verbose: true)
        .ThrowIfAnyFailures("DTO Naming Rule");
}
```

This verifies that **all classes** in the `Dtos` namespace end with `Dto`. Combining namespace-based filtering with naming rules allows you to define powerful rules.

### Interface Prefix Verification (ValidateAllInterfaces)

```csharp
[Fact]
public void Interfaces_ShouldStartWith_I()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(RepositoryNamespace)
        .ValidateAllInterfaces(Architecture, @interface => @interface
            .RequireNameStartsWith("I"),
            verbose: true)
        .ThrowIfAnyFailures("Interface Naming Rule");
}
```

When verifying interfaces, use `ArchRuleDefinition.Interfaces()` and `ValidateAllInterfaces`. **InterfaceValidator** provides the same naming verification methods as ClassValidator.

### Regular Expression Pattern Verification (RequireNameMatching)

```csharp
[Fact]
public void RepositoryInterfaces_ShouldMatch_RepositoryPattern()
{
    ArchRuleDefinition.Interfaces()
        .That()
        .ResideInNamespace(RepositoryNamespace)
        .ValidateAllInterfaces(Architecture, @interface => @interface
            .RequireNameMatching(".*Repository$"),
            verbose: true)
        .ThrowIfAnyFailures("Repository Naming Rule");
}
```

`RequireNameMatching` uses regular expression patterns to verify complex naming rules. Use it for rules that are difficult to express with prefix/suffix verification alone.

## Summary at a Glance

The following table organizes the naming rule verification methods.

### Naming Verification Methods

| Method | Verifies | Use Scenario |
|--------|----------|-------------|
| `RequireNameEndsWith(suffix)` | Name ends with specified suffix | Service, Event, Dto, etc. |
| `RequireNameStartsWith(prefix)` | Name starts with specified prefix | I (interface), Abstract, etc. |
| `RequireNameMatching(regex)` | Matches regular expression pattern | Compound naming rules |
| `ValidateAllInterfaces` | Entry point for interface target verification | Applying interface rules |

### ClassValidator vs InterfaceValidator

Both Validators inherit from the **common base class `TypeValidator`**. Naming rules (`RequireNameStartsWith`, `RequireNameEndsWith`, `RequireNameMatching`) and interface implementation rules are defined in `TypeValidator`, so they can be used identically in both Validators. The difference is the entry point:

- **`ValidateAllClasses`** -- Uses `ClassValidator` to verify classes
- **`ValidateAllInterfaces`** -- Uses `InterfaceValidator` to verify interfaces

## FAQ

### Q1: What is the difference between `RequireNameEndsWith` and `HaveNameEndingWith`?
**A**: `HaveNameEndingWith` is an ArchUnitNET **filtering** method that narrows verification targets. `RequireNameEndsWith` is a Functorium **rule verification** method that verifies whether selected targets satisfy a condition. Filtering determines "which classes to inspect", while rules determine "what conditions those classes must satisfy".

### Q2: Is case-insensitive naming verification possible?
**A**: You can specify a case-insensitive option with a regular expression in `RequireNameMatching`. For example, `RequireNameMatching("(?i).*service$")` matches both `OrderService` and `orderservice`.

### Q3: Can naming rules and visibility rules be combined in a single chain?
**A**: Yes, you can. Combine visibility and naming rules in a single Validator chain like `@class.RequirePublic().RequireNameEndsWith("Service")`. Bundling related rules together makes the intent of the rules clearer.

---

The next chapter covers how to verify class inheritance relationships and interface implementations with architecture tests.

-> [Ch 4: Inheritance and Interfaces](../04-Inheritance-And-Interface/)
