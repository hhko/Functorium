# Part 1 - Chapter 4: All 항등원과 동적 필터

> **Part 1: Specification 기초** | [← 이전: 3장 연산자](../03-Operators/README.md) | [다음: Part 2 →](../../Part2-Expression-Specification/README.md)

---

## 개요

`Specification<T>.All`은 **모든 엔터티를 만족하는 특별한 Specification**입니다. 이 장에서는 `All`이 AND 연산의 **항등원(identity element)**으로서 동적 필터 구성을 어떻게 간결하게 만드는지 배웁니다.

> **`All & X == X` — 수학의 1 * x == x처럼, All은 AND 연산에서 아무 영향을 주지 않는 중립 요소입니다.**

## 학습 목표

### 핵심 학습 목표
1. **Null Object 패턴 이해**
   - `All`은 "조건 없음"을 표현하는 Null Object
   - null 체크 대신 `All`로 시작하여 안전하게 조건 누적

2. **항등원(Identity Element) 개념**
   - `All & X == X`, `X & All == X` (AND 연산의 항등원)
   - `&` 연산자가 `All`을 감지하면 새 객체를 만들지 않고 상대방을 그대로 반환

3. **동적 필터 패턴**
   - 사용자 입력이나 검색 조건에 따라 Specification을 점진적으로 조립
   - `All`에서 시작하여 `&=`로 조건을 추가하는 패턴

### 실습을 통해 확인할 내용
- `All.IsSatisfiedBy()`가 항상 true 반환
- `All & X`가 `X`와 동일한 참조 반환 (ReferenceEquals)
- 동적 필터 구성 및 실행

## 핵심 개념

### Null Object 패턴

프로그래밍에서 "조건 없음"을 표현할 때 흔히 `null`을 사용합니다:

```csharp
// null 사용 - 매번 null 체크 필요
Specification<Product>? spec = null;

if (hasCategory)
    spec = spec == null ? new CategorySpec(cat) : spec.And(new CategorySpec(cat));

if (hasPrice)
    spec = spec == null ? new PriceRangeSpec(min, max) : spec.And(new PriceRangeSpec(min, max));

// 실행 시에도 null 체크
var results = spec == null ? products : products.Where(p => spec.IsSatisfiedBy(p));
```

`All`을 사용하면 null 체크가 사라집니다:

```csharp
// All 사용 - null 체크 불필요
var spec = Specification<Product>.All;

if (hasCategory)
    spec &= new CategorySpec(cat);

if (hasPrice)
    spec &= new PriceRangeSpec(min, max);

// 실행 - All이면 모든 상품, 조건이 있으면 필터링
var results = products.Where(p => spec.IsSatisfiedBy(p));
```

### 항등원의 참조 최적화

`&` 연산자는 `All`을 감지하면 **새 객체를 생성하지 않고 상대방을 그대로 반환**합니다:

```csharp
var all = Specification<Product>.All;
var inStock = new InStockSpec();

var result = all & inStock;
ReferenceEquals(result, inStock); // true - 새 객체가 아님!
```

이는 단순한 최적화가 아니라, **항등원의 수학적 정의**를 코드로 표현한 것입니다. 곱셈에서 `1 * x == x`인 것처럼, AND 연산에서 `All & X == X`입니다.

### 동적 필터 패턴

실무에서 가장 많이 사용되는 패턴입니다. 검색 화면에서 사용자가 선택한 조건만 적용해야 할 때:

```csharp
var spec = Specification<Product>.All;

if (categoryFilter is not null)
    spec &= new CategorySpec(categoryFilter);

if (nameFilter is not null)
    spec &= new NameContainsSpec(nameFilter);

if (onlyInStock)
    spec &= new InStockSpec();

var results = products.Where(p => spec.IsSatisfiedBy(p));
```

조건이 하나도 선택되지 않으면 `spec`은 `All` 그대로이므로 모든 상품이 반환됩니다.

## 프로젝트 설명

### 프로젝트 구조
```
AllIdentity/
├── Program.cs
├── Product.cs
├── SampleProducts.cs
├── Specifications/
│   ├── InStockSpec.cs
│   ├── PriceRangeSpec.cs
│   ├── CategorySpec.cs
│   └── NameContainsSpec.cs
└── AllIdentity.csproj

AllIdentity.Tests.Unit/
├── AllIdentityTests.cs
├── Using.cs
├── xunit.runner.json
└── AllIdentity.Tests.Unit.csproj
```

### 핵심 코드

#### 동적 필터 구성
```csharp
var spec = Specification<Product>.All;

if (categoryFilter is not null)
    spec &= new CategorySpec(categoryFilter);

if (onlyInStock)
    spec &= new InStockSpec();

var results = SampleProducts.All.Where(p => spec.IsSatisfiedBy(p));
```

## 한눈에 보는 정리

### All 항등원 특성
| 속성 | 값/동작 |
|------|---------|
| `All.IsSatisfiedBy(x)` | 항상 `true` |
| `All.IsAll` | `true` |
| `All & X` | `X` 반환 (ReferenceEquals) |
| `X & All` | `X` 반환 (ReferenceEquals) |

### null vs All 비교
| 구분 | null 방식 | All 방식 |
|------|-----------|----------|
| **초기값** | `null` | `Specification<T>.All` |
| **조건 추가** | null 체크 후 And 또는 할당 | `&=` 연산자 |
| **실행** | null 체크 후 필터 또는 전체 반환 | 그냥 필터 (All이면 전체 반환) |
| **안전성** | NullReferenceException 위험 | null-safe |

## FAQ

### Q1: All은 싱글턴인가요?
**A**: 네, `AllSpecification<T>`는 `static readonly` 인스턴스를 통해 타입별 싱글턴으로 구현되어 있습니다. `Specification<Product>.All`은 항상 동일한 객체를 반환합니다.

### Q2: All을 Or 연산에서도 사용할 수 있나요?
**A**: 기술적으로 가능하지만 의미가 다릅니다. `All | X`는 항상 true (All이 모든 것을 만족하므로)가 되어 사실상 `All`과 동일합니다. `All`은 AND 연산의 항등원으로 설계된 것이며, Or 연산에서는 흡수원(absorbing element)이 됩니다.

### Q3: 왜 And() 메서드에는 항등원 최적화가 없나요?
**A**: `And()` 메서드는 단순히 `new AndSpecification<T>(this, other)`를 반환합니다. 항등원 최적화는 `&` 연산자에만 포함되어 있습니다. 이는 메서드의 단순성을 유지하면서도, 연산자 사용 시 성능과 참조 동일성을 보장하기 위한 설계 결정입니다.

### Q4: 동적 필터에서 Or 조건도 추가할 수 있나요?
**A**: 네, `|=` 연산자를 사용할 수 있습니다. 다만 And와 Or를 혼합할 때는 연산 우선순위에 주의해야 합니다. 복잡한 조합이 필요하다면 중간 변수로 의도를 명확히 하는 것이 좋습니다.

### Q5: 이 패턴은 실무에서 어떻게 활용되나요?
**A**: 검색 API, 필터링 UI, 리포트 조건 등 사용자 입력에 따라 쿼리를 동적으로 구성해야 하는 모든 곳에서 활용됩니다. Part 3에서 EF Core와 결합하여 동적 SQL 쿼리를 생성하는 방법을 배웁니다.
