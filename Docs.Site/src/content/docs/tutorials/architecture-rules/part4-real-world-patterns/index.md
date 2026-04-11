---
title: "Real-World Patterns"
---

## Overview

The team adopted a DDD architecture. Entities must be `sealed`, Command handlers must implement specific interfaces, and the domain layer must not reference the adapter layer. This was documented, but as the project grows, the rules gradually begin to crumble.

> **Architecture documents can go unread, but failing tests cannot be ignored. Let's apply the verification techniques learned in Parts 1--3 to real-world layer architectures.**

In this part, you will learn how to enforce architecture patterns used in real projects through tests. This covers DDD tactical patterns, Command/Query patterns, Port & Adapter patterns, and layer dependency rules.

## Learning Objectives

### Core Learning Goals
1. **Comprehensive domain layer rule application**
   - Verify structural rules for Entity, Value Object, Domain Event, and Domain Service
   - Unify immutability, inheritance, and naming rules into a single test suite
2. **Enforcing application layer patterns**
   - Verify Command/Query separation rules and Usecase handler signatures
   - Ensure structural consistency of DTOs and nested class patterns
3. **Verifying Port & Adapter rules**
   - Enforce the relationship between port interfaces and adapter implementations
   - Automatically verify that adapters implement only the correct ports
4. **Automated layer dependency direction verification**
   - Combine ArchUnitNET native API with Functorium API
   - Test dependency rules in the Domain -> Application -> Adapter direction

### What You Will Verify Through Practice
- Verify that Entity classes are `public sealed` and inherit the correct base class
- Confirm that Command handlers have exactly one `Execute` method
- Automatically verify that the domain layer does not reference the adapter layer

## Chapter Structure

| Chapter | Title | Key Content |
|---------|-------|-------------|
| [Chapter 1](01-Domain-Layer-Rules/) | Domain Layer Rules | Entity, Value Object, Domain Event, Domain Service |
| [Chapter 2](02-Application-Layer-Rules/) | Application Layer Rules | Command/Query pattern, nested classes, DTOs |
| [Chapter 3](03-Adapter-Layer-Rules/) | Adapter Layer Rules | Port interfaces, adapter implementations, dependency verification |
| [Chapter 4](04-Layer-Dependency-Rules/) | Layer Dependency Rules | Multi-layer dependencies, ArchUnitNET + Functorium combination |

---

In the first chapter, we implement 21 rules for Entity, Value Object, Domain Event, Specification, and Domain Service across 6 categories.

-> [Ch 1: Domain Layer Rules](01-Domain-Layer-Rules/)
