---
title: "CQRS Repository Pattern"
---

**A practical guide to implementing Repository and Query adapters with C# Functorium**

---

## About This Tutorial

Does every new filter for your order list API mean adding yet another Repository method -- `GetByCustomer`, `GetRecent`, `SearchByKeyword`...? Are read-only properties creeping into your domain model, polluting your write logic, and creating a vicious cycle where fixing one thing breaks another?

This tutorial solves that problem with **Command and Query Responsibility Segregation (CQRS)**. Starting from domain entity fundamentals and progressing through Repository patterns, Query adapters, and use-case integration, you will learn every aspect of the CQRS pattern step by step through **22 hands-on projects**.

> **"If you are adding a new Repository method every time a new query condition is needed, that is not design -- it is inertia."**

### Target Audience

| Level | Audience | Recommended Scope |
|-------|----------|-------------------|
| **Beginner** | Developers with basic CRUD and Entity experience | Parts 0--1 |
| **Intermediate** | Developers who understand the Repository pattern and want deeper learning | Parts 2--3 |
| **Advanced** | Developers interested in CQRS architecture design and use-case integration | Parts 4--5 + Appendix |

### Learning Objectives

After completing this tutorial, you will be able to:

1. Design separated Command (IRepository) and Query (IQueryPort) interfaces using the CQRS pattern
2. Compose functional CQRS pipelines with FinT monad composition and Specification-based dynamic search
3. Build a robust CQRS architecture with transaction pipelines and domain event flows

---

### Part 0: Introduction

Start with why CQRS is needed and what problems it solves. Set up your environment and get an overview of the CQRS architecture.

- [0.1 Why CQRS](Part0-Introduction/01-why-this-tutorial.md)
- [0.2 Environment Setup](Part0-Introduction/02-prerequisites-and-setup.md)
- [0.3 CQRS Pattern Overview](Part0-Introduction/03-cqrs-pattern-overview.md)

### Part 1: Domain Entity Foundations

Are two products with the same name the same product? Starting from Entity identity, build the domain modeling foundation for CQRS -- covering Aggregate Root, domain events, and entity interfaces.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Entity and Identity](Part1-Domain-Entity-Foundations/01-Entity-And-Identity/) | Entity\<TId\>, IEntityId, Ulid-based ID |
| 2 | [Aggregate Root](Part1-Domain-Entity-Foundations/02-Aggregate-Root/) | AggregateRoot\<TId\>, domain invariants |
| 3 | [Domain Events](Part1-Domain-Entity-Foundations/03-Domain-Events/) | IDomainEvent, AddDomainEvent(), ClearDomainEvents |
| 4 | [Entity Interfaces](Part1-Domain-Entity-Foundations/04-Entity-Interfaces/) | IAuditable, ISoftDeletable |

### Part 2: Command Side -- Repository Pattern

What interface do you need to persist domain models while guaranteeing their invariants? Progress from IRepository design for Aggregate Root-level write operations through InMemory and EF Core implementations to Unit of Work.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Repository Interface](Part2-Command-Repository/01-Repository-Interface/) | IRepository\<TAggregate, TId\>, 8 CRUD operations, FinT\<IO, T\> |
| 2 | [InMemory Repository](Part2-Command-Repository/02-InMemory-Repository/) | InMemoryRepositoryBase, ConcurrentDictionary |
| 3 | [EF Core Repository](Part2-Command-Repository/03-EfCore-Repository/) | EfCoreRepositoryBase, ToDomain/ToModel |
| 4 | [Unit of Work](Part2-Command-Repository/04-Unit-Of-Work/) | IUnitOfWork, SaveChanges, IUnitOfWorkTransaction |

### Part 3: Query Side -- Read-Only Patterns

Instead of adding a new method every time a query condition changes, handle dynamic searches with a single Specification. Optimize the read-only path through DTO projections and three pagination strategies.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [IQueryPort Interface](Part3-Query-Patterns/01-QueryPort-Interface/) | IQueryPort\<TEntity, TDto\>, Search/SearchByCursor/Stream |
| 2 | [DTO Separation](Part3-Query-Patterns/02-DTO-Separation/) | Command DTO vs Query DTO, projections |
| 3 | [Pagination and Sorting](Part3-Query-Patterns/03-Pagination-And-Sorting/) | PageRequest, CursorPageRequest, SortExpression |
| 4 | [InMemory Query Adapter](Part3-Query-Patterns/04-InMemory-Query-Adapter/) | InMemoryQueryBase, GetProjectedItems |
| 5 | [Dapper Query Adapter](Part3-Query-Patterns/05-Dapper-Query-Adapter/) | DapperQueryBase, SQL generation |

### Part 4: CQRS Use-Case Integration

With Repository and Query adapters ready, it is time to integrate them into use cases. Dispatch Commands/Queries with the Mediator pattern, convert from FinT to FinResponse, wire up domain event flows and transaction pipelines to complete the CQRS architecture.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Command Use Case](Part4-CQRS-Usecase-Integration/01-Command-Usecase/) | ICommandRequest, ICommandUsecase, FinResponse |
| 2 | [Query Use Case](Part4-CQRS-Usecase-Integration/02-Query-Usecase/) | IQueryRequest, IQueryUsecase, IQueryPort integration |
| 3 | [FinT -> FinResponse](Part4-CQRS-Usecase-Integration/03-FinT-To-FinResponse/) | ToFinResponse(), LINQ monadic composition |
| 4 | [Domain Event Flow](Part4-CQRS-Usecase-Integration/04-Domain-Event-Flow/) | IDomainEventCollector, Track, publish |
| 5 | [Transaction Pipeline](Part4-CQRS-Usecase-Integration/05-Transaction-Pipeline/) | Transaction pipeline, Command auto-commit |

### Part 5: Domain-Specific Practical Examples

Apply the CQRS patterns you have learned to real domains. See firsthand the benefits of Command/Query separation in order, customer, inventory, and catalog domains.

| Ch | Topic | Key Learning |
|:---:|-------|-------------|
| 1 | [Order Management](Part5-Domain-Examples/01-Ecommerce-Order-Management/) | Complete order CQRS example |
| 2 | [Customer Management](Part5-Domain-Examples/02-Customer-Management/) | Customer management + Specification search |
| 3 | [Inventory Management](Part5-Domain-Examples/03-Inventory-Management/) | Inventory + Soft Delete + cursor paging |
| 4 | [Catalog Search](Part5-Domain-Examples/04-Catalog-Search/) | Comparison of 3 pagination approaches |

### [Appendix](Appendix/)

- [A. CQRS vs Traditional CRUD](Appendix/A-cqrs-vs-crud.md)
- [B. Repository vs Query Adapter Selection Guide](Appendix/B-repository-vs-query-adapter-guide.md)
- [C. FinT / FinResponse Type Reference](Appendix/C-fint-finresponse-reference.md)
- [D. CQRS Anti-Patterns](Appendix/D-anti-patterns.md)
- [E. Glossary](Appendix/E-glossary.md)
- [F. References](Appendix/F-references.md)

---

## Core Evolution Process

[Part 1] Domain Entity Foundations
Ch 1: Entity and Identity  ->  Ch 2: Aggregate Root  ->  Ch 3: Domain Events  ->  Ch 4: Entity Interfaces

[Part 2] Command Side -- Repository Pattern
Ch 1: Repository Interface  ->  Ch 2: InMemory Repository  ->  Ch 3: EF Core Repository  ->  Ch 4: Unit of Work

[Part 3] Query Side -- Read-Only Patterns
Ch 1: IQueryPort Interface  ->  Ch 2: DTO Separation  ->  Ch 3: Pagination and Sorting  ->  Ch 4: InMemory Query Adapter  ->  Ch 5: Dapper Query Adapter

[Part 4] CQRS Use-Case Integration
Ch 1: Command Use Case  ->  Ch 2: Query Use Case  ->  Ch 3: FinT -> FinResponse  ->  Ch 4: Domain Event Flow  ->  Ch 5: Transaction Pipeline

[Part 5] Domain-Specific Practical Examples
Ch 1: Order Management  ->  Ch 2: Customer Management  ->  Ch 3: Inventory Management  ->  Ch 4: Catalog Search

---

## Functorium CQRS Type Hierarchy

```
Command Side (Write)
├── IRepository<TAggregate, TId>
│   ├── Create / GetById / Update / Delete
│   ├── CreateRange / GetByIds / UpdateRange / DeleteRange
│   └── Return type: FinT<IO, T>
├── InMemoryRepositoryBase (ConcurrentDictionary-based)
├── EfCoreRepositoryBase (EF Core-based)
└── IUnitOfWork
    ├── SaveChanges() : FinT<IO, Unit>
    └── BeginTransactionAsync() : IUnitOfWorkTransaction

Query Side (Read)
├── IQueryPort<TEntity, TDto>
│   ├── Search(spec, page, sort) : FinT<IO, PagedResult<TDto>>
│   ├── SearchByCursor(spec, cursor, sort) : FinT<IO, CursorPagedResult<TDto>>
│   └── Stream(spec, sort) : IAsyncEnumerable<TDto>
├── InMemoryQueryBase
└── DapperQueryBase

Use-Case Integration
├── ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>>
├── ICommandUsecase<TCommand, TSuccess> : ICommandHandler
├── IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>>
├── IQueryUsecase<TQuery, TSuccess> : IQueryHandler
└── ToFinResponse() : Fin<A> -> FinResponse<A>

Specification (Search Conditions)
├── Specification<T> (abstract class)
│   ├── IsSatisfiedBy(T) : bool
│   ├── And() / Or() / Not() composition
│   ├── & / | / ! operators
│   └── All (identity element, dynamic filter builder seed)
└── ExpressionSpecification<T> (EF Core/SQL support)
    ├── ToExpression() → Expression<Func<T, bool>>
    └── sealed IsSatisfiedBy (compilation + caching)
```

---

## Prerequisites

- .NET 10.0 SDK or later
- VS Code + C# Dev Kit extension
- Basic knowledge of C# syntax
- Basic understanding of Entity and CRUD concepts

---

## Project Structure

```
cqrs-repository/
├── Part0-Introduction/                     # Part 0: Introduction
├── Part1-Domain-Entity-Foundations/         # Part 1: Domain Entity Foundations (4)
│   ├── 01-Entity-And-Identity/
│   ├── 02-Aggregate-Root/
│   ├── 03-Domain-Events/
│   └── 04-Entity-Interfaces/
├── Part2-Command-Repository/               # Part 2: Command Side Repository (4)
│   ├── 01-Repository-Interface/
│   ├── 02-InMemory-Repository/
│   ├── 03-EfCore-Repository/
│   └── 04-Unit-Of-Work/
├── Part3-Query-Patterns/                   # Part 3: Query Side Read-Only (5)
│   ├── 01-QueryPort-Interface/
│   ├── 02-DTO-Separation/
│   ├── 03-Pagination-And-Sorting/
│   ├── 04-InMemory-Query-Adapter/
│   └── 05-Dapper-Query-Adapter/
├── Part4-CQRS-Usecase-Integration/         # Part 4: Use-Case Integration (5)
│   ├── 01-Command-Usecase/
│   ├── 02-Query-Usecase/
│   ├── 03-FinT-To-FinResponse/
│   ├── 04-Domain-Event-Flow/
│   └── 05-Transaction-Pipeline/
├── Part5-Domain-Examples/                  # Part 5: Domain-Specific Practical Examples (4)
│   ├── 01-Ecommerce-Order-Management/
│   ├── 02-Customer-Management/
│   ├── 03-Inventory-Management/
│   └── 04-Catalog-Search/
├── Appendix/                               # Appendix
└── README.md                               # This document
```

---

## Testing

All example projects in every Part include unit tests. Tests follow the [Unit Testing Guide](../../guides/testing/15a-unit-testing) conventions.

### Running Tests

```bash
# Test the entire tutorial
dotnet test --solution Docs.Site/src/content/docs/tutorials/cqrs-repository/cqrs-repository.slnx

# Test an individual project
dotnet test --project Docs.Site/src/content/docs/tutorials/cqrs-repository/Part1-Domain-Entity-Foundations/01-Entity-And-Identity/EntityAndIdentity.Tests.Unit
```

### Test Project Structure

**Part 1: Domain Entity Foundations** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `EntityAndIdentity.Tests.Unit` | Entity\<TId\>, IEntityId behavior verification |
| 2 | `AggregateRoot.Tests.Unit` | AggregateRoot invariant verification |
| 3 | `DomainEvents.Tests.Unit` | Domain event add/remove verification |
| 4 | `EntityInterfaces.Tests.Unit` | IAuditable, ISoftDeletable verification |

**Part 2: Command Side -- Repository Pattern** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `RepositoryInterface.Tests.Unit` | IRepository 8 CRUD operation verification |
| 2 | `InMemoryRepository.Tests.Unit` | InMemory implementation verification |
| 3 | `EfCoreRepository.Tests.Unit` | EF Core implementation verification |
| 4 | `UnitOfWork.Tests.Unit` | SaveChanges, transaction verification |

**Part 3: Query Side -- Read-Only Patterns** (5)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `QueryPortInterface.Tests.Unit` | IQueryPort Search/Stream verification |
| 2 | `DtoSeparation.Tests.Unit` | Command/Query DTO separation verification |
| 3 | `PaginationAndSorting.Tests.Unit` | Pagination, sorting verification |
| 4 | `InMemoryQueryAdapter.Tests.Unit` | InMemory Query adapter verification |
| 5 | `DapperQueryAdapter.Tests.Unit` | Dapper SQL generation verification |

**Part 4: CQRS Use-Case Integration** (5)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `CommandUsecase.Tests.Unit` | Command handler verification |
| 2 | `QueryUsecase.Tests.Unit` | Query handler verification |
| 3 | `FinTToFinResponse.Tests.Unit` | ToFinResponse conversion verification |
| 4 | `DomainEventFlow.Tests.Unit` | Event collection/publish verification |
| 5 | `TransactionPipeline.Tests.Unit` | Transaction pipeline verification |

**Part 5: Domain-Specific Practical Examples** (4)

| Ch | Test Project | Key Test Content |
|:---:|-------------|-----------------|
| 1 | `EcommerceOrderManagement.Tests.Unit` | Order CQRS verification |
| 2 | `CustomerManagement.Tests.Unit` | Customer management verification |
| 3 | `InventoryManagement.Tests.Unit` | Inventory management verification |
| 4 | `CatalogSearch.Tests.Unit` | Pagination comparison verification |

### Test Naming Convention

Follows the T1_T2_T3 naming convention:

```csharp
// Method_ExpectedResult_Scenario
[Fact]
public void Create_ReturnsAggregate_WhenValid()
{
    // Arrange
    var order = Order.Create(OrderId.New(), customerId);
    // Act
    var actual = await repository.Create(order).RunAsync();
    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

---

## Source Code

All example code for this tutorial can be found in the Functorium project:

- Repository interfaces: `Src/Functorium/Domains/Repositories/`
- Repository implementations: `Src/Functorium.Adapters/Repositories/`
- Query adapters: `Src/Functorium/Applications/Queries/`
- Use-case interfaces: `Src/Functorium/Applications/Usecases/`
- Transaction pipeline: `Src/Functorium/Adapters/Observabilities/Pipelines/`
- Tutorial projects: `Docs.Site/src/content/docs/tutorials/cqrs-repository/`

### Related Tutorials

This tutorial is more effective when studied together with:

- **[Implementing the Specification Pattern for Domain Rules](../specification-pattern/)**: From Specification pattern fundamentals to Repository integration. The IQueryPort and IRepository in this tutorial use Specifications as parameters.

---

This tutorial was written based on real-world experience developing the CQRS framework in the Functorium project.
