---
title: "Provider Pattern"
---

## Overview

In the previous chapter, we confirmed that the pipeline is configured inside the `Initialize` method. So how exactly is this pipeline built? If you have used LINQ, operators like `Select`, `Where`, and `Collect` should be familiar. The **Provider pattern** uses exactly these LINQ-style declarative operators to compose a data pipeline that extracts and transforms needed information from source code. Our project's ObservablePortGenerator also uses this pattern to find classes with the `[GenerateObservablePort]` attribute and transform them into `ObservableClassInfo`.

## Learning Objectives

### Core Learning Objectives
1. **Understand the difference between IncrementalValuesProvider and IncrementalValueProvider**
   - Distinguishing between multiple values (0..N) and a single value (exactly 1)
2. **Learn pipeline composition using LINQ-style operators**
   - Roles and usage timing of Select, Where, Collect, and Combine
3. **Analyze the actual pipeline structure of ObservablePortGenerator**
   - ForAttributeWithMetadataName -> Where -> Collect flow

---

## What is a Provider?

A **Provider** is the core element for composing the source generator's **data pipeline**. Just as you chain `Select` and `Where` on `IEnumerable<T>` in LINQ, you chain operators with the same names on Providers to declaratively express the process of extracting and transforming needed information from source code.

```
Provider Pipeline Flow
=======================

Source Code
    |
    v
+-------------------------+
| SyntaxProvider          |  Extract nodes from source
| (ForAttributeWithMeta...)|
+----------+--------------+
           |
           v
+-------------------------+
| Select                  |  Data transformation
| (Syntax -> needed info) |
+----------+--------------+
           |
           v
+-------------------------+
| Where                   |  Filtering
| (select valid only)     |
+----------+--------------+
           |
           v
+-------------------------+
| Collect                 |  Collect into array
| (individual items -> array) |
+----------+--------------+
           |
           v
RegisterSourceOutput
(code generation)
```

---

## Two Provider Types

### IncrementalValuesProvider<T>

Represents **multiple (0 or more)** values:

```csharp
// Multiple classes may have the [GenerateObservablePort] attribute
IncrementalValuesProvider<ObservableClassInfo> provider = context.SyntaxProvider
    .ForAttributeWithMetadataName(...);

// 0: No classes with the attribute
// 1: One class
// N: Multiple classes
```

### IncrementalValueProvider<T>

Represents **exactly one** value:

```csharp
// Compilation options are always exactly one
IncrementalValueProvider<CompilationOptions> options =
    context.CompilationOptionsProvider;

// Converting with Collect yields a single value
IncrementalValueProvider<ImmutableArray<ObservableClassInfo>> collected =
    provider.Collect();
```

---

## Key Operators

Each operator has the same meaning as its LINQ counterpart. The difference is that these operators are integrated with the compiler's incremental caching system, reusing previous results when input has not changed.

### Select - Data Transformation

```csharp
// SyntaxNode -> class name
var classNames = context.SyntaxProvider
    .ForAttributeWithMetadataName(...)
    .Select((ctx, _) => ctx.TargetSymbol.Name);

// ObservableClassInfo -> code to generate
var codes = provider
    .Select((info, _) => GenerateCode(info));
```

### Where - Filtering

```csharp
// Select only valid items
var validClasses = provider
    .Where(x => x != ObservableClassInfo.None);

// Select only public classes
var publicClasses = provider
    .Where(x => x.IsPublic);
```

### Collect - Collect Into Array

```csharp
// IncrementalValuesProvider<T> -> IncrementalValueProvider<ImmutableArray<T>>
var collected = provider.Collect();

// Useful when processing multiple items at once
context.RegisterSourceOutput(collected, (ctx, items) =>
{
    foreach (var item in items)
    {
        ctx.AddSource(...);
    }
});
```

### Combine - Combine Two Providers

```csharp
// Combine class info + compilation options
var combined = provider.Combine(context.CompilationOptionsProvider);

context.RegisterSourceOutput(combined, (ctx, pair) =>
{
    var classInfo = pair.Left;
    var options = pair.Right;
    // ...
});
```

---

## Actual Code: ObservablePortGenerator

Now that we have examined individual operators, let us see how they are combined in our project.

```csharp
private static IncrementalValuesProvider<ObservableClassInfo> RegisterSourceProvider(
    IncrementalGeneratorInitializationContext context)
{
    // Stage 1: Generate fixed code (Attribute definition)
    context.RegisterPostInitializationOutput(ctx =>
        ctx.AddSource(
            hintName: GenerateObservablePortAttributeFileName,
            sourceText: SourceText.From(GenerateObservablePortAttribute, Encoding.UTF8)));

    // Stage 2: Configure pipeline
    return context
        .SyntaxProvider
        // Select only classes with [GenerateObservablePort] attribute
        .ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: FullyQualifiedAttributeName,
            predicate: IsClass,                    // Syntax-level filter
            transform: MapToObservableClassInfo)     // Semantic info extraction
        // Exclude invalid items
        .Where(x => x != ObservableClassInfo.None);
}
```

---

## Pipeline Composition Patterns

### Pattern 1: Simple Transformation

```csharp
// Extract only class names
var classNames = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Select((ctx, _) => ctx.TargetSymbol.Name);

context.RegisterSourceOutput(classNames, (ctx, name) =>
{
    ctx.AddSource($"{name}.g.cs", $"// Generated for {name}");
});
```

### Pattern 2: Complex Data Structure

```csharp
// Transform into detailed info record
var classInfos = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Select((ctx, _) => new ClassInfo(
        Name: ctx.TargetSymbol.Name,
        Namespace: ctx.TargetSymbol.ContainingNamespace.ToString(),
        Methods: GetMethods(ctx.TargetSymbol)));

context.RegisterSourceOutput(classInfos, (ctx, info) =>
{
    var code = GenerateCode(info);
    ctx.AddSource($"{info.Name}.g.cs", code);
});
```

### Pattern 3: Batch Processing

```csharp
// Process all classes at once
var allClasses = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Collect();  // Collect into ImmutableArray

context.RegisterSourceOutput(allClasses, (ctx, classes) =>
{
    // Generate summary file
    var summary = string.Join("\n", classes.Select(c => c.Name));
    ctx.AddSource("Summary.g.cs", $"// Generated {classes.Length} classes\n{summary}");

    // Generate file for each class
    foreach (var cls in classes)
    {
        ctx.AddSource($"{cls.Name}.g.cs", GenerateCode(cls));
    }
});
```

### Pattern 4: Conditional Combination

```csharp
// Generate different code based on compilation options
var withOptions = provider
    .Combine(context.CompilationOptionsProvider);

context.RegisterSourceOutput(withOptions, (ctx, pair) =>
{
    var (classInfo, options) = pair;

    string code = options.OptimizationLevel == OptimizationLevel.Debug
        ? GenerateDebugCode(classInfo)
        : GenerateReleaseCode(classInfo);

    ctx.AddSource($"{classInfo.Name}.g.cs", code);
});
```

---

## Caching and Performance

The most important reason for using the Provider pattern is **automatic caching**. At each stage of the pipeline, if the input is the same as before, the compiler retrieves the result from cache and skips processing.

```
Behavior During Incremental Build
==================================

1. File A modified
   |
   v
2. Pipeline re-execution
   - File A: Process anew
   - File B: Retrieved from cache (processing skipped)
   - File C: Retrieved from cache (processing skipped)
   |
   v
3. Code regenerated only for changed file A
```

### Caching Considerations

```csharp
// Bad example: Non-deterministic data
.Select((ctx, _) => new ClassInfo(
    Name: ctx.TargetSymbol.Name,
    Timestamp: DateTime.Now  // Different value every time!
))

// Good example: Deterministic data
.Select((ctx, _) => new ClassInfo(
    Name: ctx.TargetSymbol.Name,
    Namespace: ctx.TargetSymbol.ContainingNamespace.ToString()
))
```

---

## Data Model Design

For caching to work correctly, the data model must have **value semantics**. Two objects with the same content must be judged equal by `Equals` so the compiler can recognize "no change" and utilize the cache. This is why our project's `ObservableClassInfo` is defined as a `readonly record struct`.

```csharp
// Using readonly record struct (value semantics + automatic Equals/GetHashCode)
public readonly record struct ObservableClassInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly List<MethodInfo> Methods;
    public readonly List<ParameterInfo> BaseConstructorParameters;
    public readonly Location? Location;

    // None pattern: use empty object instead of null
    public static readonly ObservableClassInfo None = new(
        string.Empty, string.Empty, new List<MethodInfo>(),
        new List<ParameterInfo>(), null);

    public ObservableClassInfo(
        string @namespace, string className,
        List<MethodInfo> methods,
        List<ParameterInfo> baseConstructorParameters,
        Location? location)
    {
        Namespace = @namespace;
        ClassName = className;
        Methods = methods;
        BaseConstructorParameters = baseConstructorParameters;
        Location = location;
    }
}

// Constructor-based class
public class MethodInfo
{
    public string Name { get; }
    public List<ParameterInfo> Parameters { get; }
    public string ReturnType { get; }

    public MethodInfo(string name, List<ParameterInfo> parameters,
        string returnType)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
    }
}

public class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public RefKind RefKind { get; }
    public bool IsCollection { get; }

    public ParameterInfo(string name, string type, RefKind refKind)
    {
        Name = name;
        Type = type;
        RefKind = refKind;
        IsCollection = CollectionTypeHelper.IsCollectionType(type);
    }
}
```

---

## Summary at a Glance

The Provider pattern composes the source generation pipeline in the same declarative style as LINQ, while providing automatic caching at each stage to guarantee incremental build performance. Applying value semantics to data models is the key prerequisite for caching.

| Provider Type | Value Count | Purpose |
|---------------|-------------|---------|
| `IncrementalValuesProvider<T>` | 0..N | Processing multiple items |
| `IncrementalValueProvider<T>` | Exactly 1 | Single value, Collect result |

| Operator | Function | Returns |
|----------|----------|---------|
| `Select` | Transform | Same Provider type |
| `Where` | Filter | ValuesProvider |
| `Collect` | Collect into array | ValueProvider |
| `Combine` | Combine | ValueProvider (tuple) |

---

## FAQ

### Q1: How do `IncrementalValuesProvider<T>` and `IncrementalValueProvider<T>` differ?
**A**: `IncrementalValuesProvider<T>` provides 0 or more values as a stream and supports operators like `Select` and `Where`. `IncrementalValueProvider<T>` provides exactly one value, and results of `Collect()` or `Combine()` are of this type. Both types can be passed to `RegisterSourceOutput` when registering code generation.

### Q2: Why does using `readonly record struct` for data models improve caching performance?
**A**: `record struct` automatically generates value-based `Equals`/`GetHashCode`. Roslyn compares previous and current results at each pipeline stage and skips subsequent stages if they are identical. Accurate value comparison ensures higher cache hit rates and reduces unnecessary code regeneration.

### Q3: In what situations is the `Combine` operator used?
**A**: It is used when you need to combine data extracted from source code with external information like compilation options. For example, to generate different code depending on Debug/Release mode, you can combine two data sources with `provider.Combine(context.CompilationProvider)` and reference both during code generation.

---

Now that we understand the full flow of the Provider pipeline, next we examine `ForAttributeWithMetadataName`, the most frequently used API at the pipeline's starting point. We will see how this API optimizes attribute-based filtering and why it is 10-100x faster compared to manual implementation.

-> [03. ForAttributeWithMetadataName](../03-ForAttribute/)
