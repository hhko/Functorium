---
title: "Functorium Samples"
---

## Why Samples Are Needed

Guides explain individual patterns, and tutorials teach concepts incrementally. However, in real projects you face a different question:

> "Given business requirements, where do you start and in what order do you design and implement a DDD domain model?"

Guides and tutorials alone cannot answer this question. A guide tells you "how to create a Value Object," but it does not cover "should you use a Value Object for this business rule, or is a different strategy more appropriate?" Tutorials build concepts step by step, but they do not show the process of combining multiple patterns to complete an entire domain in practice.

## The Role of Samples

Samples follow a **complete design-to-implementation journey** from business requirements to a finished domain model in a single example. Rather than showing the "how" of individual patterns, they demonstrate the "when" and "why" of combining patterns.

## Methodology Overview

All samples follow a common 5-step flow:

1. **Define business requirements** — Describe rules and scenarios in the domain expert's language. No implementation technology is mentioned.
2. **Classify invariants and make type design decisions** — Classify rules as invariants and select a type strategy for each category.
3. **Code design** — Map design decisions to C#/Functorium implementation patterns.
4. **Implementation** — Write domain model code following the patterns.
5. **Verification** — Verify all business rules with unit tests.

## Expected Benefits

- **Design perspective** — Invalid states are blocked at compile time. "A contact without any contact method" or "a verified email reverting to unverified" are made impossible by the type system.
- **Implementation perspective** — Build the domain with consistent patterns. Value Object, Entity, Aggregate Root, Specification, and Domain Service each have clear roles and structures.
- **Testing perspective** — Verify systematically based on invariants. Business rules and test cases map 1:1.
- **Maintenance perspective** — When requirements change, the compiler guides the impact scope. Type changes propagate across all usage sites, ensuring nothing is missed.

## Sample List

| Sample | Domain | Key Patterns |
|--------|--------|-------------|
| [Designing with Types](./designing-with-types/) | Contact Management | Value Object, Discriminated Union, State Machine, Aggregate Root, Domain Service, Specification |
| [E-Commerce DDD Layered Architecture](./ecommerce-ddd/) | E-Commerce Order Processing | Domain Layer: Aggregate Root, Entity, Value Object, Specification, Domain Service, Domain Event / Application Layer: CQRS, Apply Pattern, Port/Adapter, FinT LINQ |
| [AI Model Governance](./ai-model-governance/) | EU AI Act-based AI Model Governance | Domain + Application + Adapter Full-Stack DDD, Advanced IO Features (Timeout, Retry, Fork, Bracket), OpenTelemetry 3-Pillar Observability |
