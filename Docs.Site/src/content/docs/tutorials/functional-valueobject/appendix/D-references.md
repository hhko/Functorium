---
title: "References"
---
## Official Documentation

### LanguageExt
- **GitHub**: https://github.com/louthy/language-ext
- **Documentation**: https://languageext.readthedocs.io/
- **NuGet**: https://www.nuget.org/packages/LanguageExt.Core

### Ardalis.SmartEnum
- **GitHub**: https://github.com/ardalis/SmartEnum
- **NuGet**: https://www.nuget.org/packages/Ardalis.SmartEnum

### ArchUnitNET
- **GitHub**: https://github.com/TNG/ArchUnitNET
- **NuGet**: https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnit

---

## References

### Domain-Driven Design
- **Title**: Domain-Driven Design: Tackling Complexity in the Heart of Software
- **Author**: Eric Evans
- **Published**: Addison-Wesley, 2003
- **Key Content**: Original definitions of value objects, entities, and aggregates

### Implementing Domain-Driven Design
- **Title**: Implementing Domain-Driven Design
- **Author**: Vaughn Vernon
- **Published**: Addison-Wesley, 2013
- **Key Content**: DDD practical implementation patterns, advanced value objects

### Functional Programming in C#
- **Title**: Functional Programming in C#: How to write better C# code
- **Author**: Enrico Buonanno
- **Published**: Manning, 2022 (2nd Edition)
- **Key Content**: `Option`, `Either`, Railway Oriented Programming

### Domain Modeling Made Functional
- **Title**: Domain Modeling Made Functional
- **Author**: Scott Wlaschin
- **Published**: Pragmatic Bookshelf, 2018
- **Key Content**: Functional DDD, type-driven design

---

## Online Resources

### Blogs & Articles

**Railway Oriented Programming**
- https://fsharpforfunandprofit.com/rop/
- Scott Wlaschin's error handling pattern series

**Value Object Patterns**
- https://enterprisecraftsmanship.com/posts/value-objects-explained/
- Vladimir Khorikov's in-depth analysis of value objects

**Always Valid Domain Model**
- https://enterprisecraftsmanship.com/posts/always-valid-domain-model/
- Designing always-valid domain models

### Videos

**NDC Conferences**
- "Functional Programming in C#" - Enrico Buonanno
- "Domain Modeling Made Functional" - Scott Wlaschin

**Pluralsight**
- "Domain-Driven Design Fundamentals"
- "Applying Functional Principles in C#"

---

## Related Libraries

### Functional Programming

Libraries that support functional patterns in C#.

| Library | Description | NuGet |
|---------|-------------|-------|
| LanguageExt.Core | Core functional types | ✅ |
| LanguageExt.Sys | Side effect management | ✅ |
| CSharpFunctionalExtensions | Lightweight Result type | ✅ |
| Optional | Simple Option implementation | ✅ |

### DDD & Clean Architecture

Libraries used for Domain-Driven Design and Clean Architecture implementation.

| Library | Description | NuGet |
|---------|-------------|-------|
| MediatR | CQRS/Mediator pattern | ✅ |
| Ardalis.Specification | Repository pattern | ✅ |
| FluentValidation | Validation library | ✅ |
| ErrorOr | Error/Result type | ✅ |

### Testing

Libraries used for writing tests and architecture verification.

| Library | Description | NuGet |
|---------|-------------|-------|
| ArchUnitNET | Architecture tests | ✅ |
| Shouldly | Assertion library | ✅ |
| xUnit | Test framework | ✅ |
| NSubstitute | Mock library | ✅ |

---

## Functorium Project Reference

### Source Code Location

```
Src/Functorium/Domains/ValueObjects/
├── IValueObject.cs                    # Marker interface
├── AbstractValueObject.cs             # Base abstract class
├── ValueObject.cs                     # Composite value object
├── SimpleValueObject.cs               # Single value wrapper
├── ComparableValueObject.cs           # Comparable composite
├── ComparableSimpleValueObject.cs     # Comparable single
└── Validations/
    ├── ValidationApplyExtensions.cs   # Tuple Apply (internal .As() handling)
    └── Typed/
        ├── ValidationRules.cs         # Type-safe validation entry point
        ├── TypedValidation.cs         # Type information wrapper
        └── TypedValidationExtensions.*.cs  # Then chaining methods

Src/Functorium/Domains/Errors/
├── DomainError.cs                     # DomainError.For<T>() helper
└── DomainErrorKind.cs                 # Error type sealed record hierarchy
```

### CQRS Integration

```
Src/Functorium/Applications/Cqrs/
├── FinExtensions.cs                   # Fin<T> → Response conversion
└── ValidationExtensions.cs            # Validation → Response conversion
```

### Tutorial Projects

```
Docs.Site/src/content/docs/tutorials/functional-valueobject/
├── Part1-ValueObject-Concepts/        # Part 1: Understanding Concepts (16)
├── Part2-Validation-Patterns/         # Part 2: Validation Patterns (6)
├── Part3-ValueObject-Patterns/        # Part 3: Value Object Patterns (9)
├── Part4-Practical-Guide/             # Part 4: Practical Guide (4)
└── Part5-Domain-Examples/             # Part 5: Domain Examples (4)
```

---

## Recommended Learning Order

### Beginners (Functional Introduction)
1. Quick Start in the official LanguageExt documentation
2. "Functional Programming in C#" book, chapters 1-5
3. This tutorial, Part 1 (Chapters 1-6)

### Intermediate (Practical Application)
1. "Domain Modeling Made Functional" book
2. This tutorial, Parts 2-3
3. Railway Oriented Programming blog

### Advanced (Architecture Design)
1. "Implementing Domain-Driven Design" book
2. This tutorial, Parts 4-5
3. Writing architecture tests with ArchUnitNET

---

## Community

### GitHub Discussions
- LanguageExt: https://github.com/louthy/language-ext/discussions
- Functorium: https://github.com/hhko/Functorium/discussions

### Stack Overflow Tags
- `languageext`
- `domain-driven-design`
- `value-objects`
- `functional-programming`

---

## Next Steps

Check the FAQ.

→ [E. FAQ](E-faq.md)
