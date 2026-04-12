---
title: "ForAttributeWithMetadataName"
---

## Overview

In the previous chapter, we mentioned that `SyntaxProvider` is used as the starting point of the Provider pipeline. In practice, for attribute-based source generators, `ForAttributeWithMetadataName` almost always serves as that starting point. This API directly leverages the compiler's internal attribute index, making it 10-100x faster than manual implementations that traverse all nodes checking for attributes. ObservablePortGenerator also operates based on the `[GenerateObservablePort]` attribute, making this API the core entry point of the pipeline.

## Learning Objectives

### Core Learning Objectives
1. **Understand the role and performance benefits of the ForAttributeWithMetadataName API**
   - Optimization principles using the compiler's internal index
2. **Learn how to use the predicate and transform callbacks**
   - Separation of Syntax-level filtering and Semantic-level transformation
3. **Understand the structure of GeneratorAttributeSyntaxContext**
   - How to access TargetSymbol, SemanticModel, and Attributes

---

## What is ForAttributeWithMetadataName?

It is the core API for **attribute-based source generation**. It efficiently filters only declarations with a specific attribute.

```csharp
IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
    string fullyQualifiedMetadataName,  // Fully qualified name of the attribute
    Func<SyntaxNode, CancellationToken, bool> predicate,  // Syntax-level filter
    Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform  // Transformation
);
```

---

## Why ForAttributeWithMetadataName?

### Manual Implementation vs ForAttributeWithMetadataName

```csharp
// Manual implementation (inefficient)
var classes = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: (node, _) => node is ClassDeclarationSyntax,
        transform: (ctx, _) =>
        {
            var classDecl = (ClassDeclarationSyntax)ctx.Node;

            // Accessing Semantic Model for every class (slow!)
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl);

            // Checking attributes
            return symbol?.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "GenerateObservablePortAttribute")
                    == true ? symbol : null;
        })
    .Where(x => x is not null);

// ForAttributeWithMetadataName (efficient)
var classes = context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyNamespace.GenerateObservablePortAttribute",  // Compiler optimizes
        predicate: (node, _) => node is ClassDeclarationSyntax,
        transform: (ctx, _) => ctx.TargetSymbol);  // Symbol already prepared
```

### Performance Difference

```
Manual Implementation
=====================
1. Traverse all classes
2. Access Semantic Model for each class
3. Query attribute list
4. Compare attribute names

ForAttributeWithMetadataName
============================
1. Compiler directly queries from attribute index
2. Returns only declarations with that attribute
3. Semantic Model is pre-prepared

-> 10-100x faster or more
```

---

## Method Signature Analysis

```csharp
.ForAttributeWithMetadataName(
    fullyQualifiedMetadataName: "Namespace.AttributeName",
    predicate: (SyntaxNode node, CancellationToken ct) => bool,
    transform: (GeneratorAttributeSyntaxContext ctx, CancellationToken ct) => T
)
```

### fullyQualifiedMetadataName

The **fully qualified metadata name** of the attribute:

```csharp
// Attribute definition
namespace Functorium.Adapters.SourceGenerators;

public class GenerateObservablePortAttribute : System.Attribute { }

// Metadata name
"Functorium.Adapters.SourceGenerators.GenerateObservablePortAttribute"

// For generic attributes
"MyNamespace.MyAttribute`1"  // Attribute with <T>
```

### predicate

Quick filtering at the **Syntax level**:

```csharp
// Select only classes
predicate: (node, _) => node is ClassDeclarationSyntax

// Select only public classes
predicate: (node, _) =>
    node is ClassDeclarationSyntax classDecl &&
    classDecl.Modifiers.Any(SyntaxKind.PublicKeyword)

// Select only specific name patterns
predicate: (node, _) =>
    node is ClassDeclarationSyntax classDecl &&
    classDecl.Identifier.Text.EndsWith("Repository")
```

### transform

Extracts needed data using **Semantic information**:

```csharp
transform: (ctx, cancellationToken) =>
{
    // ctx.TargetNode: Syntax node with the attribute
    // ctx.TargetSymbol: The corresponding symbol (ISymbol)
    // ctx.SemanticModel: Semantic Model
    // ctx.Attributes: Matched attributes

    return ExtractInfo(ctx.TargetSymbol);
}
```

---

## GeneratorAttributeSyntaxContext

The context received in the transform callback:

```csharp
public readonly struct GeneratorAttributeSyntaxContext
{
    // Syntax node with the attribute (ClassDeclarationSyntax, etc.)
    public SyntaxNode TargetNode { get; }

    // The corresponding symbol (INamedTypeSymbol, IMethodSymbol, etc.)
    public ISymbol TargetSymbol { get; }

    // Semantic Model
    public SemanticModel SemanticModel { get; }

    // Matched attributes (the same attribute can appear multiple times)
    public ImmutableArray<AttributeData> Attributes { get; }
}
```

---

## Actual Code: ObservablePortGenerator

We have examined each component of the API so far. Now let us see how these components are combined in our project's full flow.

```csharp
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePortGenerator()
    : IncrementalGeneratorBase<ObservableClassInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)
{
    private const string AttributeName = "GenerateObservablePort";
    private const string AttributeNamespace = "Functorium.Adapters.SourceGenerators";
    private const string FullyQualifiedAttributeName =
        $"{AttributeNamespace}.{AttributeName}Attribute";

    private static IncrementalValuesProvider<ObservableClassInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // 1. Generate attribute definition
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource(
                hintName: "GenerateObservablePortAttribute.g.cs",
                sourceText: SourceText.From(GenerateObservablePortAttribute, Encoding.UTF8)));

        // 2. Filter with ForAttributeWithMetadataName
        return context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: FullyQualifiedAttributeName,
                predicate: IsClass,                    // Check if class
                transform: MapToObservableClassInfo)     // Extract class info
            .Where(x => x != ObservableClassInfo.None);  // Valid only
    }

    // predicate implementation
    private static bool IsClass(SyntaxNode node, CancellationToken cancellationToken)
        => node is ClassDeclarationSyntax;

    // transform implementation
    private static ObservableClassInfo MapToObservableClassInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        // Verify class symbol
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return ObservableClassInfo.None;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Extract class information
        string className = classSymbol.Name;
        string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToString();

        // Extract methods from IObservablePort interfaces
        var methods = classSymbol.AllInterfaces
            .Where(ImplementsIObservablePort)
            .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Select(m => new MethodInfo(
                m.Name,
                m.Parameters.Select(p => new ParameterInfo(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
                    p.RefKind)).ToList(),
                m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
            .ToList();

        // No generation needed if no methods
        if (methods.Count == 0)
        {
            return ObservableClassInfo.None;
        }

        // Extract constructor parameters
        var baseConstructorParameters =
            ConstructorParameterExtractor.ExtractParameters(classSymbol);

        return new ObservableClassInfo(
            @namespace, className, methods, baseConstructorParameters);
    }
}
```

---

## Attribute Definition Generation

To use ForAttributeWithMetadataName, the **attribute must be defined**:

```csharp
// Generate attribute definition in RegisterPostInitializationOutput
public const string GenerateObservablePortAttribute = """
    // <auto-generated/>

    namespace Functorium.Adapters.SourceGenerators;

    /// <summary>
    /// Attribute that instructs pipeline wrapper generation for an adapter class
    /// </summary>
    [global::System.AttributeUsage(
        global::System.AttributeTargets.Class,
        AllowMultiple = false,
        Inherited = false)]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
        Justification = "Generated by source generator.")]
    public class GenerateObservablePortAttribute : global::System.Attribute;
    """;
```

### Why Use the global:: Prefix

```csharp
// Potential conflict
public class GenerateObservablePortAttribute : System.Attribute;
// Conflicts if user code has a System namespace

// Always safe
public class GenerateObservablePortAttribute : global::System.Attribute;
// global:: always starts from the global namespace
```

---

## Cancellation Token Handling

In long-running transforms, you must check the cancellation token:

```csharp
transform: (ctx, cancellationToken) =>
{
    // Check for cancellation before heavy work
    cancellationToken.ThrowIfCancellationRequested();

    var methods = classSymbol.AllInterfaces
        .SelectMany(i =>
        {
            // Also check within loops
            cancellationToken.ThrowIfCancellationRequested();
            return i.GetMembers().OfType<IMethodSymbol>();
        })
        .ToList();

    return new ClassInfo(...);
}
```

---

## Summary at a Glance

`ForAttributeWithMetadataName` is the core entry point for attribute-based source generators. The key to performance is the two-stage separation: fast Syntax-level filtering in the `predicate` and Semantic-level data extraction in the `transform`. The attribute name must include both the namespace and the `Attribute` suffix.

| Component | Role | Notes |
|-----------|------|-------|
| `fullyQualifiedMetadataName` | Full attribute name | Include namespace, include `Attribute` suffix |
| `predicate` | Syntax-level filter | Fast, no Semantic access |
| `transform` | Data extraction | Semantic access available, heavyweight |
| `GeneratorAttributeSyntaxContext` | transform context | TargetSymbol is key |

---

## FAQ

### Q1: Why must the `Attribute` suffix be included in `fullyQualifiedMetadataName`?
**A**: In C# syntax, the suffix can be omitted as in `[GenerateObservablePort]`, but Roslyn's metadata name uses the actual class name as-is. Therefore, the full name including the `Attribute` suffix like `Functorium.Adapters.SourceGenerators.GenerateObservablePortAttribute` must be specified for correct matching.

### Q2: Why can't the Semantic API be used in the `predicate`?
**A**: The `predicate` is called for every syntax node for fast first-pass filtering, so allowing expensive semantic analysis would greatly degrade performance. Instead, filter only by `SyntaxNode` type (`node is ClassDeclarationSyntax`), and perform detailed analysis in the `transform`.

### Q3: Why must the `CancellationToken` be checked in the `transform`?
**A**: In the IDE, compilation can be triggered repeatedly as the user types. When a new analysis starts while the previous one is not yet complete, the previous work is cancelled. If `ThrowIfCancellationRequested()` is not called, unnecessary work continues to proceed, degrading IDE responsiveness.

---

We have understood how to efficiently extract data with `ForAttributeWithMetadataName`. However, for this efficiency to be fully realized, caching must work correctly at each stage of the pipeline. In the next chapter, we examine the principles of incremental caching and common mistakes that invalidate the cache.

-> [04. Incremental Caching](../04-Incremental-Caching/)
