---
title: "Functorium Guide"
---

Have you ever experienced business logic and technical code getting tangled together, making changes difficult and testing even harder? Functorium combines DDD tactical design with functional programming to clearly separate concerns, providing comprehensive guides covering architecture, implementation patterns, and observability for your projects.

## Introduction

You have likely encountered these challenges in development:

- **Business rules scattered among infrastructure code** — have you struggled to identify where to make changes each time?
- **Unit testing domain logic** — have you felt burdened by having to set up databases or external services?
- **As projects grow, layer dependencies become tangled** — have you experienced the vicious cycle where fixing one place breaks another?

Functorium solves these problems by **separating concerns with DDD tactical design patterns and controlling side effects with functional programming.**

### What This Guide Covers

1. **Internal Architecture Design Principles** — rationale for separation of concerns, layer structure, and dependency direction
2. **DDD Tactical Design Building Blocks** — Value Object, Entity, Aggregate, Domain Event, Specification
3. **Application/Adapter Layer Implementation** — Use Case, Port, Adapter, Pipeline, DI
4. **Testing Strategy** — unit testing, integration testing, test libraries
5. **Observability** — logging, metrics, and tracing specifications and implementation

> **The Functorium Guide** is organized in WHY → WHAT → HOW order, so you can understand why each concept is needed before moving to implementation.

## DDD Tactical Design and Functorium

Functorium is a framework that combines DDD tactical design patterns with functional programming. The guide documents are arranged in DDD building block order, and each document follows the **WHY → WHAT → HOW** structure.

## Learning Roadmap

```
[00] 00-writing-guide.md ─── Documentation Writing Guide

Architecture
├── [00] 00-architecture-design-principles.md ─── Internal Architecture Design Principles
├── [01] 01-project-structure.md ─── Project Structure
├── [02] 02-solution-configuration.md ─── Solution Configuration
├── [02b] 02b-ci-cd-and-versioning.md ─── CI/CD Workflow and Versioning
└── [03] 03-dotnet-tools.md ─── .NET Tools

[04] 04-ddd-tactical-overview.md ─── DDD Tactical Design Overview
│
├── Domain Layer
│   ├── [05a] 05a-value-objects.md ─── Value Objects (Core Concepts, Validation, Implementation Patterns)
│   ├── [05b] 05b-value-objects-validation.md ─── Value Objects (Enumerations, Practical, FAQ)
│   ├── [05c] 05c-union-value-objects.md ─── Value Objects: Union Types
│   │   └── [06a] 06a-aggregate-design.md ─── Aggregate Design (WHY + WHAT)
│   │       ├── [06b] 06b-entity-aggregate-core.md ─── Entity/Aggregate Core Patterns (HOW)
│   │       └── [06c] 06c-entity-aggregate-advanced.md ─── Entity/Aggregate Advanced Patterns
│   │           └── [07] 07-domain-events.md ─── Domain Events
│   ├── [08a] 08a-error-system.md ─── Error System: Fundamentals and Naming
│   ├── [08b] 08b-error-system-domain-app.md ─── Error System: Domain/Application Errors
│   ├── [08c] 08c-error-system-adapter-testing.md ─── Error System: Adapter Errors and Testing
│   ├── [09] 09-domain-services.md ─── Domain Services
│   └── [10] 10-specifications.md ─── Specification Pattern
│
├── Application Layer
│   └── [11] 11-usecases-and-cqrs.md ─── Use Cases and CQRS
│
├── Adapter Layer
│   ├── [12] 12-ports.md ─── Port Definitions
│   ├── [13] 13-adapters.md ─── Adapter Implementation
│   ├── [14a] 14a-adapter-pipeline-di.md ─── Pipeline, DI
│   ├── [14b] 14b-adapter-testing.md ─── Unit Testing
│   └── [14c] 14c-repository-query-implementation-guide.md ─── Repository & Query Implementation Guide
│
├── Testing
│   ├── [15] 15a-unit-testing.md ─── Unit Testing
│   ├── [15b] 15b-integration-testing.md ─── Integration Testing
│   └── [16] 16-testing-library.md ─── Test Library
│
├── DTO Strategy
│   └── [17] 17-dto-strategy.md ─── DTO Strategy
│
├── Observability
│   ├── [18a] → spec/08-observability.md ─── Specification (moved to spec)
│   ├── [18b] 18b-observability-naming.md ─── Naming
│   ├── [19] 19-observability-logging.md ─── Logging
│   ├── [20] 20-observability-metrics.md ─── Metrics
│   └── [21] 21-observability-tracing.md ─── Tracing
│
├── Diagnostics
│   └── [22] 22-crash-diagnostics.md ─── Crash Dumps
│
└── Appendix
    ├── [A01] A01-vscode-debugging.md ─── VSCode Debugging
    ├── [A02] A02-git-reference.md ─── Git Reference
    ├── [A03] A03-response-type-evolution.md ─── FinResponse Type Evolution
    └── [A04] A04-architecture-rules-coverage.md ─── Architecture Rules Validation Coverage
```

## Quick Reference (Task-Based Guide Links)

### Getting Started with a Project

Guidance on how to set up project structure, build configuration, and CI/CD when starting a new service.

| Task | Reference Document |
|------|-------------------|
| **Understand architecture design principles** | [00-architecture-design-principles.md](./architecture/00-architecture-design-principles) |
| **Project structure/folder layout** | [01-project-structure.md](./architecture/01-project-structure) |
| **Solution config files/build scripts** | [02-solution-configuration.md](./architecture/02-solution-configuration) |
| **CI/CD workflow and versioning** | [02b-ci-cd-and-versioning.md](./architecture/02b-ci-cd-and-versioning) |
| **Tool usage (coverage/snapshot/ER diagrams)** | [03-dotnet-tools.md](./architecture/03-dotnet-tools) |

### Building the Domain Model

Translating business concepts into code — start with Value Objects, group them into Aggregates, and express interactions through events and errors.

| Task | Reference Document |
|------|-------------------|
| **DDD building blocks overview/naming/glossary** | [04-ddd-tactical-overview.md](./domain/04-ddd-tactical-overview) |
| **Creating Value Objects (validation, equality)** | [05a-value-objects.md](./domain/05a-value-objects) |
| **Smart Enum patterns** | [05b-value-objects-validation.md](./domain/05b-value-objects-validation) |
| **Union Value Objects (Discriminated Union)** | [05c-union-value-objects.md](./domain/05c-union-value-objects) |
| **Designing Aggregate boundaries** | [06a-aggregate-design.md](./domain/06a-aggregate-design) |
| **Entity/Aggregate implementation (creation patterns)** | [06b-entity-aggregate-core.md](./domain/06b-entity-aggregate-core) |
| **Cross-Aggregate relationships, supplementary interfaces** | [06c-entity-aggregate-advanced.md](./domain/06c-entity-aggregate-advanced) |
| **Domain Event definition/publishing/handlers** | [07-domain-events.md](./domain/07-domain-events) |
| **Error type definitions and testing** | [08a](./domain/08a-error-system) → [08b](./domain/08b-error-system-domain-app) → [08c](./domain/08c-error-system-adapter-testing) |
| **Creating Domain Services** | [09-domain-services.md](./domain/09-domain-services) |
| **Creating Specifications** | [10-specifications.md](./domain/10-specifications) |

### Application Layer

Define use cases that expose the domain model to the outside, and separate Command/Query responsibilities.

| Task | Reference Document |
|------|-------------------|
| **Creating Use Cases (CQRS)** | [11-usecases-and-cqrs.md](./application/11-usecases-and-cqrs) |
| **DTO strategy/reuse rules** | [17-dto-strategy.md](./application/17-dto-strategy) |

### Adapter Layer

The boundary where the domain meets the external world — define contracts with Ports and implement them with Adapters.

| Task | Reference Document |
|------|-------------------|
| **Defining Port interfaces** | [12-ports.md](./adapter/12-ports) |
| **Adapter implementation (Repository, API, Messaging)** | [13-adapters.md](./adapter/13-adapters) |
| **Pipeline/DI registration, Options pattern, caching** | [14a-adapter-pipeline-di.md](./adapter/14a-adapter-pipeline-di) |
| **Adapter unit testing** | [14b-adapter-testing.md](./adapter/14b-adapter-testing) |
| **Repository & Query implementation (pagination, Dapper)** | [14c-repository-query-implementation-guide.md](./adapter/14c-repository-query-implementation-guide) |

### Testing

Domain purity makes testing simple. Strategy from unit testing to integration testing.

| Task | Reference Document |
|------|-------------------|
| **Unit testing (naming, AAA, MTP)** | [15a-unit-testing.md](./testing/15a-unit-testing) |
| **Integration testing (HostTestFixture)** | [15b-integration-testing.md](./testing/15b-integration-testing) |
| **Test library (logging/architecture/source generator/Job)** | [16-testing-library.md](./testing/16-testing-library) |
| **Architecture rules coverage matrix** | [A04-architecture-rules-coverage.md](./appendix/A04-architecture-rules-coverage) |

### Observability and Operations

Unify the language of development and operations. Structured logging, metrics, and tracing specifications with crash diagnostics.

| Task | Reference Document |
|------|-------------------|
| **Observability specification** | [08-observability.md](../spec/08-observability) |
| **Crash dump setup/analysis** | [22-crash-diagnostics.md](./observability/22-crash-diagnostics) |

## Code Example Rules

| Category | Format | Description |
|----------|--------|-------------|
| Rule implementation code | Actual C# code | Compilable-level code (types, methods, pattern examples) |
| Architecture flow explanation | pseudo-code allowed | Must label with `pseudo-code` or `conceptual code` before the code block |
| Code block language tag | Always specified | ` ```csharp `, ` ```xml `, ` ```bash `, ` ```promql `, etc. |

## Complete Document List

### Architecture

Understand the rationale for separation of concerns, layer structure, and dependency direction first, then concretize with project folder structure and build configuration. This is the first area to reference when creating a new service.

| Document | Description |
|----------|-------------|
| [00-architecture-design-principles.md](./architecture/00-architecture-design-principles) | Internal architecture design principles (separation of concerns, layer structure, dependency direction) |
| [01-project-structure.md](./architecture/01-project-structure) | Service project structure (folders, naming, dependencies) |
| [02-solution-configuration.md](./architecture/02-solution-configuration) | Solution root configuration files and build scripts |
| [02b-ci-cd-and-versioning.md](./architecture/02b-ci-cd-and-versioning) | CI/CD workflow and versioning (GitHub Actions, NuGet packages, MinVer, version suggestion commands) |
| [03-dotnet-tools.md](./architecture/03-dotnet-tools) | .NET tools guide (CLI tools, source generators, file-based scripts) |

### DDD Tactical Design (Sequential Learning Path)

Build a pure domain model so that business rules do not depend on infrastructure. Guarantee immutable validation with Value Objects, set transaction boundaries with Aggregates, and achieve loose coupling with Domain Events.

| # | Document | Description |
|---|----------|-------------|
| 04 | [04-ddd-tactical-overview.md](./domain/04-ddd-tactical-overview) | DDD tactical design overview, building block map, Functorium type mapping |
| 05a | [05a-value-objects.md](./domain/05a-value-objects) | Value Objects (core concepts, base classes, validation system, implementation patterns) |
| 05b | [05b-value-objects-validation.md](./domain/05b-value-objects-validation) | Value Objects (enumeration patterns, practical examples, Application validation, FAQ) |
| 05c | [05c-union-value-objects.md](./domain/05c-union-value-objects) | Value Objects: Union types (Discriminated Union, state transitions, Match/Switch) |
| 06a | [06a-aggregate-design.md](./domain/06a-aggregate-design) | Aggregate design (WHY + WHAT: design rules, boundary setting, anti-patterns) |
| 06b | [06b-entity-aggregate-core.md](./domain/06b-entity-aggregate-core) | Entity/Aggregate core patterns (HOW: class hierarchy, ID, creation patterns, events) |
| 06c | [06c-entity-aggregate-advanced.md](./domain/06c-entity-aggregate-advanced) | Entity/Aggregate advanced patterns (Cross-Aggregate relationships, supplementary interfaces, practical examples) |
| 07 | [07-domain-events.md](./domain/07-domain-events) | Domain Event definition, publishing, handler implementation |
| 08a | [08a-error-system.md](./domain/08a-error-system) | Error system: fundamentals and naming (WHY, Fin pattern, naming rules) |
| 08b | [08b-error-system-domain-app.md](./domain/08b-error-system-domain-app) | Error system: Domain/Application errors (Domain/Application/Event error definition and testing) |
| 08c | [08c-error-system-adapter-testing.md](./domain/08c-error-system-adapter-testing) | Error system: Adapter errors and testing (Adapter errors, Custom errors, testing best practices, checklist) |
| 09 | [09-domain-services.md](./domain/09-domain-services) | Domain Services (cross-Aggregate pure logic, IDomainService) |
| 10 | [10-specifications.md](./domain/10-specifications) | Specification pattern (business rule encapsulation, composition, Repository integration) |

### Application Layer

Define use cases that expose the domain model at the application boundary. Separate write and read responsibilities with CQRS, and clarify data transfer rules between layers with DTOs.

| # | Document | Description |
|---|----------|-------------|
| 11 | [11-usecases-and-cqrs.md](./application/11-usecases-and-cqrs) | Use Case implementation (CQRS Command/Query) |
| 17 | [17-dto-strategy.md](./application/17-dto-strategy) | DTO strategy (layer ownership, reuse rules, conversion patterns) |

### Adapter Layer

The boundary where the domain meets the external world. Define contracts with Ports, implement with Adapters, and assemble with Pipeline and DI. Concrete infrastructure implementations such as Repository, external API, and messaging are located in this area.

| # | Document | Description |
|---|----------|-------------|
| 12 | [12-ports.md](./adapter/12-ports) | Port architecture, IObservablePort hierarchy, Port definition rules |
| 13 | [13-adapters.md](./adapter/13-adapters) | Adapter implementation (Repository, External API, Messaging, Query) |
| 14a | [14a-adapter-pipeline-di.md](./adapter/14a-adapter-pipeline-di) | Pipeline creation, DI registration, Options pattern |
| 14b | [14b-adapter-testing.md](./adapter/14b-adapter-testing) | Adapter unit testing, E2E Walkthrough |
| 14c | [14c-repository-query-implementation-guide.md](./adapter/14c-repository-query-implementation-guide) | Repository & Query implementation guide |

### Testing

The purity of the Domain Layer makes unit testing simple, and Adapter separation makes integration test boundaries clear. The Functorium.Testing library provides tools for architecture rule validation, structured log testing, and source generator testing.

| Document | Description |
|----------|-------------|
| [15a-unit-testing.md](./testing/15a-unit-testing) | Unit testing rules (naming, AAA pattern, MTP setup) |
| [15b-integration-testing.md](./testing/15b-integration-testing) | Integration testing (HostTestFixture, environment setup) |
| [16-testing-library.md](./testing/16-testing-library) | Functorium.Testing library (log/architecture/source generator/Job testing) |

### Observability

Define observability specifications so that development and operations can communicate in the same language. Build a consistent observability system from Field/Tag naming to structured logging, metrics, and distributed tracing.

| Document | Description |
|----------|-------------|
| [08-observability.md](../spec/08-observability) | Observability specification (Field/Tag, Meter, message template) |
| [18b-observability-naming.md](./observability/18b-observability-naming) | Observability naming guide (code, Logger methods) |
| [19-observability-logging.md](./observability/19-observability-logging) | Observability logging details |
| [20-observability-metrics.md](./observability/20-observability-metrics) | Observability metrics details |
| [21-observability-tracing.md](./observability/21-observability-tracing) | Observability tracing details |

### Diagnostics

Guide for collecting and analyzing crash dumps for post-mortem analysis of abnormal terminations in production environments.

| Document | Description |
|----------|-------------|
| [22-crash-diagnostics.md](./observability/22-crash-diagnostics) | Crash dump handler setup and analysis guide |

### Appendix

| Document | Description |
|----------|-------------|
| [A01-vscode-debugging.md](./appendix/A01-vscode-debugging) | VSCode debugging and development environment setup |
| [A02-git-reference.md](./appendix/A02-git-reference) | Git command reference and Git Hooks |
| [A03-response-type-evolution.md](./appendix/A03-response-type-evolution) | FinResponse type evolution history |
| [A04-architecture-rules-coverage.md](./appendix/A04-architecture-rules-coverage) | Architecture rules validation coverage matrix |

### Review

| Document | Description |
|----------|-------------|
| [dto-strategy-review.md](../../.claude/dto-strategy-review.md) | DTO mapping strategy review (DDD & Hexagonal Architecture perspective) |

### Others

| Document | Description |
|----------|-------------|
| [book-writing-guide.md](../../.claude/book-writing-guide.md) | Book writing guide |
| [00-writing-guide.md](./architecture/00-writing-guide) | Documentation writing guide (guide document authoring rules) |
