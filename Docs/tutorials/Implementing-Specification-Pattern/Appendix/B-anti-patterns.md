# 부록 B. 안티패턴

> **부록** | [← 이전: A. Specification vs 대안 비교](A-specification-vs-alternatives.md) | [목차](../README.md) | [다음: C. 용어집 →](C-glossary.md)

---

## 개요

Specification 패턴을 사용할 때 흔히 발생하는 안티패턴과 그 해결 방법을 정리합니다.

---

## 1. non-Expression Spec + EF Core

### 문제

`Specification<T>`(non-Expression)을 EF Core와 같은 ORM에서 직접 사용하면 런타임 에러가 발생합니다.

```csharp
// ❌ 안티패턴: non-Expression Specification을 EF Core에서 사용
public sealed class ActiveProductSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product candidate) =>
        candidate.IsActive;
}

// EF Core에서 사용 시 런타임 에러
var products = await dbContext.Products
    .Where(spec.IsSatisfiedBy)  // 💥 Expression Tree가 아니므로 SQL 변환 불가
    .ToListAsync();
```

### 원인

EF Core의 `Where`는 `Expression<Func<T, bool>>`을 요구합니다. `Specification<T>`의 `IsSatisfiedBy`는 일반 메서드이므로 Expression Tree로 변환할 수 없습니다.

### 해결

ORM 통합이 필요한 경우 `ExpressionSpecification<T>`를 사용합니다.

```csharp
// ✅ Expression 기반 Specification 사용
public sealed class ActiveProductSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.IsActive;
}
```

---

## 2. Value Object Closure 변환 누락

### 문제

Expression Tree 내에서 Value Object를 직접 참조하면 EF Core가 SQL로 변환할 수 없습니다.

```csharp
// ❌ 안티패턴: Expression 내에서 Value Object 직접 사용
public sealed class MinPriceSpec : ExpressionSpecification<Product>
{
    private readonly Money _minPrice;

    public MinPriceSpec(Money minPrice) => _minPrice = minPrice;

    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.Price >= _minPrice;  // 💥 Money 타입을 SQL로 변환 불가
}
```

### 원인

Expression Tree는 데이터베이스 타입으로 변환 가능한 값만 사용할 수 있습니다. Value Object는 도메인 개념이므로 데이터베이스가 이해할 수 없습니다.

### 해결

Value Object에서 primitive 값을 추출하여 사용합니다.

```csharp
// ✅ primitive 값으로 변환하여 사용
public sealed class MinPriceSpec : ExpressionSpecification<Product>
{
    private readonly decimal _minPrice;

    public MinPriceSpec(Money minPrice) => _minPrice = minPrice.Value;

    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.Price >= _minPrice;
}
```

---

## 3. Stateful Specification

### 문제

Specification이 내부 상태를 변경하면 예측할 수 없는 동작이 발생합니다.

```csharp
// ❌ 안티패턴: 상태를 가진 Specification
public sealed class CountingSpec : Specification<Product>
{
    private int _matchCount;

    public int MatchCount => _matchCount;

    public override bool IsSatisfiedBy(Product candidate)
    {
        if (candidate.IsActive)
        {
            _matchCount++;  // 💥 부작용: 상태 변경
            return true;
        }
        return false;
    }
}
```

### 원인

Specification은 **순수 함수**여야 합니다. 부작용이 있으면 동일 입력에 대해 다른 결과가 나올 수 있고, 스레드 안전하지 않습니다.

### 해결

Specification은 판단만 수행하고, 집계는 외부에서 처리합니다.

```csharp
// ✅ 순수 함수 Specification
public sealed class ActiveProductSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product candidate) =>
        candidate.IsActive;
}

// 집계는 외부에서 처리
var spec = new ActiveProductSpec();
var matchCount = products.Count(spec.IsSatisfiedBy);
```

---

## 4. God Specification

### 문제

하나의 Specification에 모든 조건을 넣으면 재사용과 조합이 불가능합니다.

```csharp
// ❌ 안티패턴: 모든 조건을 하나의 Specification에
public sealed class ProductFilterSpec : Specification<Product>
{
    private readonly string? _category;
    private readonly decimal? _minPrice;
    private readonly decimal? _maxPrice;
    private readonly bool? _isActive;
    private readonly bool? _isPremium;

    public ProductFilterSpec(
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null,
        bool? isPremium = null)
    {
        _category = category;
        _minPrice = minPrice;
        _maxPrice = maxPrice;
        _isActive = isActive;
        _isPremium = isPremium;
    }

    public override bool IsSatisfiedBy(Product candidate)
    {
        if (_category is not null && candidate.Category != _category)
            return false;
        if (_minPrice is not null && candidate.Price < _minPrice)
            return false;
        if (_maxPrice is not null && candidate.Price > _maxPrice)
            return false;
        if (_isActive is not null && candidate.IsActive != _isActive)
            return false;
        if (_isPremium is not null && candidate.IsPremium != _isPremium)
            return false;
        return true;
    }
}
```

### 원인

Specification 패턴의 핵심 가치는 **단일 규칙의 캡슐화와 조합**입니다. 모든 조건을 하나에 넣으면 이 가치가 사라집니다.

### 해결

각 규칙을 독립적인 Specification으로 분리하고 조합합니다.

```csharp
// ✅ 단일 책임 Specification + 조합
var spec = Specification<Product>.All;

if (filter.Category is not null)
    spec &= new ProductCategorySpec(filter.Category);
if (filter.MinPrice is not null)
    spec &= new MinPriceSpec(filter.MinPrice.Value);
if (filter.IsActive is not null)
    spec &= new ActiveProductSpec();

var products = await repository.FindAsync(spec);
```

---

## 5. Specification in Presentation Layer

### 문제

Specification을 프레젠테이션 계층에서 직접 사용하면 계층 경계가 무너집니다.

```csharp
// ❌ 안티패턴: Controller에서 Specification 직접 사용
[ApiController]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repository;

    [HttpGet]
    public async Task<IActionResult> GetProducts(string? category)
    {
        // 💥 프레젠테이션 계층에서 도메인 Specification 직접 조합
        var spec = new ActiveProductSpec();
        if (category is not null)
            spec &= new ProductCategorySpec(category);

        var products = await _repository.FindAsync(spec);
        return Ok(products);
    }
}
```

### 원인

Specification은 도메인 계층의 개념입니다. 프레젠테이션 계층이 도메인 Specification에 직접 의존하면 계층 간 결합도가 높아집니다.

### 해결

Application 계층(UseCase/Handler)에서 Specification을 조합합니다.

```csharp
// ✅ Application 계층에서 Specification 조합
public sealed class GetProductsQueryHandler
{
    private readonly IProductRepository _repository;

    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery query)
    {
        var spec = Specification<Product>.All;
        if (query.Category is not null)
            spec &= new ProductCategorySpec(query.Category);

        var products = await _repository.FindAsync(spec);
        return products.Select(p => p.ToDto()).ToList();
    }
}
```

---

## 안티패턴 요약

| # | 안티패턴 | 핵심 문제 | 해결 방법 |
|---|----------|----------|----------|
| 1 | non-Expression + EF Core | SQL 변환 불가 | ExpressionSpecification 사용 |
| 2 | VO Closure 변환 누락 | Expression Tree 실패 | primitive 값 추출 |
| 3 | Stateful Specification | 부작용, 스레드 불안전 | 순수 함수 유지 |
| 4 | God Specification | 재사용/조합 불가 | 단일 책임 + 조합 |
| 5 | Presentation Layer 사용 | 계층 위반 | Application 계층에서 조합 |

---

## 다음 단계

용어집을 확인합니다.

→ [C. 용어집](C-glossary.md)
