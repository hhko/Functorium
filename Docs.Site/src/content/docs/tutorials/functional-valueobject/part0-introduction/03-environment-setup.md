---
title: "Environment Setup"
---
## Required Prerequisites

### 1. .NET 10.0 SDK

```bash
# Check version
dotnet --version
# Expected output: 10.0.100

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

### Create a New Project

```bash
# Create a new project
dotnet new console -n MyValueObjectProject

# Install the LanguageExt package
cd MyValueObjectProject
dotnet add package LanguageExt.Core
```

### Basic using Statements

These are the basic using statements to use in your project:

```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
```

### GlobalUsings.cs (Optional)

You can manage using statements used throughout the project in a single place:

```csharp
// GlobalUsings.cs
global using LanguageExt;
global using LanguageExt.Common;
global using static LanguageExt.Prelude;
```

---

## Running Each Project

### Run a Project

```bash
# Navigate to a specific project
cd Docs/tutorials/Functional-ValueObject/01-Concept/01-Basic-Divide/BasicDivide

# Run the project
dotnet run
```

### Run Tests

```bash
# Navigate to the test project
cd Docs/tutorials/Functional-ValueObject/01-Concept/01-Basic-Divide/BasicDivide.Tests.Unit

# Run tests
dotnet test
```

### Build the Entire Solution

```bash
# From the solution root
dotnet build

# Run all tests
dotnet test
```

---

## Project Structure

Each tutorial project has the following structure:

```
01-Basic-Divide/
├── BasicDivide/                    # Main project
│   ├── Program.cs                  # Main entry file
│   ├── MathOperations.cs           # Core logic
│   ├── BasicDivide.csproj          # Project file
│   └── README.md                   # Project description
│
└── BasicDivide.Tests.Unit/         # Test project
    ├── MathOperationsTests.cs      # Test file
    ├── BasicDivide.Tests.Unit.csproj
    └── README.md
```

---

## Running the First Example

If the environment setup is complete, try running the first example:

```bash
cd Docs/tutorials/Functional-ValueObject/01-Concept/01-Basic-Divide/BasicDivide
dotnet run
```

### Expected Output

```
=== Basic Division Function ===

Normal case:
10 / 2 = 5

Exception case:
10 / 0 = System.DivideByZeroException: Attempted to divide by zero.
```

---

## Troubleshooting

### If the .NET SDK Is Not Recognized

```bash
# Check the PATH environment variable
echo $PATH

# On Windows, add the following path to system environment variables
# C:\Program Files\dotnet
```

### LanguageExt Package Installation Failure

```bash
# Check NuGet sources
dotnet nuget list source

# Add the official NuGet source
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### IntelliSense Not Working in the IDE

1. Restart the IDE
2. Run `dotnet restore`
3. Delete the `.vs` or `.vscode` folder and restart

## FAQ

### Q1: Can I run this with a version other than .NET 10?
**A**: The tutorial projects are written targeting .NET 10.0 SDK. Some code may work on earlier versions, but since there are parts that use .NET 10-specific features such as file-based program execution, .NET 10.0 or later is recommended.

### Q2: Do I have to create `GlobalUsings.cs`?
**A**: No. It is optional. Declaring `using LanguageExt;` etc. directly in each file works the same way. It is convenient when you want to reduce repetitive using statements across the entire project.

### Q3: Which version of the LanguageExt package should I install?
**A**: Running `dotnet add package LanguageExt.Core` installs the latest stable version. Each tutorial project has a verified version specified in its `.csproj` file, so building the solution will automatically restore the correct version.

---

## Next Steps

The environment setup is complete. Now in Part 1, you will directly observe the problems with a basic division function and take the first step toward evolving it into a value object.

→ [Chapter 1: Starting with Basic Division](../Part1-ValueObject-Concepts/01-Basic-Divide/)
