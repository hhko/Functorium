# Part 1 - Chapter 3: 연산자 오버로딩

> **Part 1: Specification 기초** | [← 이전: 2장 조합](../02-Composition/README.md) | [다음: 4장 All 항등원 →](../04-All-Identity/README.md)

---

## 개요

2장에서 `And()`, `Or()`, `Not()` 메서드로 Specification을 조합하는 방법을 배웠습니다. 이 장에서는 C#의 연산자 오버로딩을 활용하여 `&`, `|`, `!` 연산자로 **더 간결하고 직관적인 문법**을 사용하는 방법을 배웁니다.

> **`inStock.And(affordable.And(electronics.Not()))` 대신 `inStock & affordable & !electronics`로 작성할 수 있습니다.**

## 학습 목표

### 핵심 학습 목표
1. **연산자 오버로딩 문법 이해**
   - `&` = `And()`, `|` = `Or()`, `!` = `Not()`
   - 메서드와 연산자가 동일한 결과를 생성함을 확인

2. **연산자 선택 이유 이해**
   - `&&`/`||` 대신 `&`/`|`를 사용하는 이유
   - C#의 연산자 오버로딩 제약 사항

3. **가독성과 표현력 비교**
   - 메서드 체인 vs 연산자 문법의 장단점

### 실습을 통해 확인할 내용
- 메서드 방식과 연산자 방식의 결과 비교
- 복합 조건의 연산자 표현

## 핵심 개념

### 연산자 오버로딩 구현

`Specification<T>` 기반 클래스는 세 가지 연산자를 오버로딩합니다:

```csharp
public static Specification<T> operator &(Specification<T> left, Specification<T> right)
    => left.IsAll ? right : right.IsAll ? left : new AndSpecification<T>(left, right);

public static Specification<T> operator |(Specification<T> left, Specification<T> right)
    => new OrSpecification<T>(left, right);

public static Specification<T> operator !(Specification<T> spec)
    => new NotSpecification<T>(spec);
```

### 왜 `&`/`|` 인가? (`&&`/`||`이 아닌 이유)

C#에서 `&&`와 `||`는 직접 오버로딩할 수 없습니다. `&&`는 `&`와 `true`/`false` 연산자를 결합하여 컴파일러가 자동으로 생성하는데, Specification은 `bool`이 아니므로 `true`/`false` 연산자를 정의하는 것이 의미적으로 맞지 않습니다.

`&`와 `|`는 **비트 연산자**이지만, Specification 맥락에서는 논리적 AND/OR로 자연스럽게 읽힙니다.

### `&` 연산자의 항등원 최적화

`&` 연산자는 `And()` 메서드와 달리 **`All` 항등원 최적화**가 포함되어 있습니다:

```csharp
// & 연산자: All이면 상대방을 그대로 반환
left.IsAll ? right : right.IsAll ? left : new AndSpecification<T>(left, right)

// And() 메서드: 항상 새로운 AndSpecification 생성
new AndSpecification<T>(this, other)
```

이 최적화는 4장에서 자세히 다룹니다.

## 프로젝트 설명

### 프로젝트 구조
```
Operators/
├── Program.cs
├── Product.cs
├── Specifications/
│   ├── InStockSpec.cs
│   ├── PriceRangeSpec.cs
│   └── CategorySpec.cs
└── Operators.csproj

Operators.Tests.Unit/
├── OperatorTests.cs
├── Using.cs
├── xunit.runner.json
└── Operators.Tests.Unit.csproj
```

### 핵심 코드

#### 메서드 vs 연산자 비교
```csharp
// 메서드 방식
var method = inStock.And(affordable).And(electronics.Not());

// 연산자 방식 (동일한 결과)
var op = inStock & affordable & !electronics;
```

## 한눈에 보는 정리

### 연산자 매핑
| 연산자 | 메서드 | 의미 |
|--------|--------|------|
| `&` | `And()` | 두 조건 모두 만족 |
| `\|` | `Or()` | 하나라도 만족 |
| `!` | `Not()` | 조건 반전 |

### 메서드 vs 연산자
| 구분 | 메서드 방식 | 연산자 방식 |
|------|------------|------------|
| **가독성** | 명시적 | 간결 |
| **체인** | `.And().And()` | `& &` |
| **All 최적화** | 없음 | `&` 연산자에 포함 |
| **결과** | 동일 | 동일 |

## FAQ

### Q1: 메서드와 연산자 중 어떤 것을 사용해야 하나요?
**A**: 팀 컨벤션에 따릅니다. 연산자는 간결하지만 Specification 패턴에 익숙하지 않은 개발자에게는 `And()`/`Or()` 메서드가 더 명확할 수 있습니다. 두 방식 모두 동일한 결과를 생성하므로 일관성 있게 선택하면 됩니다.

### Q2: `&&`/`||`를 사용할 수 없는 이유를 더 자세히 설명해주세요.
**A**: C#에서 `&&`는 `&` + `true`/`false` 연산자의 조합입니다. `Specification<T>`에 `true`/`false` 연산자를 정의하면 `if (spec)` 같은 코드가 가능해지는데, Specification은 엔터티에 대한 조건이지 그 자체가 참/거짓이 아니므로 의미적으로 부적절합니다.

### Q3: 연산자 우선순위가 문제되지 않나요?
**A**: C#에서 `!`는 `&`보다 우선순위가 높고, `&`는 `|`보다 높습니다. 따라서 `inStock & !electronics | affordable`은 `(inStock & (!electronics)) | affordable`로 평가됩니다. 복잡한 표현에서는 괄호를 사용하여 의도를 명확히 하는 것이 좋습니다.

### Q4: `&` 연산자와 `And()` 메서드의 차이점은 정확히 무엇인가요?
**A**: 결과는 거의 동일하지만, `&` 연산자는 `All` 항등원 최적화가 포함되어 있습니다. `All & X`는 `X`를 그대로 반환하고, `X & All`도 `X`를 그대로 반환합니다. `And()` 메서드는 항상 새로운 `AndSpecification`을 생성합니다. 이 차이는 4장에서 자세히 다룹니다.
