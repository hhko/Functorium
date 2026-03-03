---
title: "Part 3 - Chapter 12: EF Core 구현"
---

> **Part 3: Repository 연동** | [이전: 11장 PropertyMap <-](../03-PropertyMap/)

---

## 개요

지금까지 배운 모든 것을 조합하여 **EF Core 어댑터 시뮬레이션**을 구현합니다. 실제 EF Core 의존성 없이 `AsQueryable()`을 사용하여 전체 파이프라인을 재현합니다: Specification에서 Expression을 추출하고, PropertyMap으로 변환하고, Queryable에 적용합니다.

핵심은 **새로운 Specification을 추가해도 Repository 코드를 전혀 변경할 필요가 없다**는 것입니다. Open-Closed Principle의 실현입니다.

## 학습 목표

### 핵심 학습 목표
1. **전체 파이프라인 이해** - Specification -> Expression 추출 -> PropertyMap 변환 -> Queryable 실행
2. **BuildQuery 패턴** - `TryResolve` + `Translate` + `Where` 조합
3. **Open-Closed Principle** - 새 조건 추가 시 Repository 변경 불필요

### 실습을 통해 확인할 내용
- SimulatedEfCoreProductRepository의 전체 동작
- InMemory Repository와 동일한 인터페이스로 사용
- 새로운 Specification 추가가 Repository 변경 없이 가능함을 확인

## 핵심 개념

### 전체 파이프라인

```
Specification<Product>
    |
    v
SpecificationExpressionResolver.TryResolve(spec)
    |
    v
Expression<Func<Product, bool>>     (도메인 Expression)
    |
    v
PropertyMap.Translate(expression)
    |
    v
Expression<Func<ProductDbModel, bool>>  (모델 Expression)
    |
    v
dbModels.AsQueryable().Where(translated)
    |
    v
IQueryable<ProductDbModel>          (EF Core가 SQL로 변환)
```

### BuildQuery 패턴

```csharp
private IQueryable<ProductDbModel> BuildQuery(Specification<Product> spec)
{
    // 1) Specification에서 Expression 추출
    var expression = SpecificationExpressionResolver.TryResolve(spec);
    if (expression is null)
        throw new InvalidOperationException(
            "Specification does not support expression resolution.");

    // 2) 도메인 Expression -> 모델 Expression 변환
    var translated = _propertyMap.Translate(expression);

    // 3) Queryable에 적용 (EF Core에서는 SQL로 변환됨)
    return _dbModels.AsQueryable().Where(translated);
}
```

### Open-Closed Principle

새로운 조건이 필요하면:
1. 새로운 `ExpressionSpecification<Product>`를 만든다
2. 끝. Repository 코드는 변경하지 않는다.

```csharp
// 새 Specification 추가 - Repository 변경 없음!
var newSpec = new ProductCategorySpec("전자제품") & new ProductPriceRangeSpec(50_000, decimal.MaxValue);
var results = repository.FindAll(newSpec);  // 그냥 동작함
```

## 프로젝트 설명

### 프로젝트 구조
```
EfCoreImpl/                              # 메인 프로젝트
├── Product.cs                           # 도메인 모델
├── ProductDbModel.cs                    # 퍼시스턴스 모델
├── IProductRepository.cs                # Repository 인터페이스
├── InMemoryProductRepository.cs         # InMemory 구현 (비교용)
├── SimulatedEfCoreProductRepository.cs  # EF Core 시뮬레이션 구현
├── ProductPropertyMap.cs                # PropertyMap 정의
├── Specifications/
│   ├── ProductInStockSpec.cs               # 재고 (Expression 기반)
│   ├── ProductPriceRangeSpec.cs            # 가격 범위 (Expression 기반)
│   └── ProductCategorySpec.cs              # 카테고리 (Expression 기반)
├── Program.cs                           # 전체 파이프라인 데모
└── EfCoreImpl.csproj
EfCoreImpl.Tests.Unit/                   # 테스트 프로젝트
├── EfCoreRepositoryTests.cs             # 전체 파이프라인 테스트
└── ...
```

## 한눈에 보는 정리

### InMemory vs EF Core 구현 비교
| 구분 | InMemory | EF Core (시뮬레이션) |
|------|----------|---------------------|
| **필터링 위치** | 애플리케이션 메모리 | DB (Queryable/SQL) |
| **사용하는 것** | `IsSatisfiedBy` (메서드) | Expression Tree |
| **PropertyMap** | 불필요 | 필요 (이름 변환) |
| **대용량 데이터** | 부적합 | 적합 |
| **인터페이스** | 동일 (`IProductRepository`) | 동일 |

### 파이프라인 단계
| 단계 | 입력 | 출력 | 담당 |
|------|------|------|------|
| 1 | `Specification<Product>` | `Expression<Func<Product, bool>>` | `TryResolve` |
| 2 | 도메인 Expression | 모델 Expression | `PropertyMap.Translate` |
| 3 | 모델 Expression | `IQueryable<ProductDbModel>` | `AsQueryable().Where()` |

## FAQ

### Q1: 실제 EF Core에서는 어떻게 달라지나요?
**A**: `_dbModels.AsQueryable()`이 `dbContext.Set<ProductDbModel>().AsQueryable()`로 바뀝니다. EF Core의 LINQ Provider가 Expression Tree를 SQL로 변환하므로, 필터링이 DB 수준에서 실행됩니다. 나머지 파이프라인은 동일합니다.

### Q2: Expression을 지원하지 않는 Specification을 넘기면 어떻게 되나요?
**A**: `TryResolve`가 `null`을 반환하고, `BuildQuery`에서 `InvalidOperationException`이 발생합니다. EF Core 어댑터에서는 반드시 `ExpressionSpecification`을 사용해야 합니다.

### Q3: InMemory와 EF Core 구현을 동시에 사용할 수 있나요?
**A**: 네. 둘 다 `IProductRepository`를 구현하므로 DI 컨테이너에서 환경에 따라 다른 구현을 주입할 수 있습니다. 테스트에서는 InMemory, 프로덕션에서는 EF Core 구현을 사용하는 것이 일반적입니다.
