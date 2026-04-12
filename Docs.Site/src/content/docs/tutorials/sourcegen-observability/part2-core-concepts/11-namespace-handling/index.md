---
title: "Namespace Handling"
---

## Overview

Generated code must be in the same namespace as the original class for partial class or inheritance to work correctly. If the namespace is omitted, compilation errors occur, and if classes with the same name exist in different namespaces, generated filenames will collide. This chapter covers practical techniques for extracting information from `ContainingNamespace`, safely handling the global namespace, and preventing filename collisions.

## Learning Objectives

### Core Learning Objectives
1. **Extracting information from ContainingNamespace**
   - Checking `IsGlobalNamespace` and converting to string
2. **Handling the global namespace**
   - Safe code generation for types defined without a namespace declaration
3. **Using namespace suffixes in filenames**
   - Collision prevention by extracting the last segment with `LastIndexOf('.')`

---

## Namespace Extraction

### Basic Extraction

```csharp
INamedTypeSymbol classSymbol = ...;

// Get namespace from ContainingNamespace
INamespaceSymbol namespaceSymbol = classSymbol.ContainingNamespace;

// Convert to string
string @namespace = namespaceSymbol.ToString();
// "MyApp.Infrastructure.Repositories"
```

### Global Namespace Handling

```csharp
// Special handling needed for global namespace
string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
    ? string.Empty  // Return empty string
    : classSymbol.ContainingNamespace.ToString();

// What is the global namespace?
// Types defined without a namespace declaration
// Example:
// public class GlobalClass { }  // global namespace

// namespace MyApp;
// public class NamespacedClass { }  // MyApp namespace
```

---

## Namespace Declaration Generation

### File-scoped Namespace (C# 10+)

```csharp
// When a namespace exists
if (!string.IsNullOrEmpty(@namespace))
{
    sb.AppendLine($"namespace {@namespace};")
      .AppendLine();
}

// Generated result:
// namespace MyApp.Infrastructure.Repositories;
//
// public class UserRepositoryObservable : UserRepository
// {
// }
```

### Global Namespace Handling

```csharp
// When in the global namespace, omit namespace declaration
if (string.IsNullOrEmpty(@namespace))
{
    // Define class directly without namespace declaration
    sb.AppendLine($"public class {className}Pipeline : {className}")
      .AppendLine("{");
}
else
{
    sb.AppendLine($"namespace {@namespace};")
      .AppendLine()
      .AppendLine($"public class {className}Pipeline : {className}")
      .AppendLine("{");
}
```

---

## Using Namespaces in Filenames

### Preventing Filename Collisions

```csharp
// Problem: same class name in different namespaces
// MyApp.Repositories.UserRepository
// MyApp.Services.UserRepository

// Collision with the same filename
// UserRepositoryObservable.g.cs  (which one?)
```

### Namespace Suffix Extraction

```csharp
// Actual code from ObservablePortGenerator.cs
private static void Generate(
    SourceProductionContext context,
    ImmutableArray<ObservableClassInfo> pipelineClasses)
{
    foreach (var pipelineClass in pipelineClasses)
    {
        // Extract the last part of the namespace
        string namespaceSuffix = string.Empty;
        if (!string.IsNullOrEmpty(pipelineClass.Namespace))
        {
            var lastDotIndex = pipelineClass.Namespace.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                // "MyApp.Infrastructure.Repositories" -> "Repositories"
                namespaceSuffix = pipelineClass.Namespace
                    .Substring(lastDotIndex + 1) + ".";
            }
        }

        // Generate filename
        string fileName = $"{namespaceSuffix}{pipelineClass.ClassName}Observable.g.cs";
        // "Repositories.UserRepositoryObservable.g.cs"

        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }
}
```

### Examples

```
Input Class                                Generated Filename
===========                                ==================
MyApp.Repositories.UserRepository          Repositories.UserRepositoryObservable.g.cs
MyApp.Services.UserRepository              Services.UserRepositoryObservable.g.cs
MyApp.Data.UserRepository                  Data.UserRepositoryObservable.g.cs
GlobalClass (global namespace)             GlobalClassObservable.g.cs
```

---

## Nested Namespace Handling

### Deep Namespaces

```csharp
// Input: "A.B.C.D.E"
// lastDotIndex: 7 (the '.' just before 'E')
// Suffix: "E."

string @namespace = "A.B.C.D.E";
var lastDotIndex = @namespace.LastIndexOf('.');
string suffix = @namespace.Substring(lastDotIndex + 1) + ".";
// suffix = "E."
```

### Simple Namespaces

```csharp
// Input: "MyApp"
// lastDotIndex: -1 (no dot)
// Suffix: none (empty string)

string @namespace = "MyApp";
var lastDotIndex = @namespace.LastIndexOf('.');
if (lastDotIndex >= 0)
{
    // This block is not executed
}
// suffix = ""
```

---

## Type References with Namespaces

### Using the global:: Prefix

```csharp
// When referencing types in generated code

// ❌ Risky: can conflict with user code
sb.AppendLine("using System;");
sb.AppendLine("ArgumentNullException.ThrowIfNull(value);");

// ✅ Safe: explicit with global:: prefix
sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(value);");
```

### Generated Code Example

```csharp
// Safe type references
sb.AppendLine("    private readonly global::System.Diagnostics.ActivityContext _parentContext;")
  .AppendLine()
  .AppendLine("    public void Validate()")
  .AppendLine("    {")
  .AppendLine("        global::System.ArgumentNullException.ThrowIfNull(_logger);")
  .AppendLine("    }");
```

---

## Using Aliases

### Abbreviating Long Namespaces

```csharp
// Add using alias to generated code
sb.AppendLine("using ObservabilityFields = Functorium.Adapters.Observabilities.ObservabilityFields;");

// Usage
sb.AppendLine("    var eventId = ObservabilityFields.EventIds.Adapter.AdapterRequest;");

// global:: alternative
sb.AppendLine("    var eventId = global::Functorium.Adapters.Observabilities.ObservabilityFields.EventIds.Adapter.AdapterRequest;");
// → Too long, reduces readability
```

---

## Test Scenarios

### Namespace-Related Tests

```csharp
public class NamespaceTests
{
    [Fact]
    public Task Should_Handle_Simple_Namespace()
    {
        string input = """
            namespace MyApp;

            [GenerateObservablePort]
            public class UserRepository : IObservablePort { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // Generated file: UserRepositoryObservable.g.cs
        // Namespace in code: MyApp;
    }

    [Fact]
    public Task Should_Handle_Deep_Namespace()
    {
        string input = """
            namespace A.B.C.D.E;

            [GenerateObservablePort]
            public class UserRepository : IObservablePort { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // Generated file: E.UserRepositoryObservable.g.cs
        // Namespace in code: A.B.C.D.E;
    }

    [Fact]
    public Task Should_Handle_Global_Namespace()
    {
        string input = """
            [GenerateObservablePort]
            public class UserRepository : IObservablePort { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // Generated file: UserRepositoryObservable.g.cs
        // Namespace in code: none (global)
    }
}
```

---

## Summary at a Glance

Here is a summary of the key strategies for namespace handling.

| Scenario | Handling Approach |
|----------|-------------------|
| Regular namespace | `namespace X.Y.Z;` declaration |
| Global namespace | Omit namespace declaration |
| Filename collision | Use last namespace segment as suffix |
| Type references | Use `global::` prefix |
| Long namespaces | Use using aliases |

| Method | Purpose |
|--------|---------|
| `IsGlobalNamespace` | Check if global |
| `ToString()` | Full namespace string |
| `LastIndexOf('.')` | Extract last segment |

---

## FAQ

### Q1: How are classes defined in the global namespace handled?
**A**: After checking with the `IsGlobalNamespace` property, if `true`, the `namespace` declaration is omitted and the class is defined directly. Since there is no namespace suffix in the filename, it takes the form `{ClassName}Observable.g.cs`.

### Q2: Does extracting only the last segment with `LastIndexOf('.')` completely resolve collisions?
**A**: Cases where even the last segment is identical (e.g., `A.Repositories.UserRepository` and `B.Repositories.UserRepository`) can still collide. In practice, this is rare, but for greater safety, including the full namespace in the filename can be considered. ObservablePortGenerator adopted this strategy because the last-segment approach provides an appropriate balance between readability and safety.

### Q3: Why is the `global::` prefix used in generated code?
**A**: If user code contains a class named `System`, `System.ArgumentNullException` would reference the user's `System` class, causing a compilation error. Writing `global::System.ArgumentNullException` always references the global `System` namespace precisely, fundamentally preventing such conflicts.

---

With namespace handling complete, it is now time to cover the final principle of code generation. Deterministic output, which guarantees the same output for the same input, is a key factor affecting incremental builds, source control, and CI/CD.

-> [12. Deterministic Output](../12-Deterministic-Output/)
