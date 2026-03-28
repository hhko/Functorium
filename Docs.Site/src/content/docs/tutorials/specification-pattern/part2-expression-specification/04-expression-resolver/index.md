---
title: "표현식 리졸버"
---
## 개요

지금까지 개별 `ExpressionSpecification`에서 Expression을 추출하는 방법을 배웠습니다. 하지만 `inStock & affordable` 같은 조합된 Specification에서는 어떻게 하나의 Expression을 얻을 수 있을까요? `SpecificationExpressionResolver`는 And, Or, Not으로 조합된 Specification 트리를 재귀적으로 순회하며 하나의 합성된 Expression Tree를 만들어냅니다.

> **TryResolve는 Specification에서 Expression을 추출합니다. 추출할 수 없으면 null을 반환합니다.**

## 학습 목표

### 핵심 학습 목표
1. **TryResolve가 Specification 타입별로 Expression을 추출하는 방식을** 설명할 수 있습니다
   - `IExpressionSpec<T>` → `ToExpression()` 직접 호출
   - `AndSpecification<T>` → 좌/우 Expression을 AndAlso로 합성
   - `OrSpecification<T>` → 좌/우 Expression을 OrElse로 합성
   - `NotSpecification<T>` → 내부 Expression을 Not으로 감싸기
   - 그 외 → null 반환 (graceful fallback)

2. **ParameterReplacer가 Expression 합성에 필수인 이유를** 설명할 수 있습니다
   - 서로 다른 Expression의 파라미터를 통일하는 ExpressionVisitor
   - 두 Expression을 합성할 때 동일한 파라미터를 참조하도록 교체
   - Expression Tree의 불변성을 유지하면서 트리를 변환

3. **null 반환 시 fallback 전략을** 설계할 수 있습니다
   - Non-expression Specification은 SQL로 변환할 수 없음
   - null 반환 시 어댑터가 전체 로드 + 메모리 필터링으로 fallback
   - 혼합 복합(Expression + Non-expression)도 null 반환

### 실습을 통해 확인할 내용
- 단일 ExpressionSpec에서 Expression 추출
- And/Or/Not 복합에서 합성된 Expression 추출
- Non-expression Spec과 혼합 시 null 반환 확인

## 핵심 개념

### TryResolve 패턴 매칭

```csharp
public static Expression<Func<T, bool>>? TryResolve<T>(Specification<T> spec)
{
    return spec switch
    {
        IExpressionSpec<T> e => e.ToExpression(),
        AndSpecification<T> a => CombineAnd(a),
        OrSpecification<T> o => CombineOr(o),
        NotSpecification<T> n => CombineNot(n),
        _ => null
    };
}
```

패턴 매칭으로 Specification의 타입에 따라 적절한 처리를 수행합니다. `IExpressionSpec<T>`는 가장 먼저 검사되어 직접 Expression을 추출합니다. `AndSpecification`과 `OrSpecification`은 `Left`/`Right` 프로퍼티로, `NotSpecification`은 `Inner` 프로퍼티로 내부 Specification에 접근하여 재귀적으로 Expression을 합성합니다.

### ParameterReplacer

두 Expression을 합성할 때, 각 Expression은 서로 다른 파라미터 인스턴스를 가집니다. ParameterReplacer는 ExpressionVisitor를 사용하여 모든 파라미터를 통일된 하나의 파라미터로 교체합니다.

```csharp
// left: p => p.Stock > 0       (파라미터: p)
// right: q => q.Price <= 50000  (파라미터: q)
// 합성: x => x.Stock > 0 && x.Price <= 50000  (통일된 파라미터: x)
```

### null Fallback 전략

부분 변환은 쿼리 의미를 변경할 위험이 있으므로, 전부 변환하거나 전부 메모리에서 처리하는 all-or-nothing 전략을 채택합니다. 예를 들어 And 복합에서 한쪽만 SQL로 변환하면, DB에서 충분히 필터링되지 않은 대량의 데이터가 메모리로 넘어올 수 있습니다.

```
Repository.FindAll(spec) 호출
    ↓
Adapter가 TryResolve(spec) 시도
    ↓
Expression 추출 성공 → DbContext.Set<T>().Where(expr) (SQL 변환)
Expression 추출 실패 → 전체 로드 후 IsSatisfiedBy로 메모리 필터링
```

## 프로젝트 설명

### 프로젝트 구조
```
ExpressionResolver/                          # 메인 프로젝트
├── Program.cs                               # Resolver 데모
├── Product.cs                               # 상품 레코드
├── Specifications/
│   ├── ProductInStockSpec.cs                   # Expression 기반 재고 Spec
│   ├── ProductPriceRangeSpec.cs                # Expression 기반 가격 Spec
│   ├── ProductCategorySpec.cs                  # Expression 기반 카테고리 Spec
│   └── ProductInStockPlainSpec.cs            # Non-expression 재고 Spec (fallback 데모)
├── ExpressionResolver.csproj                # 프로젝트 파일
ExpressionResolver.Tests.Unit/               # 테스트 프로젝트
├── ExpressionResolverTests.cs               # Resolver 테스트
├── Using.cs                                 # 글로벌 using
├── xunit.runner.json                        # xUnit 설정
├── ExpressionResolver.Tests.Unit.csproj     # 테스트 프로젝트 파일
index.md                                     # 이 문서
```

## 한눈에 보는 정리

### TryResolve 동작 요약
| Specification 타입 | 처리 방식 | 결과 |
|-------------------|----------|------|
| `IExpressionSpec<T>` | `ToExpression()` 직접 호출 | `Expression<Func<T, bool>>` |
| `AndSpecification<T>` | 좌/우 재귀 추출 + AndAlso 합성 | 합성된 Expression 또는 null |
| `OrSpecification<T>` | 좌/우 재귀 추출 + OrElse 합성 | 합성된 Expression 또는 null |
| `NotSpecification<T>` | 내부 재귀 추출 + Not 감싸기 | 부정 Expression 또는 null |
| 그 외 | 처리 불가 | null |

### null 반환 조건
- Non-expression Specification이 단독으로 사용된 경우
- And/Or 복합에서 좌/우 중 하나라도 non-expression인 경우
- Not 내부가 non-expression인 경우

## FAQ

### Q1: 왜 TryResolve이고 Resolve가 아닌가요?
**A**: Expression을 추출할 수 없는 Specification이 존재하기 때문입니다. Non-expression Specification이나 혼합 복합의 경우 Expression을 만들 수 없으므로, 예외를 던지는 대신 null을 반환하여 호출자가 graceful하게 fallback할 수 있도록 합니다.

### Q2: 혼합 복합(Expression + Non-expression)도 부분적으로 변환할 수 있지 않나요?
**A**: 이론적으로는 가능하지만, 부분 변환은 쿼리 의미를 변경할 위험이 있습니다. 예를 들어 And 복합에서 한쪽만 SQL로 변환하면, DB에서 필터링되지 않은 데이터가 메모리로 넘어올 수 있어 성능 문제가 발생합니다. 따라서 전부 변환하거나, 전부 메모리에서 처리하는 all-or-nothing 전략을 채택합니다.

### Q3: ParameterReplacer 없이 Expression을 합성할 수 없나요?
**A**: 없습니다. 각 Expression 람다는 고유한 ParameterExpression 인스턴스를 가집니다. 이를 통일하지 않으면 합성된 Expression이 서로 다른 파라미터를 참조하여 런타임 오류가 발생합니다.

### Q4: 실제 EF Core 어댑터에서는 어떻게 사용되나요?
**A**: Repository 어댑터에서 다음과 같이 사용됩니다:
```csharp
var expr = SpecificationExpressionResolver.TryResolve(spec);
if (expr is not null)
    return await dbContext.Set<T>().Where(expr).ToListAsync();
else
    return (await dbContext.Set<T>().ToListAsync()).Where(spec.IsSatisfiedBy).ToList();
```
이 패턴은 [Part 3: Repository 통합](../../Part3-Repository-Integration/01-Repository-With-Specification/)에서 자세히 다룹니다.

---

Part 2에서는 Expression Tree의 개념부터 Resolver까지, Specification을 ORM과 연결하기 위한 모든 기반을 다졌습니다. Part 3에서는 이 기반 위에 Repository를 설계하고, InMemory와 EF Core 어댑터를 구현합니다.

→ [Part 3의 1장: Repository와 Specification](../../Part3-Repository-Integration/01-Repository-With-Specification/)
