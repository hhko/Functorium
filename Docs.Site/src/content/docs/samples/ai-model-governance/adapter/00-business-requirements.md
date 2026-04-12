---
title: "Adapter Technical Requirements"
description: "Persistence, external service, HTTP API, and observability technical requirements for the AI Model Governance Platform"
---

## Background

This defines the technical requirements for the Adapter layer, which connects the ports defined in the [application business requirements](../application/00-business-requirements/) to actual technical implementations. This layer implements the interfaces defined by the domain/application layers and handles integration with external systems.

The key differentiator of this example is the **practical application of LanguageExt IO advanced features (Timeout, Retry, Fork, Bracket).** It solves network latency, intermittent failures, timeouts, and resource management issues that arise when integrating with external services in a functional manner.

Core characteristics of the 4 advanced IO features:

| IO Pattern | Problem Scenario | Guarantee | Functorium Integration |
|---------|----------|------|----------------|
| **Timeout + Catch** | Slow responses degrade the entire system | Limits maximum wait time + converts timeout to fallback | `IO.Timeout()` -> `.Catch(TimedOut)` -> `.Catch(Exceptional)` |
| **Retry + Schedule** | Intermittent 503/network errors | Automatic recovery with exponential backoff + jitter | `IO.Retry(exponential \| jitter \| recurs \| maxDelay)` |
| **Fork + awaitAll** | Sequential execution bottleneck for N independent tasks | Parallel execution, worst-case time = max(individual times) | `forks.Map(io => io.Fork())` -> `awaitAll(forks)` |
| **Bracket** | Resource (session, connection) leaks on exceptions | Acquire-Use-Release lifecycle guarantee | `acquire.Bracket(Use: ..., Fin: ...)` |

## Technical Areas

### 1. Persistence

The persistence layer for storing and retrieving data.

- Provides InMemory implementation as default
- Designed to support switching to Sqlite (EF Core/Dapper) implementation
- Selects the implementation via configuration file (`Persistence:Provider`)
- Implements 4 Repository interfaces: IAIModelRepository, IDeploymentRepository, IAssessmentRepository, IIncidentRepository
- Implements 5 Query ports: IAIModelQuery, IModelDetailQuery, IDeploymentQuery, IDeploymentDetailQuery, IIncidentQuery
- Supports the UnitOfWork pattern
- Registers Source Generator-generated Observable wrappers in DI

### 2. External Services (Infrastructure)

The service layer integrating with external AI platforms. Each service demonstrates a specific pattern of LanguageExt IO advanced features.

#### 2-1. Model Health Check (Timeout + Catch)

- Checks the health status of deployed models
- Applies a 10-second timeout
- Returns a TimedOut fallback result instead of failure on timeout
- Converts exceptions to AdapterError

#### 2-2. Model Monitoring (Retry + Schedule)

- Retrieves drift reports for deployed models
- On intermittent failure, retries with exponential backoff (100ms base) + jitter (0.3) + max 3 retries
- Applies a maximum delay of 5 seconds
- Converts to AdapterError on final failure

#### 2-3. Parallel Compliance Check (Fork + awaitAll)

- Checks 5 compliance criteria in parallel: DataGovernance, SecurityReview, BiasAssessment, TransparencyAudit, HumanOversight
- Each criterion check is Forked as an independent IO operation
- Collects all results via awaitAll
- Aggregates whether all criteria passed

#### 2-4. Model Registry Lookup (Bracket)

- Looks up model metadata from an external registry
- Acquire -> Use -> Release the registry session
- Session release is guaranteed regardless of success/failure
- Converts exceptions to AdapterError

### 3. HTTP API (Presentation)

Provides a REST API based on FastEndpoints.

- Model management: registration, lookup, search, risk classification (4 endpoints)
- Deployment management: creation, lookup, search, review submission, activation, quarantine (6 endpoints)
- Assessment management: initiation, lookup (2 endpoints)
- Incident management: reporting, lookup, search (3 endpoints)
- Supports FinResponse -> HTTP status code conversion

### 4. Observability

Provides OpenTelemetry 3-Pillar observability.

- Auto-generates logging/metrics/tracing via `[GenerateObservablePort]` Source Generator
- Provides event publishing observability via `ObservableDomainEventNotificationPublisher`
- Auto-registers Pipeline middleware (Validation, Logging)

## Advanced IO Feature Scenarios

### Normal Scenarios

1. **Health check success** -- Receives a response within 10 seconds and returns a Healthy/Degraded result.
2. **Health check timeout** -- Returns a TimedOut fallback result when exceeding 10 seconds (not an error).
3. **Monitoring retry success** -- Receives the drift report via retry after initial failure.
4. **Parallel check completion** -- 5 criteria execute in parallel, completing faster than sequential execution.
5. **Registry session normal release** -- Session is normally released after successful lookup.
6. **Registry session release after error** -- Session is released even if the lookup fails (Bracket guarantee).

### Rejection Scenarios

7. **Monitoring final failure** -- Returns a MonitoringFailed error if still failing after 3 retries.
8. **Parallel check partial failure** -- Returns a ComplianceCheckFailed error if some criterion checks fail.
9. **Session acquisition failure** -- Returns a RegistryLookupFailed error if registry session acquisition fails.

In the next step, we analyze these technical requirements and derive the IO pattern selection rationale in [Type Design Decisions](./01-type-design-decisions/).
