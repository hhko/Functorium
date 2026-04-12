---
title: "Symbol Type"
---

## Overview

In the previous chapter, we learned how to access symbols through the Semantic API. To actually utilize the `ISymbol` obtained via `GetDeclaredSymbol` or `ctx.TargetSymbol`, you need to understand the symbol type hierarchy and be able to cast to the appropriate type for each situation.

Our project's ObservablePortGenerator extracts class and interface information from `INamedTypeSymbol`, analyzes method signatures from `IMethodSymbol`, and reads parameter types and `RefKind` from `IParameterSymbol`. In this chapter, we systematically learn the properties and practical usage patterns of these symbol types.

## Learning Objectives

### Core Learning Objectives
1. **Understand the ISymbol hierarchy**
   - Inheritance relationships between symbol types and selection criteria by purpose
2. **Detailed study of INamedTypeSymbol and IMethodSymbol**
   - Key properties needed for class/interface analysis and method signature extraction
3. **Learn symbol APIs used in source generators**
   - Pattern learning through actual code from ObservablePortGenerator

---

## ISymbol Hierarchy

All symbols are based on the `ISymbol` interface. In source generators, the most commonly used are `INamedTypeSymbol` (class/interface analysis), `IMethodSymbol` (method signatures), and `IParameterSymbol` (parameter information):

```
ISymbol (base interface)
│
├── INamespaceSymbol          Namespace
│
├── ITypeSymbol (abstract)    Type
│   ├── INamedTypeSymbol      Class, interface, struct, enum
│   ├── IArrayTypeSymbol      Array type
│   ├── IPointerTypeSymbol    Pointer type
│   └── ITypeParameterSymbol  Generic type parameter
│
├── IMethodSymbol             Method, constructor
├── IPropertySymbol           Property
├── IFieldSymbol              Field
├── IEventSymbol              Event
├── IParameterSymbol          Parameter
├── ILocalSymbol              Local variable
└── IAliasSymbol              using alias
```

---

## ISymbol Common Properties

```csharp
ISymbol symbol = ...;

// Basic information
symbol.Name                    // Name
symbol.Kind                    // Symbol kind (SymbolKind enum)
symbol.ContainingNamespace     // Containing namespace
symbol.ContainingType          // Containing type (if a member)
symbol.ContainingSymbol        // Containing symbol (parent)

// Accessibility
symbol.DeclaredAccessibility   // Public, Private, Internal, etc.

// Metadata
symbol.IsStatic               // Is static
symbol.IsAbstract             // Is abstract
symbol.IsVirtual              // Is virtual
symbol.IsOverride             // Is override
symbol.IsSealed               // Is sealed

// Location
symbol.Locations              // Source code locations
symbol.DeclaringSyntaxReferences // Declaration syntax references
```

---

## INamedTypeSymbol

The most frequently used symbol type in source generators. It represents **classes, interfaces, structs, and enums**. In ObservablePortGenerator, `ctx.TargetSymbol` is cast to `INamedTypeSymbol` to analyze the class's interface list and members.

### Basic Properties

```csharp
INamedTypeSymbol typeSymbol = ...;

// Type kind
typeSymbol.TypeKind          // Class, Interface, Struct, Enum, Delegate

// Name-related
typeSymbol.Name              // Short name
typeSymbol.MetadataName      // Metadata name (including generics)
typeSymbol.ToDisplayString() // Full name

// Namespace
typeSymbol.ContainingNamespace
typeSymbol.ContainingNamespace.IsGlobalNamespace // Is global

// Base type
typeSymbol.BaseType          // Parent class
typeSymbol.AllInterfaces     // All interfaces (direct + inherited)
typeSymbol.Interfaces        // Only directly implemented interfaces
```

### Member Querying

```csharp
// All members
var allMembers = typeSymbol.GetMembers();

// Members with a specific name
var namedMembers = typeSymbol.GetMembers("GetUser");

// Filtering by type
var methods = typeSymbol.GetMembers()
    .OfType<IMethodSymbol>();

var properties = typeSymbol.GetMembers()
    .OfType<IPropertySymbol>();

var constructors = typeSymbol.Constructors;  // Constructors
```

### Generic Types

```csharp
// Is generic
typeSymbol.IsGenericType     // true for List<T>
typeSymbol.TypeArguments     // Type arguments [int] for List<int>
typeSymbol.TypeParameters    // Type parameters [T] for List<T>

// Original definition
typeSymbol.OriginalDefinition // List<> (unbounded)

// Example: Dictionary<string, int>
// TypeArguments: [string, int]
// TypeParameters: [TKey, TValue] (from OriginalDefinition)
```

---

## IMethodSymbol

Represents **methods, constructors, destructors, and operators**. In our project, we extract the method list of interfaces with `GetMembers().OfType<IMethodSymbol>()`, then filter with `MethodKind.Ordinary` to exclude property getters/setters and constructors.

### Basic Properties

```csharp
IMethodSymbol method = ...;

// Name
method.Name                  // Method name

// Method kind
method.MethodKind            // Ordinary, Constructor, PropertyGet, etc.

// Return type
method.ReturnType            // ITypeSymbol
method.ReturnsVoid           // Whether it returns void

// Modifiers
method.IsStatic              // Is static
method.IsAsync               // Is async
method.IsAbstract            // Is abstract
method.IsVirtual             // Is virtual
method.IsExtensionMethod     // Is extension method
```

### MethodKind Enum

```csharp
public enum MethodKind
{
    Ordinary,              // Regular method
    Constructor,           // Constructor
    StaticConstructor,     // Static constructor
    Destructor,            // Destructor
    PropertyGet,           // Property getter
    PropertySet,           // Property setter
    EventAdd,              // Event add
    EventRemove,           // Event remove
    ExplicitInterfaceImplementation,  // Explicit interface implementation
    Conversion,            // Conversion operator
    UserDefinedOperator,   // User-defined operator
    // ...
}
```

### Parameter Analysis

```csharp
// Parameter list
foreach (var param in method.Parameters)
{
    Console.WriteLine($"Name: {param.Name}");
    Console.WriteLine($"Type: {param.Type}");
    Console.WriteLine($"RefKind: {param.RefKind}");    // None, Ref, Out, In
    Console.WriteLine($"Has default value: {param.HasExplicitDefaultValue}");

    if (param.HasExplicitDefaultValue)
    {
        Console.WriteLine($"Default value: {param.ExplicitDefaultValue}");
    }
}
```

### Generic Methods

```csharp
// Is generic
method.IsGenericMethod
method.TypeArguments        // Type arguments
method.TypeParameters       // Type parameters
```

---

## Practical Usage: ObservablePortGenerator

### Extracting Method Information

```csharp
// ObservablePortGenerator.cs
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)  // Only regular methods
    .Select(m => new MethodInfo(
        m.Name,
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

### Extracting Constructor Parameters

```csharp
// ConstructorParameterExtractor.cs
public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
{
    // 1. Find parameters from the class's own constructors
    var constructor = classSymbol.Constructors
        .Where(c => c.DeclaredAccessibility == Accessibility.Public)
        .OrderByDescending(c => c.Parameters.Length)  // Prefer more parameters
        .FirstOrDefault();

    if (constructor is not null && constructor.Parameters.Length > 0)
    {
        return constructor.Parameters
            .Select(p => new ParameterInfo(
                p.Name,
                p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
                p.RefKind))
            .ToList();
    }

    // 2. Find from parent class constructors
    if (classSymbol.BaseType is not null)
    {
        return ExtractParameters(classSymbol.BaseType);
    }

    return [];
}
```

### Checking IObservablePort Implementation

```csharp
private static bool ImplementsIObservablePort(INamedTypeSymbol interfaceSymbol)
{
    // Check if it is IObservablePort itself
    if (interfaceSymbol.Name == "IObservablePort")
    {
        return true;
    }

    // Check if it is an interface that inherits IObservablePort
    return interfaceSymbol.AllInterfaces.Any(i => i.Name == "IObservablePort");
}
```

---

## IPropertySymbol

```csharp
IPropertySymbol property = ...;

// Basic information
property.Name
property.Type               // Property type
property.IsIndexer          // Is indexer

// Getter/Setter
property.GetMethod          // getter (IMethodSymbol?)
property.SetMethod          // setter (IMethodSymbol?)
property.IsReadOnly         // Read-only (no setter)
property.IsWriteOnly        // Write-only (no getter)
```

---

## IParameterSymbol

```csharp
IParameterSymbol param = ...;

// Basic information
param.Name
param.Type
param.Ordinal               // Parameter order (starting from 0)

// RefKind
param.RefKind               // None, Ref, Out, In, RefReadOnlyParameter

// Default value
param.HasExplicitDefaultValue
param.ExplicitDefaultValue

// Special parameters
param.IsParams              // Is params array
param.IsOptional            // Is optional parameter
param.IsThis                // this parameter of extension method
```

### RefKind Enum

```csharp
public enum RefKind
{
    None,      // Regular parameter
    Ref,       // ref parameter
    Out,       // out parameter
    In,        // in parameter (read-only ref)
    RefReadOnlyParameter  // ref readonly parameter
}
```

---

## Using SymbolDisplayFormat

When source generators generate code, type names must be output in their complete form with the `global::` prefix. This ensures that generated code is not affected by using declarations or namespace conflicts. Our project's `SymbolDisplayFormats.GlobalQualifiedFormat` is the custom format for this purpose.

You can specify formats when converting symbols to strings:

```csharp
ITypeSymbol type = ...; // MyApp.Models.User

// Default format
type.ToDisplayString()
// -> "User"

// Full name (with namespace)
type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
// -> "global::MyApp.Models.User"

// Custom format (recommended for source generators)
var format = new SymbolDisplayFormat(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

type.ToDisplayString(format)
// -> "global::MyApp.Models.User"
```

---

## Summary at a Glance

Understanding the symbol type hierarchy allows you to cast `ISymbol` obtained from the Semantic API to the appropriate type and extract the needed information. The role each symbol type plays in ObservablePortGenerator's pipeline is as follows: `INamedTypeSymbol` analyzes class and interface relationships, `IMethodSymbol` extracts method signatures, and `IParameterSymbol` checks parameter types and passing methods.

| Symbol Type | Key Members | Purpose |
|-------------|-------------|---------|
| `INamedTypeSymbol` | Name, AllInterfaces, GetMembers() | Class analysis |
| `IMethodSymbol` | Name, ReturnType, Parameters | Method analysis |
| `IPropertySymbol` | Type, GetMethod, SetMethod | Property analysis |
| `IParameterSymbol` | Name, Type, RefKind | Parameter analysis |

| Key Query Pattern | Code |
|--------------------|------|
| All interfaces | `typeSymbol.AllInterfaces` |
| All methods | `typeSymbol.GetMembers().OfType<IMethodSymbol>()` |
| Constructors | `typeSymbol.Constructors` |
| Regular methods only | `.Where(m => m.MethodKind == MethodKind.Ordinary)` |

---

## FAQ

### Q1: What is the difference between `INamedTypeSymbol.AllInterfaces` and `INamedTypeSymbol.Interfaces`?
**A**: `Interfaces` returns only the interfaces directly declared by that type. `AllInterfaces` follows the inheritance chain upward and includes all interfaces. The reason ObservablePortGenerator uses `AllInterfaces` when checking for `IObservablePort` implementation is that it must also include cases where it is implemented indirectly through a parent interface, not directly.

### Q2: Why filter only `Ordinary` with `IMethodSymbol.MethodKind`?
**A**: `GetMembers()` returns all method-like members including constructors (`Constructor`), property accessors (`PropertyGet`/`PropertySet`), operators (`UserDefinedOperator`), etc. Since the source generator only needs to generate wrapper code for regular methods, the `MethodKind.Ordinary` filter excludes unnecessary members.

### Q3: Why define a custom `SymbolDisplayFormat`?
**A**: The built-in `FullyQualifiedFormat` outputs `System.Int32` instead of `int`, not using C# special type aliases. Functorium's `GlobalQualifiedFormat` adds `UseSpecialTypes` and `IncludeNullableReferenceTypeModifier` options so that generated code follows natural C# syntax.

---

We have learned all three core layers of Roslyn -- Syntax Tree, Semantic Model, and Symbol. In the next chapter, we learn the `IIncrementalGenerator` pattern for implementing actual source generators by combining these three layers.

-> [Part 2 Chapter 1. IIncrementalGenerator Interface](../../Part2-Core-Concepts/01-IIncrementalGenerator/)
