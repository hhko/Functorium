---
title: "ADR-0011: Application - Pipeline Execution Order"
status: "accepted"
date: 2026-03-22
---

## Context and Problem

Functorium's use case pipeline comprises 7-8 cross-cutting concerns as middleware: CtxEnricher, Metrics, Tracing, Logging, Validation, Caching, Exception, and Transaction. The problem is that their execution order decisively determines observability data accuracy and system behavior.

Consider a specific scenario. If Logging is placed before Validation, every malformed request from bots is logged in detail, accumulating thousands of noise entries per second and driving up log storage costs. Conversely, if Metrics is placed after Validation, validation failure counts are not captured in metrics, making it impossible to explain "why is the success rate 100% yet users are complaining?" Furthermore, Commands need Transaction but Queries do not, and Queries need Caching but Caching on Commands is dangerous -- these differences must be considered to clearly define the stages and order for both pipelines.

## Considered Options

1. CtxEnricher -> Metrics -> Tracing -> Logging -> Validation -> [Caching] -> Exception -> [Transaction] -> Custom -> Handler
2. Logging-first placement
3. Validation-first placement

## Decision

**Chosen option: "CtxEnricher -> Metrics -> Tracing -> Logging -> Validation -> [Caching] -> Exception -> [Transaction] -> Custom -> Handler"**. CtxEnricher sets business context first so all subsequent Pillars can use it; Metrics/Tracing record all requests including validation failures before Validation; and Logging records meaningful requests in detail with enriched context. This order achieves the balance between observability completeness and log noise suppression.

### Consequences

- <span class="adr-good">Good</span>, because Command has 7 stages (Transaction included, Caching excluded) and Query has 8 stages (Caching included, Transaction excluded), each pipeline containing exactly the behaviors it needs.
- <span class="adr-good">Good</span>, because Metrics/Tracing are positioned before Validation, ensuring validation failure counts and failure traces are observed without gaps.
- <span class="adr-good">Good</span>, because `where` constraints block Transaction behavior from being applied to Queries and Caching behavior from being applied to Commands at compile time.
- <span class="adr-bad">Bad</span>, because with 7-8 middleware layers nested, call stacks deepen on exception, and identifying which stage caused the problem takes time.
- <span class="adr-bad">Bad</span>, because changing one behavior's order (e.g., moving Logging after Validation) cascades changes across the entire pipeline's behavior, such as altering the log collection scope.

### Confirmation

- Command pipeline: CtxEnricher -> Metrics -> Tracing -> Logging -> Validation -> Exception -> Transaction -> Custom -> Handler (7 stages).
- Query pipeline: CtxEnricher -> Metrics -> Tracing -> Logging -> Validation -> Caching -> Exception -> Custom -> Handler (8 stages).
- Verify that DI registration order matches the execution order above.

## Pros and Cons of the Options

### CtxEnricher -> Metrics -> Tracing -> Logging -> Validation -> [Caching] -> Exception -> [Transaction] -> Custom -> Handler

- <span class="adr-good">Good</span>, because CtxEnricher at the front sets business context like `ctx.order.id` and `ctx.product.category`, enabling all subsequent Metrics/Tracing/Logging to record with rich context.
- <span class="adr-good">Good</span>, because Metrics before Validation enables "N validation failures per minute" metrics on dashboards.
- <span class="adr-good">Good</span>, because Tracing before Validation means validation failure requests also leave Spans in distributed trace graphs for root cause analysis.
- <span class="adr-good">Good</span>, because Logging executes after CtxEnricher, producing structured logs with business context that include validation failure reasons.
- <span class="adr-good">Good</span>, because Exception behavior is positioned before Transaction, catching exceptions inside the transaction, converting them to `Fin.Fail`, and returning normal responses.
- <span class="adr-bad">Bad</span>, because adding a new behavior among the 7-8 stages requires analyzing dependencies with preceding and succeeding stages, demanding careful positioning review.

### Logging-First Placement

- <span class="adr-good">Good</span>, because every request -- validation failures, cache hits, normal processing -- is logged in detail without gaps, providing rich debugging information during incidents.
- <span class="adr-bad">Bad</span>, because validation failure requests from bots and malformed clients are logged in detail, causing log volume to surge and storage costs to spike.
- <span class="adr-bad">Bad</span>, because logging before CtxEnricher means business context like `ctx.order.id` is empty in logs, making it impossible to determine which order or product the request pertains to from logs alone.

### Validation-First Placement

- <span class="adr-good">Good</span>, because invalid requests are blocked at the first stage, saving processing costs in subsequent stages like Tracing/Logging/Transaction.
- <span class="adr-bad">Bad</span>, because validation failures are not counted in Metrics and no Spans are left in Tracing, making it impossible to construct "validation failure rate" metrics on operational dashboards.
- <span class="adr-bad">Bad</span>, because CtxEnricher has not yet executed, so failure responses are returned without business context, making it impossible to trace which domain object's validation failed.

## Related Information

- Related commit: `ace89d39` feat(books/pipeline): Add type-safe Usecase pipeline constraint design Book
- Related commit: `91b57254` refactor(pipeline): Compile-time Command/Query pipeline filtering via where constraints
- Related docs: `Docs.Site/src/content/docs/tutorials/usecase-pipeline/`
