---
title: "Part 3 - Chapter 11: PropertyMap"
---

> **Part 3: Repository 연동** | [이전: 10장 InMemory 구현 <-](../02-InMemory-Implementation/) | [다음: 12장 EF Core 구현 ->](../04-EfCore-Implementation/)

---

## 개요

도메인 엔티티와 DB 모델의 프로퍼티 이름이 다른 경우가 실무에서 흔합니다. 예를 들어 도메인의 `Price`가 DB에서는 `UnitPrice`로, `Stock`이 `StockQuantity`로 저장될 수 있습니다. `PropertyMap`은 이런 이름 불일치를 해결하여, **도메인 기준으로 작성된 Expression을 DB 모델 기준 Expression으로 자동 변환**합니다.

## 학습 목표

### 핵심 학습 목표
1. **엔티티-모델 불일치 문제 이해** - 도메인 모델과 퍼시스턴스 모델의 프로퍼티 이름이 다를 때 발생하는 문제
2. **PropertyMap 매핑 등록** - `Map(p => p.Name, m => m.ProductName)` 패턴
3. **Expression 자동 변환** - `Translate` 메서드로 도메인 Expression을 모델 Expression으로 변환

### 실습을 통해 확인할 내용
- 필드명 변환 (`TranslateFieldName`)
- 단일 Expression 변환
- 복합 Expression 변환 (And/Or 조합)
- 변환된 Expression으로 DbModel 컬렉션 필터링

## 핵심 개념

### 엔티티 vs 퍼시스턴스 모델

```csharp
// 도메인 엔티티: 비즈니스 언어
public record Product(string Name, decimal Price, int Stock, string Category);

// 퍼시스턴스 모델: DB 테이블 구조
public record ProductDbModel(string ProductName, decimal UnitPrice, int StockQuantity, string CategoryCode);
```

도메인 Specification은 `p => p.Stock > 0`으로 작성되지만, DB에는 `StockQuantity` 컬럼이 있습니다. PropertyMap이 이 간극을 자동으로 메워줍니다.

### PropertyMap 등록

```csharp
var map = new PropertyMap<Product, ProductDbModel>();
map.Map(p => p.Name, m => m.ProductName);
map.Map(p => p.Price, m => m.UnitPrice);
map.Map(p => p.Stock, m => m.StockQuantity);
map.Map(p => p.Category, m => m.CategoryCode);
```

### TranslatingVisitor 내부 동작

`PropertyMap.Translate`는 내부적으로 `ExpressionVisitor`를 활용합니다.

1. Expression Tree를 순회하며 `ParameterExpression`과 `MemberExpression`을 찾음
2. 엔티티 파라미터를 모델 파라미터로 교체
3. 프로퍼티 접근(`p.Stock`)을 매핑된 모델 프로퍼티(`m.StockQuantity`)로 교체
4. 결과: `p => p.Stock > 0` 이 `m => m.StockQuantity > 0`으로 변환

## 프로젝트 설명

### 프로젝트 구조
```
PropertyMapDemo/                         # 메인 프로젝트
├── Product.cs                           # 도메인 모델
├── ProductDbModel.cs                    # 퍼시스턴스 모델
├── ProductPropertyMap.cs                # PropertyMap 정의
├── Specifications/
│   ├── ProductInStockSpec.cs               # 재고 (Expression 기반)
│   └── ProductPriceRangeSpec.cs            # 가격 범위 (Expression 기반)
├── Program.cs                           # 변환 데모
└── PropertyMapDemo.csproj
PropertyMapDemo.Tests.Unit/              # 테스트 프로젝트
├── PropertyMapTests.cs                  # 필드명/Expression 변환 테스트
└── ...
```

## 한눈에 보는 정리

### 매핑 테이블
| 도메인 (Product) | DB 모델 (ProductDbModel) |
|------------------|-------------------------|
| `Name` | `ProductName` |
| `Price` | `UnitPrice` |
| `Stock` | `StockQuantity` |
| `Category` | `CategoryCode` |

### PropertyMap 핵심 메서드
| 메서드 | 설명 |
|--------|------|
| `Map(entity, model)` | 프로퍼티 매핑 등록 |
| `TranslateFieldName(name)` | 필드명 변환 |
| `Translate(expression)` | Expression Tree 전체 변환 |

## FAQ

### Q1: EF Core에는 이미 매핑 기능이 있는데 왜 PropertyMap이 필요한가요?
**A**: EF Core의 `HasColumnName`은 EF Core 내부에서만 작동합니다. Specification은 도메인 계층에 있고, EF Core는 인프라 계층에 있으므로 **도메인 계층이 EF Core에 의존하지 않으면서** Expression을 변환해야 합니다. PropertyMap은 이 변환을 인프라 계층에서 수행합니다.

### Q2: PropertyMap은 반드시 필요한가요?
**A**: 도메인 모델과 DB 모델의 프로퍼티 이름이 동일하다면 필요 없습니다. 하지만 실무에서는 DB 컬럼 네이밍 규칙, 레거시 테이블 구조 등의 이유로 이름이 다른 경우가 많습니다.

### Q3: 복합 Expression (And/Or/Not)도 변환되나요?
**A**: 네. `SpecificationExpressionResolver.TryResolve`가 복합 Specification을 단일 Expression으로 합성한 후, `PropertyMap.Translate`가 해당 Expression 전체를 변환합니다. TranslatingVisitor가 Expression Tree의 모든 노드를 재귀적으로 방문하므로 복합 Expression도 올바르게 변환됩니다.
