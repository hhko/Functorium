---
title: "FAQ"
---

A collection of frequently asked questions and answers that arise when adopting architecture tests. Organized by category covering ClassValidator, MethodValidator, custom rules, and troubleshooting, so you can look things up when you get stuck.

## General

### Q: Can architecture tests replace unit tests?

**No.** Architecture tests and unit tests verify different concerns. Architecture tests verify the **structure** of code, while unit tests verify the **behavior** of code. Use both types of tests together.

### Q: Should architecture tests be included in CI/CD?

**Yes, absolutely.** The greatest value of architecture tests is automated verification. Since they are included in `dotnet test`, they run in the CI pipeline without additional configuration.

### Q: How much do architecture tests affect build time?

The most costly part is `ArchLoader`'s assembly loading. With `static readonly` caching, it typically takes **a few hundred milliseconds**. It does not significantly affect unit test execution time.

## ClassValidator

### Q: Does `RequireSealed()` fail for abstract classes?

**Yes.** Abstract classes cannot be sealed, so it fails. Exclude abstract base classes from the filter:

```csharp
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace(DomainNamespace)
    .And().AreNotAbstract()  // Exclude abstract classes
    .ValidateAllClasses(Architecture, @class => @class
        .RequireSealed())
    .ThrowIfAnyFailures("Rule");
```

### Q: What happens when `RequireSealed()` is applied to a static class?

C# static classes are compiled as `abstract sealed` at the IL level. Using `RequireStatic()` correctly detects this. Use `RequireStatic()` for static classes and `RequireSealed()` for regular classes.

### Q: What are the 6 dimensions that `RequireImmutable()` verifies?

| Dimension | Verification Content | Violation Example |
|-----------|---------------------|-------------------|
| Writability | Non-static members are immutable | `public int X { get; set; }` |
| Constructors | No public constructors | `public MyClass() { }` |
| Properties | No public setters | `public string Name { get; set; }` |
| Fields | No public non-static fields | `public int Count;` |
| Collections | Mutable collections prohibited | `public List<int> Items { get; }` |
| Methods | Only allowed-list methods | `public void Mutate() { }` |

## MethodValidator

### Q: How does open generic matching work with `RequireReturnType(typeof(Fin<>))`?

`typeof(Fin<>)` is an open generic type. MethodValidator extracts the generic definition of the return type and compares them. For example, the generic definition of `Fin<Email>` is `Fin<>`, so it matches.

### Q: What is the difference between `RequireMethod` and `RequireMethodIfExists`?

- **`RequireMethod`:** The method must exist. Its absence is a violation.
- **`RequireMethodIfExists`:** Verifies only when the method exists. Its absence is not a violation.

Use `RequireMethodIfExists` for optional methods (e.g., a specific method inside a Validator nested class).

### Q: How do you verify extension methods?

```csharp
@class.RequireAllMethods(
    m => m.IsStatic == true,  // Filter to static methods only
    m => m.RequireExtensionMethod());
```

`RequireExtensionMethod()` verifies the presence of `[ExtensionAttribute]`.

## Custom Rules

### Q: Should I use `DelegateArchRule` or directly implement `IArchRule`?

| Scenario | Recommendation |
|----------|---------------|
| Simple single condition | `DelegateArchRule` |
| Complex logic (multiple methods) | Direct `IArchRule` implementation |
| State is needed | Direct `IArchRule` implementation |
| Quick prototyping | `DelegateArchRule` |

### Q: Is `CompositeArchRule` AND logic or OR logic?

**AND logic.** It collects violations from all sub-rules. If you need OR logic, implement it directly with `DelegateArchRule`.

## Troubleshooting

### Q: `ArchLoader` cannot find the assembly

Verify that the test project references the target project with `ProjectReference`. The assembly is generated only after a successful build.

```xml
<ProjectReference Include="..\TargetProject\TargetProject.csproj" />
```

### Q: The test raises a "No types found" error

1. Verify that the namespace string is correct
2. If you used a hardcoded string instead of `typeof().Namespace!`, check for typos
3. Verify that target classes actually exist in that namespace

### Q: `RequireImplements` does not recognize a generic interface

Use `RequireImplements(typeof(IRepository<Order>))` for closed generic interfaces (e.g., `IRepository<Order>`), and `RequireImplementsGenericInterface("IRepository")` for open generics (e.g., `IRepository<>`).

### Q: Record type fails `RequireImmutable()` verification

C# positional records generate properties with `init` setters by default. `init` setters may be considered setters and affect immutability verification. For domain records, use private constructors and factory methods.
