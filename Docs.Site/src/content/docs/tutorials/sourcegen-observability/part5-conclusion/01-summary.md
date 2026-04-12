---
title: "Summary"
---

## Overview

Through this tutorial, we started from the question "How can we eliminate repetitive observability boilerplate?" and built a complete solution that automatically generates Logging, Tracing, and Metrics code at compile time using a Roslyn-based source generator. We implemented the entire process from the basics of navigating Syntax Trees and Semantic Models, to designing generators using the Template Method pattern, and verifying generated results with snapshot tests. This summary revisits the key points of that journey.

## Learning Objectives

### Core Learning Objectives
1. **Review core concepts**
   - Revisit the operating principles of source generators and key elements of the Roslyn API
2. **Organize design patterns**
   - Structurally review the Template Method and Strategy patterns applied in ObservablePortGenerator
3. **Verify implementation checklist**
   - Confirm the configuration and verification items that are easy to miss when introducing a source generator into a real project

---

## Core Concept Summary

### What is a Source Generator?

Writing repetitive code manually is error-prone, and Reflection-based approaches incur runtime performance costs. Source generators solve this problem by **moving it to compile time**. They participate as plugins in the Roslyn pipeline, analyzing the source code written by developers and automatically generating additional C# code.

```
Source Code → Compiler → Source Generator → Additional Code → Final Assembly
                         ↓
                    [GenerateObservablePort]
                    public class UserRepository
                         ↓
                    UserRepositoryObservable.g.cs
```

### Why Source Generators?

Manual writing involves high repetitive work and error potential, T4 templates generate code at runtime making debugging difficult, and Reflection incurs runtime performance costs. **Source generators are** the only alternative that operates at compile time while providing both type safety and IDE support.

---

## Roslyn API Essentials

### IIncrementalGenerator

```csharp
public interface IIncrementalGenerator
{
    void Initialize(IncrementalGeneratorInitializationContext context);
}
```

### Symbol Types

Roslyn's Semantic Model represents code meaning as symbols. `INamedTypeSymbol` is used to query class and interface information, `IMethodSymbol` for method signatures, and `IParameterSymbol` and `IPropertySymbol` for parameter and property information. ObservablePortGenerator combines these symbols to fully understand the original class structure before generating observability code.

### ForAttributeWithMetadataName

```csharp
context.SyntaxProvider.ForAttributeWithMetadataName(
    "Namespace.GenerateObservablePortAttribute",
    predicate: (node, _) => node is ClassDeclarationSyntax,
    transform: (ctx, _) => ExtractInfo(ctx))
```

---

## ObservablePortGenerator Design

The core design challenge of source generators is "generating consistent observability code for various classes while accurately reflecting each class's unique structure." To achieve this, we combined the Template Method pattern and the Strategy pattern.

### Template Method Pattern

```csharp
public abstract class IncrementalGeneratorBase<TValue> : IIncrementalGenerator
{
    // Template method
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = _registerSourceProvider(context);
        context.RegisterSourceOutput(provider.Collect(), _generate);
    }
}
```

### Strategy Pattern (IObservablePort)

```csharp
// Strategy interface
public interface IObservablePort
{
    string RequestCategory { get; }
}

// Each Repository is a strategy implementation
public class UserRepository : IObservablePort { }
public class OrderRepository : IObservablePort { }
```

### Generation Flow

```
1. Detect [GenerateObservablePort] attribute
2. Verify IObservablePort interface
3. Extract method signatures
4. Generate Observable class
5. Inject observability code
```

---

## Observability Code Patterns

If the design patterns answer "how to generate," this section answers "what is generated." The generated Observable class inherits from the original class while including all three observability pillars: Logging, Tracing, and Metrics.

### Generated Code Structure

```csharp
public class UserRepositoryObservable : UserRepository
{
    // 1. Fields (Logging, Tracing, Metrics)
    private readonly ActivitySource _activitySource;
    private readonly ILogger<UserRepositoryObservable> _logger;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    // 2. LoggerMessage.Define delegate
    private static readonly Action<ILogger, ...> _logAdapterRequestDebug_... = ...;

    // 3. Constructor (dependency injection)
    public UserRepositoryObservable(
        ActivitySource activitySource,
        ILogger<UserRepositoryObservable> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions) { }

    // 4. Method override (observability injection)
    public new FinT<IO, User> GetUserAsync(int id) =>
        global::LanguageExt.FinT.lift<IO, User>(
            IO.lift(() => ExecuteWithSpan(
                RequestHandler,
                nameof(GetUserAsync),
                FinTToIO(base.GetUserAsync(id)),
                () => LogGetUserAsyncRequest(id),
                LogGetUserAsyncResponseSuccess,
                LogGetUserAsyncResponseFailure)));
}
```

---

## Utility Classes

Source generators must handle diverse type signatures. Tasks such as extracting inner types from generic types, determining whether something is a collection, and resolving constructor parameter name conflicts are each separated into dedicated utility classes to manage the complexity of the generator itself.

### TypeExtractor

```csharp
// Extract T from FinT<IO, T>
TypeExtractor.ExtractSecondTypeParameter("FinT<IO, List<User>>")
// → "List<User>"
```

### CollectionTypeHelper

```csharp
// Check collection type
CollectionTypeHelper.IsCollectionType("List<User>")  // true
CollectionTypeHelper.IsTupleType("(int, string)")    // true

// Generate Count expression
CollectionTypeHelper.GetCountExpression("result", "List<User>")
// → "result?.Count ?? 0"
```

### ConstructorParameterExtractor

```csharp
// Extract constructor parameters
var parameters = ConstructorParameterExtractor.ExtractParameters(classSymbol);
```

### ParameterNameResolver

```csharp
// Resolve parameter name conflicts
ParameterNameResolver.ResolveNames(parameters);
// "logger" → "baseLogger"
```

---

## Code Generation Principles

Code produced by source generators must yield identical results with every build. Non-deterministic output creates unnecessary diffs and disrupts source control.

### Deterministic Output

Three principles are applied to **guarantee deterministic output**. First, prevent namespace conflicts with the `global::` prefix. Second, use `.OrderBy()` to always maintain consistent generation order. Third, exclude timestamps to ensure reproducible builds.

### LoggerMessage.Define Limitation

`LoggerMessage.Define` has a constraint that only supports up to 6 parameters. Therefore, when there are 6 or fewer parameters, `LoggerMessage.Define` is used, and when there are 7 or more, it falls back to `logger.LogDebug()`. The generator automatically handles this branching by analyzing the number of method parameters.

---

## Test Strategy

Since source generator output is string-based code, the key is verifying that the result exactly matches the expected output. Snapshot tests compare generated code with `.verified.txt` files to immediately detect unintended changes.

### Snapshot Tests

```csharp
[Fact]
public Task Should_Generate_ObservableClass()
{
    string? actual = _sut.Generate(input);
    return Verify(actual);  // Compare with .verified.txt
}
```

### Test Categories

| Category | Test Count |
|----------|-----------|
| Basic Generation | 1 |
| Basic Adapter | 3 |
| Parameters | 8 |
| Return Types | 6 |
| Constructors | 4 |
| Interfaces | 3 |
| Namespaces | 2 |
| Diagnostics | 4 |

> **Note**: The above 31 are generator snapshot tests from `ObservablePortGeneratorTests`. Separately, runtime Observability structure verification tests (`ObservablePortObservabilityTests`, `ObservablePortLoggingStructureTests`, `ObservablePortMetricsStructureTests`, `ObservablePortTracingStructureTests`) verify tag structure, logging field, metrics tag, and Tracing tag specification compliance.

---

## Implementation Checklist

When starting a source generator project in practice, here is a compilation of items that are easy to miss, from project setup to testing.

### Project Setup

- [ ] `netstandard2.0` target framework
- [ ] `IsRoslynComponent = true`
- [ ] `EnforceExtendedAnalyzerRules = true`
- [ ] Microsoft.CodeAnalysis.CSharp package

### Source Generator Implementation

- [ ] `IIncrementalGenerator` implementation
- [ ] `[Generator]` attribute applied
- [ ] `ForAttributeWithMetadataName` used
- [ ] Marker Attribute auto-generation

### Code Generation

- [ ] `global::` prefix usage
- [ ] `SymbolDisplayFormat` consistency
- [ ] Deterministic output guaranteed
- [ ] Namespace handling

### Testing

- [ ] `CSharpCompilation` test environment
- [ ] Verify snapshot tests
- [ ] Test coverage per scenario

---

## Key File Reference

| File | Role |
|------|------|
| `ObservablePortGenerator.cs` | Main source generator |
| `IncrementalGeneratorBase.cs` | Template Method pattern |
| `TypeExtractor.cs` | Generic type extraction |
| `CollectionTypeHelper.cs` | Collection type handling |
| `SymbolDisplayFormats.cs` | Type string format |
| `SourceGeneratorTestRunner.cs` | Test utility |

---

## FAQ

### Q1: What minimum preparation is needed to introduce ObservablePortGenerator into another project?
**A**: Three things are needed. First, configure a source generator project targeting `netstandard2.0`. Second, set up references so the target project can use the `IObservablePort` interface and `[GenerateObservablePort]` attribute. Third, prepare a test project that verifies generation results with Verify snapshot tests.

### Q2: How do you fix bugs in code generated by a source generator?
**A**: Directly modifying generated `.g.cs` files will be overwritten on the next build. Modify the source generator's code generation logic (e.g., `GenerateMethod()`), run tests, and update Verify snapshots to confirm the fix. Through snapshot diffs, you can also immediately identify whether the fix affects other scenarios.

### Q3: Can the patterns covered in this tutorial be applied to code generation methods other than source generators?
**A**: The Template Method pattern, deterministic output principle, `StringBuilder`-based code assembly, and snapshot testing patterns can be equally applied to T4 templates, text template engines like Scriban, or CLI-based code generation tools. However, compile-time execution and incremental caching are unique advantages of Roslyn source generators.

---

Having reviewed the core concepts and design patterns, it's time to look at the directions in which this knowledge can be extended.

→ [02. Next Steps](02-next-steps.md)
