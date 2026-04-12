---
title: "References"
---
Reference materials for deeper learning.

## Official Documentation

### Functorium
- **GitHub**: https://github.com/hhko/Functorium
- **Specification Types**: `Src/Functorium/Domains/Specifications/`

### .NET Expression Trees
- **Official Docs**: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/
- **Expression Trees Advanced**: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/expression-trees-building

### ArchUnitNET
- **GitHub**: https://github.com/TNG/ArchUnitNET
- **NuGet**: https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnit

---

## References

### Domain-Driven Design
- **Title**: Domain-Driven Design: Tackling Complexity in the Heart of Software
- **Author**: Eric Evans
- **Publisher**: Addison-Wesley, 2003
- **Key Content**: Foundational definition of the Specification pattern, Repository pattern

### Implementing Domain-Driven Design
- **Title**: Implementing Domain-Driven Design
- **Author**: Vaughn Vernon
- **Publisher**: Addison-Wesley, 2013
- **Key Content**: Practical DDD implementation patterns, Specification usage examples

### Patterns of Enterprise Application Architecture
- **Title**: Patterns of Enterprise Application Architecture
- **Author**: Martin Fowler
- **Publisher**: Addison-Wesley, 2002
- **Key Content**: Systematic definition of Specification pattern and Repository pattern

### Functional Programming in C#
- **Title**: Functional Programming in C#: How to write better C# code
- **Author**: Enrico Buonanno
- **Publisher**: Manning, 2022 (2nd Edition)
- **Key Content**: Expression Trees, functional composition patterns

---

## Online Resources

### Blogs & Articles

**Specification Pattern**
- https://www.martinfowler.com/apsupp/spec.pdf
- Original Specification pattern paper by Eric Evans and Martin Fowler

**Specification Pattern in DDD**
- https://enterprisecraftsmanship.com/posts/specification-pattern-always-valid-domain-model/
- Vladimir Khorikov on the Specification pattern and always-valid domain models

**Expression Trees in C#**
- https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/expression-trees/
- Official .NET Expression Tree guide

---

## Related Libraries

### Specification Pattern

| Library | Description | NuGet |
|-----------|------|-------|
| Functorium | Specification framework used in this tutorial | ✅ |
| Ardalis.Specification | Specification library by Steve Smith | ✅ |

### DDD & Clean Architecture

| Library | Description | NuGet |
|-----------|------|-------|
| MediatR | CQRS/Mediator pattern | ✅ |
| FluentValidation | Validation library | ✅ |
| LanguageExt.Core | Functional programming types | ✅ |

### ORM

| Library | Description | NuGet |
|-----------|------|-------|
| Microsoft.EntityFrameworkCore | EF Core ORM | ✅ |
| Microsoft.EntityFrameworkCore.InMemory | InMemory for testing | ✅ |

### Testing

| Library | Description | NuGet |
|-----------|------|-------|
| ArchUnitNET | Architecture testing | ✅ |
| Shouldly | Assertion library | ✅ |
| xUnit | Testing framework | ✅ |
| NSubstitute | Mocking library | ✅ |

---

## Functorium Project Reference

### Source Code Location

```
Src/Functorium/Domains/Specifications/
├── Specification.cs                       # Abstract base class
├── IExpressionSpec.cs                     # Expression interface
├── ExpressionSpecification.cs             # Expression Tree support
├── AllSpecification.cs                    # Identity element (internal sealed)
├── AndSpecification.cs                    # AND composition (internal sealed)
├── OrSpecification.cs                     # OR composition (internal sealed)
├── NotSpecification.cs                    # NOT negation (internal sealed)
└── Expressions/
    ├── SpecificationExpressionResolver.cs # Expression synthesis utility
    └── PropertyMap.cs                     # Entity->Model conversion
```

### Tutorial Projects

```
Docs.Site/src/content/docs/tutorials/specification-pattern/
├── Part1-Specification-Basics/            # Basics (4 chapters)
├── Part2-Expression-Specification/        # Expression (4 chapters)
├── Part3-Repository-Integration/          # Repository Integration (4 chapters)
├── Part4-Real-World-Patterns/             # Real-World Patterns (4 chapters)
└── Part5-Domain-Examples/                 # Domain Examples (2 chapters)
```

---

## Recommended Learning Path

### Beginner (Pattern Introduction)
1. Specification chapter in Eric Evans' DDD book
2. This tutorial Part 1 (Chapters 1-4)
3. Read the Functorium Specification source code

### Intermediate (Practical Application)
1. .NET Expression Trees official documentation
2. This tutorial Parts 2-3
3. Comparative analysis with Ardalis.Specification

### Advanced (Architecture Design)
1. "Implementing Domain-Driven Design" book
2. This tutorial Parts 4-5
3. Writing architecture tests with ArchUnitNET

---

This tutorial was written based on the actual development experience of the Functorium project's Specification framework.
