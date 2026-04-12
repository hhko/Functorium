---
title: "Type Extraction"
---

## Overview

In the previous chapter, we converted return types to consistent strings like `"global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"` using `SymbolDisplayFormat`. However, what the logging method's success callback needs is not this entire string, but only the second type parameter, `User`. Since the `FinT<IO, T>` pattern is the return type convention followed by all adapter methods in Functorium, this type extraction is an essential step in code generation.

## Learning Objectives

### Core Learning Objectives
1. **Understand TypeExtractor's** string parsing logic
   - Extraction method based on the first comma and last angle bracket
2. **Learn how to handle various type forms**
   - Simple types, collections, tuples, nested generics, non-generics
3. **Understand the trade-offs** between string parsing and symbol API approaches

---

## Why Type Extraction Is Needed

Observability code requires the **actual value type** of the return type:

```csharp
// Original method
public FinT<IO, User> GetUserAsync(int id);

// Generated logging code
void LogSuccess(User result, double elapsed)
//              ^^^^
// We need User, not FinT<IO, User>!
```

---

## TypeExtractor Class

### Full Implementation

```csharp
// Generators/ObservablePortGenerator/TypeExtractor.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// Utility for extracting specific type parameters from generic types
/// </summary>
public static class TypeExtractor
{
    /// <summary>
    /// Extracts the second type parameter.
    /// Example: FinT&lt;IO, User&gt; → User
    /// </summary>
    public static string ExtractSecondTypeParameter(string genericTypeName)
    {
        // Input: "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"
        // Output: "global::MyApp.User"

        int firstComma = genericTypeName.IndexOf(',');
        if (firstComma == -1)
        {
            return genericTypeName;  // Return original if not generic
        }

        int lastAngle = genericTypeName.LastIndexOf('>');
        if (lastAngle == -1)
        {
            return genericTypeName;  // Return original if malformed
        }

        // After the first comma, before the last >
        return genericTypeName
            .Substring(firstComma + 1, lastAngle - firstComma - 1)
            .Trim();
    }
}
```

### Behavior Example

```
Input: "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"
                                      ^                 ^
                                firstComma         lastAngle

Extracted: ", global::MyApp.User>"
           → "global::MyApp.User" (after Trim)
```

---

## Handling Various Type Forms

### Simple Types

```csharp
// Input: "global::LanguageExt.FinT<global::LanguageExt.IO, int>"
// Output: "int"

// Input: "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.String>"
// Output: "global::System.String"
```

### Collection Types

```csharp
// Input: "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<global::MyApp.User>>"
// Output: "global::System.Collections.Generic.List<global::MyApp.User>"
```

### Tuple Types

```csharp
// Input: "global::LanguageExt.FinT<global::LanguageExt.IO, (int, string)>"
// Output: "(int, string)"
```

### Nested Generics

```csharp
// Input: "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>>"
// Output: "global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>"
```

---

## Practical Usage

### Generating Logging Methods

```csharp
// In ObservablePortGenerator.cs
private static void GenerateMethod(
    StringBuilder sb,
    ObservableClassInfo classInfo,
    MethodInfo method)
{
    // Extract actual type from return type
    string actualReturnType = ExtractActualReturnType(method.ReturnType);
    // "global::MyApp.User"

    // Generate logging callback signature
    sb.AppendLine($"    private void Log{method.Name}Success(");
    sb.AppendLine($"        string requestHandler,");
    sb.AppendLine($"        string requestHandlerMethod,");
    sb.AppendLine($"        {actualReturnType} result,");  // ← Extracted type used
    sb.AppendLine($"        double elapsedMilliseconds) {{ ... }}");
}

private static string ExtractActualReturnType(string returnType)
{
    return TypeExtractor.ExtractSecondTypeParameter(returnType);
}
```

### Generated Code

```csharp
// Original interface
public interface IUserRepository : IObservablePort
{
    FinT<IO, User> GetUserAsync(int id);
    FinT<IO, IEnumerable<User>> GetUsersAsync();
}

// Generated code
public class UserRepositoryObservable : UserRepository
{
    // Success logging for GetUserAsync - uses User type
    private void LogGetUserAsyncSuccess(
        string requestHandler,
        string requestHandlerMethod,
        global::MyApp.User result,        // ← Extracted
        double elapsedMilliseconds) { }

    // Success logging for GetUsersAsync - uses IEnumerable<User> type
    private void LogGetUsersAsyncSuccess(
        string requestHandler,
        string requestHandlerMethod,
        global::System.Collections.Generic.IEnumerable<global::MyApp.User> result,  // ← Extracted
        double elapsedMilliseconds) { }
}
```

---

## Alternative: Using ITypeSymbol Directly

Instead of string parsing, you can also use the symbol API:

```csharp
// Approach using ITypeSymbol directly
ITypeSymbol returnType = method.ReturnType;

if (returnType is INamedTypeSymbol namedType
    && namedType.IsGenericType
    && namedType.TypeArguments.Length >= 2)
{
    // Direct access to the second type argument
    ITypeSymbol secondTypeArg = namedType.TypeArguments[1];
    string actualType = secondTypeArg.ToDisplayString(
        SymbolDisplayFormats.GlobalQualifiedFormat);
}
```

### String Parsing vs Symbol API

| Approach | Pros | Cons |
|----------|------|------|
| String parsing | Simple, uses already-converted strings | May fail with complex types |
| Symbol API | Accurate, type-safe | Requires additional processing |

**Functorium** chose string parsing because the types have already been converted to strings.

---

## Edge Case Handling

### Non-Generic Types

```csharp
// Input: "void"
// indexOf(',') == -1 → returns original

public static string ExtractSecondTypeParameter(string genericTypeName)
{
    int firstComma = genericTypeName.IndexOf(',');
    if (firstComma == -1)
    {
        return genericTypeName;  // Not generic
    }
    // ...
}
```

### Single Type Parameter

```csharp
// Input: "global::LanguageExt.Fin<global::MyApp.User>"
// indexOf(',') == -1 → returns original
```

### Nullable Types

```csharp
// Input: "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User?>"
// Output: "global::MyApp.User?"

// The ? marker is preserved
```

---

## Test Cases

```csharp
public class TypeExtractorTests
{
    [Theory]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, int>",
        "int")]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>",
        "global::MyApp.User")]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<int>>",
        "global::System.Collections.Generic.List<int>")]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, (int, string)>",
        "(int, string)")]
    [InlineData(
        "void",
        "void")]  // Non-generic returns original
    public void ExtractSecondTypeParameter_Should_Work(
        string input,
        string expected)
    {
        var actual = TypeExtractor.ExtractSecondTypeParameter(input);
        actual.ShouldBe(expected);
    }
}
```

---

## Summary at a Glance

`TypeExtractor.ExtractSecondTypeParameter` is a utility that extracts the actual value type `T` from the `FinT<IO, T>` pattern. Although it takes a simple approach of parsing based on the first comma and last angle bracket in the string, it correctly handles nested generics and tuples.

| Scenario | Input | Output |
|----------|-------|--------|
| Simple value type | `FinT<IO, int>` | `int` |
| Reference type | `FinT<IO, User>` | `User` |
| Collection | `FinT<IO, List<User>>` | `List<User>` |
| Tuple | `FinT<IO, (int, string)>` | `(int, string)` |
| Non-generic | `void` | `void` |

---

## FAQ

### Q1: Why not use `INamedTypeSymbol.TypeArguments[1]` instead of string parsing?
**A**: In the ObservablePortGenerator, type information has already been converted to strings via `SymbolDisplayFormat`, so string parsing is simpler than going back to the symbol API. Symbol API access is only available in the `transform` stage, and by the code generation stage, only strings remain.

### Q2: Why does the `firstComma` and `lastAngle` strategy work correctly with nested generic types?
**A**: In `FinT<IO, Dictionary<string, List<int>>>`, the first comma is the one after `IO`, and the last `>` is the outermost closing bracket. The string between these two positions is exactly the full content of the second type parameter. The commas and angle brackets of inner generics are contained within this range and are correctly preserved.

### Q3: How is a non-generic return type (e.g., `void`) handled when provided as input?
**A**: If the result of `IndexOf(',')` is `-1`, it means there is no comma, so the input is determined to be non-generic and the original string is returned as-is. This serves as a safe fallback for methods that do not follow the `FinT<IO, T>` pattern.

---

With symbol analysis and type extraction, we have secured all the data needed for code generation. In the next chapter, we will examine usage patterns for `StringBuilder`, the tool for assembling this data into actual C# code strings.

→ [09. StringBuilder Pattern](../09-StringBuilder-Pattern/)
