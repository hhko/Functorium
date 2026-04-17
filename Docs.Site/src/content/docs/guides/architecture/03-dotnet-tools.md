---
title: ".NET Tools Guide"
---

This document covers the detailed usage of .NET tools used in the project. They are categorized into three groups: CLI tools, source generators, and .NET 10 file-based programs.

## Introduction

"Are you manually generating code coverage reports every time?"
"Are you spending time individually approving snapshot test results?"
"Are you manually updating ER diagrams when EF Core schemas change?"

Repetitive development tasks such as generating code coverage reports, approving snapshot tests, generating ER diagrams, and analyzing slow tests can be automated with .NET tools. Integrating the right tools into the build pipeline reduces manual work and lets you focus on the development flow.

### What You Will Learn

This document covers the following topics:

1. **CLI tool management and usage** - Parameters and execution methods for ReportGenerator, Verify.Tool, and Siren
2. **Source generator usage** - Triggers and generated output for EntityIdGenerator and ObservablePortGenerator
3. **Source Generator debugging** - Test project-based debugging and the Debugger.Launch() method
4. **.NET 10 file-based programs** - Running scripts like SummarizeSlowestTests and ApiGenerator
5. **New tool addition checklist** - Addition procedures for CLI tools, source generators, and scripts

> **The key to leveraging .NET tools is** automating repetitive development tasks with CLI tools and source generators, and integrating them into the build pipeline.

## Summary

### Key Commands

```powershell
# Restore tools (after clone)
dotnet tool restore

# Generate coverage report
dotnet reportgenerator -reports:**/*.cobertura.xml -targetdir:.coverage/reports/html -reporttypes:"Html;Cobertura"

# Approve Verify snapshots
dotnet verify accept -y

# Generate ER diagram
dotnet siren-gen -a bin/Release/net10.0/MyApp.Persistence.dll -o ER-Diagram.md

# Analyze slow tests
dotnet .coverage/scripts/SummarizeSlowestTests.cs --glob "**/*.trx" --threshold 30
```

### Key Procedures

**1. Adding a new CLI tool:**
1. `dotnet tool install <package-name>` (automatically registered in manifest)
2. Check `rollForward` setting (set to `true` if tool target framework is lower than current SDK)
3. Update related documentation

**2. Source Generator debugging:**
1. Set breakpoints in the test project (recommended)
2. Run Debug from Test Explorer
3. Step into Generator internals with F11

### Key Concepts

| Category | Tool | Purpose |
|---------|------|------|
| CLI tool | ReportGenerator | Code coverage HTML report |
| CLI tool | Verify.Tool | Snapshot test approval |
| CLI tool | Siren | EF Core to Mermaid ER diagram |
| Source generator | EntityIdGenerator | Ulid-based EntityId auto-generation |
| Source generator | ObservablePortGenerator | Observability wrapping Pipeline generation |
| Source generator | UnionTypeGenerator | Auto-generation of Match/Switch methods for union types |
| .NET 10 script | SummarizeSlowestTests | Slow test analysis report |

---

## Overview

This guide covers the **detailed usage** of .NET tools used in the project. Tools are classified into three categories.

The following table summarizes the characteristics and representative tools for each tool category.

| Category | Description | Examples |
|---------|------|------|
| CLI tools | `dotnet tool` manifest management | ReportGenerator, Verify.Tool, Siren |
| Source generators | Automatic code generation at compile time | EntityIdGenerator, ObservablePortGenerator |
| .NET 10 scripts | Direct execution of `.cs` files | SummarizeSlowestTests, ApiGenerator |

> **Relationship with 02-solution-configuration.md**: For `dotnet-tools.json` manifest creation/management methods and build script pipeline overview, see [02-solution-configuration.md](../02-solution-configuration). This document covers the purpose, commands, parameters, and execution examples for each tool.

## CLI Tools (.config/dotnet-tools.json)

### Tool Management Basics

CLI tools are managed via the `.config/dotnet-tools.json` manifest. For manifest creation and tool installation/restoration methods, see [02-solution-configuration.md §.config/dotnet-tools.json](../02-solution-configuration#configdotnet-toolsjson).

**rollForward setting:**

| Value | Behavior | When to Use |
|----|------|----------|
| `false` (default) | Requires runtime exactly matching the tool's target framework | When tool and SDK versions match |
| `true` | Allows execution on higher version runtimes | When the tool targets an older version (e.g., running .NET 9 tool on .NET 10 SDK) |

> `Build-Local.ps1` Step 1 automatically performs `dotnet tool restore`.

### ReportGenerator (Code Coverage)

| Item | Value |
|------|-----|
| Package | `dotnet-reportgenerator-globaltool` |
| Command | `reportgenerator` |
| Purpose | Cobertura XML to HTML/Markdown coverage report conversion |

**Standalone execution:**

```powershell
dotnet reportgenerator `
  -reports:.coverage/reports/**/*.cobertura.xml `
  -targetdir:.coverage/reports/html `
  -reporttypes:"Html;Cobertura;MarkdownSummaryGithub"
```

**Key parameters:**

| Parameter | Description | Example |
|---------|------|------|
| `-reports` | Input coverage files (glob) | `**/*.cobertura.xml` |
| `-targetdir` | Output directory | `.coverage/reports/html` |
| `-reporttypes` | Report formats | `Html;Cobertura;MarkdownSummaryGithub` |
| `-assemblyfilters` | Assembly include/exclude | `+MyApp*;-*.Tests*` |
| `-filefilters` | Source file include/exclude | `-**/AssemblyReference.cs` |

> `Build-Local.ps1` Step 7 automatically generates HTML + Cobertura + Markdown reports.

### Verify.Tool (Snapshot Management)

| Item | Value |
|------|-----|
| Package | `verify.tool` |
| Command | `dotnet-verify` |
| Purpose | Approve Verify.Xunit snapshot `.received` to `.verified` |

**Execution:**

```powershell
dotnet verify accept -y
```

**When to use:**
- When `*.received.*` files are generated after running snapshot tests
- When output has intentionally changed and the new snapshot needs to be approved

> `Build-VerifyAccept.ps1` automatically performs this command.

### Siren (ER Diagrams)

| Item | Value |
|------|-----|
| Package | `gman.siren` |
| Command | `siren-gen` |
| Purpose | EF Core DbContext to Mermaid ER diagram generation |
| rollForward | `true` (running .NET 9 tool on .NET 10) |

**Input modes:**

| Mode | Flag | Description |
|------|--------|------|
| Assembly | `-a <dll path>` | Extract schema from assembly containing Migrations |
| Connection string | `-c <connection string>` | Read schema from existing database |

**Execution examples:**

```powershell
# Assembly mode (project using Migrations)
dotnet siren-gen `
  -a bin/Release/net10.0/MyApp.Persistence.dll `
  -o ER-Diagram.md

# Connection string mode (existing DB)
dotnet siren-gen `
  -c "Data Source=myapp.db" `
  -o ER-Diagram.md
```

**Key parameters:**

| Parameter | Description |
|---------|------|
| `-o, --outputPath` | Output Markdown file path (required) |
| `-a, --assemblyPath` | Migration assembly DLL path |
| `-c, --connectionString` | Database connection string |
| `-f, --filterEntities` | Entity name filter to include (comma-separated) |
| `-s, --skipEntities` | Entity names to exclude (comma-separated) |
| `-h, --filterSchemas` | Schema filter to include |
| `-x, --skipSchemas` | Schemas to exclude |
| `-t, --template` | Rendering template (default: `default`) |

**Constraints:**
- Assembly mode (`-a`): Requires EF Core Migrations. Does not work with `EnsureCreated()` pattern projects
- Connection string mode (`-c`): SQL Server only. SQLite/InMemory not supported

> Siren is a general-purpose tool for rendering Mermaid diagrams to images, but here we only use the EF Core to Mermaid ER diagram generation feature.

Due to these constraints, the project uses the `Build-ERDiagram.ps1` script (see §Build-ERDiagram.ps1) to directly generate Mermaid ER diagrams based on EF Core Configuration. Example: [Tests.Hosts/01-SingleHost/ER-Diagram.md](../../Tests.Hosts/01-SingleHost/ER-Diagram.md)

### Build-ERDiagram.ps1 (Direct ER Diagram Generation)

| Item | Value |
|------|-----|
| Location | `Tests.Hosts/01-SingleHost/Build-ERDiagram.ps1` |
| Purpose | Mermaid ER diagram generation based on EF Core Configuration |
| Output | `Tests.Hosts/01-SingleHost/ER-Diagram.md` |

This script bypasses Siren tool constraints (requiring Migrations or SQL Server only) by outputting ER diagram templates defined within the script to `ER-Diagram.md`. When schemas change, the `$erDiagram` variable inside the script must be manually updated.

**Execution:**

```powershell
# Run in Tests.Hosts/01-SingleHost/ directory
./Build-ERDiagram.ps1

# Help
./Build-ERDiagram.ps1 -Help
```

**Reference files**: When EF Core Configuration changes, refer to the following files to update the script:
- `Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/`

While CLI tools run independently in the build pipeline, source generators automatically generate code at compile time.

## Source Generators

### Functorium.SourceGenerators (Internal)

| Item | Value |
|------|-----|
| Project | `Src/Functorium.SourceGenerators` |
| Target | `netstandard2.0` (Roslyn requirement) |
| NuGet packaging | Placed in `analyzers/dotnet/cs` path |

**Provided generators:**

The following table summarizes the source generators provided by Functorium and the code each generates.

| Generator | Trigger Attribute | Generated Output |
|--------|------------------|----------|
| `EntityIdGenerator` | `[GenerateEntityId]` | EntityId struct + EF Core Converter/Comparer |
| `ObservablePortGenerator` | `[GenerateObservablePort]` | Observability wrapping Pipeline class |
| `UnionTypeGenerator` | `[UnionType]` | Auto-generated `Match`/`Switch` methods for `abstract partial record` |

#### EntityIdGenerator

Applying `[GenerateEntityId]` to an Entity/AggregateRoot class automatically generates a Ulid-based EntityId.

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId> { ... }
```

**Generated code:**
- `ProductId` record struct -- implements `IEntityId<ProductId>`, `IParsable<ProductId>`
- `ProductIdConverter` -- EF Core `ValueConverter<ProductId, string>`
- `ProductIdComparer` -- EF Core `ValueComparer<ProductId>`
- JSON serialization/deserialization (`JsonConverter`)
- Comparison operators (`<`, `>`, `<=`, `>=`)

#### ObservablePortGenerator

Applying `[GenerateObservablePort]` to an IObservablePort implementation class automatically generates an Observability wrapping Pipeline.

```csharp
[GenerateObservablePort]
public class EfCoreProductRepository : IProductRepository { ... }
```

**Generated code:**
- `EfCoreProductRepositoryPipeline` class -- inherits from the original class
- Overrides each method to add:
  - `ActivitySource` distributed tracing (span creation)
  - `ILogger` structured logging (request/response/error)
  - `IMeterFactory` metrics (counters, histograms)
  - Error classification (Expected vs Exceptional)

#### UnionTypeGenerator

Applying `[UnionType]` to an `abstract partial record` automatically generates pattern matching methods.

```csharp
[UnionType]
public abstract partial record Shape
{
    public sealed record Circle(double Radius) : Shape;
    public sealed record Rectangle(double Width, double Height) : Shape;
}
```

**Generated code:**
- `Match<TResult>(...)` -- exhaustive pattern matching for all derived types
- `Switch(...)` -- void-returning version of pattern matching

### Mediator.SourceGenerator

| Item | Value |
|------|-----|
| Package | `Mediator.SourceGenerator` (v3.0.1) |
| Purpose | Auto-generation of Mediator pattern handler code |

**Note:** When referencing the host project from a test project, Mediator.SourceGenerator may run in duplicate, causing build errors. In this case, add `ExcludeAssets="analyzers"` to the host project reference.

```xml
<ProjectReference Include="..." ExcludeAssets="analyzers" />
```

## Source Generator Debugging

### Debugging Method Comparison

| Method | Stability | Repeatability | Recommended |
|------|--------|--------|------|
| Debugging from test project | High | High | Recommended |
| Using `Debugger.Launch()` | Medium | Medium | For emergencies |
| Attach to Process | Low | Low | Not recommended |

### Method 1: Debugging from Test Project (Recommended)

Debug the source generator using existing unit tests.

1. Set breakpoints in the test file (e.g., at the `_sut.Generate(input)` call site)
2. Also set breakpoints in the source generator code
3. Click **Debug** in Visual Studio Test Explorer or **Debug Test** above the code
4. Step into the source generator internals with F11 (Step Into)

**Advantages:** No compiler process timing issues, can test multiple times with the same input, full build not required

### Method 2: Using Debugger.Launch()

Automatically displays the debugger attach dialog when compilation starts.

1. Set the Generator class's `AttachDebugger` parameter to `true`:
   ```csharp
   [Generator(LanguageNames.CSharp)]
   public sealed class ObservablePortGenerator()
       : IncrementalGeneratorBase<ObservableClassInfo>(
           RegisterSourceProvider,
           Generate,
           AttachDebugger: true)  // Enable debugging
   ```
2. Completely restart Visual Studio
3. Build a project that uses the source generator
4. Select the current VS instance in the Just-In-Time Debugger dialog
5. **After debugging, make sure to revert to `AttachDebugger: false`** (do not commit)

### Debugging Tips

**Check generated code:** In Solution Explorer > Dependencies > Analyzers > `Functorium.SourceGenerators`, check the generated `.g.cs` files

**Check from build log:**

```powershell
dotnet build Observability.Adapters.Infrastructure -v:diag > build.log
# Search for "SourceGenerator" in build.log
```

**Useful Watch window expressions:**

```csharp
classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
classSymbol.AllInterfaces.Select(i => i.Name).ToArray()
method.Parameters.Select(p => p.Type.ToDisplayString()).ToArray()
```

**Conditional breakpoints:** Right-click breakpoint > Conditions, set conditions like `className == "RepositoryIo"`

### Troubleshooting

| Symptom | Cause | Solution |
|------|------|------|
| Debugger not attached | Insufficient VS administrator privileges | Run VS as administrator |
| Breakpoint shows hollow circle | Symbols not loaded | Restart VS + delete bin/obj and rebuild |
| Code changes not reflected | Build cache | Close VS -> delete bin/obj -> restart VS -> Clean -> Rebuild |
| Cannot debug test | ProjectReference setting | Check `OutputItemType="Analyzer" ReferenceOutputAssembly="true"` |

While source generators operate at compile time, .NET 10 file-based programs execute `.cs` files directly without a separate build.

## .NET 10 File-Based Programs

.NET 10 supports "file-based programs" that execute `.cs` files directly. NuGet dependencies are declared with the `#:package` directive.

> Each script folder has its own `Directory.Build.props` that blocks the root `Directory.Build.props` Source Link dependency. For details, see [02-solution-configuration.md §Nested configuration files](../02-solution-configuration#nested-configuration-files).

### SummarizeSlowestTests.cs

| Item | Value |
|------|-----|
| Location | `.coverage/scripts/SummarizeSlowestTests.cs` |
| Purpose | Generate slow test analysis report from TRX test results |
| NuGet | `System.CommandLine`, `Microsoft.Extensions.FileSystemGlobbing` |

**Generated reports:**
- Overall test statistics (passed, failed, skipped)
- Execution time distribution per test project
- Top 100 slowest test list
- Failed test summary
- Percentile analysis (50th, 90th, 95th, 99th)

**Execution:**

```powershell
dotnet .coverage/scripts/SummarizeSlowestTests.cs `
  --glob "**/*.trx" `
  --threshold 30 `
  --output .coverage/reports
```

**Key parameters:**

| Parameter | Description | Default |
|---------|------|--------|
| `--glob` | TRX file search pattern | -- |
| `--threshold` | Slow test threshold (seconds) | 30 |
| `--output` | Report output directory | -- |

> Automatically performed in `Build-Local.ps1` Step 9.

### ApiGenerator.cs

| Item | Value |
|------|-----|
| Location | `.release-notes/scripts/ApiGenerator.cs` |
| Purpose | Generate Public API surface text from compiled DLLs |
| NuGet | `PublicApiGenerator` |

**Behavior:**
1. Extracts Public API from the specified DLL
2. Automatically resolves .NET 10 / ASP.NET Core reference assemblies
3. Outputs API definitions as text or file

**Execution:**

```powershell
dotnet .release-notes/scripts/ApiGenerator.cs `
  --dll-path bin/Release/net10.0/MyLib.dll `
  --output-path .api/MyLib.cs
```

### ExtractApiChanges.cs

| Item | Value |
|------|-----|
| Location | `.release-notes/scripts/ExtractApiChanges.cs` |
| Purpose | Extract API changes between branches (for release notes) |
| NuGet | `System.CommandLine`, `Spectre.Console` |

**Behavior:**
1. Search Functorium source projects (excluding tests)
2. Publish each project in Release mode
3. Call `ApiGenerator.cs` to generate API files
4. Merge all APIs into a single uber file
5. Extract changes via Git diff
6. Generate summary report

**Output:** `.analysis-output/api-changes-build-current/`

Now that we have reviewed the detailed usage of individual tools, let us finally take an overall view of all tools.

## Complete Tool Map

### CLI Tools

The following table provides a complete map of CLI tools, source generators, and scripts organized by category.

| Package | Command | Purpose | Build-Local.ps1 Step |
|--------|------|------|---------------------|
| `dotnet-reportgenerator-globaltool` | `reportgenerator` | Coverage report | Step 7 |
| `verify.tool` | `dotnet-verify` | Snapshot approval | Build-VerifyAccept.ps1 |
| `gman.siren` | `siren-gen` | ER diagram | Manual execution |

### Source Generators

| Generator | Attribute | Project |
|--------|-----------|---------|
| EntityIdGenerator | `[GenerateEntityId]` | Functorium.SourceGenerators |
| ObservablePortGenerator | `[GenerateObservablePort]` | Functorium.SourceGenerators |
| UnionTypeGenerator | `[UnionType]` | Functorium.SourceGenerators |
| Mediator.SourceGenerator | Interface-based | NuGet (v3.0.1) |

### .NET 10 File-Based Programs

| File | Purpose | Build-Local.ps1 Step |
|------|------|---------------------|
| `.coverage/scripts/SummarizeSlowestTests.cs` | Slow test analysis | Step 9 |
| `.release-notes/scripts/ApiGenerator.cs` | Public API surface generation | Manual/Release |
| `.release-notes/scripts/ExtractApiChanges.cs` | API change extraction | Manual/Release |

## New Tool Addition Checklist

### Adding a CLI Tool

1. `dotnet tool install <package-name>` (automatically registered in manifest)
2. Check `rollForward` setting in `.config/dotnet-tools.json` (set to `true` if tool target framework is lower than current SDK)
3. Update the tool list table in [02-solution-configuration.md](../02-solution-configuration)
4. Add a detailed usage section in this document

### Adding a Source Generator

1. Add `<PackageVersion>` to `Directory.Packages.props`
2. Add `<PackageReference>` to the using project's `.csproj`
3. Check whether `ExcludeAssets="analyzers"` is needed for test project references

### Adding a .NET 10 Script

1. Create a `.cs` file in the appropriate directory
2. Declare NuGet dependencies with the `#:package` directive
3. Check whether a `Directory.Build.props` exists in the folder (create if root props blocking is needed)
4. Consider updating the target pattern in `Build-CleanRunFileCache.ps1`

## Troubleshooting

### rollForward Related Error

**Symptom:** "The tool ... is not supported on the current .NET SDK" error during `dotnet tool restore`

**Resolution:** Set the tool's `rollForward` to `true` in `.config/dotnet-tools.json`.

```json
"tool-name": {
  "version": "x.y.z",
  "commands": ["cmd"],
  "rollForward": true
}
```

### .NET 10 Script Package Loading Error

**Symptom:** Packages like `System.CommandLine` are not loaded, or a previous version cache is used

**Resolution:** Clean the runfile cache with `Build-CleanRunFileCache.ps1`.

```powershell
# Clean specific script cache
./Build-CleanRunFileCache.ps1

# Clean all runfile cache
./Build-CleanRunFileCache.ps1 -Pattern "All"

# Check deletion targets only
./Build-CleanRunFileCache.ps1 -WhatIf
```

Cache location: `%TEMP%\dotnet\runfile\`

### Source Generator CS0436 Type Conflict Warning

**Symptom:** `warning CS0436: The type 'AssemblyReference' conflicts`

**Cause:** Occurs when Project A uses a Source Generator and Project B references A while using the same Source Generator. If `InternalsVisibleTo` is configured, internal generated types conflict.

**Resolution (3 combinable approaches):**

1. **Add NoWarn (recommended):**
   ```xml
   <PropertyGroup>
     <NoWarn>$(NoWarn);CS0436</NoWarn>
   </PropertyGroup>
   ```

2. **Disable Generator (Mediator example):**
   ```xml
   <PropertyGroup>
     <Mediator_DisableGenerator>true</Mediator_DisableGenerator>
   </PropertyGroup>
   ```

3. **ExcludeAssets setting:**
   ```xml
   <ProjectReference Include="..\ProjectA\ProjectA.csproj">
     <ExcludeAssets>analyzers</ExcludeAssets>
   </ProjectReference>
   ```

**Affected libraries:** Commonly occurs in libraries using Source Generator patterns such as Mediator, CommunityToolkit.Maui, StronglyTypedId, xUnit, etc.

> The CS0436 warning does not affect functionality and can be safely suppressed with `NoWarn`. However, in projects with `TreatWarningsAsErrors` enabled, it causes build failures and must be addressed.

### Siren Assembly Mode Failure

**Symptom:** NullReferenceException or empty result when running `siren-gen -a <dll>`

**Cause:** Assembly mode may not work in projects that do not use EF Core Migrations (`EnsureCreated()` pattern).

**Resolution:**
1. Use connection string mode: First create the DB, then run `siren-gen -c "Data Source=..."`
2. Write the Mermaid ER diagram manually (manual alternative)

## FAQ

### Q1. What is the difference between CLI tools and source generators?

CLI tools are managed via the `dotnet tool` manifest and run independently from the command line. Source generators are referenced as NuGet packages and automatically generate code at compile time. CLI tools operate in the build pipeline, while source generators operate in real-time during development.

### Q2. When should rollForward be set to true?

Set it when the tool's target framework is lower than the current SDK version. For example, running a .NET 9 target tool on a .NET 10 SDK requires `rollForward: true`. Currently `gman.siren` uses this setting.

### Q3. What should I do when a package error occurs in .NET 10 file-based programs?

Clean the runfile cache with `Build-CleanRunFileCache.ps1`. The cache location is `%TEMP%\dotnet\runfile\`, and you can clean all caches with the `-Pattern "All"` option or clean only specific script caches with the default.

### Q4. How can I check the code generated by Source Generators?

In Visual Studio's Solution Explorer, check the generated `.g.cs` files under Dependencies > Analyzers > `Functorium.SourceGenerators`. Alternatively, generate a build log with `dotnet build -v:diag > build.log` and search for "SourceGenerator".

### Q5. What is the alternative when the Siren tool fails to generate ER diagrams?

Siren's assembly mode requires EF Core Migrations, and connection string mode is SQL Server only. To bypass these constraints, use the `Build-ERDiagram.ps1` script to directly generate Mermaid ER diagrams based on EF Core Configuration.

---

## References

- [02-solution-configuration.md](../02-solution-configuration) -- dotnet-tools.json management, build script pipeline
- [01-project-structure.md](../01-project-structure) -- Project structure and dependencies
- [15a-unit-testing.md](../testing/15a-unit-testing) -- Unit testing (including Verify.Xunit snapshots)
- [16-testing-library.md](../testing/16-testing-library) -- Functorium.Testing library (including source generator testing)
