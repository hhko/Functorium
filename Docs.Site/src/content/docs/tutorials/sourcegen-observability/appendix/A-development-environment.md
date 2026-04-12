---
title: "Development Environment"
---

This appendix provides a quick reference for the environment required for source generator development. Use it when reconstructing the environment after completing the main tutorial. For detailed explanations of each item, refer to [Part 1-01. Development Environment Setup](../Part1-Fundamentals/01-development-environment.md).

---

## Required Tools

| Tool | Minimum Version | Purpose |
|------|----------------|---------|
| .NET SDK | 10.0 | Source generator build and test |
| Visual Studio 2022 | 17.12+ | IDE (source generator debugging support) |
| VS Code + C# Dev Kit | Latest | Alternative IDE |

```bash
# Verify installation
dotnet --version
# Example output: 10.0.100
```

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# compiler API (Syntax, Semantic) |
| `Microsoft.CodeAnalysis.Analyzers` | Analyzer development rule validation |

Both packages should specify `PrivateAssets="all"` to prevent transitive dependency to consumer projects.

---

## Project Setup Checklist

| Property | Value | Description |
|----------|-------|-------------|
| `TargetFramework` | `netstandard2.0` | Required target for compatibility across all IDE/CLI environments |
| `IsRoslynComponent` | `true` | Recognized as a source generator component |
| `EnforceExtendedAnalyzerRules` | `true` | Enforces analyzer packaging rules |
| `IncludeBuildOutput` | `false` | Excludes build output from NuGet package distribution |

---

## Project Reference Configuration

### Production Project (using source generator)

```xml
<ProjectReference Include="..\MySourceGenerator\MySourceGenerator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

| Property | Description |
|----------|-------------|
| `OutputItemType="Analyzer"` | Recognized as an analyzer/generator |
| `ReferenceOutputAssembly="false"` | Excludes runtime reference (compile-time only) |

### Test Project (source generator debugging)

```xml
<ProjectReference Include="..\MySourceGenerator\MySourceGenerator.csproj"
                  ReferenceOutputAssembly="true" />
```

In test projects, set `ReferenceOutputAssembly="true"` to allow the debugger to step into the source generator internals.

---

## Further Reading

→ [Part 1-01. Development Environment Setup](../Part1-Fundamentals/01-development-environment.md)
