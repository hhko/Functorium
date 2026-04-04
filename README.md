# Functorium

[![Build](https://github.com/hhko/Functorium/actions/workflows/build.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/build.yml) [![Publish](https://github.com/hhko/Functorium/actions/workflows/publish.yml/badge.svg)](https://github.com/hhko/Functorium/actions/workflows/publish.yml)

**English** | **[한국어](./README.ko.md)**

> **Functorium** is named from **`functor + dominium`** with a touch of **`fun`**.
> **dominium** is Latin for "dominion, ownership" — Domain is not just a scope, but **the problem space we own and govern**.
>
> Functorium is an **AI Native .NET framework** where AI agents directly guide domain design and generate code.
> The result compiles into production code with functional architecture + DDD + Observability built in.

- Learning is excitement.
- Learning is humility.
- Learning is altruism.

**6 specialist AI agents** guide a 7-step workflow from requirements analysis to testing. At each step, design documents and compilable C# code are generated simultaneously. No need to manually make hundreds of architecture decisions.

Functorium is designed for .NET teams practicing enterprise DDD, teams seeking to bridge the language gap between development and operations, and architects systematically adopting functional DDD architecture.

## Problems to Solve

1. **Domain logic is mixed with exceptions and implicit side effects** — Business rule success and failure are handled via exceptions, making flow unpredictable and composition impossible.
2. **Development language and operations language are separated** — Feature specifications and operational requirements are managed in different systems, preventing a common language from being established and accumulating interpretation gaps.
3. **Observability is added as an afterthought** — Logs, metrics, and traces are attached separately after implementation is complete, causing critical context to be lost during incident analysis.

These are not simply process problems — they are **problems of design philosophy and structure**.

Mediator, LanguageExt, FluentValidation, and OpenTelemetry are each excellent. But integrating them into a coherent DDD architecture requires hundreds of decisions about error propagation, pipeline ordering, observability boundaries, and type constraints. Functorium makes these decisions once, consistently — and **AI agents automatically apply these decisions to your project.**

## How AI Breaks Through the Problems

### From Problem to Code — Structure Connected by AI

| Problem | AI Agent's Role | What the Framework Guarantees |
|---------|----------------|-------------------------------|
| Exceptions and implicit side effects | **domain-architect** classifies business invariants and maps them to types | `Fin<T>`, `FinT<IO,T>` result types, Always-valid Value Object |
| Development/operations language separation | **product-analyst** extracts Ubiquitous Language and consistently reflects it across code/docs/metrics | Bounded Context, `ctx.*` field auto-propagation |
| Observability as afterthought | **observability-engineer** designs KPI→metric mapping, dashboards, and alerts | 3-Pillar auto instrumentation, `[GenerateObservablePort]` |

### 7-Step Workflow

From PRD writing to testing, 8 skills + 6 specialist agents guide the way.

```
project-spec                    : PRD writing, Ubiquitous Language, Aggregate boundary extraction
  → architecture-design         : Project structure, layer composition, infrastructure decisions
  → domain-develop              : Value Object, Entity, Aggregate, Specification implementation
  → application-develop         : CQRS usecase, Port design and implementation
  → adapter-develop             : Repository, Query Adapter, Endpoint, DI registration
  → observability-develop       : KPI→metric mapping, dashboards, alerts, ctx.* propagation
  → test-develop                : Unit/integration/architecture rule test writing
  → domain-review               : DDD review of existing code and improvement guidance
```

Each step follows a **4-stage document pattern**. Every design decision has traceable rationale:

```
00-business-requirements        : Business rule definition
  →  01-type-design-decisions   : Invariant → type mapping
  →  02-code-design             : C# pattern design
  →  03-implementation-results  : Compilable code + tests
```

### 6 Specialist Agents

| Agent | Expertise |
|-------|-----------|
| **product-analyst** | PRD writing, Ubiquitous Language definition, Aggregate boundary extraction |
| **domain-architect** | Invariant classification, Functorium type mapping, Always-valid pattern design |
| **application-architect** | CQRS usecase decomposition, port identification, FinT LINQ composition design |
| **adapter-engineer** | Repository, Query Adapter, Endpoint, DI registration, Observable Port implementation |
| **observability-engineer** | KPI→metric mapping, dashboard design, alert patterns, ctx.* propagation strategy |
| **test-engineer** | Unit/integration/architecture rule/Observability verification tests |

### AI-Generated Artifacts

- Value Objects (Always-valid, structured error codes)
- AggregateRoot (with domain events)
- CQRS Command/Query usecases
- EF Core Repository + Dapper Query Adapter
- FastEndpoints API endpoints
- Observable Port (automatic 3-Pillar instrumentation)
- Unit tests, integration tests, architecture rule tests
- Design documents for all stages (traceable design rationale)

## AI-Generated Code: Functional Architecture

### Domain Model

The **domain-architect** agent of the `domain-develop` skill classifies business invariants and maps them to the Functorium type system. All core business logic resides within the domain model, and entities, value objects, aggregates, and domain services have clear responsibilities.

**Value Object** — Ensures value-based equality and immutability:

```csharp
public abstract class AbstractValueObject : IValueObject, IEquatable<AbstractValueObject>
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    // Value-based equality, cached hash code, ORM proxy handling
}
```

**Entity / AggregateRoot** — Provides Ulid-based IDs and domain event management:

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>
    where T : struct, IEntityId<T>
{
    Ulid Value { get; }
    static abstract T New();
    static abstract T Create(Ulid id);
    static abstract T Create(string id);
}

public abstract class AggregateRoot<TId> : Entity<TId>, IDomainEventDrain
    where TId : struct, IEntityId<TId>
{
    protected void AddDomainEvent(IDomainEvent domainEvent);
    public void ClearDomainEvents();
}
```

**DomainError** — Ensures recoverability through structured error codes:

```csharp
// Auto-generated error code: "DomainErrors.Email.Empty"
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");

// Auto-generated error code: "DomainErrors.Password.TooShort"
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
```

**Domain Event** — Integrates Mediator-based Pub/Sub with event tracking:

```csharp
public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
    Ulid EventId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}
```

### CQRS and Functional Composition

The `application-develop` skill assembles domain models into CQRS usecases. Core domain logic is composed of pure functions. By maintaining a structure where identical inputs always produce identical outputs, the logic becomes predictable and easy to test. Side effects (database, external APIs, messaging, file I/O) are handled outside the domain logic. The `IO` monad provides built-in advanced features such as Timeout, Retry (exponential backoff), Fork (parallel execution), and Bracket (resource lifecycle management), enabling type-safe fault tolerance configuration for external service calls.

**`Fin<T>`, `FinT<IO, T>`** — Handles errors with explicit result types instead of exceptions. The Command path Repository returns `FinT<IO, T>` to explicitly express side effects:

```csharp
// Command: IRepository — Per-Aggregate Root CRUD, change tracking and transaction management via EF Core
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    FinT<IO, TAggregate> Create(TAggregate aggregate);
    FinT<IO, TAggregate> GetById(TId id);
    FinT<IO, TAggregate> Update(TAggregate aggregate);
    FinT<IO, int> Delete(TId id);

    // Bulk operations
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
```

**CQRS** — Structurally separates write and read paths, applying optimized data access strategies for each. Command uses `IRepository` + EF Core for Aggregate consistency and transactions, while Query uses `IQueryPort` + Dapper for direct DTO projection without Aggregate reconstruction. Both paths unify results through `FinResponse<T>`:

```csharp
// Command
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>> { }
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess> { }

// Query
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>> { }
public interface IQueryUsecase<in TQuery, TSuccess>
    : IQueryHandler<TQuery, FinResponse<TSuccess>>
    where TQuery : IQueryRequest<TSuccess> { }
```

```csharp
// Query: IQueryPort — Direct DTO projection without Aggregate reconstruction, lightweight SQL mapping via Dapper
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec, PageRequest page, SortExpression sort);

    FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec, CursorPageRequest cursor, SortExpression sort);

    IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec, SortExpression sort,
        CancellationToken cancellationToken = default);
}
```

| | Command (IRepository) | Query (IQueryPort) |
|------|----------------------|-------------------|
| **Purpose** | Aggregate Root lifecycle management | Read-only DTO projection |
| **Implementation** | EF Core — change tracking, transactions, domain events | Dapper — pure SQL, lightweight mapping |
| **Specification** | `PropertyMap` → EF Core LINQ translation | `DapperSpecTranslator` → SQL WHERE translation |
| **Pagination** | — | Offset/Limit, Cursor (keyset), Streaming |

### Observability by Design

The `observability-develop` skill embeds operational stability from the design phase. All Commands and Queries automatically pass through a pipeline with built-in Observability (Logging, Metrics, Tracing) and validation. Developers do not need to write log code manually.

```mermaid
flowchart TD
    A["Common: Observability + Validation
    (Logging · Metrics · Tracing · Validation)"] --> B[Command Path]
    A --> C[Query Path]
    B --> D["Transaction + Domain Event Publishing"]
    C --> E["Caching + Pagination"]
```

Command publishes domain events within transaction boundaries, and Query provides caching and pagination. For the exact pipeline stages and order, see the [Observability Specification](./Docs.Site/src/content/docs/spec/08-observability.md).

**IObservablePort** — All external dependencies are abstracted as observable ports:

```csharp
public interface IObservablePort
{
    string RequestCategory { get; }
}
```

**ctx.* 3-Pillar Enrichment** — The Source Generator automatically transforms Request/Response/DomainEvent properties into `ctx.{snake_case}` fields, propagating business context simultaneously to Logging, Tracing, and Metrics. Metrics tags can be opted in with `[CtxTarget(CtxPillar.All)]`.

**`[GenerateObservablePort]`** — The Source Generator automatically creates Observable wrappers for Adapters, transparently providing OpenTelemetry-based Tracing, Logging, and Metrics:

```csharp
[GenerateObservablePort]  // → Observable{ClassName} auto-generated (e.g., ObservableOrderRepository)
public class OrderRepository : IRepository<Order, OrderId> { ... }
```

**Automatic Error Classification** — Business rule violations (e.g., "insufficient stock") are classified as `expected`, system failures (`NullReferenceException`) as `exceptional`, and compound validation failures as `aggregate`. The `error.type` field allows separate querying of business errors and system failures in Seq/Grafana.

## Quick Example

### Before/After — From Exceptions to Type Safety

**Before** — Traditional C# validation. Exceptions are landmines buried in control flow:

```csharp
public class Email
{
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty");   // Runtime bomb — if the next developer forgets try-catch, the system dies
        Value = value;
    }
    public string Value { get; }
}
```

**After** — Functorium's functional validation. Failure possibility is explicit in the return type — if you don't handle it, it won't compile:

```csharp
public sealed partial class Email : SimpleValueObject<string>
{
    public static Fin<Email> Create(string? value) =>               // Fin<T>: success or structured error
        CreateFromValidation(Validate(value), v => new Email(v));   // Composable pipeline without exceptions
}
```

### Full Implementation — Always-valid Value Object

Validates with type-safe error codes without exceptions, providing a composable functional validation pipeline:

```csharp
public sealed partial class Email : SimpleValueObject<string>
{
    public const int MaxLength = 320;

    private Email(string value) : base(value) { }

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new Email(v));

    // Each validation condition failure auto-generates a corresponding error code:
    //   NotNull    → "DomainErrors.Email.Null"
    //   NotEmpty   → "DomainErrors.Email.Empty"
    //   MaxLength  → "DomainErrors.Email.TooLong"
    //   Matches    → "DomainErrors.Email.InvalidFormat"
    // Composite Value Objects use the Apply pattern to validate multiple fields
    // in parallel, collecting all errors at once.
    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<Email>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenNormalize(v => v.Trim().ToLowerInvariant())
            .ThenMaxLength(MaxLength)
            .ThenMatches(EmailRegex(), "Invalid email format");

    // For ORM/Repository restoration — accepts only already-normalized data
    public static Email CreateFromValidated(string value) => new(value);

    public static implicit operator string(Email email) => email.Value;
}
```

For CQRS Command/Query usecase implementation examples, see the [CQRS Repository Tutorial](./Docs.Site/src/content/docs/tutorials/cqrs-repository/index.md).

## Key Features

| Value | Features |
|-------|----------|
| **Domain Safety** | Value Object hierarchy (6 types + Union), Entity/AggregateRoot, Specification Pattern, structured error codes |
| **Functional Composition** | `Fin<T>`/`FinT<IO,T>` Discriminated Union, LINQ composition, Bind/Apply validation, CQRS path-optimized |
| **Advanced IO** | Timeout, Retry (exponential backoff), Fork (parallel execution), Bracket (resource lifecycle management) |
| **Automation** | 5 Source Generators, Usecase Pipeline (Observability + Validation built-in), architecture rule tests |
| **Observability** | 3-Pillar automatic instrumentation, ctx.* business context propagation, automatic error classification (expected/exceptional/aggregate) |

## Getting Started

### Installation

```bash
# Load both plugins simultaneously
claude --plugin-dir ./.claude/plugins/functorium-develop --plugin-dir ./.claude/plugins/release-note
```

### release-note (v1.0.0) — Release Note Automation

C# script-based data collection, Conventional Commits analysis, Breaking Changes detection, release note writing, and validation — a 5-step workflow automated by 1 skill + 1 agent.

| Principle | Description |
|-----------|-------------|
| Accuracy First | APIs not in the Uber file (`all-api-changes.txt`) are never documented |
| Value Delivery Required | All major features include a "Why this matters" section |
| Automatic Breaking Changes Detection | Git Diff analysis takes precedence over commit message patterns |
| Traceability | All features are tracked by commit SHA |

Detailed documentation: [AX (AI Transformation)](https://hhko.github.io/Functorium/ax/)

### Getting Started with AI (Recommended)

> Start with "Write a PRD for an e-commerce platform" and the AI agents will guide you through the 7-step workflow.

### Getting Started with Packages

```bash
# Core domain modeling — Value Object, Entity, AggregateRoot, Specification, error system
dotnet add package Functorium

# Infrastructure adapters — OpenTelemetry, Serilog, EF Core, Dapper, Pipeline
dotnet add package Functorium.Adapters

# Code generation — [GenerateObservablePort], [GenerateEntityId], CtxEnricher
dotnet add package Functorium.SourceGenerators

# Test utilities — ArchUnitNET, xUnit extensions, integration test fixtures
dotnet add package Functorium.Testing
```

**5-Minute Quickstart:** Build a Value Object → AggregateRoot → Command Usecase in 5 minutes at [Quickstart](./Docs.Site/src/content/docs/quickstart/index.mdx).

**First Tutorial:** Dive deep into Value Objects at the [Functional ValueObject Tutorial](./Docs.Site/src/content/docs/tutorials/functional-valueobject/index.md).

**Full Documentation:** [https://hhko.github.io/Functorium](https://hhko.github.io/Functorium)

## Architecture Overview

![](./Functorium.Architecture.png)

The system is composed of three layers. The domain depends on nothing external, and dependencies always flow inward.

- **Domain Layer** — Pure business logic. Entity, AggregateRoot, Value Object, Specification, DomainError, Domain Event, Repository port (IRepository), IObservablePort. Expresses business rules through pure functions without external dependencies.
- **Application Layer** — Usecase orchestration. CQRS (ICommandRequest, IQueryRequest), FinResponse, IQueryPort (read-only DTO projection), FluentValidation extensions, FinT LINQ composition, Domain Event publishing, IUnitOfWork. Connects domain logic with infrastructure and manages side effect boundaries.
- **Adapter Layer** — Infrastructure implementation. OpenTelemetry configuration, Usecase Pipeline (Observability + Validation built-in, including CtxEnricher), Observable domain event publishing, structured loggers, DapperQueryAdapterBase, AdapterError, 5 Source Generators ([GenerateObservablePort], [GenerateEntityId], CtxEnricher, DomainEventCtxEnricher, [UnionType]). Depends on domain, but domain does not depend on infrastructure.

## Observability

Functorium provides unified observability (Logging, Metrics, Tracing) based on OpenTelemetry.

![](./Functorium.Observability.png)

### Three Observation Paths

| Observation Path | Target | Mechanism | Recorded Content |
|-----------------|--------|-----------|-----------------|
| **Usecase Pipeline** | All Command/Query | Mediator `IPipelineBehavior` | request/response fields + ctx.* + error classification |
| **Observable Port** | Repository, QueryAdapter, ExternalService | `[GenerateObservablePort]` Source Generator | Same request/response field scheme |
| **DomainEvent** | Publisher + Handler | `ObservableDomainEventPublisher` | Event type/count + partial failure tracking |

The Application layer (EventId 1001–1004) and Adapter layer (EventId 2001–2004) use **identical `request.*` / `response.*` / `error.*` naming**, enabling end-to-end request flow tracking with a single dashboard query.

For detailed specifications and guides, see the documentation site:
- [Observability Specification](./Docs.Site/src/content/docs/spec/08-observability.md) — Field/Tag structure, ctx.* 3-Pillar Enrichment, Meter/Instrument specification
- [Logging Guide](./Docs.Site/src/content/docs/guides/observability/19-observability-logging.md) — Structured logging detailed guide
- [Metrics Guide](./Docs.Site/src/content/docs/guides/observability/20-observability-metrics.md) — Metrics collection and analysis guide
- [Tracing Guide](./Docs.Site/src/content/docs/guides/observability/21-observability-tracing.md) — Distributed tracing detailed guide

## Quality Strategy

- Core domain logic maintains a high level of **unit test coverage**.
- Side effect areas are explicitly separated, providing a **verifiable structure**.
- **Observability verification** is completed before deployment.
- Defined error codes and recovery procedures are **documented and validated**.
- Architecture rules are **automatically verified through unit tests** via ClassValidator/InterfaceValidator.

> Quality comes from structure, not from results alone.

## Documentation

**Full Documentation Site:** [https://hhko.github.io/Functorium](https://hhko.github.io/Functorium)

### Tutorials

| Tutorial | Topic | Exercises |
|----------|-------|-----------|
| [Implementing Functional ValueObject](./Docs.Site/src/content/docs/tutorials/functional-valueobject/index.md) | Value Object, Validation, Immutability | 29 |
| [Implementing Specification Pattern](./Docs.Site/src/content/docs/tutorials/specification-pattern/index.md) | Specification, Expression Tree | 18 |
| [Implementing CQRS Repository And Query Patterns](./Docs.Site/src/content/docs/tutorials/cqrs-repository/index.md) | CQRS, Repository, Query Adapter | 22 |
| [Designing TypeSafe Usecase Pipeline Constraints](./Docs.Site/src/content/docs/tutorials/usecase-pipeline/index.md) | Generic Variance, IFinResponse, Pipeline Constraints | 20 |
| [Enforcing Architecture Rules with Testing](./Docs.Site/src/content/docs/tutorials/architecture-rules/index.md) | Architecture Rules, ClassValidator | 16 |
| [Automating ObservabilityCode with SourceGenerator](./Docs.Site/src/content/docs/tutorials/sourcegen-observability/index.md) | Source Generator, Observable Wrapper | — |
| [Automating ReleaseNotes with ClaudeCode and .NET 10](./Docs.Site/src/content/docs/tutorials/release-notes-claude/index.md) | AI Automation, Release Notes | — |

### Samples

| Sample | Scope | Aggregates | Key Patterns |
|--------|-------|------------|--------------|
| [Designing with Types](./Docs.Site/src/content/docs/samples/designing-with-types/index.md) | Domain | 1 | VO, Union, Composite, Specification |
| [E-Commerce DDD](./Docs.Site/src/content/docs/samples/ecommerce-ddd/index.md) | Domain + Application | 5 | CQRS, EventHandler, DomainService, ApplyT |
| [AI Model Governance](./Docs.Site/src/content/docs/samples/ai-model-governance/index.md) | Domain + Application + Adapter | 4 | EfCore/Dapper/InMemory, FastEndpoints, IO.Retry/Timeout/Fork/Bracket |

### Packages

| Package | Description |
|---------|-------------|
| `Functorium` | Core domain modeling — Value Object, Entity, AggregateRoot, Specification, error system |
| `Functorium.Adapters` | Infrastructure adapters — OpenTelemetry, Serilog, EF Core, Dapper, Pipeline |
| `Functorium.SourceGenerators` | Code generation — `[GenerateObservablePort]`, `[GenerateEntityId]`, `CtxEnricherGenerator` |
| `Functorium.Testing` | Test utilities — ArchUnitNET, xUnit extensions, integration test fixtures |

## Tech Stack

| Category | Key Libraries |
|----------|---------------|
| Functional | LanguageExt.Core, Ulid, Ardalis.SmartEnum |
| Validation | FluentValidation |
| Mediator | Mediator (source-generated), Scrutor |
| Persistence | EF Core, Dapper |
| Observability | OpenTelemetry, Serilog |
| Testing | xUnit v3, ArchUnitNET, Verify.Xunit, Shouldly |
