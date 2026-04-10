---
title: "API Specification Reference"
---

This is the **API specification document** defining the public types, interfaces, and attributes of the Functorium framework. For design intent and hands-on guidance, see the [Framework Guide](../guides/). Use this document to verify "what exactly is provided."

## Project Structure

| NuGet Package | Namespace Root | Role |
|-------------|-----------------|------|
| **Functorium** | `Functorium.Domains.*`, `Functorium.Applications.*`, `Functorium.Abstractions.*` | Domain/Application core types |
| **Functorium.Adapters** | `Functorium.Adapters.*` | Infrastructure adapters, Pipeline, DI registration |
| **Functorium.SourceGenerators** | `Functorium.SourceGenerators.*` | Compile-time code generators |
| **Functorium.Testing** | `Functorium.Testing.*` | Test utilities, architecture rules |

## Specification List

### Domain Core

| Document | Description |
|------|------|
| [Entity and Aggregate](./01-entity-aggregate) | `Entity<TId>`, `AggregateRoot<TId>`, EntityId, mixin interfaces |
| [Value Object](./02-value-object) | `ValueObject`, `SimpleValueObject<T>`, Union types, equality/comparison |
| [Error System](./04-error-system) | `DomainErrorType`, `ApplicationErrorType`, `AdapterErrorType`, factory API |

### Application Layer

| Document | Description |
|------|------|
| [Validation System](./03-validation) | `TypedValidation`, `ContextualValidation`, FluentValidation integration |
| [Usecase CQRS](./05-usecase-cqrs) | `FinResponse<T>`, CQRS interfaces, LINQ extensions, caching/persistence contracts |
| [Domain Events](./09-domain-events) | `DomainEvent`, Publisher/Collector, Handler, Ctx Enricher |

### Adapter/Infrastructure

| Document | Description |
|------|------|
| [Port and Adapter](./06-port-adapter) | `IRepository`, `IQueryPort`, Specification, DI registration, implementation base classes |
| [Pipeline](./07-pipeline) | 8 Pipeline behaviors, custom extensions, `PipelineConfigurator`, OpenTelemetry settings |

### Cross-Cutting Concerns

| Document | Description |
|------|------|
| [Observability](./08-observability) | Field/Tag specification, Meter definitions, message templates, Pipeline ordering |
| [Source Generators](./10-source-generators) | 5 source generator specifications (EntityId, ObservablePort, CtxEnricher, UnionType) |
| [Testing Library](./11-testing) | `FinTFactory`, Host Fixture, architecture rules, error assertions |
