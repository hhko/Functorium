---
title: "Designing with Types"
---

## Background

Contact management seems simple, but when real business rules like data validity, contact method combinations, email verification lifecycle, and lifecycle management are involved, a naive implementation allows invalid states. This sample combines Eric Evans's DDD tactical patterns with Functorium's type system to embed business rules directly into the structure of the domain model.

> This sample is based on Scott Wlaschin's [Designing with Types](https://fsharpforfunandprofit.com/series/designing-with-types/) series. The original was written in F# and has been reimplemented with C# and the Functorium framework.

## Naive Starting Point

```csharp
public class Contact
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? MiddleInitial { get; set; }
    public string? EmailAddress { get; set; }
    public bool IsEmailVerified { get; set; }
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
}
```

This implementation compiles and runs. However, it allows the following invalid states:
- 100-character names, non-numeric zip codes — no validation exists
- A contact with neither email nor address — a contact without any contact method is possible
- `IsEmailVerified = true` while `EmailAddress = null` — a contradictory state
- Reverting a verified email to `false` — one-way transitions are not guaranteed
- First name and email are both `string` — swapping them by accident goes unnoticed by the compiler

## Goal

Block the problems above not through runtime validation but through the **type system**:

- **Invalid values cannot be created** — constrained value objects complete validation at creation time
- **Invalid states cannot be represented** — union types enumerate only the permitted combinations
- **Invalid transitions cannot execute** — a type-safe state machine enforces the rules
- **Failures cannot be ignored** — `Fin<T>` return types force callers to handle errors

DDD tactical patterns define the rule boundaries, and Functorium's functional types enforce them at the compiler level.

## 5-Step Journey

This sample goes through 5 steps from naive code to a complete DDD domain model. Each step takes the output of the previous step as input to derive the next decision.

| Step | Key Question | Input | Output | Document |
|------|-------------|-------|--------|----------|
| 1. Requirements | What needs to be done? | Domain expert | Business rules + scenarios | [Business Requirements](./domain/00-business-requirements/) |
| 2. Design Decisions | How to guarantee each invariant? | Business rules | Type strategy per invariant category | [Type Design Decisions](./domain/01-type-design-decisions/) |
| 3. Code Design | Which C#/Functorium patterns? | Type strategy | Implementation pattern mapping | [Code Design](./domain/02-code-design/) |
| 4. Implementation | How to realize in code? | Pattern mapping | Domain model source | [Implementation Results](./domain/03-implementation-results/) |
| 5. Verification | Are the rules guaranteed? | Business rules + code | Unit tests (138 tests) | `Tests/DesigningWithTypes.Tests.Unit/` |

## Applied DDD Building Blocks

| DDD Concept | Functorium Type | Application |
|-------------|----------------|-------------|
| Value Object | `SimpleValueObject<T>`, `ValueObject` | String50, EmailAddress, StateCode, ZipCode, PersonalName, PostalAddress, NoteContent |
| Discriminated Union | `UnionValueObject` + `[UnionType]` (auto-generated Match/Switch) | ContactInfo, EmailVerificationState |
| Entity | `Entity<TId>` | ContactNote |
| Aggregate Root | `AggregateRoot<TId>` | Contact |
| Domain Event | `DomainEvent` | CreatedEvent, NameUpdatedEvent, EmailVerifiedEvent, etc. (7 types) |
| Specification | `ExpressionSpecification<T>` | ContactEmailSpec, ContactEmailUniqueSpec |
| Domain Service | `IDomainService` | ContactEmailCheckService |
| Repository | `IRepository<T, TId>` | IContactRepository |

## Project Structure

```
samples/designing-with-types/
├── Directory.Build.props              # Build settings (net10.0, C# 14)
├── Directory.Build.targets            # Block root inheritance
├── designing-with-types.slnx          # Solution file
├── domain/                            # Domain design documents
│   ├── 00-business-requirements.md    # Step 1: Business requirements
│   ├── 01-type-design-decisions.md    # Step 2: Type design decisions
│   ├── 02-code-design.md              # Step 3: Code design
│   └── 03-implementation-results.md   # Step 4: Implementation results
├── Src/
│   └── DesigningWithTypes/            # Step 4: Implementation
│       ├── SharedModels/              # Shared domain elements
│       │   └── ValueObjects/
│       │       └── String50.cs        # Max 50 char string VO (shared primitive type)
│       ├── AggregateRoots/
│       │   └── Contacts/              # Contact Aggregate boundary
│       │       ├── Contact.cs         # Aggregate Root
│       │       ├── ContactNote.cs     # Child entity
│       │       ├── IContactRepository.cs  # Repository interface
│       │       ├── ValueObjects/
│       │       │   ├── Simples/       # Primitive type wrappers
│       │       │   │   ├── EmailAddress.cs
│       │       │   │   ├── StateCode.cs
│       │       │   │   ├── ZipCode.cs
│       │       │   │   └── NoteContent.cs
│       │       │   ├── Composites/    # Multiple VO compositions
│       │       │   │   ├── PersonalName.cs
│       │       │   │   └── PostalAddress.cs
│       │       │   └── Unions/        # Discriminated Union
│       │       │       ├── ContactInfo.cs
│       │       │       └── EmailVerificationState.cs
│       │       ├── Specifications/    # Query specifications
│       │       │   ├── ContactEmailSpec.cs
│       │       │   └── ContactEmailUniqueSpec.cs
│       │       └── Services/          # Domain services
│       │           └── ContactEmailCheckService.cs
│       └── Program.cs                 # Demo
└── Tests/
    └── DesigningWithTypes.Tests.Unit/ # Step 5: Verification (138 tests)
        ├── Architecture/              # Architecture rule verification (24 tests)
        │   ├── ArchitectureTestBase.cs
        │   ├── ValueObjectArchitectureRuleTests.cs
        │   ├── EntityArchitectureRuleTests.cs
        │   ├── DomainEventArchitectureRuleTests.cs
        │   ├── DomainServiceArchitectureRuleTests.cs
        │   └── SpecificationArchitectureRuleTests.cs
        └── Domain/
            ├── SharedModels/
            │   └── ValueObjectTests.cs
            ├── Contacts/
            │   ├── ContactTests.cs
            │   ├── ContactNoteTests.cs
            │   ├── PersonalNameTests.cs
            │   ├── PostalAddressTests.cs
            │   ├── ContactInfoTests.cs
            │   ├── EmailVerificationStateTests.cs
            │   ├── NoteContentTests.cs
            │   └── ContactSpecificationTests.cs
            └── Services/
                └── ContactEmailCheckServiceTests.cs
```

## How to Run

```bash
# Build
dotnet build Docs.Site/src/content/docs/samples/designing-with-types/designing-with-types.slnx

# Test
dotnet test --solution Docs.Site/src/content/docs/samples/designing-with-types/designing-with-types.slnx

# Run demo
dotnet run --project Docs.Site/src/content/docs/samples/designing-with-types/Src/DesigningWithTypes/DesigningWithTypes.csproj
```
