---
title: "Introducing ArchUnitNET"
---

## Tools for Architecture Testing

In the previous chapter, we identified the limitations of manually verifying design rules. So what tools are needed to automate these rules? The most widely used architecture testing tool in the .NET ecosystem is **ArchUnitNET.**

> **"Architecture tests analyze assemblies through reflection to automatically verify whether the structure of compiled code follows the team's rules."**

## Introducing ArchUnitNET

**ArchUnitNET** is an architecture testing library that ports Java's ArchUnit to .NET. It analyzes compiled assemblies through reflection to verify rules for types, dependencies, naming, and more.

### Core Concepts

**The Architecture object** is the result of loading the assemblies to be verified:

```csharp
using ArchUnitNET.Loader;

static readonly Architecture Architecture =
    new ArchLoader()
        .LoadAssemblies(typeof(MyDomain.Order).Assembly)
        .Build();
```

**ArchRuleDefinition** is the entry point for defining rules:

```csharp
using ArchUnitNET.Fluent;

// Class rules
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace("MyApp.Domains")
    .Should().BePublic()
    .Check(Architecture);

// Dependency rules
ArchRuleDefinition.Types()
    .That()
    .ResideInNamespace("MyApp.Domains")
    .Should().NotDependOnAnyTypesThat()
    .ResideInNamespace("MyApp.Infrastructure")
    .Check(Architecture);
```

### Limitations of ArchUnitNET

ArchUnitNET is powerful, but it is difficult to express compound rules like the following:

```txt
"All domain entities must:
  1. Be public sealed classes
  2. Be immutable
  3. Have a Create factory method
  4. That method must be static and return Fin<T>"
```

To write such rules with ArchUnitNET alone, they must be spread across multiple separate tests, or complex custom logic is required. For domain rules that need to verify multiple attributes at once, this fragmentation reduces the **cohesion** of the rules.

## Functorium ArchitectureRules Framework

**Functorium ArchitectureRules** is a fluent architecture verification system built on top of ArchUnitNET. While ArchUnitNET provides the foundation for loading assemblies and filtering types, Functorium ArchitectureRules expresses compound rules cohesively on top of it using the **Validator pattern.**

### 1. Validator Pattern

**ClassValidator/InterfaceValidator/MethodValidator** verify rules at the type level using a chaining approach:

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .ValidateAllClasses(Architecture, @class => @class
        .RequirePublic()
        .RequireSealed()
        .RequireNoPublicSetters()
        .RequireMethod("Create", m => m
            .RequireStatic()
            .RequireReturnType(typeof(Fin<>))),
        verbose: true)
    .ThrowIfAnyFailures("Domain Entity Rule");
```

Multiple rules can be expressed fluently in a single test.

### 2. Built-in Rules

Frequently used compound rules are pre-implemented and provided:

```csharp
// ImmutabilityRule: 6-dimension immutability verification
@class.RequireImmutable()

// 6 dimensions: writability, constructors, properties, fields, collections, methods
```

### 3. Custom Rule Composition

Team-specific rules can be defined with the `IArchRule<T>` interface and composed with `DelegateArchRule` and `CompositeArchRule`:

```csharp
// Define a custom rule with a lambda
var namingRule = new DelegateArchRule<Class>(
    "Forbids Dto suffix in domain",
    (target, _) => target.Name.EndsWith("Dto")
        ? [new RuleViolation(target.FullName, "NamingRule", "Dto suffix not allowed")]
        : []);

// Compose multiple rules with AND
var compositeRule = new CompositeArchRule<Class>(
    new ImmutabilityRule(),
    namingRule);

// Apply to Validator
@class.Apply(compositeRule)
```

> **"If ArchUnitNET decides 'which classes to verify', Functorium ArchitectureRules expresses 'what conditions those classes must satisfy'."**

## ArchUnitNET vs Functorium ArchitectureRules

The following table compares the roles and characteristics of the two libraries.

| Aspect | ArchUnitNET | Functorium ArchitectureRules |
|--------|------------|------------------------------|
| **Verification level** | Types / dependencies / naming | Types + method signatures + immutability |
| **Rule expression** | Should chain | Validator chaining |
| **Compound rules** | Spread across separate tests | Unified in a single Validator |
| **Custom rules** | Implement IArchRule directly | DelegateArchRule / CompositeArchRule |
| **Relationship** | Standalone library | Built on top of ArchUnitNET |

The two libraries have a **complementary relationship**, not a replacement relationship. Rules like layer dependencies are better suited to ArchUnitNET's `Should().NotDependOnAnyTypesThat()`, while internal type structure verification is better suited to Functorium ArchitectureRules.

## FAQ

### Q1: Can I use Functorium ArchitectureRules without ArchUnitNET?
**A**: No. Functorium ArchitectureRules is built on top of ArchUnitNET. Assembly loading (`ArchLoader`) and type filtering (`ArchRuleDefinition.Classes().That()...`) are handled by ArchUnitNET, and Functorium's role is to express fine-grained rules using the Validator pattern on top of that.

### Q2: Can I mix ArchUnitNET's `Should()` chain and Functorium's `ValidateAllClasses` in the same test?
**A**: Yes, you can. Write rules better suited to ArchUnitNET -- like layer dependencies -- with the `Should()` chain, and write internal type structure verification with `ValidateAllClasses`. Both approaches can coexist in the same test class.

### Q3: What advantage does Functorium's Validator pattern have over ArchUnitNET's custom rules?
**A**: The biggest difference is **cohesion.** In ArchUnitNET, expressing a "public + sealed + immutable + factory method" rule requires 4 separate tests, but in Functorium's Validator, all conditions can be seen at a glance in a single chain.

---

## Next Steps

Now that we understand the role of the tools, let's prepare the practice environment. The next chapter covers setting up the environment for writing architecture tests -- from NuGet package installation to test project configuration.

-> [0.3 Environment Setup](03-environment-setup.md)
