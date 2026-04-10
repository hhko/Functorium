---
title: "ADR-0021: Event - Domain Event Tracing and Correlation IDs"
status: "accepted"
date: 2026-03-23
---

## Context and Problem

Suppose a payment failure alert fires in the early morning. The logs contain dozens of domain events mixed together -- `OrderCreated`, `InventoryReserved`, `PaymentFailed`, `InventoryReleased`, etc. To determine "which `OrderCreated` started the flow that led to this `PaymentFailed`?" or "is this `InventoryReleased` a compensating event from `PaymentFailed`, or from a separate request?" requires manually cross-referencing timestamps at the millisecond level to reconstruct event chains.

The existing `IDomainEvent` interface had no event unique identifier, no request trace ID, and no preceding event reference. Events from different concurrent requests occurring at the same time could not be distinguished, and there was no structural way to represent causal chains between events, forcing incident analysis to rely on guesswork.

## Considered Options

1. Ulid EventId + CorrelationId + CausationId + OccurredAt required properties
2. Guid-based EventId
3. No tracing IDs included
4. Single TraceId only

## Decision

**Chosen option: "Ulid EventId + CorrelationId + CausationId + OccurredAt required properties"**, to structurally trace causal relationships between events and transform incident analysis from guesswork to data-driven investigation.

- **EventId (Ulid)**: The event's unique identifier. Ulid encodes the timestamp in the upper bits, so lexicographic sorting alone restores event occurrence order. Unlike Guid v4, no separate sort key is needed.
- **CorrelationId**: Groups all events derived from a single user request. "The entire event flow for order #A" can be queried with a single query.
- **CausationId**: References the EventId of the immediately preceding event that caused this event. Causal chains like `OrderCreated -> InventoryReserved -> PaymentRequested` can be reconstructed as a tree structure.
- **OccurredAt**: Explicitly records the event occurrence time, providing audit trailing independent of Ulid's time information.

### Consequences

- <span class="adr-good">Good</span>, because CorrelationId queries "all events from this request" and CausationId traces "the cause chain of this event" as a tree structure, dramatically reducing incident analysis time.
- <span class="adr-good">Good</span>, because Ulid-based EventId's lexicographic ordering equals chronological ordering, naturally preserving occurrence order in event stores without separate indexes.
- <span class="adr-good">Good</span>, because mapping CorrelationId to OpenTelemetry's TraceId enables unified querying of domain event traces and infrastructure distributed traces on a single dashboard.
- <span class="adr-bad">Bad</span>, because all domain event records must include the 4 required properties `EventId`, `CorrelationId`, `CausationId`, and `OccurredAt`, increasing event definition boilerplate.

### Confirmation

- Verify through architecture rule tests that IDomainEvent implementations include all 4 required properties.
- Verify through integration tests that CausationId in event chains matches the preceding event's EventId.

## Pros and Cons of the Options

### Ulid EventId + CorrelationId + CausationId + OccurredAt Required Properties

- <span class="adr-good">Good</span>, because the upper 48 bits of Ulid's 128 bits are a millisecond timestamp, solving both uniqueness and chronological ordering with a single ID without separate fields.
- <span class="adr-good">Good</span>, because CorrelationId provides horizontal querying across the entire request, while CausationId provides vertical tracing of a specific event's cause, supporting analysis along both axes.
- <span class="adr-good">Good</span>, because OccurredAt records precise audit-purpose timestamps independent of Ulid's timestamp.
- <span class="adr-bad">Bad</span>, because all 4 properties must be set on every event creation, and the interface must enforce compile errors on omission.

### Guid-Based EventId

- <span class="adr-good">Good</span>, because `System.Guid` is a .NET standard type, usable without external library dependencies.
- <span class="adr-bad">Bad</span>, because Guid v4 is randomly generated, so comparing two EventIds cannot determine which event occurred first, always requiring separate timestamp comparison for event ordering.
- <span class="adr-bad">Bad</span>, because using non-sequential Guids as DB index keys causes frequent page splits, degrading write performance.

### No Tracing IDs Included

- <span class="adr-good">Good</span>, because events contain only business payloads, keeping the structure simplest.
- <span class="adr-bad">Bad</span>, because when hundreds of events occur simultaneously in production, there is no way to filter events belonging to a specific request, forcing incident analysis to rely on millisecond timestamp cross-referencing.
- <span class="adr-bad">Bad</span>, because there is no structural way to determine which `OrderCreated` started the flow leading to a `PaymentFailed` event, leaving causal relationship reconstruction at the level of guesswork.

### Single TraceId Only

- <span class="adr-good">Good</span>, because only one property needs to be added, making implementation simple.
- <span class="adr-good">Good</span>, because TraceId can group "all events from this request."
- <span class="adr-bad">Bad</span>, because within the same request, the causal relationship that `OrderCreated` triggered `InventoryReserved` cannot be expressed -- the event list is visible but the flow direction is unknown.
- <span class="adr-bad">Bad</span>, because when 10 events occur from a single request, the order and which event triggered which event cannot be reconstructed as a tree structure.

## Related Information

- Related commits: `58b2719c`, `292a5850`
- Related guide: `Docs.Site/src/content/docs/guides/domain/`
- Reference: OpenTelemetry Trace Context, W3C Trace Context specification
