---
title: "Unit Test Setup"
---

## Overview

Typical unit tests call a method and verify the return value. However, since source generators run at compile time, testing them requires directly configuring the Roslyn compilation pipeline. You need to compile input source code with `CSharpCompilation`, run the source generator with `CSharpGeneratorDriver`, and then extract the generated code as a string. Functorium abstracts this process into a utility called `SourceGeneratorTestRunner`, so that test code can obtain generation results with a single line: `_sut.Generate(input)`.

## Learning Objectives

### Core Learning Objectives
1. **Building a test environment using CSharpCompilation**
   - How to run a source generator using the Roslyn compiler API
2. **Understanding the SourceGeneratorTestRunner utility**
   - Assembly reference management and generation result extraction process
3. **Test project configuration**
   - Required NuGet packages and project reference setup

---

## The Uniqueness of Source Generator Testing

Source generators run at compile time, so they require a different approach than regular unit tests.

```
Regular Unit Tests
==================
Input -> Method call -> Verify output

Source Generator Tests
======================
Input source code -> Compile -> Verify generated code
```

---

## SourceGeneratorTestRunner Implementation

### Overall Structure

```csharp
// Functorium.Testing/Actions/SourceGenerators/SourceGeneratorTestRunner.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Functorium.Testing.SourceGenerators;

/// <summary>
/// Utility class for source generator testing.
/// Runs IIncrementalGenerator in a test environment and returns results.
/// </summary>
public static class SourceGeneratorTestRunner
{
    // List of required assembly types that must always be referenced in tests
    private static readonly Type[] RequiredTypes =
    [
        typeof(object),                                        // System.Runtime
        typeof(LanguageExt.IO),                                // LanguageExt.Core
        typeof(LanguageExt.FinT<,>),                           // LanguageExt.Core (generic)
        typeof(Microsoft.Extensions.Logging.ILogger),          // Microsoft.Extensions.Logging
    ];

    /// <summary>
    /// Runs the source generator and returns the generated code.
    /// </summary>
    public static string? Generate<TGenerator>(this TGenerator generator, string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        // Implementation...
    }
}
```

---

## Test Execution Flow

### 1. Syntax Tree Creation

```csharp
// Create Syntax Tree from source code
var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
```

Converts the input source code into a form that Roslyn can understand.

### 2. Required Assembly References

```csharp
// Add required assemblies first (order guaranteed)
var requiredReferences = RequiredTypes
    .Select(t => t.Assembly)
    .Distinct()
    .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
    .Cast<MetadataReference>();

// Convert currently loaded non-dynamic assemblies to references
var otherReferences = AppDomain
    .CurrentDomain
    .GetAssemblies()
    .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
    .Where(assembly => !RequiredTypes.Any(t => t.Assembly == assembly))
    .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
    .Cast<MetadataReference>();

// Required references first, then other references
var references = requiredReferences.Concat(otherReferences);
```

### 3. Compilation Creation

```csharp
var compilation = CSharpCompilation.Create(
    "SourceGeneratorTests",     // Assembly name to create
    [syntaxTree],               // Sources
    references,                 // References
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
```

### 4. Source Generator Execution

```csharp
// Compile: invoke IIncrementalGenerator source generator
CSharpGeneratorDriver
    .Create(generator)
    .RunGeneratorsAndUpdateCompilation(
        compilation,
        out var outputCompilation,          // Source generator result: sources
        out var diagnostics);               // Source generator diagnostics: warnings, errors
```

### 5. Result Verification

```csharp
// Source generator diagnostics (compiler errors)
diagnostics
    .Where(d => d.Severity == DiagnosticSeverity.Error)
    .ShouldBeEmpty();

// Source generator results (compiler output)
return outputCompilation
    .SyntaxTrees
    .Skip(1)                // [0] Exclude original source SyntaxTree
    .LastOrDefault()?
    .ToString();
```

---

## Test Project Configuration

### Project References

```xml
<!-- Tests/Functorium.Tests.Unit/Functorium.Tests.Unit.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Test framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1" />

    <!-- Assertion -->
    <PackageReference Include="Shouldly" Version="4.3.0" />

    <!-- Snapshot testing -->
    <PackageReference Include="Verify.Xunit" Version="28.9.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- Test utilities -->
    <ProjectReference Include="..\..\Src\Functorium.Testing\Functorium.Testing.csproj" />

    <!-- Source generator under test -->
    <ProjectReference Include="..\..\Src\Functorium.SourceGenerators\Functorium.SourceGenerators.csproj" />
  </ItemGroup>

</Project>
```

### NuGet Packages

| Package | Purpose |
|---------|---------|
| `xunit` | Test framework |
| `Shouldly` | Fluent Assertion |
| `Verify.Xunit` | Snapshot testing |
| `Microsoft.CodeAnalysis.CSharp` | Roslyn compiler |

---

## Writing Basic Tests

### Test Class Structure

```csharp
// ObservablePortGeneratorTests.cs
using Functorium.Adapters.SourceGenerators;
using Functorium.Testing.SourceGenerators;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

[Trait(nameof(UnitTest), UnitTest.Functorium_SourceGenerator)]
public sealed class ObservablePortGeneratorTests
{
    private readonly ObservablePortGenerator _sut;

    public ObservablePortGeneratorTests()
    {
        _sut = new ObservablePortGenerator();
    }

    [Fact]
    public Task Should_Generate_PipelineClass()
    {
        // Arrange
        string input = """
            using Functorium.Adapters.SourceGenerators;
            using Functorium.Abstractions.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITestAdapter : IObservablePort
            {
                FinT<IO, int> GetValue();
            }

            [GenerateObservablePort]
            public class TestAdapter : ITestAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }
}
```

### Input Source Code Pattern

```csharp
string input = """
    // 1. Required using statements
    using Functorium.Adapters.SourceGenerators;
    using Functorium.Abstractions.Observabilities;
    using LanguageExt;

    // 2. Namespace
    namespace TestNamespace;

    // 3. Interface definition (inheriting IObservablePort)
    public interface ITestAdapter : IObservablePort
    {
        FinT<IO, int> GetValue();
    }

    // 4. Apply [GenerateObservablePort] attribute
    [GenerateObservablePort]
    public class TestAdapter : ITestAdapter
    {
        public string RequestCategory => "Test";
        public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
    }
    """;
```

---

## Using Extension Methods

### Using the Generate Method

```csharp
// SourceGeneratorTestRunner extension method
string? actual = _sut.Generate(input);

// Internally:
// 1. CSharpSyntaxTree.ParseText(input)
// 2. CSharpCompilation.Create(...)
// 3. CSharpGeneratorDriver.Create(_sut).RunGeneratorsAndUpdateCompilation(...)
// 4. Returns ToString() of generated SyntaxTree
```

### Null Result Handling

```csharp
[Fact]
public void Should_Return_Null_When_NoAttributeApplied()
{
    string input = """
        public class RegularClass { }
        """;

    string? actual = _sut.Generate(input);

    // No generation when [GenerateObservablePort] attribute is absent
    actual.ShouldBeNull();
}
```

---

## Test Execution

### Visual Studio

```
Test Explorer -> Run All Tests
```

### Command Line

```bash
dotnet test Tests/Functorium.Tests.Unit/Functorium.Tests.Unit.csproj
```

### Running Specific Tests Only

```bash
dotnet test --filter "FullyQualifiedName~ObservablePortGeneratorTests"
```

---

## Summary at a Glance

The key to source generator testing is reproducing the Roslyn compilation pipeline in a test environment. `SourceGeneratorTestRunner` encapsulates the entire process of Syntax Tree creation, assembly reference collection, Compilation creation, and Generator execution, so test code can focus solely on input and output. `Verify` snapshot tests and `Shouldly` assertions are used together for verifying generated code.

---

## FAQ

### Q1: What is the criterion for adding types to `RequiredTypes` in `SourceGeneratorTestRunner`?
**A**: Assemblies of external types used in the input source code must be referenced in the compilation. By registering assemblies of types that appear in code analyzed by ObservablePortGenerator -- such as `LanguageExt.IO`, `FinT<,>`, and `ILogger` -- in `RequiredTypes`, they are automatically collected via `MetadataReference.CreateFromFile()`. When new external types are added to test inputs, this array must also be updated.

### Q2: Why does `outputCompilation.SyntaxTrees.Skip(1)` skip the first tree?
**A**: The first item in `SyntaxTrees` is the original source code provided as test input. Code added by the source generator comes after it, so `Skip(1).LastOrDefault()` retrieves the last generated file (typically the Observable class code). Since the marker Attribute is also included in generated files, the last file is the actual generation code.

### Q3: How do you debug when compilation errors occur in source generator tests?
**A**: Filtering `DiagnosticSeverity.Error` from `diagnostics` reveals the error messages. Common causes include missing assemblies for types used in input source code from `RequiredTypes`, or syntax errors in the input code itself. Printing the complete diagnostic list with `outputCompilation.GetDiagnostics()` helps pinpoint the cause.

---

With the test environment in place, we will learn about the Verify snapshot testing approach that saves and compares the entire generated code as a file.

-> [06. Verify Snapshot Testing](../06-Verify-Snapshot-Testing/)
