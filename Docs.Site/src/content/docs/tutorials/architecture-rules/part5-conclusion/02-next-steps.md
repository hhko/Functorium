---
title: "Next Steps"
---

## Overview

In this tutorial, we learned how to express architecture rules as code and automatically verify them using ArchUnitNET and Functorium's Validator extensions. But this is just the starting point. Architecture tests realize their true value when combined with other design patterns such as domain modeling, CQRS, and the Specification pattern.

> **"Architecture tests engrave the team's design agreements into code. As agreements grow, the tests grow with them."**

## Related Tutorials

You can deepen your learning of patterns related to architecture tests through other tutorials in the Functorium project:

| Tutorial | Related Content |
|----------|----------------|
| [Implementing Success-Driven Value Objects with Functional Programming](../functional-valueobject/) | ValueObject pattern and `Fin<T>` return types |
| [Implementing CQRS Repository & Query Patterns](../cqrs-repository/) | Entity, Repository, Command/Query patterns |
| [Implementing the Specification Pattern](../specification-pattern/) | Domain layer Specification pattern |
| [Designing a Type-Safe Usecase Pipeline](../usecase-pipeline/) | Application layer Usecase structure |

## Framework Extension

### Custom IArchRule Implementation

For rules too complex to express with `DelegateArchRule`, directly implement `IArchRule<T>`:

```csharp
public sealed class NoDuplicatePropertyNamesRule : IArchRule<Class>
{
    public string Description => "No duplicate property names across class hierarchy";

    public IReadOnlyList<RuleViolation> Validate(Class target, Architecture architecture)
    {
        var propertyNames = target.GetPropertyMembers()
            .Select(p => p.Name)
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (propertyNames.Count == 0)
            return [];

        return [new RuleViolation(
            target.FullName,
            nameof(NoDuplicatePropertyNamesRule),
            $"Duplicate properties: {string.Join(", ", propertyNames)}")];
    }
}
```

### Building a Rule Library

By packaging frequently used rule combinations into a library, the same architecture standards can be instantly applied in new projects:

```csharp
// Team-wide common rule collection
public static class TeamArchRules
{
    public static readonly CompositeArchRule<Class> EntityRule = new(
        new ImmutabilityRule(),
        new NoDuplicatePropertyNamesRule(),
        FactoryMethodRule);

    private static readonly DelegateArchRule<Class> FactoryMethodRule = new(
        "Requires Create factory method",
        (target, _) => { /* ... */ });
}
```

## References

### Official Documentation

| Resource | Link |
|----------|------|
| ArchUnitNET GitHub | https://github.com/TngTech/ArchUnitNET |
| ArchUnitNET Wiki | https://github.com/TngTech/ArchUnitNET/wiki |
| xUnit.net v3 | https://xunit.net/ |

### Related Books and Articles

| Resource | Description |
|----------|-------------|
| *Clean Architecture* (Robert C. Martin) | Principles of layered architecture |
| *Domain-Driven Design* (Eric Evans) | The original source of DDD tactical patterns |
| *Implementing Domain-Driven Design* (Vaughn Vernon) | Practical implementation of DDD patterns |

### Functorium Project

| Resource | Path |
|----------|------|
| ArchitectureRules Source Code | `Src/Functorium.Testing/Assertions/ArchitectureRules/` |
| Pre-Built Test Suites | [Part 4-05 Architecture Test Suites](../Part4-Real-World-Patterns/05-Architecture-Test-Suites/) |
| Real-World Architecture Test Examples (42) | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Architecture/` |
| API Reference | [Appendix A](../Appendix/A-api-reference.md) |

For more detailed API usage, see [Appendix A: API Reference](../Appendix/A-api-reference.md). For ArchUnitNET core patterns, see [Appendix B: ArchUnitNET Cheat Sheet](../Appendix/B-archunitnet-cheatsheet.md). For frequently asked questions, see [Appendix C: FAQ](../Appendix/C-faq.md).

## FAQ

### Q1: What rule should I start with when introducing architecture tests for the first time?
**A**: Start with a single layer dependency rule. For example, adding the most basic rule like "the Domain layer must not depend on Infrastructure" reduces code review burden while automatically detecting architecture violations.

### Q2: When should DelegateArchRule vs direct IArchRule<T> implementation be distinguished?
**A**: `DelegateArchRule` is sufficient for simple condition checks. When complex logic like class hierarchy traversal or attribute grouping is needed, directly implementing `IArchRule<T>` is advantageous in terms of readability and reusability.

### Q3: What should I watch out for when integrating architecture tests into CI?
**A**: Architecture tests run with `dotnet test` just like regular unit tests, so they can be integrated into CI pipelines without additional configuration. However, as the number of rules increases, execution time may grow, so dividing rules into logical groups and running them in parallel is recommended.

### Q4: How do other Functorium tutorials connect with architecture tests?
**A**: Layer boundaries like `IRepository` and `IQueryPort` learned in the CQRS tutorial can be enforced with architecture tests, and the domain layer location of Specification patterns can also be verified. Architecture tests are a safeguard that automatically checks whether other patterns are being used in their intended locations.

---

## Wrapping Up

The patterns learned in this tutorial are a starting point. The true power of architecture tests lies in expressing the team's agreed-upon design principles as code and having CI automatically verify them on every commit. Start with a single small rule. Even one layer dependency rule alone can reduce code review burden and give the team confidence that the architecture is being maintained as intended.

---

The appendices provide a complete API reference, ArchUnitNET cheat sheet, and frequently asked questions.

-> [Appendix A: API Reference](../Appendix/A-api-reference.md)
