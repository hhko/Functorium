---
title: "용어집"
---
이 튜토리얼에서 사용하는 주요 용어의 정의와 코드 예시입니다.

## A

### All (항등원)
Specification의 항등원. 모든 후보에 대해 `true`를 반환하며, And 조합의 시작점으로 사용됩니다. 동적 필터 체이닝에서 초기 시드 역할을 합니다.

```csharp
var spec = Specification<Product>.All;
spec &= new ActiveProductSpec();
```

### AllSpecification<T> (internal)
모든 후보를 만족하는 Specification. `ExpressionSpecification<T>`를 상속하여 `ToExpression() => _ => true`를 제공하므로, `SpecificationExpressionResolver.TryResolve()`로 Expression 추출이 가능합니다. `Specification<T>.All` 프로퍼티를 통해 접근하며, 싱글턴 패턴으로 구현된 내부(internal) 클래스입니다.

### And (논리곱)
두 Specification을 결합하여 둘 다 만족하는 후보만 통과시키는 조합 연산.

```csharp
var spec = new ActiveProductSpec().And(new ProductInStockSpec());
// 또는 연산자: new ActiveProductSpec() & new ProductInStockSpec()
```

---

## C

### Composition (조합)
여러 Specification을 And, Or, Not으로 결합하여 복합 규칙을 만드는 것. Specification 패턴의 핵심 가치.

---

## D

### Delegate Caching (델리게이트 캐싱)
ExpressionSpecification에서 Expression Tree를 한 번만 컴파일하고 결과 델리게이트를 캐싱하는 최적화 기법.

---

## E

### Expression Tree (식 트리)
코드를 데이터 구조로 표현한 것. `Expression<Func<T, bool>>` 형태로, ORM이 SQL 등으로 변환할 수 있습니다.

```csharp
Expression<Func<Product, bool>> expr = p => p.IsActive;
```

### ExpressionSpecification<T>
Expression Tree를 지원하는 Specification 추상 클래스. `ToExpression`을 구현하면 `IsSatisfiedBy`는 컴파일된 델리게이트를 자동 캐싱합니다.

### IExpressionSpec<T>
Expression Tree를 제공할 수 있음을 나타내는 인터페이스. `ExpressionSpecification<T>`가 이를 구현하며, `SpecificationExpressionResolver.TryResolve()`가 패턴 매칭으로 이 인터페이스를 확인합니다.

---

## I

### Identity Element (항등원)
조합 연산에서 상대방의 값을 변화시키지 않는 요소. `Specification<T>.All`은 And 연산의 항등원입니다.

### IsSatisfiedBy
Specification의 핵심 메서드. 후보 객체가 규칙을 만족하는지 판단하여 `bool`을 반환합니다.

```csharp
public override bool IsSatisfiedBy(Product candidate) =>
    candidate.IsActive;
```

---

## N

### Not (논리 부정)
Specification의 결과를 반전시키는 조합 연산.

```csharp
var spec = new ActiveProductSpec().Not();
// 또는 연산자: !new ActiveProductSpec()
```

---

## O

### Or (논리합)
두 Specification 중 하나라도 만족하면 통과시키는 조합 연산.

```csharp
var spec = new PremiumSpec().Or(new DiscountedSpec());
// 또는 연산자: new PremiumSpec() | new DiscountedSpec()
```

---

## P

### ParameterReplacer
Expression Tree 조합 시 서로 다른 파라미터 표현식을 하나로 통합하는 ExpressionVisitor. And, Or 조합에서 내부적으로 사용됩니다.

### PropertyMap<TEntity, TModel>
도메인 모델과 데이터베이스 엔티티 간의 속성 매핑을 정의하는 클래스. Expression Tree에서 도메인 속성을 엔티티 속성으로 변환할 때 사용합니다.

---

## R

### Repository Pattern
데이터 접근 로직을 추상화하는 패턴. Specification과 결합하면 하나의 `FindAsync(spec)` 메서드로 다양한 조건 조회가 가능합니다.

---

## S

### Specification<T>
비즈니스 규칙을 캡슐화하는 추상 클래스. `IsSatisfiedBy` 메서드를 구현하여 후보 객체의 충족 여부를 판단합니다.

### SpecificationExpressionResolver
여러 Specification의 Expression Tree를 재귀적으로 합성하는 유틸리티. And, Or, Not 조합의 Expression을 하나의 Expression Tree로 병합합니다.

---

## T

### ToExpression
ExpressionSpecification의 핵심 메서드. `Expression<Func<T, bool>>` 형태로 규칙을 반환하며, ORM이 SQL로 변환할 수 있습니다.

### TranslatingVisitor
PropertyMap을 기반으로 Expression Tree의 속성 접근을 변환하는 ExpressionVisitor. 도메인 모델 기반 Expression을 데이터베이스 엔티티 기반으로 변환합니다.

### TryResolve
SpecificationExpressionResolver의 핵심 메서드. Specification에서 Expression Tree를 추출하고, 조합된 Specification의 경우 재귀적으로 합성합니다.

---

## 다음 단계

참고 자료를 확인합니다.

→ [D. 참고 자료](D-references.md)
