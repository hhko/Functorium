---
title: "ADR-0020: Event - Domain Event Publisher Simplification (BatchHandler Removal)"
status: "accepted"
date: 2026-03-24
---

## Context and Problem

During a bulk price change operation on 1,000 products, when `PriceChangedEvent` handling failed, the stack trace traversed 7 layers of reflection-based dispatch logic, consuming unnecessary time finding the actual cause. `IDomainEventBatchHandler` contained 100 lines of reflection logic that dynamically discovered and invoked type-specific generic handlers using `MakeGenericType` and `Invoke`, leaving only unclear error messages like "Object does not match target type" on type mismatches. Debugging capability and type safety were being sacrificed for the convenience of bulk event batch processing.

A simple and explicit structure was needed that eliminates the fundamental complexity of reflection while still meeting event processing requirements for bulk operations.

## Considered Options

- **Option 1**: Keep IDomainEventBatchHandler and optimize reflection
- **Option 2**: Remove BatchHandler + move bulk logic to Domain Service
- **Option 3**: Remove event publishing entirely

## Decision

**Option 2: Remove BatchHandler and move bulk logic to Domain Service. (BREAKING CHANGE)**

The reason for accepting a BREAKING CHANGE is clear. Debugging capability and type safety are more valuable than the "automation convenience" of 100 lines of reflection. Batch processing of events from bulk operations is not something the framework should do implicitly -- it is a responsibility that business logic should express explicitly.

- The `IDomainEventBatchHandler` interface and reflection-based dispatch logic are completely removed.
- Event processing from bulk operations is handled by explicit Domain Services (e.g., `ProductBulkOperations`).
- The domain event publisher focuses solely on single event publishing, clarifying its responsibility.
- Domain Services aggregate bulk operation results and publish summary events as needed.

### Consequences

- **Positive**: With 100 lines of reflection removed, the event publisher's code is reduced to less than half, and stack traces on failure point directly to business code. Business intent for operations like bulk price changes is explicitly visible in `ProductBulkOperations` Domain Service, and reading the code alone reveals "what is being batched together." All event dispatch is type-verified at compile time.
- **Negative**: As a BREAKING CHANGE, all existing `IDomainEventBatchHandler` implementations must be migrated to Domain Services. Writing dedicated Domain Service classes for each bulk operation introduces some boilerplate.

### Confirmation

- Verify that all code previously using BatchHandler has been migrated to Domain Services.
- Verify through integration tests that individual events are published without omission in bulk operations.
- Verify that Domain Service bulk logic operates correctly within transaction boundaries.

## Pros and Cons of the Options

### Option 1: Keep IDomainEventBatchHandler and Optimize Reflection

- **Pros**: Minimal existing code changes. Bulk event batch processing happens automatically at the framework level, keeping call-site code concise.
- **Cons**: The fundamental complexity of `MakeGenericType`/`Invoke`-based dispatch is not solved by optimization. Unclear errors like "Object does not match target type" persist on type mismatches. Reflection call overhead occurs on every bulk operation. Stack traces traverse reflection internals, making it difficult to reach the actual cause.

### Option 2: Remove BatchHandler + Move to Domain Service

- **Pros**: With 100 lines of reflection completely removed, the event publisher code is simplified, and stack traces on failure point directly to business code. Bulk logic is explicitly visible in Domain Services like `ProductBulkOperations`, making business intent apparent from code alone. All event dispatch is type-verified at compile time.
- **Cons**: As a BREAKING CHANGE, all existing BatchHandler implementations must be migrated to Domain Services. Dedicated Domain Services for each bulk operation introduce boilerplate, and framework-level automatic batch processing is lost.

### Option 3: Remove Event Publishing Entirely

- **Pros**: Simplest approach. No event publisher, handlers, or subscription infrastructure needed.
- **Cons**: Side effects like sending notifications on order completion, recording audit logs, and cache invalidation must be handled without domain events, creating direct inter-Aggregate dependencies. Loose coupling and the eventual consistency mechanism are forfeited.

## Related Information

- Commits: e48330b3 (BREAKING CHANGE), 2731059d, 774ff4dd, bad1d541
