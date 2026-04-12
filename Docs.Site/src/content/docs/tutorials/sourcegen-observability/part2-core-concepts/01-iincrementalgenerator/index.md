---
title: "IIncrementalGenerator"
---

## Overview

The first question you face when building a source generator is "which interface should I implement?". Before .NET 6, `ISourceGenerator` was the only option, but its structure of reprocessing all source files on every keystroke caused serious IDE performance degradation. **IIncrementalGenerator** is the current standard interface introduced to solve this problem, allowing you to declaratively compose an incremental pipeline that only processes changed files.

## Learning Objectives

### Core Learning Objectives
1. **Understand the IIncrementalGenerator interface structure**
   - How the entire pipeline is composed through a single Initialize method
2. **Identify the key members of IncrementalGeneratorInitializationContext**
   - Role distinction between fixed code registration, source analysis, and output registration
3. **Learn the actual application pattern in ObservablePortGenerator**
   - Template method pattern through IncrementalGeneratorBase

---

## What is IIncrementalGenerator?

**IIncrementalGenerator** is the core interface for **incremental source generators** introduced in .NET 6. While the previous `ISourceGenerator` processed all files every time, `IIncrementalGenerator` selectively processes only changed files, greatly improving build performance.

```
ISourceGenerator (legacy)
========================
- Processes all source files every time
- No caching
- Slow build performance

IIncrementalGenerator (current standard)
================================
- Processes only changed files
- Automatic caching
- Fast build performance (incremental build support)
```

---

## Interface Definition

```csharp
namespace Microsoft.CodeAnalysis;

public interface IIncrementalGenerator
{
    void Initialize(IncrementalGeneratorInitializationContext context);
}
```

Very simple. You only need to implement the **Initialize** method.

---

## Minimal Implementation Example

```csharp
using Microsoft.CodeAnalysis;

[Generator(LanguageNames.CSharp)]  // <- Required attribute
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Configure the source generation pipeline here
    }
}
```

### [Generator] Attribute

```csharp
[Generator(LanguageNames.CSharp)]  // C# only
[Generator(LanguageNames.VisualBasic)]  // VB only
[Generator]  // All languages (not recommended)
```

---

## IncrementalGeneratorInitializationContext

`IncrementalGeneratorInitializationContext`, the parameter of the Initialize method, provides everything needed to configure the source generator pipeline. The members of this struct are divided into three main roles: **fixed code registration** (code that is always the same, such as Attribute definitions), **source analysis** (data extraction through SyntaxProvider), and **output registration** (actual code generation based on analysis results).

### Key Members

```csharp
public readonly struct IncrementalGeneratorInitializationContext
{
    // 1. Fixed code registration (Post-initialization)
    public void RegisterPostInitializationOutput(
        Action<IncrementalGeneratorPostInitializationContext> callback);

    // 2. Source code analysis (Syntax Provider)
    public SyntaxValueProvider SyntaxProvider { get; }

    // 3. Additional text file access
    public IncrementalValuesProvider<AdditionalText> AdditionalTextsProvider { get; }

    // 4. Compilation options access
    public IncrementalValueProvider<CompilationOptions> CompilationOptionsProvider { get; }

    // 5. Analyzer options access
    public IncrementalValueProvider<AnalyzerConfigOptionsProvider> AnalyzerConfigOptionsProvider { get; }

    // 6. Full compilation access
    public IncrementalValueProvider<Compilation> CompilationProvider { get; }

    // 7. Source output registration
    public void RegisterSourceOutput<TSource>(
        IncrementalValueProvider<TSource> source,
        Action<SourceProductionContext, TSource> action);

    public void RegisterSourceOutput<TSource>(
        IncrementalValuesProvider<TSource> source,
        Action<SourceProductionContext, TSource> action);
}
```

---

## Source Generation Pipeline Structure

```
What happens in the Initialize method
======================================

1. RegisterPostInitializationOutput
   |  Register fixed code (e.g., Attribute definitions)
   |
   v
2. Filtering with SyntaxProvider
   |  Select only nodes of interest
   |
   v
3. Data transformation
   |  Syntax -> Information needed for code generation
   |
   v
4. RegisterSourceOutput
   |  Actual code generation and output
   |
   v
5. Compiler builds including the generated code
```

---

## Basic Patterns

### Pattern 1: Generate Fixed Code Only

```csharp
[Generator(LanguageNames.CSharp)]
public class FixedCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generate always-identical code
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("Constants.g.cs", """
                namespace MyApp;

                public static class GeneratedConstants
                {
                    public const string Version = "1.0.0";
                }
                """);
        });
    }
}
```

### Pattern 2: Attribute-Based Code Generation

```csharp
[Generator(LanguageNames.CSharp)]
public class AttributeBasedGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Generate Attribute definition
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("MyAttribute.g.cs", """
                namespace MyApp;

                [System.AttributeUsage(System.AttributeTargets.Class)]
                public class GenerateAttribute : System.Attribute { }
                """);
        });

        // 2. Find classes with [Generate] attribute
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "MyApp.GenerateAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => ctx.TargetSymbol.Name);

        // 3. Generate code
        context.RegisterSourceOutput(provider, (ctx, className) =>
        {
            ctx.AddSource($"{className}.g.cs", $"""
                namespace MyApp;

                public partial class {className}
                {{
                    public void GeneratedMethod() {{ }}
                }}
                """);
        });
    }
}
```

---

## Functorium's IncrementalGeneratorBase

The Functorium project provides a base class applying the **template method pattern**:

```csharp
// IncrementalGeneratorBase.cs
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext,
         IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    //Action<IncrementalGeneratorPostInitializationContext>? registerPostInitializationSourceOutput = null,
    bool AttachDebugger = false)
    : IIncrementalGenerator
{
    protected const string ClassEntityName = "class";

    private readonly bool _attachDebugger = AttachDebugger;
    private readonly Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>> _registerSourceProvider = registerSourceProvider;
    private readonly Action<SourceProductionContext, ImmutableArray<TValue>> _generate = generate;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // Debugger attachment support only in DEBUG builds
        if (_attachDebugger && Debugger.IsAttached is false)
        {
            Debugger.Launch();
        }
#endif

        // Stage 1: Register source provider (defined by implementation) + null filtering
        IncrementalValuesProvider<TValue> provider = _registerSourceProvider(context)
            .Where(static m => m is not null);

        // Stage 2: Register code generation (defined by implementation)
        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TValue> displayValues)
    {
        _generate(context, displayValues);
    }
}
```

### Usage Example

```csharp
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePortGenerator()
    : IncrementalGeneratorBase<ObservableClassInfo>(
        RegisterSourceProvider,    // Stage 1 implementation
        Generate,                  // Stage 2 implementation
        AttachDebugger: false)
{
    private static IncrementalValuesProvider<ObservableClassInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // Attribute definition generation + class filtering
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<ObservableClassInfo> observableClasses)
    {
        // Generate Observable code for each class
    }
}
```

---

## SourceProductionContext

The context used when outputting code:

```csharp
public readonly struct SourceProductionContext
{
    // Add source code
    public void AddSource(string hintName, string source);
    public void AddSource(string hintName, SourceText sourceText);

    // Report diagnostics
    public void ReportDiagnostic(Diagnostic diagnostic);

    // Cancellation token
    public CancellationToken CancellationToken { get; }
}
```

### Considerations When Adding Sources

```csharp
// hintName: file name (including extension, must be unique)
ctx.AddSource("UserRepository.g.cs", code);

// Add prefix to prevent namespace conflicts
ctx.AddSource("Repositories.UserRepositoryObservable.g.cs", code);
```

---

## Summary at a Glance

`IIncrementalGenerator` is a simple interface requiring only a single `Initialize` method implementation, yet it supports powerful incremental builds through declarative pipelines. Remember the three-stage structure: register fixed code like Attributes with `RegisterPostInitializationOutput`, filter nodes of interest with `SyntaxProvider`, and generate actual code with `RegisterSourceOutput`.

| Component | Role |
|-----------|------|
| `IIncrementalGenerator` | Source generator interface |
| `[Generator]` | Notifies the compiler this is a generator |
| `Initialize` | Pipeline configuration |
| `RegisterPostInitializationOutput` | Fixed code generation |
| `SyntaxProvider` | Source code analysis |
| `RegisterSourceOutput` | Dynamic code generation |

---

## FAQ

### Q1: When is attribute code generated by `RegisterPostInitializationOutput` added to compilation?
**A**: It is added immediately at the Post-Initialization stage before pipeline execution. Since this code is compiled together with user source code, `ForAttributeWithMetadataName` can reference that attribute. It is suitable for fixed code that always produces the same result regardless of source changes.

### Q2: Why is `Collect()` needed in `IncrementalGeneratorBase<TValue>`?
**A**: `Collect()` gathers multiple `IncrementalValuesProvider<T>` items into a single `ImmutableArray<T>`. In Functorium, this pattern is used to receive all target classes at once and generate code sequentially while reusing a `StringBuilder`. However, when individual item caching is needed, processing individually without `Collect` is more efficient.

### Q3: What rules should be followed for `AddSource`'s `hintName`?
**A**: `hintName` must be unique within the project, and the convention is to use the `.g.cs` extension. Since classes with the same name may exist in different namespaces, specifying it with a namespace suffix like `Repositories.UserRepositoryObservable.g.cs` prevents conflicts.

---

Now that we understand the overall structure of `IIncrementalGenerator`, next we examine the Provider pattern, the core component of the pipeline. We will learn how LINQ-like declarative operators transform and filter data.

-> [02. Provider Pattern](../02-Provider-Pattern/)
