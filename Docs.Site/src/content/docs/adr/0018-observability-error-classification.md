---
title: "ADR-0018: Observability - 3-Type Error Classification (Expected/Exceptional/Aggregate)"
status: "accepted"
date: 2026-03-22
---

## Context and Problem

Hundreds of error alerts poured into the Grafana alerting channel daily, and most were errors that naturally occur in business flows -- insufficient stock, validation failures, and the like. The problem was that these business errors and actual infrastructure failures like DB connection failures were recorded at the same `error` level, leading the operations team to develop a habit of ignoring alerts and repeatedly missing system failures that required immediate response. Additionally, when 300 out of 1,000 bulk order operations failed, instead of 300 individual error alerts each triggering separately, a composite error expression summarizing "300/1,000 failed, primary causes: insufficient stock 280, price mismatch 20" was needed.

A classification system was needed that consistently distinguishes error severity and cause across OpenTelemetry 3-Pillar (Trace, Metrics, Logs), allowing business errors to be monitored for trends while reserving immediate response for system failures only.

## Considered Options

- **Option 1**: Single error.type string
- **Option 2**: HTTP status code-based classification
- **Option 3**: Expected/Exceptional/Aggregate 3-Type automatic classification
- **Option 4**: No error classification, logging only

## Decision

**Option 3: Adopt Expected/Exceptional/Aggregate 3-Type automatic classification.**

Errors are classified into 3 types and consistently propagated across all OpenTelemetry layers through `error.type` and `error.code` fields. The key is that developers do not manually specify the classification each time -- the pipeline inspects response types and classifies automatically. When humans classify, mistakes happen, and when mistakes happen, alerting rules break.

- **Expected (business error)**: Domain-rule-predictable errors like insufficient stock and validation failures. Recorded at Warning level, not triggering alerts, with trends observed only on business metrics dashboards.
- **Exceptional (system failure)**: Unexpected infrastructure errors like DB connection failures and timeouts. Recorded at Error level, triggering immediate alerts for the operations team to respond.
- **Aggregate (composite error)**: Bundles multiple individual errors from bulk operations into one, summarizing success/failure ratios and common causes. One summary record instead of 300 individual errors.

### Consequences

- **Positive**: Only Exceptional errors trigger alerts, so overnight alerts for the operations team are limited to actual system failures. Dashboards separate business error trends (e.g., "insufficient stock errors up 40% this week") and system failures into separate panels, enabling distinct response strategies for each. Partial failures in bulk operations are summarized into a single Aggregate error, preventing alert floods.
- **Negative**: When adding new DomainErrorTypes, the Expected/Exceptional classification logic must also be updated. For boundary cases like "business errors requiring immediate response" (e.g., payment gateway rejection), team consensus on classification criteria is needed.

### Confirmation

- Verify that the `error.type` field is recorded identically across Trace Spans, Metric Attributes, and Structured Logs.
- Verify that Expected errors do not trigger alerts and only Exceptional errors do.
- Verify that Aggregate errors include individual error lists and summary information.

## Pros and Cons of the Options

### Option 1: Single error.type String

- **Pros**: Simplest implementation. Only a single string like `error.type = "ValidationFailed"` needs to be recorded.
- **Cons**: `"ValidationFailed"` and `"DatabaseConnectionFailed"` are recorded at the same error level, making it impossible to separate alerting rules. Being string-based, inconsistencies like `"validationFailed"` vs `"ValidationFailed"` accumulate, breaking dashboard filters.

### Option 2: HTTP Status Code-Based Classification

- **Pros**: The 400/500 distinction is a widely known standard and immediately compatible with default filters in monitoring tools like Grafana and Datadog.
- **Cons**: Different business causes like validation failures and insufficient permissions are mixed within the same 400 Bad Request, losing business cause information. Not applicable to non-HTTP communication like gRPC and message queues. 1:1 mapping between domain error codes like `StockInsufficient` and HTTP status codes is impossible.

### Option 3: Expected/Exceptional/Aggregate 3-Type Automatic Classification

- **Pros**: Insufficient stock (Expected) is automatically separated as Warning and DB failures (Exceptional) as Error, enabling immediate alerting rule granularity. The pipeline inspects response types for automatic classification, leaving no room for developers to forget or misclassify. Composite errors from bulk operations are summarized as Aggregate, preventing individual alert floods. The same `error.type` value is used across all OpenTelemetry 3-Pillars.
- **Cons**: Initial learning cost for the team to understand the Expected/Exceptional/Aggregate concepts and automatic classification criteria. Classification judgment may be ambiguous for boundary cases like "payment gateway rejection" that are business errors yet require immediate response.

### Option 4: No Error Classification, Logging Only

- **Pros**: Zero implementation cost. No existing code to touch.
- **Cons**: Insufficient stock and DB failures pour in at the same `error` level, leaving alert fatigue unresolved. "Business error trends this week" and "system failure frequency" cannot be viewed separately on dashboards, making error pattern analysis and operational decision-making practically impossible.

## Related Information

- Commits: 6ecd6ae6, a5027a78
- Related ADR: [ADR-0016 Observability field naming snake_case + dot](../0016-observability-field-naming-snake-case-dot.md)
