---
title: "Architecture Test Suites"
---

## Overview

In Parts 1--4, we learned how to write architecture rules one by one. Whether an Entity is sealed, whether a ValueObject is immutable, whether a DomainService is stateless -- understanding and writing each rule is important.

But what if you had to write 21 domain rules from scratch for every real project? Functorium provides **pre-built test suites**. Just inherit an abstract class and override two properties, and verified rules are instantly applied.

> **"Understanding rules and writing rules every time are different things. Suites are the fastest way to instantly apply rules you understand."**

## Learning Objectives

### Core Learning Goals

1. **DomainArchitectureTestSuite inheritance**
   - Override only `Architecture` and `DomainNamespace` for automatic application of 21 rules
   - Includes rules for Entity, ValueObject, DomainEvent, Specification, DomainService

2. **Adding custom rules**
   - After inheriting the Suite, add project-specific unique rules as `[Fact]` methods
   - Existing Suite rules and new rules run together

3. **Customizing behavior with virtual properties**
   - `ValueObjectExcludeFromFactoryMethods`: Exclude specific ValueObjects from factory method verification
   - `DomainServiceAllowedFieldTypes`: Specify allowed field types for DomainService

### What You Will Verify Through Practice
- **DomainArchitectureTestSuite inheritance**: Verify DomainLayerRules domain code with the Suite
- **Adding custom rules**: Add AggregateRoot inheritance rules as project-specific rules

## Project Structure

```
05-Architecture-Test-Suites/
├── ArchitectureTestSuites.Tests.Unit/
│   ├── ArchitectureTestSuites.Tests.Unit.csproj   # References DomainLayerRules project
│   ├── xunit.runner.json
│   └── ArchitectureTests.cs                       # Suite inheritance tests
└── index.md
```

This chapter does not create a separate domain project. It **references the DomainLayerRules project from Part 4-01** to apply Suite-based verification to the same domain code.

## DomainArchitectureTestSuite (21 tests)

### Basic Usage

Suite inheritance takes two steps:

**Step 1**: Override abstract properties

```csharp
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader()
            .LoadAssemblies(typeof(Order).Assembly)
            .Build();

    protected override string DomainNamespace { get; } =
        typeof(Order).Namespace!;
}
```

This alone automatically runs 21 `[Fact]` tests.

### 21 Rules Automatically Applied

| Category | Test Count | Verification Content |
|----------|:---------:|---------------------|
| **Entity** | 7 | AggregateRoot/Entity -- public sealed, Create/CreateFromValidated factory, GenerateEntityId attribute, private constructors |
| **ValueObject** | 4 | public sealed + private constructors, immutability (ImmutabilityRule), Create -> `Fin<T>`, Validate -> `Validation<Error, T>` |
| **DomainEvent** | 2 | sealed record, "Event" suffix |
| **Specification** | 3 | public sealed, `Specification<T>` inheritance, domain layer location |
| **DomainService** | 5 | public sealed, stateless (no instance fields), IObservablePort dependency prohibition, public methods return `Fin<T>`, not record |

### Abstract Properties

| Property | Type | Description |
|----------|------|-------------|
| `Architecture` | `Architecture` | Assembly architecture loaded with ArchLoader |
| `DomainNamespace` | `string` | Root namespace where domain types reside |

### Virtual Properties (Customization)

| Property | Default | Description |
|----------|---------|-------------|
| `ValueObjectExcludeFromFactoryMethods` | `[]` | ValueObject types to exclude from Create/Validate factory method verification |
| `DomainServiceAllowedFieldTypes` | `[]` | Field types to allow in DomainService's `RequireNoInstanceFields` |

Customization example:

```csharp
public sealed class DomainArchTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } = ...;
    protected override string DomainNamespace { get; } = ...;

    // UnitOfMeasure is enumeration-style and has no Create/Validate
    protected override IReadOnlyList<Type> ValueObjectExcludeFromFactoryMethods =>
        [typeof(UnitOfMeasure)];

    // Allow ILogger fields in DomainService
    protected override string[] DomainServiceAllowedFieldTypes =>
        ["ILogger"];
}
```

## Adding Custom Rules

After inheriting the Suite, add project-specific rules as `[Fact]` methods. They run alongside the Suite's 21 rules.

```csharp
public sealed class DomainArchitectureRuleTests : DomainArchitectureTestSuite
{
    protected override Architecture Architecture { get; } = ...;
    protected override string DomainNamespace { get; } = ...;

    // Project-specific additional rule
    [Fact]
    public void AggregateRoot_ShouldInherit_AggregateRootBase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(AggregateRoot<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireInherits(typeof(AggregateRoot<>)),
                verbose: true)
            .ThrowIfAnyFailures("AggregateRoot Inheritance Rule");
    }
}
```

## ApplicationArchitectureTestSuite (4 tests)

A Suite that verifies the Command/Query pattern structure of the application layer.

```csharp
public sealed class ApplicationArchitectureRuleTests : ApplicationArchitectureTestSuite
{
    protected override Architecture Architecture { get; } =
        new ArchLoader()
            .LoadAssemblies(typeof(CreateOrderCommand).Assembly)
            .Build();

    protected override string ApplicationNamespace { get; } =
        "MyApp.Application";
}
```

### 4 Rules Automatically Applied

| Test | Verification Content |
|------|---------------------|
| `Command_ShouldHave_ValidatorNestedClass` | If a Command has a Validator, it must be sealed + implement `AbstractValidator` |
| `Command_ShouldHave_UsecaseNestedClass` | Command must have a Usecase, sealed + implement `ICommandUsecase` |
| `Query_ShouldHave_ValidatorNestedClass` | If a Query has a Validator, it must be sealed + implement `AbstractValidator` |
| `Query_ShouldHave_UsecaseNestedClass` | Query must have a Usecase, sealed + implement `IQueryUsecase` |

### Abstract Properties

| Property | Type | Description |
|----------|------|-------------|
| `Architecture` | `Architecture` | Assembly architecture loaded with ArchLoader |
| `ApplicationNamespace` | `string` | Root namespace where application types reside |

## Manual Rules vs Suite Comparison

| Aspect | Manual Rules (Part 4-01~04) | Suite Inheritance (This Chapter) |
|--------|---------------------------|--------------------------------|
| **Rule authoring** | Implement rules one by one | Instantly applied through inheritance |
| **Learning value** | Understand how each rule works | Rapid application to real projects |
| **Customization** | Full freedom | Virtual properties + additional `[Fact]` |
| **Maintenance** | Manual updates on framework changes | Automatic reflection on framework updates |
| **Recommended scenario** | Rule learning, special verification needs | New projects, team standard application |

## Real-World Example References

Projects where you can see how the Suite pattern is used in practice.

| Project | Path | Test Count |
|---------|------|:---------:|
| LayeredArch Host | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Architecture/` | 42+ |
| ECommerce DDD Example | `Docs.Site/src/content/docs/samples/ecommerce-ddd/Tests/ECommerce.Tests.Unit/Architecture/` | 26+ |

Both projects inherit `DomainArchitectureTestSuite` and `ApplicationArchitectureTestSuite` and add project-specific custom rules.

## FAQ

### Q1: Can Suites and manual rules be used together?
**A**: Yes, Suite inheritance and manual rule authoring can be combined in the same test project. Rules not provided by the Suite (e.g., layer dependencies, Adapter rules) are written as separate test classes. In practice, it is common to use Suites + manual rules + ArchUnitNET native rules together.

### Q2: Can specific tests in a Suite be disabled?
**A**: xUnit's `[Fact(Skip = "reason")]` cannot be applied to inherited tests. To skip specific tests, use virtual properties. For example, exclude specific ValueObjects with `ValueObjectExcludeFromFactoryMethods` or specify allowed field types with `DomainServiceAllowedFieldTypes`.

### Q3: Does using Suites make learning Parts 1--4 unnecessary?
**A**: No. Suites are tools for rapidly applying verified rules, but understanding how each API works is essential for diagnosing the cause when a rule is violated. Learning Parts 1--4 is essential regardless of whether Suites are used.

### Q4: In what order should architecture tests be introduced to a new project?
**A**: 1) Inherit `DomainArchitectureTestSuite` for instant domain rule application, 2) Inherit `ApplicationArchitectureTestSuite` for application rules, 3) Add project-specific custom rules, 4) Add layer dependency rules with ArchUnitNET native API. Starting with Suites secures maximum verification with minimal code.

---

Using Suites significantly reduces the cost of introducing architecture rules to new projects. Understanding the rules (Parts 1--4), instantly applying them with Suites (this chapter), and extending as needed is the most effective pattern in practice.

-> [Part 5 Ch 1: Best Practices](../../Part5-Conclusion/01-best-practices.md)
