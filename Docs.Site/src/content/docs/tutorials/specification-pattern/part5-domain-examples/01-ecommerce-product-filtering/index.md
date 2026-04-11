---
title: "전자상거래 상품 필터링"
---
## 개요

Part 1부터 Part 4까지 Specification 패턴의 기초, Expression Tree, Repository 통합, 실전 패턴을 차례로 학습했습니다. 이제 이 모든 것을 하나의 완결된 도메인 시나리오에 적용해보겠습니다. 이커머스 플랫폼에서 상품을 다양한 조건으로 필터링하는 실전 예제를 통해, 지금까지 배운 패턴이 실제로 어떻게 조합되는지 확인합니다.

> **Value Object와 Specification의 결합으로 타입 안전한 도메인 필터링을 구현합니다.**

## 학습 목표

1. **Value Object 기반 도메인 모델링**: `ProductName`, `Money`, `Quantity`, `Category` 등 도메인 개념을 타입으로 표현
2. **ExpressionSpecification 구현**: 다양한 필터링 조건을 개별 Specification으로 캡슐화
3. **Specification 조합**: `&`, `|`, `!` 연산자를 활용한 복합 필터링 조건 구성
4. **Repository 패턴 통합**: Specification을 Repository에서 활용하여 상품 검색

## 핵심 개념

### Value Object로 도메인 표현

원시 타입 대신 Value Object를 사용하면 도메인 의미가 코드에 명확하게 드러납니다.

```csharp
// 원시 타입: 의미가 불분명
Product(string name, decimal price, int stock, string category)

// Value Object: 도메인 의미가 명확
Product(ProductName name, Money price, Quantity stock, Category category)
```

### ExpressionSpecification과 Value Object

Expression 내부에서 Value Object를 사용할 때는 implicit 연산자를 통해 원시 타입으로 변환해야 합니다. Expression Tree는 커스텀 타입의 비교를 직접 지원하지 않기 때문입니다.

```csharp
public override Expression<Func<Product, bool>> ToExpression()
{
    decimal min = MinPrice;  // implicit 변환
    decimal max = MaxPrice;
    return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
}
```

### Specification 조합을 통한 복합 필터링

개별 Specification을 `&`, `|`, `!` 연산자로 조합하여 복잡한 비즈니스 조건을 표현합니다.

```csharp
// 전자기기 AND 재고 있음 AND 100만원 이하
var spec = new ProductCategorySpec(new Category("전자기기"))
    & new ProductInStockSpec()
    & new ProductPriceRangeSpec(new Money(0m), new Money(1_000_000m));
```

## 프로젝트 설명

### 프로젝트 구조
```
EcommerceFiltering/
├── Domain/
│   ├── ValueObjects/
│   │   ├── ProductName.cs
│   │   ├── Money.cs
│   │   ├── Quantity.cs
│   │   └── Category.cs
│   ├── Product.cs
│   ├── IProductRepository.cs
│   └── Specifications/
│       ├── ProductNameUniqueSpec.cs
│       ├── ProductPriceRangeSpec.cs
│       ├── ProductLowStockSpec.cs
│       ├── ProductCategorySpec.cs
│       └── ProductInStockSpec.cs
├── Infrastructure/
│   └── InMemoryProductRepository.cs
├── SampleProducts.cs
└── Program.cs
```

### Specification 목록

| Specification | 설명 | 매개변수 |
|---------------|------|----------|
| `ProductNameUniqueSpec` | 상품명 일치 확인 | `ProductName` |
| `ProductPriceRangeSpec` | 가격 범위 필터링 | `Money min`, `Money max` |
| `ProductLowStockSpec` | 재고 부족 상품 | `Quantity threshold` |
| `ProductCategorySpec` | 카테고리 필터링 | `Category` |
| `ProductInStockSpec` | 재고 있는 상품 | 없음 |

## 한눈에 보는 정리

| 구분 | 내용 |
|------|------|
| **도메인** | 전자상거래 상품 (Product) |
| **Value Objects** | ProductName, Money, Quantity, Category |
| **Specification 기반** | 모두 ExpressionSpecification |
| **핵심 패턴** | Specification 조합 (`&`, `|`, `!`) |
| **Repository** | Specification 기반 FindAll, Exists |

## FAQ

### Q1: Expression에서 Value Object를 직접 비교할 수 없는 이유는?
**A**: Expression Tree는 런타임에 SQL 등으로 번역될 수 있어야 합니다. 커스텀 타입의 `==` 연산자는 번역이 불가능하므로, implicit 변환을 통해 원시 타입으로 변환한 후 비교해야 합니다.

### Q2: 모든 Specification이 ExpressionSpecification인 이유는?
**A**: 이 예제에서는 모든 필터링 조건이 Expression Tree로 표현 가능합니다. EF Core 등 ORM과 통합할 때 자동 SQL 번역을 지원하기 위해 ExpressionSpecification을 사용합니다. 다음 장에서는 non-Expression Specification도 함께 사용하는 예제를 다룹니다.

### Q3: SampleProducts에서 한글 변수명을 사용하는 이유는?
**A**: 도메인 예제에서 한글 변수명은 도메인 용어를 그대로 코드에 반영하여 가독성을 높입니다. 실제 프로젝트에서는 팀 컨벤션에 따라 결정합니다.

---

상품 필터링은 Specification 패턴의 가장 직관적인 적용 사례입니다. 다음 장에서는 동일한 패턴을 고객 관리 도메인에 적용하여, Specification의 범용성과 Expression/non-Expression 혼합 사용을 살펴봅니다.

→ [고객 관리](../02-Customer-Management/)
