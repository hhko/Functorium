---
title: "Observability Business Requirements"
description: "Why observability is needed and 3-Pillar requirements for the AI Model Governance Platform"
---

## Why Observability Is Needed

The AI model governance platform automates EU AI Act regulatory compliance. If core workflows such as model registration, deployment state transitions, compliance assessments, and incident auto-quarantine fail, it can lead to regulatory violations. Observability answers the following questions:

1. **Is the system operating normally?** -- Assess overall health status through request throughput, error rate, and latency
2. **Where is the problem occurring?** -- Identify bottlenecks through per-handler, per-layer drill-down
3. **Are business KPIs being achieved?** -- Track incident auto-quarantine response time, compliance assessment pass rate, etc.

---

## 3-Pillar Requirements

### Metrics

Measures system health status through quantitative time-series data.

- Measure request count, response count, and processing time for all Command/Query UseCases
- Measure call count, response count, and processing time for all Repository/Query Adapters
- Measure call count, response count, and processing time for external services (ExternalService)
- Measure DomainEvent publication count and EventHandler processing time
- Provide error classification by error.type (expected/exceptional)
- Propagate bounded cardinality ctx.* fields as MetricsTags to support segment analysis

### Tracing (Distributed Tracing)

Traces the entire path of requests to visualize bottlenecks and dependencies.

- Record the entire span chain from UseCase entry to Repository save
- Record spans for external service calls (health check, monitoring, compliance check, registry)
- Record spans for DomainEvent publication and EventHandler processing
- Propagate ctx.* identifiers (ModelId, DeploymentId) as span tags for searchability

### Logging (Structured Logging)

Records human-readable detailed context.

- Record UseCase entry/exit, Adapter calls/responses as structured logs
- Include error.type, error.code, @error fields on error occurrence
- Propagate request/response ctx.* fields to Serilog LogContext for correlation analysis
- Propagate debug-detailed data via Logging-only pillar

---

## SLO Targets

| Category | Metric | Target | Measurement Method |
|---------|------|------|----------|
| Command UseCase | P95 latency | < 200ms | `histogram_quantile(0.95, application_usecase_command_duration)` |
| Query UseCase | P95 latency | < 50ms | `histogram_quantile(0.95, application_usecase_query_duration)` |
| Repository Adapter | P95 latency | < 100ms | `histogram_quantile(0.95, adapter_repository_duration)` |
| External API | P95 latency | < 500ms | `histogram_quantile(0.95, adapter_external_service_duration)` |
| Overall error rate | Error ratio | < 0.1% | `rate(responses{response_status=failure}) / rate(responses)` |
| Availability | Success ratio | > 99.9% | `1 - (failure_rate)` |

---

## Observability Implementation Strategy

This project uses Functorium's Source Generator-based observability:

1. **`[GenerateObservablePort]`** -- Applied to Repository, Query, and External Service classes to auto-generate Observable wrappers
2. **`ObservableDomainEventNotificationPublisher`** -- Automatically provides observability for DomainEvent publication/handling
3. **Pipeline middleware** -- Batch-enables CtxEnricher, Metrics, Tracing, and Logging with UseObservability(), and registers the rest via explicit opt-in with UseValidation(), UseException(), etc.
4. **`RegisterScopedObservablePort`** -- Registers Observable wrappers to interfaces in DI, providing transparent observability

In the next step, we map business KPIs to technical metrics and decide the ctx.* propagation strategy in [Type Design Decisions](./01-type-design-decisions/).
