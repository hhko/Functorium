---
title: "Custom Rules"
---

## Overview

"All domain classes must have a `Create` factory method", "Classes with a Service suffix are prohibited" -- the framework does not provide these team-specific rules by default. But what if you could create them yourself?

In this chapter, you will learn how to **define and compose project-specific custom rules** using `DelegateArchRule`, `CompositeArchRule`, and the `Apply()` method. When built-in rules are not enough, infinite extensibility becomes possible.

> **"A good architecture test framework is not one with rich built-in rules, but one that is easily extensible when built-in rules fall short."**

## Learning Objectives

### Core Learning Goals

1. **Writing lambda-based custom rules with `DelegateArchRule<T>`**
   - Constructor pattern that takes a rule description and validation function
   - How to report violations by returning `RuleViolation`

2. **AND composition of multiple rules with `CompositeArchRule<T>`**
   - Pattern for combining individual rules into compound rules
   - How all violations from all rules are collected

3. **Integrating custom rules into existing verification chains with `Apply()`**
   - Freely mix with built-in rules (`RequireSealed()`, `RequireImmutable()`)
   - Apply both built-in and custom rules in a single verification chain

### What You Will Verify Through Practice
- **Factory method rule**: Verify that all domain classes have a static `Create` method
- **Service suffix prohibition rule**: Verify that domain class names do not end with `Service`
- **Composite rule composition**: Combine two custom rules with AND and apply at once

## Domain Code

### Invoice / Payment - Domain Classes

```csharp
public sealed class Invoice
{
    public string InvoiceNo { get; }
    public decimal Amount { get; }

    private Invoice(string invoiceNo, decimal amount)
    {
        InvoiceNo = invoiceNo;
        Amount = amount;
    }

    public static Invoice Create(string invoiceNo, decimal amount)
        => new(invoiceNo, amount);
}

public sealed class Payment
{
    public string PaymentId { get; }
    public decimal Amount { get; }
    public string Method { get; }

    private Payment(string paymentId, decimal amount, string method)
    {
        PaymentId = paymentId;
        Amount = amount;
        Method = method;
    }

    public static Payment Create(string paymentId, decimal amount, string method)
        => new(paymentId, amount, method);
}
```

## Test Code

### DelegateArchRule - Lambda-Based Custom Rules

`DelegateArchRule<T>` defines rules with lambda functions. The constructor takes a rule description and validation function.

```csharp
private static readonly DelegateArchRule<Class> s_factoryMethodRule = new(
    "All domain classes must have a static Create factory method",
    (target, _) =>
    {
        var hasCreate = target.Members
            .OfType<MethodMember>()
            .Any(m => m.Name.StartsWith("Create(") && m.IsStatic == true);
        return hasCreate
            ? []
            : [new RuleViolation(target.FullName, "FactoryMethodRequired",
                $"Class '{target.Name}' must have a static Create method.")];
    });
```

The validation function takes `(TType target, Architecture architecture)` parameters and returns `IReadOnlyList<RuleViolation>`.
If there are no violations it returns an empty list; if there are violations it returns a list of `RuleViolation` instances.

### CompositeArchRule - AND Composition

`CompositeArchRule<T>` composes multiple `IArchRule<T>` instances with AND. It collects violations from all rules.

```csharp
private static readonly CompositeArchRule<Class> s_domainClassRule = new(
    s_factoryMethodRule,
    s_noServiceSuffixRule);
```

### Apply - Applying Custom Rules

Custom rules are integrated into the verification chain with the `Apply()` method.

```csharp
[Fact]
public void DomainClasses_ShouldSatisfy_CompositeRule()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .Apply(s_domainClassRule),
            verbose: true)
        .ThrowIfAnyFailures("Domain Composite Rule");
}
```

### Mixing Built-In and Custom Rules

Built-in methods like `RequireSealed()` and `RequireImmutable()` can be freely chained with `Apply()`.

```csharp
[Fact]
public void DomainClasses_ShouldSatisfy_CompositeRuleWithBuiltIn()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireSealed()
            .RequireImmutable()
            .Apply(s_domainClassRule),
            verbose: true)
        .ThrowIfAnyFailures("Domain Full Composite Rule");
}
```

## Summary at a Glance

The following table summarizes the core types used for custom rule authoring.

### Custom Rule Core Type Summary

| Type | Role | Usage |
|------|------|-------|
| **`IArchRule<T>`** | Interface for custom rules | Defines `Description` and `Validate()` |
| **`DelegateArchRule<T>`** | Define rules with lambda functions | `new DelegateArchRule<Class>("description", (target, arch) => ...)` |
| **`CompositeArchRule<T>`** | AND composition of multiple rules | `new CompositeArchRule<Class>(rule1, rule2)` |
| **`RuleViolation`** | Sealed record containing violation information | `(TargetName, RuleName, Description)` |
| **`Apply(rule)`** | Integrates custom rules into verification chain | `.Apply(s_domainClassRule)` |

The following table compares the roles of built-in and custom rules.

### Built-In Rules vs Custom Rules

| Aspect | Built-In Rules | Custom Rules |
|--------|---------------|--------------|
| **Definition method** | `RequireXxx()` method calls | `DelegateArchRule` or `IArchRule` implementation |
| **Application method** | Direct chaining | `Apply(rule)` |
| **Composition** | Automatic AND through chaining | Explicit AND with `CompositeArchRule` |
| **Reusability** | Provided by framework | Shareable within project |

## FAQ

### Q1: What is the difference between `DelegateArchRule` and directly implementing `IArchRule`?
**A**: `DelegateArchRule` is suitable for quickly defining simple rules with lambdas. When rule logic is complex, state (fields) is needed, or the rule must be reused in multiple places, creating a class that directly implements the `IArchRule<T>` interface is more appropriate.

### Q2: Does `CompositeArchRule` also support OR composition?
**A**: No. `CompositeArchRule` only supports AND composition -- it collects and returns violations from all rules. If OR composition is needed, you must implement the OR logic directly inside a `DelegateArchRule`.

### Q3: When is the `Architecture` parameter used in custom rules?
**A**: The `Architecture` parameter is used when accessing type information across the entire project. For example, it is needed when analyzing relationships between types, such as "does this class depend on another class that implements a specific interface?" For simple member inspection, it can be ignored with `_`.

### Q4: Can `Apply()` be called multiple times?
**A**: Yes, you can apply multiple custom rules sequentially like `.Apply(rule1).Apply(rule2)`. This has the same effect as bundling them with `CompositeArchRule`, but can be expressed more readably in chaining style.

---

Being able to write custom rules means the framework's limits do not become the project's limits. Now that you have learned all the advanced verification techniques in Part 3, the next Part 4 applies all these techniques to real-world layer-by-layer architecture rules.

-> [Part 4: Real-World Patterns](../../Part4-Real-World-Patterns/)
