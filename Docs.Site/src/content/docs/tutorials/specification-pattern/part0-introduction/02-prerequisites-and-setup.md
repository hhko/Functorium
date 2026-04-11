---
title: "Environment Setup"
---
Prepare the environment for hands-on learning by running code directly. Each step takes just a few minutes.

## Required Prerequisites

### 1. .NET 10.0 SDK

The .NET SDK is required for building and running all projects.

```bash
# Version check
dotnet --version
# Example output: 10.0.100

# Install (Windows)
winget install Microsoft.DotNet.SDK.10

# Install (macOS)
brew install --cask dotnet-sdk

# Install (Linux - Ubuntu)
sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
```

### 2. IDE Setup

#### VS Code + C# Dev Kit

- Install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension

---

## Project Setup

### Clone Source Code

```bash
# Clone the Functorium project
git clone https://github.com/hhko/Functorium.git
cd functorium
```

### Verify Build

```bash
# Build the entire solution
dotnet build Functorium.slnx

# Run all tests
dotnet test --solution Functorium.slnx
```

### Run Individual Projects

```bash
# Build the entire tutorial
dotnet build specification-pattern.slnx

# Test the entire tutorial
dotnet test --solution specification-pattern.slnx
```

---

## Default Using Statements

Default using statements used in projects:

```csharp
using Functorium.Domains.Specifications;
```

When using Expression Specification:

```csharp
using System.Linq.Expressions;
using Functorium.Domains.Specifications;
```

---

## How to Run Each Project

### Running Tests

```bash
# Test the entire tutorial
dotnet test --solution specification-pattern.slnx

# Run specific tests only
dotnet test --solution specification-pattern.slnx --filter "IsSatisfiedBy_ReturnsTrue_WhenProductIsActive"
```

### Full Solution Test

```bash
# From the solution root
dotnet test --solution specification-pattern.slnx
```

---

## Project Structure

Each tutorial project has the following structure:

```
01-First-Specification/
├── FirstSpecification/                    # Main project
│   ├── FirstSpecification.csproj          # Project file
│   └── Specifications/                    # Specification classes
│       └── ActiveProductSpec.cs
│
└── FirstSpecification.Tests.Unit/         # Test project
    ├── FirstSpecification.Tests.Unit.csproj
    ├── xunit.runner.json
    └── ActiveProductSpecTests.cs
```

---

## Troubleshooting

### .NET SDK Not Recognized

```bash
# Check PATH environment variable
echo $PATH

# For Windows, add the following path to system environment variables
# C:\Program Files\dotnet
```

### IntelliSense Not Working in IDE

1. Restart the IDE
2. Run `dotnet restore`
3. Delete the `.vs` or `.vscode` folder and restart

---

## FAQ

### Q1: Is .NET 10.0 SDK mandatory?
**A**: Yes. All projects in this tutorial target .NET 10.0. Some Functorium library APIs require .NET 10.0 or later, so the build may fail on earlier versions.

### Q2: Can individual projects be built/tested independently?
**A**: Yes, running `dotnet test` from each chapter's project folder tests that project independently. The full solution build is performed with `dotnet build Functorium.slnx`.

### Q3: Which IDE should I use?
**A**: Use whichever is comfortable among VS Code + C# Dev Kit, JetBrains Rider, or Visual Studio 2022. Any IDE will work as long as the C# development environment is set up and .NET 10.0 SDK is installed.

---

## Next Steps

Environment setup is complete. Now let's look at the big picture of what problems the Specification pattern solves.

-> [0.3 Specification Pattern Overview](03-specification-pattern-overview.md)
