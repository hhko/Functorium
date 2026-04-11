---
title: "Why Architecture Testing"
---

## What the Compiler Cannot Verify

The C# compiler catches type safety violations, access modifier errors, and syntax mistakes. However, **design rules agreed upon by the team** fall outside the compiler's scope.

For example, consider the following rules:

- "Domain entities must be `sealed` classes"
- "Value Objects must be immutable"
- "The domain layer must not depend on the infrastructure layer"
- "All Commands must have nested Request and Response classes"

These rules can easily be violated even in code that compiles without errors.

> **"The compiler verifies whether code runs, but it does not verify whether code is correctly designed."**

## Limitations of Manual Verification

Most teams verify these rules through **code reviews**. However, manual verification has fundamental limitations.

Imagine it is Friday afternoon and a PR with 100 changed files has been submitted. The reviewer must check one by one whether the newly added `OrderItem` class is `sealed`, whether it has a `Create` factory method, and whether immutability is maintained. After about 50 files, fatigue sets in and items start getting missed.

| Problem | Description |
|---------|-------------|
| **Lack of consistency** | Different reviewers apply different standards, and items missed vary with fatigue levels |
| **Scalability limits** | As the codebase grows, checking all rules becomes increasingly difficult |
| **Delayed feedback** | Violations are only discovered at the PR review stage, raising the cost of fixes |
| **Implicit knowledge** | If rules are not documented, new team members easily violate them |

## The Value of Architecture Testing

**Architecture tests** express design rules as executable code. This enables:

1. **Automated verification:** Rule violations are detected on every commit in the CI/CD pipeline
2. **Immediate feedback:** Developers learn about violations at the time they write code
3. **Living documentation:** The test code itself serves as the architecture rule specification
4. **Consistent standards:** Verification applies the same criteria regardless of the reviewer's condition

```csharp
// This is not a rule document. It is an executable verification.
[Fact]
public void DomainClasses_ShouldBe_PublicAndSealed()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequirePublic()
            .RequireSealed(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Class Visibility Rule");
}
```

> **"Architecture tests turn your team's design agreements into automated verification. When rules are expressed as code, violations surface immediately -- just like compilation errors."**

## Architecture Tests vs Unit Tests

| Aspect | Unit Tests | Architecture Tests |
|--------|------------|-------------------|
| **Verification target** | Business logic behavior | Code structure and design rules |
| **Verification timing** | Runtime behavior | Structure of compiled assemblies |
| **Failure cause** | Incorrect logic | Rule violations (e.g., missing sealed) |
| **Maintenance** | Modified when requirements change | Modified when architecture rules change |

The two types of tests are complementary. If unit tests verify "does the code behave correctly?", architecture tests verify "is the code structured correctly?".

## FAQ

### Q1: Do architecture tests replace unit tests?
**A**: No. Architecture tests verify **the structure of code**, while unit tests verify **the behavior of code**. If an architecture test checks whether the `Employee` class is `sealed`, a unit test verifies whether `Employee.Create()` returns the correct result. Both types of tests are needed to trust both structure and behavior.

### Q2: Are architecture tests necessary for small projects?
**A**: The criterion is **the importance of the rules**, not the team size or codebase size. Even in small projects, if there are core rules like "domain entities must be sealed", automating them with architecture tests is effective. As the project grows, their value increases further.

### Q3: How are architecture tests run in CI/CD?
**A**: Architecture tests run in the same way as regular xUnit tests. They are automatically executed in the CI/CD pipeline by the `dotnet test` command, requiring no additional tools or configuration.

### Q4: What happens when an architecture rule is violated?
**A**: The class that violated the rule and the violation details are output in detail in the test failure message. Developers can run tests locally to identify and fix violations before submitting a PR.

---

## Next Steps

To automatically verify design rules, appropriate tools are needed. The next chapter introduces ArchUnitNET and the Functorium ArchitectureRules framework -- the architecture testing tools for the .NET ecosystem.

-> [0.2 Introducing ArchUnitNET](02-archunitnet-and-functorium.md)
