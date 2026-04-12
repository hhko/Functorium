---
title: "Constructor Handling"
---

## Overview

When a source generator creates an Observable class, it inherits from the original class, so the parent's constructor must be called correctly. However, considering C# 12's Primary Constructor, optimal selection among multiple constructors, and name collisions between Observable's own parameters and ones like `logger`, constructor handling requires analysis beyond simple code copying. ObservablePortGenerator systematically solves this problem through two utilities: `ConstructorParameterExtractor` and `ParameterNameResolver`.

## Learning Objectives

### Core Learning Objectives
1. **Primary Constructor support (C# 12+)**
   - How to identify Primary Constructors in Roslyn and extract their parameters
2. **Extracting parent class constructor parameters**
   - Search priority for constructors of the target class and parent class
3. **Resolving parameter name conflicts**
   - Automatic renaming when Observable's reserved names overlap with parent parameters

---

## The Need for Constructor Handling

Observable classes inherit from the original class. If the parent class has constructor parameters, they must be forwarded.

```csharp
// Original class (Primary Constructor)
[GenerateObservablePort]
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
    public FinT<IO, User> GetUserAsync(int id) => ...;
}

// Generated Observable class
public class UserRepositoryObservable : UserRepository
{
    // Must forward the parent's logger parameter!
    public UserRepositoryObservable(
        ActivitySource activitySource,
        ILogger<UserRepositoryObservable> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        ILogger<UserRepository> baseLogger)  // <- logger for parent
        : base(baseLogger)  // <- Parent constructor call
    {
        // ...
    }
}
```

---

## ConstructorParameterExtractor

### Full Implementation

```csharp
// Generators/ObservablePortGenerator/ConstructorParameterExtractor.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// Utility class for extracting constructor parameters from a class
/// </summary>
internal static class ConstructorParameterExtractor
{
    /// <summary>
    /// Extracts constructor parameters from the target class or its parent class.
    ///
    /// Priority:
    /// 1. Target class's own constructor (if it has parameters)
    /// 2. Parent class's constructor (if the target class has no parameterized constructor)
    /// </summary>
    public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
    {
        // 1. Check target class's constructor (priority)
        var targetConstructorParams = TryExtractFromTargetClass(classSymbol);
        if (targetConstructorParams.Count > 0)
        {
            return targetConstructorParams;
        }

        // 2. Check parent class's constructor
        return TryExtractFromBaseClass(classSymbol);
    }

    private static List<ParameterInfo> TryExtractFromTargetClass(INamedTypeSymbol classSymbol)
    {
        var constructors = GetPublicConstructors(classSymbol);
        var selectedConstructor = SelectBestConstructor(constructors);

        if (selectedConstructor != null && selectedConstructor.Parameters.Length > 0)
        {
            return ConvertToParameterInfoList(selectedConstructor.Parameters);
        }

        return new List<ParameterInfo>();
    }

    private static List<ParameterInfo> TryExtractFromBaseClass(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.BaseType == null || classSymbol.BaseType.SpecialType == SpecialType.System_Object)
        {
            return new List<ParameterInfo>();
        }

        var constructors = GetPublicConstructors(classSymbol.BaseType);
        var selectedConstructor = SelectBestConstructor(constructors);

        if (selectedConstructor != null && selectedConstructor.Parameters.Length > 0)
        {
            return ConvertToParameterInfoList(selectedConstructor.Parameters);
        }

        return new List<ParameterInfo>();
    }

    /// <summary>
    /// Selects the most appropriate constructor.
    /// Priority: 1. Primary constructor (C# 12+), 2. Constructor with the most parameters
    /// </summary>
    private static IMethodSymbol? SelectBestConstructor(List<IMethodSymbol> constructors)
    {
        // 1st priority: Primary constructor
        var primaryConstructor = constructors.FirstOrDefault(IsPrimaryConstructor);
        if (primaryConstructor != null)
        {
            return primaryConstructor;
        }

        // 2nd priority: Constructor with the most parameters
        return constructors
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();
    }

    private static bool IsPrimaryConstructor(IMethodSymbol constructor)
    {
        var syntaxReferences = constructor.DeclaringSyntaxReferences;
        if (syntaxReferences.Length == 0) return false;

        var syntax = syntaxReferences[0].GetSyntax();
        return syntax is TypeDeclarationSyntax typeDecl && typeDecl.ParameterList != null;
    }
}
```

### Execution Flow

```
1. TryExtractFromTargetClass: Search for public constructors of the target class
   |
   +- Constructor found -> SelectBestConstructor -> Extract parameters -> Return
   |
   +- No constructor (or no parameters)
       |
       v
2. TryExtractFromBaseClass: Search for public constructors of the parent class
   |
   +- Reached object -> Return empty list
   |
   +- Constructor found -> SelectBestConstructor -> Extract parameters -> Return
```

---

## Primary Constructor Support

### C# 12 Primary Constructor

```csharp
// Primary Constructor form
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
    // logger is available throughout the class
}

// Equivalent regular constructor
public class UserRepository : IObservablePort
{
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ILogger<UserRepository> logger)
    {
        _logger = logger;
    }
}
```

### Handling in Roslyn

Primary Constructors are included in `Constructors`:

```csharp
var constructor = classSymbol.Constructors
    .FirstOrDefault();

// In the case of Primary Constructor
// - Parameters.Length > 0
// - MethodKind == MethodKind.Constructor (same)
```

---

## Resolving Parameter Name Conflicts

### Problem Scenario

```csharp
// Original class
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort { }

// Generated Observable (conflict!)
public class UserRepositoryObservable : UserRepository
{
    public UserRepositoryObservable(
        ILogger<UserRepositoryObservable> logger,  // Observable's logger
        ILogger<UserRepository> logger)          // ❌ Same name!
        : base(logger)
    {
    }
}
```

### ParameterNameResolver

```csharp
// Generators/ObservablePortGenerator/ParameterNameResolver.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// Utility class for resolving parameter name conflicts
/// </summary>
internal static class ParameterNameResolver
{
    /// <summary>
    /// Returns a new name if it conflicts with a reserved name.
    /// </summary>
    public static string ResolveName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return parameterName;
        }

        // Starts with underscore: _logger -> baseLogger
        if (parameterName.StartsWith("_"))
        {
            string nameWithoutUnderscore = parameterName.Substring(1);
            return $"{ObservableGeneratorConstants.NameConflictPrefix}{char.ToUpper(nameWithoutUnderscore[0])}{nameWithoutUnderscore.Substring(1)}";
        }

        // Conflicts with reserved name: logger -> baseLogger
        if (ObservableGeneratorConstants.ReservedParameterNames.Contains(parameterName))
        {
            return $"{ObservableGeneratorConstants.NameConflictPrefix}{char.ToUpper(parameterName[0])}{parameterName.Substring(1)}";
        }

        return parameterName;
    }

    /// <summary>
    /// Resolves names in a parameter list without conflicts.
    /// </summary>
    public static List<(ParameterInfo Original, string ResolvedName)> ResolveNames(List<ParameterInfo> parameters)
    {
        return parameters
            .Select(p => (Original: p, ResolvedName: ResolveName(p.Name)))
            .ToList();
    }
}
```

### Resolution Result

```csharp
// Original: logger
// Resolved: baseLogger

public class UserRepositoryObservable : UserRepository
{
    public UserRepositoryObservable(
        ActivitySource activitySource,
        ILogger<UserRepositoryObservable> logger,      // For Observable
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
        ILogger<UserRepository> baseLogger)           // <- Name changed
        : base(baseLogger)                            // <- Forwarded to parent
    {
        // ...
    }
}
```

---

## Constructor Code Generation

### Parameter Declaration Generation

```csharp
private static string GenerateBaseConstructorParameters(
    List<ParameterInfo> baseConstructorParameters)
{
    if (baseConstructorParameters.Count == 0)
    {
        return string.Empty;
    }

    var resolvedParams = ParameterNameResolver.ResolveNames(baseConstructorParameters);

    var parameters = resolvedParams
        .Select(p => $",\n        {p.Original.Type} {p.ResolvedName}")
        .ToList();

    return string.Join("", parameters);
}

// Example output:
// ",
//         global::Microsoft.Extensions.Logging.ILogger<global::MyApp.UserRepository> baseLogger"
```

### Parent Constructor Call Generation

```csharp
private static string GenerateBaseConstructorCall(
    List<ParameterInfo> baseConstructorParameters)
{
    if (baseConstructorParameters.Count == 0)
    {
        return string.Empty;
    }

    var resolvedParams = ParameterNameResolver.ResolveNames(baseConstructorParameters);
    var parameterNames = resolvedParams.Select(p => p.ResolvedName);

    return $"        : base({string.Join(", ", parameterNames)})";
}

// Example output:
// "        : base(baseLogger)"
```

---

## Test Scenarios

### Primary Constructor

```csharp
[Fact]
public Task Should_Handle_Primary_Constructor()
{
    string input = """
        [GenerateObservablePort]
        public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
        {
            public FinT<IO, User> GetUserAsync(int id) => throw new();
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### Multiple Constructors

```csharp
[Fact]
public Task Should_Select_Constructor_With_Most_Parameters()
{
    string input = """
        [GenerateObservablePort]
        public class UserRepository : IObservablePort
        {
            public UserRepository() { }
            public UserRepository(ILogger<UserRepository> logger) { }
            public UserRepository(ILogger<UserRepository> logger, IDbContext db) { }  // Selected
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### Parameter Name Conflict

```csharp
[Fact]
public Task Should_Resolve_Parameter_Name_Conflict()
{
    string input = """
        [GenerateObservablePort]
        public class UserRepository(ILogger<UserRepository> logger) : IObservablePort { }
        """;

    string? actual = _sut.Generate(input);

    // Verify name changed to baseLogger
    actual.ShouldContain("baseLogger");
    actual.ShouldContain(": base(baseLogger)");

    return Verify(actual);
}
```

---

## Summary at a Glance

Constructor handling consists of two main stages. First, `ConstructorParameterExtractor` selects the optimal constructor from the target class or parent class and extracts its parameters. Then, `ParameterNameResolver` adds a `base` prefix to parameters that conflict with Observable's reserved names to resolve naming issues.

| Conflicting Name | Resolved Name |
|-------------------|---------------|
| `activitySource` | `baseActivitySource` |
| `logger` | `baseLogger` |
| `meterFactory` | `baseMeterFactory` |
| `openTelemetryOptions` | `baseOpenTelemetryOptions` |
| `_logger` | `baseLogger` (underscore removed + prefix) |

---

## FAQ

### Q1: When both a Primary Constructor and regular constructors exist, which one is selected?
**A**: `ConstructorParameterExtractor` selects the Primary Constructor as the 1st priority. Only when there is no Primary Constructor does it select the regular constructor with the most parameters. In Roslyn, a Primary Constructor is identified by its `DeclaringSyntaxReferences` syntax node being `TypeDeclarationSyntax` with a non-null `ParameterList`.

### Q2: What is the scope of reserved names that `ParameterNameResolver` adds the `base` prefix to?
**A**: The parameter names that the Observable class uses internally (`activitySource`, `logger`, `meterFactory`, `openTelemetryOptions`) are the reserved names. When a parent class constructor parameter has the same name, it is automatically converted to `baseLogger`, `baseMeterFactory`, etc. Parameters starting with an underscore (`_logger`) also have the underscore removed before the same prefix rule is applied.

### Q3: What happens when neither the target class nor the parent class has a constructor?
**A**: `ConstructorParameterExtractor.ExtractParameters()` returns an empty list, and the generated Observable class constructor contains only Observable's own parameters (`ActivitySource`, `ILogger`, `IMeterFactory`, `IOptions<OpenTelemetryOptions>`). The `: base(...)` call is also omitted.

---

Through constructor handling, the Observable class can now correctly forward the parent's dependencies. The next section covers generic type handling for extracting the inner type `T` from `FinT<IO, T>`.

-> [02. Generic Types](../02-Generic-Types/)
