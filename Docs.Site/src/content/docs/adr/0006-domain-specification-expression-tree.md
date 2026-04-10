---
title: "ADR-0006: Domain - Specification Pattern with Expression Tree-Based Query Translation"
status: "accepted"
date: 2026-03-20
---

## Context and Problem

Suppose there is a business rule: "customers who are active and Gold grade or above." This condition is implemented in the domain model's `Customer.IsEligibleForPromotion()` method, and also separately written as `c => c.IsActive && c.Grade >= Grade.Gold` in the Repository's LINQ Where clause. When the grade threshold later changes to Silver, the domain method is updated but the LINQ query is missed, and a bug is discovered in production where the promotion target query and domain validation return different customer sets.

When the same business rule is scattered across domain code and query code, failing to update one side during rule changes leads to silent inconsistencies. Beyond simple conditions like "active AND Gold or above," a structure is needed to declaratively compose complex conditions like "active AND (Gold or above OR VIP)."

## Considered Options

1. ExpressionSpecification\<T\> + PropertyMap bridge
2. Direct LINQ Where clause writing
3. Dynamic LINQ library
4. Query Object pattern

## Decision

**Chosen option: "ExpressionSpecification\<T\> + PropertyMap bridge"**, to establish a Single Source of Truth for business rules. A single Specification class defines the rule as an Expression Tree; for domain validation, `IsSatisfiedBy()` evaluates it in-memory, while for Repository queries, EF Core translates the same Expression into SQL. When a rule changes, modifying the Specification in one place instantly reflects in both directions.

The `&` (AND), `|` (OR), and `!` (NOT) operator overloads enable declarative composition like `ActiveSpec & (GoldOrHigherSpec | VipSpec)`, and the PropertyMap bridge absorbs differences between domain property names and DB column names, preserving domain model purity.

### Consequences

- <span class="adr-good">Good</span>, because the same Specification is reused across domain validation, query filters, and API response filters, so changing a rule requires modification in only one place.
- <span class="adr-good">Good</span>, because compositions like `ActiveSpec & GoldOrHigherSpec | !SuspendedSpec` express business intent as readable code.
- <span class="adr-good">Good</span>, because the PropertyMap bridge declaratively resolves naming differences between the domain's `Grade` property and the DB's `customer_grade` column in a single location.
- <span class="adr-bad">Bad</span>, because the `ParameterReplacer` and `ExpressionVisitor` combination logic inside Expression Trees is harder to debug than regular code, and incorrect Expression composition manifests as runtime `InvalidOperationException`.
- <span class="adr-bad">Bad</span>, because a separate PropertyMap must be written for every Specification where the domain model and persistence model have different property names.

### Confirmation

- Verify through unit tests that Specification composition (`&`, `|`, `!`) produces correct Expressions.
- Verify through integration tests that DB query translation via PropertyMap correctly converts to actual SQL.

## Pros and Cons of the Options

### ExpressionSpecification\<T\> + PropertyMap Bridge

- <span class="adr-good">Good</span>, because modifying a single Specification class instantly reflects in both domain validation and DB queries when a rule changes.
- <span class="adr-good">Good</span>, because EF Core directly translates the Expression Tree into a SQL WHERE clause, filtering at the DB level without in-memory filtering.
- <span class="adr-good">Good</span>, because operator overloading like `spec1 & spec2 | !spec3` expresses business rule composition in near-natural language.
- <span class="adr-good">Good</span>, because PropertyMap declaratively resolves property name and type differences between domain models and persistence models outside the Specification.
- <span class="adr-bad">Bad</span>, because the `ParameterExpression` replacement logic during `AndSpecification`, `OrSpecification`, and other Expression combinations is complex, resulting in high initial framework implementation cost.
- <span class="adr-bad">Bad</span>, because a PropertyMap must be additionally defined for every Specification where domain properties and DB columns differ, creating boilerplate.

### Direct LINQ Where Clause Writing

- <span class="adr-good">Good</span>, because `.Where(c => c.IsActive && c.Grade >= Grade.Gold)` can be written directly without additional abstractions, with zero learning cost.
- <span class="adr-bad">Bad</span>, because the same `IsActive && Grade >= Gold` condition exists separately in both the domain method and Repository LINQ, risking silent inconsistency when one side is missed during updates.
- <span class="adr-bad">Bad</span>, because when rules are scattered across multiple locations, the impact scope of changes can only be determined through full-text code search.
- <span class="adr-bad">Bad</span>, because composing complex conditions requires manual Where clause assembly each time, with no reusable structure.

### Dynamic LINQ Library

- <span class="adr-good">Good</span>, because string-based dynamic queries like `"Age > 18 AND IsActive"` can be constructed at runtime, offering flexibility.
- <span class="adr-bad">Bad</span>, because renaming the `"Age"` property to `"UserAge"` compiles successfully but throws a runtime `ParseException`, lacking type safety.
- <span class="adr-bad">Bad</span>, because typos like `"Actve"` in strings only surface as runtime errors in specific branches, delaying discovery.
- <span class="adr-bad">Bad</span>, because string queries are separate from the domain layer's business rules, leaving the rule duplication problem unresolved.

### Query Object Pattern

- <span class="adr-good">Good</span>, because query logic is encapsulated in objects like `ActiveCustomerQuery`, enabling reuse across Repositories.
- <span class="adr-bad">Bad</span>, because Query Objects do not directly generate Expression Trees, so an additional translation layer must be implemented to integrate with EF Core's SQL translation.
- <span class="adr-bad">Bad</span>, because Query Objects are DB-query-only, so they cannot be used for in-memory validation in the domain layer, leaving the rule duplication problem intact.

## Related Information

- Related commit: `f1dec480`
- Related tutorial: `Docs.Site/src/content/docs/tutorials/specification-pattern/`
- Reference: Eric Evans, Domain-Driven Design -- Chapter 9, Specification pattern
