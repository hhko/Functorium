---
title: "Environment Setup"
---
## Requirements

### 1. .NET 10.0 SDK

```bash
# Check version
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

#### JetBrains Rider

- .NET 10.0 SDK is automatically detected
- Verify the SDK path in File -> Settings -> Build, Execution, Deployment -> Toolset and Build

#### Visual Studio 2022

- Visual Studio 2022 version 17.12 or later recommended
- Workloads: ".NET desktop development" or "ASP.NET and web development"

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
# Run tests for Part 1 first chapter
dotnet test --project Docs.Site/src/content/docs/tutorials/cqrs-repository/Part1-Domain-Entity-Foundations/01-Entity-And-Identity/EntityAndIdentity.Tests.Unit

# Build/test the entire tutorial
dotnet build Docs.Site/src/content/docs/tutorials/cqrs-repository/cqrs-repository.slnx
dotnet test --solution Docs.Site/src/content/docs/tutorials/cqrs-repository/cqrs-repository.slnx
```

---

## Default Using Statements

These are the default using statements used in the projects:

```csharp
// Domain entities
using Functorium.Domains.Entities;

// Repository
using Functorium.Domains.Repositories;

// Query adapter
using Functorium.Applications.Queries;

// Usecase
using Functorium.Applications.Usecases;

// Specification
using Functorium.Domains.Specifications;
```

When using LanguageExt functional types:

```csharp
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
```

---

## Running Each Project

### Running Tests

```bash
# Run tests for a specific project
dotnet test --project Docs.Site/src/content/docs/tutorials/cqrs-repository/Part1-Domain-Entity-Foundations/01-Entity-And-Identity/EntityAndIdentity.Tests.Unit

# Run a specific test
dotnet test --filter "Create_ReturnsAggregate_WhenValid"
```

### Running All Solution Tests

```bash
# From the solution root
dotnet test --solution Functorium.slnx
```

---

## Project Structure

Each tutorial project follows this structure:

```
01-Entity-And-Identity/
├── EntityAndIdentity/                       # Main project
│   ├── EntityAndIdentity.csproj             # Project file
│   └── Domains/                             # Domain classes
│       ├── Product.cs
│       └── ProductId.cs
│
└── EntityAndIdentity.Tests.Unit/            # Test project
    ├── EntityAndIdentity.Tests.Unit.csproj
    ├── xunit.runner.json
    └── ProductTests.cs
```

---

## Functorium Dependencies

Each project references the Functorium library. It is configured in the project file as follows:

```xml
<ItemGroup>
    <ProjectReference Include="../../../../../../../../../Src/Functorium/Functorium.csproj" />
</ItemGroup>
```

Key CQRS-related types provided by Functorium:

| Namespace | Key Types | Purpose |
|-----------|-----------|---------|
| `Functorium.Domains.Entities` | Entity\<TId\>, AggregateRoot\<TId\>, IEntityId | Domain entities |
| `Functorium.Domains.Repositories` | IRepository\<TAggregate, TId\> | Command-side Repository |
| `Functorium.Applications.Queries` | IQueryPort\<TEntity, TDto\> | Query-side adapter |
| `Functorium.Applications.Usecases` | ICommandRequest, IQueryRequest | Usecase interfaces |
| `Functorium.Applications.Persistence` | IUnitOfWork, IUnitOfWorkTransaction | Persistence |

---

## Troubleshooting

### .NET SDK Not Recognized

```bash
# Check PATH environment variable
echo $PATH

# On Windows, add the following path to system environment variables
# C:\Program Files\dotnet
```

### IntelliSense Not Working in IDE

1. Restart the IDE
2. Run `dotnet restore`
3. Delete the `.vs` or `.vscode` folder and restart

### LanguageExt Build Errors

Verify that the LanguageExt.Core package has been properly restored:

```bash
dotnet restore
dotnet build
```

---

## FAQ

### Q1: Can I use an earlier version instead of .NET 10.0 SDK?
**A**: The projects in this tutorial target .NET 10.0. Some LanguageExt and Functorium APIs may not be compatible with earlier versions, so installing the .NET 10.0 SDK is recommended.

### Q2: Can I test individual projects without building the entire solution?
**A**: Yes, running `dotnet test` from each chapter's test project folder will build/test only that project. Building the entire solution is used to verify consistency across all projects.

### Q3: What should I do if LanguageExt build errors occur?
**A**: First, restore packages with `dotnet restore`. If that doesn't resolve it, delete the `bin` and `obj` folders and rebuild. Also verify that the LanguageExt.Core package downloads correctly from the NuGet source.

---

With the environment setup complete, it's time to understand the overall structure of the CQRS pattern.

-> [Chapter 0.3: CQRS Pattern Overview](03-cqrs-pattern-overview.md)
