# ORM 통합 패턴

> **Part 4: 실전 가이드** | [← 이전: Functorium 프레임워크](../01-Functorium-Framework/README.md) | [목차](../../README.md) | [다음: CQRS 통합 →](../03-CQRS-Integration/README.md)

---

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 값 객체를 Entity Framework Core와 통합하는 패턴을 학습합니다. DDD의 값 객체는 도메인 모델에서는 강력한 타입으로 존재하지만, 데이터베이스에는 원시 타입으로 저장해야 합니다. 이 변환 과정을 ORM 수준에서 투명하게 처리하는 방법을 익힙니다.

Entity Framework Core가 제공하는 `OwnsOne`, `OwnsMany`, `Value Converter` 세 가지 패턴을 사용하여 값 객체를 영속화하는 방법을 실습합니다.

## 학습 목표

### **핵심 학습 목표**
1. **OwnsOne 패턴**: 복합 값 객체(Address, Money 등)를 엔티티의 일부로 매핑하는 방법을 학습합니다.
2. **Value Converter 패턴**: 단일 값 객체(Email, ProductCode 등)를 데이터베이스 컬럼으로 변환하는 방법을 익힙니다.
3. **OwnsMany 패턴**: 값 객체 컬렉션(OrderLineItem 등)을 매핑하는 방법을 학습합니다.
4. **영속성과 도메인 분리**: 도메인 모델의 순수성을 유지하면서 데이터베이스와 통합하는 방법을 이해합니다.

### **실습을 통해 확인할 내용**
- `OwnsOne`으로 Address, Email 같은 복합/단일 값 객체 매핑
- `HasConversion`으로 ProductCode를 문자열로 저장
- `OwnsMany`로 OrderLineItem 컬렉션 매핑
- 값 객체의 private 생성자와 EF Core의 호환성

## 왜 필요한가?

값 객체를 데이터베이스에 저장할 때 몇 가지 기술적 도전이 있습니다.

**첫 번째 도전은 타입 변환입니다.** 도메인에서 `Email`은 강타입 객체이지만, 데이터베이스에는 `VARCHAR` 컬럼으로 저장됩니다. 이 변환을 매번 수동으로 처리하면 코드 중복과 실수가 발생합니다.

**두 번째 도전은 복합 값 객체의 매핑입니다.** `Address(City, Street, PostalCode)`처럼 여러 속성을 가진 값 객체를 어떻게 저장할지 결정해야 합니다. 별도 테이블로 분리하면 불필요한 조인이 발생하고, 같은 테이블에 저장하면 컬럼 매핑을 명시해야 합니다.

**세 번째 도전은 컬렉션 값 객체입니다.** 주문에 포함된 `List<OrderLineItem>`처럼 값 객체 컬렉션은 별도 테이블이 필요하지만, 엔티티가 아닌 소유된 타입으로 관리해야 합니다.

EF Core의 Owned Entity 기능과 Value Converter를 사용하면 이러한 도전들을 우아하게 해결할 수 있습니다.

## 핵심 개념

### 첫 번째 개념: OwnsOne 패턴

`OwnsOne`은 값 객체를 엔티티의 일부로 매핑합니다. 값 객체의 각 속성이 부모 테이블의 컬럼으로 저장됩니다.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Email 값 객체: User 테이블에 Email_Value 컬럼으로 저장
    modelBuilder.Entity<User>()
        .OwnsOne(u => u.Email);

    // Address 복합 값 객체: User 테이블에 Address_City, Address_Street, Address_PostalCode 컬럼으로 저장
    modelBuilder.Entity<User>()
        .OwnsOne(u => u.Address);
}
```

**핵심 아이디어는 "값 객체를 부모 엔티티의 일부로 취급"하는 것입니다.** 별도 테이블이 아닌 같은 테이블의 컬럼들로 저장되며, 부모 엔티티와 함께 로드됩니다.

생성되는 테이블 구조:
```
Users 테이블
├── Id (PK)
├── Name
├── Email_Value          # OwnsOne으로 매핑된 Email
├── Address_City         # OwnsOne으로 매핑된 Address
├── Address_Street
└── Address_PostalCode
```

### 두 번째 개념: Value Converter 패턴

`HasConversion`은 값 객체를 단일 컬럼으로 변환합니다. 객체에서 원시 값으로, 원시 값에서 객체로의 양방향 변환을 정의합니다.

```csharp
modelBuilder.Entity<Product>()
    .Property(p => p.Code)
    .HasConversion(
        code => code.Value,                           // 저장 시: ProductCode → string
        value => ProductCode.CreateFromValidated(value) // 로드 시: string → ProductCode
    );
```

**핵심 아이디어는 "투명한 타입 변환"입니다.** 도메인 코드는 `ProductCode` 타입으로 작업하고, 데이터베이스에는 문자열로 저장됩니다. 이 변환 과정이 ORM 수준에서 자동으로 처리됩니다.

`OwnsOne`과의 차이점:
- `OwnsOne`: 값 객체의 각 속성이 별도 컬럼으로 저장
- `HasConversion`: 값 객체 전체가 하나의 컬럼으로 저장

### 세 번째 개념: OwnsMany 패턴

`OwnsMany`는 값 객체 컬렉션을 매핑합니다. 별도 테이블에 저장되지만 엔티티가 아닌 소유된 타입으로 관리됩니다.

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems);
```

**핵심 아이디어는 "값 객체 컬렉션을 부모의 일부로 관리"하는 것입니다.** `OrderLineItem`은 별도 테이블에 저장되지만, `Order`가 삭제되면 함께 삭제됩니다. 독립적인 생명주기가 없습니다.

생성되는 테이블 구조:
```
Orders 테이블
├── Id (PK)
└── CustomerName

OrderLineItem 테이블
├── OrderId (FK, PK 일부)
├── Id (PK 일부)
├── ProductName
├── Quantity
└── UnitPrice
```

### 네 번째 개념: private 생성자와 EF Core 호환성

값 객체는 불변성을 위해 private 생성자를 사용합니다. EF Core와 호환성을 유지하려면 매개변수 없는 private 생성자가 필요합니다.

```csharp
public sealed class Email
{
    public string Value { get; private set; }

    // EF Core 매핑용 private 생성자
    private Email() => Value = string.Empty;

    // 실제 생성용 private 생성자
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value) { ... }
}
```

**핵심 아이디어는 "ORM 호환성과 도메인 순수성의 균형"입니다.** EF Core는 매개변수 없는 생성자로 객체를 생성한 후 속성을 설정합니다. private setter와 함께 사용하면 외부에서의 변경은 막으면서 ORM 매핑은 가능합니다.

## 실전 지침

### 예상 출력
```
=== ORM 통합 패턴 ===

1. OwnsOne 패턴 - 복합 값 객체 매핑
────────────────────────────────────────
   저장된 사용자: 홍길동
   이메일: hong@example.com
   주소: 서울 강남구 테헤란로 123 (06234)

2. Value Converter 패턴 - 단일 값 객체 변환
────────────────────────────────────────
   상품 코드: EL-001234
   가격: 50,000 KRW

3. OwnsMany 패턴 - 컬렉션 값 객체 매핑
────────────────────────────────────────
   주문자: 김철수
   주문 항목:
      - 상품 A: 2개 x 10,000원
      - 상품 B: 1개 x 25,000원
```

### DbContext 설정 예시

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 1. OwnsOne: Email 값 객체
        modelBuilder.Entity<User>()
            .OwnsOne(u => u.Email);

        // 2. OwnsOne: Address 복합 값 객체
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

        // 5. OwnsMany: OrderLineItem 컬렉션
        modelBuilder.Entity<Order>()
            .OwnsMany(o => o.LineItems);
    }
}
```

## 프로젝트 설명

### 프로젝트 구조
```
02-ORM-Integration/
├── OrmIntegration/
│   ├── Program.cs                # 메인 실행 파일 (값 객체, 엔티티, DbContext 포함)
│   └── OrmIntegration.csproj     # 프로젝트 파일
└── README.md                     # 프로젝트 문서
```

### 의존성
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
</ItemGroup>
```

### 핵심 코드

**엔티티 정의**
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Email Email { get; set; } = null!;       // 단일 값 객체
    public Address Address { get; set; } = null!;   // 복합 값 객체
}

public class Product
{
    public Guid Id { get; set; }
    public ProductCode Code { get; set; } = null!;  // Value Converter 사용
    public Money Price { get; set; } = null!;       // OwnsOne 사용
}

public class Order
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderLineItem> LineItems { get; set; } = new();  // OwnsMany 사용
}
```

**값 객체 정의**
```csharp
// EF Core 호환 값 객체
public sealed class Email
{
    public string Value { get; private set; }

    private Email() => Value = string.Empty;  // EF Core용
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value) { ... }
    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());
}

// 복합 값 객체
public sealed class Address
{
    public string City { get; private set; }
    public string Street { get; private set; }
    public string PostalCode { get; private set; }

    private Address()  // EF Core용
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

## 한눈에 보는 정리

### ORM 매핑 패턴 비교

| 패턴 | 저장 방식 | 적합한 값 객체 | 테이블 구조 |
|------|----------|---------------|------------|
| `OwnsOne` | 부모 테이블 컬럼 | Email, Address, Money | 같은 테이블 |
| `HasConversion` | 단일 컬럼 | ProductCode, UserId | 같은 테이블, 1컬럼 |
| `OwnsMany` | 별도 테이블 | OrderLineItem | 자식 테이블 |

### 패턴 선택 가이드

| 상황 | 권장 패턴 |
|------|----------|
| 단일 속성 값 객체 | `HasConversion` 또는 `OwnsOne` |
| 다중 속성 값 객체 | `OwnsOne` |
| 값 객체 컬렉션 | `OwnsMany` |
| JSON 직렬화 필요 | `HasConversion` + JSON |

### EF Core 호환성 체크리스트

| 항목 | 설명 |
|------|------|
| 매개변수 없는 private 생성자 | EF Core가 객체를 생성할 수 있도록 |
| private setter | 불변성 유지하면서 EF Core 매핑 허용 |
| `CreateFromValidated()` 메서드 | Value Converter에서 사용 |
| 기본값 초기화 | nullable 경고 방지 |

## FAQ

### Q1: OwnsOne과 HasConversion 중 어떤 것을 선택해야 하나요?
**A**: 값 객체의 구조에 따라 선택합니다.

- **단일 속성, 복잡한 변환 로직**: `HasConversion`이 적합합니다. `ProductCode`처럼 하나의 문자열로 저장하면서 로드 시 `CreateFromValidated()`로 복원합니다.
- **단일 속성, 단순 저장**: `OwnsOne`도 가능합니다. `Email`처럼 단순히 Value 속성만 있어도 `OwnsOne`으로 매핑할 수 있습니다.
- **다중 속성**: `OwnsOne`을 사용합니다. `Address`처럼 City, Street, PostalCode를 각각 컬럼으로 저장합니다.

`HasConversion`은 더 유연한 변환 로직을 정의할 수 있고, `OwnsOne`은 속성별로 컬럼이 생성되어 쿼리에서 개별 속성을 조건으로 사용할 수 있습니다.

### Q2: private 생성자를 사용하면서 EF Core와 호환되게 하려면?
**A**: EF Core는 Reflection을 사용하여 private 생성자도 호출할 수 있습니다. 핵심은 매개변수 없는 생성자를 제공하는 것입니다.

```csharp
public sealed class Email
{
    public string Value { get; private set; }

    // EF Core용: 매개변수 없는 private 생성자
    private Email() => Value = string.Empty;

    // 실제 생성용: 매개변수 있는 private 생성자
    private Email(string value) => Value = value;
}
```

`private set`을 사용하면 EF Core가 값을 설정할 수 있으면서도 외부 코드에서의 변경은 막을 수 있습니다. 완전한 불변성이 필요하면 백킹 필드를 사용하는 방법도 있습니다.

### Q3: OwnsMany로 매핑된 컬렉션의 정렬은 어떻게 하나요?
**A**: `OwnsMany`는 기본적으로 정렬 순서를 보장하지 않습니다. 순서가 중요하면 명시적으로 정렬 컬럼을 추가해야 합니다.

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems, builder =>
    {
        builder.Property<int>("Sequence");
        builder.HasKey("OrderId", "Sequence");
    });
```

또는 도메인 로직에서 정렬하거나, 로드 후 `OrderBy()`를 사용할 수 있습니다.

### Q4: 값 객체의 null 처리는 어떻게 하나요?
**A**: 두 가지 접근 방식이 있습니다.

**필수 값 객체**: `OwnsOne`으로 매핑하면 기본적으로 필수입니다. 부모 엔티티가 있으면 값 객체도 항상 존재합니다.

**선택적 값 객체**: null을 허용하려면 nullable로 선언하고 쿼리에서 처리합니다.
```csharp
public class User
{
    public Address? SecondaryAddress { get; set; }  // nullable
}

// 조회 시
var users = context.Users
    .Where(u => u.SecondaryAddress != null)
    .ToList();
```

값 객체의 모든 컬럼이 NULL이면 EF Core는 해당 값 객체를 null로 로드합니다.

### Q5: 값 객체를 조건으로 쿼리할 수 있나요?
**A**: `OwnsOne`으로 매핑된 값 객체는 속성별로 쿼리할 수 있습니다.

```csharp
// Address의 City로 필터링
var seoulUsers = context.Users
    .Where(u => u.Address.City == "서울")
    .ToList();

// Email의 Value로 검색
var user = context.Users
    .FirstOrDefault(u => u.Email.Value == "hong@example.com");
```

`HasConversion`을 사용한 경우 변환된 값으로 쿼리합니다.
```csharp
// ProductCode.Value가 "EL-001234"인 상품
var product = context.Products
    .FirstOrDefault(p => p.Code.Value == "EL-001234");
```

EF Core는 이를 적절한 SQL로 변환합니다. 단, 복잡한 도메인 로직이 포함된 비교는 클라이언트 평가될 수 있으므로 주의가 필요합니다.

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
| OwnsOnePatternTests | Address, Email 복합 값 객체 영속화 |
| ValueConverterPatternTests | ProductCode 단일 값 변환 |
| OwnsManyPatternTests | OrderLineItem 컬렉션 영속화 |
