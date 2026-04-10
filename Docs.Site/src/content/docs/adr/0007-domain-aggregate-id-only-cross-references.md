---
title: "ADR-0007: Domain - ID-Only Cross-Aggregate References"
status: "accepted"
date: 2026-03-22
---

## Context and Problem

Suppose the `Order` Aggregate holds a direct reference to a `Customer` object (`public Customer Customer { get; }`). When querying an order, EF Core loads `Customer` as well, and then cascades to load `Address`, `LoyaltyProgram`, and other objects referenced by `Customer`. What was meant to be reading a single order results in a 4-table join.

The problem goes beyond performance. When saving an order, EF Core's change tracking includes the `Customer` object, potentially capturing unintended customer information changes in the same transaction. If two users concurrently modify different orders for the same customer, concurrency conflicts arise through the `Customer` object reference. Furthermore, since `Order` has a direct compile-time dependency on the `Customer` type, the Aggregate boundary cannot be cleanly split when extracting the customer service into a separate microservice in the future.

## Considered Options

1. ID (value type) only references + domain events
2. Direct object references
3. Lazy Loading
4. Saga pattern

## Decision

**Chosen option: "ID (value type) only references + domain events"**, to physically enforce Aggregate transaction boundaries and structurally prevent boundary violations. Since `Order` holds only a `CustomerId` value type instead of a `Customer` object, cascading loads, unintended change tracking, and concurrency conflicts become structurally impossible.

When cross-Aggregate consistency is needed (e.g., accumulating customer loyalty points after order creation), it is handled through domain events with eventual consistency. Strongly-typed IDs (`OrderId`, `CustomerId`) are distinct from primitive types like `string` or `Ulid`, so accidentally assigning a `ProductId` to an `Order` triggers a compile error.

### Consequences

- <span class="adr-good">Good</span>, because saving an `Order` does not include `Customer` change tracking, physically isolating the transaction boundary to the Aggregate unit.
- <span class="adr-good">Good</span>, because passing a `ProductId` where a `CustomerId` is expected triggers a compile error, blocking incorrect ID assignment before runtime.
- <span class="adr-good">Good</span>, because loading an `Order` does not trigger joins to `Customer`, `Address`, and other related tables, confining queries to single-table scope.
- <span class="adr-good">Good</span>, because inter-Aggregate dependencies are limited to ID values, enabling clean boundary separation when extracting the customer service into a separate microservice without code changes.
- <span class="adr-bad">Bad</span>, because "displaying order and customer information together" requires querying the order by `OrderId` then making a separate query with `CustomerId`, or constructing a Read Model.
- <span class="adr-bad">Bad</span>, because adopting eventual consistency instead of strong consistency requires a business judgment on whether temporary inconsistencies during event processing delays are acceptable.

### Confirmation

- Verify through architecture rule tests that Aggregate Roots do not directly reference entities of other Aggregates.
- Verify during code reviews that cross-Aggregate references consist solely of ID value types.

## Pros and Cons of the Options

### ID (Value Type) Only References + Domain Events

- <span class="adr-good">Good</span>, because each Aggregate only locks its own table, minimizing the scope of concurrency conflicts.
- <span class="adr-good">Good</span>, because strongly-typed IDs like `OrderId` and `CustomerId` block primitive type confusion at compile time.
- <span class="adr-good">Good</span>, because inter-Aggregate communication occurs through domain events, enabling loose coupling and independent deployment.
- <span class="adr-bad">Bad</span>, because cross-Aggregate reads like "display customer name on order list" require separate queries or denormalized Read Models, increasing read complexity.

### Direct Object References

- <span class="adr-good">Good</span>, because navigation properties like `order.Customer.Name` provide intuitive access to related data.
- <span class="adr-bad">Bad</span>, because when saving `Order`, EF Core tracks `Customer` changes as well, potentially including unintended data modifications in the same transaction, and concurrent modifications of different orders cause concurrency conflicts on the `Customer` row.
- <span class="adr-bad">Bad</span>, because cascading loads like `Order` -> `Customer` -> `Address` -> `Region` result in multi-table joins for a simple order query.
- <span class="adr-bad">Bad</span>, because `Order` has a compile-time dependency on the `Customer` type, preventing code separation at the Aggregate boundary when extracting the customer domain into a separate service.

### Lazy Loading

- <span class="adr-good">Good</span>, because related Aggregates are not loaded at initial load time, with no immediate performance cost.
- <span class="adr-bad">Bad</span>, because iterating over 100 orders and accessing each `order.Customer` triggers 100 individual SELECT queries, exhibiting the N+1 problem.
- <span class="adr-bad">Bad</span>, because Lazy Loading proxies inject EF Core dependency into domain models, compromising domain layer purity.
- <span class="adr-bad">Bad</span>, because lazily loaded `Customer` objects are still change-tracked in the same DbContext, leaving the transaction boundary violation problem unresolved.

### Saga Pattern

- <span class="adr-good">Good</span>, because long-running business processes spanning different databases can be managed with compensating transactions.
- <span class="adr-bad">Bad</span>, because applying Saga to the problem of decoupling Aggregate references within the same database introduces excessive complexity -- message brokers, Saga orchestrators, and other infrastructure.
- <span class="adr-bad">Bad</span>, because compensating logic and state machines must be designed and tested for each step, significantly increasing implementation cost compared to the ID-only reference + domain event approach.

## Related Information

- Related commit: `71272343`
- Related guide: `Docs.Site/src/content/docs/guides/domain/06a-aggregate-design`
- Reference: Vaughn Vernon, Implementing Domain-Driven Design -- Chapter 10, Aggregates
