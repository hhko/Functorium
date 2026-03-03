---
title: "Part 4 - Chapter 14: Dynamic Filter Builder"
---

## 개요

`Specification<T>.All`을 초기값(seed)으로 사용하고, 조건부 `&=` 체이닝으로 동적 필터를 구성하는 패턴을 학습합니다. 이 패턴은 검색 API, 목록 조회 등 선택적 필터가 필요한 모든 곳에서 활용됩니다.

## 학습 목표

1. **All을 초기값으로 사용하는 패턴** - `Specification<T>.All`의 항등원 성질 이해
2. **조건부 `&=` 체이닝** - null/empty 체크 후 점진적 조합
3. **Filter Builder 분리** - 필터 구성 로직을 독립 클래스로 추출

## 핵심 개념

### All을 초기값(Seed)으로 사용

`Specification<T>.All`은 `&` 연산의 항등원입니다. `All & X = X`이므로, 필터가 하나도 없으면 `All`이 그대로 반환되어 전체 조회로 동작합니다.

```csharp
var spec = Specification<Product>.All;  // 초기값

if (!string.IsNullOrWhiteSpace(request.Name))
    spec &= new ProductNameContainsSpec(request.Name);
if (!string.IsNullOrWhiteSpace(request.Category))
    spec &= new ProductCategorySpec(request.Category);

return spec;  // 필터 없으면 All 반환
```

### Filter Builder 패턴

필터 구성 로직을 `static` 메서드로 분리하면 Usecase 코드가 깔끔해지고, 필터 로직을 독립적으로 테스트할 수 있습니다.

```csharp
public static class ProductFilterBuilder
{
    public static Specification<Product> Build(SearchProductsRequest request)
    {
        var spec = Specification<Product>.All;
        // 조건부 &= 체이닝
        return spec;
    }
}
```

## 프로젝트 설명

### 프로젝트 구조

```
DynamicFilter/
├── Product.cs                          # 상품 레코드
├── SampleProducts.cs                   # 샘플 데이터
├── SearchProductsRequest.cs            # 검색 요청 DTO
├── ProductFilterBuilder.cs             # 동적 필터 빌더
├── Specifications/
│   ├── ProductNameContainsSpec.cs             # 이름 포함 검색
│   ├── ProductCategorySpec.cs                 # 카테고리 필터
│   ├── ProductPriceRangeSpec.cs               # 가격 범위
│   └── ProductInStockSpec.cs                  # 재고 있음
└── Program.cs                          # 데모 실행
```

## 한눈에 보는 정리

| 요청 상태 | `Build()` 반환값 | `IsAll` | 동작 |
|-----------|-----------------|---------|------|
| 필터 없음 | `All` | `true` | 전체 조회 |
| 필터 1개 | 해당 Spec | `false` | 단일 필터 |
| 필터 N개 | `And` 조합 | `false` | 복합 필터 |

### null 폴백 vs All 초기값 비교

| 항목 | `null` 폴백 | `All` 초기값 |
|------|------------|-------------|
| **null 체크** | 매번 필요 | 불필요 |
| **조합 문법** | `spec = spec is not null ? spec & x : x` | `spec &= x` |
| **빈 필터 처리** | 별도 분기 필요 | 자동 (All 반환) |

## FAQ

### Q1: All은 성능에 영향을 주나요?
**A**: `All`의 `IsSatisfiedBy()`는 항상 `true`를 반환하므로 오버헤드가 거의 없습니다. 또한 `&` 연산자가 항등원 최적화를 수행하므로 (`All & X = X`), 불필요한 `And` 래핑이 발생하지 않습니다.

### Q2: Filter Builder를 static 메서드 대신 인스턴스 클래스로 만들어야 하나요?
**A**: 외부 의존성(예: 현재 사용자 정보, 설정값)이 필요한 경우 인스턴스 클래스로 만들 수 있습니다. 그러나 순수한 필터 조합이라면 static 메서드로 충분합니다.

### Q3: `|=` (OR 체이닝)도 같은 패턴으로 사용할 수 있나요?
**A**: `All`은 `&` 연산의 항등원이지만, `|` 연산의 항등원은 아닙니다 (`All | X = All`, 모든 것을 만족하는 조건과 OR하면 항상 전체가 됩니다). OR 조합이 필요하면 별도의 초기값 전략이 필요합니다.
