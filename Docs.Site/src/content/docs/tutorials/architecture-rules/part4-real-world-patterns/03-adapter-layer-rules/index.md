---
title: "Adapter Layer Rules"
---

## Overview

Port interfaces are the contracts through which the domain communicates with the outside world. But if an adapter implementation does not implement the port, or a port interface is missing the `I` prefix, or the domain directly depends on the adapter -- the fundamentals of Hexagonal Architecture collapse.

In this chapter, you will learn how to use Functorium's `InterfaceValidator` together with ArchUnitNET's layer dependency rules to **automatically verify the structural relationship between port interfaces and adapter implementations**.

> **"The relationship between ports and adapters should not exist only in architecture diagrams. Tests must enforce this relationship at the code level so that diagrams and actual code remain in sync."**

## Learning Objectives

### Core Learning Goals

1. **Verifying port interface rules with `ValidateAllInterfaces`**
   - Enforce naming rules with `RequireNameStartsWith("I")`
   - Filter based on `Domains.Ports` namespace

2. **Verifying inter-layer dependencies with ArchUnitNET's `NotDependOnAnyTypesThat`**
   - Automatically verify that the domain does not depend on adapters
   - Test failure on rule violation with `.Check(Architecture)`

3. **Ensuring extensibility of port implementations with `RequireVirtual()`**
   - Enforce virtual methods on `IObservablePort` implementations for decorator pattern support
   - Combining `RequireNotSealed()` and `RequireVirtual()`

4. **Role division between Functorium API and ArchUnitNET native API**
   - Functorium: type internal structure verification (naming, members, immutability)
   - ArchUnitNET: inter-type relationship verification (dependencies, inheritance)

### What You Will Verify Through Practice
- **IOrderRepository, INotificationService**: Port interface `I` prefix verification
- **OrderRepository**: `IObservablePort` implementation virtual methods, not sealed verification
- **Layer dependency**: Domain -> Adapter dependency prohibition verification

## Domain Code Structure

```
Domains/
├── Order.cs
└── Ports/
    ├── IObservablePort.cs        # Observability marker interface
    ├── IOrderRepository.cs       # Port interface
    └── INotificationService.cs   # Port interface
Adapters/
├── Persistence/
│   └── OrderRepository.cs        # IObservablePort implementation (non-sealed, virtual)
└── Infrastructure/
    └── EmailNotificationService.cs  # Adapter implementation (sealed)
```

**Ports** are interfaces through which the domain communicates with the outside world. They reside in the `Domains.Ports` namespace.

**Adapters** are concrete implementations of ports. They reside in `Adapters` sub-namespaces and must implement port interfaces.

## Test Code Walkthrough

### Port Interface Naming Rules

Using `ValidateAllInterfaces` and `RequireNameStartsWith` to verify that all ports have the `I` prefix:

```csharp
Interfaces()
    .That()
    .ResideInNamespace(PortNamespace)
    .ValidateAllInterfaces(Architecture, @interface => @interface
        .RequireNameStartsWith("I"),
        verbose: true)
    .ThrowIfAnyFailures("Port Interface Naming Rule");
```

### Layer Dependency Rules

Using ArchUnitNET's native API to verify that the domain does not depend on adapters:

```csharp
using static ArchUnitNET.Fluent.ArchRuleDefinition;

Types()
    .That()
    .ResideInNamespace(DomainNamespace)
    .Should()
    .NotDependOnAnyTypesThat()
    .ResideInNamespace(AdapterNamespace)
    .Check(Architecture);
```

`.Check(Architecture)` is an extension method provided by the ArchUnitNET xUnitV3 package that fails the xUnit test on rule violation.

## Ensuring Port Implementation Extensibility

### Why Virtual Methods Are Needed

In the Observability pattern, adapters are **wrapped with decorators** to transparently add logging, metrics, and tracing. For the decorator to override the original adapter's methods, those methods must be `virtual`.

Adapters that implement the `IObservablePort` marker interface must not be sealed, and all methods must be virtual:

```csharp
// Adapters implementing IObservablePort support the decorator pattern
public class OrderRepository : IOrderRepository, IObservablePort
{
    public virtual Task<Order?> GetByIdAsync(string id) => ...;
    public virtual Task SaveAsync(Order order) => ...;
}
```

### RequireVirtual Test

Combining `RequireNotSealed()` and `RequireVirtual()` to enforce decorator pattern support:

```csharp
[Fact]
public void ObservablePortAdapters_ShouldHave_VirtualMethods()
{
    ArchRuleDefinition.Classes()
        .That()
        .ImplementInterface(typeof(IObservablePort))
        .And().AreNotAbstract()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNotSealed()
            .RequireAllMethods(method => method
                .RequireVirtual()),
            verbose: true)
        .ThrowIfAnyFailures("Observable Port Adapter Virtual Methods Rule");
}
```

Simple adapters that do not implement `IObservablePort` (`EmailNotificationService`) remain sealed. The sealed/non-sealed distinction is determined by whether decorator pattern support is needed.

## Summary at a Glance

The following table compares the tools and rules for each adapter layer verification target.

### Adapter Layer Verification Rules

| Target | Verification Tool | Verification Rule | Core Intent |
|--------|------------------|-------------------|-------------|
| **Port Interface** | Functorium `ValidateAllInterfaces` | `I` prefix naming | Unify naming convention |
| **Adapter** | Functorium `ValidateAllClasses` | public | Unify implementation structure |
| **Observable Port Adapter** | Functorium `ValidateAllClasses` | not sealed, virtual methods | Decorator pattern support |
| **Layer Dependency** | ArchUnitNET `.Check()` | Domain -> Adapter dependency prohibition | Ensure dependency inversion |

The following table organizes the role division between the two tools.

### Functorium vs ArchUnitNET Role Division

| Verification Type | Suitable Tool | Example |
|-------------------|---------------|---------|
| **Type internal structure** | Functorium | sealed, immutable, naming, member verification |
| **Inter-type relationships** | ArchUnitNET native API | Dependency direction, inheritance relationships |
| **Compound verification** | Both tools combined | Structure + dependency simultaneous verification |

## FAQ

### Q1: Why use both Functorium's `ValidateAllInterfaces` and ArchUnitNET's native API?
**A**: Functorium specializes in verifying type internal structure (naming, members, immutability), while ArchUnitNET's native API specializes in verifying inter-type dependency relationships. The adapter layer needs both, so they are used together.

### Q2: What is the difference between `.Check(Architecture)` and `.ThrowIfAnyFailures()`?
**A**: `.Check(Architecture)` is ArchUnitNET native API's verification execution method. `.ThrowIfAnyFailures()` is the termination method for Functorium's `ValidateAllClasses`/`ValidateAllInterfaces` chain. Each is used within its own API chain.

### Q3: Can naming rules other than the `I` prefix be applied to port interfaces?
**A**: Yes, you can additionally chain `RequireNameEndsWith("Repository")` or `RequireNameContains("Service")`. Port naming rules can be further refined based on their roles.

### Q4: Why are IObservablePort implementations not sealed?
**A**: To support the decorator pattern. For the Observability layer to transparently add logging, metrics, and tracing by wrapping the original adapter, it must be able to override the original methods. Sealed classes cannot be inherited, and non-virtual methods cannot be overridden, so IObservablePort implementations enforce extensibility with `RequireNotSealed()` and `RequireVirtual()`.

### Q5: Can an adapter depend on another adapter?
**A**: Direct dependency between adapters is generally not recommended. However, since they are technically in the same layer, ArchUnitNET rules do not prohibit it. If needed, you can restrict it with `Types().That().ResideInNamespace(AdapterNamespace).Should().NotDependOnAnyTypesThat().ResideInNamespace(OtherAdapterNamespace)`.

---

By enforcing the relationship between ports and adapters through tests, the dependency inversion principle of Hexagonal Architecture is guaranteed at the code level. The next chapter examines how to comprehensively verify dependency directions across all layers.

-> [Ch 4: Layer Dependency Rules](../04-Layer-Dependency-Rules/)
