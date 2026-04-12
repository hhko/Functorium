---
title: "ORM Integration Patterns"
---
## Overview

In the domain model, `Email` is a strongly-typed object, but it must be stored as a `VARCHAR` column in the database. How do you map a composite value object like `Address(City, Street, PostalCode)`? What about collections like `List<OrderLineItem>` within an order?

In this chapter, we cover how to persist value objects while maintaining domain model purity, using three patterns provided by Entity Framework Core: `OwnsOne`, `OwnsMany`, and `Value Converter`.

## Learning Objectives

- Map composite value objects (Address, Money, etc.) as part of an entity using the `OwnsOne` pattern.
- Convert single value objects (Email, ProductCode, etc.) to database columns using the `Value Converter` pattern.
- Map value object collections (OrderLineItem, etc.) using the `OwnsMany` pattern.
- Design a structure that integrates with EF Core while maintaining domain model purity.

## Why Is This Needed?

There are several technical challenges when persisting value objects to a database.

In the domain, `Email` is a strongly-typed object, but it is stored as a `VARCHAR` column in the database. Manually handling this type conversion every time causes code duplication and mistakes. Separating composite value objects like `Address(City, Street, PostalCode)` into separate tables causes unnecessary joins, while storing them in the same table requires explicit column mapping. Additionally, value object collections like `List<OrderLineItem>` require a separate table but must be managed as owned types rather than entities.

EF Core's Owned Entity feature and Value Converters can transparently solve these challenges.

## Core Concepts

### OwnsOne Pattern

`OwnsOne` maps a value object as part of an entity. Each property of the value object is stored as a column in the parent table.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Email value object: stored as Email_Value column in the User table
    modelBuilder.Entity<User>()
        .OwnsOne(u => u.Email);

    // Address composite value object: stored as Address_City, Address_Street, Address_PostalCode columns in the User table
    modelBuilder.Entity<User>()
        .OwnsOne(u => u.Address);
}
```

The data is stored as columns in the same table rather than a separate table, and is loaded together with the parent entity. The resulting table structure is as follows.

```
Users table
├── Id (PK)
├── Name
├── Email_Value          # Email mapped via OwnsOne
├── Address_City         # Address mapped via OwnsOne
├── Address_Street
└── Address_PostalCode
```

### Value Converter Pattern

`HasConversion` converts a value object into a single column. It defines bidirectional conversion from object to primitive value and from primitive value back to object.

```csharp
modelBuilder.Entity<Product>()
    .Property(p => p.Code)
    .HasConversion(
        code => code.Value,                           // On save: ProductCode -> string
        value => ProductCode.CreateFromValidated(value) // On load: string -> ProductCode
    );
```

Domain code works with the `ProductCode` type while the database stores it as a string. This conversion process is handled automatically at the ORM level. While `OwnsOne` stores each property of a value object as a separate column, `HasConversion` stores the entire value object as a single column.

### OwnsMany Pattern

`OwnsMany` maps value object collections. They are stored in a separate table but managed as owned types rather than entities.

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems);
```

`OrderLineItem` is stored in a separate table, but is deleted together when the `Order` is deleted. It has no independent lifecycle. The resulting table structure is as follows.

```
Orders table
├── Id (PK)
└── CustomerName

OrderLineItem table
├── OrderId (FK, part of PK)
├── Id (part of PK)
├── ProductName
├── Quantity
└── UnitPrice
```

### Private Constructors and EF Core Compatibility

Value objects use private constructors for immutability. A parameterless private constructor is needed to maintain compatibility with EF Core.

```csharp
public sealed class Email
{
    public string Value { get; private set; }

    // Private constructor for EF Core mapping
    private Email() => Value = string.Empty;

    // Private constructor for actual creation
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value) { ... }
}
```

EF Core creates objects using the parameterless constructor and then sets properties. Used with `private setter`, it blocks external modification while allowing ORM mapping.

## Practical Guidelines

### Expected Output
```
=== ORM Integration Patterns ===

1. OwnsOne Pattern - Composite Value Object Mapping
────────────────────────────────────────
   Saved user: Hong Gildong
   Email: hong@example.com
   Address: Seoul Gangnam-gu Teheran-ro 123 (06234)

2. Value Converter Pattern - Single Value Object Conversion
────────────────────────────────────────
   Product code: EL-001234
   Price: 50,000 KRW

3. OwnsMany Pattern - Collection Value Object Mapping
────────────────────────────────────────
   Customer: Kim Cheolsu
   Order items:
      - Product A: 2 x 10,000 won
      - Product B: 1 x 25,000 won
```

### DbContext Configuration Example

A `DbContext` configuration with all three mapping patterns applied.

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. OwnsOne: Email value object
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Email);

        // 2. OwnsOne: Address composite value object
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Address);

        // 3. Value Converter: ProductCode
        modelBuilder.Entity<Product>()
            .Property(p => p.Code)
            .HasConversion(
                code => code.Value,
                value => ProductCode.CreateFromValidated(value));

        // 4. OwnsOne: Money
        modelBuilder.Entity<Product>()
            .OwnsOne(p => p.Price);

        // 5. OwnsMany: OrderLineItem collection
        modelBuilder.Entity<Order>()
            .OwnsMany(o => o.LineItems);
    }
}
```

## Project Description

### Project Structure
```
02-ORM-Integration/
├── OrmIntegration/
│   ├── Program.cs                # Main executable (includes value objects, entities, DbContext)
│   └── OrmIntegration.csproj     # Project file
└── README.md                     # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
</ItemGroup>
```

### Core Code

**Entity Definitions**
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Email Email { get; set; } = null!;       // Single value object
    public Address Address { get; set; } = null!;   // Composite value object
}

public class Product
{
    public Guid Id { get; set; }
    public ProductCode Code { get; set; } = null!;  // Uses Value Converter
    public Money Price { get; set; } = null!;       // Uses OwnsOne
}

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderLineItem> LineItems { get; set; } = new();  // Uses OwnsMany
}
```

**value object Definitions**
```csharp
// EF Core compatible value object
public sealed class Email
{
    public string Value { get; private set; }

    private Email() => Value = string.Empty;  // For EF Core
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value) { ... }
    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());
}

// Composite value object
public sealed class Address
{
    public string City { get; private set; }
    public string Street { get; private set; }
    public string PostalCode { get; private set; }

    private Address()  // For EF Core
    {
        City = string.Empty;
        Street = string.Empty;
        PostalCode = string.Empty;
    }

    public Address(string city, string street, string postalCode)
    {
        City = city;
        Street = street;
        PostalCode = postalCode;
    }
}
```

## Summary at a Glance

### ORM 매핑 패턴 비교

세 가지 패턴의 저장 방식과 적합한 value object 유형을 compares.

| 패턴 | 저장 방식 | 적합한 value object | 테이블 구조 |
|------|----------|---------------|------------|
| `OwnsOne` | 부모 테이블 컬럼 | Email, Address, Money | 같은 테이블 |
| `HasConversion` | 단일 컬럼 | ProductCode, UserId | 같은 테이블, 1컬럼 |
| `OwnsMany` | 별도 테이블 | OrderLineItem | 자식 테이블 |

### 패턴 선택 가이드

value object의 구조에 따라 적합한 매핑 패턴을 선택합니다.

| 상황 | 권장 패턴 |
|------|----------|
| 단일 속성 value object | `HasConversion` 또는 `OwnsOne` |
| 다중 속성 value object | `OwnsOne` |
| value object 컬렉션 | `OwnsMany` |
| JSON 직렬화 필요 | `HasConversion` + JSON |

### EF Core 호환성 체크리스트

value object를 EF Core와 통합할 때 확인해야 할 항목입니다.

| 항목 | Description |
|------|------|
| 매개변수 없는 private 생성자 | EF Core가 객체를 생성할 수 있도록 |
| private setter | immutability 유지하면서 EF Core 매핑 허용 |
| `CreateFromValidated()` 메서드 | Value Converter에서 사용 |
| 기본값 초기화 | nullable 경고 방지 |

## FAQ

### Q1: OwnsOne과 HasConversion 중 어떤 것을 선택해야 하나요?
**A**: 단일 속성이면서 로드 시 변환 로직이 필요하면 `HasConversion`이 적합합니다. 다중 속성이면 `OwnsOne`을 uses. `OwnsOne`은 속성별로 컬럼이 생성되어 쿼리에서 개별 속성을 조건으로 사용할 수 있습니다.

### Q2: private 생성자를 사용하면서 EF Core와 호환되게 하려면?
**A**: EF Core는 Reflection으로 private 생성자를 호출할 수 있습니다. 매개변수 없는 private 생성자를 제공하고, `private set`을 사용하면 EF Core가 값을 설정하면서도 외부 코드에서의 변경은 차단됩니다.

### Q3: OwnsMany로 매핑된 컬렉션의 정렬은 어떻게 하나요?
**A**: `OwnsMany`는 기본적으로 정렬 순서를 보장하지 않습니다. 순서가 중요하면 정렬 컬럼을 명시적으로 추가합니다.

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems, builder =>
    {
        builder.Property<int>("Sequence");
        builder.HasKey("OrderId", "Sequence");
    });
```

---

## 테스트

이 프로젝트에는 단위 테스트가 포함되어 있습니다.

### 테스트 실행
```bash
cd OrmIntegration.Tests.Unit
dotnet test
```

### 테스트 구조
```
OrmIntegration.Tests.Unit/
├── OwnsOnePatternTests.cs       # OwnsOne 매핑 패턴 테스트
├── ValueConverterPatternTests.cs # Value Converter 패턴 테스트
└── OwnsManyPatternTests.cs      # OwnsMany 컬렉션 매핑 테스트
```

### 주요 테스트 케이스

| 테스트 클래스 | 테스트 내용 |
|-------------|-----------|
| OwnsOnePatternTests | Address, Email 복합 value object 영속화 |
| ValueConverterPatternTests | ProductCode 단일 값 변환 |
| OwnsManyPatternTests | OrderLineItem 컬렉션 영속화 |

value object를 데이터베이스에 저장하는 패턴을 익혔으니, Next chapter에서는 CQRS 아키텍처에서 value object를 Command/Query와 통합하는 방법을 다룹니다.

---

→ [3장: CQRS와 value object](../03-CQRS-Integration/)
