# Part 2 - Chapter 6: ExpressionSpecification 클래스

> **Part 2: Expression Specification** | [← 목차로](../../../README.md)

---

## 개요

이 장에서는 Functorium 프레임워크의 `ExpressionSpecification<T>` 추상 클래스를 학습합니다. 이 클래스는 `Specification<T>`을 상속하면서 `ToExpression()` 메서드를 통해 Expression Tree를 제공하고, `IsSatisfiedBy()`는 sealed로 자동 컴파일 + 캐싱을 수행합니다.

> **ToExpression()만 구현하면 IsSatisfiedBy()가 자동으로 제공됩니다.**

## 학습 목표

### 핵심 학습 목표
1. **ExpressionSpecification의 설계 의도 이해**
   - `ToExpression()`을 오버라이드하여 조건을 Expression Tree로 정의
   - `IsSatisfiedBy()`는 sealed로 하위 클래스에서 오버라이드 불가
   - Expression을 한 번 컴파일하여 캐싱하는 패턴

2. **Specification과의 차이점**
   - `Specification<T>`: `IsSatisfiedBy()`를 직접 구현
   - `ExpressionSpecification<T>`: `ToExpression()`을 구현하면 나머지는 자동
   - Expression 기반이므로 ORM에서 SQL 변환 가능

3. **실제 Specification 정의 방법**
   - 파라미터 없는 Specification (InStockExprSpec)
   - 생성자 파라미터를 가진 Specification (PriceRangeExprSpec, CategoryExprSpec)

### 실습을 통해 확인할 내용
- IsSatisfiedBy가 Expression 컴파일 결과를 올바르게 반환
- ToExpression으로 IQueryable에서 직접 사용 가능
- 캐싱으로 반복 호출 시 일관된 결과 보장

## 핵심 개념

### sealed IsSatisfiedBy

`ExpressionSpecification<T>`의 핵심 설계는 `IsSatisfiedBy()`를 sealed로 선언하는 것입니다. 이를 통해:

1. **일관성 보장**: ToExpression()과 IsSatisfiedBy()가 항상 동일한 조건을 평가
2. **캐싱 자동화**: 컴파일된 델리게이트를 내부적으로 캐싱
3. **실수 방지**: 하위 클래스에서 조건을 두 곳에 중복 정의하는 것을 방지

```csharp
public abstract class ExpressionSpecification<T> : Specification<T>
{
    private Func<T, bool>? _compiled;

    public abstract Expression<Func<T, bool>> ToExpression();

    public sealed override bool IsSatisfiedBy(T entity)
    {
        _compiled ??= ToExpression().Compile();
        return _compiled(entity);
    }
}
```

### 델리게이트 캐싱 패턴

`_compiled ??= ToExpression().Compile()`는 null 병합 할당 연산자를 사용하여, 첫 번째 호출에서만 Compile()이 실행되고 이후에는 캐싱된 델리게이트가 재사용됩니다.

### ExpressionSpecification 구현

```csharp
// 파라미터 없는 Specification
public sealed class InStockExprSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Stock > 0;
}

// 생성자 파라미터가 있는 Specification
public sealed class PriceRangeExprSpec(decimal min, decimal max)
    : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Price >= min && product.Price <= max;
}
```

## 프로젝트 설명

### 프로젝트 구조
```
ExpressionSpec/                           # 메인 프로젝트
├── Program.cs                            # ExpressionSpecification 데모
├── Product.cs                            # 상품 레코드
├── Specifications/
│   ├── InStockExprSpec.cs                # 재고 Specification
│   ├── PriceRangeExprSpec.cs             # 가격 범위 Specification
│   └── CategoryExprSpec.cs               # 카테고리 Specification
├── ExpressionSpec.csproj                 # 프로젝트 파일
ExpressionSpec.Tests.Unit/                # 테스트 프로젝트
├── ExpressionSpecTests.cs                # ExpressionSpecification 테스트
├── Using.cs                              # 글로벌 using
├── xunit.runner.json                     # xUnit 설정
├── ExpressionSpec.Tests.Unit.csproj      # 테스트 프로젝트 파일
README.md                                 # 이 문서
```

## 한눈에 보는 정리

### Specification vs ExpressionSpecification
| 구분 | `Specification<T>` | `ExpressionSpecification<T>` |
|------|--------------------|-----------------------------|
| **구현 대상** | `IsSatisfiedBy()` | `ToExpression()` |
| **IsSatisfiedBy** | 직접 구현 | sealed (자동 컴파일) |
| **Expression** | 없음 | `ToExpression()`으로 제공 |
| **SQL 변환** | 불가능 | ORM 어댑터가 변환 가능 |
| **캐싱** | 없음 | 컴파일 결과 자동 캐싱 |
| **사용 시점** | 메모리 전용 조건 | DB 쿼리 변환이 필요한 조건 |

### ExpressionSpecification 선택 기준
- DB에서 실행될 수 있는 조건 → `ExpressionSpecification<T>`
- 메모리에서만 실행되는 복잡한 로직 → `Specification<T>`

## FAQ

### Q1: 왜 IsSatisfiedBy가 sealed인가요?
**A**: ToExpression()과 IsSatisfiedBy()가 항상 동일한 조건을 평가하도록 보장하기 위해서입니다. 만약 sealed가 아니라면, 하위 클래스에서 IsSatisfiedBy()를 ToExpression()과 다른 조건으로 오버라이드할 수 있어 일관성이 깨질 수 있습니다.

### Q2: 캐싱은 스레드 안전한가요?
**A**: `??=` 연산자 자체는 원자적이지 않지만, Compile() 결과가 항상 동일하므로 여러 스레드에서 동시에 초기화해도 실질적인 문제가 발생하지 않습니다. 최악의 경우 Compile()이 여러 번 호출될 수 있지만, 결과는 동일합니다.

### Q3: ExpressionSpecification에서 And/Or/Not 조합은 어떻게 동작하나요?
**A**: Specification의 `&`, `|`, `!` 연산자로 조합할 수 있습니다. 조합된 Specification에서 Expression을 추출하려면 `SpecificationExpressionResolver`가 필요합니다. 이는 4장(Expression Resolver)에서 다룹니다.

### Q4: 언제 ExpressionSpecification 대신 일반 Specification을 사용해야 하나요?
**A**: Expression Tree로 표현할 수 없는 복잡한 로직(예: 외부 서비스 호출, 복잡한 문자열 처리 등)은 일반 Specification을 사용합니다. Expression Tree는 SQL로 변환 가능한 단순한 조건식에 적합합니다.
