---
title: "첫 번째 명세"
---
## 개요

Specification 패턴의 핵심은 **비즈니스 조건을 독립적인 객체로 캡슐화**하는 것입니다. 이 장에서는 `Specification<T>` 추상 클래스를 상속하여 첫 번째 Specification을 직접 만들어봅니다.

> **"재고가 있는가?", "가격이 범위 안에 있는가?" — 이런 질문 하나하나가 Specification 객체가 됩니다.**

## 학습 목표

### 핵심 학습 목표
1. **Specification\<T\> 추상 클래스의 구조 이해**
   - `IsSatisfiedBy(T entity)` 메서드의 역할과 구현 방법
   - 추상 클래스를 상속하여 구체적인 조건을 정의하는 패턴

2. **도메인 조건의 객체화**
   - 조건문(`if`)을 Specification 객체로 분리하는 이유
   - 재사용 가능하고 테스트 가능한 조건 표현

3. **경계값 테스트의 중요성**
   - Specification의 정확한 동작을 경계값으로 검증

### 실습을 통해 확인할 내용
- **ProductInStockSpec**: 재고가 0보다 큰 상품 필터링
- **ProductPriceRangeSpec**: 최솟값/최댓값 범위 내 상품 필터링

## 핵심 개념

### Specification\<T\> 추상 클래스

Functorium의 `Specification<T>`는 도메인 조건을 캡슐화하는 추상 기반 클래스입니다. 핵심은 단 하나의 메서드입니다:

```csharp
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T entity);
}
```

이 설계가 인터페이스가 아닌 **추상 클래스**인 이유는, `And()`, `Or()`, `Not()` 같은 조합 메서드와 연산자 오버로딩을 기반 클래스에서 제공하기 위함입니다. 이를 통해 모든 Specification이 조합 기능을 자동으로 갖게 됩니다.

### 조건을 객체로 분리하는 이유

일반적인 코드에서는 조건을 `if`문이나 람다로 직접 작성합니다:

```csharp
// 인라인 조건 - 재사용 불가, 테스트 어려움
var inStock = products.Where(p => p.Stock > 0);
```

Specification으로 분리하면:

```csharp
// Specification 객체 - 재사용 가능, 단위 테스트 가능
var spec = new ProductInStockSpec();
var inStock = products.Where(p => spec.IsSatisfiedBy(p));
```

조건이 복잡해질수록 이 분리의 가치가 커집니다.

## 프로젝트 설명

### 프로젝트 구조
```
FirstSpecification/
├── Program.cs                          # 데모 실행
├── Product.cs                          # 도메인 모델
├── Specifications/
│   ├── ProductInStockSpec.cs                  # 재고 확인 Specification
│   └── ProductPriceRangeSpec.cs               # 가격 범위 Specification
└── FirstSpecification.csproj

FirstSpecification.Tests.Unit/
├── ProductInStockSpecTests.cs                 # 재고 경계값 테스트
├── ProductPriceRangeSpecTests.cs              # 가격 범위 경계값 테스트
├── Using.cs
├── xunit.runner.json
└── FirstSpecification.Tests.Unit.csproj
```

### 핵심 코드

#### Product.cs
```csharp
public record Product(string Name, decimal Price, int Stock, string Category);
```

#### ProductInStockSpec.cs
```csharp
public sealed class ProductInStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
```

#### ProductPriceRangeSpec.cs
```csharp
public sealed class ProductPriceRangeSpec(decimal min, decimal max) : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) =>
        entity.Price >= min && entity.Price <= max;
}
```

## 한눈에 보는 정리

### Specification\<T\> 설계 요약
| 구분 | 설명 |
|------|------|
| **기반 타입** | `abstract class Specification<T>` |
| **핵심 메서드** | `IsSatisfiedBy(T entity)` — 조건 충족 여부 반환 |
| **왜 추상 클래스인가** | 조합 메서드(`And`, `Or`, `Not`)와 연산자 오버로딩을 기반에서 제공 |
| **구현 방법** | 상속 후 `IsSatisfiedBy` 오버라이드 |

### 인라인 조건 vs Specification
| 구분 | 인라인 조건 | Specification |
|------|-------------|---------------|
| **재사용성** | 낮음 (복사/붙여넣기) | 높음 (객체 공유) |
| **테스트 용이성** | 어려움 | 쉬움 (단위 테스트) |
| **가독성** | 복잡해지면 저하 | 이름으로 의도 전달 |
| **조합** | 수동 (`&&`, `\|\|`) | 자동 (`And`, `Or`, `Not`) |

## FAQ

### Q1: 왜 인터페이스가 아닌 추상 클래스인가요?
**A**: `Specification<T>`는 `And()`, `Or()`, `Not()` 조합 메서드와 `&`, `|`, `!` 연산자 오버로딩을 기반 클래스에서 구현합니다. 인터페이스에서는 연산자 오버로딩을 제공할 수 없고, 조합 로직이 매번 중복될 수 있습니다. 추상 클래스를 사용하면 모든 Specification이 조합 기능을 자동으로 상속받습니다.

### Q2: `sealed` 키워드를 사용하는 이유가 있나요?
**A**: 구체적인 Specification 클래스(`ProductInStockSpec`, `ProductPriceRangeSpec`)는 더 이상 상속할 필요가 없는 최종 구현입니다. `sealed`를 붙이면 의도하지 않은 상속을 방지하고, JIT 컴파일러가 가상 호출을 최적화할 수 있습니다.

### Q3: Specification에 상태(생성자 매개변수)를 전달해도 되나요?
**A**: 네, `ProductPriceRangeSpec(min, max)`처럼 생성 시점에 조건 매개변수를 받는 것은 자연스러운 패턴입니다. Specification은 생성 후 불변(immutable)이어야 하며, `IsSatisfiedBy` 호출 시 외부 상태에 의존하지 않아야 합니다.

### Q4: 경계값 테스트가 왜 중요한가요?
**A**: `ProductPriceRangeSpec(100, 500)`에서 가격이 정확히 100이거나 500일 때의 동작은 `>=`인지 `>`인지에 따라 달라집니다. 경계값 테스트는 이런 미묘한 차이를 명확히 검증하여 Specification의 정확한 의미를 보장합니다.

### Q5: 다음 장에서는 무엇을 배우나요?
**A**: 2장에서는 여러 Specification을 `And`, `Or`, `Not`으로 조합하는 방법을 배웁니다. 예를 들어 "재고가 있고 AND 가격이 범위 안에 있는" 복합 조건을 만들 수 있습니다.
