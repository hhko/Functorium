---
title: "Semantic API"
---

## Overview

In the previous chapter, we identified the limitations of the Syntax API. We could not determine whether a parameter type `User` is a class or interface, or which namespace it belongs to, through syntax analysis alone. The Semantic API starts at precisely this point.

The Semantic API combines **type information and semantic analysis results** with the Syntax Tree, enabling programmatic querying of code "meaning". The fact that our project's ObservablePortGenerator extracts the list of interfaces a class implements, method signatures, and fully qualified return type names through `ctx.TargetSymbol` in the `transform` stage is all thanks to the Semantic API.

## Learning Objectives

### Core Learning Objectives
1. **Understand the role of the Semantic Model**
   - The semantic analysis layer that adds type information to the Syntax Tree
2. **Learn type information querying methods**
   - When to use `GetSymbolInfo`, `GetTypeInfo`, `GetDeclaredSymbol`
3. **Learn the integration of Syntax API and Semantic API**
   - The two-stage analysis pattern from `predicate` (Syntax) to `transform` (Semantic)

---

## What is the Semantic API?

The **Semantic API** adds **type information and semantic analysis** to the Syntax Tree.

```
Syntax API vs Semantic API
==========================

Syntax API (syntax)
-----------------
Code: public void Process(User user) { }

What it can tell:
- Method name is "Process"
- Parameter name is "user"
- Parameter type text is "User"

What it cannot tell:
- Is User a class? Interface? Struct?
- What is User's full namespace?
- What members does User have?


Semantic API (semantics)
------------------
What it can tell:
- User is the class MyApp.Models.User
- User implements the IEntity interface
- User has Id, Name properties
- Process method's return type is void
```

---

## Obtaining a SemanticModel

### General Approach

```csharp
// Obtaining SemanticModel from Compilation
var compilation = CSharpCompilation.Create(
    "MyAssembly",
    [syntaxTree],
    references,
    options);

SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
```

### In Source Generators

```csharp
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyNamespace.GenerateObservablePortAttribute",
        predicate: (node, _) => node is ClassDeclarationSyntax,
        transform: (ctx, _) =>
        {
            // Direct access from GeneratorAttributeSyntaxContext
            SemanticModel semanticModel = ctx.SemanticModel;

            // Or use the target symbol directly
            ISymbol symbol = ctx.TargetSymbol;

            return symbol;
        });
```

---

## Querying Symbol Information

### GetSymbolInfo

Obtains symbol information from a Syntax node:

```csharp
string code = """
    public class User
    {
        public int Id { get; set; }
    }

    public class Example
    {
        public void Process(User user)
        {
            var id = user.Id;  // Analyze this part
        }
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Test", [tree], references);
var semanticModel = compilation.GetSemanticModel(tree);

// Find the user.Id expression
var memberAccess = tree.GetRoot()
    .DescendantNodes()
    .OfType<MemberAccessExpressionSyntax>()
    .First();

// Query symbol information
SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
ISymbol? symbol = symbolInfo.Symbol;

Console.WriteLine($"Symbol: {symbol?.Name}");           // Id
Console.WriteLine($"Kind: {symbol?.Kind}");             // Property
Console.WriteLine($"Containing type: {symbol?.ContainingType}"); // User
```

### GetTypeInfo

Obtains type information of an expression:

```csharp
// Type of id in: var id = user.Id;
var variableDecl = tree.GetRoot()
    .DescendantNodes()
    .OfType<VariableDeclaratorSyntax>()
    .First(v => v.Identifier.Text == "id");

var initializer = variableDecl.Initializer!.Value;
TypeInfo typeInfo = semanticModel.GetTypeInfo(initializer);

Console.WriteLine($"Type: {typeInfo.Type}");          // int
Console.WriteLine($"Converted type: {typeInfo.ConvertedType}"); // int
```

### GetDeclaredSymbol

Obtains a symbol from a declaration:

```csharp
var classDecl = tree.GetRoot()
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// Obtain symbol from class declaration
INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

Console.WriteLine($"Class: {classSymbol?.Name}");
Console.WriteLine($"Namespace: {classSymbol?.ContainingNamespace}");
Console.WriteLine($"Interfaces: {string.Join(", ", classSymbol?.AllInterfaces ?? [])}");
```

---

## Usage in Source Generators

In actual source generators, there is no need to create `SemanticModel` directly -- `GeneratorAttributeSyntaxContext` already provides a prepared `SemanticModel` and `TargetSymbol`. Our project's `MapToObservableClassInfo` method is a representative example of utilizing this.

### Utilizing GeneratorAttributeSyntaxContext

```csharp
private static ObservableClassInfo MapToObservableClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    // 1. Direct access to target symbol (Semantic API)
    if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
    {
        return ObservableClassInfo.None;
    }

    // 2. Extract class information
    string className = classSymbol.Name;
    string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
        ? string.Empty
        : classSymbol.ContainingNamespace.ToString();

    // 3. Analyze implemented interfaces
    var interfaces = classSymbol.AllInterfaces;

    // 4. Extract interface methods
    var methods = interfaces
        .Where(ImplementsIObservablePort)
        .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
        .Where(m => m.MethodKind == MethodKind.Ordinary)
        .ToList();

    return new ObservableClassInfo(@namespace, className, methods);
}
```

---

## Type Comparison and Inspection

### Verifying Type Identity

```csharp
// Check if two types are the same
bool areSameType = SymbolEqualityComparer.Default.Equals(type1, type2);

// SymbolEqualityComparer options
// Default: basic comparison
// IncludeNullability: comparison including nullable annotations
```

### Checking for a Specific Type

```csharp
// Check if it implements the IObservablePort interface
bool implementsIObservablePort = classSymbol.AllInterfaces
    .Any(i => i.Name == "IObservablePort");

// Check if it belongs to a specific namespace
bool isInMyNamespace = classSymbol.ContainingNamespace
    .ToDisplayString() == "MyApp.Models";
```

### Obtaining Type Names

```csharp
// Obtaining type name in various formats
ITypeSymbol type = ...;

// Short name
string shortName = type.Name;  // User

// With namespace
string fullName = type.ToDisplayString();  // MyApp.Models.User

// With global:: prefix (important for deterministic code generation)
string globalName = type.ToDisplayString(
    SymbolDisplayFormat.FullyQualifiedFormat);  // global::MyApp.Models.User
```

---

## Method Symbol Analysis

```csharp
IMethodSymbol method = ...;

// Basic information
Console.WriteLine($"Name: {method.Name}");
Console.WriteLine($"Return type: {method.ReturnType}");
Console.WriteLine($"Is static: {method.IsStatic}");
Console.WriteLine($"Is async: {method.IsAsync}");

// Parameter analysis
foreach (var param in method.Parameters)
{
    Console.WriteLine($"Parameter: {param.Type} {param.Name}");
    Console.WriteLine($"  - RefKind: {param.RefKind}");  // None, Ref, Out, In
    Console.WriteLine($"  - Has default value: {param.HasExplicitDefaultValue}");
}

// Generic type parameters
if (method.IsGenericMethod)
{
    foreach (var typeParam in method.TypeParameters)
    {
        Console.WriteLine($"Type parameter: {typeParam.Name}");
    }
}
```

---

## Actual Code Example: ObservablePortGenerator

```csharp
// Extracting method information in ObservablePortGenerator.cs
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)
    .Select(m => new MethodInfo(
        m.Name,
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            // Obtaining precise type string via Semantic API
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),
        // Precisely extracting return type as well
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

---

## Semantic API Performance Considerations

```
Performance Tips
================

1. SemanticModel is heavyweight
   - Cache when possible
   - Do not create unnecessarily multiple times

2. GetSymbolInfo vs GetDeclaredSymbol
   - Obtaining symbol from declaration: GetDeclaredSymbol (fast)
   - Obtaining symbol from reference: GetSymbolInfo (slightly slower)

3. Utilize ForAttributeWithMetadataName
   - More efficient than directly traversing Syntax Tree
   - Optimized for incremental builds
```

---

## Summary at a Glance

The Semantic API is the essential tool for querying type information, namespaces, and interface implementation relationships that the Syntax API cannot provide. In source generators, you access the prepared `SemanticModel` and `TargetSymbol` through `GeneratorAttributeSyntaxContext`, so there is no need to generate the model directly from `Compilation`.

| Method | Purpose | Input | Output |
|--------|---------|-------|--------|
| `GetSymbolInfo` | Reference resolution | Expression node | SymbolInfo |
| `GetTypeInfo` | Type information | Expression node | TypeInfo |
| `GetDeclaredSymbol` | Declaration symbol | Declaration node | ISymbol |

| Comparison | Syntax API | Semantic API |
|------------|------------|--------------|
| Information | Structure | Structure + Types |
| Speed | Fast | Relatively slower |
| Purpose | Filtering (`predicate`) | Detailed analysis (`transform`) |

---

## FAQ

### Q1: How do you distinguish between `GetSymbolInfo` and `GetDeclaredSymbol`?
**A**: `GetDeclaredSymbol` is used to obtain a symbol from **declaration** nodes such as classes, methods, and variables. `GetSymbolInfo` is used to resolve the symbol at **usage** points such as type references or method calls. In source generators, since you mainly analyze declarations, `GetDeclaredSymbol` is used more frequently.

### Q2: Why don't you need to create `SemanticModel` directly in source generators?
**A**: The `GeneratorAttributeSyntaxContext` passed to the `transform` callback of `ForAttributeWithMetadataName` already has `SemanticModel` and `TargetSymbol` prepared. Since the Roslyn pipeline automatically provides them during the compilation process, there is no need to call `Compilation.GetSemanticModel()` directly.

### Q3: Why is `ForAttributeWithMetadataName` more efficient than directly traversing the Syntax Tree?
**A**: Roslyn internally leverages attribute metadata indexes to quickly find target nodes. Additionally, during incremental builds, unchanged files are skipped, greatly reducing unnecessary analysis compared to manual traversal.

---

We learned how to access symbols through the Semantic API. In the next chapter, we learn the hierarchy of symbol types such as `INamedTypeSymbol`, `IMethodSymbol`, `IParameterSymbol` and the detailed information that can be extracted from each type.

-> [04. Symbol Types](../07-Symbol-Types/)
