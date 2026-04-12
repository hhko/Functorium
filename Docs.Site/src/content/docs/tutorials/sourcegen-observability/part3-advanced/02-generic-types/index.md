---
title: "Generic Type Handling"
---

## Overview

All Adapter methods in Functorium use the `FinT<IO, T>` return type. When ObservablePortGenerator generates Pipeline code, it must precisely extract the inner type `T`, as in `FinT.lift<IO, T>(...)`. The problem is that `T` can be a simple type, but it can also be a nested generic like `Dictionary<string, List<int>>`. `TypeExtractor` safely parses even these complex types through a bracket counting algorithm.

## Learning Objectives

### Core Learning Objectives
1. **Extracting T from FinT<IO, T>**
   - How to precisely separate the second type parameter
2. **Parsing nested generic types**
   - An algorithm that tracks `<>` depth with bracket counting
3. **Using the TypeExtractor utility**
   - How extracted types are used in actual code generation

---

## The Need for Generic Type Handling

Adapter methods use the `FinT<IO, T>` return type. The inner type `T` must be extracted during Pipeline code generation.

```csharp
// Original method
public virtual FinT<IO, User> GetUserAsync(int id) => ...;

// Generated Pipeline code
public override FinT<IO, User> GetUserAsync(int id) =>
    FinT.lift<IO, User>(  // <- T = User extraction needed
        from activityContext in IO.lift(() => CreateActivity(...))
        // ...
    );
```

---

## TypeExtractor Implementation

### Full Code

```csharp
// Generators/ObservablePortGenerator/TypeExtractor.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// Utility class for extracting inner types from generic types
/// </summary>
internal static class TypeExtractor
{
    /// <summary>
    /// Extracts the second type parameter B from a FinT<A, B> form.
    /// B can be a generic type (e.g., List<T>), so nested <> handling is supported.
    /// </summary>
    public static string ExtractSecondTypeParameter(string returnType)
    {
        if (string.IsNullOrEmpty(returnType))
        {
            return returnType;
        }

        int finTStart = returnType.IndexOf("FinT<", StringComparison.Ordinal);
        if (finTStart == -1)
        {
            return returnType;
        }

        // Start after FinT<
        int start = finTStart + 5;

        // Find comma to skip first type parameter (A)
        int? commaIndex = FindFirstTypeParameterSeparator(returnType, start);

        if (!commaIndex.HasValue)
        {
            return returnType;
        }

        // Start after comma (skip whitespace)
        start = SkipWhitespace(returnType, commaIndex.Value + 1);

        // Find end of second type parameter (B)
        int? end = FindTypeParameterEnd(returnType, start);

        if (!end.HasValue)
        {
            return returnType;
        }

        return returnType.Substring(start, end.Value - start).Trim();
    }

    // ... (helper methods)
}
```

---

## Parsing Algorithm

### Bracket Counting

**Bracket counting** is needed to correctly parse nested generics.

```
Input: FinT<IO, Dictionary<string, List<int>>>
      ^   ^  ^        ^      ^   ^  ^^^^^^^
      |   |  |        |      |   |     |
      |   |  |        |      |   +-----+--- Count: 3->2->1
      |   |  |        |      +------------- Count: 2
      |   |  |        +-------------------- Count: 1 (comma ignored)
      |   |  +----------------------------- Count: 1 (split here!)
      |   +-------------------------------- Count: 1
      +------------------------------------ Count: 0->1

Result: Dictionary<string, List<int>>
```

### Helper Method - Finding Comma

```csharp
/// <summary>
/// Finds the position of the comma separating the first and second type parameters.
/// Ignores commas inside nested generic types.
/// </summary>
private static int? FindFirstTypeParameterSeparator(string text, int startIndex)
{
    int bracketCount = 1; // Start at 1 because of < in FinT<

    for (int i = startIndex; i < text.Length; i++)
    {
        char c = text[i];

        if (c == '<')
        {
            bracketCount++;
        }
        else if (c == '>')
        {
            bracketCount--;

            if (bracketCount == 0)
            {
                // Reached end of FinT but no comma found
                return null;
            }
        }
        else if (c == ',' && bracketCount == 1)
        {
            // Found comma at the first level
            return i;
        }
    }

    return null;
}
```

### Helper Method - Finding End Position

```csharp
/// <summary>
/// Finds the end position of a type parameter.
/// </summary>
private static int? FindTypeParameterEnd(string text, int startIndex)
{
    int bracketCount = 1; // Start at 1 because of parent FinT<

    for (int i = startIndex; i < text.Length; i++)
    {
        char c = text[i];

        if (c == '<')
        {
            bracketCount++;
        }
        else if (c == '>')
        {
            bracketCount--;

            if (bracketCount == 0)
            {
                return i;
            }
        }
    }

    return null;
}
```

---

## Supported Type Patterns

### 1. Simple Types

```csharp
// Input
"FinT<IO, string>"
"FinT<IO, int>"
"FinT<IO, bool>"

// Output
"string"
"int"
"bool"
```

### 2. Generic Collections

```csharp
// Input
"FinT<IO, List<int>>"
"FinT<IO, Dictionary<string, int>>"

// Output
"List<int>"
"Dictionary<string, int>"
```

### 3. Nested Generics

```csharp
// Input
"FinT<IO, Dictionary<string, List<int>>>"
"FinT<IO, Result<Data<User<string>>>>"

// Output
"Dictionary<string, List<int>>"
"Result<Data<User<string>>>"
```

### 4. Fully Qualified Names

```csharp
// Input (actual use in source generator)
"global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<DataResult>>"

// Output
"global::System.Collections.Generic.List<DataResult>"
```

### 5. Array Types

```csharp
// Input
"FinT<IO, string[]>"
"FinT<IO, int[]>"

// Output
"string[]"
"int[]"
```

### 6. Nullable Types

```csharp
// Input
"FinT<IO, int?>"
"FinT<IO, string?>"

// Output
"int?"
"string?"
```

### 7. Tuple Types

```csharp
// Input
"FinT<IO, (string Name, int Age)>"
"FinT<IO, ((int A, int B), string C)>"
"FinT<IO, (List<int> Numbers, string Name)>"

// Output
"(string Name, int Age)"
"((int A, int B), string C)"
"(List<int> Numbers, string Name)"
```

---

## Usage in Code Generation

### During Method Generation

```csharp
private static void AppendMethodOverride(
    StringBuilder sb,
    IMethodSymbol method,
    string className,
    int methodIndex)
{
    // Extract inner type from return type
    string returnType = method.ReturnType.ToDisplayString(
        SymbolDisplayFormats.GlobalQualifiedFormat);

    string innerType = TypeExtractor.ExtractSecondTypeParameter(returnType);

    // Used as T in FinT.lift<IO, T>
    sb.Append($"        global::LanguageExt.FinT.lift<global::LanguageExt.IO, {innerType}>(");
    // ...
}
```

### Generated Code Example

```csharp
// Original: FinT<IO, List<User>> GetUsers()
// Extracted type: List<User>

public override FinT<IO, List<User>> GetUsers() =>
    FinT.lift<IO, List<User>>(  // <- Extracted type used
        from activityContext in IO.lift(() => CreateActivity("GetUsers"))
        from _ in IO.lift(() => StartActivity(activityContext))
        from result in FinTToIO(base.GetUsers())
        from __ in IO.lift(() =>
        {
            // For collections, add Count field
            activityContext?.SetTag("result.Count", result?.Count ?? 0);
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

---

## Edge Case Handling

### When FinT Is Not Present

```csharp
// Input
"string"

// TypeExtractor behavior
if (finTStart == -1)  // FinT< not found
{
    return returnType;  // Return original as-is
}

// Output
"string"
```

### Empty String

```csharp
// Input
""
null

// TypeExtractor behavior
if (string.IsNullOrEmpty(returnType))
{
    return returnType;
}

// Output
""
null
```

---

## Test Scenarios

### Simple Type Test

```csharp
[Fact]
public Task Should_Extract_SimpleType()
{
    string input = """
        [GenerateObservablePort]
        public class DataRepository : IObservablePort
        {
            public virtual FinT<IO, int> GetNumber() => FinT<IO, int>.Succ(42);
            public virtual FinT<IO, string> GetText() => FinT<IO, string>.Succ("hello");
            public virtual FinT<IO, bool> GetFlag() => FinT<IO, bool>.Succ(true);
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify FinT.lift<IO, int>, FinT.lift<IO, string>, FinT.lift<IO, bool>
    return Verify(actual);
}
```

### Collection Type Test

```csharp
[Fact]
public Task Should_Extract_CollectionType()
{
    string input = """
        public class User { public int Id { get; set; } }

        [GenerateObservablePort]
        public class UserRepository : IObservablePort
        {
            public virtual FinT<IO, List<User>> GetUsers()
                => FinT<IO, List<User>>.Succ(new List<User>());
            public virtual FinT<IO, string[]> GetNames()
                => FinT<IO, string[]>.Succ(Array.Empty<string>());
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify List<User>, string[] extraction
    return Verify(actual);
}
```

### Complex Generic Test

```csharp
[Fact]
public Task Should_Extract_ComplexGenericType()
{
    string input = """
        [GenerateObservablePort]
        public class DataRepository : IObservablePort
        {
            public virtual FinT<IO, Dictionary<string, List<int>>> GetComplexData()
                => FinT<IO, Dictionary<string, List<int>>>.Succ(
                    new Dictionary<string, List<int>>());
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify Dictionary<string, List<int>> extraction
    return Verify(actual);
}
```

### Tuple Type Test

```csharp
[Fact]
public Task Should_Extract_TupleType()
{
    string input = """
        [GenerateObservablePort]
        public class UserRepository : IObservablePort
        {
            public virtual FinT<IO, (int Id, string Name)> GetUserInfo()
                => FinT<IO, (int Id, string Name)>.Succ((1, "Test"));
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify (int Id, string Name) extraction
    return Verify(actual);
}
```

---

## Summary at a Glance

The core of TypeExtractor is the bracket counting algorithm. It increments the count when encountering `<` and decrements when encountering `>`, separating the first and second type parameters at the comma where the count is 1. This approach works accurately regardless of nesting depth.

| Pattern | Input Example | Output |
|---------|---------------|--------|
| Simple type | `FinT<IO, int>` | `int` |
| Collection | `FinT<IO, List<T>>` | `List<T>` |
| Nested generic | `FinT<IO, Dict<K, List<V>>>` | `Dict<K, List<V>>` |
| Array | `FinT<IO, T[]>` | `T[]` |
| Tuple | `FinT<IO, (A, B)>` | `(A, B)` |
| Nullable | `FinT<IO, T?>` | `T?` |

---

## FAQ

### Q1: Are there cases where the bracket counting algorithm fails?
**A**: For return types that are not in `FinT<IO, T>` form (e.g., plain `string`), the `FinT<` pattern is not found, and the original string is returned as-is. This is intentional behavior, and since all methods processed by ObservablePortGenerator are in `FinT<IO, T>` form, no issues actually occur in practice.

### Q2: Does type extraction work correctly with Fully Qualified Names (including `global::` prefix)?
**A**: Yes. `TypeExtractor` finds the position of the `FinT<` pattern within the string using `IndexOf`, then performs bracket counting from that point onward. Even with long Fully Qualified Names like `global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<DataResult>>`, the second type parameter is extracted accurately.

### Q3: Can `TypeExtractor` be replaced with `IMethodSymbol.ReturnType`'s `TypeArguments`?
**A**: Roslyn's `INamedTypeSymbol.TypeArguments` allows direct access to type parameters at the symbol level. However, since ObservablePortGenerator generates code based on strings already converted by `SymbolDisplayFormat`, the string parsing approach maintains consistency with the rest of the pipeline.

---

Now that type extraction is possible, we will learn how to determine whether an extracted type is a collection and automatically generate Count/Length tags.

-> [03. Collection Types](../03-Collection-Types/)
