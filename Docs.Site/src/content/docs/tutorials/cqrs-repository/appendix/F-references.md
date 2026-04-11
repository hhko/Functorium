---
title: "References"
---
## Official Documentation

### Functorium
- **GitHub**: https://github.com/hhko/Functorium
- **Repository types**: `Src/Functorium/Domains/Repositories/`
- **Query adapters**: `Src/Functorium/Applications/Queries/`
- **Usecase interfaces**: `Src/Functorium/Applications/Usecases/`

### .NET
- **Entity Framework Core**: https://learn.microsoft.com/en-us/ef/core/
- **Dapper**: https://github.com/DapperLib/Dapper

### LanguageExt
- **GitHub**: https://github.com/louthy/language-ext
- **FinT documentation**: https://github.com/louthy/language-ext/wiki

---

## Books

### Domain-Driven Design
- **Title**: Domain-Driven Design: Tackling Complexity in the Heart of Software
- **Author**: Eric Evans
- **Publisher**: Addison-Wesley, 2003
- **Key content**: Foundational definitions of Entity, Aggregate Root, Repository, and Specification patterns

### Implementing Domain-Driven Design
- **Title**: Implementing Domain-Driven Design
- **Author**: Vaughn Vernon
- **Publisher**: Addison-Wesley, 2013
- **Key content**: Practical DDD implementation, CQRS and Event Sourcing

### Patterns of Enterprise Application Architecture
- **Title**: Patterns of Enterprise Application Architecture
- **Author**: Martin Fowler
- **Publisher**: Addison-Wesley, 2002
- **Key content**: Repository pattern, Unit of Work pattern, Query Object pattern

### Functional Programming in C#
- **Title**: Functional Programming in C#: How to write better C# code
- **Author**: Enrico Buonanno
- **Publisher**: Manning, 2022 (2nd Edition)
- **Key content**: Functional programming, monads, error handling

---

## Online Resources

### CQRS

**Greg Young's CQRS Documents**
- https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf
- The original document on the CQRS pattern. Theoretical foundations of Command and Query separation.

**Martin Fowler's CQRS**
- https://martinfowler.com/bliki/CQRS.html
- A concise explanation of the CQRS pattern and application guide.

**Microsoft CQRS Pattern**
- https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs
- Microsoft's CQRS pattern explanation and application on Azure.

### DDD & Repository Pattern

**Martin Fowler's Repository Pattern**
- https://martinfowler.com/eaaCatalog/repository.html
- Definition and explanation of the Repository pattern.

**Martin Fowler's Unit of Work**
- https://martinfowler.com/eaaCatalog/unitOfWork.html
- Definition of the Unit of Work pattern.

### Specification Pattern

**Eric Evans & Martin Fowler's Specification Pattern**
- https://www.martinfowler.com/apsupp/spec.pdf
- The original paper on the Specification pattern. Used as search criteria in IQueryPort.

---

## Related Libraries

### CQRS & Mediator

| Library | Description | NuGet |
|---------|-------------|-------|
| Functorium | The CQRS framework for this tutorial | - |
| Mediator | Source Generator-based Mediator pattern | Yes |

### ORM & Data Access

| Library | Description | NuGet |
|---------|-------------|-------|
| Microsoft.EntityFrameworkCore | EF Core ORM (Command side) | Yes |
| Dapper | Lightweight ORM (Query side) | Yes |

### Functional Programming

| Library | Description | NuGet |
|---------|-------------|-------|
| LanguageExt.Core | Functional programming types (Fin, FinT, IO) | Yes |

### Testing

| Library | Description | NuGet |
|---------|-------------|-------|
| xUnit | Test framework | Yes |
| Shouldly | Assertion library | Yes |
| NSubstitute | Mock library | Yes |

---

## Functorium Project Reference

### Source Code Locations

```
Src/Functorium/
├── Domains/
│   ├── Entities/                  # Entity<TId>, AggregateRoot<TId>, IEntityId
│   ├── Repositories/              # IRepository<TAggregate, TId>
│   └── Specifications/            # Specification<T>
├── Applications/
│   ├── Queries/                   # IQueryPort<TEntity, TDto>
│   ├── Usecases/                  # ICommandRequest, IQueryRequest, FinResponse
│   └── Persistence/               # IUnitOfWork, IUnitOfWorkTransaction
Src/Functorium.Adapters/
├── Repositories/                  # InMemoryRepositoryBase, EfCoreRepositoryBase
├── Events/                        # DomainEventCollector
└── Observabilities/Pipelines/     # UsecaseTransactionPipeline
```

### Tutorial Projects

```
Docs.Site/src/content/docs/tutorials/cqrs-repository/
├── Part1-Domain-Entity-Foundations/   # Domain entities (4)
├── Part2-Command-Repository/          # Repository pattern (4)
├── Part3-Query-Patterns/              # Query patterns (5)
├── Part4-CQRS-Usecase-Integration/    # Usecase integration (5)
└── Part5-Domain-Examples/             # Practical examples (4)
```

---

## Related Tutorials

This tutorial is more effective when studied alongside the following tutorials:

- **[Implementing Domain Rules with the Specification Pattern](../specification-pattern/)**: From Specification pattern basics to Repository integration. In this tutorial, IQueryPort and IRepository use Specification as a parameter.

---

## Recommended Learning Order

### Beginner (Pattern introduction)
1. Repository chapter from Eric Evans' DDD book
2. Part 1 of this tutorial
3. Read Functorium Repository/Entity source code

### Intermediate (Practical application)
1. Martin Fowler's CQRS document
2. Parts 2~3 of this tutorial
3. EF Core / Dapper official documentation

### Advanced (Architecture design)
1. Greg Young's CQRS Documents
2. Parts 4~5 of this tutorial
3. Advanced study of LanguageExt functional types

---

This tutorial was written based on real-world CQRS framework development experience with the Functorium project.
