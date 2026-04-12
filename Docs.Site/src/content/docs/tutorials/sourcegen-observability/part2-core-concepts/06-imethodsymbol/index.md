---
title: "IMethodSymbol"
---

## Overview

In the previous chapter, we retrieved the interface member list through `INamedTypeSymbol`. Among those members, the ones that cast to `IMethodSymbol` are the direct targets of code generation. ObservablePortGenerator determines logging method names from each method's name, constructs `LoggerMessage.Define` type arguments from the parameter list, and extracts `T` from `FinT<IO, T>` in the return type to generate success logging signatures. This chapter examines the `IMethodSymbol` API that forms the foundation for all these processes.

## Learning Objectives

### Core Learning Objectives
1. **Analyze method signatures using IMethodSymbol's basic properties**
   - Roles of Name, ReturnType, Parameters
2. **Understand why only regular methods are filtered using MethodKind**
   - Why getters, setters, constructors, etc. must be excluded
3. **Learn the pattern of utilizing parameter information for logging code generation**
   - LoggerMessage.Define's parameter slot limitation and corresponding strategies

---

## What is IMethodSymbol?

**IMethodSymbol** is a symbol representing **methods, constructors, destructors, operators**, etc.

```csharp
// Getting method symbols from an interface
var methods = interfaceSymbol.GetMembers()
    .OfType<IMethodSymbol>()
    .Where(m => m.MethodKind == MethodKind.Ordinary);
```

---

## Basic Information Extraction

### Name and Kind

```csharp
IMethodSymbol method = ...;

// Method name
string name = method.Name;  // "GetUserAsync"

// Method kind
MethodKind kind = method.MethodKind;
// Ordinary: regular method
// Constructor: constructor
// PropertyGet: getter
// PropertySet: setter
// etc.
```

### Filtering Regular Methods Only with MethodKind

An interface's `GetMembers()` returns all members including property getters/setters and event add/remove accessors. In source generators, only **regular methods (Ordinary)** that contain actual business logic are needed, so we filter by `MethodKind`. Key values include `Ordinary` (regular method), `Constructor` (constructor), `PropertyGet`/`PropertySet` (property accessors), `EventAdd`/`EventRemove` (event accessors), etc.

```csharp
// Filter regular methods only in source generators
.Where(m => m.MethodKind == MethodKind.Ordinary)
```

### Modifiers

```csharp
// Accessibility
Accessibility accessibility = method.DeclaredAccessibility;

// Is static
bool isStatic = method.IsStatic;

// Virtual/Abstract/Override
bool isVirtual = method.IsVirtual;
bool isAbstract = method.IsAbstract;
bool isOverride = method.IsOverride;

// Async
bool isAsync = method.IsAsync;

// Extension method
bool isExtension = method.IsExtensionMethod;
```

---

## Return Type Analysis

### ReturnType

```csharp
IMethodSymbol method = ...;

// Return type symbol
ITypeSymbol returnType = method.ReturnType;

// Is void
bool returnsVoid = method.ReturnsVoid;

// Type name (deterministic format)
string returnTypeName = method.ReturnType.ToDisplayString(
    SymbolDisplayFormats.GlobalQualifiedFormat);
// "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.Models.User>"
```

### Extracting Actual Type from Return Type

In observability code, `T` from `FinT<IO, T>` is needed:

```csharp
// TypeExtractor.cs
public static class TypeExtractor
{
    /// <summary>
    /// Extracts User from FinT&lt;IO, User&gt;.
    /// </summary>
    public static string ExtractSecondTypeParameter(string genericTypeName)
    {
        // "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"
        // -> "global::MyApp.User"

        int firstComma = genericTypeName.IndexOf(',');
        if (firstComma == -1) return genericTypeName;

        int lastAngle = genericTypeName.LastIndexOf('>');
        if (lastAngle == -1) return genericTypeName;

        // String after first comma, before last >
        return genericTypeName
            .Substring(firstComma + 1, lastAngle - firstComma - 1)
            .Trim();
    }
}

// Usage example
string returnType = "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>";
string actualType = TypeExtractor.ExtractSecondTypeParameter(returnType);
// -> "global::MyApp.User"
```

---

## Parameter Analysis

Method parameter information is used for two purposes. First, it constructs the generated wrapper method's signature. Second, it determines whether to include parameter values in logging message templates. In particular, due to `LoggerMessage.Define`'s maximum 6 parameter limit, the code generation strategy differs based on the number of method parameters.

### Parameters

```csharp
IMethodSymbol method = ...;

// Parameter list
ImmutableArray<IParameterSymbol> parameters = method.Parameters;

foreach (var param in parameters)
{
    Console.WriteLine($"Name: {param.Name}");
    Console.WriteLine($"Type: {param.Type}");
    Console.WriteLine($"RefKind: {param.RefKind}");
    Console.WriteLine($"Order: {param.Ordinal}");
}
```

### IParameterSymbol Details

```csharp
IParameterSymbol param = ...;

// Basic information
string name = param.Name;           // "userId"
ITypeSymbol type = param.Type;      // int
int ordinal = param.Ordinal;        // 0, 1, 2...

// RefKind
RefKind refKind = param.RefKind;
// None: regular parameter
// Ref: ref parameter
// Out: out parameter
// In: in parameter

// Default value
bool hasDefault = param.HasExplicitDefaultValue;
object? defaultValue = param.ExplicitDefaultValue;

// Special
bool isParams = param.IsParams;      // params array
bool isOptional = param.IsOptional;  // optional parameter
bool isThis = param.IsThis;          // this in extension methods
```

---

## Practical Usage: Creating MethodInfo

This is the point where the Name, Parameters, and ReturnType examined earlier are combined into one. The code below is the actual code from our project that creates a `MethodInfo` data model from `IMethodSymbol`.

```csharp
// Extracting method information in ObservablePortGenerator.cs
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)
    .Select(m => new MethodInfo(
        // 1. Method name
        m.Name,

        // 2. Parameter list
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),

        // 3. Return type
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

### MethodInfo Record

```csharp
// Generators/ObservablePortGenerator/MethodInfo.cs
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

// Generators/ObservablePortGenerator/ParameterInfo.cs
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

## Parameter Usage in Logging Code Generation

Let us look specifically at how parameter analysis impacts code generation. `LoggerMessage.Define` supports a maximum of 6 type parameters, and observability logging uses 4 slots by default (handler name, method name, layer, status info). Therefore, only 2 slots are available for method parameters, and the code generation strategy varies based on this limitation.

### Handling Based on Parameter Count

```csharp
// LoggerMessage.Define supports maximum 6 parameters
const int MaxLoggerMessageParameters = 6;

// Default 4 parameters:
// - requestHandler (class name)
// - requestHandlerMethod (method name)
// - layer (Adapter)
// - response-related (elapsed, status)

// Slots available for method parameters: 6 - 4 = 2

int methodParameterCount = method.Parameters.Length;
bool canUseLoggerMessageDefine = methodParameterCount <= 2;

if (canUseLoggerMessageDefine)
{
    // Generate high-performance logging code
    GenerateLoggerMessageDefine(method);
}
else
{
    // Fallback: regular logging
    GenerateFallbackLogging(method);
}
```

### Parameter String Generation

```csharp
// Parameter list for method signature
string parameterList = string.Join(", ",
    method.Parameters.Select(p =>
        $"{GetRefKindKeyword(p.RefKind)}{p.Type} {p.Name}".Trim()));

// Parameter list for invocation
string argumentList = string.Join(", ",
    method.Parameters.Select(p =>
        $"{GetRefKindKeyword(p.RefKind)}{p.Name}".Trim()));

// ref, out, in keyword handling
static string GetRefKindKeyword(RefKind refKind) => refKind switch
{
    RefKind.Ref => "ref ",
    RefKind.Out => "out ",
    RefKind.In => "in ",
    _ => ""
};
```

---

## Generic Methods

```csharp
IMethodSymbol method = ...;

// Is generic method
bool isGeneric = method.IsGenericMethod;

// Type parameters
var typeParams = method.TypeParameters;  // [T, TResult]

// Type arguments (when bound)
var typeArgs = method.TypeArguments;  // [int, string]

// Original definition
var original = method.OriginalDefinition;
```

---

## Method Invocation Code Generation

```csharp
// Example of generated pipeline method
public new FinT<IO, User> GetUserAsync(int userId)
{
    long startTimestamp = Stopwatch.GetTimestamp();

    return ExecuteWithActivity(
        RequestHandler,           // "UserRepository"
        nameof(GetUserAsync),     // Method name
        FinTToIO(base.GetUserAsync(userId)),  // Actual invocation
        () => LogRequest(userId), // Request logging
        LogResponseSuccess,       // Success logging
        LogResponseFailure,       // Failure logging
        startTimestamp);
}
```

---

## Summary at a Glance

`IMethodSymbol` provides all the information needed for method-level code generation. In our project, `Name` determines logging method names, `Parameters` determines signatures and logging templates, and `T` is extracted from `FinT<IO, T>` in `ReturnType` to determine the success response type. `MethodKind == Ordinary` filtering is essential to exclude accessors like getters/setters.

| Property/Method | Purpose | Return |
|-----------------|---------|--------|
| `Name` | Method name | string |
| `MethodKind` | Method kind | MethodKind |
| `ReturnType` | Return type | ITypeSymbol |
| `ReturnsVoid` | Is void return | bool |
| `Parameters` | Parameter list | ImmutableArray |
| `IsAsync` | Is async | bool |
| `IsStatic` | Is static | bool |

| Parameter Property | Purpose |
|--------------------|---------|
| `Name` | Parameter name |
| `Type` | Parameter type |
| `RefKind` | ref/out/in status |
| `Ordinal` | Order (from 0) |

---

## FAQ

### Q1: What is the impact of `LoggerMessage.Define`'s 6-parameter limit on code generation?
**A**: ObservablePortGenerator uses 4 slots by default (handler name, method name, layer, status info), leaving only 2 slots for method parameters. When parameters exceed 2, a branch is needed to fall back from high-performance `LoggerMessage.Define` to regular logging code, and this decision is based on `IMethodSymbol.Parameters.Length`.

### Q2: What other `MethodKind` values exist besides `MethodKind.Ordinary`?
**A**: `Constructor`, `PropertyGet`, `PropertySet`, `EventAdd`, `EventRemove`, `UserDefinedOperator`, `Conversion`, `Destructor`, etc. Since `GetMembers().OfType<IMethodSymbol>()` returns all these kinds, the `MethodKind.Ordinary` filter is essential to select only regular methods as wrapping targets in source generators.

### Q3: How are parameters with `RefKind` other than `None` handled in code generation?
**A**: `ref`, `out`, `in` parameters must include the corresponding keyword in both the method signature and call site. During code generation, the keyword string is obtained with `GetRefKindKeyword(p.RefKind)` and prepended to the type to produce compilable code.

---

When extracting parameter types and return types from `IMethodSymbol`, we used `ToDisplayString`. However, the same type can be expressed differently depending on the format: `"User"`, `"MyApp.User"`, `"global::MyApp.User"`, etc. In the next chapter, we examine `SymbolDisplayFormat` for maintaining this representation consistently.

-> [07. SymbolDisplayFormat](../07-SymbolDisplayFormat/)
