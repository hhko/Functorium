---
title: "Project Structure"
---

## Overview

In the previous chapter, we set up the .NET SDK and IDE. Now it's time to examine the internal structure of source generator projects.

The csproj file of a source generator requires unique properties different from regular libraries. Without understanding what settings like `IsRoslynComponent`, `PrivateAssets="all"`, and `OutputItemType="Analyzer"` do, you may end up in a situation where the build succeeds but the generator doesn't work at all. This chapter explains the meaning of each setting and analyzes the actual ObservablePortGenerator project structure and data models.

## Learning Objectives

### Core Learning Objectives
1. **Understand csproj settings for source generator projects**
   - The role of required properties like `IsRoslynComponent` and `EnforceExtendedAnalyzerRules`
2. **Understand the role of `IsRoslynComponent` and related properties**
   - Impact on IDE recognition, build output, and NuGet packaging
3. **Analyze the actual Functorium project structure**
   - File structure and data model design of ObservablePortGenerator

---

## Source Generator Project Settings

### Required PropertyGroup Settings

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- 1. Target framework: must be netstandard2.0 -->
    <TargetFramework>netstandard2.0</TargetFramework>

    <!-- 2. Use latest C# language version -->
    <LangVersion>latest</LangVersion>

    <!-- 3. Mark as Roslyn component (critical!) -->
    <IsRoslynComponent>true</IsRoslynComponent>

    <!-- 4. Enforce extended analyzer rules -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>

    <!-- 5. Nullable reference types -->
    <Nullable>enable</Nullable>

    <!-- 6. Enable implicit usings -->
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

### Property Detailed Descriptions

Understanding why each property is needed is important. In particular, `IsRoslynComponent` and `EnforceExtendedAnalyzerRules` are settings unique to source generator projects.

| Property | Value | Description |
|------|-----|------|
| `TargetFramework` | `netstandard2.0` | Runnable in all .NET environments |
| `LangVersion` | `latest` | Use C# 13 syntax (in generator code) |
| `IsRoslynComponent` | `true` | IDE recognizes it as a source generator |
| `EnforceExtendedAnalyzerRules` | `true` | Enforces analyzer development best practices |
| `Nullable` | `enable` | Null safety checks |
| `ImplicitUsings` | `enable` | Enable implicit usings |

---

## Role of IsRoslynComponent

When `IsRoslynComponent` is set to `true`:

```
1. IDE Recognition
==================
Visual Studio and VS Code recognize this project
as a source generator/analyzer.

2. Build Output
===============
The DLL is placed in the analyzers folder:
MyGenerator/
├── bin/
│   └── Debug/
│       └── netstandard2.0/
│           └── MyGenerator.dll
│
└── obj/
    └── Debug/
        └── netstandard2.0/
            └── analyzer/  ← Analyzer output folder

3. NuGet Packaging
==================
Placed in the correct location when distributed as NuGet package:
analyzers/dotnet/cs/MyGenerator.dll
```

---

## Actual Project Analysis: Functorium.SourceGenerators

### Complete csproj Structure

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>

    <!-- Source Generator required settings -->
    <IsRoslynComponent>true</IsRoslynComponent>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IsPackable>true</IsPackable>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Analyzer packaging required settings -->
    <IncludeSymbols>false</IncludeSymbols>
    <NoWarn>$(NoWarn);NU5128;RS2008</NoWarn>
  </PropertyGroup>

  <!-- NuGet Package Settings -->
  <PropertyGroup>
    <PackageId>Functorium.SourceGenerators</PackageId>
    <Description>Functorium Source Generator for Adapter Pipeline generation</Description>
    <PackageTags>$(PackageTags);source-generator;roslyn;analyzer</PackageTags>
  </PropertyGroup>

  <!-- Package Files -->
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Functorium.png" Pack="true" PackagePath="\" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                      PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <!-- Include analyzer DLL in NuGet package -->
  <ItemGroup>
    <None Include="bin\$(Configuration)\$(TargetFramework)\$(AssemblyName).dll"
          Pack="true"
          PackagePath="analyzers/dotnet/cs"
          Visible="false" />
  </ItemGroup>

</Project>
```

### Meaning of PrivateAssets="all"

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp"
                  Version="4.12.0"
                  PrivateAssets="all" />
```

```
Effect of PrivateAssets="all"
=============================

1. Transitive dependency blocking
   - Microsoft.CodeAnalysis.CSharp is not
   - passed to projects that reference this package

2. Excluded from NuGet package
   - Roslyn package is not included
   - in the source generator NuGet package

3. Runtime dependency removal
   - Used only at compile time
   - Not needed at application runtime
```

---

## Project File Structure

```
Functorium.SourceGenerators/
│
├── Functorium.SourceGenerators.csproj
│
├── Abstractions/
│   ├── Constants.cs                     # Common constants (headers, etc.)
│   └── Selectors.cs                     # Common selectors
│
└── Generators/
    ├── IncrementalGeneratorBase.cs      # Template method pattern base class
    │
    ├── ObservablePortGenerator/        # Observability code generator
    │   ├── ObservablePortGenerator.cs   # Main source generator
    │   ├── ObservableGeneratorConstants.cs  # Generator-specific constants
    │   ├── ObservableClassInfo.cs       # Class information record
    │   ├── MethodInfo.cs                # Method information
    │   ├── ParameterInfo.cs             # Parameter information
    │   ├── TypeExtractor.cs             # Type extraction utility
    │   ├── CollectionTypeHelper.cs      # Collection type detection
    │   ├── SymbolDisplayFormats.cs      # Type string formats
    │   ├── ConstructorParameterExtractor.cs  # Constructor analysis
    │   └── ParameterNameResolver.cs     # Name conflict resolution
    │
    ├── EntityIdGenerator/              # Entity ID auto-generator
    │   ├── EntityIdGenerator.cs         # Ulid-based ID struct generation
    │   └── EntityIdInfo.cs              # Entity information record
    │
    └── UnionTypeGenerator/             # Union Type generator
        ├── UnionTypeGenerator.cs        # Match/Switch method generation
        └── UnionTypeInfo.cs             # Union information record
```

---

## Data Models (Records)

Source generators must carry information extracted from the Roslyn API through to the code generation stage. This requires immutable data models that hold class, method, and parameter information collected at compile time. ObservablePortGenerator uses three core records.

### ObservableClassInfo

```csharp
using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// Class information needed for pipeline generation
/// </summary>
public readonly record struct ObservableClassInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly List<MethodInfo> Methods;
    public readonly List<ParameterInfo> BaseConstructorParameters;
    public readonly Location? Location;                              // Diagnostic location

    public static readonly ObservableClassInfo None = new(
        string.Empty, string.Empty, new List<MethodInfo>(), new List<ParameterInfo>(), null);

    public ObservableClassInfo(
        string @namespace,
        string className,
        List<MethodInfo> methods,
        List<ParameterInfo> baseConstructorParameters,
        Location? location)
    {
        Namespace = @namespace;
        ClassName = className;
        Methods = methods;
        BaseConstructorParameters = baseConstructorParameters;
        Location = location;
    }
}
```

### MethodInfo

```csharp
/// <summary>
/// Method information
/// </summary>
public class MethodInfo
{
    public string Name { get; }
    public List<ParameterInfo> Parameters { get; }
    public string ReturnType { get; }

    public MethodInfo(string name, List<ParameterInfo> parameters, string returnType)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
    }
}
```

### ParameterInfo

```csharp
/// <summary>
/// Parameter information
/// </summary>
public class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public RefKind RefKind { get; }
    public bool IsCollection { get; }       // Whether it is a collection type

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

## Project Reference Configuration

### Projects Using the Source Generator

```xml
<!-- Functorium.csproj (core library) -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Source generator reference (compile-time only) -->
    <ProjectReference
        Include="..\Functorium.SourceGenerators\Functorium.SourceGenerators.csproj"
        OutputItemType="Analyzer"
        ReferenceOutputAssembly="false" />
  </ItemGroup>

</Project>
```

```
Reference Property Descriptions
================================

OutputItemType="Analyzer"
  → MSBuild treats this reference as an analyzer

ReferenceOutputAssembly="false"
  → Excludes runtime assembly reference
  → Source generator runs only at compile time
```

### Test Project

```xml
<!-- Functorium.Tests.Unit.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Direct source generator reference (for testing) -->
    <ProjectReference
        Include="..\Functorium.SourceGenerators\Functorium.SourceGenerators.csproj" />

    <!-- Test utilities -->
    <ProjectReference
        Include="..\Functorium.Testing\Functorium.Testing.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Verify.Xunit" />
    <PackageReference Include="Shouldly" />
  </ItemGroup>

</Project>
```

---

## Verify Build Output

Verify generated files after building:

```bash
# Build
dotnet build Functorium.SourceGenerators.csproj

# Check output
ls bin/Debug/netstandard2.0/
# Functorium.SourceGenerators.dll
# Functorium.SourceGenerators.pdb
```

---

## Summary at a Glance

Source generator projects are fundamentally different from regular libraries in their csproj settings and project reference methods. `IsRoslynComponent` handles IDE recognition, `PrivateAssets="all"` blocks transitive dependency of Roslyn packages, and `OutputItemType="Analyzer"` handles compile-time-only references. Data models are designed as immutable types—`ObservableClassInfo`, `MethodInfo`, `ParameterInfo`—for safe transfer in the incremental build pipeline.

| Item | Description |
|------|------|
| `IsRoslynComponent` | IDE recognizes it as a source generator |
| `PrivateAssets="all"` | Prevents Roslyn package transitivity |
| `OutputItemType="Analyzer"` | Treated as analyzer during project reference |
| Data models | Defined as immutable types (ObservableClassInfo, etc.) |

---

## FAQ

### Q1: Why should data models be defined as `record` or `readonly record struct`?
**A**: Roslyn's incremental pipeline compares previous and current execution results using `Equals`/`GetHashCode` to determine whether changes occurred. `record` automatically generates value-based equality comparison, so if data is the same, unnecessary code regeneration can be skipped.

### Q2: What roles do `IsRoslynComponent` and `EnforceExtendedAnalyzerRules` each play?
**A**: `IsRoslynComponent` enables the IDE (especially Visual Studio) to recognize the project as a source generator/analyzer and provide real-time feedback. `EnforceExtendedAnalyzerRules` catches API usage not allowed in source generators (file system access, etc.) as compile errors.

### Q3: Why is the `Location?` field included in `ObservableClassInfo`?
**A**: `Location` is needed for the source generator to report diagnostic messages (warnings, errors) with the exact code location to the user. For example, when detecting an incorrect usage pattern, a warning can be displayed at the class declaration location.

---

Now that we understand the project structure and data models, the next chapter covers one of the most challenging parts of source generator development: debugging environment setup.

→ [03. Debugging Setup](../03-Debugging-Setup/)
