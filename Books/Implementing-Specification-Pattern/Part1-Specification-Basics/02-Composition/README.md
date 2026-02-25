# Part 1 - Chapter 2: Specification 조합

> **Part 1: Specification 기초** | [← 이전: 1장 첫 번째 Specification](../01-First-Specification/README.md) | [다음: 3장 연산자 →](../03-Operators/README.md)

---

## 개요

개별 Specification을 만드는 것만으로는 복잡한 비즈니스 조건을 표현할 수 없습니다. 이 장에서는 `And()`, `Or()`, `Not()` 메서드를 사용하여 여러 Specification을 **논리적으로 조합**하는 방법을 배웁니다.

> **작은 조건들을 레고 블록처럼 조합하여 복잡한 비즈니스 규칙을 만들 수 있습니다.**

## 학습 목표

### 핵심 학습 목표
1. **And/Or/Not 조합 메서드 사용법**
   - `And()`: 두 조건이 모두 참일 때만 참
   - `Or()`: 하나라도 참이면 참
   - `Not()`: 조건을 반전

2. **내부 조합 클래스 구조 이해**
   - `AndSpecification<T>`, `OrSpecification<T>`, `NotSpecification<T>`의 역할
   - 기반 클래스에서 제공하므로 사용자가 직접 구현할 필요 없음

3. **체인 조합 패턴**
   - `spec1.And(spec2.Not())` 같은 연쇄 조합

### 실습을 통해 확인할 내용
- **And 조합**: 재고 있고 저렴한 상품
- **Or 조합**: 전자제품이거나 저렴한 상품
- **Not 조합**: 전자제품이 아닌 상품
- **복합 조합**: And + Not 결합

## 핵심 개념

### 조합의 내부 구조

`Specification<T>` 기반 클래스는 세 가지 조합 메서드를 제공합니다:

```csharp
public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);
public Specification<T> Or(Specification<T> other)  => new OrSpecification<T>(this, other);
public Specification<T> Not()                        => new NotSpecification<T>(this);
```

각 조합은 내부적으로 `AndSpecification`, `OrSpecification`, `NotSpecification` 클래스를 생성합니다. 이 클래스들은 `internal`로 선언되어 있어 라이브러리 사용자에게 노출되지 않습니다.

### AndSpecification 동작 원리

```csharp
internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right)
    : Specification<T>
{
    public override bool IsSatisfiedBy(T entity)
        => left.IsSatisfiedBy(entity) && right.IsSatisfiedBy(entity);
}
```

`left`와 `right` 두 Specification을 보관하고, `IsSatisfiedBy` 호출 시 둘 다 만족하는지 확인합니다. `OrSpecification`과 `NotSpecification`도 같은 원리로 동작합니다.

### 조합은 새로운 Specification을 반환한다

중요한 점은 `And()`, `Or()`, `Not()`이 **원본을 변경하지 않고 새로운 Specification 객체를 반환**한다는 것입니다. 이는 불변성(immutability)을 보장합니다.

```csharp
var inStock = new InStockSpec();
var affordable = new PriceRangeSpec(10_000m, 100_000m);

// 원본은 변경되지 않음 - 새로운 Specification이 생성됨
var combined = inStock.And(affordable);
```

## 프로젝트 설명

### 프로젝트 구조
```
Composition/
├── Program.cs
├── Product.cs
├── Specifications/
│   ├── InStockSpec.cs
│   ├── PriceRangeSpec.cs
│   └── CategorySpec.cs
└── Composition.csproj

Composition.Tests.Unit/
├── CompositionTests.cs
├── Using.cs
├── xunit.runner.json
└── Composition.Tests.Unit.csproj
```

### 핵심 코드

#### CategorySpec.cs
```csharp
public sealed class CategorySpec(string category) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Category.Equals(category, StringComparison.OrdinalIgnoreCase);
}
```

#### 조합 사용 예
```csharp
var inStock = new InStockSpec();
var affordable = new PriceRangeSpec(10_000m, 100_000m);
var electronics = new CategorySpec("전자제품");

// And: 재고 있고 저렴한 상품
var spec1 = inStock.And(affordable);

// Or: 전자제품이거나 저렴한 상품
var spec2 = electronics.Or(affordable);

// Not: 전자제품이 아닌 상품
var spec3 = electronics.Not();

// 복합: 재고 있고 전자제품이 아닌 상품
var spec4 = inStock.And(electronics.Not());
```

## 한눈에 보는 정리

### 조합 메서드 요약
| 메서드 | 내부 클래스 | 동작 |
|--------|------------|------|
| `And(other)` | `AndSpecification<T>` | 두 조건 모두 참일 때 참 |
| `Or(other)` | `OrSpecification<T>` | 하나라도 참이면 참 |
| `Not()` | `NotSpecification<T>` | 조건을 반전 |

### 조합의 특성
| 특성 | 설명 |
|------|------|
| **불변성** | 원본 Specification을 변경하지 않고 새 객체 반환 |
| **체인 가능** | `a.And(b.Not())`처럼 연쇄 조합 가능 |
| **내부 구현** | 조합 클래스는 `internal`로 캡슐화됨 |
| **자동 제공** | 기반 클래스에서 제공하므로 직접 구현 불필요 |

## FAQ

### Q1: 조합 클래스가 internal인 이유가 있나요?
**A**: 사용자는 `And()`, `Or()`, `Not()` 메서드만 사용하면 됩니다. 내부 조합 클래스의 구현 세부사항을 노출하면 라이브러리의 캡슐화가 깨지고, 향후 구현을 변경하기 어려워집니다.

### Q2: 조합을 얼마나 깊게 중첩할 수 있나요?
**A**: 기술적으로 제한은 없지만, 너무 깊은 중첩은 가독성을 해칩니다. 복잡한 조합이 필요하다면 중간 변수에 이름을 부여하거나, 의미 있는 이름의 새로운 Specification 클래스를 만드는 것이 좋습니다.

### Q3: And와 Or의 평가 순서가 중요한가요?
**A**: `AndSpecification`은 `&&` 연산자를 사용하므로 **단축 평가(short-circuit evaluation)**가 적용됩니다. 즉, 왼쪽이 false이면 오른쪽은 평가하지 않습니다. `OrSpecification`도 마찬가지로 왼쪽이 true이면 오른쪽을 평가하지 않습니다.

### Q4: 다음 장에서는 무엇을 배우나요?
**A**: 3장에서는 `And()`, `Or()`, `Not()` 메서드 대신 `&`, `|`, `!` 연산자를 사용하는 방법을 배웁니다. 연산자 오버로딩을 통해 더 간결하고 직관적인 문법으로 조합을 표현할 수 있습니다.
