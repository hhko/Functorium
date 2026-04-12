---
title: "Prerequisites and Setup"
---

Please verify the following items before starting the tutorial.

## Required Tools

| Tool | Version | Purpose |
|------|------|------|
| .NET SDK | 10.0 or higher | Build and run |
| VS Code | Latest | Code editing |
| C# Dev Kit | Latest | C# development support |

## Prerequisite Knowledge

You should be familiar with the following concepts to study this tutorial:

| Concept | Level | Description |
|------|------|------|
| C# Generics | Basic | Basic generic syntax like `List<T>`, `where T : class` |
| Interfaces | Basic | Interface definition, implementation, polymorphism |
| Record types | Basic | `record class`, sealed record, positional record |
| Mediator pattern | Optional | Needed from Part 2 onward (not required for Part 1) |

## Project Build

```bash
# Clone the repository
git clone https://github.com/hhko/Functorium.git
cd Functorium

# Build the tutorial
dotnet build Docs.Site/src/content/docs/tutorials/usecase-pipeline/usecase-pipeline.slnx

# Run tutorial tests
dotnet test --solution Docs.Site/src/content/docs/tutorials/usecase-pipeline/usecase-pipeline.slnx

# Build the entire solution
dotnet build Functorium.slnx
```

## Project Types

The projects in this tutorial are divided into two types:

| Type | Part | References | Description |
|------|------|------|------|
| Standalone | Part 1, 2 (Sections 2-3), 3 | LanguageExt.Core only | For concept learning, independently runnable |
| Functorium Reference | Part 2 (Section 1), 4, 5 | Functorium.csproj | Practical application, uses Pipeline/Usecase |

### Standalone Projects

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="LanguageExt.Core" />
  </ItemGroup>
</Project>
```

### Functorium Reference Projects

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
  </ItemGroup>
</Project>
```

## Recommended Study Order

It is recommended to study Parts 1-3 in order. Parts 4-5 can be studied freely after completing Part 3.

## FAQ

### Q1: Can Part 1 be studied without Mediator pattern knowledge?
**A**: Yes. Part 1 covers only C# generic variance (covariance/contravariance/invariance), so Mediator pattern knowledge is not needed. `IPipelineBehavior` first appears in Part 2, so understanding the basics of the Mediator pattern by then is sufficient.

### Q2: What is the difference between Standalone and Functorium Reference projects?
**A**: Standalone projects reference only `LanguageExt.Core` and are independently runnable for concept learning. Functorium Reference projects reference `Functorium.csproj` and are used for practical Pipeline and Usecase implementation. Parts 1-3 primarily use Standalone, while Parts 4-5 use Functorium Reference projects.

### Q3: Can I follow the tutorial with a .NET SDK version earlier than 10.0?
**A**: This tutorial uses C# 11's `static abstract` members and modern record syntax. `static abstract` is supported from .NET 7 onward, but since the project build settings target .NET 10, .NET SDK 10.0 or higher is recommended.

---

The following section examines the overall structure of Mediator Pipelines and the capabilities each Pipeline requires from the response type.

→ [Section 0.3: Usecase Pipeline Architecture Overview](03-usecase-pipeline-overview.md)
