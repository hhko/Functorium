---
title: "ADR-0017: Observability - CtxEnricher Pillar Targeting Strategy"
status: "accepted"
date: 2026-03-24
---

## Context and Problem

Functorium's CtxEnricher propagates business context (e.g., `ctx.order.id`, `ctx.product.category`) to the observability 3-Pillar (Logging, Tracing, Metrics). The problem is that each Pillar has entirely different sensitivity to context.

Consider what happens when `ctx.order.id` is propagated as a Metrics tag. If there are 100,000 orders per day, 100,000 unique time series are created. Prometheus cardinality warnings trigger, Grafana dashboards fail to render due to query timeouts, and cloud monitoring costs surge proportionally to the number of time series. Meanwhile, the same `ctx.order.id` is essential in Logging for filtering logs by specific order, and in Tracing for tracking distributed call graphs per order. Rather than indiscriminately propagating all context to all 3-Pillars, a default strategy and opt-in mechanism are needed to control which context is propagated to each Pillar.

## Considered Options

1. Default Logging+Tracing, Metrics opt-in via [CtxTarget]
2. Default propagation to all 3-Pillars
3. Default propagation to Logging only

## Decision

**Chosen option: "Default Logging+Tracing, Metrics opt-in via [CtxTarget]"**. Without any configuration, all business context propagates to Logging and Tracing, enabling individual request tracing. By default, no context is propagated as Metrics tags, structurally preventing cardinality explosion. Only fields with low cardinality useful for dashboard filtering, like `ctx.product.category`, are explicitly included with `[CtxTarget(Metrics)]`.

### Consequences

- <span class="adr-good">Good</span>, because high-cardinality fields like `ctx.order.id` are not propagated to Metrics tags by default, structurally preventing time series count growth proportional to business traffic.
- <span class="adr-good">Good</span>, because Logging can filter logs by `ctx.order.id` for a specific order, and Tracing can track distributed call graphs by the same value, recording all information needed for individual request diagnosis without gaps.
- <span class="adr-good">Good</span>, because the `[CtxTarget(Metrics)]` attribute controls Metrics propagation at the field level, enabling fine-grained policies like using `ctx.product.category` (cardinality ~50) as a dashboard filter while excluding `ctx.order.id` (cardinality ~infinite).
- <span class="adr-bad">Bad</span>, because when adding new context fields, developers must judge whether to include them in Metrics and explicitly add `[CtxTarget]`, potentially resulting in missing filter tags needed for dashboards discovered belatedly in production.

### Confirmation

- Verify through unit tests that CtxEnricher propagates context to Logging and Tracing only by default.
- Verify through snapshot tests that only fields with `[CtxTarget(Metrics)]` are included in Metrics tags.
- Verify in 3-Pillar tests that each Pillar's context propagation scope matches expectations.

## Pros and Cons of the Options

### Default Logging+Tracing, Metrics Opt-In via [CtxTarget]

- <span class="adr-good">Good</span>, because Metrics cardinality is controlled with default settings alone, preventing Prometheus cardinality warnings and Grafana query timeouts.
- <span class="adr-good">Good</span>, because low-cardinality fields like `ctx.product.category` can be selectively included via `[CtxTarget(Metrics)]` for dashboard filtering.
- <span class="adr-good">Good</span>, because all business context propagates to Logging and Tracing by default, placing no constraints on individual order/product request tracing.
- <span class="adr-bad">Bad</span>, because developers must understand cardinality concepts and decide `[CtxTarget]` applicability for each context field, requiring cardinality awareness as a prerequisite.

### Default Propagation to All 3-Pillars

- <span class="adr-good">Good</span>, because no `[CtxTarget]` configuration is needed, making implementation simple, and all business context is immediately available in any Pillar.
- <span class="adr-bad">Bad</span>, because including `ctx.order.id` as a Metrics tag creates 100,000 time series per day with 100,000 orders, making Prometheus storage capacity and query costs uncontrollable.
- <span class="adr-bad">Bad</span>, because increasing time series count degrades Grafana dashboard query response times from seconds to timeouts, potentially making operational monitoring itself impossible.

### Default Propagation to Logging Only

- <span class="adr-good">Good</span>, because no context propagates to Metrics and Tracing, structurally eliminating cardinality explosion risk.
- <span class="adr-bad">Bad</span>, because without `ctx.order.id` in Tracing Spans, filtering distributed call graphs by order in Jaeger/Zipkin becomes impossible, making it impossible to trace a specific order's call path during incidents.
- <span class="adr-bad">Bad</span>, because adding business context to Tracing requires building a separate opt-in mechanism again, ultimately regressing to the same design as option 1.

## Related Information

- Related commit: `3a080788` refactor(observability): Rename LogEnricher to CtxEnricher
- Related commit: `e4aae12a` test(observability): Snapshot folder restructuring by category + ctx Enricher 3-Pillar tests added
- Related docs: `Docs.Site/src/content/docs/guides/observability/`
