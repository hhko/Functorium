---
title: "ADR-0003: Domain - CQRS Read/Write Separation"
status: "accepted"
date: 2026-03-18
---

## Context and Problem

Consider a product listing API. The screen only needs three columns -- name, price, and category -- yet an EF Core query loads the entire Aggregate with change tracking enabled. Even with `AsNoTracking`, the overhead of mapping the full domain model remains. Conversely, when creating an order, the Aggregate consistency boundary must be maintained and domain events must be published when OrderItems are added, but Dapper's raw SQL supports neither change tracking nor automatic event collection.

Since reads and writes require optimization in diametrically opposite directions, satisfying both with a single technology inevitably forces one side to compromise. The CQRS pattern should be applied, but a clear standard is needed for which layer defines the write port (IRepository) and read port (IQueryPort), and which implementation technology pairs with each.

## Considered Options

1. Command=IRepository(Domain-defined)+EF Core, Query=IQueryPort(Application-defined)+Dapper
2. EF Core only
3. Dapper only
4. No CQRS (status quo)

## Decision

**Chosen option: "Command=IRepository(Domain-defined)+EF Core, Query=IQueryPort(Application-defined)+Dapper"**. On the write side, EF Core's change tracking protects the Aggregate consistency boundary and automatically collects domain events at `SaveChanges` time. On the read side, Dapper's raw SQL projects only the needed columns, eliminating unnecessary mapping and change tracking. This achieves both performance and consistency by applying the optimal tool to each side.

### Consequences

- <span class="adr-good">Good</span>, because when creating an order, EF Core's change tracking ensures consistency between the Aggregate root and child entities, and automatically collects and publishes domain events at `SaveChanges` time.
- <span class="adr-good">Good</span>, because product listing queries use Dapper to execute projections at the `SELECT Name, Price, Category` level, eliminating unnecessary mapping and memory allocation compared to loading the full Aggregate.
- <span class="adr-good">Good</span>, because IRepository is defined in the Domain layer (Aggregate lifecycle management) and IQueryPort in the Application layer (use case query requirements), so each port's dependency direction aligns with its layer's responsibilities.
- <span class="adr-bad">Bad</span>, because the team must be proficient in both EF Core mapping configuration and Dapper SQL queries, resulting in a dual technology stack.
- <span class="adr-bad">Bad</span>, because when columns of the same entity change, both EF Core's Fluent API mapping and Dapper's SQL queries must be updated separately, risking synchronization misses.

### Confirmation

- Verify that Command handlers persist through EF Core via `IRepository<T>`.
- Verify that Query handlers query through Dapper via `IQueryPort`.
- Verify through architecture tests that IRepository resides in the Domain project and IQueryPort in the Application project.

## Pros and Cons of the Options

### Command=IRepository+EF Core, Query=IQueryPort+Dapper

- <span class="adr-good">Good</span>, because the write side uses EF Core's change tracking and event collection while the read side uses Dapper's raw SQL projections, so the optimal technology for each side does not interfere with the other.
- <span class="adr-good">Good</span>, because IRepository resides in the Domain layer and IQueryPort in the Application layer, so read optimizations do not pollute the domain model.
- <span class="adr-bad">Bad</span>, because the team must be proficient in both EF Core (migrations, Fluent API) and Dapper (raw SQL, parameter mapping), resulting in dual operational costs.

### EF Core Only

- <span class="adr-good">Good</span>, because a single technology unifies the stack, speeding up team onboarding and reducing learning costs.
- <span class="adr-good">Good</span>, because change tracking, migrations, and domain event collection are naturally integrated within a single DbContext.
- <span class="adr-bad">Bad</span>, because even read-only queries like product listings activate the ChangeTracker, consuming unnecessary memory and CPU.
- <span class="adr-bad">Bad</span>, because LINQ expressiveness is insufficient for complex projections involving multi-table joins or window functions, ultimately requiring `FromSqlRaw` workarounds that diminish EF Core's benefits.

### Dapper Only

- <span class="adr-good">Good</span>, because writing SQL directly achieves maximum performance at the database level with full control over query execution plans.
- <span class="adr-bad">Bad</span>, because without change tracking, when saving an Order and its OrderItems together, the developer must manually ensure Aggregate consistency boundary through raw SQL.
- <span class="adr-bad">Bad</span>, because without a domain event collection mechanism, event publishing code must be manually written in every Repository method instead of using `SaveChanges` interceptor automation.

### No CQRS (Status Quo)

- <span class="adr-good">Good</span>, because reads and writes share the same model, keeping the structure simple and easy to understand.
- <span class="adr-bad">Bad</span>, because applying read optimizations (projections, index hints) distorts the domain model to fit query requirements, and maintaining write optimizations (change tracking, events) sacrifices read performance, forcing compromises on both sides.

## Related Information

- Related commit: `074e2475` feat(books/cqrs): Add CQRS Repository and Query pattern learning Book
- Related commit: `6b027c31` refactor(cqrs-repository): Sync Part4 Usecase code to latest Mediator/IQueryPort API
- Related docs: `Docs.Site/src/content/docs/tutorials/cqrs-repository/`
