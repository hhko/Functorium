---
title: "Environment Setup"
---

## Required Environment

| Item | Minimum Version | Notes |
|------|----------------|-------|
| .NET SDK | 10.0 | Verify with `dotnet --version` |
| IDE | VS 2022 / Rider / VS Code | IDE with C# support |

## Required Packages

The following NuGet packages are needed for architecture test projects.

### Test Framework

These packages provide the foundation for test execution and verification.

| Package | Purpose |
|---------|---------|
| `xunit.v3` | Test framework |
| `xunit.runner.visualstudio` | IDE test explorer support |
| `Microsoft.NET.Test.Sdk` | .NET test SDK |

### Architecture Testing

These packages analyze assemblies and verify rules.

| Package | Purpose |
|---------|---------|
| `TngTech.ArchUnitNET.xUnitV3` | ArchUnitNET xUnit integration |
| `Shouldly` | Assertion library |

### Project References

These references are needed to use Functorium's Validator pattern (`ClassValidator`, `InterfaceValidator`, etc.).

| Project | Purpose |
|---------|---------|
| `Functorium.Testing` | ArchitectureRules framework (ClassValidator, etc.) |

## Project Structure

Each chapter follows this structure:

```txt
01-Chapter-Name/
├── README.md                           # Chapter description
├── ProjectName/                        # Target project for verification
│   ├── ProjectName.csproj
│   ├── Program.cs
│   └── Domains/                        # Domain classes
│       └── ...
└── ProjectName.Tests.Unit/             # Architecture tests
    ├── ProjectName.Tests.Unit.csproj
    ├── xunit.runner.json
    ├── ArchitectureTestBase.cs         # Common setup
    └── XxxArchitectureTests.cs         # Test files
```

## Test Project Configuration

### .csproj File

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
    <PackageReference Include="Shouldly" />
    <PackageReference Include="TngTech.ArchUnitNET.xUnitV3" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProjectName\ProjectName.csproj" />
    <ProjectReference Include="..\..\..\..\..\..\Src\Functorium.Testing\Functorium.Testing.csproj" />
  </ItemGroup>

</Project>
```

> Package versions are centrally managed in `Directory.Packages.props`.

### xunit.runner.json

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true,
  "methodDisplay": "method",
  "methodDisplayOptions": "replaceUnderscoreWithSpace",
  "diagnosticMessages": true
}
```

## Architecture Loading Pattern

All architecture tests inherit from the `ArchitectureTestBase` class. This class loads the assemblies and provides namespace strings, so individual tests can focus solely on rule definitions.

```csharp
using ArchUnitNET.Loader;

public abstract class ArchitectureTestBase
{
    protected static readonly ArchUnitNET.Domain.Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(SomeClassInTargetAssembly).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(SomeClassInTargetAssembly).Namespace!;
}
```

> **"ArchitectureTestBase defines 'what to analyze' in one place. Individual tests only need to focus on 'what rules to apply'."**

**Key points:**

1. Load the target assembly for verification with `ArchLoader`
2. Reference the assembly with `typeof(...).Assembly`
3. Safely extract the namespace string with `typeof(...).Namespace!`
4. Declare as `static readonly` fields to reuse across tests

## Running Tests

```bash
# Test individual project
dotnet test --project Path/To/ProjectName.Tests.Unit

# Test entire solution
dotnet test --solution architecture-rules.slnx
```

## FAQ

### Q1: Can multiple assemblies be loaded at once with `ArchLoader`?
**A**: Yes, you can pass multiple assemblies like `LoadAssemblies(assembly1, assembly2, ...)`. When verifying dependency rules between layers, you need to load the domain, application, and infrastructure assemblies together.

### Q2: Does `ArchitectureTestBase` have to be an abstract class?
**A**: It is not mandatory, but making it abstract prevents the test framework from recognizing this class itself as a test. It also clearly communicates the intent that each test class must inherit from it.

### Q3: Why declare it as `static readonly`?
**A**: Loading assemblies is an expensive operation. Declaring it as `static readonly` ensures it is loaded only once per test class, significantly improving overall test execution speed.

---

## Next Steps

Now that the environment setup is complete, let's write the first architecture test.

-> [Part 1: ClassValidator Basics - 1.1 First Architecture Test](../Part1-ClassValidator-Basics/01-First-Architecture-Test/)
