# Two-Way Mapping Layered Architecture Tutorial

이 튜토리얼은 [HappyCoders - Hexagonal Architecture](https://www.happycoders.eu/software-craftsmanship/hexagonal-architecture/) 문서에서 제시한 **Two-Way Mapping** 전략을 Functorium 프레임워크로 구현한 예제입니다.

> **"In my experience, this variant is the most suitable."**
> (제 경험상, 이 방식이 가장 적합합니다.)
> — Sven Woltmann, HappyCoders

---

## 목차

1. [Two-Way Mapping이란?](#two-way-mapping이란)
2. [왜 Mapping이 필요한가?](#왜-mapping이-필요한가)
3. [핵심 원칙](#핵심-원칙)
4. [프로젝트 구조](#프로젝트-구조)
5. [핵심 구현](#핵심-구현)
6. [에러 처리 패턴](#에러-처리-패턴)
7. [유효성 검사 패턴](#유효성-검사-패턴)
8. [데이터 흐름](#데이터-흐름)
9. [One-Way Mapping과의 비교](#one-way-mapping과의-비교)
10. [장단점](#장단점)
11. [실행 방법](#실행-방법)
12. [Functorium 패턴](#functorium-패턴)

---

## Two-Way Mapping이란?

Two-Way Mapping은 Hexagonal Architecture에서 **Core(Domain)와 Adapter 사이에 별도의 모델을 유지**하고, **양방향 변환(Mapper)**을 통해 두 계층을 연결하는 전략입니다.

```
┌─────────────────┐                    ┌─────────────────┐
│   Domain Core   │ ◄───── Mapper ────►│     Adapter     │
│                 │                    │                 │
│  Product        │     ToEntity()     │  ProductEntity  │
│  (비즈니스 로직) │ ◄───────────────── │  (기술 어노테이션)│
│                 │                    │                 │
│  - FormattedPrice│     ToDomain()    │  - [Table]      │
│  - Update()     │ ─────────────────► │  - [Column]     │
└─────────────────┘                    └─────────────────┘
```

### Repository 반환 타입: Domain Entity

**Two-Way Mapping의 핵심 특징**: Repository가 **Domain 엔티티(Product)**를 직접 반환합니다.

```csharp
// IProductRepository.cs (Port)
public interface IProductRepository
{
    FinT<IO, Product> GetById(ProductId id);    // Product(Domain) 반환
    FinT<IO, Product> Create(Product product);  // Product(Domain) 반환
}
```

**왜 Product를 반환하는가?**
- Application Layer에서 비즈니스 메서드 즉시 사용 가능
- 추가 변환 없이 `product.FormattedPrice`, `product.Update()` 호출
- Adapter 내부에서 변환이 완료되어 Domain으로 반환

---

## 왜 Mapping이 필요한가?

### 문제 상황

Hexagonal Architecture에서 기술적 세부사항은 애플리케이션 Core로부터 격리되어야 합니다.
그러나 O/R 매퍼(EF Core, Hibernate 등)는 엔티티 클래스에 어노테이션을 요구합니다:

```csharp
// ❌ Domain에 기술 어노테이션이 침투하면 안 됨!
[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
}
```

### 딜레마

- 도메인 엔티티에 어노테이션을 배치하면 → **의존성 규칙 위반**
- 어노테이션을 제거하면 → **영속성 계층이 작동하지 않음**

### 해결책: Two-Way Mapping

Core와 Adapter 사이에 **별도의 모델과 변환 계층(Mapper)**을 도입하여 이 문제를 해결합니다.

---

## 핵심 원칙

| 계층 | 모델 | 특징 |
|------|------|------|
| **Domain Core** | `Product` | 기술 어노테이션 없음, 순수 비즈니스 로직, Value Object 사용 |
| **Adapter** | `ProductEntity` | ORM 어노테이션 포함, 비즈니스 로직 없음, Primitive 타입 |
| **Mapper** | `ProductMapper` | 양방향 변환 수행, Adapter 내부에 위치 |

### 의존성 방향

```
              ┌───────────────────┐
              │    Applications   │
              │    (Use Cases)    │
              └─────────┬─────────┘
                        │ depends on
                        ▼
              ┌───────────────────┐
              │      Domains      │  ◄── 핵심: 모든 화살표가 Domain을 향함
              │  (Product, Port)  │
              └─────────▲─────────┘
                        │ implements
              ┌─────────┴─────────┐
              │      Adapters     │
              │ (ProductEntity,   │
              │  ProductMapper,   │
              │  Repository구현체) │
              └───────────────────┘
```

---

## 프로젝트 구조

```
TwoWayMappingLayered/
├── Src/
│   ├── TwoWayMappingLayered.Domains/              # Domain Core (핵심)
│   │   ├── Entities/
│   │   │   └── Product.cs                         # 순수 도메인 엔티티 (어노테이션 없음)
│   │   ├── ValueObjects/
│   │   │   ├── ProductId.cs                       # ID Value Object
│   │   │   └── Money.cs                           # Money Value Object
│   │   └── Repositories/
│   │       └── IProductRepository.cs              # Output Port (Domain 반환)
│   │
│   ├── TwoWayMappingLayered.Applications/         # Application Layer
│   │   ├── Commands/
│   │   │   ├── CreateProductCommand.cs            # 상품 생성
│   │   │   └── UpdateProductCommand.cs            # 상품 수정
│   │   └── Queries/
│   │       ├── GetProductByIdQuery.cs             # 단건 조회
│   │       └── GetAllProductsQuery.cs             # 전체 조회
│   │
│   ├── TwoWayMappingLayered.Adapters.Persistence/ # Persistence Adapter
│   │   ├── Entities/
│   │   │   └── ProductEntity.cs                   # EF Core 어노테이션 포함
│   │   ├── Mappers/
│   │   │   └── ProductMapper.cs                   # ⭐ 양방향 매퍼 (Two-Way 핵심!)
│   │   └── Repositories/
│   │       └── InMemoryProductRepository.cs       # Port 구현체
│   │
│   ├── TwoWayMappingLayered.Adapters.Presentation/# Presentation Adapter
│   │   └── Endpoints/
│   │       ├── CreateProductEndpoint.cs
│   │       ├── GetProductByIdEndpoint.cs
│   │       ├── GetAllProductsEndpoint.cs
│   │       └── UpdateProductEndpoint.cs
│   │
│   ├── TwoWayMappingLayered.Adapters.Infrastructure/
│   │   └── Abstractions/Registrations/            # DI 등록
│   │
│   └── TwoWayMappingLayered/                      # Web API Host
│       └── Program.cs
│
└── README.md
```

---

## 핵심 구현

### 1. Domain Entity (기술 어노테이션 없음)

```csharp
// TwoWayMappingLayered.Domains/Entities/Product.cs

public sealed class Product
{
    public ProductId Id { get; }                    // Value Object
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }        // Value Object
    public int StockQuantity { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    // ✅ 비즈니스 표현 로직 - Domain에만 존재
    public string FormattedPrice => Price.Formatted;

    // ✅ 비즈니스 메서드 - Fluent API 패턴
    public Product Update(string name, string description, Money price, int stockQuantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(stockQuantity);

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        UpdatedAt = DateTime.UtcNow;
        return this;  // LINQ let 표현식 지원
    }

    // ✅ Factory Method
    public static Product Create(string name, string description, Money price, int stockQuantity)
    {
        return new Product(ProductId.New(), name, description, price, stockQuantity, DateTime.UtcNow);
    }

    // ✅ Reconstitute - Adapter에서 Domain 복원 시 사용
    public static Product Reconstitute(
        ProductId id, string name, string description, Money price,
        int stockQuantity, DateTime createdAt, DateTime? updatedAt)
    {
        return new Product(id, name, description, price, stockQuantity, createdAt, updatedAt);
    }
}
```

**특징:**
- `[Table]`, `[Column]` 등 **기술 어노테이션 없음**
- **Value Object** 사용 (`ProductId`, `Money`)
- **비즈니스 메서드** 포함 (`Update`, `FormattedPrice`)
- **Reconstitute** 메서드로 Adapter에서 복원 지원

---

### 2. Adapter Entity (기술 어노테이션 포함)

```csharp
// TwoWayMappingLayered.Adapters.Persistence/Entities/ProductEntity.cs

[Table("products")]
public class ProductEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }                    // Primitive 타입

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("price")]
    public decimal Price { get; set; }              // Money.Amount 분해

    [Required]
    [MaxLength(3)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";   // Money.Currency 분해

    [Column("stock_quantity")]
    public int StockQuantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
```

**특징:**
- **EF Core 어노테이션** 포함 (`[Table]`, `[Key]`, `[Column]`, `[Required]`, `[MaxLength]`)
- **비즈니스 로직 없음**
- **Primitive 타입** 사용 (Value Object 분해)

---

### 3. 양방향 Mapper (Two-Way Mapping 핵심)

```csharp
// TwoWayMappingLayered.Adapters.Persistence/Mappers/ProductMapper.cs

public static class ProductMapper
{
    /// <summary>
    /// Domain → Adapter (저장 시)
    /// Value Object를 primitive 값으로 분해
    /// </summary>
    public static ProductEntity ToEntity(Product product) => new()
    {
        Id = (Guid)product.Id,              // implicit operator
        Name = product.Name,
        Description = product.Description,
        Price = product.Price.Amount,       // Money 분해
        Currency = product.Price.Currency,  // Money 분해
        StockQuantity = product.StockQuantity,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };

    /// <summary>
    /// Adapter → Domain (조회 시)
    /// primitive 값을 Value Object로 재구성
    /// </summary>
    public static Product ToDomain(ProductEntity entity) =>
        Product.Reconstitute(
            ProductId.FromValue(entity.Id),                      // Value Object 복원
            entity.Name,
            entity.Description,
            Money.FromValues(entity.Price, entity.Currency),     // Money 복원
            entity.StockQuantity,
            entity.CreatedAt,
            entity.UpdatedAt);

    /// <summary>
    /// 기존 엔티티 업데이트 (Domain → Adapter)
    /// EF Core Change Tracking 활용
    /// </summary>
    public static void UpdateEntity(ProductEntity entity, Product product)
    {
        entity.Name = product.Name;
        entity.Description = product.Description;
        entity.Price = product.Price.Amount;
        entity.Currency = product.Price.Currency;
        entity.StockQuantity = product.StockQuantity;
        entity.UpdatedAt = product.UpdatedAt;
    }
}
```

**변환 방향:**

| 메서드 | 방향 | 용도 | Value Object 처리 |
|--------|------|------|------------------|
| `ToEntity` | Domain → Adapter | 저장 시 | 분해 (Amount, Currency) |
| `ToDomain` | Adapter → Domain | 조회 시 | 재구성 (Money.FromValues) |
| `UpdateEntity` | Domain → Adapter | 업데이트 시 | 분해 |

---

### 4. Repository (Port 구현)

#### Port (Domain Layer)

```csharp
// TwoWayMappingLayered.Domains/Repositories/IProductRepository.cs

public interface IProductRepository : IPort
{
    FinT<IO, Product> Create(Product product);      // ✅ Product 반환
    FinT<IO, Product> GetById(ProductId id);        // ✅ Product 반환
    FinT<IO, Seq<Product>> GetAll();                // ✅ Product 컬렉션 반환
    FinT<IO, Product> Update(Product product);      // ✅ Product 반환
    FinT<IO, bool> ExistsByName(string name);
}
```

#### 구현체 (Adapter Layer)

```csharp
// TwoWayMappingLayered.Adapters.Persistence/Repositories/InMemoryProductRepository.cs

[GeneratePortObservable]
public class InMemoryProductRepository : IProductRepository
{
    // ⭐ 내부 저장소: Adapter 모델(ProductEntity) 사용
    private static readonly ConcurrentDictionary<Guid, ProductEntity> _products = new();

    public virtual FinT<IO, Product> Create(Product product)
    {
        return IO.lift(() =>
        {
            // ⭐ Domain → Adapter 변환 후 저장
            ProductEntity entity = ProductMapper.ToEntity(product);
            _products[entity.Id] = entity;
            return Fin.Succ(product);  // Domain 반환
        });
    }

    public virtual FinT<IO, Product> GetById(ProductId id)
    {
        return IO.lift(() =>
        {
            if (_products.TryGetValue((Guid)id, out ProductEntity? entity))
            {
                // ⭐ Adapter → Domain 변환 후 반환
                Product product = ProductMapper.ToDomain(entity);
                return Fin.Succ(product);  // Domain 반환
            }
            return Fin.Fail<Product>(Error.New($"상품을 찾을 수 없습니다"));
        });
    }

    public virtual FinT<IO, Product> Update(Product product)
    {
        return IO.lift(() =>
        {
            if (!_products.TryGetValue((Guid)product.Id, out ProductEntity? entity))
            {
                return Fin.Fail<Product>(Error.New($"상품을 찾을 수 없습니다"));
            }

            // ⭐ Domain → Adapter: 기존 엔티티 업데이트
            ProductMapper.UpdateEntity(entity, product);
            return Fin.Succ(product);  // Domain 반환
        });
    }
}
```

**핵심 포인트:**
- Repository **내부**는 `ProductEntity` (Adapter 모델) 사용
- Repository **외부 인터페이스**는 `Product` (Domain 모델) 반환
- **Mapper로 양방향 변환** 수행

---

### 5. Application Layer (Use Case)

```csharp
// TwoWayMappingLayered.Applications/Commands/CreateProductCommand.cs

public sealed class Usecase : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
    {
        // Validator에서 검증 완료 - 안전하게 Value Object 생성
        Money price = Money.FromValues(request.Price, request.Currency.ToUpperInvariant());

        // LINQ 쿼리 표현식: Repository가 Product(Domain) 반환
        FinT<IO, Response> usecase =
            from exists in _productRepository.ExistsByName(request.Name)
            from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
            from product in _productRepository.Create(
                Product.Create(request.Name, request.Description, price, request.StockQuantity))
            select new Response(
                (Guid)product.Id,
                product.Name,
                product.Description,
                product.FormattedPrice,  // ⭐ Two-Way: 비즈니스 메서드 즉시 사용!
                product.StockQuantity,
                product.CreatedAt);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

```csharp
// TwoWayMappingLayered.Applications/Commands/UpdateProductCommand.cs

FinT<IO, Response> usecase =
    from product in _productRepository.GetById(productId)
    let updatedProduct = product.Update(request.Name, request.Description, price, request.StockQuantity)
    from saved in _productRepository.Update(updatedProduct)
    select new Response(
        (Guid)saved.Id,
        saved.Name,
        saved.Description,
        saved.FormattedPrice,  // ⭐ Two-Way: 비즈니스 메서드 사용
        saved.StockQuantity,
        saved.UpdatedAt);
```

**Two-Way Mapping의 장점:**
- Repository가 `Product` (Domain) 반환
- **비즈니스 메서드** (`FormattedPrice`, `Update`) **즉시 사용 가능**
- 추가 변환 없이 도메인 로직 활용

---

## 에러 처리 패턴

Functorium은 계층별로 일관된 에러 처리 패턴을 제공합니다.

### ApplicationError (Use Case 레이어)

Application Layer에서 비즈니스 로직 에러를 표현할 때 사용합니다.

```csharp
// CreateProductCommand.cs
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;

// LINQ 쿼리 표현식 내에서 guard와 함께 사용
FinT<IO, Response> usecase =
    from exists in _productRepository.ExistsByName(request.Name)
    from _ in guard(!exists, ApplicationError.For<Usecase>(
        new AlreadyExists(),
        request.Name,
        $"상품명이 이미 존재합니다: '{request.Name}'"))
    from product in _productRepository.Create(...)
    select new Response(...);
```

**생성되는 에러 코드 형식:**
```
ApplicationErrors.Usecase.AlreadyExists
```

**제공되는 에러 타입 (`ApplicationErrorType`):**

| 타입 | 설명 |
|------|------|
| `AlreadyExists` | 값이 이미 존재함 |
| `NotFound` | 값을 찾을 수 없음 |
| `ValidationFailed(PropertyName?)` | 검증 실패 |
| `BusinessRuleViolated(RuleName?)` | 비즈니스 규칙 위반 |
| `Unauthorized` | 인증되지 않음 |
| `Forbidden` | 접근 금지 |
| `ConcurrencyConflict` | 동시성 충돌 |
| `Custom(Name)` | 커스텀 에러 |

### AdapterError (Adapter 레이어)

Adapter Layer에서 인프라 관련 에러를 표현할 때 사용합니다.

```csharp
// InMemoryProductRepository.cs
using Functorium.Adapters.Errors;
using static Functorium.Adapters.Errors.AdapterErrorType;

public virtual FinT<IO, Product> GetById(ProductId id)
{
    return IO.lift(() =>
    {
        if (_products.TryGetValue((Guid)id, out ProductEntity? entity))
        {
            return Fin.Succ(ProductMapper.ToDomain(entity));
        }

        return Fin.Fail<Product>(AdapterError.For<InMemoryProductRepository>(
            new NotFound(),
            ((Guid)id).ToString(),
            $"상품 ID '{(Guid)id}'을(를) 찾을 수 없습니다"));
    });
}
```

**생성되는 에러 코드 형식:**
```
AdapterErrors.InMemoryProductRepository.NotFound
```

**제공되는 에러 타입 (`AdapterErrorType`):**

| 타입 | 설명 |
|------|------|
| `NotFound` | 값을 찾을 수 없음 |
| `AlreadyExists` | 값이 이미 존재함 |
| `ConnectionFailed(Target?)` | 연결 실패 |
| `Timeout(Duration?)` | 타임아웃 |
| `Serialization(Format?)` | 직렬화 실패 |
| `Deserialization(Format?)` | 역직렬화 실패 |
| `ExternalServiceUnavailable(ServiceName?)` | 외부 서비스 사용 불가 |
| `Custom(Name)` | 커스텀 에러 |

### 에러 계층 구조

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  FinResponse<T> → HTTP Status Code 매핑                     │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  ApplicationError.For<TUsecase>(errorType, value, message)  │
│  → "ApplicationErrors.{Usecase}.{ErrorType}"                │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      Adapter Layer                          │
│  AdapterError.For<TAdapter>(errorType, value, message)      │
│  → "AdapterErrors.{Adapter}.{ErrorType}"                    │
└─────────────────────────────────────────────────────────────┘
```

---

## 유효성 검사 패턴

Functorium은 **Value Object의 검증 로직**과 **FluentValidation**을 통합하는 패턴을 제공합니다.

### 복합 값 객체의 개별 필드 검증

Money처럼 여러 필드를 가진 Value Object는 **개별 검증 메서드**를 public으로 노출합니다.

```csharp
// Money.cs (Domain Layer)
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    /// <summary>
    /// Money 검증 (병렬 검증 - 모든 오류 수집)
    /// </summary>
    public static Validation<Error, Money> Validate(decimal amount, string currency) =>
        (ValidateAmount(amount), ValidateCurrency(currency))
            .Apply((a, c) => new Money(a, c))
            .As();

    /// <summary>
    /// Amount 개별 검증 - FluentValidation 통합용
    /// </summary>
    public static Validation<Error, decimal> ValidateAmount(decimal amount) =>
        Validate<Money>.NonNegative(amount);

    /// <summary>
    /// Currency 개별 검증 - FluentValidation 통합용
    /// </summary>
    public static Validation<Error, string> ValidateCurrency(string currency) =>
        Validate<Money>.NotEmpty(currency ?? "")
            .ThenExactLength(3)
            .ThenNormalize(v => v.ToUpperInvariant());
}
```

### FluentValidation과 Value Object 통합

`MustSatisfyValidation` 확장 메서드로 Value Object의 검증 메서드를 직접 참조합니다.
C# 14의 extension members 문법을 사용하여 타입 추론이 완벽하게 작동합니다.

```csharp
// CreateProductCommand.cs (Application Layer)
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("상품명은 필수입니다")
            .MaximumLength(100).WithMessage("상품명은 100자를 초과할 수 없습니다");

        // Money Value Object 검증: Amount
        // MustSatisfyValidation: C# 14 타입 추론 - 명시적 타입 불필요
        RuleFor(x => x.Price)
            .MustSatisfyValidation(Money.ValidateAmount);

        // Money Value Object 검증: Currency
        // MustSatisfyValidation: C# 14 타입 추론 - 명시적 타입 불필요
        RuleFor(x => x.Currency)
            .MustSatisfyValidation(Money.ValidateCurrency);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
    }
}
```

### C# 14 Extension Members와 타입 추론

Functorium은 C# 14의 extension members 문법을 사용하여 타입 추론을 개선합니다.

#### MustSatisfyValidation (입력 타입 == 출력 타입)

입력 타입과 검증 결과 타입이 **동일**한 경우 사용합니다. 타입 추론이 완벽하게 작동합니다.

```csharp
// decimal → Validation<Error, decimal> (입력 == 출력)
RuleFor(x => x.Price)
    .MustSatisfyValidation(Money.ValidateAmount);

// string → Validation<Error, string> (입력 == 출력)
RuleFor(x => x.Currency)
    .MustSatisfyValidation(Money.ValidateCurrency);

// Guid → Validation<Error, Guid> (입력 == 출력)
RuleFor(x => x.ProductId)
    .MustSatisfyValidation(ProductId.Validate);
```

#### MustSatisfyValidationOf<TValueObject> (입력 타입 != 출력 타입)

입력 타입과 검증 결과 타입이 **다른** 경우 사용합니다. TValueObject 타입만 명시합니다.

```csharp
// string → Validation<Error, ProductName> (입력 != 출력)
RuleFor(x => x.Name)
    .MustSatisfyValidationOf<ProductName>(ProductName.Validate);

// string → Validation<Error, Email> (입력 != 출력)
RuleFor(x => x.Email)
    .MustSatisfyValidationOf<Email>(Email.Validate);
```

#### 이전 방식과 비교

```csharp
// ❌ 이전 방식: 3개의 타입 파라미터를 명시적으로 지정
RuleFor(x => x.Price)
    .MustSatisfyValueObjectValidation<Request, decimal, decimal>(Money.ValidateAmount);

// ✅ 새 방식: 타입 추론 작동 - 명시적 타입 불필요
RuleFor(x => x.Price)
    .MustSatisfyValidation(Money.ValidateAmount);
```

### 검증 로직 재사용의 장점

| 기존 방식 | 개선된 방식 |
|-----------|------------|
| Validator에서 검증 로직 중복 작성 | Value Object 메서드 직접 참조 |
| 검증 규칙 변경 시 여러 곳 수정 | 단일 소스 (Single Source of Truth) |
| Money 내부 구현과 Validator 불일치 위험 | 항상 동일한 검증 보장 |
| 3개의 타입 파라미터 명시 필요 | C# 14 타입 추론으로 간결한 코드 |

```csharp
// ❌ 기존 방식: 검증 로직 중복, 타입 명시 필요
RuleFor(x => x.Price)
    .MustSatisfyValueObjectValidation<Request, decimal, decimal>(
        price => DomainValidate.NonNegative(price));  // Money.ValidateAmount와 중복!

// ✅ 개선된 방식: Value Object 메서드 직접 참조 + 타입 추론
RuleFor(x => x.Price)
    .MustSatisfyValidation(Money.ValidateAmount);  // 타입 명시 불필요!
```

### 검증 흐름

```
┌─────────────────────────────────────────────────────────────┐
│                      Request 수신                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              FluentValidation Validator                     │
│  MustSatisfyValueObjectValidation                          │
│    → Money.ValidateAmount(price)                            │
│    → Money.ValidateCurrency(currency)                       │
│    → ProductId.Validate(productId)                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ (검증 통과)
┌─────────────────────────────────────────────────────────────┐
│                    Use Case Handler                         │
│  검증 완료된 데이터로 Value Object 생성                      │
│  Money.FromValues(price, currency)                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 데이터 흐름

### 생성 (Create) 흐름

```
[Client Request]
       │
       ▼
┌─────────────────────┐
│   Presentation      │  Request DTO 수신
│   (Endpoint)        │
└─────────────────────┘
       │
       ▼
┌─────────────────────┐
│   Application       │  Product.Create() 호출
│   (Command)         │  Domain Entity 생성
└─────────────────────┘
       │
       ▼
┌─────────────────────┐
│   Repository        │  ProductMapper.ToEntity()
│   (Adapter 구현체)   │  Domain → Adapter 변환
│                     │  ProductEntity 저장
└─────────────────────┘
       │
       ▼
┌─────────────────────┐
│   Persistence       │  ProductEntity 저장
│   (Database)        │  (EF Core 어노테이션 활용)
└─────────────────────┘
```

### 조회 (Read) 흐름

```
┌─────────────────────┐
│   Persistence       │  ProductEntity 조회
│   (Database)        │
└─────────────────────┘
       │
       ▼
┌─────────────────────┐
│   Repository        │  ProductMapper.ToDomain()
│   (Adapter 구현체)   │  Adapter → Domain 변환
│                     │  Product 반환
└─────────────────────┘
       │
       ▼
┌─────────────────────┐
│   Application       │  product.FormattedPrice
│   (Query)           │  비즈니스 메서드 즉시 사용
└─────────────────────┘
       │
       ▼
┌─────────────────────┐
│   Presentation      │  Response DTO 생성
│   (Endpoint)        │
└─────────────────────┘
       │
       ▼
[Client Response]
```

---

## One-Way Mapping과의 비교

| 항목 | Two-Way Mapping | One-Way Mapping |
|------|-----------------|-----------------|
| **Repository 반환** | `Product` (Domain Entity) | `IProductModel` (Interface) |
| **비즈니스 메서드** | ✅ 즉시 사용 가능 | ⚠️ 변환 후 사용 |
| **변환 방향** | 양방향 (ToEntity, ToDomain) | Core → Adapter 단방향 |
| **직관성** | 높음 | 낮음 |
| **저자 평가** | ✅ "Most suitable" | ⚠️ "More overhead" |

### 코드 비교

```csharp
// ✅ Two-Way Mapping: 비즈니스 메서드 즉시 사용
Product product = await repository.GetById(id);
string price = product.FormattedPrice;     // 즉시 사용!
product.Update(name, description, price, stockQuantity);  // 즉시 호출!

// ⚠️ One-Way Mapping: 인터페이스로 반환, 비즈니스 메서드 없음
IProductModel model = await repository.GetById(id);
// model.FormattedPrice  ← 사용 불가! (인터페이스에 없음)
Product product = Product.FromModel(model);  // 변환 필요
string price = product.FormattedPrice;
```

---

## 장단점

### 장점

| 항목 | 설명 |
|------|------|
| **명확한 아키텍처 경계** | Domain Core가 기술 의존성으로부터 완전히 격리 |
| **독립적인 변경** | ORM 버전 변경이 Domain에 영향 없음 |
| **테스트 용이성** | Domain을 순수 단위 테스트 가능 |
| **비즈니스 로직 집중** | Domain Entity에 비즈니스 메서드 자유롭게 추가 |
| **Value Object 활용** | ProductId, Money 등 도메인 개념 명확히 표현 |
| **즉시 사용 가능** | Repository가 Domain 반환하므로 비즈니스 메서드 즉시 호출 |

### 단점

| 항목 | 설명 |
|------|------|
| **코드 중복** | 유사한 모델을 두 계층에 유지 (Product, ProductEntity) |
| **Mapper 유지보수** | 필드 추가 시 Mapper도 함께 수정 필요 |
| **변환 오버헤드** | 매 요청마다 객체 변환 발생 |
| **개발 시간 증가** | 초기 설정에 더 많은 코드 필요 |

---

## 실행 방법

### 빌드

```bash
dotnet build Tutorials/TwoWayMappingLayered/
```

### 실행

```bash
dotnet run --project Tutorials/TwoWayMappingLayered/Src/TwoWayMappingLayered/
```

### API 테스트

```bash
# 상품 생성
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"노트북","description":"고성능 노트북","price":1500000,"currency":"KRW","stockQuantity":10}'

# 상품 단건 조회
curl http://localhost:5000/api/products/{id}

# 상품 전체 조회
curl http://localhost:5000/api/products

# 상품 수정
curl -X PUT http://localhost:5000/api/products/{id} \
  -H "Content-Type: application/json" \
  -d '{"productId":"{id}","name":"게이밍 노트북","description":"RTX 4090 탑재","price":2500000,"currency":"KRW","stockQuantity":5}'
```

---

## Functorium 패턴

이 튜토리얼은 Functorium의 다음 패턴을 사용합니다:

| 패턴 | 설명 |
|------|------|
| **FinT<IO, T>** | 부수효과를 포함한 함수형 결과 타입 |
| **LINQ 쿼리 표현식** | 모나드 체이닝 (`from ... in ... select`) |
| **Value Objects** | `SimpleValueObject<T>`, `ValueObject` 기반 |
| **Validate<T>** | 타입 안전한 검증 (`NonNegative`, `ThenExactLength`) |
| **CQRS** | Command/Query 분리 (`ICommandUsecase`, `IQueryUsecase`) |
| **FinResponse<T>** | 성공/실패를 표현하는 응답 타입 |
| **[GeneratePortObservable]** | 관찰 가능성 파이프라인 자동 생성 |
| **MustSatisfyValidation** | FluentValidation과 Value Object 검증 통합 (입력 == 출력 타입) |
| **MustSatisfyValidationOf<T>** | FluentValidation과 Value Object 검증 통합 (입력 != 출력 타입) |

---

## 참고 자료

- [HappyCoders - Hexagonal Architecture](https://www.happycoders.eu/software-craftsmanship/hexagonal-architecture/)
- [Functorium Value Object 가이드](../../.claude/guides/valueobject-implementation-guide.md)
- [LanguageExt - Functional Programming in C#](https://github.com/louthy/language-ext)
