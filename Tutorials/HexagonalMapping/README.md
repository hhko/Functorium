# Hexagonal Architecture - Mapping Strategies (매핑 전략)

![Hexagonal Architecture](https://www.happycoders.eu/wp-content/uploads/2022/11/hexagonal-architecture-happycoders-800x446.jpg)

> 이 튜토리얼은 [HappyCoders - Hexagonal Architecture](https://www.happycoders.eu/software-craftsmanship/hexagonal-architecture/)
> 문서의 Mapping 전략 섹션을 기반으로 작성되었습니다.

## 목차

1. [왜 Mapping이 필요한가?](#왜-mapping이-필요한가)
2. [전략 1: Two-Way Mapping (양방향 매핑)](#전략-1-two-way-mapping-양방향-매핑) ✅ 권장
3. [전략 2: One-Way Mapping (단방향 매핑)](#전략-2-one-way-mapping-단방향-매핑)
4. [전략 3: External Configuration (외부 설정)](#전략-3-external-configuration-외부-설정)
5. [전략 4: Weakened Boundaries (약화된 경계)](#전략-4-weakened-boundaries-약화된-경계) ❌ Anti-pattern
6. [REST Adapter에서의 Mapping](#rest-adapter에서의-mapping)
7. [전략 비교](#전략-비교)
8. [프로젝트 구조](#프로젝트-구조)
9. [실행 방법](#실행-방법)

---

## 왜 Mapping이 필요한가?

### 문제 상황

Hexagonal Architecture에서 기술적 세부사항은 애플리케이션 코어로부터 격리되어야 합니다.
그러나 O/R 매퍼(Hibernate, Entity Framework 등)는 엔티티 클래스에 어노테이션을 요구합니다:

- 테이블 이름 (`[Table]`)
- 컬럼 매핑 (`[Column]`)
- 기본 키 생성 (`[Key]`)
- 컬렉션 관계 (`[ForeignKey]`)

이로 인해 딜레마가 발생합니다:
- 도메인 엔티티에 이러한 어노테이션을 배치하면 → **의존성 규칙 위반**
- 어노테이션을 제거하면 → **영속성 계층이 작동하지 않음**

### 해결책: Mapping

Core와 Adapter 사이에 **변환 계층(Mapping)**을 도입하여 이 문제를 해결합니다.

```
+-------------------------------------------------------------+
|                         Adapter                             |
|    (Model with tech annotations: ProductEntity)             |
+-----------------------------+-------------------------------+
                              |
                        +-----v-----+
                        |  Mapper   |  <-- Mapping Layer
                        +-----+-----+
                              |
+-----------------------------v-------------------------------+
|                      Domain Core                            |
|        (Pure business logic: Product, Money, ProductId)     |
+-------------------------------------------------------------+
```

---

## 전략 1: Two-Way Mapping (양방향 매핑) ✅ 권장

> "In my experience, this variant is the most suitable."
> (제 경험상, 이 방식이 가장 적합합니다.) - Sven Woltmann

### 개념

각 Adapter 계층에 **별도의 모델 클래스**를 생성합니다:
- **Domain Core**: 기술 어노테이션 없는 순수 비즈니스 엔티티
- **Adapter**: 필요한 기술 메타데이터를 가진 전용 모델 클래스

**양방향 매핑**으로 두 표현 사이를 변환합니다.

### 데이터 흐름

```
+-------------------------------------------------------------+
|                       REST Adapter                          |
|  CreateProductRequest --> ProductDtoMapper --> Product      |
|  Product --> ProductDtoMapper --> ProductDto                |
+-----------------------------+-------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|                       Domain Core                           |
|                         Product                             |
|                   (Pure business logic)                     |
+-----------------------------+-------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|                    Persistence Adapter                      |
|  Product --> ProductMapper --> ProductEntity                |
|  ProductEntity --> ProductMapper --> Product                |
+-------------------------------------------------------------+
```

### 구현 예제

**Domain (Core)**
```csharp
// Pure domain model without tech annotations
public sealed class Product
{
    public ProductId Id { get; }
    public string Name { get; private set; }
    public Money Price { get; private set; }

    public static Product Create(string name, decimal amount, string currency)
    {
        return new Product(ProductId.New(), name, Money.Create(amount, currency));
    }
}
```

**Adapter (Persistence)**
```csharp
// Adapter-specific model with tech annotations
[Table("products")]
public class ProductEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("currency")]
    public string Currency { get; set; }
}
```

**Mapper**
```csharp
public static class ProductMapper
{
    public static ProductEntity ToEntity(Product product) => new()
    {
        Id = product.Id.Value,
        Name = product.Name,
        Price = product.Price.Amount,
        Currency = product.Price.Currency
    };

    public static Product ToDomain(ProductEntity entity) =>
        Product.Reconstitute(entity.Id, entity.Name, entity.Price, entity.Currency);
}
```

### 장점
- ✅ 명확한 아키텍처 경계 유지
- ✅ Core가 기술 의존성으로부터 완전히 자유로움
- ✅ 각 계층이 독립적으로 진화 가능
- ✅ 책임의 명확한 분리

### 단점
- ⚠️ 더 많은 코드와 유지보수 필요
- ⚠️ 매핑 로직 오버헤드

---

## 전략 2: One-Way Mapping (단방향 매핑)

### 개념

Core에 **인터페이스**를 정의하고, Domain 엔티티와 Adapter 모델 모두 이를 구현합니다.
Core→Adapter 방향만 매핑이 필요합니다.

```csharp
// Interface defined in Core
public interface IProductModel
{
    Guid Id { get; }
    string Name { get; }
    decimal Price { get; }
    string Currency { get; }
}

// Domain implements interface
public class Product : IProductModel { ... }

// Adapter implements interface
public class ProductEntity : IProductModel { ... }
```

### 데이터 흐름

```
+-------------------------------------------------------------+
|                       Domain Core                           |
|                                                             |
|    IProductModel (interface)                                |
|          ^                                                  |
|          | implements                                       |
|          |                                                  |
|    Product (with business logic)                            |
+-----------------------------+-------------------------------+
                |                           ^
                |                           |
    Domain --> Adapter            Adapter --> Domain
    (pass via interface)       (Product.FromModel() required)
                |                           |
                v                           |
+-------------------------------------------------------------+
|                    Persistence Adapter                      |
|                                                             |
|    ProductEntity : IProductModel                            |
|    (tech annotations + interface impl)                      |
+-------------------------------------------------------------+
```

### 제한사항

인터페이스는 **데이터 접근자만** 노출해야 하며, 비즈니스 로직 메서드는 포함하면 안 됩니다.
이로 인해 직관적이지 않은 분리가 발생합니다.

### 저자 평가

> "I don't like this strategy because it is less intuitive and, in my experience, is more overhead."
> (이 전략은 덜 직관적이고, 제 경험상 오히려 더 많은 오버헤드가 발생하기 때문에 선호하지 않습니다.)
> - Sven Woltmann

---

## 전략 3: External Configuration (외부 설정)

### 개념

매핑 메타데이터를 **코드 어노테이션 대신 설정 파일(XML)**에 저장합니다.
이를 통해 Adapter가 Core 도메인 클래스를 직접 사용할 수 있습니다.

### XML 매핑 예시 (NHibernate 스타일)

```xml
<?xml version="1.0" encoding="utf-8"?>
<hibernate-mapping>
  <class name="Product" table="products">
    <id name="Id" column="id">
      <generator class="assigned"/>
    </id>
    <property name="Name" column="name" length="200"/>
    <component name="Price">
      <property name="Amount" column="price"/>
      <property name="Currency" column="currency"/>
    </component>
  </class>
</hibernate-mapping>
```

### EF Core Fluent API 예시

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        entity.ToTable("products");
        entity.HasKey("Id");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
        entity.OwnsOne(e => e.Price, money =>
        {
            money.Property(m => m.Amount).HasColumnName("price");
            money.Property(m => m.Currency).HasColumnName("currency");
        });
    });
}
```

### 저자 평가

> 코드 중복을 피할 수 있지만, "confusing(혼란스럽다)"고 평가됨.
> 어노테이션 기반 접근 방식보다 덜 선호됨.
> - Sven Woltmann

---

## 전략 4: Weakened Boundaries (약화된 경계) ❌ Anti-pattern

### 개념

Core에서 ORM 라이브러리로의 의존성을 허용합니다.
도메인 엔티티에 기술 어노테이션을 직접 배치하여 엄격한 아키텍처 격리를 포기합니다.

### 문제의 코드 예시

```csharp
// BAD: Domain class with tech annotations
[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(200)]
    [Column("product_name")]
    public string Name { get; private set; }

    // Private ctor for EF Core (another tech requirement)
    private Product() { }
}
```

### 깨진 창문 이론 (Broken Windows Theory)

경계가 한 번 약화되면, 이후 위반이 점점 더 정상화되고 증가합니다:

1. 첫 번째 위반: "이건 그냥 어노테이션일 뿐이야..."
   → `[Table]`, `[Column]` 추가

2. 두 번째 위반: "EF Core가 필요로 하니까..."
   → private 파라미터 없는 생성자 추가
   → virtual 네비게이션 프로퍼티 추가

3. 세 번째 위반: "JSON 직렬화도 필요하니까..."
   → `[JsonIgnore]`, `[JsonPropertyName]` 추가

4. 네 번째 위반: "검증도 여기서 하면 편하니까..."
   → `[Required]`, `[Range]`, `[StringLength]` 추가

5. 결국...
   → Domain 클래스가 수십 개의 어노테이션으로 뒤덮임
   → 비즈니스 로직과 기술 관심사의 완전한 혼재

### 저자 평가

> "I would always advise against this option."
> (이 옵션은 항상 권장하지 않습니다.)
> - Sven Woltmann

---

## REST Adapter에서의 Mapping

매핑은 영속성 계층뿐만 아니라 **REST Adapter**에서도 필요합니다:
- 속성 가시성 제어 (기본 키, 타임스탬프 숨김)
- 포맷팅 요구사항 관리 (`@JsonFormat`, `@JsonIgnore`)

```csharp
// REST DTO - Separate model for API contract
public record ProductDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("formattedPrice")]
    public string FormattedPrice => $"{Price:N2} {Currency}";
}
```

---

## 전략 비교

| 전략 | 코드 중복 | 직관성 | 경계 유지 | 권장 |
|------|----------|--------|----------|------|
| **Two-Way Mapping** | 높음 | 높음 | 완벽 | ✅ |
| **One-Way Mapping** | 중간 | 낮음 | 높음 | ⚠️ |
| **External Config** | 없음 | 낮음 | 높음 | ❌ |
| **Weakened Boundaries** | 없음 | 높음 | 없음 | ❌ |

---

## 프로젝트 구조

```
HexagonalMapping/
|-- README.md
|-- Src/
|   |-- HexagonalMapping.Domain/             # Common Domain (Core)
|   |   |-- Entities/
|   |   |   |-- Product.cs
|   |   |   |-- ProductId.cs
|   |   |   +-- Money.cs
|   |   +-- Ports/
|   |       |-- IProductRepository.cs        # Output Port
|   |       +-- IProductService.cs           # Input Port
|   |
|   |-- HexagonalMapping.Strategy1.TwoWayMapping/    # Strategy 1: Two-Way (Recommended)
|   |   |-- Application/
|   |   |   +-- ProductService.cs            # Use Case
|   |   |-- Adapters/
|   |   |   |-- Persistence/
|   |   |   |   |-- ProductEntity.cs         # Adapter-specific model
|   |   |   |   |-- ProductMapper.cs         # Two-Way Mapper
|   |   |   |   |-- ProductDbContext.cs
|   |   |   |   +-- ProductRepository.cs
|   |   |   +-- Rest/
|   |   |       |-- ProductDto.cs            # REST DTO
|   |   |       |-- ProductDtoMapper.cs      # REST Mapper
|   |   |       +-- ProductController.cs     # Driving Adapter
|   |   +-- Program.cs
|   |
|   |-- HexagonalMapping.Strategy2.OneWayMapping/    # Strategy 2: One-Way
|   |   |-- Domain/
|   |   |   |-- IProductModel.cs             # Shared interface
|   |   |   |-- IProductService.cs           # Input Port
|   |   |   |-- Product.cs                   # Implements IProductModel
|   |   |   +-- IProductRepository.cs        # Output Port
|   |   |-- Application/
|   |   |   +-- ProductService.cs            # Use Case
|   |   |-- Adapters/Persistence/
|   |   |   |-- ProductEntity.cs             # Implements IProductModel
|   |   |   |-- ProductDbContext.cs
|   |   |   +-- ProductRepository.cs
|   |   +-- Program.cs
|   |
|   |-- HexagonalMapping.Strategy3.ExternalConfig/   # Strategy 3: External Config
|   |   |-- Application/
|   |   |   +-- ProductService.cs            # Use Case
|   |   |-- Adapters/Persistence/
|   |   |   |-- ProductMapping.xml           # XML ORM config
|   |   |   |-- ProductDbContext.cs          # Fluent API config
|   |   |   +-- ProductRepository.cs
|   |   +-- Program.cs
|   |
|   +-- HexagonalMapping.Strategy4.WeakenedBoundary/ # Strategy 4: Anti-pattern
|       |-- Domain/
|       |   |-- Product.cs                   # With tech annotations (BAD!)
|       |   |-- IProductService.cs           # Input Port
|       |   +-- IProductRepository.cs        # Output Port
|       |-- Application/
|       |   +-- ProductService.cs            # Use Case
|       |-- Adapters/Persistence/
|       |   |-- ProductDbContext.cs
|       |   +-- ProductRepository.cs
|       +-- Program.cs
|
+-- Tests/
    +-- HexagonalMapping.Tests.Unit/
        |-- Strategy1.TwoWayMappingTests.cs
        |-- Strategy2.OneWayMappingTests.cs
        +-- MapperTests.cs
```

### 모든 전략의 공통 구조

모든 매핑 전략은 Hexagonal Architecture의 기본 구조를 따릅니다:

```
+-----------------------------------------------------------------------+
|                        Driving Adapter                                |
|                   (Controller, CLI, etc.)                             |
+----------------------------------+------------------------------------+
                                   | calls
                                   v
+-----------------------------------------------------------------------+
|                         Input Port                                    |
|                      (IProductService)                                |
+----------------------------------+------------------------------------+
                                   | implements
                                   v
+-----------------------------------------------------------------------+
|                    Application (Use Case)                             |
|                       (ProductService)                                |
+----------------------------------+------------------------------------+
                                   | uses
                                   v
+-----------------------------------------------------------------------+
|                        Domain Core                                    |
|              (Entities, Value Objects, Output Ports)                  |
+----------------------------------+------------------------------------+
                                   | implements
                                   v
+-----------------------------------------------------------------------+
|                       Driven Adapter                                  |
|                (Repository, External Services)                        |
+-----------------------------------------------------------------------+
```

**차이점은 Adapter와 Domain 사이의 매핑 전략에만 있습니다.**

---

## 실행 방법

### 빌드

```bash
# 개별 프로젝트 빌드
dotnet build Tutorials/HexagonalMapping/Src/HexagonalMapping.Strategy1.TwoWayMapping/
```

### 각 전략 실행

```bash
# 전략 1: Two-Way Mapping (권장)
dotnet run --project Tutorials/HexagonalMapping/Src/HexagonalMapping.Strategy1.TwoWayMapping/

# 전략 2: One-Way Mapping
dotnet run --project Tutorials/HexagonalMapping/Src/HexagonalMapping.Strategy2.OneWayMapping/

# 전략 3: External Configuration
dotnet run --project Tutorials/HexagonalMapping/Src/HexagonalMapping.Strategy3.ExternalConfig/

# 전략 4: Weakened Boundaries (Anti-pattern)
dotnet run --project Tutorials/HexagonalMapping/Src/HexagonalMapping.Strategy4.WeakenedBoundary/
```

### 테스트 실행

```bash
dotnet test --project Tutorials/HexagonalMapping/Tests/HexagonalMapping.Tests.Unit/
```

---

## 참고 자료

- [HappyCoders - Hexagonal Architecture](https://www.happycoders.eu/software-craftsmanship/hexagonal-architecture/)
- [Alistair Cockburn - Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
