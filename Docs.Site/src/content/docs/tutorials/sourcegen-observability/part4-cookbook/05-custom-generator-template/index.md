---
title: "Custom Generator Template"
---

## Overview

If you have implemented the ObservablePortGenerator in Part 2 and built the Entity Id, ValueConverter, and Validation generators in this chapter, the remaining question is: "How do I start the next generator?" Configuring a project from scratch every time is inefficient, and it is easy to forget previously validated structures. This section provides project templates and code templates for quickly starting new source generators, based on the experience accumulated so far.

## Learning Objectives

### Core Learning Objectives
1. **Understand the project structure for a new source generator**
   - Separation principles for generator, attributes, models, and test projects
2. **Learn reusable template code**
   - Generator skeleton code ready to copy and use immediately
3. **Use the development checklist**
   - A verification list covering everything from project setup to deployment

---

## Project Structure Template

### Recommended Folder Structure

```
MyCompany.SourceGenerator/
├── MyCompany.SourceGenerator/
│   ├── MyCompany.SourceGenerator.csproj
│   ├── MyGenerator.cs                    # Main generator
│   ├── Attributes/
│   │   └── MyAttribute.cs                # Marker attribute source code
│   ├── Models/
│   │   └── MyInfo.cs                     # Metadata record
│   └── Generators/
│       └── MyCodeGenerator.cs            # Code generation logic
│
├── MyCompany.SourceGenerator.Tests/
│   ├── MyCompany.SourceGenerator.Tests.csproj
│   ├── MyGeneratorTests.cs               # Test class
│   ├── TestRunner.cs                     # Test utility
│   └── Snapshots/
│       └── *.verified.txt                # Verify snapshots
│
└── MyCompany.SourceGenerator.sln
```

This folder structure is a pattern commonly used across the ObservablePortGenerator and the three generators in this chapter. Through separation of concerns, the generator logic, attribute definitions, metadata models, and code generation logic can each be modified independently.

---

## Project File Template

### Source Generator Project (csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Required: netstandard2.0 -->
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Required source generator settings -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>

    <!-- NuGet package information -->
    <PackageId>MyCompany.SourceGenerator</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Company>My Company</Company>
    <Description>Source generator for automating boilerplate code</Description>
    <PackageTags>source-generator;roslyn;codegen</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageReadmeFile>README.md</PackageReadmeFile>

    <!-- Build settings -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <!-- Roslyn API (version pinning recommended) -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <!-- Package as source generator -->
    <None Include="$(OutputPath)\$(AssemblyName).dll"
          Pack="true"
          PackagePath="analyzers/dotnet/cs"
          Visible="false" />
    <None Include="..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

### Test Project (csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <!-- Test framework -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>

    <!-- Verify snapshot testing -->
    <PackageReference Include="Verify.Xunit" Version="26.6.0" />

    <!-- Assertions -->
    <PackageReference Include="Shouldly" Version="4.2.1" />

    <!-- Roslyn test utility -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyCompany.SourceGenerator\MyCompany.SourceGenerator.csproj" />
  </ItemGroup>

</Project>
```

Once the project setup is ready, you can begin writing the actual code. The templates below are structures extracted from the ObservablePortGenerator in Part 2 and the generators in this chapter, where they were used repeatedly.

---

## Code Templates

### Main Generator Class

The main generator performs three responsibilities in sequence. It registers the marker attribute via Post-Initialization, collects target types with `ForAttributeWithMetadataName`, and then generates code with `RegisterSourceOutput`. This structure is common to all IIncrementalGenerators.

```csharp
// MyGenerator.cs
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MyCompany.SourceGenerator.Attributes;
using MyCompany.SourceGenerator.Models;
using MyCompany.SourceGenerator.Generators;

namespace MyCompany.SourceGenerator;

/// <summary>
/// Generates code for types annotated with [MyAttribute].
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Generate fixed code (attribute definition)
        RegisterPostInitialization(context);

        // Step 2: Collect target types
        var provider = RegisterSourceProvider(context);

        // Step 3: Generate code
        context.RegisterSourceOutput(provider, Execute);
    }

    private static void RegisterPostInitialization(
        IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(
                hintName: "MyAttribute.g.cs",
                sourceText: SourceText.From(MyAttribute.Source, Encoding.UTF8));
        });
    }

    private static IncrementalValuesProvider<MyInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: MyAttribute.FullyQualifiedName,
                predicate: IsTargetNode,
                transform: MapToMyInfo)
            .Where(static x => x is not null)!;
    }

    private static bool IsTargetNode(SyntaxNode node, CancellationToken _)
    {
        // TODO: Specify target node type
        return node is TypeDeclarationSyntax;
    }

    private static MyInfo? MapToMyInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken _)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        // TODO: Metadata extraction logic
        return new MyInfo(
            TypeName: typeSymbol.Name,
            Namespace: typeSymbol.ContainingNamespace.ToDisplayString());
    }

    private static void Execute(
        SourceProductionContext context,
        MyInfo info)
    {
        var source = MyCodeGenerator.Generate(info);
        var fileName = $"{info.Namespace.Replace(".", "")}{info.TypeName}.g.cs";

        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }
}
```

### Marker Attribute Definition

The marker attribute defines its source code as a string constant and injects it into the compilation during the Post-Initialization stage. The `global::` prefix is used to prevent conflicts with the consumer project's namespaces.

```csharp
// Attributes/MyAttribute.cs
namespace MyCompany.SourceGenerator.Attributes;

/// <summary>
/// Marker attribute source code
/// </summary>
internal static class MyAttribute
{
    public const string Source = """
        // <auto-generated/>
        #nullable enable

        namespace MyCompany.SourceGenerator;

        /// <summary>
        /// Applied to types targeted for code generation.
        /// </summary>
        [global::System.AttributeUsage(
            global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct,
            AllowMultiple = false,
            Inherited = false)]
        [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
            Justification = "Generated by source generator.")]
        public sealed class MyAttribute : global::System.Attribute;
        """;

    public const string FullyQualifiedName = "MyCompany.SourceGenerator.MyAttribute";
}
```

### Metadata Class

The metadata class must be defined as a `record`. This is because Roslyn's incremental pipeline compares previous execution results with current results to regenerate code only when changes occur, and `Equals`/`GetHashCode` are used for this comparison.

```csharp
// Models/MyInfo.cs
namespace MyCompany.SourceGenerator.Models;

/// <summary>
/// Metadata needed for code generation
/// </summary>
public sealed record MyInfo(
    string TypeName,
    string Namespace);
```

### Code Generator

Separating the code generation logic into a separate class keeps the main generator's `Execute` method concise and allows the generation logic to be tested independently. The `// <auto-generated/>` header and `#nullable enable` are standard preambles for generated code.

```csharp
// Generators/MyCodeGenerator.cs
using System.Text;
using MyCompany.SourceGenerator.Models;

namespace MyCompany.SourceGenerator.Generators;

/// <summary>
/// Source code generation logic
/// </summary>
internal static class MyCodeGenerator
{
    private const string Header = """
        // <auto-generated/>
        // This code was generated by MyCompany.SourceGenerator.
        // Do not modify this file directly.

        #nullable enable

        """;

    public static string Generate(MyInfo info)
    {
        var sb = new StringBuilder();

        // Header
        sb.Append(Header);
        sb.AppendLine();

        // using statements
        sb.AppendLine("using System;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();

        // TODO: Write the code to generate
        sb.AppendLine($"// Generated code for {info.TypeName}");
        sb.AppendLine($"public partial class {info.TypeName}Generated");
        sb.AppendLine("{");
        sb.AppendLine("    // TODO: Members to generate");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
```

Once the code templates are ready, you can write tests. Source generator tests follow a consistent pattern of "compiling the input source code, running the generator, and verifying the generated code." The test runner below encapsulates this process.

---

## Test Templates

### Test Runner

The test runner uses Roslyn's `CSharpCompilation` and `CSharpGeneratorDriver` to execute generators. Adding required runtime types to the `RequiredTypes` array will automatically collect reference assemblies.

```csharp
// TestRunner.cs
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace MyCompany.SourceGenerator.Tests;

/// <summary>
/// Source generator test utility
/// </summary>
public static class TestRunner
{
    private static readonly Type[] RequiredTypes =
    [
        typeof(object),       // System.Runtime
        typeof(Attribute),    // System.Runtime
    ];

    /// <summary>
    /// Runs the source generator and returns the generated code.
    /// </summary>
    public static string? Generate<TGenerator>(
        this TGenerator generator,
        string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        // 1. Create syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // 2. Collect reference assemblies
        var references = RequiredTypes
            .Select(t => t.Assembly.Location)
            .Distinct()
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToImmutableArray<MetadataReference>();

        // 3. Create compilation
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 4. Run the generator
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        // 5. Verify diagnostics
        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        // Display errors if any
        foreach (var error in errors)
        {
            Console.WriteLine($"Error: {error.GetMessage()}");
        }

        errors.ShouldBeEmpty("Compilation should not have errors");

        // 6. Return generated code (last file - excluding attributes)
        var result = driver.GetRunResult();
        return result.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .LastOrDefault();
    }

    /// <summary>
    /// Returns all generated files.
    /// </summary>
    public static IReadOnlyList<(string FileName, string Content)> GenerateAll<TGenerator>(
        this TGenerator generator,
        string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var references = RequiredTypes
            .Select(t => t.Assembly.Location)
            .Distinct()
            .Select(loc => MetadataReference.CreateFromFile(loc))
            .ToImmutableArray<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var result = driver.GetRunResult();
        return result.GeneratedTrees
            .Select(t => (
                FileName: Path.GetFileName(t.FilePath),
                Content: t.GetText().ToString()))
            .ToList();
    }
}
```

### Test Class

Tests are written around three basic scenarios: verifying attribute generation, code generation for target types, and negative tests when no attribute is present. This structure was used identically across the Entity Id, ValueConverter, and Validation generators.

```csharp
// MyGeneratorTests.cs
using Xunit;

namespace MyCompany.SourceGenerator.Tests;

public sealed class MyGeneratorTests
{
    private readonly MyGenerator _sut = new();

    [Fact]
    public Task MyGenerator_ShouldGenerate_Attribute()
    {
        // Arrange
        string input = """
            namespace TestNamespace;

            public class TestClass { }
            """;

        // Act
        var files = _sut.GenerateAll(input);

        // Assert
        var attributeFile = files.FirstOrDefault(f => f.FileName.Contains("MyAttribute"));
        return Verify(attributeFile.Content);
    }

    [Fact]
    public Task MyGenerator_ShouldGenerate_ForTargetType()
    {
        // Arrange
        string input = """
            using MyCompany.SourceGenerator;

            namespace TestNamespace;

            [My]
            public class TestClass { }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual);
    }

    [Fact]
    public void MyGenerator_ShouldNotGenerate_WhenNoAttribute()
    {
        // Arrange
        string input = """
            namespace TestNamespace;

            public class TestClass { }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldBeNull();
    }
}
```

Now that the code templates and test templates are ready, let's conclude by organizing items that are easy to overlook during development into a checklist. This list corresponds to the 7-step workflow from Section 01.

---

## Development Checklist

```markdown
# Source Generator Development Checklist

## Project Setup
- [ ] TargetFramework: netstandard2.0
- [ ] EnforceExtendedAnalyzerRules: true
- [ ] IsRoslynComponent: true
- [ ] Microsoft.CodeAnalysis.CSharp reference (version pinned)
- [ ] Microsoft.CodeAnalysis.Analyzers reference

## Implementation
- [ ] IIncrementalGenerator implementation
- [ ] [Generator(LanguageNames.CSharp)] attribute applied
- [ ] Marker attribute generation via RegisterPostInitializationOutput
- [ ] Target filtering via ForAttributeWithMetadataName
- [ ] Target node type validation in predicate
- [ ] Metadata extraction in transform
- [ ] Code generation connected via RegisterSourceOutput

## Generated Code Quality
- [ ] // <auto-generated/> header
- [ ] #nullable enable
- [ ] ExcludeFromCodeCoverage attribute
- [ ] Namespace conflict prevention with global:: prefix
- [ ] XML documentation comments

## Testing
- [ ] Verify snapshot tests
- [ ] Basic case tests
- [ ] Edge case tests
- [ ] Negative case tests (when no attribute is present)
- [ ] Namespace variation tests

## Packaging
- [ ] PackageId, Version configured
- [ ] Package description written
- [ ] DLL included at analyzers/dotnet/cs path
- [ ] dotnet pack -c Release tested

## Documentation
- [ ] README.md written
- [ ] Usage examples included
- [ ] Limitations documented
```

Here are debugging techniques you can use when the generator does not behave as expected.

---

## Debugging Tips

### Debugging in Visual Studio

```csharp
// Add to the generator code
public void Initialize(IncrementalGeneratorInitializationContext context)
{
#if DEBUG
    // Wait for debugger attachment
    if (!System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Launch();
    }
#endif

    // ... rest of code
}
```

### Diagnostic Output

```csharp
// Diagnostic message output
private static void Execute(
    SourceProductionContext context,
    MyInfo info)
{
    // Informational diagnostic
    context.ReportDiagnostic(Diagnostic.Create(
        new DiagnosticDescriptor(
            id: "MYGEN001",
            title: "Code Generated",
            messageFormat: "Generated code for {0}",
            category: "MyGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true),
        Location.None,
        info.TypeName));

    // ... code generation
}
```

### Logging (During Development)

```csharp
// Output logs to file (use only during development)
private static void Log(string message)
{
#if DEBUG
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "generator-log.txt");
    File.AppendAllText(logPath, $"{DateTime.Now}: {message}\n");
#endif
}
```

Once development and testing are complete, you can deploy as a NuGet package. The `analyzers/dotnet/cs` path setting already configured in the csproj takes effect here.

---

## Packaging and Deployment

### NuGet Package Creation

```bash
# Release build (important!)
dotnet build -c Release

# Create package
dotnet pack -c Release -o ./packages

# Verify package
dotnet nuget locals all --list
```

### Local Testing

```xml
<!-- Consumer project's nuget.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local" value="C:\path\to\packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- Consumer project csproj -->
<ItemGroup>
  <PackageReference Include="MyCompany.SourceGenerator"
                    Version="1.0.0"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Publishing to NuGet.org

```bash
# Set API key
dotnet nuget setapikey YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Publish
dotnet nuget push ./packages/MyCompany.SourceGenerator.1.0.0.nupkg \
    --source https://api.nuget.org/v3/index.json
```

---

## Summary at a Glance

Here is a summary of the key configuration of the source generator project template.

| Item | Recommendation |
|------|---------------|
| **TargetFramework** | netstandard2.0 |
| **Roslyn version** | 4.8.0 (version pinned) |
| **Test framework** | xUnit + Verify |
| **Code structure** | Generator / Attributes / Models / Generation logic separated |
| **Build** | dotnet pack -c Release |

This template is a structure that has been repeatedly validated through the implementation of the ObservablePortGenerator, Entity Id generator, ValueConverter generator, and Validation generator. When starting a new generator, copy this template, replace `My` with the actual name, and fill in the TODO comments.

---

## Additional Learning Resources

- [Roslyn Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [Microsoft Learn: Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [Andrew Lock: Source Generators](https://andrewlock.net/series/creating-a-source-generator/)

---

## FAQ

### Q1: Should the `TypeDeclarationSyntax` filtering in `IsTargetNode` of the template's `MyGenerator` be made more specific?
**A**: Yes. `TypeDeclarationSyntax` includes `class`, `struct`, `record`, and `interface`, so failing to precisely filter target nodes results in unnecessary symbol analysis. Like the Entity Id generator, combining `RecordDeclarationSyntax` with `StructKeyword`, or restricting to only `ClassDeclarationSyntax` as needed, improves incremental caching efficiency.

### Q2: What problems arise if the metadata class is defined as a `class` instead of a `record`?
**A**: Roslyn's incremental pipeline compares previous execution results with current results using `Equals()` to determine whether changes occurred. A `class` uses reference equality by default, so even with identical content, it is recognized as a different object each time, causing code to be regenerated on every build. A `record` automatically provides value equality, allowing incremental caching to work correctly.

### Q3: When should `GenerateAll()` and `Generate()` methods be used respectively?
**A**: `Generate()` returns only the last generated file (typically the main generated code), making it suitable for most snapshot tests. `GenerateAll()` returns all generated files including marker attributes, interfaces, and main code along with their file names, so it is used when verifying that attribute code is generated correctly or checking the list of generated files.

---

We have covered all the content of the Part 4 Cookbook. From the development workflow to three practical generators and reusable templates, we now have the tools needed to build source generators independently. In the next chapter, we will look back at the entire tutorial and summarize the key points.

→ [Part 5, Chapter 1: Summary](../../Part5-Conclusion/01-summary.md)
