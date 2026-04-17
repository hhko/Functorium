---
title: "Observability Code Design"
description: "L1 scorecard, L2 drill-down, alert rules, and ctx.* propagation code patterns"
---

## L1 Scorecard: Overall Health Status

Grasp the overall status of the AI Model Governance platform at a glance with 6 health indicators.

| Indicator | PromQL | Threshold (Green / Yellow / Red) |
|------|--------|-------------------------------|
| Request Count | `sum(rate(application_usecase_command_requests_total[5m]))` | Normal range / +-50% fluctuation / +-80% fluctuation |
| Success Rate | `sum(rate(responses{response_status="success"}[5m])) / sum(rate(responses[5m])) * 100` | > 99.9% / > 99% / < 99% |
| P95 Latency | `histogram_quantile(0.95, sum(rate(duration_bucket[5m])) by (le))` | < 200ms / < 500ms / > 500ms |
| Error Rate | `sum(rate(responses{response_status="failure"}[5m])) / sum(rate(responses[5m])) * 100` | < 0.1% / < 1% / > 1% |
| Availability | `1 - (sum(rate(responses{error_type="exceptional"}[5m])) / sum(rate(responses[5m])))` | > 99.9% / > 99.5% / < 99.5% |
| Throughput | `sum(rate(responses{response_status="success"}[5m]))` | Stable relative to baseline / -20% / -50% |

### Grafana Panel Configuration

```
L1 Scorecard Dashboard
├── Row 1: Stat panels x 6 (health indicators)
├── Row 2: Time series graph (request count + error rate overlay)
├── Row 3: Time series graph (P95/P99 latency)
└── Row 4: Table (recent error top 10 by error.code)
```

---

## L2 Drill-Down: Per-Handler Details

The L2 dashboard drills down by `request.layer` x `request.category.name` x `request.handler.name` dimensions.

### Application Layer Drill-Down

```promql
# Requests per second by handler
sum(rate(application_usecase_command_requests_total[5m])) by (request_handler_name)

# P95 latency by handler
histogram_quantile(0.95,
  sum(rate(application_usecase_command_duration_bucket[5m])) by (le, request_handler_name)
)

# Error rate by handler
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m])) by (request_handler_name)
/ sum(rate(application_usecase_command_responses_total[5m])) by (request_handler_name) * 100

# Distribution by error.type
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m])) by (error_type)
```

### Adapter Layer Drill-Down

```promql
# P95 latency by repository
histogram_quantile(0.95,
  sum(rate(adapter_repository_duration_bucket[5m])) by (le, request_handler_name)
)

# Error rate by external service
sum(rate(adapter_external_service_responses_total{response_status="failure"}[5m])) by (request_handler_name)
/ sum(rate(adapter_external_service_responses_total[5m])) by (request_handler_name) * 100
```

### DomainEvent Visualization

```promql
# Published events by event type
sum(rate(adapter_event_requests_total[5m])) by (request_handler_name)

# Processing time by event handler
histogram_quantile(0.95,
  sum(rate(application_usecase_event_duration_bucket[5m])) by (le, request_handler_name)
)
```

### Grafana Panel Configuration

```
L2 Drill-Down Dashboard
├── Variable: $layer (application/adapter), $category, $handler
├── Row 1: Selected handler request count + error rate
├── Row 2: P50/P95/P99 latency time series
├── Row 3: error.type distribution (expected vs exceptional)
├── Row 4: error.code top 10 table
└── Row 5: DomainEvent publish → Handler chain
```

---

## Alert Rules

### P0 -- Critical (Immediate Response)

| Condition | PromQL | Action |
|------|--------|------|
| `error.type=exceptional` spike | `rate(responses{error_type="exceptional"}[5m]) > 0.01` | System error -> infrastructure check, log review |
| Overall error rate > 10% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m]) > 0.1` | Declare incident immediately |
| External service timeout cascade | Multiple external services with simultaneous `error.type=exceptional` | Check dependency service outage |

```yaml
groups:
  - name: ai-governance-p0
    rules:
      - alert: ExceptionalErrorRateHigh
        expr: |
          sum(rate(application_usecase_command_responses_total{error_type="exceptional"}[5m]))
          / sum(rate(application_usecase_command_responses_total[5m]))
          > 0.01
        for: 2m
        labels:
          severity: critical
          team: platform
        annotations:
          summary: "System error rate exceeded 1%"
          description: "error.type=exceptional ratio: {{ $value | humanizePercentage }}"
```

### P1 -- Warning (Respond Within 15 Minutes)

| Condition | PromQL | Action |
|------|--------|------|
| Key handler P95 > 1s | `histogram_quantile(0.95, duration_bucket{request_handler_name="..."}) > 1` | Analyze slow queries, external API latency |
| Error rate > 5% | `rate(responses{response_status="failure"}[5m]) / rate(responses[5m]) > 0.05` | Classify by error.code and identify root causes |
| EventHandler processing delay | `histogram_quantile(0.95, application_usecase_event_duration_bucket) > 5` | Analyze event handler bottleneck |

```yaml
      - alert: HandlerLatencyHigh
        expr: |
          histogram_quantile(0.95,
            sum(rate(application_usecase_command_duration_bucket[5m])) by (le, request_handler_name)
          ) > 1.0
        for: 5m
        labels:
          severity: warning
          team: backend
        annotations:
          summary: "{{ $labels.request_handler_name }} P95 latency exceeded 1 second"
```

### P2 -- Info (Review During Business Hours)

| Condition | PromQL | Action |
|------|--------|------|
| P95 > 500ms | `histogram_quantile(0.95, duration_bucket) > 0.5` | Check performance trends, register in backlog |
| New error.code appears | Previously unseen error.code emerges | Analyze new error path |
| Traffic pattern change | Request count +-50% relative to baseline | Review capacity planning |

---

## ctx.* Propagation Code Patterns

### Specifying CtxPillar in Command Requests

```csharp
public sealed class CreateDeploymentCommand
{
    public sealed record Request(
        string ModelId,                                      // Default(L+T): Unbounded ID
        string EndpointUrl,                                  // Default(L+T): URL
        [CtxTarget(CtxPillar.All)] string Environment,       // All(L+T+MetricsTag): Bounded (2 values)
        [CtxTarget(CtxPillar.Default | CtxPillar.MetricsValue)]
        decimal DriftThreshold                               // Default + MetricsValue: numeric
    ) : ICommandRequest<Response>;
}
```

### Specifying CtxPillar in DomainEvents

```csharp
public sealed record ReportedEvent(
    ModelIncidentId IncidentId,                              // Default(L+T)
    [CtxTarget(CtxPillar.All)] IncidentSeverity Severity,    // All(L+T+MetricsTag): Bounded (4 values)
    ModelDeploymentId DeploymentId                           // Default(L+T)
) : DomainEvent;
```

### Generated ctx.* Field Mapping Example (CreateDeploymentCommand)

| ctx Field | Logging | Tracing | MetricsTag | MetricsValue |
|----------|---------|---------|------------|--------------|
| `ctx.create_deployment_command.request.model_id` | O | O | - | - |
| `ctx.create_deployment_command.request.endpoint_url` | O | O | - | - |
| `ctx.create_deployment_command.request.environment` | O | O | **O** | - |
| `ctx.create_deployment_command.request.drift_threshold` | O | O | - | **O** |

### Segment Analysis PromQL

```promql
# Error rate by deployment environment (Staging vs Production)
sum(rate(application_usecase_command_responses_total{response_status="failure"}[5m]))
  by (ctx_create_deployment_command_request_environment)
/ sum(rate(application_usecase_command_responses_total[5m]))
  by (ctx_create_deployment_command_request_environment) * 100

# Auto-isolation latency by incident severity
histogram_quantile(0.95,
  sum(rate(application_usecase_event_duration_bucket[5m]))
    by (le, ctx_reported_event_severity)
)
```

---

## Alert -> Analysis Flow

When an alert is triggered, analyze the root cause in the following order:

1. **Check L1 Scorecard** -- Assess overall health status
2. **L2 Drill-Down** -- Identify the problematic handler by `request.handler.name`
3. **Distributed Tracing** -- Search for slow traces of the handler, analyze span chain
4. **ctx.* Segments** -- Check if concentrated in a specific environment (Staging/Production) or severity
5. **Detailed Logs** -- Identify root cause via `error.code` and `@error` fields

Check the actual Observable Port status and pipeline configuration in the [Implementation Results](../03-implementation-results/).
