---
title: "ExpressionSpecification 클래스"
---
## 개요

Expression Tree를 활용하면 Specification을 ORM이 이해할 수 있는 형태로 만들 수 있습니다. 하지만 매번 Expression을 수동으로 컴파일하고 캐싱하는 것은 번거롭습니다. `ExpressionSpecification<T>`는 이 과정을 자동화합니다 — `ToExpression()`만 구현하면 `IsSatisfiedBy`는 컴파일된 델리게이트를 캐싱하여 자동으로 제공됩니다.

> **ToExpression()만 구현하면 IsSatisfiedBy()가 자동으로 제공됩니다.**

## 학습 목표

### 핵심 학습 목표
1. **ExpressionSpecification의 설계 의도를** 설명할 수 있습니다
   - `ToExpression()`을 오버라이드하여 조건을 Expression Tree로 정의
   - `IsSatisfiedBy()`는 sealed로 하위 클래스에서 오버라이드 불가
   - Expression을 한 번 컴파일하여 캐싱하는 패턴

2. **Specification과의 차이점을** 구분할 수 있습니다
   - `Specification<T>`: `IsSatisfiedBy()`를 직접 구현
   - `ExpressionSpecification<T>`: `ToExpression()`을 구현하면 나머지는 자동
   - Expression 기반이므로 ORM에서 SQL 변환 가능

3. **ExpressionSpecification을 상속하여** 구체 Specification을 정의할 수 있습니다
   - 파라미터 없는 Specification (ProductInStockSpec)
   - 생성자 파라미터를 가진 Specification (ProductPriceRangeSpec, ProductCategorySpec)

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
public abstract class ExpressionSpecification<T> : Specification<T>, IExpressionSpec<T>
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

### IExpressionSpec<T> 인터페이스

`ExpressionSpecification<T>`는 `IExpressionSpec<T>` 인터페이스를 구현합니다. 이 인터페이스는 `ToExpression()` 메서드만 정의하며, 4장에서 다룰 `SpecificationExpressionResolver`가 패턴 매칭(`spec is IExpressionSpec<T>`)으로 Expression을 추출하는 핵심 열쇠입니다.

```csharp
public interface IExpressionSpec<T>
{
    Expression<Func<T, bool>> ToExpression();
}
```

### 델리게이트 캐싱 패턴

`_compiled ??= ToExpression().Compile()`는 null 병합 할당 연산자를 사용하여, 첫 번째 호출에서만 Compile()이 실행되고 이후에는 캐싱된 델리게이트가 재사용됩니다.

### ExpressionSpecification 구현

```csharp
// 파라미터 없는 Specification
public sealed class ProductInStockSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Stock > 0;
}

// 생성자 파라미터가 있는 Specification
public sealed class ProductPriceRangeSpec(decimal min, decimal max)
    : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
        => product => product.Price >= min && product.Price <= max;
}
```

## 프로젝트 설명

이 개념을 코드로 확인해보겠습니다.

### 프로젝트 구조
```
ExpressionSpec/                           # 메인 프로젝트
├── Program.cs                            # ExpressionSpecification 데모
├── Product.cs                            # 상품 레코드
├── Specifications/
│   ├── ProductInStockSpec.cs                # 재고 Specification
│   ├── ProductPriceRangeSpec.cs             # 가격 범위 Specification
│   └── ProductCategorySpec.cs               # 카테고리 Specification
├── ExpressionSpec.csproj                 # 프로젝트 파일
ExpressionSpec.Tests.Unit/                # 테스트 프로젝트
├── ExpressionSpecTests.cs                # ExpressionSpecification 테스트
├── Using.cs                              # 글로벌 using
├── xunit.runner.json                     # xUnit 설정
├── ExpressionSpec.Tests.Unit.csproj      # 테스트 프로젝트 파일
index.md                                  # 이 문서
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
**A**: Specification의 `&`, `|`, `!` 연산자로 조합할 수 있습니다. 조합된 Specification에서 Expression을 추출하려면 `SpecificationExpressionResolver`가 필요합니다. 이는 [4장: Expression Resolver](../04-Expression-Resolver/)에서 다룹니다.

### Q4: 언제 ExpressionSpecification 대신 일반 Specification을 사용해야 하나요?
**A**: Expression Tree로 표현할 수 없는 복잡한 로직(예: 외부 서비스 호출, 복잡한 문자열 처리 등)은 일반 Specification을 사용합니다. Expression Tree는 SQL로 변환 가능한 단순한 조건식에 적합합니다.

### Q5: Compile() 캐싱은 인스턴스별인가요, 타입별인가요?

**A**: `_compiled` 필드는 **인스턴스별**입니다. `new ProductInStockSpec()`을 호출할 때마다 새 인스턴스가 생성되고, 첫 `IsSatisfiedBy` 호출 시 `Compile()`이 실행됩니다. `Compile()` 자체는 마이크로초 수준이므로 대부분의 시나리오에서 성능 문제가 되지 않습니다. 고빈도 경로에서는 Specification 인스턴스를 재사용(static 필드, 캐싱)하면 불필요한 재컴파일을 피할 수 있습니다.

---

ExpressionSpecification은 primitive 타입으로 조건을 표현합니다. 하지만 도메인 모델이 Value Object를 사용한다면 어떨까요? 다음 장에서는 Value Object를 Expression Tree에서 안전하게 사용하기 위한 primitive 변환 패턴을 다룹니다.

→ [3장: Value Object 변환 패턴](../03-ValueObject-Primitive-Conversion/)
