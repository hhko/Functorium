---
title: "Collection Type Handling"
---

## Overview

In observability, simply knowing "a user list was queried" is not enough. In actual production environments, "how many items were returned" becomes a key metric for performance analysis and anomaly detection. ObservablePortGenerator automatically adds `Count` or `Length` tags when a return type or parameter is a collection. However, when a collection is contained inside a tuple, the tuple itself has no Count property, so it must be treated as an exception.

## Learning Objectives

### Core Learning Objectives
1. **Collection type detection method**
   - Identifying `List<T>`, `Dictionary<K,V>`, arrays, etc. through pattern matching
2. **Automatic Count/Length field generation**
   - Generating the appropriate size access expression based on collection type
3. **Exception handling for collections inside tuples**
   - Why and how to ignore inner collections in tuple return types

---

## The Need for Collection Type Handling

Collection size information is an important metric in observability code.

```csharp
// Original method
public virtual FinT<IO, List<User>> GetUsersAsync() => ...;

// Generated Pipeline code
public override FinT<IO, List<User>> GetUsersAsync() =>
    FinT.lift<IO, List<User>>(
        // ...
        from __ in IO.lift(() =>
        {
            // <- Record collection size as a tag
            activityContext?.SetTag("response.result.count", result?.Count ?? 0);
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

---

## CollectionTypeHelper Implementation

### Collection Pattern Definition

```csharp
// Generators/ObservablePortGenerator/CollectionTypeHelper.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// Helper class for checking whether a type is a collection
/// </summary>
public static class CollectionTypeHelper
{
    private static readonly string[] CollectionTypePatterns = [
        // Regular namespaces
        "System.Collections.Generic.List<",
        "System.Collections.Generic.IList<",
        "System.Collections.Generic.ICollection<",
        "System.Collections.Generic.IEnumerable<",
        "System.Collections.Generic.IReadOnlyList<",
        "System.Collections.Generic.IReadOnlyCollection<",
        "System.Collections.Generic.HashSet<",
        "System.Collections.Generic.Dictionary<",
        "System.Collections.Generic.IDictionary<",
        "System.Collections.Generic.IReadOnlyDictionary<",
        "System.Collections.Generic.Queue<",
        "System.Collections.Generic.Stack<",

        // global:: prefixed versions
        "global::System.Collections.Generic.List<",
        "global::System.Collections.Generic.IList<",
        // ... (same patterns)
    ];
}
```

### Collection Type Check

```csharp
/// <summary>
/// Checks whether a type is a collection with a Count property.
/// Tuple types are not treated as collections even if they contain collections internally.
/// </summary>
public static bool IsCollectionType(string typeFullName)
{
    if (string.IsNullOrEmpty(typeFullName))
        return false;

    // Tuple types are not treated as collections
    if (IsTupleType(typeFullName))
        return false;

    // Check array types (e.g., int[], string[])
    if (typeFullName.Contains("[]"))
        return true;

    // Check collection type patterns
    return CollectionTypePatterns.Any(pattern => typeFullName.Contains(pattern));
}
```

---

## Tuple Exception Handling

### Why Exclude Tuples?

Even when a collection exists inside a tuple, recording the Count of the **tuple itself** is meaningless.

```csharp
// Return type: (int Id, List<string> Tags)

// ❌ Incorrect handling - recognizing tuple as collection
result?.Count  // Tuple has no Count!

// ✅ Correct handling - do not generate Count for tuples
// Count field not generated
```

### Tuple Type Check

```csharp
/// <summary>
/// Checks whether a type is a tuple.
/// </summary>
public static bool IsTupleType(string typeFullName)
{
    if (string.IsNullOrEmpty(typeFullName))
        return false;

    // C# tuple syntax: (int Id, string Name)
    if (typeFullName.StartsWith("(") && typeFullName.EndsWith(")"))
        return true;

    // ValueTuple type
    if (typeFullName.Contains("System.ValueTuple") ||
        typeFullName.Contains("global::System.ValueTuple"))
        return true;

    return false;
}
```

---

## Count Expression Generation

### Count vs Length

```csharp
/// <summary>
/// Generates a Count access expression for collection types.
/// Arrays use Length, others use Count.
/// </summary>
public static string? GetCountExpression(string variableName, string typeFullName)
{
    if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(typeFullName))
        return null;

    if (!IsCollectionType(typeFullName))
        return null;

    // Arrays use Length
    if (typeFullName.Contains("[]"))
        return $"{variableName}?.Length ?? 0";

    // Other collections use Count
    return $"{variableName}?.Count ?? 0";
}
```

### Expression Result Examples

| Type | Expression |
|------|------------|
| `List<User>` | `result?.Count ?? 0` |
| `string[]` | `result?.Length ?? 0` |
| `Dictionary<K, V>` | `result?.Count ?? 0` |
| `IEnumerable<T>` | `result?.Count ?? 0` |

---

## Field Name Generation

### Request Parameter Fields

```csharp
/// <summary>
/// Generates a field name for a request parameter.
/// Example: "ms" -> "request.params.ms", "name" -> "request.params.name"
/// Dynamic fields use the request.params.{name} format to distinguish from static fields.
/// </summary>
public static string GetRequestFieldName(string parameterName)
{
    if (string.IsNullOrEmpty(parameterName))
        return parameterName;

    // Convert to lowercase using snake_case + dot format
    return $"request.params.{parameterName.ToLowerInvariant()}";
}

/// <summary>
/// Generates a Count field name for a request parameter.
/// Example: "orders" -> "request.params.orders.count"
/// </summary>
/// <returns>Count field name. null if parameterName is empty</returns>
public static string? GetRequestCountFieldName(string parameterName)
{
    if (string.IsNullOrEmpty(parameterName))
        return null;

    // Convert to lowercase using snake_case + dot format
    return $"request.params.{parameterName.ToLowerInvariant()}.count";
}
```

### Response Fields

```csharp
/// <summary>
/// Generates a field name for response results.
/// Returns: "response.result"
/// </summary>
public static string GetResponseFieldName()
{
    return "response.result";
}

/// <summary>
/// Generates a Count field name for response results.
/// Returns: "response.result.count"
/// </summary>
public static string GetResponseCountFieldName()
{
    return "response.result.count";
}
```

---

## Usage in Code Generation

### Return Type Handling

```csharp
private static void AppendResultTagging(
    StringBuilder sb,
    string innerType)
{
    if (CollectionTypeHelper.IsCollectionType(innerType))
    {
        string? countExpr = CollectionTypeHelper.GetCountExpression("result", innerType);
        string countField = CollectionTypeHelper.GetResponseCountFieldName();

        sb.AppendLine($"            activityContext?.SetTag(\"{countField}\", {countExpr});");
    }

    sb.AppendLine("            activityContext?.Dispose();");
}
```

### Parameter Handling

```csharp
private static void AppendParameterTags(
    StringBuilder sb,
    IMethodSymbol method)
{
    foreach (var param in method.Parameters)
    {
        string paramType = param.Type.ToDisplayString(
            SymbolDisplayFormats.GlobalQualifiedFormat);

        string fieldName = CollectionTypeHelper.GetRequestFieldName(param.Name);
        sb.AppendLine($"            activityContext?.SetTag(\"{fieldName}\", {param.Name});");

        // Add Count tag for collection parameters
        if (CollectionTypeHelper.IsCollectionType(paramType))
        {
            string? countField = CollectionTypeHelper.GetRequestCountFieldName(param.Name);
            string? countExpr = CollectionTypeHelper.GetCountExpression(param.Name, paramType);

            if (countField is not null && countExpr is not null)
            {
                sb.AppendLine($"            activityContext?.SetTag(\"{countField}\", {countExpr});");
            }
        }
    }
}
```

---

## Generated Result Examples

### Collection Parameter

```csharp
// Original
public virtual FinT<IO, int> ProcessItems(List<string> items) => ...;

// Generated code
public override FinT<IO, int> ProcessItems(List<string> items) =>
    FinT.lift<IO, int>(
        from activityContext in IO.lift(() => CreateActivity("ProcessItems"))
        from _ in IO.lift(() =>
        {
            activityContext?.SetTag("request.params.items", items);
            activityContext?.SetTag("request.params.items.count", items?.Count ?? 0);  // <- Count tag
            StartActivity(activityContext);
            return Unit.Default;
        })
        from result in FinTToIO(base.ProcessItems(items))
        from __ in IO.lift(() =>
        {
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

### Collection Return Type

```csharp
// Original
public virtual FinT<IO, List<User>> GetUsers() => ...;

// Generated code
public override FinT<IO, List<User>> GetUsers() =>
    FinT.lift<IO, List<User>>(
        // ...
        from __ in IO.lift(() =>
        {
            activityContext?.SetTag("response.result.count", result?.Count ?? 0);  // <- Count tag
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

### Array Return Type

```csharp
// Original
public virtual FinT<IO, string[]> GetNames() => ...;

// Generated code
// ...
activityContext?.SetTag("response.result.count", result?.Length ?? 0);  // <- Length used
```

---

## Test Scenarios

### Collection Parameter Test

```csharp
[Fact]
public Task Should_Generate_CollectionCountFields_WithCollectionParameters()
{
    string input = """
        [GenerateObservablePort]
        public class DataRepository : IObservablePort
        {
            public virtual FinT<IO, int> ProcessItems(List<string> items)
                => FinT<IO, int>.Succ(items?.Count ?? 0);
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify request.params.items.count field
    actual.ShouldContain("request.params.items.count");
    actual.ShouldContain("items?.Count ?? 0");

    return Verify(actual);
}
```

### Tuple Return Type Test

```csharp
[Fact]
public Task Should_Not_Generate_Count_ForTupleContainingCollection()
{
    // Count should not be generated even if tuple contains a collection
    string input = """
        [GenerateObservablePort]
        public class UserRepository : IObservablePort
        {
            public virtual FinT<IO, (int Id, List<string> Tags)> GetUserWithTags()
                => FinT<IO, (int Id, List<string> Tags)>.Succ((1, new List<string>()));
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify response.result.count is not generated
    actual.ShouldNotContain("response.result.count");

    return Verify(actual);
}
```

### Array-Containing Tuple Test

```csharp
[Fact]
public Task Should_Not_Generate_Length_ForTupleContainingArray()
{
    string input = """
        [GenerateObservablePort]
        public class StudentRepository : IObservablePort
        {
            public virtual FinT<IO, (string Name, int[] Scores)> GetStudentScores()
                => FinT<IO, (string Name, int[] Scores)>.Succ(("Student", new[] { 90, 85 }));
        }
        """;

    string? actual = _sut.Generate(input);

    // Verify response.result.count (Length) is not generated
    actual.ShouldNotContain("response.result.count");

    return Verify(actual);
}
```

---

## Type Behavior Summary

| Return Type | Count/Length Generated | Expression |
|-------------|----------------------|------------|
| `List<T>` | O | `?.Count ?? 0` |
| `T[]` | O | `?.Length ?? 0` |
| `Dictionary<K, V>` | O | `?.Count ?? 0` |
| `(int, string)` | X | - |
| `(int, List<T>)` | X | - |
| `(T, T[])` | X | - |
| `int` | X | - |
| `string` | X | - |

---

## Summary at a Glance

`CollectionTypeHelper` unifies collection detection, tuple exception handling, and Count/Length expression generation into a single utility. Its pattern matching approach correctly recognizes Fully Qualified Names including the `global::` prefix, and field names follow the `request.params.{name}.count` and `response.result.count` conventions.

---

## FAQ

### Q1: `IEnumerable<T>` is recognized as a collection, but wouldn't calling `Count()` trigger a full enumeration?
**A**: `CollectionTypeHelper` detects types through pattern matching, but the actual generated code uses the `?.Count ?? 0` expression. This calls the `ICollection<T>.Count` property (O(1)), not LINQ's `Count()` extension method (O(n)). However, for types that only implement pure `IEnumerable<T>`, the `Count` property does not exist and a compilation error may occur, so using concrete collection types is recommended in practice.

### Q2: Why is Count not generated when a collection exists inside a tuple?
**A**: Since the tuple itself has no `Count` property, an expression like `result?.Count` would cause a compilation error. To access individual elements inside the tuple, you would need to decompose them like `result.Item2?.Count`, which greatly increases the complexity of the generator while offering limited value from an observability perspective.

### Q3: Why are `global::` prefixed versions added separately to the `CollectionTypePatterns` array?
**A**: Depending on Roslyn's `SymbolDisplayFormat`, type strings can appear in two forms: `List<T>` or `global::System.Collections.Generic.List<T>`. For `Contains()` pattern matching to work correctly in both cases, both patterns must be included.

---

With collection Count fields added, the total number of parameters needed for logging increases. The next section covers .NET `LoggerMessage.Define`'s 6-parameter limit and the fallback strategy when this limit is exceeded.

-> [04. LoggerMessage.Define Limits](../04-LoggerMessage-Limits/)
