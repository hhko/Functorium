---
title: "DTO Separation"
---
## Overview

What happens if you share a single `ProductDto` between Command and Query? Writes don't need `Id` and `CreatedAt`, and list queries don't need heavy fields like `Description`. Cramming all purposes into a single DTO means changes on one side unnecessarily affect the other. This chapter covers designing different DTOs for different purposes on the same domain entity.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Explain the role differences between Command DTOs and Query DTOs
2. Understand why input (Request) and output (Response) DTOs should be separated
3. Apply design criteria for List and Detail Query DTOs
4. Explain why domain entities should not be returned directly

---

## Core Concepts

### "Why Is This Needed?" -- When a Single DTO Handles Everything

```csharp
// Sharing a single DTO between Command and Query?
public record ProductDto(
    string Id,              // Unnecessary for Command input (server generates)
    string Name,
    decimal Price,
    string Description,     // Unnecessary for list queries (heavy field)
    string Category,
    int Stock,
    DateTime CreatedAt      // Unnecessary for Command input (server generates)
);
```

Command input must send `Id` and `CreatedAt` as empty values, and list queries transmit `Description` every time. When purposes differ, DTOs should differ too.

### DTO Classification System

Separating DTOs by purpose means each includes only the necessary fields.

| Classification | DTO | Role |
|---------------|-----|------|
| Command Input | `CreateProductRequest` | Client -> Server (write data) |
| Command Output | `CreateProductResponse` | Server -> Client (creation confirmation) |
| Query List | `ProductListDto` | List view (minimal fields) |
| Query Detail | `ProductDetailDto` | Detail view (all fields) |

### Command DTO vs Query DTO

The two DTO groups differ in data flow direction and purpose.

| Aspect | Command DTO | Query DTO |
|--------|------------|-----------|
| Direction | Client -> Server / Server -> Client | Server -> Client |
| Purpose | Carries data needed for state changes | Read-optimized projection |
| Examples | CreateProductRequest, CreateProductResponse | ProductListDto, ProductDetailDto |
| Fields | Only fields needed for writing | Only fields needed for reading |

### List DTO vs Detail DTO

- **ProductListDto** includes only Name, Price, and Category. Heavy fields like Description are excluded to reduce network cost.
- **ProductDetailDto** includes all fields. Used for single product detail views.

---

## Project Description

### Product (Domain Entity)

An Aggregate Root containing business logic (ChangePrice, DecreaseStock). This entity is not returned directly to clients.

### CreateProductRequest / CreateProductResponse

Command DTOs. Request excludes server-generated Id and CreatedAt, while Response includes only the minimum fields needed for creation confirmation.

### ProductListDto / ProductDetailDto

Query DTOs. Different projections are used for list and detail queries on the same Product.

---

## Summary at a Glance

| Item | Description |
|------|-------------|
| Command Input DTO | Only write-needed fields (excludes server-generated fields) |
| Command Output DTO | Minimum creation confirmation info (Id, Name, CreatedAt) |
| Query List DTO | Minimum fields for lists (excludes heavy fields) |
| Query Detail DTO | All fields needed for detail view |
| Principle | Do not return domain entities directly |

---

## FAQ

### Q1: Can't we just use a single unified DTO?
**A**: You can, but it's inefficient. Transmitting large fields like Description on every list query increases network costs, and including Id in Command input requires clients to send unnecessary values.

### Q2: Won't too many DTOs be hard to manage?
**A**: DTOs are simple records, so maintenance burden is low. In contrast, using one large DTO for multiple purposes creates worse problems from change side effects.

### Q3: Why shouldn't domain entities be returned directly?
**A**: (1) Domain logic (ChangePrice, DecreaseStock) gets exposed to clients, (2) ORM proxy object lazy loading issues occur, and (3) read optimization (SELECT only needed columns) becomes impossible.

---

We've separated Command DTOs and Query DTOs. But what happens if you return 100,000 products at once? In the next chapter, we'll look at controlling large data through pagination and sorting.

-> [Chapter 3: Pagination and Sorting](../03-Pagination-And-Sorting/)
