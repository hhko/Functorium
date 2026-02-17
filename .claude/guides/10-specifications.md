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
├── Specification<T>              (추상 기반 클래스)
│   ├── IsSatisfiedBy()           (추상 메서드)
│   ├── And() / Or() / Not()     (조합 메서드)
│   └── & / | / !                (연산자 오버로드)
├── ExpressionSpecification<T>    (추상, Expression 기반 — 권장)
│   ├── ToExpression()            (추상 메서드)
│   └── IsSatisfiedBy()           (자동 구현, delegate 캐싱)
├── IExpressionSpec<T>            (Expression 제공 인터페이스)
├── AndSpecification<T>           (internal sealed)
├── OrSpecification<T>            (internal sealed)
└── NotSpecification<T>           (internal sealed)

Functorium.Domains.Specifications.Expressions
├── SpecificationExpressionResolver  (And/Or/Not Expression 합성)
└── PropertyMap<TEntity, TModel>     (Entity → Model Expression 변환)
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
using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace {프로젝트}.Domain.AggregateRoots.{Aggregate}.Specifications;

public sealed class {Aggregate}{조건}Spec : ExpressionSpecification<{Aggregate}>
{
    public {ValueObjectType} {PropertyName} { get; }

    public {Aggregate}{조건}Spec({ValueObjectType} {paramName})
    {
        {PropertyName} = {paramName};
    }

    public override Expression<Func<{Aggregate}, bool>> ToExpression()
    {
        // Value Object → primitive 변환 후 클로저 캡처
        var {paramPrimitive} = ({PrimitiveType}){PropertyName};
        return entity => ({PrimitiveType})entity.{EntityProperty} == {paramPrimitive};
    }
    // IsSatisfiedBy()는 ToExpression() 컴파일로 자동 구현됨
}
```

**핵심 규칙:**
- `ExpressionSpecification<T>` 상속 (Expression 기반 자동 SQL 번역 지원)
- `ToExpression()`에서 Value Object를 primitive로 변환하여 클로저에 캡처
- Entity 프로퍼티 접근 시 `(primitiveType)entity.Property` 캐스트 사용
- `IsSatisfiedBy()`는 자동 구현되므로 별도 구현 불필요

### 실전 예제

#### 상품명 중복 확인 (ProductNameUniqueSpec)

```csharp
public sealed class ProductNameUniqueSpec : ExpressionSpecification<Product>
{
    public ProductName Name { get; }
    public ProductId? ExcludeId { get; }

    public ProductNameUniqueSpec(ProductName name, ProductId? excludeId = null)
    {
        Name = name;
        ExcludeId = excludeId;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        string nameStr = Name;
        string? excludeIdStr = ExcludeId?.ToString();
        return product => (string)product.Name == nameStr &&
                          (excludeIdStr == null || product.Id.ToString() != excludeIdStr);
    }
}
```

#### 가격 범위 (ProductPriceRangeSpec)

```csharp
public sealed class ProductPriceRangeSpec : ExpressionSpecification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money minPrice, Money maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal min = MinPrice;
        decimal max = MaxPrice;
        return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
    }
}
```

#### 재고 부족 (ProductLowStockSpec)

```csharp
public sealed class ProductLowStockSpec : ExpressionSpecification<Product>
{
    public Quantity Threshold { get; }

    public ProductLowStockSpec(Quantity threshold)
    {
        Threshold = threshold;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        int threshold = Threshold;
        return product => (int)product.StockQuantity < threshold;
    }
}
```

### Expression에서 Value Object 변환 패턴

`ToExpression()`에서 Value Object를 primitive로 변환할 때:

```csharp
// ✅ 클로저 캡처 전에 primitive로 변환
decimal min = MinPrice;  // Value Object → primitive (implicit operator)
return product => (decimal)product.Price >= min;

// ✅ EntityId는 ToString()으로 변환
string? excludeIdStr = ExcludeId?.ToString();
return product => product.Id.ToString() != excludeIdStr;

// ❌ Expression 내부에서 Value Object 직접 비교 (PropertyMap이 변환 불가)
return product => product.Price >= MinPrice;
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

### EfCore 구현 패턴 (Expression 기반 자동 SQL 번역)

`PropertyMap`으로 Entity Expression → Model Expression 자동 변환 후 EF Core LINQ에 적용합니다. **switch 케이스 불필요**:

```csharp
// PropertyMap 구성 (static readonly, 한 번만)
private static readonly PropertyMap<Product, ProductModel> _propertyMap =
    new PropertyMap<Product, ProductModel>()
        .Map(p => (decimal)p.Price, m => m.Price)
        .Map(p => (string)p.Name, m => m.Name)
        .Map(p => (int)p.StockQuantity, m => m.StockQuantity)
        .Map(p => p.Id.ToString(), m => m.Id);

// BuildQuery — switch 제거, 자동 변환
private IQueryable<ProductModel> BuildQuery(Specification<Product> spec)
{
    var expression = SpecificationExpressionResolver.TryResolve(spec);
    if (expression is not null)
    {
        var modelExpression = _propertyMap.Translate(expression);
        return _dbContext.Products.Where(modelExpression);
    }

    throw new NotSupportedException(
        $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다. " +
        $"ExpressionSpecification<T>을 상속하고 ToExpression()을 구현하세요.");
}
```

**새 Specification 추가 시 변경 사항:**
- Domain: `ExpressionSpecification<T>`만 상속하고 `ToExpression()` 구현
- Adapter: **변경 불필요** (PropertyMap에 이미 매핑된 프로퍼티만 사용한다면)
- PropertyMap: 새 Entity 프로퍼티를 사용하는 Spec이면 매핑 추가

> **설계 결정**: `ExpressionSpecification<T>`의 `ToExpression()`은 도메인 엔터티 기준으로 Expression을 정의하되, Value Object를 primitive로 캐스트합니다. `PropertyMap`의 `ExpressionVisitor`가 이 캐스트 패턴을 인식하여 Model 프로퍼티로 자동 변환합니다. And/Or/Not 조합도 `SpecificationExpressionResolver`가 자동 합성합니다.

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
    private readonly IProductRepository _productRepository = productRepository;

    public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        var spec = BuildSpecification(request);

        // spec이 null이면 필터 없음 → GetAll() 폴백
        // (Specification<T>을 상속한 폴백 클래스는 ExpressionSpecification<T>이 아니므로
        //  EfCore에서 NotSupportedException이 발생합니다)
        FinT<IO, Response> usecase =
            from products in spec is not null
                ? _productRepository.FindAll(spec)
                : _productRepository.GetAll()
            select new Response(
                products
                    .Select(p => new ProductDto(p.Id.ToString(), p.Name, p.Price, p.StockQuantity))
                    .ToSeq());

        Fin<Response> response = await usecase.Run().RunAsync();
        return response.ToFinResponse();
    }

    private static Specification<Product>? BuildSpecification(Request request)
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

        return spec;
    }
}
```

**포인트:**
- `BuildSpecification`에서 선택적 필터를 `&` 연산자로 점진적 조합
- 필터가 없으면 `null` 반환 → Usecase에서 `GetAll()` 폴백
- `AllProductsSpec` 같은 비-Expression 폴백 클래스는 EfCore에서 `NotSupportedException`이 발생하므로 사용 금지

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

- [ ] `ExpressionSpecification<T>` 상속 (`Functorium.Domains.Specifications`)
- [ ] `sealed class`로 선언
- [ ] `ToExpression()` 구현 — Value Object → primitive 캐스트 사용
- [ ] `{Aggregate}/Specifications/` 폴더에 배치
- [ ] 네이밍: `{Aggregate}{조건}Spec`

### Repository 통합 시

- [ ] Port에 `Exists(Specification<T>)` / `FindAll(Specification<T>)` 추가
- [ ] InMemory 구현: `IsSatisfiedBy()` 직접 사용 (자동 구현됨)
- [ ] EfCore 구현: `PropertyMap` 구성 + `SpecificationExpressionResolver.TryResolve()` 사용
- [ ] 새 Entity 프로퍼티 사용 시 `PropertyMap.Map()` 추가

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
