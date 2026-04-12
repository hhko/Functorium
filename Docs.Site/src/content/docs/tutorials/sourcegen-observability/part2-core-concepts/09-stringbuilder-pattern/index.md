---
title: "StringBuilder Pattern"
---

## Overview

In previous chapters, we secured all the data needed for code generation through symbol analysis and type extraction. Now it is time to assemble that data into actual C# source code strings. ObservablePortGenerator generates fields, constructors, wrapper methods, and logging methods for each class, so the output can reach thousands of lines. String concatenation (the `+` operator) creates a new object each time, consuming `O(n^2)` memory, whereas `StringBuilder` reuses an internal buffer for `O(n)` processing.

## Learning Objectives

### Core Learning Objectives
1. **Understand the efficiency of StringBuilder** and utilize its basic API
   - The roles of Append, AppendLine, Clear, and ToString
2. **Write readable code generation logic** using the method chaining pattern
3. **Learn mixed-use patterns with Raw String Literals**
   - Fixed parts as literals, dynamic parts with StringBuilder

---

## Why StringBuilder?

### Inefficiency of String Concatenation

```csharp
// ❌ String concatenation (inefficient)
string code = "";
code = code + "public class " + className + "\n";
code = code + "{\n";
code = code + "    // members\n";
code = code + "}\n";

// Problem: creates a new string object each time
// Memory: O(n^2) - where n is the number of concatenations
```

### Efficiency of StringBuilder

```csharp
// ✅ StringBuilder (efficient)
var sb = new StringBuilder();
sb.Append("public class ").Append(className).AppendLine();
sb.AppendLine("{");
sb.AppendLine("    // members");
sb.AppendLine("}");
string code = sb.ToString();

// Advantage: reuses internal buffer
// Memory: O(n) - linear growth
```

---

## Basic Usage

### Append vs AppendLine

```csharp
var sb = new StringBuilder();

// Append: add without line break
sb.Append("public ");
sb.Append("class ");
sb.Append("User");
// → "public class User"

// AppendLine: add with line break
sb.AppendLine("public class User");
// → "public class User\n"

// AppendLine(): add an empty line
sb.AppendLine();
// → "\n"
```

### Method Chaining

```csharp
// Improve readability with method chaining
sb.Append("public class ")
  .Append(className)
  .AppendLine()
  .AppendLine("{")
  .AppendLine("}")
  .AppendLine();
```

---

## Code Generation Patterns

### Indentation Management

```csharp
// Manual indentation (Functorium approach)
sb.AppendLine("public class UserObservable")
  .AppendLine("{")
  .AppendLine("    private readonly ILogger _logger;")  // 4-space indent
  .AppendLine()
  .AppendLine("    public UserObservable(ILogger logger)")
  .AppendLine("    {")
  .AppendLine("        _logger = logger;")  // 8-space indent
  .AppendLine("    }")
  .AppendLine("}");
```

### Indentation Helper (Optional)

```csharp
// Indentation level management
public class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private const string IndentString = "    ";  // 4 spaces

    public void Indent() => _indentLevel++;
    public void Unindent() => _indentLevel--;

    public void AppendLine(string line)
    {
        for (int i = 0; i < _indentLevel; i++)
            _sb.Append(IndentString);
        _sb.AppendLine(line);
    }
}

// Usage
var isb = new IndentedStringBuilder();
isb.AppendLine("public class User");
isb.AppendLine("{");
isb.Indent();
isb.AppendLine("private int _id;");
isb.Unindent();
isb.AppendLine("}");
```

---

## Functorium Code Generation Example

### Class Generation

```csharp
// Actual code from ObservablePortGenerator.cs
private static string GenerateObservableClassSource(
    ObservableClassInfo classInfo,
    StringBuilder sb)
{
    sb.Append(Header)
      .AppendLine()
      .AppendLine("using System.Diagnostics;")
      .AppendLine("using System.Diagnostics.Metrics;")
      .AppendLine("using Functorium.Adapters.Observabilities;")
      .AppendLine("using Functorium.Adapters.Observabilities.Naming;")
      .AppendLine("using Functorium.Abstractions.Observabilities;")
      .AppendLine()
      .AppendLine("using LanguageExt;")
      .AppendLine("using Microsoft.Extensions.Logging;")
      .AppendLine("using Microsoft.Extensions.Options;")
      .AppendLine()
      .AppendLine($"namespace {classInfo.Namespace};")
      .AppendLine()
      .AppendLine($"public class {classInfo.ClassName}Observable : {classInfo.ClassName}")
      .AppendLine("{");

    // Generate fields
    GenerateFields(sb, classInfo);

    // Generate constructor
    GenerateConstructor(sb, classInfo);

    // Add helper methods
    GenerateHelperMethods(sb, classInfo);

    // Generate methods
    foreach (var method in classInfo.Methods)
    {
        GenerateMethod(sb, classInfo, method);
    }

    sb.AppendLine("}")
      .AppendLine()
      .AppendLine($"internal static class {classInfo.ClassName}ObservableLoggers")
      .AppendLine("{");

    // Generate logging extension methods
    foreach (var method in classInfo.Methods)
    {
        GenerateLoggingMethods(sb, classInfo, method);
    }

    sb.AppendLine("}")
      .AppendLine();

    return sb.ToString();
}
```

### Field Generation

```csharp
private static void GenerateFields(StringBuilder sb, ObservableClassInfo classInfo)
{
    sb.AppendLine("    private readonly ActivitySource _activitySource;")
      .AppendLine($"    private readonly ILogger<{classInfo.ClassName}Observable> _logger;")
      .AppendLine()
      .AppendLine("    // Metrics")
      .AppendLine("    private readonly Counter<long> _requestCounter;")
      .AppendLine("    private readonly Counter<long> _responseCounter;")
      .AppendLine("    private readonly Histogram<double> _durationHistogram;")
      .AppendLine()
      .AppendLine($"    private const string RequestHandler = nameof({classInfo.ClassName});")
      .AppendLine()
      .AppendLine("    private readonly string _requestCategoryLowerCase;")
      .AppendLine()
      .AppendLine("    private readonly bool _isDebugEnabled;")
      .AppendLine("    private readonly bool _isInformationEnabled;")
      .AppendLine("    private readonly bool _isWarningEnabled;")
      .AppendLine("    private readonly bool _isErrorEnabled;")
      .AppendLine();
}
```

### Dynamic Parameter Generation

```csharp
// Generate method parameter list
private static string GenerateParameterList(List<ParameterInfo> parameters)
{
    var sb = new StringBuilder();

    for (int i = 0; i < parameters.Count; i++)
    {
        var param = parameters[i];

        if (i > 0) sb.Append(", ");

        // ref/out/in keywords
        if (param.RefKind != RefKind.None)
        {
            sb.Append(param.RefKind.ToString().ToLower())
              .Append(' ');
        }

        sb.Append(param.Type)
          .Append(' ')
          .Append(param.Name);
    }

    return sb.ToString();
}

// Usage
// Input: [("int", "id"), ("string", "name")]
// Output: "int id, string name"
```

---

## Raw String Literals (C# 11+)

### Template-Based Generation

```csharp
// Verbatim String Literal
public const string Header = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by source generator
//
//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

#nullable enable
";

// Interpolated Raw String Literals
private static string GenerateClass(string className, string @namespace)
{
    return $$"""
        namespace {{@namespace}};

        public class {{className}}Observable
        {
            // ...
        }
        """;
}
```

### Mixed Usage

```csharp
// StringBuilder + Raw String Literals mixed usage
private static string GenerateObservableClass(ObservableClassInfo classInfo)
{
    var sb = new StringBuilder();

    // Fixed part: Raw String Literal
    sb.Append("""
        // <auto-generated/>
        #nullable enable

        using System.Diagnostics;
        using Functorium.Adapters.Observabilities.Naming;
        using LanguageExt;

        """);

    // Dynamic part: StringBuilder
    sb.AppendLine($"namespace {classInfo.Namespace};");
    sb.AppendLine();
    sb.AppendLine($"public class {classInfo.ClassName}Observable");
    sb.AppendLine("{");

    // Generate methods
    foreach (var method in classInfo.Methods)
    {
        GenerateMethod(sb, method);
    }

    sb.AppendLine("}");

    return sb.ToString();
}
```

---

## Performance Optimization

### Specifying Initial Capacity

```csharp
// Specify initial capacity when the expected size is known
var sb = new StringBuilder(capacity: 4096);

// Or rough estimation
int estimatedSize = classInfo.Methods.Count * 500;  // ~500 chars per method
var sb = new StringBuilder(estimatedSize);
```

### Reuse

```csharp
// ❌ Creating new each time
foreach (var classInfo in classes)
{
    var sb = new StringBuilder();  // allocation each time
    GenerateCode(sb, classInfo);
}

// ✅ Reuse
var sb = new StringBuilder();
foreach (var classInfo in classes)
{
    sb.Clear();  // clear content only, reuse buffer
    GenerateCode(sb, classInfo);
}
```

---

## Summary at a Glance

`StringBuilder` is the core tool for assembling code in source generators. Method chaining improves readability, and when generating multiple classes, `Clear()` reuses the buffer to maximize memory efficiency. In the Functorium project, Raw String Literals are used for fixed parts and StringBuilder for dynamic parts in a mixed approach.

| Method | Purpose | Line Break |
|--------|---------|------------|
| `Append()` | Add string | None |
| `AppendLine()` | Add string + line break | Yes |
| `AppendLine("")` | Empty line | Yes |
| `Clear()` | Reset content | - |
| `ToString()` | Result string | - |

| Pattern | Description |
|---------|-------------|
| Method chaining | `.Append().Append().AppendLine()` |
| Manual indentation | Directly managed with `"    "` prefix |
| Raw String Literals | Used for fixed templates |
| Initial capacity | Specified when large output is expected |

---

## FAQ

### Q1: What is the difference between reusing `StringBuilder` with `Clear()` and creating a new one each time?
**A**: `Clear()` only erases the content while keeping the internal buffer. When generating code sequentially for multiple classes, no buffer reallocation occurs, which reduces GC pressure. One reason Functorium uses `Collect()` to gather all classes for processing is this `StringBuilder` reuse.

### Q2: What is the criterion for mixing Raw String Literals with `StringBuilder`?
**A**: **Fixed** text such as `using` statements, headers, and `#nullable enable` is more readable when written as Raw String Literals. **Dynamic** parts that change, like class names, method names, and type names, are assembled using `StringBuilder`'s `Append`/`AppendLine`. Functorium's `Header` constant is a representative example of this mixed pattern.

### Q3: How effective is specifying an initial capacity for `StringBuilder`?
**A**: The default capacity starts at 16 characters and doubles whenever needed. If the generated code is thousands of lines, expansion occurs multiple times, causing unnecessary memory copies. By estimating a rough size based on the number of methods and specifying the initial capacity, you can reduce this overhead.

---

We have learned how to assemble code line by line with `StringBuilder`. However, when hundreds of lines of generation logic are mixed into a single method, maintenance becomes difficult. In the next chapter, we will examine template design that hierarchically separates fixed and dynamic parts such as headers, fields, constructors, and methods.

-> [10. Template Design](../10-Template-Design/)
