---
title: "인메모리 구현"
---
## 개요

앞 장에서 설계한 Repository 인터페이스가 얼마나 간단하게 구현되는지 확인해보겠습니다. InMemory 어댑터는 테스트 환경에서 유용할 뿐 아니라, Repository 패턴의 핵심 동작을 가장 명확하게 보여주는 구현입니다.

LINQ의 `Where`와 `Any`에 메서드 참조(`spec.IsSatisfiedBy`)를 전달하는 것만으로 구현이 완성됩니다.

## 학습 목표

### 핵심 학습 목표
1. **IsSatisfiedBy 직접 활용** - `Where(spec.IsSatisfiedBy)` 패턴 이해
2. **메서드 참조 구문** - 람다 `p => spec.IsSatisfiedBy(p)` 대신 메서드 참조 `spec.IsSatisfiedBy` 사용
3. **Repository 구현의 단순성** - Specification 덕분에 Repository 구현이 극도로 간결해짐

### 실습을 통해 확인할 내용
- InMemoryProductRepository의 FindAll/Exists 동작
- 다양한 Specification 조합으로 조회
- 존재 여부 확인 (Exists)

## 핵심 개념

### IsSatisfiedBy 메서드 참조

`Specification<T>.IsSatisfiedBy`는 `Func<T, bool>` 시그니처와 호환됩니다. 따라서 LINQ의 `Where`에 직접 전달할 수 있습니다.

```csharp
// 람다 방식 (장황)
_products.Where(p => spec.IsSatisfiedBy(p));

// 메서드 참조 방식 (간결)
_products.Where(spec.IsSatisfiedBy);
```

두 방식은 동일한 결과를 반환하지만, 메서드 참조 방식이 더 간결하고 의도가 명확합니다.

### InMemory 구현의 핵심

```csharp
public class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public InMemoryProductRepository(IEnumerable<Product> products)
        => _products = products.ToList();

    public IEnumerable<Product> FindAll(Specification<Product> spec)
        => _products.Where(spec.IsSatisfiedBy);

    public bool Exists(Specification<Product> spec)
        => _products.Any(spec.IsSatisfiedBy);
}
```

Repository는 **어떤 조건으로 필터링하는지 전혀 모릅니다**. `IsSatisfiedBy`에 위임할 뿐입니다. 새로운 Specification이 추가되어도 Repository 코드는 변경할 필요가 없습니다.

## 프로젝트 설명

### 프로젝트 구조
```
InMemoryImpl/                            # 메인 프로젝트
├── Product.cs                           # 도메인 모델
├── IProductRepository.cs                # Repository 인터페이스
├── InMemoryProductRepository.cs         # InMemory 구현
├── SampleProducts.cs                    # 예제 데이터 (8개 상품)
├── Specifications/
│   ├── ProductInStockSpec.cs                   # 재고 있는 상품
│   ├── ProductPriceRangeSpec.cs                # 가격 범위 상품
│   └── ProductCategorySpec.cs                  # 카테고리별 상품
├── Program.cs                           # FindAll/Exists 데모
└── InMemoryImpl.csproj
InMemoryImpl.Tests.Unit/                 # 테스트 프로젝트
├── InMemoryRepositoryTests.cs           # Repository 동작 테스트
└── ...
```

## 한눈에 보는 정리

### InMemory 구현 핵심
| 메서드 | LINQ 메서드 | 설명 |
|--------|------------|------|
| `FindAll(spec)` | `Where(spec.IsSatisfiedBy)` | 조건을 만족하는 모든 항목 반환 |
| `Exists(spec)` | `Any(spec.IsSatisfiedBy)` | 조건을 만족하는 항목 존재 여부 |

### InMemory의 특징
| 특징 | 설명 |
|------|------|
| **장점** | 구현이 극도로 단순, 테스트에 적합, 외부 의존성 없음 |
| **한계** | 메모리에 모든 데이터를 로드해야 함, 대용량 데이터에 부적합 |
| **용도** | 단위 테스트, 프로토타이핑, 소규모 데이터 |

## FAQ

### Q1: InMemory 구현은 실제 프로젝트에서 쓸모가 있나요?
**A**: 네. 단위 테스트에서 실제 DB 대신 InMemory 구현을 사용하면 테스트가 빠르고 격리됩니다. Repository 인터페이스 덕분에 테스트와 프로덕션에서 다른 구현을 사용할 수 있습니다.

### Q2: `Where(spec.IsSatisfiedBy)` 대신 `Where(p => spec.IsSatisfiedBy(p))`를 써야 하는 경우가 있나요?
**A**: 기능적으로 동일합니다. 메서드 참조가 더 간결하므로 일반적으로 `spec.IsSatisfiedBy` 형태를 선호합니다.

### Q3: 대용량 데이터에서는 어떻게 해야 하나요?
**A**: 3장(PropertyMap)과 4장(EF Core 구현)에서 Expression Tree를 활용하여 DB 수준에서 필터링하는 방법을 학습합니다. InMemory 방식은 모든 데이터를 메모리에 로드한 후 필터링하므로 대용량에는 적합하지 않습니다.

---

InMemory 구현은 `IsSatisfiedBy`를 직접 호출하므로 도메인 모델만으로 충분했습니다. 하지만 EF Core처럼 Expression Tree를 SQL로 변환하는 환경에서는, 도메인 모델과 DB 모델의 프로퍼티 이름이 다를 때 문제가 발생합니다. 다음 장에서는 이 간극을 메우는 PropertyMap을 다룹니다.

→ [3장: PropertyMap](../03-PropertyMap/)
