---
title: "ADR-0004: Domain - Ulid-Based Entity ID"
status: "accepted"
date: 2026-03-16
---

## Context and Problem

In DDD, Entities are distinguished by unique identifiers (IDs), and the choice of ID type directly impacts database write performance, collision probability in distributed environments, and integration with the framework's type system.

Consider a product table using Guid v4 as the primary key. When inserting 1 million records, random values wedge into the middle of the B-Tree index, causing frequent page splits and rapidly increasing index fragmentation. Switching to auto-increment long guarantees sequential insertion, but in a distributed environment where the order service and product service use separate databases, ID collisions cannot be avoided without a central sequence. Guid v7 enables time-ordered sorting, but its integration with Functorium's `[GenerateEntityId]` source generator and LanguageExt type system has not been validated.

## Considered Options

1. Ulid
2. Guid v4
3. Guid v7
4. long auto-increment

## Decision

**Chosen option: "Ulid"**. The 48-bit timestamp ensures time-ordered sorting, minimizing B-Tree index page splits, while the 80 random bits provide collision-free uniqueness even in distributed environments. Crucially, it integrates with the `[GenerateEntityId]` source generator to automatically generate type-safe IDs like `ProductId` and `OrderId` along with EF Core ValueConverters, naturally coupling with Functorium's type system.

### Consequences

- <span class="adr-good">Good</span>, because timestamp-based sequential ordering inserts at the end of the B-Tree index in order, significantly reducing page splits compared to Guid v4 and delivering stable write performance during bulk inserts.
- <span class="adr-good">Good</span>, because the 80-bit randomness provides uniqueness that safely generates IDs without a central sequence even in independent database environments like order service/product service.
- <span class="adr-good">Good</span>, because integration with `[GenerateEntityId]` auto-generates `ProductId.New()`, EF Core ValueConverters, and `IParsable<T>` implementations, eliminating repetitive code for each ID type.
- <span class="adr-good">Good</span>, because the 26-character Crockford Base32 encoding (`01ARZ3NDEKTSV4RRFFQ69G5FAV`) is URL-safe and shorter than Guid's 36-character dash-inclusive format, improving readability in logs and API responses.
- <span class="adr-bad">Bad</span>, because Ulid is not built into the .NET standard library, adding an external dependency on the Cysharp/Ulid NuGet package.
- <span class="adr-bad">Bad</span>, because integration with external systems that expect Guid as a primary key (Azure AD, third-party APIs, etc.) requires `Ulid.ToGuid()` / `Guid -> Ulid` conversion code.

### Confirmation

- Verify that Entity ID types are generated based on Ulid (e.g., `ProductId.New()` returns a Ulid-formatted result).
- Verify that ValueConverters are auto-generated for ID types with the `[GenerateEntityId]` attribute.
- Verify through performance tests that sequential insertion in B-Tree indexes does not cause page splits.

## Pros and Cons of the Options

### Ulid

- <span class="adr-good">Good</span>, because the 48-bit timestamp (millisecond precision) guarantees time-ordered sorting and the 80 random bits prevent collisions even within the same millisecond, achieving both sortability and uniqueness simultaneously.
- <span class="adr-good">Good</span>, because new IDs always append to the end of the B-Tree index, minimizing page splits and maintaining stable performance without index fragmentation even with bulk inserts of over 1 million records.
- <span class="adr-good">Good</span>, because the 26-character Crockford Base32 encoding (`01ARZ3NDEKTSV4RRFFQ69G5FAV`) is URL-safe with no case-sensitivity confusion.
- <span class="adr-good">Good</span>, because integration with `[GenerateEntityId]` auto-generates ValueConverter, `ToString`, `Parse`, and equality comparison code for `ProductId`, `OrderId`, etc.
- <span class="adr-bad">Bad</span>, because if the Cysharp/Ulid package ceases maintenance, a replacement must be found or a self-maintained fork created, posing an external dependency risk.

### Guid v4

- <span class="adr-good">Good</span>, because `System.Guid` is built into the .NET standard library, requiring no additional NuGet dependency.
- <span class="adr-good">Good</span>, because most external systems (Azure, AWS, third-party APIs) expect Guid as a primary key, requiring no conversion during integration.
- <span class="adr-bad">Bad</span>, because fully random values insert at arbitrary positions in the B-Tree index, causing page splits and index fragmentation that accumulate and degrade performance during bulk writes.
- <span class="adr-bad">Bad</span>, because there is no time information, so creation order cannot be inferred from the ID alone, and a separate timestamp column is needed to determine "which entity was created first" during debugging.

### Guid v7

- <span class="adr-good">Good</span>, because timestamp-based time-ordered sorting is possible, offering B-Tree index performance equivalent to Ulid.
- <span class="adr-good">Good</span>, because .NET 9's `Guid.CreateVersion7()` provides standard library support with no external dependencies.
- <span class="adr-bad">Bad</span>, because the `[GenerateEntityId]` source generator is designed around Ulid, so switching to Guid v7 would require rewriting the code generation logic for ValueConverter, Parse, and equality comparison.
- <span class="adr-bad">Bad</span>, because at Functorium's design time, Guid v7's integration with LanguageExt NewType and Crockford Base32 encoding support had not been validated.

### long auto-increment

- <span class="adr-good">Good</span>, because at 8 bytes, storage and index size are half that of Guid (16 bytes) or Ulid (16 bytes), and fully sequential ordering delivers theoretically optimal B-Tree write performance.
- <span class="adr-good">Good</span>, because integer comparison is faster than byte array comparison, offering a marginal advantage in join and query performance.
- <span class="adr-bad">Bad</span>, because when the order service and product service use separate databases, sequences collide, and resolving this requires a central ID issuing service that becomes a single point of failure (SPOF).
- <span class="adr-bad">Bad</span>, because ID values like 1, 2, 3 are predictable, making them vulnerable to IDOR (Insecure Direct Object Reference) attacks.
- <span class="adr-bad">Bad</span>, because the ID must be retrieved via `SCOPE_IDENTITY()` after `INSERT`, making it impossible to create and test entities in the domain layer without a database.

## Related Information

- Related commit: `0470af7b` refactor(domains): Move GenerateEntityIdAttribute to Entities namespace
- Related commit: `adfa72c8` feat: Add IParsable<T> constraint to IEntityId
- Related docs: `Docs.Site/src/content/docs/guides/domain/`
