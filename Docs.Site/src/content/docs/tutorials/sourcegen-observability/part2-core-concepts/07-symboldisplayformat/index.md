---
title: "SymbolDisplayFormat"
---

## Overview

In the previous chapter, we used `ToDisplayString` to extract type strings from the `ReturnType` and `Parameters` of `IMethodSymbol`. However, the problem is that the same `User` type can be represented as different strings depending on context: `"User"`, `"MyApp.User"`, `"global::MyApp.User"`, and so on. In incremental caching, these differences translate directly to cache misses. **SymbolDisplayFormat** defines the rules for converting types to strings, ensuring that the same type is always represented as the same string. The Functorium project addresses this by defining a custom format called `SymbolDisplayFormats.GlobalQualifiedFormat` and using it consistently across all type conversions.

## Learning Objectives

### Core Learning Objectives
1. **Understand the role of SymbolDisplayFormat** and its relationship to deterministic output
   - Why the default `ToDisplayString()` is insufficient
2. **Understand why Functorium's GlobalQualifiedFormat** chose each option
   - The rationale behind `UseSpecialTypes`, `EscapeKeywordIdentifiers`, and `IncludeNullableReferenceTypeModifier`
3. **Learn the pattern** of using a consistent format across the entire project

---

## Why Is SymbolDisplayFormat Important?

The same type can be **represented differently**:

```csharp
// All the same type but different strings
"User"
"MyApp.User"
"MyApp.Models.User"
"global::MyApp.Models.User"

// Problem: Cache invalidation
// In Build A: "User" → generates UserObservable.g.cs
// In Build B: "MyApp.User" → recognized as a different file → cache miss
```

By using **SymbolDisplayFormat**, you can always obtain strings in **the same format**.

---

## Basic Usage

### ToDisplayString()

```csharp
ITypeSymbol type = ...;

// Default format (varies by context)
string name1 = type.ToDisplayString();
// "User" or "MyApp.User" (depending on context)

// Fully qualified format (recommended)
string name2 = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
// "global::MyApp.Models.User"

// Minimally qualified format
string name3 = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
// "User"
```

---

## Built-in Formats

### FullyQualifiedFormat

```csharp
SymbolDisplayFormat.FullyQualifiedFormat

// Characteristics:
// - Includes global:: prefix
// - Includes full namespace
// - Includes generic type parameters

// Examples:
// List<int> → "global::System.Collections.Generic.List<global::System.Int32>"
// User → "global::MyApp.Models.User"
```

### MinimallyQualifiedFormat

```csharp
SymbolDisplayFormat.MinimallyQualifiedFormat

// Characteristics:
// - Shortest form
// - May vary depending on using directives

// Examples:
// List<int> → "List<int>"
// User → "User"
```

### CSharpErrorMessageFormat

```csharp
SymbolDisplayFormat.CSharpErrorMessageFormat

// Characteristics:
// - Suitable for error messages
// - Human-readable form

// Examples:
// List<int> → "System.Collections.Generic.List<int>"
```

---

## Custom Format Configuration

When the built-in formats do not exactly match the project's requirements, you can combine options to create a custom format. Functorium's `GlobalQualifiedFormat` was created this way. Below, we examine each option category and then review the rationale behind the project's actual choices.

### SymbolDisplayFormat Constructor

```csharp
var customFormat = new SymbolDisplayFormat(
    globalNamespaceStyle: ...,       // global:: prefix
    typeQualificationStyle: ...,     // Namespace display style
    genericsOptions: ...,            // Generics display style
    memberOptions: ...,              // Member display style
    parameterOptions: ...,           // Parameter display style
    miscellaneousOptions: ...        // Miscellaneous options
);
```

### GlobalNamespaceStyle

```csharp
// global:: prefix control
SymbolDisplayGlobalNamespaceStyle.Omitted      // Omit
SymbolDisplayGlobalNamespaceStyle.Included     // Include (recommended)
SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining
```

### TypeQualificationStyle

```csharp
// Namespace display style
SymbolDisplayTypeQualificationStyle.NameOnly
// "User"

SymbolDisplayTypeQualificationStyle.NameAndContainingTypes
// "Models.User" (for nested classes)

SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
// "MyApp.Models.User" (recommended)
```

### GenericsOptions

```csharp
// Generics display style
SymbolDisplayGenericsOptions.None
// "List" (type parameters omitted)

SymbolDisplayGenericsOptions.IncludeTypeParameters
// "List<T>" or "List<int>"

SymbolDisplayGenericsOptions.IncludeTypeConstraints
// "List<T> where T : class"

SymbolDisplayGenericsOptions.IncludeVariance
// "IEnumerable<out T>"
```

### MiscellaneousOptions

```csharp
// Miscellaneous options
SymbolDisplayMiscellaneousOptions.UseSpecialTypes
// "int" instead of "System.Int32" (or vice versa)

SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
// Escapes keywords (@class, @event, etc.)

SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
// Displays "string?"
```

---

## Functorium's GlobalQualifiedFormat

Now let's examine how the above options are combined in our project. The key point is that the rationale for each option choice is documented in code comments. The `global::` prefix prevents namespace conflicts, `UseSpecialTypes` improves readability of generated code by using C# keywords like `int` and `string`, and `IncludeNullableReferenceTypeModifier` preserves nullable information.

### SymbolDisplayFormats.cs

```csharp
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// SymbolDisplayFormat definition for deterministic code generation
/// </summary>
public static class SymbolDisplayFormats
{
    /// <summary>
    /// Global qualified format - used for deterministic code generation
    /// </summary>
    public static readonly SymbolDisplayFormat GlobalQualifiedFormat = new(
        // Include global:: prefix
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,

        // Include full namespace
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,

        // Include generic type parameters
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,

        // Miscellaneous options
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |      // Use int, string, etc.
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |  // Escape keywords
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier  // Display ?
    );
}
```

### Usage Example

```csharp
// Parameter type
string paramType = param.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
// "global::System.Int32" or "int" (due to UseSpecialTypes)

// Return type
string returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
// "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.Models.User>"
```

---

## Special Type Handling

### UseSpecialTypes Option

```csharp
// With UseSpecialTypes (default)
"int"
"string"
"bool"
"object"

// Without UseSpecialTypes
"global::System.Int32"
"global::System.String"
"global::System.Boolean"
"global::System.Object"
```

### Nullable Types

```csharp
// With IncludeNullableReferenceTypeModifier
"global::System.String?"
"global::MyApp.Models.User?"

// Without IncludeNullableReferenceTypeModifier
"global::System.String"
"global::MyApp.Models.User"
```

---

## Deterministic Output Verification

### Consistent Output for the Same Type

```csharp
// Same type in different contexts
var type1 = compilation1.GetTypeByMetadataName("MyApp.Models.User");
var type2 = compilation2.GetTypeByMetadataName("MyApp.Models.User");

string name1 = type1.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
string name2 = type2.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);

// Must always be identical
Debug.Assert(name1 == name2);
// → "global::MyApp.Models.User"
```

### Verification via Tests

```csharp
[Fact]
public void TypeDisplayString_Should_Be_Deterministic()
{
    // Arrange
    string sourceCode = """
        namespace MyApp.Models;
        public class User { }
        """;

    // Act - Compile twice
    var type1 = CompileAndGetType(sourceCode, "MyApp.Models.User");
    var type2 = CompileAndGetType(sourceCode, "MyApp.Models.User");

    // Assert
    var format = SymbolDisplayFormats.GlobalQualifiedFormat;
    type1.ToDisplayString(format).ShouldBe(type2.ToDisplayString(format));
}
```

---

## Caveats

The most common mistake is mixing different formats at different points in the code. If you convert parameter types with the default format and return types with `FullyQualifiedFormat`, the same type may be represented differently, which can invalidate caching.

### 1. Use a Consistent Format

```csharp
// ❌ Do not mix formats
var paramTypes = method.Parameters
    .Select(p => p.Type.ToDisplayString())  // Default format
    .ToList();

var returnType = method.ReturnType.ToDisplayString(
    SymbolDisplayFormat.FullyQualifiedFormat);  // Different format

// ✅ Always use the same format
var format = SymbolDisplayFormats.GlobalQualifiedFormat;

var paramTypes = method.Parameters
    .Select(p => p.Type.ToDisplayString(format))
    .ToList();

var returnType = method.ReturnType.ToDisplayString(format);
```

### 2. Define as a Reusable Constant

```csharp
// ✅ Define as a constant for reuse
public static class SymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat GlobalQualifiedFormat = ...;
}

// Usage
type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
```

---

## Summary at a Glance

`SymbolDisplayFormat` is a foundational tool for deterministic code generation. The Functorium project defines a custom format that prevents namespace conflicts with the `global::` prefix, ensures readability with `UseSpecialTypes`, and preserves nullable information with `IncludeNullableReferenceTypeModifier`. The most important principle is to use this format consistently throughout the entire project.

| Format | Example Output | Purpose |
|--------|---------------|---------|
| Default | "User" | Display only (not recommended) |
| FullyQualifiedFormat | "global::MyApp.User" | Deterministic output |
| MinimallyQualifiedFormat | "User" | Concise display |
| Custom GlobalQualifiedFormat | "global::MyApp.User" | **Recommended for source generators** |

| Option | Description |
|--------|-------------|
| `GlobalNamespaceStyle.Included` | global:: prefix |
| `TypeQualificationStyle.NameAndContainingTypesAndNamespaces` | Full path |
| `GenericsOptions.IncludeTypeParameters` | Generic parameters |
| `MiscellaneousOptions.UseSpecialTypes` | int, string, etc. |

---

## FAQ

### Q1: What is the difference between `FullyQualifiedFormat` and Functorium's `GlobalQualifiedFormat`?
**A**: `FullyQualifiedFormat` displays `int` as `System.Int32`, whereas Functorium's `GlobalQualifiedFormat` keeps `int` as-is through the `UseSpecialTypes` option. It also adds `IncludeNullableReferenceTypeModifier` to preserve nullable information like `string?`. As a result, the generated code follows more natural C# syntax.

### Q2: Why should a single `SymbolDisplayFormat` be shared across the entire project?
**A**: Converting the same type with different formats creates inconsistencies like `"int"` vs `"global::System.Int32"`. When these inconsistencies enter the data model, `Equals` comparisons differ, invalidating incremental caching, and creating consistency issues where the same type is represented differently within the generated code.

### Q3: In what situations is the `EscapeKeywordIdentifiers` option needed?
**A**: When identifiers with names identical to C# keywords (e.g., `@class`, `@event`) are used in type names or namespaces, this option automatically adds the `@` prefix to generate compilable code. Without this option, the generated code may conflict with keywords and cause compilation errors.

---

Now we understand how to convert types to consistent strings using `SymbolDisplayFormat`. However, from a return type like `FinT<IO, User>`, what we actually need is only the second type parameter, `User`. In the next chapter, we will explore techniques for extracting specific type parameters from generic types.

→ [08. Type Extraction](../08-Type-Extraction/)
