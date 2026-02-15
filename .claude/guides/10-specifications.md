# Specification 패턴

이 문서는 Functorium 프레임워크에서 Specification 패턴을 정의하고 사용하는 방법을 설명합니다.

## 목차

- [1. 왜 Specification 패턴인가 (WHY)](#1-왜-specification-패턴인가-why)
- [2. Specification이란 무엇인가 (WHAT)](#2-specification이란-무엇인가-what)
- [3. Specification 구현 (HOW)](#3-specification-구현-how)
- [4. Repository에서 사용 (HOW)](#4-repository에서-사용-how)
- [5. Usecase에서 사용 (HOW)](#5-usecase에서-사용-how)
- [6. 테스트 패턴](#6-테스트-패턴)
- [7. 체크리스트](#7-체크리스트)
- [참고 문서](#참고-문서)

---

## 1. 왜 Specification 패턴인가 (WHY)

Specification 패턴은 DDD에서 **비즈니스 규칙을 캡슐화하고 조합 가능하게** 만드는 빌딩블록입니다.

### Specification이 해결하는 문제

**비즈니스 규칙 캡슐화**:
"가격이 100원 이상 200원 이하", "재고가 5개 미만" 같은 조건이 Repository 메서드에 흩어지면 재사용이 어렵습니다. Specification은 이 조건을 독립적인 도메인 객체로 캡슐화합니다.

**Repository 메서드 폭발 방지**:
새 필터 조건마다 Repository에 메서드를 추가하면 인터페이스가 비대해집니다. Specification을 받는 `Exists(spec)`/`FindAll(spec)` 메서드 하나로 모든 조건을 처리합니다.

**조합 가능성**:
`And`, `Or`, `Not` 조합으로 단순한 Specification을 복잡한 비즈니스 규칙으로 합성할 수 있습니다. 각 Specification은 단일 책임을 유지합니다.

### Specification 없이 vs 있을 때

```csharp
// ❌ Specification 없이: 조건마다 Repository 메서드 추가
public interface IProductRepository
{
    FinT<IO, bool> ExistsByName(ProductName name, ProductId? excludeId = null);
    FinT<IO, Seq<Product>> FindByPriceRange(Money min, Money max);
    FinT<IO, Seq<Product>> FindByLowStock(Quantity threshold);
    FinT<IO, Seq<Product>> FindByPriceRangeAndLowStock(Money min, Money max, Quantity threshold);
    // 조합이 늘어날수록 메서드가 폭발적으로 증가...
}

// ✅ Specification 사용: 범용 메서드 + 조합
public interface IProductRepository
{
    FinT<IO, bool> Exists(Specification<Product> spec);
    FinT<IO, Seq<Product>> FindAll(Specification<Product> spec);
}
```

---

## 2. Specification이란 무엇인가 (WHAT)

### `Specification<T>` 추상 클래스

`Functorium.Domains.Specifications` 네임스페이스에 위치합니다.

```csharp
public abstract class Specification<T>
{
    // 엔터티가 조건을 만족하는지 확인
    public abstract bool IsSatisfiedBy(T entity);

    // 메서드 조합
    public Specification<T> And(Specification<T> other);
    public Specification<T> Or(Specification<T> other);
    public Specification<T> Not();

    // 연산자 오버로드
    public static Specification<T> operator &(Specification<T> left, Specification<T> right);
    public static Specification<T> operator |(Specification<T> left, Specification<T> right);
    public static Specification<T> operator !(Specification<T> spec);
}
```

### 조합 방식

메서드와 연산자 두 가지 스타일을 지원합니다:

```csharp
// 메서드 스타일
var spec = priceRange.And(lowStock.Not());

// 연산자 스타일 (동일한 결과)
var spec = priceRange & !lowStock;
```

### 내부 조합 클래스

조합 클래스는 `internal sealed`로 프레임워크 내부에서만 사용됩니다:

| 클래스 | 생성 방법 | 동작 |
|--------|----------|------|
| `AndSpecification<T>` | `And()` / `&` | 양쪽 모두 만족 시 `true` |
| `OrSpecification<T>` | `Or()` / `\|` | 한쪽이라도 만족 시 `true` |
| `NotSpecification<T>` | `Not()` / `!` | 반전 |

### Functorium 타입 계층에서의 위치

```
Functorium.Domains.Specifications
├── Specification<T>          (추상 기반 클래스)
│   ├── IsSatisfiedBy()       (추상 메서드)
│   ├── And() / Or() / Not()  (조합 메서드)
│   └── & / | / !             (연산자 오버로드)
├── AndSpecification<T>       (internal sealed)
├── OrSpecification<T>        (internal sealed)
└── NotSpecification<T>       (internal sealed)
```

---

## 3. Specification 구현 (HOW)

### 폴더 구조

```
LayeredArch.Domain/
└── AggregateRoots/
    └── Products/
        ├── Product.cs
        ├── Ports/
        │   └── IProductRepository.cs
        └── Specifications/           ← Aggregate 하위에 배치
            ├── ProductNameUniqueSpec.cs
            ├── ProductPriceRangeSpec.cs
            └── ProductLowStockSpec.cs
```

**네임스페이스**: `{프로젝트}.Domain.AggregateRoots.{Aggregate}.Specifications`

### 기본 구조 (template)

```csharp
using Functorium.Domains.Specifications;

namespace {프로젝트}.Domain.AggregateRoots.{Aggregate}.Specifications;

public sealed class {Aggregate}{조건}Spec : Specification<{Aggregate}>
{
    // public 프로퍼티: EfCore adapter에서 pattern-match SQL 최적화에 사용
    public {ValueObjectType} {PropertyName} { get; }

    public {Aggregate}{조건}Spec({ValueObjectType} {paramName})
    {
        {PropertyName} = {paramName};
    }

    public override bool IsSatisfiedBy({Aggregate} entity) =>
        // 조건 로직
}
```

### 실전 예제

#### 상품명 중복 확인 (ProductNameUniqueSpec)

```csharp
public sealed class ProductNameUniqueSpec : Specification<Product>
{
    public ProductName Name { get; }
    public ProductId? ExcludeId { get; }

    public ProductNameUniqueSpec(ProductName name, ProductId? excludeId = null)
    {
        Name = name;
        ExcludeId = excludeId;
    }

    public override bool IsSatisfiedBy(Product product) =>
        (string)product.Name == (string)Name &&
        (ExcludeId is null || product.Id != ExcludeId.Value);
}
```

#### 가격 범위 (ProductPriceRangeSpec)

```csharp
public sealed class ProductPriceRangeSpec : Specification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money minPrice, Money maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public override bool IsSatisfiedBy(Product product) =>
        product.Price >= MinPrice && product.Price <= MaxPrice;
}
```

#### 재고 부족 (ProductLowStockSpec)

```csharp
public sealed class ProductLowStockSpec : Specification<Product>
{
    public Quantity Threshold { get; }

    public ProductLowStockSpec(Quantity threshold)
    {
        Threshold = threshold;
    }

    public override bool IsSatisfiedBy(Product product) =>
        product.StockQuantity < Threshold;
}
```

### public 프로퍼티 패턴 (EfCore SQL 최적화용)

Specification의 파라미터를 **public 프로퍼티**로 노출하면, EfCore Adapter에서 `switch` pattern-match로 SQL 최적화 쿼리를 생성할 수 있습니다:

```csharp
// ✅ public 프로퍼티 — EfCore에서 pattern-match 가능
public Money MinPrice { get; }
public Money MaxPrice { get; }

// ❌ private 필드 — EfCore에서 접근 불가, 클라이언트 필터링으로 폴백
private readonly Money _minPrice;
```

---

## 4. Repository에서 사용 (HOW)

### Port 정의 (Domain Layer)

Repository 인터페이스에 Specification을 받는 메서드를 추가합니다:

```csharp
public interface IProductRepository : IRepository<Product, ProductId>
{
    // Specification 기반 메서드
    FinT<IO, bool> Exists(Specification<Product> spec);
    FinT<IO, Seq<Product>> FindAll(Specification<Product> spec);
}
```

### InMemory 구현 패턴

`IsSatisfiedBy()`를 직접 사용합니다:

```csharp
public virtual FinT<IO, bool> Exists(Specification<Product> spec)
{
    return IO.lift(() =>
    {
        bool exists = _products.Values.Any(p => spec.IsSatisfiedBy(p));
        return Fin.Succ(exists);
    });
}

public virtual FinT<IO, Seq<Product>> FindAll(Specification<Product> spec)
{
    return IO.lift(() =>
    {
        var products = _products.Values.Where(p => spec.IsSatisfiedBy(p));
        return Fin.Succ(toSeq(products));
    });
}
```

### EfCore 구현 패턴 (pattern-match SQL 최적화)

알려진 Specification 타입은 `switch` pattern-match로 EF Core LINQ 쿼리로 변환하여 SQL 최적화합니다. 미지의 타입은 `IsSatisfiedBy()`로 폴백합니다:

```csharp
public virtual FinT<IO, bool> Exists(Specification<Product> spec)
{
    return IO.liftAsync(async () =>
    {
        bool exists = spec switch
        {
            // 알려진 Spec → SQL 최적화 쿼리
            ProductNameUniqueSpec s => await _dbContext.Products.AnyAsync(p =>
                EF.Property<string>(p, nameof(Product.Name)) == (string)s.Name &&
                (s.ExcludeId == null || p.Id != s.ExcludeId.Value)),
            // 미지의 Spec → 클라이언트 평가 폴백
            _ => await _dbContext.Products.AnyAsync(p => spec.IsSatisfiedBy(p))
        };

        return Fin.Succ(exists);
    });
}

public virtual FinT<IO, Seq<Product>> FindAll(Specification<Product> spec)
{
    return IO.liftAsync(async () =>
    {
        IQueryable<Product> query = spec switch
        {
            ProductPriceRangeSpec s => _dbContext.Products.Where(p =>
                EF.Property<decimal>(p, nameof(Product.Price)) >= (decimal)s.MinPrice &&
                EF.Property<decimal>(p, nameof(Product.Price)) <= (decimal)s.MaxPrice),
            ProductLowStockSpec s => _dbContext.Products.Where(p =>
                EF.Property<int>(p, nameof(Product.StockQuantity)) < (int)s.Threshold),
            _ => _dbContext.Products.Where(p => spec.IsSatisfiedBy(p))
        };

        var products = await query.Include(p => p.Tags).ToListAsync();
        return Fin.Succ(toSeq(products));
    });
}
```

> **설계 결정**: `Expression<Func<T, bool>>` 대신 `bool IsSatisfiedBy(T)` 메서드를 사용합니다. Expression Tree는 EF Core와의 결합도가 높고 Value Object 변환에서 복잡성이 증가합니다. Pattern-match 접근법은 도메인 순수성을 유지하면서 필요한 곳에서만 SQL 최적화를 적용합니다.

---

## 5. Usecase에서 사용 (HOW)

### 단일 Spec 사용 — 중복 검사 (CreateProductCommand)

```csharp
public sealed class Usecase(IProductRepository productRepository)
    : ICommandUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        var productName = ProductName.Create(request.Name).ThrowIfFail();

        FinT<IO, Response> usecase =
            from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
            from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                new AlreadyExists(),
                request.Name,
                $"Product name already exists: '{request.Name}'"))
            from product in _productRepository.Create(...)
            select new Response(...);

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }
}
```

### 복합 Spec 조합 — 검색 필터 (SearchProductsQuery)

```csharp
public sealed class Usecase(IProductRepository productRepository)
    : IQueryUsecase<Request, Response>
{
    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        var spec = BuildSpecification(request);

        FinT<IO, Response> usecase =
            from products in _productRepository.FindAll(spec)
            select new Response(
                products
                    .Select(p => new ProductDto(p.Id.ToString(), p.Name, p.Price, p.StockQuantity))
                    .ToSeq());

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }

    private static Specification<Product> BuildSpecification(Request request)
    {
        Specification<Product>? spec = null;

        if (request.MinPrice.HasValue && request.MaxPrice.HasValue)
        {
            spec = new ProductPriceRangeSpec(
                Money.Create(request.MinPrice.Value).ThrowIfFail(),
                Money.Create(request.MaxPrice.Value).ThrowIfFail());
        }

        if (request.LowStockThreshold.HasValue)
        {
            var lowStockSpec = new ProductLowStockSpec(
                Quantity.Create(request.LowStockThreshold.Value).ThrowIfFail());

            spec = spec is not null ? spec & lowStockSpec : lowStockSpec;
        }

        // 필터가 없으면 모든 상품 반환
        return spec ?? new AllProductsSpec();
    }
}

// file-scoped 폴백 Specification
file sealed class AllProductsSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product product) => true;
}
```

**포인트:**
- `BuildSpecification`에서 선택적 필터를 `&` 연산자로 점진적 조합
- 필터가 없을 때의 폴백용 `AllProductsSpec`은 `file` 접근자로 Usecase 파일 내부에 선언

---

## 6. 테스트 패턴

### Specification 자체 테스트 (경계값)

```csharp
public class ProductPriceRangeSpecTests
{
    private static Product CreateSampleProduct(decimal price = 100m)
    {
        return Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(price).ThrowIfFail(),
            Quantity.Create(10).ThrowIfFail());
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenPriceWithinRange()
    {
        // Arrange
        var product = CreateSampleProduct(price: 150m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ReturnsTrue_WhenPriceEqualsMinPrice()
    {
        // Arrange
        var product = CreateSampleProduct(price: 100m);
        var sut = new ProductPriceRangeSpec(
            Money.Create(100m).ThrowIfFail(),
            Money.Create(200m).ThrowIfFail());

        // Act
        var actual = sut.IsSatisfiedBy(product);

        // Assert
        actual.ShouldBeTrue();
    }
}
```

### Specification 조합 테스트

```csharp
// 메서드 스타일 조합
var sut = new IsPositiveSpec().And(new IsEvenSpec());
sut.IsSatisfiedBy(2).ShouldBe(true);   // 양수이면서 짝수
sut.IsSatisfiedBy(3).ShouldBe(false);  // 양수이지만 홀수

// 연산자 스타일 조합
var sut = new IsPositiveSpec() & !new IsEvenSpec();
sut.IsSatisfiedBy(3).ShouldBe(true);   // 양수이면서 짝수가 아닌 수
```

### Usecase 테스트 (NSubstitute Mock)

Specification 타입까지 검증할 필요 없이 `Arg.Any<Specification<T>>()`로 Mock합니다:

```csharp
public class SearchProductsQueryTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly SearchProductsQuery.Usecase _sut;

    public SearchProductsQueryTests()
    {
        _sut = new SearchProductsQuery.Usecase(_productRepository);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPriceRangeProvided()
    {
        // Arrange
        var matchingProducts = Seq(Product.Create(...));
        var request = new SearchProductsQuery.Request(100m, 200m, null);

        _productRepository.FindAll(Arg.Any<Specification<Product>>())
            .Returns(FinTFactory.Succ(matchingProducts));

        // Act
        var actual = await _sut.Handle(request, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
```

---

## 7. 체크리스트

### Specification 구현 시

- [ ] `Specification<T>` 상속 (`Functorium.Domains.Specifications`)
- [ ] `sealed class`로 선언
- [ ] `IsSatisfiedBy()` 구현 — 순수 함수 (I/O 없음)
- [ ] 파라미터를 **public 프로퍼티**로 노출 (EfCore SQL 최적화용)
- [ ] `{Aggregate}/Specifications/` 폴더에 배치
- [ ] 네이밍: `{Aggregate}{조건}Spec`

### Repository 통합 시

- [ ] Port에 `Exists(Specification<T>)` / `FindAll(Specification<T>)` 추가
- [ ] InMemory 구현: `IsSatisfiedBy()` 직접 사용
- [ ] EfCore 구현: 알려진 Spec은 pattern-match SQL 최적화, 미지는 폴백

### 테스트 시

- [ ] Specification 자체 테스트: 만족/불만족 경계값
- [ ] 조합 테스트: `And`, `Or`, `Not` (메서드 + 연산자)
- [ ] Usecase 테스트: `Arg.Any<Specification<T>>()` Mock

---

## 참고 문서

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [09-domain-services.md](./09-domain-services.md) — 도메인 서비스
- [12-ports-and-adapters.md](./12-ports-and-adapters.md) — Port와 Adapter
- [13-unit-testing.md](./13-unit-testing.md) — 단위 테스트 규칙

### 실전 예제 파일

| 분류 | 파일 |
|------|------|
| **프레임워크** | `Src/Functorium/Domains/Specifications/Specification.cs` |
| | `Src/Functorium/Domains/Specifications/AndSpecification.cs` |
| | `Src/Functorium/Domains/Specifications/OrSpecification.cs` |
| | `Src/Functorium/Domains/Specifications/NotSpecification.cs` |
| **도메인 Spec** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Specifications/ProductNameUniqueSpec.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Specifications/ProductPriceRangeSpec.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Specifications/ProductLowStockSpec.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Customers/Specifications/CustomerEmailSpec.cs` |
| **Repository Port** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Products/Ports/IProductRepository.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Domain/AggregateRoots/Customers/Ports/ICustomerRepository.cs` |
| **Repository 구현** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/InMemory/InMemoryProductRepository.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/EfCoreProductRepository.cs` |
| **Usecase** | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Products/CreateProductCommand.cs` |
| | `Tests.Hosts/01-SingleHost/Src/LayeredArch.Application/Usecases/Products/SearchProductsQuery.cs` |
| **프레임워크 테스트** | `Tests/Functorium.Tests.Unit/DomainsTests/Specifications/SpecificationTests.cs` |
| | `Tests/Functorium.Tests.Unit/DomainsTests/Specifications/SpecificationOperatorTests.cs` |
| **도메인 Spec 테스트** | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductPriceRangeSpecTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductLowStockSpecTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductNameUniqueSpecTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Products/ProductSpecificationCompositionTests.cs` |
| | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Domain/Customers/CustomerEmailSpecTests.cs` |
| **Usecase 테스트** | `Tests.Hosts/01-SingleHost/Tests/LayeredArch.Tests.Unit/Application/Products/SearchProductsQueryTests.cs` |
