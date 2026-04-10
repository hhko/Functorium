---
title: "ADR-0008: Domain - Domain Service Pure Function vs Repository Dual Strategy"
status: "accepted"
date: 2026-03-24
---

## Context and Problem

When implementing cross-Aggregate business logic as Domain Services, cases requiring external data access and cases not requiring it are fundamentally different in nature.

Logic like "apply customer grade-based discount rate to order amount" can be purely computed by taking `Order` and `CustomerGrade` as parameters. Given the same inputs, the result is always the same, and unit tests can be written without mocks. On the other hand, "check customer email uniqueness" requires querying hundreds of thousands of existing customer emails from the database. Pre-querying the entire email list in the Application Service and passing it to the Domain Service would maintain purity, but this loads massive unnecessary data into memory. When this pattern accumulates, the Application Service bloats into a "data retrieval hub" and the domain model degenerates into an anemic model.

## Considered Options

1. Pure functions by default + Evans Ch.9 Repository pattern transition
2. Always pure functions (pass all data as parameters)
3. Always Repository-dependent
4. Handle logic in Application Service

## Decision

**Chosen option: "Pure functions by default + Evans Ch.9 Repository pattern transition"**, to use testability of pure functions as the default while allowing Repository dependency only when bulk data retrieval is unavoidable, ensuring practicality. The transition criterion is clear: it is determined by the single question "Is the data too large to pass as parameters?"

- **Pure Domain Service**: `static` class, all inputs as parameters, returns `Fin<T>`. Used for computation-centric logic such as discount rate calculation and amount validation. Tested with inputs and outputs only, without mocks.
- **Repository Domain Service**: Receives a Repository interface via constructor injection, returns `FinT<IO, T>`. Used when bulk data retrieval is necessary, such as email uniqueness checks and inventory availability verification.

### Consequences

- <span class="adr-good">Good</span>, because computation-centric Domain Services like discount rate calculation and amount validation remain as `static` pure functions, testable with inputs and outputs only without mocks.
- <span class="adr-good">Good</span>, because when bulk data retrieval is needed, such as email uniqueness checks, the Repository queries the DB directly instead of loading hundreds of thousands of records into memory, ensuring efficiency.
- <span class="adr-good">Good</span>, because the single criterion "Is the data too large to pass as parameters?" clearly determines which style to choose.
- <span class="adr-bad">Bad</span>, because two styles coexist -- pure functions and Repository-dependent -- requiring repeated design judgments during code reviews like "this logic could be a pure function, so why was a Repository injected?"

### Confirmation

- Verify through architecture rule tests that pure Domain Services are implemented as `static` without external dependencies.
- Verify that Repository Domain Services depend only on Repository interfaces in the domain layer.

## Pros and Cons of the Options

### Pure Functions by Default + Evans Ch.9 Repository Pattern Transition

- <span class="adr-good">Good</span>, because computation logic is tested as pure functions without mocks, while bulk query logic ensures DB efficiency through Repositories, capturing the best of both approaches.
- <span class="adr-good">Good</span>, because the single criterion "Can this data be passed as parameters?" eliminates ambiguity in style selection.
- <span class="adr-good">Good</span>, because it precisely aligns with "coexistence of pure Domain Services and Repository-accessing Domain Services" as described in Eric Evans's DDD (Chapter 9).
- <span class="adr-bad">Bad</span>, because two styles coexist in the same domain layer, requiring documentation of selection guidelines for new team members.

### Always Pure Functions (Pass All Data as Parameters)

- <span class="adr-good">Good</span>, because all Domain Services are unified as `static` pure functions, eliminating the need for style selection decisions.
- <span class="adr-good">Good</span>, because no Repository mocks are needed in tests, simplifying test code to pure input-output verification.
- <span class="adr-bad">Bad</span>, because email uniqueness checks require the Application Service to pre-query the entire customer email list (hundreds of thousands of records) and pass it as a parameter, resulting in N+1 queries and memory waste.
- <span class="adr-bad">Bad</span>, because the Application Service bloats as a "role of pre-querying all data to pass to Domain Services," blurring business logic boundaries.

### Always Repository-Dependent

- <span class="adr-good">Good</span>, because all Domain Services are unified with constructor injection + `FinT<IO, T>` return, guaranteeing structural consistency.
- <span class="adr-good">Good</span>, because needed data is queried directly from the DB at the needed time, avoiding unnecessary pre-loading.
- <span class="adr-bad">Bad</span>, because even pure computations like `CalculateDiscount(order, grade)` require mocking unused Repositories, polluting tests with unnecessary setup like "when the Repository returns empty results..."
- <span class="adr-bad">Bad</span>, because all Domain Services having external dependencies degrades unit test isolation and execution speed.

### Handle Logic in Application Service

- <span class="adr-good">Good</span>, because processing directly in the Application Service without a Domain Service layer reduces one architecture layer.
- <span class="adr-bad">Bad</span>, because core business logic like discount rate calculation resides in the Application layer, leaving the domain model as a mere data structure -- an Anemic Domain Model.
- <span class="adr-bad">Bad</span>, because identical discount rate calculation logic is separately implemented in "create order" and "modify order" use cases, requiring both to be updated simultaneously when rules change.

## Related Information

- Related commits: `2731059d`, `d446fcfa`
- Related guide: `Docs.Site/src/content/docs/guides/domain/`
- Reference: Eric Evans, Domain-Driven Design -- Chapter 9, Services
