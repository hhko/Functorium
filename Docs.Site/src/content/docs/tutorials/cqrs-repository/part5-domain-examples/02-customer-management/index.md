---
title: "Customer Management"
---
## Overview

Duplicate customer registration with the same email must be prevented. How do you implement email duplicate checking? Loading the entire customer list and comparing manually is inefficient, and code becomes complex as search conditions increase.

This chapter implements **Specification pattern** to encapsulate search conditions through the Customer domain and validates uniqueness using Repository's `Exists()` method. It also covers tracking creation/modification timestamps with the IAuditable interface and the dynamic filter builder pattern.

---

## Learning Objectives

After completing this chapter, you will be able to:

1. Encapsulate search conditions with the **Specification pattern**
2. Leverage **Specification composition** (And, Or, Not operators)
3. Validate uniqueness with **Repository.Exists()**
4. Implement a **dynamic filter builder** (All + conditional chaining)
5. Implement an **InMemoryQueryBase**-based Query Adapter

---

## Core Concepts

### Specification Pattern

Encapsulating search conditions as objects makes reuse and composition easy. See how to create individual Specifications and compose them with `&`, `|` operators.

```csharp
// Individual Specification
var emailSpec = new CustomerEmailSpec("kim@example.com");
var nameSpec = new CustomerNameSpec("Kim");

// Composition: & operator for And
var composedSpec = nameSpec & emailSpec;

// Dynamic builder: conditional addition with All as seed
var filter = Specification<Customer>.All;
if (!string.IsNullOrEmpty(nameFilter))
    filter = filter & new CustomerNameSpec(nameFilter);
```

`Specification<T>.All` is the identity element for And operations, so when no conditions are added, it returns all data.

### Using Specification with Repository

For checking "does data matching this condition exist?" like email duplicate checking, use `Exists()`. It's performant because it doesn't load entire Aggregates.

```csharp
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    FinT<IO, bool> Exists(Specification<Customer> spec);
}

// Email duplicate check
var exists = await repository
    .Exists(new CustomerEmailSpec("kim@example.com"))
    .Run().RunAsync();
```

---

## Project Description

### File Structure

Check each file's role in the Specification pattern.

| File | Role |
|------|------|
| `CustomerId.cs` | Ulid-based customer identifier |
| `Customer.cs` | Customer Aggregate Root (IAuditable) |
| `CustomerDto.cs` | Query-side DTO |
| `CustomerEmailSpec.cs` | Email Specification (case-insensitive) |
| `CustomerNameSpec.cs` | Name Specification (partial match) |
| `ICustomerRepository.cs` | Repository + Exists(Specification) |
| `InMemoryCustomerRepository.cs` | InMemory Repository implementation |
| `InMemoryCustomerQuery.cs` | InMemory Query Adapter |

---

## Summary at a Glance

A summary of the Specification pattern elements used in this example.

| Concept | Implementation |
|---------|---------------|
| Specification | `CustomerEmailSpec`, `CustomerNameSpec` |
| Composition | `spec1 & spec2` (And), `spec1 \| spec2` (Or) |
| Identity element | `Specification<Customer>.All` |
| Exists validation | `ICustomerRepository.Exists(spec)` |
| Query Adapter | `InMemoryCustomerQuery : InMemoryQueryBase<Customer, CustomerDto>` |
| Audit tracking | `IAuditable` -> `CreatedAt`, `UpdatedAt` |

---

## FAQ

### Q1: Why not implement Specification as ExpressionSpecification?
**A**: This example uses an InMemory environment, so only `IsSatisfiedBy()` is needed. When integrating with EF Core/Dapper, `ExpressionSpecification<T>` is used to support automatic SQL translation.

### Q2: Why use All as the seed in dynamic filters?
**A**: `Specification<T>.All` is the identity element for And operations. Since `All & X = X`, when no conditions are added, all data is returned. This pattern cleanly handles nullable filter parameters.

### Q3: Why provide Exists() as a separate method?
**A**: The intent is clearer than `GetById()` followed by a null check, and it's performant because entire Aggregates aren't loaded. In actual DB environments, it translates to lightweight queries like `SELECT COUNT(1)`.

---

Customer management and the Specification pattern are complete. Next is inventory management. If you delete a product, do the related order histories disappear too? The next chapter explores preserving data while marking it as deleted with the soft delete pattern.

-> [Chapter 3: Inventory Management](../03-Inventory-Management/)
