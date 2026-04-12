---
title: "INamedTypeSymbol"
---

## Overview

In the previous chapter, we accessed symbols through `ctx.TargetSymbol` in the `transform` callback of `ForAttributeWithMetadataName`. This symbol is precisely `INamedTypeSymbol`. ObservablePortGenerator extracts all the information needed for code generation from this single symbol -- class name, namespace, implemented interfaces, and method list. In this chapter, we examine **why** each of these APIs is needed and **how** they are used in our project.

## Learning Objectives

### Core Learning Objectives
1. **Understand the basic information extraction API of INamedTypeSymbol**
   - Roles and usage of Name, ContainingNamespace, TypeKind
2. **Learn in-depth analysis using AllInterfaces and GetMembers()**
   - Interface hierarchy traversal and member filtering
3. **Learn how these APIs are combined in ObservablePortGenerator's `MapToObservableClassInfo`**

---

## What is INamedTypeSymbol?

**INamedTypeSymbol** is a symbol representing **named types** (classes, interfaces, structs, enums, delegates).

```csharp
// How to obtain in source generators
GeneratorAttributeSyntaxContext ctx = ...;

if (ctx.TargetSymbol is INamedTypeSymbol classSymbol)
{
    // Access class/interface/struct information
}
```

---

## Basic Information Extraction

To generate code, the source generator first needs to know the target class's name and namespace. Both the generated `UserRepositoryObservable` class name and the `namespace` declaration come from here.

### Name and Namespace

```csharp
INamedTypeSymbol classSymbol = ...;

// Short name
string name = classSymbol.Name;  // "UserRepository"

// Namespace
string @namespace = classSymbol.ContainingNamespace.ToString();
// "MyApp.Infrastructure.Repositories"

// Check for global namespace
bool isGlobal = classSymbol.ContainingNamespace.IsGlobalNamespace;

// Namespace handling in actual code
string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
    ? string.Empty
    : classSymbol.ContainingNamespace.ToString();
```

### Checking Type Kind

```csharp
// Check type kind with TypeKind
switch (classSymbol.TypeKind)
{
    case TypeKind.Class:
        Console.WriteLine("This is a class");
        break;
    case TypeKind.Interface:
        Console.WriteLine("This is an interface");
        break;
    case TypeKind.Struct:
        Console.WriteLine("This is a struct");
        break;
    case TypeKind.Enum:
        Console.WriteLine("This is an enum");
        break;
}
```

### Checking Modifiers

```csharp
// Accessibility
Accessibility accessibility = classSymbol.DeclaredAccessibility;
// Public, Internal, Private, etc.

// Abstract/Sealed/Static
bool isAbstract = classSymbol.IsAbstract;
bool isSealed = classSymbol.IsSealed;
bool isStatic = classSymbol.IsStatic;

// Generic
bool isGeneric = classSymbol.IsGenericType;
```

---

## Interface Analysis

For ObservablePortGenerator to find methods to wrap, it needs to know whether the target class implements `IObservablePort` and through what interface hierarchy it does so. This is where the difference between `AllInterfaces` and `Interfaces` becomes important.

### AllInterfaces vs Interfaces

```csharp
// Interfaces: only directly implemented interfaces
var directInterfaces = classSymbol.Interfaces;

// AllInterfaces: all interfaces (direct + inherited)
var allInterfaces = classSymbol.AllInterfaces;

// Example:
// public interface IUserRepository : IObservablePort { }
// public class UserRepository : IUserRepository { }

// classSymbol.Interfaces -> [IUserRepository]
// classSymbol.AllInterfaces -> [IUserRepository, IObservablePort]
```

### Checking IObservablePort Implementation

```csharp
// In ObservablePortGenerator.cs
private static bool ImplementsIObservablePort(INamedTypeSymbol interfaceSymbol)
{
    // Check if it is IObservablePort itself
    if (interfaceSymbol.Name == "IObservablePort")
    {
        return true;
    }

    // Check if it inherits IObservablePort
    return interfaceSymbol.AllInterfaces.Any(i => i.Name == "IObservablePort");
}

// Usage
var adapterInterfaces = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort);
```

---

## Member Analysis

Once the interfaces are found, their methods need to be extracted. `GetMembers()` returns all members of the type (methods, properties, fields, etc.), and `OfType<T>()` can filter to the desired kind. The "Extract methods from interface" code below is the core logic of our project.

### GetMembers()

```csharp
// Get all members
var allMembers = classSymbol.GetMembers();

// Members with a specific name
var namedMembers = classSymbol.GetMembers("GetUser");

// Filtering by type
var methods = classSymbol.GetMembers()
    .OfType<IMethodSymbol>();

var properties = classSymbol.GetMembers()
    .OfType<IPropertySymbol>();

var fields = classSymbol.GetMembers()
    .OfType<IFieldSymbol>();
```

### Extracting Methods from Interfaces

```csharp
// Actual code from ObservablePortGenerator.cs
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)  // Regular methods only
    .Select(m => new MethodInfo(
        m.Name,
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

---

## Constructor Analysis

The generated `Observable` class inherits the original class, so it must pass the parent's constructor parameters as-is. The `Constructors` property provides access to the constructor list, and each constructor's parameters are analyzed and reflected in the generated code.

### Constructors Property

```csharp
// All constructors
var constructors = classSymbol.Constructors;

// Public constructors only
var publicConstructors = classSymbol.Constructors
    .Where(c => c.DeclaredAccessibility == Accessibility.Public);

// Constructor with the most parameters
var primaryConstructor = classSymbol.Constructors
    .OrderByDescending(c => c.Parameters.Length)
    .FirstOrDefault();
```

### Primary Constructor (C# 12+)

```csharp
// Primary Constructor example
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
}

// Actual code from ConstructorParameterExtractor.cs
public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
{
    // 1. Find parameters from the class's own constructors
    var constructor = classSymbol.Constructors
        .Where(c => c.DeclaredAccessibility == Accessibility.Public)
        .OrderByDescending(c => c.Parameters.Length)
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

    // 2. Find from parent class constructors (recursive)
    if (classSymbol.BaseType is not null
        && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
    {
        return ExtractParameters(classSymbol.BaseType);
    }

    return [];
}
```

---

## Inheritance Hierarchy Analysis

### BaseType

```csharp
// Parent class
INamedTypeSymbol? baseType = classSymbol.BaseType;

// Traverse inheritance hierarchy
void PrintHierarchy(INamedTypeSymbol type, int indent = 0)
{
    Console.WriteLine(new string(' ', indent * 2) + type.Name);

    if (type.BaseType is not null
        && type.BaseType.SpecialType != SpecialType.System_Object)
    {
        PrintHierarchy(type.BaseType, indent + 1);
    }
}

// Example output:
// UserRepository
//   RepositoryBase
//     object (omitted)
```

---

## Generic Type Handling

```csharp
// Check for generic type
if (classSymbol.IsGenericType)
{
    // Type parameters (T, TValue, etc.)
    var typeParams = classSymbol.TypeParameters;

    // Type arguments (int, string, etc. - bound generics)
    var typeArgs = classSymbol.TypeArguments;

    // Original definition (unbounded)
    var original = classSymbol.OriginalDefinition;
}

// Example: List<int>
// TypeParameters: [T]
// TypeArguments: [int]
// OriginalDefinition: List<T>
```

---

## Practical Usage: Creating ObservableClassInfo

We have examined individual APIs so far. Now let us see the full flow of how these APIs are combined in the `MapToObservableClassInfo` method to create a single `ObservableClassInfo`. This method is used as the `transform` callback of `ForAttributeWithMetadataName`.

```csharp
private static ObservableClassInfo MapToObservableClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    // 1. Verify type symbol
    if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
    {
        return ObservableClassInfo.None;
    }

    cancellationToken.ThrowIfCancellationRequested();

    // 2. Extract basic information
    string className = classSymbol.Name;
    string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
        ? string.Empty
        : classSymbol.ContainingNamespace.ToString();

    // 3. Extract methods from interfaces
    var methods = classSymbol.AllInterfaces
        .Where(ImplementsIObservablePort)
        .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
        .Where(m => m.MethodKind == MethodKind.Ordinary)
        .Select(m => MapToMethodInfo(m))
        .ToList();

    // 4. No generation needed if no methods
    if (methods.Count == 0)
    {
        return ObservableClassInfo.None;
    }

    // 5. Extract constructor parameters
    var baseConstructorParameters =
        ConstructorParameterExtractor.ExtractParameters(classSymbol);

    // 6. Return result
    return new ObservableClassInfo(
        @namespace, className, methods, baseConstructorParameters);
}
```

---

## Summary at a Glance

`INamedTypeSymbol` is the core tool for extracting type information in source generators. In our project, `Name` and `ContainingNamespace` determine the generated class's name and namespace, `AllInterfaces` verifies `IObservablePort` implementation, `GetMembers()` extracts methods to wrap, and `Constructors` passes parent constructor parameters.

| Property/Method | Purpose | Return |
|-----------------|---------|--------|
| `Name` | Short name | string |
| `ContainingNamespace` | Namespace | INamespaceSymbol |
| `TypeKind` | Type kind | TypeKind |
| `AllInterfaces` | All interfaces | ImmutableArray |
| `Interfaces` | Directly implemented interfaces | ImmutableArray |
| `GetMembers()` | All members | ImmutableArray |
| `Constructors` | Constructors | ImmutableArray |
| `BaseType` | Parent class | INamedTypeSymbol? |

---

## FAQ

### Q1: Is it safe to compare `IObservablePort` directly by name in `AllInterfaces`?
**A**: Name-based comparison can cause incorrect matching if an interface with the same name exists in a different namespace. A safer approach is to compare with `SymbolEqualityComparer.Default` or verify using the full metadata name (including `ContainingNamespace`). In Functorium, since there are no name conflicts within the project, the concise name comparison is used.

### Q2: Why is the `ObservableClassInfo.None` pattern used instead of returning `null`?
**A**: Since the `transform` callback's return type is `ObservableClassInfo` (a value type), `null` cannot be returned. The empty object pattern expresses invalid results, which are later removed with the `.Where(x => x != ObservableClassInfo.None)` filter. This pattern cleanly handles null treatment for value types.

### Q3: Why check `ContainingNamespace.IsGlobalNamespace`?
**A**: Types defined without a `namespace` declaration belong to the global namespace. In this case, `ContainingNamespace.ToString()` returns `"<global namespace>"`, and using this directly in the generated code's `namespace` declaration would cause a compilation error. When in the global namespace, it is treated as an empty string to omit the `namespace` declaration.

---

We have understood how to extract class and interface level information with `INamedTypeSymbol`. In the next chapter, we go one level deeper and examine `IMethodSymbol`, which analyzes each method's signature (name, parameters, return type). This information becomes the basis for generating logging code and pipeline wrappers.

-> [06. IMethodSymbol](../06-IMethodSymbol/)
