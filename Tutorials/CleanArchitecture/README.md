# Clean Architecture in .NET

> 이 프로젝트는 [Compile & Conquer](https://medium.com/@compileandconquer)의 Clean Architecture 시리즈를 기반으로 구현되었습니다.

## 목차

1. [소개](#소개)
2. [왜 Clean Architecture인가?](#왜-clean-architecture인가)
3. [4개의 레이어 이해하기](#4개의-레이어-이해하기)
4. [프로젝트 구조](#프로젝트-구조)
5. [핵심 원칙: 의존성 규칙](#핵심-원칙-의존성-규칙)
6. [각 레이어 상세 설명](#각-레이어-상세-설명)
7. [테스트 전략](#테스트-전략)
8. [흔한 실수들](#흔한-실수들)
9. [실행 방법](#실행-방법)

---

## 소개

Clean Architecture는 단순한 폴더 구조가 아닙니다. **변화에 대한 두려움 없이 소프트웨어를 발전시킬 수 있는 방법**입니다. 비즈니스 규칙이 인프라스트럭처 변경으로부터 보호받을 때, 기능 추가는 악몽이 아닌 즐거움이 됩니다.

![Clean Architecture Diagram](docs/images/clean-architecture-diagram.png)

---

## 왜 Clean Architecture인가?

### 기존 3계층 아키텍처의 문제점

기존의 3계층 아키텍처(Presentation, Business, Data Access)는 겉보기에는 깔끔해 보이지만, 실제로는:

- `ProductService`가 `DbContext`를 알고 있음
- 컨트롤러에 데이터베이스 로직이 스며들어 있음
- 비즈니스 규칙이 레이어 전체에 흩어져 있음

**진짜 문제는 구조가 아니라 방향입니다.**

3계층 아키텍처에서는 **모든 것이 데이터베이스에 의존**합니다:
- ORM을 바꾸면? 모든 것을 다시 작성해야 합니다
- SQL Server에서 MongoDB로 전환? 비즈니스 로직이 인프라스트럭처 결정에 인질로 잡힙니다

### Clean Architecture의 해답

> **의존성은 반드시 안쪽을 향해야 합니다. 내부 레이어는 외부 레이어에 대해 아무것도 모릅니다.**

---

## 4개의 레이어 이해하기

### 1. Domain Layer (도메인 레이어) - 심장

비즈니스가 코드로 살아있는 곳입니다. `[Table]` 어트리뷰트도 없고, `DbContext`도 없고, `HttpClient`도 없습니다. **순수한 C#으로 표현된 엔티티, 값 객체, 그리고 비즈니스 규칙만** 존재합니다.

**포함되는 것:**
- Entities (Product, Order, Customer)
- Value Objects (Money, Address, Email)
- Domain Events (ProductCreated, OrderPlaced)
- Domain Exceptions
- Repository Interfaces (구현이 아닌 계약만)

**포함되지 않는 것:**
- 데이터베이스 어노테이션 (`[Table]`, `[Column]`)
- 프레임워크 의존성
- DTOs나 ViewModels
- "인프라스트럭처" 냄새가 나는 모든 것

### 2. Application Layer (애플리케이션 레이어) - 오케스트레이터

**Use Case(유스케이스)**를 담고 있습니다. 애플리케이션이 **무엇을 할 수 있는지** 알지만, 데이터가 **어떻게 저장되거나 표시되는지는 모릅니다**.

**포함되는 것:**
- Commands & Queries (CQRS)
- Handlers (유스케이스 구현)
- Input/Output용 DTOs
- 외부 서비스를 위한 Interfaces
- 유효성 검사 로직
- 애플리케이션 레벨 예외

**포함되지 않는 것:**
- HTTP 개념 (상태 코드, 헤더)
- 데이터베이스 쿼리 (raw SQL, LINQ to EF)
- 서드파티 SDK 구현

### 3. Infrastructure Layer (인프라스트럭처 레이어) - 어댑터

추상화가 현실을 만나는 곳입니다. 내부 레이어에서 정의된 모든 인터페이스가 여기서 **구현**됩니다.

**포함되는 것:**
- Repository 구현
- DbContext와 EF Core 설정
- 외부 API 클라이언트
- 메시지 큐 구현
- 파일 시스템 접근
- 이메일/SMS 서비스

**핵심 통찰:** 이 레이어는 **교체 가능**합니다. PostgreSQL을 MongoDB로, SendGrid를 Mailgun으로 바꿔도 Domain이나 Application 레이어를 건드리지 않아야 합니다.

### 4. Presentation Layer (프레젠테이션 레이어) - 진입점

사용자(또는 다른 시스템)가 애플리케이션과 상호작용하는 곳입니다. HTTP 요청을 애플리케이션 명령과 쿼리로 **변환**합니다.

**포함되는 것:**
- Controllers
- Middleware
- Filters
- Request/Response 모델
- API 버전 관리
- 인증 설정

---

## 프로젝트 구조

![Project Structure](docs/images/project-structure.png)

```
CleanArchitecture/
├── CleanArchitecture.sln
├── src/
│   ├── CleanArchitecture.Domain/          # 핵심 비즈니스 로직
│   │   ├── Common/
│   │   │   └── BaseEntity.cs
│   │   ├── Entities/
│   │   │   └── Product.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   └── Email.cs
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   └── Interfaces/
│   │       ├── IProductRepository.cs
│   │       └── IUnitOfWork.cs
│   │
│   ├── CleanArchitecture.Application/     # 유스케이스
│   │   ├── Abstractions/
│   │   │   ├── ICommand.cs
│   │   │   ├── IQuery.cs
│   │   │   ├── ICommandHandler.cs
│   │   │   └── IQueryHandler.cs
│   │   ├── Products/
│   │   │   ├── ProductDto.cs
│   │   │   ├── Create/
│   │   │   ├── GetById/
│   │   │   ├── GetAll/
│   │   │   └── UpdatePrice/
│   │   ├── Services/
│   │   │   ├── IDateTimeProvider.cs
│   │   │   └── IEmailService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── CleanArchitecture.Infrastructure/  # 외부 서비스 구현
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   ├── Configurations/
│   │   │   └── Repositories/
│   │   ├── Services/
│   │   └── DependencyInjection.cs
│   │
│   └── CleanArchitecture.WebAPI/          # API 진입점
│       ├── Controllers/
│       ├── Models/
│       ├── Middleware/
│       └── Program.cs
│
└── tests/
    ├── CleanArchitecture.Domain.Tests/
    └── CleanArchitecture.Application.Tests/
```

---

## 핵심 원칙: 의존성 규칙

### 올바른 접근 방식

```csharp
// Application 레이어는 필요한 것을 정의합니다
public interface IEmailService
{
    Task SendProductCreatedNotificationAsync(Guid productId, string productName);
}

// Handler는 추상화에 의존합니다
public class CreateProductHandler
{
    private readonly IProductRepository _repository;
    private readonly IEmailService _emailService;  // 추상화!

    public async Task<Guid> HandleAsync(CreateProductCommand command)
    {
        var product = new Product(command.Name, price);
        await _repository.AddAsync(product);

        // SendGrid인지, Mailgun인지, 비둘기인지 모르고 상관도 없음
        await _emailService.SendProductCreatedNotificationAsync(
            product.Id, product.Name);

        return product.Id;
    }
}

// Infrastructure가 구현을 제공합니다
public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;

    public async Task SendProductCreatedNotificationAsync(...)
    {
        // SendGrid 특화 구현
    }
}
```

**이 접근 방식의 힘:** `IEmailService`를 모킹하여 `CreateProductHandler`를 단위 테스트할 수 있습니다. 이메일 서버 불필요. 외부 의존성 없음. 순수하고, 빠르고, 신뢰할 수 있는 테스트.

### 잘못된 접근 방식

```csharp
// Application 레이어가 직접 인프라스트럭처를 사용
public class CreateProductHandler
{
    private readonly SendGridClient _emailClient;  // 인프라스트럭처 누출!

    public async Task<Guid> HandleAsync(CreateProductCommand command)
    {
        // ... 제품 생성
        await _emailClient.SendAsync(...);  // SendGrid에 결합됨
    }
}
```

---

## 각 레이어 상세 설명

### Domain Layer - Rich Domain Model

#### 빈약한 모델 (Anemic Model) - 피해야 할 것

```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }    // 누구나 아무 값이나 설정 가능
    public decimal Price { get; set; }  // 음수 가격? 물론이죠!
    public int Stock { get; set; }      // -500 재고? 왜 안 돼요!
}
```

#### Rich Domain Model - 올바른 방법

```csharp
public class Product : BaseEntity
{
    public string Name { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    // 팩토리 메서드로 유효성 검사 내장
    public static Product Create(string name, string sku, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");
        // ...
    }

    // 비즈니스 행위는 엔티티 내부에 존재
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
        StockQuantity += quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {StockQuantity}");
        StockQuantity -= quantity;
    }
}
```

### Value Objects

값 객체는 **불변**이며 **정체성이 없습니다** - 오직 속성으로만 정의됩니다.

```csharp
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO code");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }
}
```

**`record`를 쓰는 이유:** 자동 동등성 비교, 불변성, 더 적은 보일러플레이트.

### Application Layer - CQRS

CQRS는 읽기와 쓰기 작업을 분리합니다:
- **Commands** — 상태 변경 (Create, Update, Delete)
- **Queries** — 상태 읽기 (Get, List, Search)

![Application Layer Structure](docs/images/application-layer-structure.png)

```csharp
// Command
public record CreateProductCommand(
    string Name,
    string Sku,
    decimal Price,
    string Currency) : ICommand<Guid>;

// Handler
public class CreateProductHandler : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken ct)
    {
        // 중복 확인
        if (await _productRepository.ExistsAsync(command.Sku, ct))
            throw new ApplicationException($"Product with SKU '{command.Sku}' already exists");

        // 도메인 엔티티 생성 (비즈니스 규칙은 생성자에서 강제됨)
        var price = new Money(command.Price, command.Currency);
        var product = Product.Create(command.Name, command.Sku, price);

        // 저장
        await _productRepository.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return product.Id;
    }
}
```

### Infrastructure Layer

![Infrastructure Layer Structure](docs/images/infrastructure-layer-structure.png)

EF Core 설정은 Infrastructure에 있습니다 - Domain 엔티티는 깔끔하게 유지됩니다.

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Value Object: Money (owned entity)
        builder.OwnsOne(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2);
            priceBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });
    }
}
```

---

## 테스트 전략

![Testing Strategy](docs/images/testing-strategy.png)

각 레이어는 다른 테스트 접근 방식을 가집니다. Clean Architecture의 아름다움 - **내부 레이어는 모킹이 필요 없습니다**.

### Domain 테스트 - 순수 단위 테스트

```csharp
public class ProductTests
{
    [Fact]
    public void Create_WithValidData_ReturnsProduct()
    {
        var price = new Money(99.99m, "USD");
        var product = Product.Create("Laptop", "LAP-001", price);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Laptop", product.Name);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void RemoveStock_WithInsufficientStock_ThrowsDomainException()
    {
        var product = Product.Create("Laptop", "LAP-001", new Money(99.99m, "USD"));
        product.AddStock(5);

        Assert.Throws<DomainException>(() => product.RemoveStock(10));
    }
}
```

### Application 테스트 - Repository 인터페이스 모킹

```csharp
public class CreateProductHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateProductHandler _handler;

    [Fact]
    public async Task HandleAsync_WithValidCommand_ReturnsProductId()
    {
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, "USD");
        _repositoryMock.Setup(r => r.ExistsAsync("LAP-001", default)).ReturnsAsync(false);

        var result = await _handler.HandleAsync(command);

        Assert.NotEqual(Guid.Empty, result);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), default), Times.Once);
    }
}
```

---

## 흔한 실수들

### 1. 빈약한 도메인 모델 (Anemic Domain Model)

```csharp
// 나쁜 예: 엔티티가 그저 데이터 컨테이너
public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// 로직이 서비스에 흩어져 있음
public class ProductService
{
    public void UpdatePrice(Product product, decimal newPrice)
    {
        if (newPrice <= 0) throw new Exception("Invalid price");
        product.Price = newPrice;
    }
}

// 좋은 예: 엔티티가 행위를 포함
public class Product
{
    public Money Price { get; private set; }

    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new DomainException("Price must be greater than zero");
        Price = newPrice;
    }
}
```

### 2. Domain에 Infrastructure 누출

```csharp
// 나쁜 예: Domain이 EF Core를 알고 있음
public class Product
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; }
}

// 좋은 예: Domain은 깨끗하고, 설정은 Infrastructure에
public class Product : BaseEntity
{
    public string Name { get; private set; }
}

// Infrastructure/Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
    }
}
```

### 3. Handler에서 엔티티 반환

```csharp
// 나쁜 예: 도메인 엔티티 노출
public class GetProductHandler : IQueryHandler<GetProductQuery, Product>
{
    public async Task<Product> HandleAsync(GetProductQuery query)
    {
        return await _repository.GetByIdAsync(query.Id);
    }
}

// 좋은 예: DTO 반환
public class GetProductHandler : IQueryHandler<GetProductQuery, ProductDto?>
{
    public async Task<ProductDto?> HandleAsync(GetProductQuery query)
    {
        var product = await _repository.GetByIdAsync(query.Id);
        if (product is null) return null;

        return new ProductDto(
            product.Id,
            product.Name,
            product.Price.Amount,
            product.Price.Currency);
    }
}
```

### 4. Handler에 비즈니스 로직

```csharp
// 나쁜 예: Handler가 비즈니스 규칙 포함
public async Task<Guid> HandleAsync(CreateOrderCommand command)
{
    var order = new Order();
    foreach (var item in command.Items)
    {
        // Handler에 비즈니스 로직!
        if (item.Quantity > 100)
            throw new Exception("Max 100 items");
        order.Items.Add(new OrderItem(item.ProductId, item.Quantity));
    }
    return order.Id;
}

// 좋은 예: 비즈니스 규칙은 엔티티에
public async Task<Guid> HandleAsync(CreateOrderCommand command)
{
    var order = Order.Create(command.CustomerId);
    foreach (var item in command.Items)
    {
        order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
    }
    return order.Id;
}

// Domain/Entities/Order.cs
public void AddItem(Guid productId, int quantity, Money unitPrice)
{
    if (quantity > 100)
        throw new DomainException("Maximum 100 items per line");
    _items.Add(new OrderItem(productId, quantity, unitPrice));
}
```

---

## 실행 방법

### 필수 조건

- .NET 8.0 이상
- Visual Studio 2022 또는 VS Code

### 빌드

```bash
dotnet build
```

### 테스트 실행

```bash
dotnet test
```

### API 실행

```bash
cd src/CleanArchitecture.WebAPI
dotnet run
```

개발 환경에서는 InMemory 데이터베이스를 사용합니다. Swagger UI는 `https://localhost:{port}/swagger`에서 확인할 수 있습니다.

### API 엔드포인트

| Method | Endpoint | 설명 |
|--------|----------|------|
| POST | `/api/products` | 새 제품 생성 |
| GET | `/api/products` | 모든 제품 조회 |
| GET | `/api/products/{id}` | ID로 제품 조회 |
| PUT | `/api/products/{id}/price` | 제품 가격 수정 |

### 요청 예시

```json
// POST /api/products
{
    "name": "MacBook Pro",
    "sku": "MBP-001",
    "price": 2499.99,
    "currency": "USD"
}
```

---

## 핵심 요약

| 원칙 | 설명 |
|------|------|
| **의존성은 안쪽으로** | Domain은 인프라스트럭처에 대해 아무것도 모름 |
| **Domain은 신성하다** | 프레임워크 의존성 없음, 데이터베이스 어노테이션 없음 |
| **Application이 조율한다** | 유스케이스가 여기에 살고, 비즈니스 규칙은 아님 |
| **Infrastructure가 구현한다** | 교체 가능하고 분리 가능 |
| **Presentation이 번역한다** | HTTP 입력 → 도메인 개념 출력 |
| **엔티티는 행위를 가진다** | 빈약한 모델을 피하라 |
| **각 레이어를 적절히 테스트** | Domain은 모킹이 필요 없음 |

---

## 참고 자료

이 프로젝트는 다음 문서 시리즈를 기반으로 작성되었습니다:

1. [Clean Architecture in .NET: The Foundation That Changes Everything](https://medium.com/@compileandconquer/clean-architecture-in-net-the-foundation-that-changes-everything-6fb4425fa402)
2. [Clean Architecture in .NET: Building the Domain & Application Layers](https://medium.com/@compileandconquer/clean-architecture-in-net-building-the-domain-application-layers-d97c6d4928bc)
3. [Clean Architecture in .NET: Infrastructure & Presentation Layers](https://medium.com/@compileandconquer/clean-architecture-in-net-infrastructure-presentation-layers-69b6fb37ac3f)
4. [Clean Architecture in .NET: Testing, Best Practices & Final Thoughts](https://medium.com/@compileandconquer/clean-architecture-in-net-testing-best-practices-final-thoughts-1ae7316e0004)

---

> **Clean Architecture는 규칙을 맹목적으로 따르는 것이 아닙니다. 변화하는 프레임워크, 데이터베이스, 요구사항의 혼란으로부터 비즈니스 로직을 보호하는 것입니다.**
