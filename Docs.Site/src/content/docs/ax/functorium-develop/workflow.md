---
title: "Workflow"
description: "7-step development workflow from PRD to testing"
---

AX provides a development workflow consisting of 7 sequential steps from PRD writing to testing. Each step takes the deliverables from the previous step as input, ensuring design intent is consistently carried through to the code.

## Overall Flow

```
 1. project-spec          Vision -> Requirements specification
        |
        v
 2. architecture-design   Structure -> Folder/naming/infrastructure design
        |
        v
 3. domain-develop        Model -> VO, Aggregate, Event implementation
        |
        v
 4. application-develop   Flow -> Command/Query/EventHandler implementation
        |
        v
 5. adapter-develop       Connection -> Repository, Endpoint, DI implementation
        |
        v
 6. observability-develop Observation -> KPI mapping, dashboard, alerts, ctx.* propagation
        |
        v
 7. test-develop          Verification -> Unit/integration/architecture/observability tests
```

Separately, the `domain-review` skill reviews existing code from a DDD perspective at any point.

## Step-by-Step Summary

### Step 1: Project Spec -- From Vision to Spec

| Item | Description |
|------|-------------|
| Skill | [project-spec](../skills/project-spec/) |
| Input | User's vision, business problem, target users |
| Output | `{context}/00-project-spec.md` |
| Key Activities | Ubiquitous language extraction, Aggregate candidate identification, business rule classification, MVP scope definition |

Starting with business language, it draws the first outline of the domain model. Through conversation, it progresses from "What problem are we solving?" to "What Aggregates are needed and what rules exist?"

### Step 2: Architecture Design -- Structural Decisions

| Item | Description |
|------|-------------|
| Skill | [architecture-design](../skills/architecture-design/) |
| Input | `00-project-spec.md` (automatically referenced if available) |
| Output | `{context}/01-architecture-design.md` |
| Key Activities | Project structure, layer composition, naming conventions, persistence/observability/API infrastructure decisions |

Before writing code, the solution skeleton is determined. It documents the 3-dimensional folder structure (Aggregate/CQRS Role/Technology), project reference direction, DI registration strategy, and Provider switching configuration.

### Step 3: Domain Develop -- Model Implementation

| Item | Description |
|------|-------------|
| Skill | [domain-develop](../skills/domain-develop/) |
| Input | `00-project-spec.md`, `01-architecture-design.md` (automatically referenced if available) |
| Output | `domain/00~03` 4 documents + VO, Aggregate, Event, Spec, Service source code |
| Key Activities | Invariant classification, Functorium type mapping, code generation, unit tests |

This step encodes invariants as types. Business rules are classified (single-value/comparison/enumeration/state-transition/exclusive-state), mapped to appropriate Functorium types (`SimpleValueObject`, `ComparableSimpleValueObject`, `UnionValueObject`, `AggregateRoot`, etc.), then actual code and tests are generated.

### Step 4: Application Develop -- Use Case Implementation

| Item | Description |
|------|-------------|
| Skill | [application-develop](../skills/application-develop/) |
| Input | `domain/03-implementation-results.md` (domain model status) |
| Output | `application/00~03` 4 documents + Command, Query, EventHandler, Validator source code |
| Key Activities | CQRS classification, port identification, FinT LINQ composition, FluentValidation integration |

Business flows are built on top of the domain model. Workflows are decomposed into Command/Query/EventHandler, ports (IRepository, IQueryPort, External Service) that each use case depends on are identified, then handlers are implemented with FinT monad composition.

### Step 5: Adapter Develop -- Infrastructure Connection

| Item | Description |
|------|-------------|
| Skill | [adapter-develop](../skills/adapter-develop/) |
| Input | `application/03-implementation-results.md` (port list), `01-architecture-design.md` (infrastructure strategy) |
| Output | `adapter/00~03` 4 documents + Repository, Query Adapter, Endpoint, DI registration source code |
| Key Activities | InMemory/EfCore Repository, Dapper Query, FastEndpoints, Observable Port, DI registration |

Concrete infrastructure technologies are connected to port interfaces. `[GenerateObservablePort]` automatically provides observability, Mappers separate the domain-persistence boundary, and Provider switching enables toggling between InMemory/Sqlite.

### Step 6: Observability Develop -- Observability Strategy

| Item | Description |
|------|-------------|
| Skill | [observability-develop](../skills/observability-develop/) |
| Input | Implemented adapter code (Observable Port, CtxEnricher) |
| Output | Observability strategy document (KPI mapping, dashboard layout, alert rules) |
| Key Activities | KPI-to-metric mapping, baseline setting, dashboard design, alert patterns, ctx.* propagation strategy |

This step designs how to analyze collected observation data and take action. Business KPIs are mapped to technical metrics, L1/L2 dashboards are designed, P0/P1/P2 alerts are classified, and distributed tracing analysis procedures for failures are defined.

### Step 7: Test Develop -- Quality Verification

| Item | Description |
|------|-------------|
| Skill | [test-develop](../skills/test-develop/) |
| Input | Implemented source code, `03-implementation-results.md` documents, observability strategy document |
| Output | Unit tests, integration tests, architecture rule tests, observability verification test code |
| Key Activities | VO/Aggregate/Usecase unit tests, HostTestFixture integration tests, ArchUnitNET rules, ctx 3-Pillar snapshot tests |

This verifies that the implementation meets the design intent. It tests Value Object Create success/failure, Aggregate state changes and event publishing, Usecase success/failure scenarios, HTTP endpoint status codes, layer dependency direction, and ctx.* 3-Pillar propagation consistency.

## Inter-Step Connections

Each step's output documents become the input for the next step. This connection ensures consistency of design intent.

```
00-project-spec.md
  |-- Ubiquitous language table --> Referenced by domain-develop Phase 1
  |-- Aggregate candidate list  --> Reflected in architecture-design folder structure
  |-- Business rules            --> Used by domain-develop Phase 2 for type mapping
  |-- Use case overview         --> Used by application-develop Phase 1 for Command/Query classification

01-architecture-design.md
  |-- Folder structure           --> Determines code generation location for domain/application/adapter
  |-- Naming conventions         --> Applied to all code generation
  |-- Persistence strategy       --> Used for adapter-develop Provider selection

domain/03-implementation-results.md
  |-- Aggregate list             --> Identifies Repository Ports for application-develop
  |-- VO list                    --> Used for Validator writing in application-develop
  |-- Domain Event list          --> Identifies EventHandlers for application-develop

application/03-implementation-results.md
  |-- Port interfaces            --> Implementation targets for adapter-develop
  |-- Request/Response DTOs      --> Used for Endpoint writing in adapter-develop

adapter/03-implementation-results.md
  |-- Observable Port list       --> KPI mapping targets for observability-develop
  |-- CtxEnricher fields         --> ctx.* propagation strategy for observability-develop
```

## Flexible Entry Points

It is not necessary to follow the 7 steps in order. Each skill operates independently, and if prerequisite documents are missing, it asks the user directly.

| Situation | Starting Skill | Reason |
|-----------|---------------|--------|
| Starting a new project | project-spec | Systematically define from the vision |
| Requirements already organized | architecture-design or domain-develop | Skip PRD and start from structure/model |
| Adding use cases to existing domain model | application-develop | Add only new Commands/Queries |
| Replacing only adapters in existing code | adapter-develop | Switch from InMemory to EF Core |
| Filling in missing tests | test-develop | Add tests to existing code |
| Checking existing code quality | domain-review | Identify improvements through DDD review |

## Real Project Example

Here is the process of developing an AI model governance platform in 7 steps.

### Step 1: Project Spec

```text
Write the PRD. I want to build an AI model governance platform.
```

Deliverables:
- Ubiquitous language: AIModel, ModelDeployment, ComplianceAssessment, ModelIncident
- 4 Aggregates identified
- State transitions: Draft -> PendingReview -> Active -> Quarantined -> Decommissioned
- Cross-cutting rules: Critical incident -> Automatic quarantine of Active deployment
- Forbidden states: Deployment of models with Unacceptable risk level

### Step 2: Architecture Design

```text
Design the project structure.
```

Deliverables:
- `AiGovernance.Domain`, `AiGovernance.Application`, `AiGovernance.Adapters.*` project structure
- Folders per Aggregate: `AIModels/`, `ModelDeployments/`, `ComplianceAssessments/`, `ModelIncidents/`
- Persistence: InMemory (development) + SQLite (production)
- Observability: OpenTelemetry 3-Pillar

### Step 3: Domain Develop

```text
Design and implement the AIModel Aggregate.
```

Deliverables:
- Value Object: ModelName, ModelVersion, RiskLevel(SmartEnum), ModelStatus(UnionValueObject)
- AggregateRoot: AIModel (Create, SubmitForReview, Activate, Quarantine, Decommission)
- Domain Event: CreatedEvent, ActivatedEvent, QuarantinedEvent
- Specification: AIModelByStatusSpec, AIModelByRiskLevelSpec
- 40+ unit tests

### Step 4: Application Develop

```text
Create the model registration Command Usecase.
```

Deliverables:
- Command: RegisterAIModelCommand, SubmitForReviewCommand
- Query: GetAIModelByIdQuery, SearchAIModelsQuery
- EventHandler: OnModelActivated (deployment trigger)
- Validator: FluentValidation + MustSatisfyValidation

### Step 5: Adapter Develop

```text
Implement the AIModel Repository with EF Core.
```

Deliverables:
- InMemory + EfCore Repository
- Model, Configuration, Mapper
- FastEndpoints endpoints
- DI registration (Provider switching)

### Step 6: Observability Develop

```text
Design the observability strategy.
```

Deliverables:
- KPI-to-metric mapping: Model registration success rate, deployment P95 latency, compliance assessment throughput
- ctx.* propagation strategy: risk_tier(MetricsTag), model_id(Tracing), assessment_detail(Logging)
- L1 dashboard: 6 health indicator scorecard
- Alert rules: P0(DB connection failure), P1(Deployment API P95 > 1s), P2(New error code appearance)

### Step 7: Test Develop

```text
Write unit tests for the AIModel domain.
```

Deliverables:
- VO unit tests: Create success/failure, normalization
- Aggregate unit tests: State transition success/failure, event publishing
- Usecase unit tests: Mock-based success/failure
- Integration tests: HTTP endpoint 201/400/404
- Architecture rules: Layer dependency, sealed class
- Observability verification: ctx.* 3-Pillar snapshot tests

## Per-Layer 4-Step Documents

The domain-develop, application-develop, and adapter-develop skills each generate the same 4-step documents.

| Document | Content | Purpose |
|----------|---------|---------|
| `00-business-requirements.md` | Requirements, ubiquitous language, business rules | What are we building |
| `01-type-design-decisions.md` | Invariant classification, type/port/adapter mapping strategy | What types to use |
| `02-code-design.md` | Translation from strategy to C#/Functorium patterns | How to implement |
| `03-implementation-results.md` | Implementation code + test verification results | What was actually built |

This 4-step document system provides traceability of design decisions. The question "Why was this type chosen?" is answered by document 01, and the question "Why was this pattern used?" is answered by document 02.

## Next Steps

- [Project Spec Skill](../skills/project-spec/) -- Start from the first step
- [Expert Agents](../agents/) -- Leverage experts for design decisions
- [Plugin Overview](../) -- Installation and structure guide
