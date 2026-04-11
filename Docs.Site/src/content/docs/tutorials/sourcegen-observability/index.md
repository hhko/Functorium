---
title: "Source Generator Observability"
---

**A practical guide to auto-generating logging, tracing, and metrics code with the C# Roslyn API**

---

## About This Tutorial

If you are copy-pasting logging, tracing, and metrics code into every Repository method by hand, a Source Generator can put an end to that repetitive work.

This tutorial guides you from **learning C# Source Generators from scratch** to using them in production. Starting from the fundamentals of the Roslyn compiler platform, you will learn high-performance Source Generator development using the **IIncrementalGenerator pattern** step by step.

> **Automate repetitive boilerplate code and build a Source Generator that guarantees 100% consistent observability.**

### Target Audience

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers who know basic C# syntax but are new to Source Generators | Parts 0--1 |
| **Intermediate** | Developers without Roslyn API experience | All of Part 2 |
| **Advanced** | Developers who want to automate repetitive boilerplate code | Parts 3--4 + Appendix |

### Learning Objectives

After completing this tutorial, you will be able to:

1. Understand the architecture and operating principles of the **Roslyn compiler platform**
2. Develop Source Generators implementing the **IIncrementalGenerator interface**
3. Extract code metadata through **symbol analysis**
4. Apply **deterministic code generation** techniques
5. Write Source Generator **unit tests**

---

### Part 0: Introduction

Understand the concept and necessity of Source Generators.

- [0.1 What Is a Source Generator?](Part0-Introduction/01-what-is-source-generator.md)
- [0.2 Hello World Generator](Part0-Introduction/02-hello-world-generator/)
- [0.3 Why Source Generators Are Needed](Part0-Introduction/03-why-source-generator.md)
- [0.4 Reflection vs Source Generator](Part0-Introduction/04-reflection-vs-sourcegen/)
- [0.5 Project Overview](Part0-Introduction/05-project-overview.md)

### Part 1: Fundamentals

Set up the development environment and understand the Roslyn compiler platform.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Development Environment](Part1-Fundamentals/01-development-environment.md) | Development environment setup |
| 2 | [Project Structure](Part1-Fundamentals/02-Data-Models/) | Source Generator project configuration |
| 3 | [Debugging Setup](Part1-Fundamentals/03-Debugging-Setup/) | Debugging environment setup |
| 4 | [Roslyn Architecture](Part1-Fundamentals/04-Roslyn-Architecture/) | Compiler platform architecture |
| 5 | [Syntax API](Part1-Fundamentals/05-Syntax-Api/) | Syntax tree analysis |
| 6 | [Semantic API](Part1-Fundamentals/06-Semantic-Api/) | Semantic analysis |
| 7 | [Symbol Types](Part1-Fundamentals/07-Symbol-Types/) | Understanding Symbol types |

### Part 2: Core Concepts

Learn Incremental Source Generator implementation and code generation techniques.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [IIncrementalGenerator Interface](Part2-Core-Concepts/01-IIncrementalGenerator/) | Incremental Generator interface |
| 2 | [Provider Pattern](Part2-Core-Concepts/02-Provider-Pattern/) | Data Provider Pattern |
| 3 | [ForAttributeWithMetadataName](Part2-Core-Concepts/03-ForAttribute/) | Attribute-based filtering |
| 4 | [Incremental Caching](Part2-Core-Concepts/04-Incremental-Caching/) | Performance optimization |
| 5 | [INamedTypeSymbol](Part2-Core-Concepts/05-INamedTypeSymbol/) | Type Symbol analysis |
| 6 | [IMethodSymbol](Part2-Core-Concepts/06-IMethodSymbol/) | Method Symbol analysis |
| 7 | [SymbolDisplayFormat](Part2-Core-Concepts/07-SymbolDisplayFormat/) | Symbol display format |
| 8 | [Type Extraction](Part2-Core-Concepts/08-Type-Extraction/) | Extracting type information |
| 9 | [StringBuilder Pattern](Part2-Core-Concepts/09-StringBuilder-Pattern/) | Basic code generation |
| 10 | [Template Design](Part2-Core-Concepts/10-Template-Design/) | Structuring code templates |
| 11 | [Namespace Handling](Part2-Core-Concepts/11-Namespace-Handling/) | Namespace management |
| 12 | [Deterministic Output](Part2-Core-Concepts/12-Deterministic-Output/) | Deterministic code generation |

### Part 3: Advanced

Learn complex case handling and testing strategies.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Constructor Handling](Part3-Advanced/01-Constructor-Handling/) | Constructor analysis and generation |
| 2 | [Generic Types](Part3-Advanced/02-Generic-Types/) | Generic type handling |
| 3 | [Collection Types](Part3-Advanced/03-Collection-Types/) | Collection type handling |
| 4 | [LoggerMessage.Define Limitations](Part3-Advanced/04-LoggerMessage-Limits/) | Logger message constraints |
| 5 | [Unit Test Setup](Part3-Advanced/05-Unit-Testing-Setup/) | Test environment setup |
| 6 | [Verify Snapshot Testing](Part3-Advanced/06-Verify-Snapshot-Testing/) | Snapshot testing |
| 7 | [Test Scenarios](Part3-Advanced/07-Test-Scenarios/) | Writing test cases |

### Part 4: Cookbook

Learn Source Generator development procedures through various practical examples.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Source Generator Development Workflow](Part4-Cookbook/01-Development-Workflow/) | Development workflow overview |
| 2 | [Entity ID Generator](Part4-Cookbook/02-Entity-Id-Generator/) | DDD strongly-typed ID (Ulid-based) |
| 3 | [EF Core Value Converter](Part4-Cookbook/03-EfCore-Value-Converter/) | Auto-generating ValueConverters |
| 4 | [Validation Generator](Part4-Cookbook/04-Validation-Generator/) | FluentValidation rule generation |
| 5 | [Custom Generator Template](Part4-Cookbook/05-Custom-Generator-Template/) | Guide to starting a new project |

### Part 5: Conclusion

Summarize the content and provide guidance for next steps.

- [5.1 Summary](Part5-Conclusion/01-summary.md)
- [5.2 Next Steps](Part5-Conclusion/02-next-steps.md)

### [Appendix](Appendix/)

- [A. Development Environment Setup](Appendix/A-development-environment.md)
- [B. API Reference](Appendix/B-api-reference.md)
- [C. Test Scenario Catalog](Appendix/C-test-scenario-catalog.md)
- [D. Troubleshooting](Appendix/D-troubleshooting.md)

---

## Core Evolution Process

[Part 1] Fundamentals
Ch 1: Development Environment  ->  Ch 2: Project Structure  ->  Ch 3: Debugging Setup  ->  Ch 4: Roslyn Architecture  ->  Ch 5: Syntax API  ->  Ch 6: Semantic API  ->  Ch 7: Symbol Types

[Part 2] Core Concepts
Ch 1: IIncrementalGenerator Interface  ->  Ch 2: Provider Pattern  ->  Ch 3: ForAttributeWithMetadataName  ->  Ch 4: Incremental Caching  ->  Ch 5: INamedTypeSymbol  ->  Ch 6: IMethodSymbol  ->  Ch 7: SymbolDisplayFormat  ->  Ch 8: Type Extraction  ->  Ch 9: StringBuilder Pattern  ->  Ch 10: Template Design  ->  Ch 11: Namespace Handling  ->  Ch 12: Deterministic Output

[Part 3] Advanced
Ch 1: Constructor Handling  ->  Ch 2: Generic Types  ->  Ch 3: Collection Types  ->  Ch 4: LoggerMessage.Define Limitations  ->  Ch 5: Unit Test Setup  ->  Ch 6: Verify Snapshot Testing  ->  Ch 7: Test Scenarios

[Part 4] Cookbook
Ch 1: Source Generator Development Workflow  ->  Ch 2: Entity ID Generator  ->  Ch 3: EF Core Value Converter  ->  Ch 4: Validation Generator  ->  Ch 5: Custom Generator Template

---

## Hands-On Project: ObservablePortGenerator

In this tutorial, you will implement an actual Source Generator called **ObservablePortGenerator** step by step. For details on the project's design goals, structure, and expected benefits, see [Part 0-05. Project Overview](Part0-Introduction/05-project-overview.md).

```csharp
// Code the developer writes -- focus only on business logic
[GenerateObservablePort]
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
    public FinT<IO, User> GetUserAsync(int id) => /* pure logic */;
}

// Auto-generated by the Source Generator -- observability code included
public class UserRepositoryObservable : UserRepository
{
    // Logging, tracing, and metrics automatically applied to all methods
}
```

---

## Prerequisites

- .NET 10.0 SDK (Preview or release version)
- Visual Studio 2022 (17.12 or later) or VS Code (C# Dev Kit extension)
- Basic knowledge of C# 14 syntax

---

## Project Structure

```
sourcegen-observability/
├── Part0-Introduction/         # Part 0: Introduction
│   ├── 01-what-is-source-generator.md
│   ├── 02-hello-world-generator/
│   ├── 03-why-source-generator.md
│   ├── 04-reflection-vs-sourcegen/
│   └── 05-project-overview.md
├── Part1-Fundamentals/         # Part 1: Fundamentals
│   ├── 01-development-environment.md
│   ├── 02-Data-Models/
│   ├── 03-Debugging-Setup/
│   ├── ...
│   └── 07-Symbol-Types/
├── Part2-Core-Concepts/        # Part 2: Core Concepts
│   ├── 01-IIncrementalGenerator/
│   ├── ...
│   └── 12-Deterministic-Output/
├── Part3-Advanced/             # Part 3: Advanced
│   ├── 01-Constructor-Handling/
│   ├── ...
│   └── 07-Test-Scenarios/
├── Part4-Cookbook/              # Part 4: Cookbook
│   ├── 01-Development-Workflow/
│   ├── ...
│   └── 05-Custom-Generator-Template/
├── Part5-Conclusion/           # Part 5: Conclusion
│   ├── 01-summary.md
│   └── 02-next-steps.md
└── Appendix/                   # Appendix
    ├── A-development-environment.md
    ├── B-api-reference.md
    ├── C-test-scenario-catalog.md
    └── D-troubleshooting.md
```

---

## Testing

All example projects in every Part include unit tests. Tests follow the [Unit Testing Guide](../../guides/testing/15a-unit-testing.md).

### Running Tests

```bash
# Build the entire tutorial
dotnet build sourcegen-observability.slnx

# Test the entire tutorial
dotnet test --solution sourcegen-observability.slnx
```

### Test Project Structure

**Part 4: Cookbook** (5)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `DevelopmentWorkflow.Tests.Unit` | Source Generator development workflow verification |
| 2 | `EntityIdGenerator.Tests.Unit` | DDD strongly-typed ID (Ulid-based) generation |
| 3 | `EfCoreValueConverter.Tests.Unit` | ValueConverter auto-generation verification |
| 4 | `ValidationGenerator.Tests.Unit` | FluentValidation rule generation verification |
| 5 | `CustomGeneratorTemplate.Tests.Unit` | Custom Generator template verification |

### Test Naming Convention

Follows the T1_T2_T3 naming convention:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Generate_ProducesExpectedOutput_WhenClassHasObservablePortAttribute()
{
    // Arrange
    var source = /* input source code */;
    // Act
    var actual = GeneratorTestHelper.RunGenerator(source);
    // Assert
    actual.ShouldMatchExpected();
}
```

---

## Source Code

All example code for this tutorial can be found in the Functorium project:

- Source Generators: `Src/Functorium.SourceGenerators/`
- Adapters (attributes, naming): `Src/Functorium.Adapters/`
- Tests: `Tests/Functorium.Tests.Unit/AdaptersTests/SourceGenerators/`
- Test utilities: `Src/Functorium.Testing/Actions/SourceGenerators/`

---

This tutorial was written based on real-world experience developing Source Generators in the Functorium project.
