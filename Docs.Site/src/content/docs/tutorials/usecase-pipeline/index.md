---
title: "Use-Case Pipeline Constraints"
---

**A practical guide from C# generic variance to solving Mediator Pipeline constraints**

---

## About This Tutorial

`Fin<T>` is a sealed struct and cannot be used as a constraint. To achieve type-safe response handling in a Mediator Pipeline without reflection, you need an interface hierarchy design that works around this single-line constraint.

This tutorial is a practical guide designed for step-by-step learning, starting from the **fundamentals of C# generic variance (covariance/contravariance)** and progressing through **designing the IFinResponse interface hierarchy and applying Pipeline constraints**. Through **20 hands-on projects**, you will systematically learn the complete path from variance fundamentals to Fin\<T\> limitations to IFinResponse hierarchy to Pipeline constraints to real-world use cases.

> **`Fin<T>` is a sealed struct and cannot be used as a constraint -- experience the entire process of IFinResponse interface hierarchy design that began from this single-line constraint.**

### Target Audience

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers who know basic C# syntax and want to learn generic variance | Part 1 |
| **Intermediate** | Developers interested in Mediator Pipeline and type constraint design | Parts 1--3 |
| **Advanced** | Developers who want to apply Pipeline architecture and functional patterns in production | Parts 4--5 + Appendix |

### Learning Objectives

After completing this tutorial, you will be able to:

1. Understand the principles and application conditions of **C# generic variance** (covariance, contravariance, invariance)
2. Master the **sealed struct constraint limitations** and design patterns to work around them with interface hierarchies
3. Design the **IFinResponse interface hierarchy** yourself and implement the CRTP factory pattern
4. Design **minimal constraint conditions per Pipeline** for type-safe Pipelines without reflection
5. Integrate the full Pipeline flow with **Command/Query Use Cases** based on FinResponse

---

### Part 0: Introduction

Introduce why type-safe pipelines are needed and provide an overview of the architecture.

- [0.1 Why Type-Safe Pipelines](Part0-Introduction/01-why-this-tutorial.md)
- [0.2 Environment Setup](Part0-Introduction/02-prerequisites-and-setup.md)
- [0.3 Use-Case Pipeline Architecture Overview](Part0-Introduction/03-usecase-pipeline-overview.md)

### Part 1: Generic Variance Foundations

Learn the core concepts of C# generic variance through code.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Covariance (out)](Part1-Generic-Variance-Foundations/01-Covariance/) | IEnumerable\<out T\>, output position, Dog->Animal assignment |
| 2 | [Contravariance (in)](Part1-Generic-Variance-Foundations/02-Contravariance/) | Action\<in T\>, IHandler\<in T\>, handler substitution |
| 3 | [Invariance and Constraints](Part1-Generic-Variance-Foundations/03-Invariance-And-Constraints/) | List\<T\> invariance, sealed struct constraint impossibility, where constraints |
| 4 | [Interface Segregation and Variance Composition](Part1-Generic-Variance-Foundations/04-Interface-Segregation-And-Variance/) | Read(out)/Write(in)/Factory separation, ISP+variance |

### Part 2: Problem Definition -- Fin and Mediator Collision

Analyze the constraint collision between `Fin<T>` sealed struct and Mediator Pipeline.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Mediator Pipeline Behavior Structure](Part2-Problem-Definition/01-Mediator-Pipeline-Structure/) | IPipelineBehavior, MessageHandlerDelegate, constraint roles |
| 2 | [Limitations of Using Fin\<T\> Directly](Part2-Problem-Definition/02-Fin-Direct-Limitation/) | sealed struct constraint impossibility, reflection needed in 3 places |
| 3 | [Limitations of IFinResponse Wrapper](Part2-Problem-Definition/03-IFinResponse-Wrapper-Limitation/) | Dual interface, reflection in 1 place, CreateFail impossible |
| 4 | [Requirements Summary](Part2-Problem-Definition/04-pipeline-requirements-summary.md) | 4 requirements, Pipeline capability matrix |

### Part 3: Solution -- IFinResponse Hierarchy Design

Design the IFinResponse interface hierarchy that enables type-safe Pipelines without reflection.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [IFinResponse Non-Generic Marker](Part3-IFinResponse-Hierarchy/01-IFinResponse-Marker/) | IsSucc/IsFail, Pipeline read-only access |
| 2 | [IFinResponse\<out A\> Covariant Interface](Part3-IFinResponse-Hierarchy/02-IFinResponse-Covariant/) | out applied, covariant Pipeline access |
| 3 | [IFinResponseFactory CRTP Factory](Part3-IFinResponse-Hierarchy/03-IFinResponseFactory-CRTP/) | static abstract, CRTP, CreateFail |
| 4 | [IFinResponseWithError Error Access](Part3-IFinResponse-Hierarchy/04-IFinResponseWithError/) | Error property, implemented only on Fail, pattern matching |
| 5 | [FinResponse\<A\> Discriminated Union](Part3-IFinResponse-Hierarchy/05-FinResponse-Discriminated-Union/) | Succ/Fail sealed records, Match/Map/Bind, implicit conversion |

### Part 4: Pipeline Constraint Pattern Application

Apply minimal constraint conditions to each Pipeline using the IFinResponse hierarchy.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Create-Only Constraint](Part4-Pipeline-Constraint-Patterns/01-Create-Only-Constraint/) | `where TResponse : IFinResponseFactory<TResponse>` |
| 2 | [Read+Create Constraint](Part4-Pipeline-Constraint-Patterns/02-Read-Create-Constraint/) | `where TResponse : IFinResponse, IFinResponseFactory<TResponse>` |
| 3 | [Transaction/Caching Pipeline](Part4-Pipeline-Constraint-Patterns/03-Transaction-Caching-Pipeline/) | Command/Query branching, ICacheable conditional |
| 4 | [Fin -> FinResponse Bridge](Part4-Pipeline-Constraint-Patterns/04-Fin-To-FinResponse-Bridge/) | ToFinResponse() extension method, cross-layer conversion |

### Part 5: Practical Use-Case Examples

Complete Command/Query Use-Case examples integrating the full Pipeline.

- [5.1 Command Use-Case Complete Example](Part5-Practical-Usecase-Examples/01-Command-Usecase-Example/)
- [5.2 Query Use-Case Complete Example](Part5-Practical-Usecase-Examples/02-Query-Usecase-Example/)
- [5.3 Full Pipeline Integration](Part5-Practical-Usecase-Examples/03-Full-Pipeline-Integration/)

### [Appendix](Appendix/)

- [A. IFinResponse Interface Hierarchy Complete Reference](Appendix/A-interface-hierarchy-reference.md)
- [B. Pipeline Constraints vs Alternatives Comparison](Appendix/B-constraint-vs-alternatives.md)
- [C. Railway Oriented Programming Reference](Appendix/C-railway-oriented-programming.md)
- [D. Glossary](Appendix/D-glossary.md)
- [E. References](Appendix/E-references.md)

---

## Core Evolution Process

[Part 1] Generic Variance Foundations
Ch 1: Covariance (out)  ->  Ch 2: Contravariance (in)  ->  Ch 3: Invariance and Constraints  ->  Ch 4: Interface Segregation and Variance Composition

[Part 2] Problem Definition -- Fin and Mediator Collision
Ch 1: Mediator Pipeline Behavior Structure  ->  Ch 2: Limitations of Using Fin\<T\> Directly  ->  Ch 3: Limitations of IFinResponse Wrapper  ->  Ch 4: Requirements Summary

[Part 3] IFinResponse Hierarchy Design
Ch 1: IFinResponse Non-Generic Marker  ->  Ch 2: IFinResponse\<out A\> Covariant Interface  ->  Ch 3: IFinResponseFactory CRTP Factory  ->  Ch 4: IFinResponseWithError Error Access  ->  Ch 5: FinResponse\<A\> Discriminated Union

[Part 4] Pipeline Constraint Pattern Application
Ch 1: Create-Only Constraint  ->  Ch 2: Read+Create Constraint  ->  Ch 3: Transaction/Caching Pipeline  ->  Ch 4: Fin -> FinResponse Bridge

[Part 5] Practical Use-Case Examples
Ch 1: Command Use-Case Complete Example  ->  Ch 2: Query Use-Case Complete Example  ->  Ch 3: Full Pipeline Integration

---

## IFinResponse Type Hierarchy

```
IFinResponse                              Non-generic marker (IsSucc/IsFail)
├── IFinResponse<out A>                   Covariant interface (read-only)
│
IFinResponseFactory<TSelf>                CRTP factory (CreateFail)
│
IFinResponseWithError                     Error access (Error property)
│
FinResponse<A>                            Discriminated Union
├── : IFinResponse<A>                     Covariant interface implementation
├── : IFinResponseFactory<FinResponse<A>> CRTP factory implementation
│
├── sealed record Succ(A Value)           Success case
│
└── sealed record Fail(Error Error)       Failure case
    └── : IFinResponseWithError           Error access only on Fail
```

### Constraint Conditions Per Pipeline

```
Pipeline                    TResponse Constraint                         Capability
──────────────────────────  ─────────────────────────────────────        ────────────
Metrics Pipeline            IFinResponse, IFinResponseFactory<...>       Read + Create
Tracing Pipeline            IFinResponse, IFinResponseFactory<...>       Read + Create
Logging Pipeline            IFinResponse, IFinResponseFactory<...>       Read + Create
Validation Pipeline         IFinResponseFactory<TResponse>               CreateFail
Caching Pipeline            IFinResponse, IFinResponseFactory<...>       Read + Create
Exception Pipeline          IFinResponseFactory<TResponse>               CreateFail
Transaction Pipeline        IFinResponse, IFinResponseFactory<...>       Read + Create
Custom Pipeline             (User-defined)                               Varies
```

---

## Prerequisites

- .NET 10.0 SDK or later
- VS Code + C# Dev Kit extension
- Basic knowledge of C# syntax
- Basic understanding of generics (type parameters, where constraints)

---

## Project Structure

```
usecase-pipeline/
├── Part0-Introduction/                        # Part 0: Introduction (3)
├── Part1-Generic-Variance-Foundations/         # Part 1: Generic Variance Foundations (4)
│   ├── 01-Covariance/
│   ├── 02-Contravariance/
│   ├── 03-Invariance-And-Constraints/
│   └── 04-Interface-Segregation-And-Variance/
├── Part2-Problem-Definition/                  # Part 2: Problem Definition (4)
│   ├── 01-Mediator-Pipeline-Structure/
│   ├── 02-Fin-Direct-Limitation/
│   ├── 03-IFinResponse-Wrapper-Limitation/
│   └── 04-pipeline-requirements-summary.md
├── Part3-IFinResponse-Hierarchy/              # Part 3: IFinResponse Hierarchy (5)
│   ├── 01-IFinResponse-Marker/
│   ├── 02-IFinResponse-Covariant/
│   ├── 03-IFinResponseFactory-CRTP/
│   ├── 04-IFinResponseWithError/
│   └── 05-FinResponse-Discriminated-Union/
├── Part4-Pipeline-Constraint-Patterns/        # Part 4: Pipeline Constraints (4)
│   ├── 01-Create-Only-Constraint/
│   ├── 02-Read-Create-Constraint/
│   ├── 03-Transaction-Caching-Pipeline/
│   └── 04-Fin-To-FinResponse-Bridge/
├── Part5-Practical-Usecase-Examples/          # Part 5: Practical Examples (3)
│   ├── 01-Command-Usecase-Example/
│   ├── 02-Query-Usecase-Example/
│   └── 03-Full-Pipeline-Integration/
├── Appendix/                                  # Appendix
└── README.md                                  # This document
```

---

## Testing

All example projects in every Part include unit tests. Tests follow the [Unit Testing Guide](../../guides/testing/15a-unit-testing.md).

### Running Tests

```bash
# Test the entire tutorial
dotnet test --solution usecase-pipeline.slnx
```

### Test Project Structure

**Part 1: Generic Variance Foundations** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `Covariance.Tests.Unit` | Covariance, IEnumerable\<out T\> assignment |
| 2 | `Contravariance.Tests.Unit` | Contravariance, Action\<in T\> handler substitution |
| 3 | `InvarianceAndConstraints.Tests.Unit` | Invariance, sealed struct constraint impossibility |
| 4 | `InterfaceSegregationAndVariance.Tests.Unit` | ISP + variance composition |

**Part 2: Problem Definition** (3)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `MediatorPipelineStructure.Tests.Unit` | IPipelineBehavior structure verification |
| 2 | `FinDirectLimitation.Tests.Unit` | Fin\<T\> direct usage limitation verification |
| 3 | `FinResponseWrapperLimitation.Tests.Unit` | IFinResponse wrapper limitation verification |

**Part 3: IFinResponse Hierarchy Design** (5)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `FinResponseMarker.Tests.Unit` | IFinResponse marker IsSucc/IsFail |
| 2 | `FinResponseCovariant.Tests.Unit` | IFinResponse\<out A\> covariant interface |
| 3 | `FinResponseFactoryCrtp.Tests.Unit` | CRTP factory CreateFail |
| 4 | `FinResponseWithError.Tests.Unit` | IFinResponseWithError error access |
| 5 | `FinResponseDiscriminatedUnion.Tests.Unit` | FinResponse\<A\> DU, Match/Map/Bind |

**Part 4: Pipeline Constraint Patterns** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `CreateOnlyConstraint.Tests.Unit` | IFinResponseFactory constraint verification |
| 2 | `ReadCreateConstraint.Tests.Unit` | IFinResponse + Factory constraint verification |
| 3 | `TransactionCachingPipeline.Tests.Unit` | Transaction/Caching Pipeline branching |
| 4 | `FinToFinResponseBridge.Tests.Unit` | Fin->FinResponse conversion bridge |

**Part 5: Practical Use-Case Examples** (3)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `CommandUsecaseExample.Tests.Unit` | Command Use Case full Pipeline |
| 2 | `QueryUsecaseExample.Tests.Unit` | Query Use Case full Pipeline |
| 3 | `FullPipelineIntegration.Tests.Unit` | Full Pipeline flow integration |

### Test Naming Convention

Follows the T1_T2_T3 naming convention:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Assign_Succeeds_WhenCovarianceApplies()
{
    // Arrange
    IEnumerable<Animal> animals;

    // Act
    animals = new List<Dog>();

    // Assert
    animals.ShouldNotBeNull();
}
```

---

## Source Code

All example code for this tutorial can be found in the Functorium project:

- IFinResponse interfaces: `Src/Functorium/Applications/Usecases/IFinResponse.cs`
- FinResponse implementation: `Src/Functorium/Applications/Usecases/IFinResponse.Impl.cs`
- Static factory: `Src/Functorium/Applications/Usecases/IFinResponse.Factory.cs`
- Fin->FinResponse conversion: `Src/Functorium/Applications/Usecases/IFinResponse.FinConversions.cs`
- Command/Query interfaces: `Src/Functorium/Applications/Usecases/ICommandRequest.cs`, `IQueryRequest.cs`
- Pipeline implementation: `Src/Functorium.Adapters/Observabilities/Pipelines/`

### Related Tutorials

This tutorial is more effective when studied together with:

- **[Separating Commands and Queries with the CQRS Pattern](../cqrs-repository/)**: From CQRS pattern fundamentals to use-case integration. Learn the CQRS architecture where this tutorial's Pipeline constraints are applied.

---

This tutorial was written based on the IFinResponse interface hierarchy design experience in the Functorium project.
