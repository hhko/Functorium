---
title: "Customer Management"
---
## Overview

E-commerce product filtering is a representative use case for the Specification pattern. However, this pattern applies equally to Aggregates other than Product. This chapter uses Specifications in the customer management domain to confirm the pattern's versatility.

> **Choose between Expression and non-Expression Specifications based on the situation.**

## Learning Objectives

1. **Applying Specifications to other Aggregates**: Usage in the Customer domain rather than Product
2. **Selection criteria for Expression vs non-Expression**: Understanding the appropriate scenarios for each approach
3. **Mixed composition**: Combining ExpressionSpecification and Specification with `&`, `|` operators
4. **Case-insensitive search**: String comparison strategies in Expression Trees

## Core Concepts

### Expression vs non-Expression Specification

Not all conditions need to be expressed as Expression Trees. For simple property checks, overriding only `IsSatisfiedBy` is more concise.

```csharp
// ExpressionSpecification: when EF Core SQL translation is needed
public sealed class CustomerEmailSpec : ExpressionSpecification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        string emailStr = Email;
        return customer => (string)customer.Email == emailStr;
    }
}

// non-Expression Specification: when only in-memory validation is needed
public sealed class CustomerActiveSpec : Specification<Customer>
{
    public override bool IsSatisfiedBy(Customer entity) => entity.IsActive;
}
```

### Case-Insensitive Search in Expressions

Inside Expression Trees, you cannot use `string.Contains(string, StringComparison)`. Instead, use the `.ToLower().Contains()` pattern.

```csharp
public override Expression<Func<Customer, bool>> ToExpression()
{
    string searchLower = ((string)SearchName).ToLower();
    return customer => ((string)customer.Name).ToLower().Contains(searchLower);
}
```

### Mixed Composition

ExpressionSpecification and non-Expression Specification can be freely composed through the `&`, `|`, `!` operators of their base class `Specification<T>`.

```csharp
// CustomerActiveSpec(non-Expression) & CustomerNameContainsSpec(Expression)
var spec = new CustomerActiveSpec() & new CustomerNameContainsSpec(new CustomerName("Kim"));
```

## Project Description

### Project Structure
```
CustomerManagement/
├── Domain/
│   ├── ValueObjects/
│   │   ├── CustomerId.cs
│   │   ├── CustomerName.cs
│   │   └── Email.cs
│   ├── Customer.cs
│   ├── ICustomerRepository.cs
│   └── Specifications/
│       ├── CustomerEmailSpec.cs          # ExpressionSpecification
│       ├── CustomerNameContainsSpec.cs   # ExpressionSpecification
│       └── CustomerActiveSpec.cs         # non-Expression Specification
├── Infrastructure/
│   └── InMemoryCustomerRepository.cs
├── SampleCustomers.cs
└── Program.cs
```

### Specification List

| Specification | Base Class | Description |
|---------------|------------|-------------|
| `CustomerEmailSpec` | ExpressionSpecification | Exact email match |
| `CustomerNameContainsSpec` | ExpressionSpecification | Partial name match (case-insensitive) |
| `CustomerActiveSpec` | Specification | Active customer check |

## At a Glance

| Aspect | Details |
|--------|---------|
| **Domain** | Customer management (Customer) |
| **Value Objects** | CustomerId, CustomerName, Email |
| **ExpressionSpecification** | CustomerEmailSpec, CustomerNameContainsSpec |
| **non-Expression Specification** | CustomerActiveSpec |
| **Core pattern** | Mixed Expression/non-Expression composition |

### Expression vs non-Expression Selection Criteria

| Criteria | ExpressionSpecification | Specification |
|----------|------------------------|---------------|
| **EF Core SQL translation** | Supported | Not supported |
| **Implementation complexity** | Higher (Expression Tree) | Lower (direct logic) |
| **Usage scenario** | DB query filtering | In-memory validation |
| **Value Object handling** | Implicit conversion required | Direct access possible |

## FAQ

### Q1: When should I use a plain Specification instead of ExpressionSpecification?
**A**: If SQL translation through an ORM like EF Core is not needed and the condition is simple enough to verify only in memory, a plain Specification is more concise. `CustomerActiveSpec`, which is a simple property check, is a typical example.

### Q2: Why can Expression and non-Expression Specifications be composed together?
**A**: Both inherit from the `Specification<T>` base class, so they can be freely composed through the `&`, `|`, `!` operators. The composed result is evaluated in memory via `IsSatisfiedBy`.

### Q3: Why can't StringComparison be used in Expression Trees?
**A**: Expression Trees must be translatable to SQL and similar formats, so .NET-specific APIs like `StringComparison` cannot be translated. Using the `.ToLower().Contains()` pattern instead naturally translates to SQL's `LOWER()` function.

---

This concludes the main content of the Specification pattern tutorial. From fundamentals to real-world application -- we've covered condition encapsulation, Expression Trees, Repository integration, and application across various domains. The appendix provides comparisons with alternative patterns, anti-patterns, a glossary, and references.

→ [Appendix A: Specification vs Alternatives](../../Appendix/A-specification-vs-alternatives.md)
