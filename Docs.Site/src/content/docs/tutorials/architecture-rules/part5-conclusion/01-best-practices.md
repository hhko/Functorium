---
title: "Best Practices"
---

## Overview

Once architecture rules are created, the next challenge is for the entire team to operate them continuously. If rules do not grow with the code, they gradually degrade into "always-failing tests" that get ignored over time. In this chapter, we cover how to keep architecture tests alive in practice -- from rule design, test organization, and performance to team adoption strategy.

> **"Good architecture rules are ones where the violation message alone tells you what went wrong."**

## Start with Suite Inheritance

When introducing architecture tests in a new project, start by inheriting `DomainArchitectureTestSuite` and `ApplicationArchitectureTestSuite`. Overriding just two properties instantly applies 25 verified rules. Project-specific rules are added as `[Fact]` methods on top of the Suite.

```csharp
// 1. Instantly secure 25 rules through Suite inheritance
public sealed class DomainArchTests : DomainArchitectureTestSuite { ... }
public sealed class AppArchTests : ApplicationArchitectureTestSuite { ... }

// 2. Add project-specific rules
// 3. Add layer dependency rules with ArchUnitNET native API
```

For detailed usage, see [Part 4-05 Architecture Test Suites](../Part4-Real-World-Patterns/05-Architecture-Test-Suites/).

## Rule Design Principles

### Rules Must Have Clear Names

The rule name passed to `ThrowIfAnyFailures` should make it immediately clear what went wrong when a violation occurs. Ambiguous names shift additional investigation costs to the developer fixing the violation.

```csharp
// Good: Rule intent is clear
.ThrowIfAnyFailures("ValueObject Immutability Rule");
.ThrowIfAnyFailures("Entity Factory Method Rule");

// Bad: Unclear what is being verified
.ThrowIfAnyFailures("Rule1");
.ThrowIfAnyFailures("Check");
```

### One Concern Per Test

Bundle related rules into a single Validator chain, but separate different concerns into individual tests. Mixing concerns means when one fails, you cannot check the rest.

```csharp
// Good: "Visibility" concern as one test
[Fact]
public void Entity_ShouldBe_PublicSealed()
{
    // RequirePublic() + RequireSealed() = visibility concern
}

// Good: "Factory method" concern as a separate test
[Fact]
public void Entity_ShouldHave_CreateFactoryMethod()
{
    // RequireMethod("Create", ...) = factory method concern
}
```

### Use Verbose Mode

Using `verbose: true` provides detailed debugging information when violations occur. Always enable it during development, and it is recommended to keep it active even after stabilization. The overhead of verbose mode is negligible, and it significantly reduces the time spent tracking down violation causes.

## Test Organization Patterns

### ArchitectureTestBase Pattern

Extract the `Architecture` object and namespace strings commonly used across all architecture tests into a base class. This eliminates duplication of assembly loading code and requires modifying only one place when namespaces change.

```csharp
public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(
                typeof(Domain.AssemblyReference).Assembly,
                typeof(Application.AssemblyReference).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(Domain.AssemblyReference).Namespace!;
}
```

**Key points:**
- Declare as `static readonly` to perform assembly loading only once
- Safely extract namespace strings with `typeof().Namespace!`
- Using reflection instead of hardcoded strings detects namespace changes as compilation errors

### Test File Organization

Separate test files by layer or pattern. The file name alone should tell you what rules are contained within.

```txt
Architecture/
├── ArchitectureTestBase.cs          # Common setup
├── EntityArchitectureRuleTests.cs   # Entity rules
├── ValueObjectArchitectureRuleTests.cs  # ValueObject rules
├── UsecaseArchitectureRuleTests.cs  # Usecase rules
├── DtoArchitectureRuleTests.cs      # DTO rules
└── LayerDependencyArchitectureRuleTests.cs  # Layer dependency rules
```

### Custom Rule Reuse

Define team-wide common rules with `DelegateArchRule` or `CompositeArchRule` for reuse across multiple tests. When rules are defined in one place, changes are consistently reflected across all tests.

```csharp
// Define shared rules as static readonly fields
private static readonly DelegateArchRule<Class> s_domainNamingRule = new(
    "Forbids infrastructure suffixes",
    (target, _) => { /* verification logic */ });

private static readonly CompositeArchRule<Class> s_entityCoreRule = new(
    new ImmutabilityRule(),
    s_domainNamingRule);
```

## Performance Considerations

### ArchLoader Caching

`ArchLoader().LoadAssemblies().Build()` operates on a reflection basis and is costly. **Share the `Architecture` object across test classes:**

```csharp
// Good: Load only once with static readonly
protected static readonly Architecture Architecture = ...;

// Bad: Load fresh for every test
[Fact]
public void Test()
{
    var arch = new ArchLoader().LoadAssemblies(...).Build(); // Slow!
}
```

### Load Only Required Assemblies

Do not load assemblies you are not verifying. Unnecessary assembly loading increases startup time.

## Team Adoption Strategy

### Gradual Adoption

Introducing all rules at once invites team resistance. Start with rules that are easy to understand and have a big impact, and expand gradually after the team experiences the value.

1. **Start with layer dependency rules:** Easiest to understand and highest impact
2. **Visibility/modifier rules:** Add simple rules like `RequirePublic()`, `RequireSealed()`
3. **Naming rules:** Enforce team conventions as code
4. **Method signature rules:** Advanced rules like factory method patterns
5. **Custom rules:** Define team-specific rules with `DelegateArchRule`

### New Rule Addition Workflow

```txt
1. Discover repeated review comments in code reviews
2. Reach consensus on the rule in a team meeting
3. Implement as architecture test -> Identify violations in existing code
4. Fix violating code
5. Integrate into CI for automated verification
```

### Coexistence with Existing Code

When it is difficult to retroactively apply rules to existing code, use ArchUnitNET filtering. Apply rules only to new code and gradually fix legacy code as a separate plan.

```csharp
// Verify only classes in a specific namespace (excluding legacy)
ArchRuleDefinition.Classes()
    .That()
    .ResideInNamespace("MyApp.Domains.V2")  // New code only
    .ValidateAllClasses(Architecture, @class => @class
        .RequireImmutable())
    .ThrowIfAnyFailures("New Domain Immutability Rule");
```

## Summary at a Glance

| Area | Best Practice | Key Reason |
|------|---------------|------------|
| **Rule names** | Use clear names that describe the violation | Problem can be identified from violation message alone |
| **Separation of concerns** | One concern per test | Easy to isolate failure cause |
| **Verbose mode** | Always keep `verbose: true` | Reduces debugging time |
| **Architecture caching** | Load only once with `static readonly` | Reduces reflection cost |
| **Assembly scope** | Load only required assemblies | Minimizes startup time |
| **Custom rule reuse** | Use `DelegateArchRule`/`CompositeArchRule` | Consistent reflection on rule changes |
| **Gradual adoption** | Layer dependencies -> visibility -> naming -> advanced | Secures team acceptance |
| **Legacy coexistence** | Verify only new code with namespace filter | Maintains existing code stability |

## FAQ

### Q1: Does it become hard to manage when there are too many rules?

**A:** Structure matters more than the number of rules. By extracting common setup into `ArchitectureTestBase`, separating test files by pattern, and bundling related rules with `CompositeArchRule`, even dozens of rules can be systematically managed. If rules exceed 50, consider folder separation by category.

### Q2: What is the criterion for adding a new rule?

**A:** "Has the same comment been repeated 3 or more times in code reviews?" is a good criterion. Repeated comments signal that people are relying on memory, making it worth automating with architecture tests. Only add rules agreed upon in team meetings.

### Q3: Should architecture test failures block the build?

**A:** Yes, they must be blocked in CI. If you "just leave a warning and pass", violations accumulate and the credibility of the rules disappears. If there are many existing violations when introducing a new rule, apply it only to new code with namespace filters and gradually expand the scope.

### Q4: What if team members do not feel the value of architecture tests?

**A:** The most effective persuasion is a "prevented incident" case. Even introducing just one layer dependency rule can immediately catch infrastructure dependencies infiltrating the domain layer. Create a small success case and share it with the team.

---

The next chapter introduces related learning resources and framework extension methods.

-> [Ch 2: Next Steps](02-next-steps.md)
