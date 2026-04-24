---
title: "ApiGenerator"
---

How do you accurately extract only the Public API from a .NET DLL? You could enumerate types using reflection, but outputting generic constraints, extension methods, and namespace structures cleanly in C# format requires considerable work. ApiGenerator.cs solves this problem using the PublicApiGenerator library. If ExtractApiChanges.cs is the orchestrator that builds projects and assembles results, ApiGenerator.cs is the specialist that actually opens DLLs and extracts Public APIs.

## File Location and Usage

```txt
.release-notes/scripts/ApiGenerator.cs
```

```bash
# Output to file
dotnet ApiGenerator.cs <dll-path> <output-file>

# Output to console (using -)
dotnet ApiGenerator.cs <dll-path> -
```

When called from ExtractApiChanges.cs, console output mode (`-`) is used to receive results through the pipeline.

## Package References

Unlike other scripts, this script uses the **PublicApiGenerator package.** This library, made by Microsoft, provides the core functionality of extracting an assembly's Public API in C# format.

```csharp
#!/usr/bin/env dotnet

#:package PublicApiGenerator@11.3.0
#:package System.CommandLine@2.0.1

using System;
using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using PublicApiGenerator;
```

## Script Structure

The CLI takes two Arguments: the DLL path and the output file path.

```csharp
var dllArgument = new Argument<string>("dll", "Path to the DLL file");
var outputArgument = new Argument<string>("output", "Output file path (use - for stdout)");

var rootCommand = new RootCommand("Generate public API from a DLL")
{
    dllArgument,
    outputArgument
};

rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var dllPath = parseResult.GetValue(dllArgument)!;
    var outputPath = parseResult.GetValue(outputArgument)!;

    GenerateApi(dllPath, outputPath);
    return 0;
});

return rootCommand.Parse(args).Invoke();
```

## DLL Loading and Dependency Resolution

To extract APIs, the DLL must be loaded into memory. However, simply using `Assembly.LoadFrom()` can cause problems. The target DLL may fail to load because it cannot find other assemblies it references.

To solve this problem, a **custom AssemblyLoadContext** is used. .NET's AssemblyLoadContext is a mechanism for isolating and customizing assembly loading. When looking for dependencies, it first searches the default context (assemblies already loaded in the runtime), and if not found, searches the same directory as the DLL. Since `dotnet publish` copies all dependencies to the output directory, searching the same directory resolves most dependencies.

```csharp
static void GenerateApi(string dllPath, string outputPath)
{
    // Check DLL existence
    if (!File.Exists(dllPath))
    {
        Console.Error.WriteLine($"Error: DLL not found: {dllPath}");
        Environment.Exit(1);
    }

    var dllDirectory = Path.GetDirectoryName(dllPath)!;

    // Create custom AssemblyLoadContext
    var loadContext = new CustomAssemblyLoadContext(dllDirectory);

    // Load assembly
    var assembly = loadContext.LoadFromAssemblyPath(dllPath);
}
```

```csharp
class CustomAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _basePath;

    public CustomAssemblyLoadContext(string basePath) : base(isCollectible: true)
    {
        _basePath = basePath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Search default context first
        try
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }
        catch { }

        // Search same directory
        var dllPath = Path.Combine(_basePath, $"{assemblyName.Name}.dll");
        if (File.Exists(dllPath))
        {
            return LoadFromAssemblyPath(dllPath);
        }

        return null;
    }
}
```

Creating with `isCollectible: true` allows garbage collection after use. If a dependency is not found, `null` is returned instead of an exception, preventing the entire process from failing due to non-essential assemblies.

## Public API Extraction

Once the assembly is loaded, the API is extracted with PublicApiGenerator. Options are used to exclude unnecessary assembly attributes and compiler-generated namespaces.

```csharp
// PublicApiGenerator options
var options = new ApiGeneratorOptions
{
    IncludeAssemblyAttributes = false,  // Exclude assembly attributes
    DenyNamespacePrefixes = new[]       // Namespaces to exclude
    {
        "System.Runtime.CompilerServices",
        "Microsoft.CodeAnalysis"
    }
};

// Generate API
var publicApi = assembly.GeneratePublicApi(options);
```

Results are output to console (`-`) or file.

```csharp
// Console output (-) or file output
if (outputPath == "-")
{
    Console.Write(publicApi);
}
else
{
    File.WriteAllText(outputPath, publicApi);
    Console.WriteLine($"API written to: {outputPath}");
}
```

## Output Format

The generated API text is similar to actual C# code but has some characteristics. Method bodies are shown only as `{ }`, type names are output as full paths (`LanguageExt.Common.Error`, `System.Exception`), and generic constraints and the `this` keyword for extension methods are preserved as-is.

```csharp
namespace Functorium.Abstractions.Errors
{
    public static class ErrorFactory
    {
        public static LanguageExt.Common.Error Create(string errorCode, string errorCurrentValue, string errorMessage) { }
        public static LanguageExt.Common.Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage)
            where T : notnull { }
        public static LanguageExt.Common.Error CreateFromException(string errorCode, System.Exception exception) { }
    }
}

namespace Functorium.Abstractions.Registrations
{
    public static class OpenTelemetryRegistration
    {
        public static Functorium.Adapters.Observabilities.Builders.OpenTelemetryBuilder RegisterObservability(
            this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration configuration) { }
    }
}
```

## PublicApiGenerator Options

The extraction scope can be adjusted as needed.

| Option | Default | Description |
|--------|---------|-------------|
| `IncludeAssemblyAttributes` | true | Include assembly attributes |
| `DenyNamespacePrefixes` | (none) | Namespaces to exclude |
| `AllowNamespacePrefixes` | (none) | Namespaces to include |
| `ExcludeAttributes` | (none) | Attributes to exclude |

```csharp
var options = new ApiGeneratorOptions
{
    IncludeAssemblyAttributes = false,
    DenyNamespacePrefixes = new[]
    {
        "System.Runtime.CompilerServices",
        "Microsoft.CodeAnalysis"
    },
    ExcludeAttributes = new[]
    {
        "System.Diagnostics.DebuggerNonUserCodeAttribute"
    }
};
```

## Integration with ExtractApiChanges.cs

When ExtractApiChanges.cs calls ApiGenerator.cs, it uses console output mode. The output API text is received, an `<auto-generated>` header is added, and it is saved to a file.

```csharp
// In ExtractApiChanges.cs
var apiResult = await RunProcessAsync(
    "dotnet",
    $"\"{apiGeneratorPath}\" \"{dllPath}\" -"  // Output to console (-)
);

if (apiResult.ExitCode == 0)
{
    // Save API text to file
    var content = new StringBuilder();
    content.AppendLine("// <auto-generated>");
    content.Append(apiResult.Output);
    await File.WriteAllTextAsync(outputFile, content.ToString());
}
```

## Error Handling

Three cases are handled: DLL not found, dependency resolution failure, and API generation failure. Dependency resolution failure returns `null` to treat it as a non-fatal error, while DLL not found and API generation failure terminate the process.

```csharp
if (!File.Exists(dllPath))
{
    Console.Error.WriteLine($"Error: DLL not found: {dllPath}");
    Environment.Exit(1);
}
```

```csharp
try
{
    var publicApi = assembly.GeneratePublicApi(options);
    // ...
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error generating API: {ex.Message}");
    Environment.Exit(1);
}
```

## Practical Usage Example

You can also run it directly from the command line to check APIs.

```bash
# Extract API from Functorium.dll
dotnet ApiGenerator.cs bin/Release/net10.0/Functorium.dll api-output.cs

# Output to console for checking
dotnet ApiGenerator.cs bin/Release/net10.0/Functorium.dll - | head -50
```

Normally, ExtractApiChanges.cs calls it automatically, so direct execution is not necessary.

```bash
# ApiGenerator.cs is called internally when running ExtractApiChanges.cs
dotnet ExtractApiChanges.cs

# Check results
cat Src/Functorium/.api/Functorium.cs
```

We have now examined all three scripts used for Phase 2 data collection. AnalyzeAllComponents.cs collects Git changes, ExtractApiChanges.cs orchestrates API extraction, and ApiGenerator.cs reads actual APIs from DLLs. Once this data is prepared, the next step is the templates and configuration files that determine the structure of the release notes.

## FAQ

### Q1: Can't you extract APIs directly with reflection instead of `PublicApiGenerator`?
**A**: While it is possible to enumerate types and methods with reflection, considerable code is needed to cleanly output generic constraints (`where T : notnull`), the `this` keyword of extension methods, per-namespace sorting, and attribute display in C# format. **`PublicApiGenerator`** is a proven library that handles all of this, greatly reducing maintenance burden compared to direct implementation.

### Q2: Why set `isCollectible: true` in `CustomAssemblyLoadContext`?
**A**: Creating with `isCollectible: true` allows **garbage collection to release the context and loaded assemblies from memory** after use. This is useful for preventing memory accumulation in ExtractApiChanges.cs, which sequentially analyzes multiple DLLs.

### Q3: Is it safe to return `null` when a dependency is not found?
**A**: PublicApiGenerator does not necessarily need all dependent assemblies when extracting Public APIs. For example, assemblies for types used only in method bodies do not affect Public API extraction. Returning `null` causes the .NET runtime to **throw an exception only when that assembly is actually needed**, preventing unnecessary errors.

### Q4: Why are method bodies shown as `{ }` in the output format?
**A**: PublicApiGenerator extracts only the **API contract.** Implementation details of methods are not part of the Public API, so the body is left empty. What matters for the release notes is "what method exists with what signature", not internal implementation.

## Next Step

- [TEMPLATE.md Structure](07-template-structure.md)
