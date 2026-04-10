---
title: "ADR-0016: Adapter - Persistence Class Suffix Naming Pattern"
status: "accepted"
date: 2026-03-27
---

## Context and Problem

When trying to modify the Repository for the Product Aggregate, opening `Ctrl+P` (Go to File) in the IDE and typing "Product" showed `EfCoreProductRepository`, `DapperProductQueryAdapter`, and `InMemoryProductRepository` scattered at positions E, D, and I respectively. Finding files for the same domain subject required remembering each technology prefix, and autocomplete demanded typing "EfCore..." first to reach the desired class. The developer's thought flow is "find Product's Repository," but the naming forced "find Product among EfCore ones."

The project had 37 persistence classes with a mix of prefix and suffix patterns, resulting in an inconsistent file navigation experience. A unified naming convention was needed to support domain-subject-centric navigation.

## Considered Options

- **Option 1**: Prefix pattern (`EfCoreProductRepository`, `DapperProductQueryAdapter`)
- **Option 2**: Suffix pattern (`ProductRepositoryEfCore`, `ProductQueryAdapterDapper`)
- **Option 3**: Technology-based folder separation (`EfCore/ProductRepository`, `Dapper/ProductQueryAdapter`)

## Decision

**Option 2: Adopt the `{Subject}{Role}{Variant}` suffix pattern, combined with Aggregate/CQRS folder structure.**

When navigating code, what developers think of first is the domain subject (Product), not the technology (EfCore). Naming should follow this thought flow.

- **Subject**: Domain subject (e.g., `Product`, `Order`)
- **Role**: Role (e.g., `Repository`, `QueryAdapter`, `Configuration`)
- **Variant**: Technology variant (e.g., `EfCore`, `Dapper`, `InMemory`)

The folder structure is divided by Aggregate and follows the CQRS pattern, separating Command/Query. Typing `Product` now lists `ProductRepositoryEfCore`, `ProductQueryAdapterDapper`, and `ProductConfigurationEfCore` consecutively, providing an at-a-glance view of all persistence implementations for that Aggregate.

### Consequences

- **Positive**: Typing "Product" in the IDE shows Repository, QueryAdapter, and Configuration adjacent in alphabetical order. After bulk-renaming all 37 classes, domain-subject-centric navigation became immediately available in Go to File and autocomplete. The time for new team members to grasp all persistence implementations for a specific Aggregate was reduced.
- **Negative**: A large-scale rename operation was required across all 37 classes along with updating all references, tests, and documentation. To see "all implementations using EfCore technology" at a glance, a separate search by technology name is needed.

### Confirmation

- Verify through naming convention tests that all persistence classes follow the `{Subject}{Role}{Variant}` pattern.
- Verify that searching by domain subject name in the IDE's Go to File groups related files together.

## Pros and Cons of the Options

### Option 1: Prefix Pattern

- **Pros**: Typing "EfCore" lists all EfCore-based implementations, providing an at-a-glance view of the impact scope during technology replacement.
- **Cons**: Typing "Product" scatters `DapperProductQueryAdapter` (D), `EfCoreProductRepository` (E), and `InMemoryProductRepository` (I) alphabetically. Repository, Configuration, and QueryAdapter for the same Aggregate are dispersed, making domain-context navigation inconvenient. Autocomplete requires typing the technology name first, misaligning with the developer's thought flow ("find Product's Repository").

### Option 2: Suffix Pattern

- **Pros**: Typing "Product" lists `ProductConfigurationEfCore`, `ProductQueryAdapterDapper`, and `ProductRepositoryEfCore` consecutively. Aligns with the natural developer thought flow of entering business terms first in autocomplete. Combined with Aggregate folders, the file explorer reveals both business boundaries and technical implementations simultaneously.
- **Cons**: Viewing "all implementations of EfCore technology" requires a separate technology-name search. Suffixes can become long (e.g., `ProductRepositoryEfCore`).

### Option 3: Technology-Based Folder Separation

- **Pros**: `EfCore/` and `Dapper/` folders physically separate technology dependencies, enabling folder-level work during technology replacement.
- **Cons**: The Product's Command Repository (`EfCore/ProductRepository`) and Query Adapter (`Dapper/ProductQueryAdapter`) reside in different folders, requiring navigation across multiple folders to understand a single Aggregate. CQRS Command/Query separation and technology-based folder separation create a dual hierarchy, deepening folder depth and increasing cognitive load.

## Related Information

- Commits: a6a70539 (37-class rename), f32318d0
