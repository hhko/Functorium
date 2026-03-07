---
title: "프리미티브 변환"
---
## 개요

도메인 모델에서 가격을 `decimal`이 아닌 `Money` Value Object로 표현한다고 가정해보겠습니다. Expression Tree에서 `product.Price.Value > 1000`과 같은 표현은 EF Core가 SQL로 변환할 수 없습니다 — EF Core는 Value Object의 내부 구조를 모르기 때문입니다. 이 장에서는 Value Object를 primitive 타입으로 변환하는 패턴을 학습합니다.

> **Expression Tree에서 Value Object를 사용하려면 primitive 타입으로 변환해야 합니다.**

## 학습 목표

### 핵심 학습 목표
1. **Value Object가 Expression Tree에서 문제가 되는 이유 이해**
   - 클로저가 Value Object를 직접 캡처하면 Expression Tree에 VO 타입이 포함됨
   - ORM은 VO 타입을 SQL 컬럼으로 매핑할 수 없음
   - Expression Tree 내부에 VO가 있으면 SQL 변환 실패

2. **로컬 변수 추출 패턴 학습**
   - VO를 메서드 내에서 primitive 로컬 변수로 변환
   - Expression 람다에서는 primitive 로컬 변수만 참조
   - 엔터티의 VO 속성은 명시적 캐스트로 primitive 변환

3. **캐스트 패턴 `(string)product.Name` 이해**
   - implicit 연산자를 통해 VO를 primitive로 변환
   - Expression Tree에서 Convert 노드로 표현됨
   - ORM의 PropertyMap이 이를 실제 DB 컬럼으로 매핑

### 실습을 통해 확인할 내용
- Value Object 속성을 가진 Product에 대한 Specification 정의
- 각 Specification이 IsSatisfiedBy로 올바르게 동작
- ToExpression 결과로 AsQueryable 필터링 가능

## 핵심 개념

### 문제: Value Object를 직접 캡처하면

```csharp
// 문제가 되는 코드 (VO가 Expression Tree에 직접 포함됨)
public override Expression<Func<Product, bool>> ToExpression()
    => product => product.Name == Name;  // Name은 ProductName 타입
    // ORM이 ProductName 타입을 SQL로 변환할 수 없음!
```

### 해결: 로컬 변수 추출 + 캐스트 패턴

```csharp
public override Expression<Func<Product, bool>> ToExpression()
{
    // 1. Value Object를 로컬 변수로 추출하여 primitive로 변환
    string nameStr = Name;  // implicit operator 호출

    // 2. Expression 람다에서는 primitive만 참조 + 엔터티 속성도 캐스트
    return product => (string)product.Name == nameStr;
}
```

이 패턴이 필요한 이유:
1. **`string nameStr = Name`**: 클로저가 캡처하는 값이 string이 됨 (VO가 아님)
2. **`(string)product.Name`**: Expression Tree에 Convert 노드가 생성되어 ORM이 해석 가능

### Value Object 정의

```csharp
public sealed record ProductName(string Value)
{
    public static implicit operator string(ProductName name) => name.Value;
}

public sealed record Money(decimal Amount)
{
    public static implicit operator decimal(Money money) => money.Amount;
}

public sealed record Quantity(int Value)
{
    public static implicit operator int(Quantity qty) => qty.Value;
}
```

`implicit operator`를 통해 VO에서 primitive로의 암묵적 변환을 지원합니다.

## 프로젝트 설명

### 프로젝트 구조
```
ValueObjectConversion/                           # 메인 프로젝트
├── Program.cs                                   # Value Object Spec 데모
├── Product.cs                                   # VO 기반 상품 레코드
├── Specifications/
│   ├── ProductNameSpec.cs                       # 이름 Specification
│   ├── ProductPriceRangeSpec.cs                 # 가격 범위 Specification
│   └── ProductLowStockSpec.cs                   # 재고 부족 Specification
├── ValueObjectConversion.csproj                 # 프로젝트 파일
ValueObjectConversion.Tests.Unit/                # 테스트 프로젝트
├── ValueObjectConversionTests.cs                # VO 변환 테스트
├── Using.cs                                     # 글로벌 using
├── xunit.runner.json                            # xUnit 설정
├── ValueObjectConversion.Tests.Unit.csproj      # 테스트 프로젝트 파일
README.md                                        # 이 문서
```

### 핵심 코드

#### ProductPriceRangeSpec.cs
```csharp
public sealed class ProductPriceRangeSpec : ExpressionSpecification<Product>
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    public ProductPriceRangeSpec(Money min, Money max) { MinPrice = min; MaxPrice = max; }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        decimal min = MinPrice;  // Money → decimal
        decimal max = MaxPrice;  // Money → decimal
        return product => (decimal)product.Price >= min && (decimal)product.Price <= max;
    }
}
```

## 한눈에 보는 정리

### 변환 패턴 요약
| 단계 | 코드 | 설명 |
|------|------|------|
| **파라미터 변환** | `string nameStr = Name;` | VO를 primitive 로컬 변수로 변환 |
| **속성 캐스트** | `(string)product.Name` | 엔터티의 VO 속성을 primitive로 캐스트 |
| **Expression 생성** | `product => (string)product.Name == nameStr` | primitive만 포함된 Expression |

### VO 타입별 변환 예시
| Value Object | Primitive | 파라미터 변환 | 속성 캐스트 |
|-------------|-----------|--------------|------------|
| `ProductName` | `string` | `string nameStr = Name;` | `(string)product.Name` |
| `Money` | `decimal` | `decimal min = MinPrice;` | `(decimal)product.Price` |
| `Quantity` | `int` | `int threshold = Threshold;` | `(int)product.Stock` |

## FAQ

### Q1: 왜 implicit operator만으로는 충분하지 않나요?
**A**: C# 컴파일러는 Expression 람다 내에서 implicit 변환을 자동으로 삽입하지만, 클로저가 캡처하는 객체의 타입까지 변환하지는 않습니다. 파라미터를 로컬 변수로 추출하지 않으면, 클로저가 VO 인스턴스를 직접 캡처하여 Expression Tree에 VO 타입이 남게 됩니다.

### Q2: EF Core에서 이 패턴이 실제로 동작하나요?
**A**: 네, EF Core의 ValueConverter와 함께 사용하면 동작합니다. EF Core는 Expression Tree의 Convert 노드를 인식하여 해당 DB 컬럼으로 매핑합니다. Functorium의 PropertyMap 어댑터가 이 변환을 자동으로 처리합니다.

### Q3: 모든 Value Object에 implicit operator가 필요한가요?
**A**: Expression Tree에서 사용할 VO에만 필요합니다. 메모리에서만 사용되는 VO는 explicit cast나 `.Value` 속성 접근으로 충분합니다. implicit operator는 코드의 가독성을 위한 편의 기능입니다.

### Q4: record 대신 class로 VO를 정의해도 되나요?
**A**: 네, 가능합니다. 이 예제에서는 간결함을 위해 record를 사용했지만, 실제 프로젝트에서는 Functorium의 ValueObject 기반 클래스를 사용하여 유효성 검증과 동등성을 자동으로 처리합니다.

---

개별 ExpressionSpecification에서 Expression을 추출하는 방법을 배웠습니다. 하지만 `inStock & affordable` 같은 조합된 Specification에서는 어떻게 하나의 Expression을 얻을 수 있을까요? 다음 장에서는 이 문제를 해결하는 Expression Resolver를 다룹니다.
