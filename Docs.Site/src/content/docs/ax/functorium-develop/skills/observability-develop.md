---
title: "Observability Develop"
description: "Observability strategy design (KPI mapping, dashboard, alerts, ctx.* propagation)"
---

> project-spec -> architecture-design -> domain-develop -> application-develop -> adapter-develop -> **observability-develop** -> test-develop

## Prerequisites

Performed after Observable Port and CtxEnricher are implemented in the `adapter-develop` skill.
Assumes Functorium's 3-Pillar (Logging/Metrics/Tracing) pipeline is registered in DI.

## Background

The Functorium framework is strong at observability **collection**. `[GenerateObservablePort]` automatically provides Logging/Metrics/Tracing to all adapters, and `CtxEnricher` simultaneously propagates business context to the 3-Pillar.

However, **collection alone is not enough.** Without a strategy for how to analyze collected data, how to determine which metrics are healthy, and how to act when problems occur -- dashboards become "graphs you just look at."

The `observability-develop` skill bridges this gap: **instrument -> analyze -> alert -> act**.

## Skill Overview

### 4 Phase Workflow

| Phase | Activity | Deliverable |
|-------|----------|-------------|
| 1. Observability Strategy | KPI-to-metric mapping, baseline setting, ctx.* propagation strategy | Observability strategy document |
| 2. Dashboard Design | L1 scorecard, L2 drilldown, DomainEvent tracking | Dashboard layout |
| 3. Alert Design | P0/P1/P2 classification, thresholds, alert hygiene | Alert rules document |
| 4. Analysis + Action | Distributed tracing diagnosis, hypothesis-experiment, review templates | Analysis procedure document |

### Trigger Examples

```text
Design observability
Design the dashboard
Analyze metrics
Set up alerts
Analyze performance
```

## Phase 1: Observability Strategy

### KPI -> Technical Metric Mapping

Maps business performance indicators to Functorium observation fields:

| Business KPI | Technical Metric | Functorium Field |
|-------------|-----------------|------------------|
| User response time | P95 latency | `response.elapsed` (Histogram) |
| Service availability | Error rate | `response.status` + `error.type` |
| Per-feature usage | Request count | `request.handler.name` (Counter) |
| Payment success rate | Success/failure ratio | `response.status` by `request.handler.name` |

### Baseline (SLO) Setting

| Metric | Command Baseline | Query Baseline | External API Baseline |
|--------|-----------------|---------------|-----------------------|
| P95 Latency | < 200ms | < 50ms | < 1000ms |
| Error Rate | < 0.1% | < 0.1% | < 1% |
| Throughput | > 100 RPS | > 500 RPS | - |

### ctx.* Propagation Strategy

| CtxPillar | Purpose | Example Fields | Cardinality |
|-----------|---------|---------------|-------------|
| Logging only | Debug/detailed data | Request body, parameter details | Unlimited |
| Logging + Tracing (Default) | Identifiers, tracing context | customer_id, order_id | High |
| All (+ MetricsTag) | Segment analysis | customer_tier, region | **Must be low** |
| MetricsValue | Numeric recording | order_total_amount | - |

**Cardinality Rule:** Only use fields with limited unique values for MetricsTag (customer_tier: 3-5 kinds, customer_id: millions -> forbidden).

## Phase 2: Dashboard Design

### L1 Scorecard (6 Health Indicators)

| Indicator | PromQL Example | Status |
|-----------|---------------|--------|
| Request Count | `rate(usecase_request_total[5m])` | Throughput trend |
| Success Rate | `1 - (error_total / request_total)` | 99.9% or higher |
| P95 Latency | `histogram_quantile(0.95, duration_bucket)` | < 200ms |
| Error Rate | `rate(error_total[5m]) / rate(request_total[5m])` | < 0.1% |
| Exceptional Errors | `rate(error_total{error_type="exceptional"}[5m])` | Converge to 0 |
| DomainEvent Throughput | `rate(event_publish_total[5m])` | Trend check |

### L2 Drilldown

Decomposes into `request.layer` x `request.category.name` x `request.handler.name` 3 dimensions to identify bottlenecks.

## Phase 3: Alert Design

### P0/P1/P2 Classification

| Priority | Condition | Example | Response |
|----------|-----------|---------|----------|
| **P0** (Immediate) | `error.type = "exceptional"` spike | DB connection failure, external API timeout | On-call page |
| **P1** (1 hour) | P95 > 1s or error rate > 5% | Specific handler performance degradation | Slack alert |
| **P2** (Daily) | P95 > 500ms or new error code | Gradual performance degradation | Dashboard review |

## Phase 4: Analysis + Action

When a problem signal is detected, diagnose the cause with distributed tracing:

1. **Signal detection** -- Identify anomaly from dashboard/alert
2. **Trace query** -- Search `request.handler.name = "X"` AND `duration > threshold`
3. **Span analysis** -- Check which child span is consuming time
4. **Hypothesis** -- DB N+1? Cache miss? External API delay?
5. **Experiment** -- Apply improvement and compare against baseline

## Observability Field System

Fields automatically collected by the Functorium Source Generator.

### Request/Response Fields

| Field | Description | Example |
|-------|-------------|---------|
| `request.layer` | Architecture layer | `"application"`, `"adapter"` |
| `request.category.name` | Request category | `"usecase"`, `"repository"`, `"event"` |
| `request.category.type` | CQRS type | `"command"`, `"query"`, `"event"` |
| `request.handler.name` | Handler class name | `"CreateProductCommand"` |
| `request.handler.method` | Handler method name | `"Handle"`, `"GetById"` |
| `response.status` | Response status | `"success"`, `"failure"` |
| `response.elapsed` | Processing time (seconds) | Recorded as Histogram instrument |

### Error Classification System

| `error.type` | Classification | Description | Alert Response |
|-------------|---------------|-------------|----------------|
| `expected` | Business error | Domain rule violation, validation failure | Monitor only (normal flow) |
| `exceptional` | System error | DB connection failure, external API timeout | P0/P1 alert (immediate response) |
| `aggregate` | Composite error | Multiple validation failure accumulation | Monitor (Apply pattern result) |

`error.code` is a domain-specific error code. E.g.: `"ProductName.Required"`, `"Order.InvalidTransition"`.

### Meter/Instrument Naming

| Component | Pattern | Example |
|-----------|---------|---------|
| Meter Name | `{service.namespace}.{layer}[.{category}]` | `AiGovernance.application.usecase` |
| Instrument Name | `{layer}.{category}[.{cqrs}].{type}` | `application.usecase.command.duration` |

Uses dot separation, lowercase, and plural forms.

## Core Principles

- **Collection is just the beginning** -- Design the full cycle: instrument -> analyze -> alert -> act
- **Start from business KPIs** -- Don't just look at technical metrics; translate to business impact
- **Cardinality management** -- Forbid high-cardinality fields in MetricsTag (prevent unbounded series)
- **Alerts must be actionable** -- If you can't answer "What should I do when I receive this alert?", remove it

## References

- [Workflow](../workflow/) -- 7-step overall flow
- [Adapter Develop Skill](../adapter-develop/) -- Preceding step: Observable Port implementation
- [Test Develop Skill](../test-develop/) -- Following step: Observability verification tests
