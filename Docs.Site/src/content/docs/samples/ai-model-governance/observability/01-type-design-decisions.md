---
title: "Observability Type Design Decisions"
description: "KPI-metric mapping, key metrics per layer, SLO baselines, and ctx.* propagation strategy"
---

## KPI -> Technical Metric Mapping

Maps the SLOs and KPIs defined in [business requirements](../00-business-requirements/) to Functorium metrics.

| Business KPI | Functorium Metric | Field/Tag |
|-------------|-------------------|----------|
| Model registration completion rate | `application.usecase.command.responses` | `request.handler.name=RegisterModelCommand`, `response.status` |
| Incident auto-quarantine response time | `application.usecase.event.duration` | `request.handler.name=QuarantineDeploymentOnCriticalIncidentHandler` |
| Compliance assessment pass rate | `application.usecase.command.responses` | `request.handler.name=InitiateAssessmentCommand`, `response.status` |
| External service stability | `adapter.external_service.responses` | `response.status`, `error.type` |
| Overall error rate | `application.usecase.command.responses` | `response.status=failure` |

---

## Key Metrics Per Layer

### Application Layer

| Metric | Instrument | Type | Description |
|------|-----------|------|------|
| UseCase request count | `application.usecase.{cqrs}.requests` | Counter | Command/Query/Event request count |
| UseCase processing time | `application.usecase.{cqrs}.duration` | Histogram | Command/Query/Event processing time (seconds) |
| UseCase error count | `application.usecase.{cqrs}.responses` | Counter | `response.status=failure` filter |

### Adapter Layer

| Metric | Instrument | Type | Description |
|------|-----------|------|------|
| Repository call count | `adapter.repository.requests` | Counter | Repository method call count |
| Repository processing time | `adapter.repository.duration` | Histogram | Repository processing time (seconds) |
| External Service call count | `adapter.external_service.requests` | Counter | External service call count |
| External Service processing time | `adapter.external_service.duration` | Histogram | External service processing time (seconds) |

### DomainEvent

| Metric | Instrument | Type | Description |
|------|-----------|------|------|
| Event publication count | `adapter.event.requests` | Counter | DomainEvent publication count |
| Event handler processing time | `application.usecase.event.duration` | Histogram | EventHandler processing time (seconds) |

---

## SLO Baselines

| Category | Metric | Target | PromQL |
|---------|------|------|--------|
| Command UseCase | P95 latency | < 200ms | `histogram_quantile(0.95, sum(rate(application_usecase_command_duration_bucket[5m])) by (le))` |
| Query UseCase | P95 latency | < 50ms | `histogram_quantile(0.95, sum(rate(application_usecase_query_duration_bucket[5m])) by (le))` |
| Repository | P95 latency | < 100ms | `histogram_quantile(0.95, sum(rate(adapter_repository_duration_bucket[5m])) by (le))` |
| External Service | P95 latency | < 500ms | `histogram_quantile(0.95, sum(rate(adapter_external_service_duration_bucket[5m])) by (le))` |
| Overall error rate | Error ratio | < 0.1% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m])` |

---

## ctx.* Propagation Strategy

### CtxPillar Selection Decisions

The ctx.* fields and CtxPillar propagation strategy used in this project.

#### RegisterModelCommand

| Property | CtxPillar | Rationale |
|---------|-----------|------|
| `Name` (string) | Default (L+T) | Model name is unbounded cardinality -> MetricsTag prohibited |
| `Version` (string) | Default (L+T) | SemVer string -> for trace searching |
| `Purpose` (string) | Logging only | Long string (500 chars) -> for debug/audit |

#### CreateDeploymentCommand

| Property | CtxPillar | Rationale |
|---------|-----------|------|
| `ModelId` (string) | Default (L+T) | Unbounded ID -> for trace searching |
| `EndpointUrl` (string) | Default (L+T) | URL -> for trace searching |
| `Environment` (string) | All (L+T+MetricsTag) | Bounded (2 values: Staging/Production) -> safe for segment analysis |
| `DriftThreshold` (decimal) | Default + MetricsValue | Numeric -> for Histogram distribution analysis |

#### ReportIncidentCommand

| Property | CtxPillar | Rationale |
|---------|-----------|------|
| `DeploymentId` (string) | Default (L+T) | Unbounded ID -> for trace searching |
| `Severity` (string) | All (L+T+MetricsTag) | Bounded (4 values: Critical/High/Medium/Low) -> safe for segment analysis |
| `Description` (string) | Logging only | Long string (2000 chars) -> for debug/audit |

### Cardinality Management Principles

| Cardinality Level | MetricsTag Allowed | Example (this project) |
|---------------|----------------|-------------------|
| Fixed (`bool`) | Safe | -- |
| BoundedLow (`enum`, < 20 values) | Conditionally allowed | `Environment` (2 values), `Severity` (4 values), `RiskTier` (4 values), `DeploymentStatus` (6 values) |
| Unbounded (`string`, `Guid`) | **Prohibited** | `ModelId`, `DeploymentId`, `EndpointUrl` |
| Numeric (`decimal`, `int`) | **Warning** | `DriftThreshold` -> use MetricsValue instead |

### Decision Flow

```
Is the property for debugging? (Purpose, Description)
├── YES -> Logging only: [CtxTarget(CtxPillar.Logging)]
└── NO -> Needs trace searching?
    ├── NO -> [CtxIgnore]
    └── YES -> Use as metric segment?
        ├── NO -> Default (L+T)
        └── YES -> Is cardinality bounded?
            ├── YES -> [CtxTarget(CtxPillar.All)]
            └── NO -> Is it numeric?
                ├── YES -> [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
                └── NO -> Keep Default (MetricsTag prohibited)
```

In the next step, we materialize this metric design into dashboards, alerts, and code patterns in [Code Design](../02-code-design/).
