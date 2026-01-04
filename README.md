# Functorium
> A functional domain = **functor** + domin**ium** with **fun**.

Functorium is a C# framework for implementing **Domain-Centric Functional Architecture**.

It enables expressing domain logic as pure functions and pushing side effects to architectural boundaries, allowing you to write **testable and predictable business logic**. The framework provides a functional type system based on LanguageExt 5.x and integrated observability through OpenTelemetry.

### Core Principles

| Principle | Description | Functorium Support |
|-----------|-------------|-------------------|
| **Domain First** | Domain model is the center of architecture | Value Object hierarchy, immutable domain types |
| **Pure Core** | Business logic expressed as pure functions | `Fin<T>` return type, exception-free error handling |
| **Impure Shell** | Side effects handled at boundary layers | Adapter Pipeline, ActivityContext propagation |
| **Explicit Effects** | All effects explicitly typed | `FinResponse<T>`, `FinT<IO, T>` monad |

## Book
- [Architecture](./Docs/ArchitectureIs/README.md)
- [Automating Release Notes with Claude Code and .NET 10](./Books/Automating-ReleaseNotes-with-ClaudeCode-and-.NET10/README.md)
- [Automating Observability Code with SourceGenerator](./Books/Automating-ObservabilityCode-with-SourceGenerator/README.md)
- [Implementing Functional ValueObject](./Books/Implementing-Functional-ValueObject/README.md)

## Observability

### Metrics (UsecaseMetricsPipeline)

**Metric List:**

| Metric Name | Type | Unit | Description |
|------------|------|------|------|
| `application.usecase.{cqrs}.requests` | Counter | `{request}` | Total request count |
| `application.usecase.{cqrs}.responses` | Counter | `{response}` | Response count (distinguished by status tag) |
| `application.usecase.{cqrs}.duration` | Histogram | `s` | Processing time (seconds) |

**Tag Structure:**

| Tag Key | requestCounter | responseCounter (success) | responseCounter (failure) |
|---------|----------------|----------------------|----------------------|
| `request.layer` | `"application"` | `"application"` | `"application"` |
| `request.category` | `"usecase"` | `"usecase"` | `"usecase"` |
| `request.handler.cqrs` | `"command"` / `"query"` | `"command"` / `"query"` | `"command"` / `"query"` |
| `request.handler` | handler name | handler name | handler name |
| `request.handler.method` | `"Handle"` | `"Handle"` | `"Handle"` |
| `response.status` | - | `"success"` | `"failure"` |
| `error.type` | - | - | `"expected"` / `"exceptional"` / `"aggregate"` |
| `error.code` | - | - | Primary error code |
| **Total Tags** | **5** | **6** | **8** |

**Error Type Tag Values:**

| Error Case | error.type | error.code |
|---------|----------------|----------------------|
| ErrorCodeExpected | "expected" | Error code (e.g., DomainErrors.City.Empty) |
| ErrorCodeExceptional | "exceptional" | Error code (e.g., InfraErrors.Database.Timeout) |
| ManyErrors | "aggregate" | Primary error code (Exceptional takes priority) |
| Other Expected | "expected" | Type name |
| Other Exceptional | "exceptional" | Type name |

## Framework
### Abstractions
- [x] Structured Error
- [ ] Dependency Registration

### Adapter Layer
- [x] Configuration Option
- [ ] Observability(Logging, Tracing, Metrics)
- [ ] Scheduling
- [ ] HTTP Method
- [ ] MQ
- [ ] ORM

### Application Layer
- [x] CQRS
- [x] Pipeline
- [ ] IAdapter(Observability)
- [ ] Usecase(LINQ: FinT, Fin, IO, Guard, Validation)

### Domain Layer
- [x] Value Object
- [ ] Entity

## Infrastructure
### AI
- [x] Commit
- [x] Release Note
- [ ] Sprint Plan

### Testing
- [x] MTP(Microsoft Testing Platform)
- [x] Code Coverage
- [x] Architecture Testing
- [x] Snapshot Testing
- [ ] Container Testing
- [ ] Performance Testing

### CI/CD
- [ ] Versioning
- [x] Local Build
- [x] Remote Build
- [ ] Remote Deployment

### System
- [ ] Observability
