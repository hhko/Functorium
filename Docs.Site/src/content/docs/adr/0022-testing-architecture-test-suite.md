---
title: "ADR-0022: Testing - Architecture Test Suite Framework (Custom Implementation)"
status: "accepted"
date: 2026-03-20
---

## Context and Problem

During PR reviews, identical comments like "this Value Object is missing sealed" and "this property's `set` should be changed to `init`" repeated weekly. Human inspection inevitably allowed rule violations to accumulate, and standards varied depending on the reviewer. An attempt to automate with ArchUnitNET proved strong for layer dependency verification, but lacked APIs to express C# type-system-level DDD tactical rules like "is this Value Object sealed?", "is this Aggregate Root's default constructor private?", or "is this property init-only?" Trying to create custom rules hit limited extension points, ultimately leading to the judgment that building from scratch was preferable to awkwardly fitting onto ArchUnitNET.

An architecture test framework was needed that can automatically enforce DDD tactical patterns (sealed, private constructor, immutability) in CI.

## Considered Options

- **Option 1**: ArchUnitNET alone
- **Option 2**: NetArchTest
- **Option 3**: Custom ClassValidator/InterfaceValidator/MethodValidator implementation + Suite inheritance pattern
- **Option 4**: Manual code review

## Decision

**Option 3: Adopt custom Validator implementation + Suite inheritance pattern.**

After actually trying ArchUnitNET and NetArchTest, both verified layer dependencies well but could not even express the simple question "is this Value Object sealed?" Direct access to fine-grained type information provided by C# reflection -- `IsSealed`, `GetConstructors()`, `PropertyInfo.SetMethod.IsInitOnly` -- was essential to accurately verify DDD tactical rules.

`ClassValidator`, `InterfaceValidator`, and `MethodValidator` are implemented from scratch, then composed into Suite classes like `DomainArchitectureTestSuite` that can be applied to project-specific architecture tests with a single line of inheritance.

Key validation rules:
- **ImmutabilityRule**: Verifies that Value Object and Entity properties are `init` or `private set`
- **SealedRule**: Verifies that Value Objects are sealed
- **PrivateConstructorRule**: Verifies that Aggregate Root default constructors are private
- **LayerDependencyRule**: Verifies that inter-layer dependency directions are correct

Decoupled from xUnit for test framework independence, simplified from the initial 3-tier (Validator -> Rule -> Suite) to 1-tier (Suite directly contains Rules).

### Consequences

- **Positive**: Repetitive PR review comments like "missing sealed" and "change to init" are now caught automatically at the CI stage and have disappeared. Adding a single class inheriting `DomainArchitectureTestSuite` to a new project immediately applies all DDD rules. Thanks to xUnit decoupling, rules themselves are reusable even when switching test frameworks.
- **Negative**: Maintenance burden for the framework itself emerges. In particular, if C# language changes occur (e.g., new access modifiers), reflection-based validation logic must be updated accordingly.

### Confirmation

- Verify that test classes inheriting DomainArchitectureTestSuite accurately detect sealed violations and immutability violations.
- Write test code that intentionally violates rules to verify there are no false negatives.
- Verify that architecture tests connect to build failures in the CI pipeline.

## Pros and Cons of the Options

### Option 1: ArchUnitNET Alone

- **Pros**: Mature open-source library with Fluent API like `Types().That().ResideInNamespace("Domain").ShouldNot().DependOn("Infrastructure")` for declarative layer dependency verification. Rich community support and documentation.
- **Cons**: No API to express "is this Value Object sealed?" Verification of C# type-system-level attributes like `IsSealed`, constructor access modifiers, and `init`-only properties is impossible. Custom rule extension API is limited, forcing DDD tactical patterns to be awkwardly fitted.

### Option 2: NetArchTest

- **Pros**: .NET-specific, integrating naturally with C# projects. Provides Fluent API at the `Types.InAssembly().That().AreClasses().Should().BeSealed()` level.
- **Cons**: While sealed verification is possible, extension points for adding DDD-specific rules like "init-only property verification" and "CRTP pattern base class inheritance verification" are insufficient. Library maintenance is not active, with slow response to latest C# features.

### Option 3: Custom Validator Implementation + Suite Inheritance Pattern

- **Pros**: Leverages all C# reflection capabilities -- `Type.IsSealed`, `PropertyInfo.SetMethod.IsInitOnly`, `ConstructorInfo.IsPrivate` -- to accurately express DDD tactical rules. A single line inheriting `DomainArchitectureTestSuite` instantly applies to new projects. Independent of xUnit, unaffected by test framework changes. Simple internal structure with 1-tier architecture.
- **Cons**: Custom implementation incurs maintenance cost. Fluent expressiveness for layer dependency verification may not match ArchUnitNET. Reflection-based, so there is slight performance cost during test execution (only runs in CI, so practical impact is minimal).

### Option 4: Manual Code Review

- **Pros**: Zero tool adoption cost. Flexible judgment based on context.
- **Cons**: Repetitive comments like "missing sealed" cause reviewer fatigue, and standards vary by reviewer. As project scale grows, the number of types to inspect increases, causing review costs to grow exponentially, with omissions discovered not in CI but in production.

## Related Information

- Commits: 7a073b9d (xUnit decoupling, ImmutabilityRule), 5af2b12b (simplification from 3-tier to 1-tier Suite)
