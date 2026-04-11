---
title: "ArchUnitNET Cheat Sheet"
---

A cheat sheet for quick reference of ArchUnitNET core APIs. Frequently used patterns are organized with code examples, covering type selection, filtering, rule definition, and layer dependency verification. It also includes how to use them together with Functorium's Validator extensions, so keep it handy when writing rules.

## Basic Structure

```csharp
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
```

## Architecture Loading

```csharp
static readonly Architecture Architecture =
    new ArchLoader()
        .LoadAssemblies(
            typeof(SomeClass).Assembly,
            typeof(AnotherClass).Assembly)
        .Build();
```

## Type Selection

| Code | Description |
|------|-------------|
| `Classes()` | Select all classes |
| `Interfaces()` | Select all interfaces |
| `Types()` | Select all types |

## Filter Chain (That)

| Filter | Description |
|--------|-------------|
| `.That().ResideInNamespace(ns)` | Resides in namespace |
| `.That().DoNotResideInNamespace(ns)` | Does not reside in namespace |
| `.That().HaveNameStartingWith(prefix)` | Name starts with prefix |
| `.That().HaveNameEndingWith(suffix)` | Name ends with suffix |
| `.That().HaveNameContaining(fragment)` | Name contains string |
| `.That().ArePublic()` | Public types |
| `.That().AreInternal()` | Internal types |
| `.That().AreSealed()` | Sealed types |
| `.That().AreAbstract()` | Abstract types |
| `.That().AreNotAbstract()` | Non-abstract types |
| `.That().ImplementInterface(typeof(I))` | Implements interface |
| `.That().AreAssignableTo(typeof(T))` | Assignable to type |
| `.That().HaveAnyAttributes(typeof(A))` | Has attribute |

## Filter Combination

```csharp
// And: Add conditions
.That()
.ResideInNamespace(ns)
.And().ArePublic()
.And().AreNotAbstract()

// Or: Branch conditions
.That()
.HaveNameEndingWith("Service")
.Or().HaveNameEndingWith("Repository")
```

## Rule Chain (Should)

### Visibility/Modifiers

| Code | Description |
|------|-------------|
| `.Should().BePublic()` | Must be public |
| `.Should().BeInternal()` | Must be internal |
| `.Should().BeSealed()` | Must be sealed |
| `.Should().BeAbstract()` | Must be abstract |

### Naming

| Code | Description |
|------|-------------|
| `.Should().HaveNameStartingWith(prefix)` | Name prefix |
| `.Should().HaveNameEndingWith(suffix)` | Name suffix |
| `.Should().HaveNameContaining(fragment)` | Name contains |

### Dependencies

| Code | Description |
|------|-------------|
| `.Should().NotDependOnAnyTypesThat().ResideInNamespace(ns)` | Namespace dependency prohibition |
| `.Should().OnlyDependOnTypesThat().ResideInNamespace(ns)` | Only allowed namespaces |
| `.Should().NotHaveDependencyOtherThan(ns)` | No dependencies outside specified namespace |

### Inheritance/Implementation

| Code | Description |
|------|-------------|
| `.Should().ImplementInterface(typeof(I))` | Interface implementation required |
| `.Should().BeAssignableTo(typeof(T))` | Type assignability required |

## Rule Execution

```csharp
// ArchUnitNET default approach
rule.Check(Architecture);

// Functorium extension approach
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(ns)
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed(),
        verbose: true)
    .ThrowIfAnyFailures("Rule Name");
```

## Layer Dependency Pattern

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

// Domain -> Application dependency prohibition
[Fact]
public void DomainLayer_ShouldNotDependOn_ApplicationLayer()
{
    Types().That().ResideInNamespace(DomainNamespace)
        .Should().NotDependOnAnyTypesThat()
        .ResideInNamespace(ApplicationNamespace)
        .Check(Architecture);
}

// Domain -> Infrastructure dependency prohibition
[Fact]
public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
{
    Types().That().ResideInNamespace(DomainNamespace)
        .Should().NotDependOnAnyTypesThat()
        .ResideInNamespace(InfrastructureNamespace)
        .Check(Architecture);
}
```

## Frequently Used Combinations

### Domain Entity Filtering

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreAssignableTo(typeof(Entity<>))
    .And().AreNotAbstract()
```

### Interface Filtering

```csharp
ArchRuleDefinition.Interfaces()
    .That()
    .ResideInNamespace(PortNamespace)
    .And().HaveNameStartingWith("I")
```

### Namespace Exclusion

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().DoNotResideInNamespace(DomainNamespace + ".Ports")
```

---

The following appendix covers frequently asked questions and a troubleshooting guide for architecture test adoption.

-> [Appendix C: FAQ](C-faq.md)
