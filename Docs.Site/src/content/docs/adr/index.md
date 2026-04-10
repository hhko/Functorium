---
title: "Architecture Decision Records"
---

## Overview

This document records the major architecture decisions for the Functorium framework. Each decision follows the [MADR v4.0](https://adr.github.io/madr/) template, structuring the context, alternatives considered, rationale for the chosen option, and expected consequences.

### Why ADR

When architecture decisions are scattered across code and commit messages, it becomes increasingly difficult to trace "why was this done this way?" over time. ADRs preserve the context at the time of each decision, allowing new team members to quickly understand the rationale when joining or when revisiting existing decisions.

### Status Legend

| Status | Description |
|------|------|
| `accepted` | Adopted and currently in effect |
| `proposed` | Proposed but not yet agreed upon |
| `deprecated` | No longer valid |
| `superseded` | Replaced by a newer ADR |

### ADR List

| Number | Title | Status |
|------|------|------|
| [ADR-0001](./0001-foundation-use-fin-over-exceptions/) | Foundation - Represent Failures with Fin Types Instead of Exceptions | accepted |
| [ADR-0002](./0002-foundation-languageext/) | Foundation - Adopt LanguageExt as the Functional Base Library | accepted |
| [ADR-0003](./0003-domain-cqrs-read-write-separation/) | Domain - CQRS Read/Write Path Separation | accepted |
| [ADR-0004](./0004-domain-ulid-based-entity-id/) | Domain - Ulid-Based Entity ID | accepted |
| [ADR-0005](./0005-domain-union-valueobject-state-machine/) | Domain - UnionValueObject State Machine | accepted |
| [ADR-0006](./0006-domain-specification-expression-tree/) | Domain - Specification + Expression Tree | accepted |
| [ADR-0007](./0007-domain-aggregate-id-only-cross-references/) | Domain - ID-Only Cross-Aggregate References | accepted |
| [ADR-0008](./0008-domain-service-pure-vs-repository/) | Domain - Domain Service Pure vs Repository | accepted |
| [ADR-0009](./0009-domain-value-object-class-record-duality/) | Domain - Value Object Class/Record Dual Hierarchy | accepted |
| [ADR-0010](./0010-domain-error-code-sealed-record-hierarchy/) | Domain - Error Code Sealed Record Hierarchy | accepted |
| [ADR-0011](./0011-application-pipeline-execution-order/) | Application - Pipeline Execution Order Design | accepted |
| [ADR-0012](./0012-application-finresponse-pipeline-constraints/) | Application - FinResponse Pipeline Type Constraints | accepted |
| [ADR-0013](./0013-application-validation-normalize-maxlength/) | Application - Validation Order: Normalize Then MaxLength | accepted |
| [ADR-0014](./0014-application-explicit-transaction/) | Application - Explicit Transaction Support | accepted |
| [ADR-0015](./0015-adapter-observable-port-source-generator/) | Adapter - Observable Port Source Generator | accepted |
| [ADR-0016](./0016-adapter-suffix-naming-persistence/) | Adapter - Persistence Suffix Naming Pattern | accepted |
| [ADR-0017](./0017-observability-ctx-enricher-pillar-targeting/) | Observability - ctx.* Pillar Targeting Defaults | accepted |
| [ADR-0018](./0018-observability-error-classification/) | Observability - Automatic 3-Type Error Classification | accepted |
| [ADR-0019](./0019-observability-field-naming-snake-case-dot/) | Observability - Field Naming: snake_case + dot | accepted |
| [ADR-0020](./0020-event-publisher-simplification/) | Event - DomainEvent Publisher Simplification | accepted |
| [ADR-0021](./0021-event-tracing-correlation/) | Event - DomainEvent Tracing Correlation IDs | accepted |
| [ADR-0022](./0022-testing-architecture-test-suite/) | Testing - Architecture Test Suite Framework | accepted |

### References

- [MADR v4.0 — Markdown Any Decision Records](https://adr.github.io/madr/)
- [Michael Nygard's Original ADR Proposal](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
