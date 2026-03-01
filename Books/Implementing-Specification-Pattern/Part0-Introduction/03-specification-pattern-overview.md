# 0.3 Specification 패턴 개요

> **Part 0: 서론** | [← 이전: 0.2 사전 준비와 환경 설정](02-prerequisites-and-setup.md) | [목차](../README.md) | [다음: 1장 첫 번째 Specification →](../Part1-Specification-Basics/01-First-Specification/)

---

## 개요

Specification 패턴은 **비즈니스 규칙을 독립적인 객체로 캡슐화**하여 재사용, 조합, 테스트를 용이하게 하는 도메인 주도 설계(DDD) 패턴입니다. Eric Evans와 Martin Fowler가 정의한 이 패턴은 복잡한 조건 로직을 명확하게 표현하는 강력한 도구입니다.

---

## Repository 메서드 폭발 문제

### 문제: 조건마다 메서드가 늘어남

비즈니스 요구사항이 추가될 때마다 Repository에 새로운 메서드가 필요합니다:

```csharp
// ❌ 메서드 폭발 - 조건 조합마다 새로운 메서드
public interface IProductRepository
{
    Task<List<Product>> GetActiveProductsAsync();
    Task<List<Product>> GetActiveProductsByCategoryAsync(string category);
    Task<List<Product>> GetActiveProductsByPriceRangeAsync(decimal min, decimal max);
    Task<List<Product>> GetActiveProductsByCategoryAndPriceAsync(string category, decimal min, decimal max);
    Task<List<Product>> GetPremiumProductsAsync();
    Task<List<Product>> GetPremiumActiveProductsAsync();
    Task<List<Product>> GetDiscountedProductsAsync();
    // ... 조합이 늘어날수록 메서드도 늘어남
}
```

**문제점:**

| 문제 | 설명 |
|------|------|
| **메서드 폭발** | N개 조건의 조합 = 최대 2^N개 메서드 |
| **중복 코드** | 조건 로직이 여러 메서드에 반복 |
| **변경 취약** | 규칙 변경 시 여러 메서드 수정 필요 |
| **테스트 부담** | 모든 조합에 대한 테스트 필요 |

---

### 해결: Specification 패턴

Specification 패턴은 **조건을 객체로 분리**하여 이 문제를 해결합니다:

```csharp
// ✅ Specification 패턴 - 하나의 메서드로 모든 조건 처리
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> FindAsync(Specification<Product> spec);
    Task<int> CountAsync(Specification<Product> spec);
}

// 조건을 자유롭게 조합
var spec = new ActiveProductSpec() & new ProductCategorySpec("Electronics");
var products = await repository.FindAsync(spec);
```

---

## Specification이 해결하는 것

| 문제 | Specification 해결 방식 |
|------|------------------------|
| 메서드 폭발 | 하나의 `FindAsync(spec)` 메서드로 통합 |
| 중복 코드 | 각 규칙이 독립적인 Specification 클래스 |
| 변경 취약 | 규칙 변경 시 해당 Specification만 수정 |
| 테스트 부담 | 개별 Specification을 독립적으로 테스트 |
| 동적 조건 | And, Or, Not으로 런타임 조합 |

---

## DDD에서의 위치

Specification 패턴은 DDD의 **도메인 계층**에 위치합니다:

```
┌──────────────────────────────────────────┐
│            Presentation Layer            │
├──────────────────────────────────────────┤
│            Application Layer             │
│  ├── UseCase / Handler                   │
│  └── Specification 조합 (동적 필터)      │
├──────────────────────────────────────────┤
│              Domain Layer                │
│  ├── Entity / Aggregate                  │
│  ├── Value Object                        │
│  ├── Specification  ← 여기              │
│  └── Domain Service                      │
├──────────────────────────────────────────┤
│          Infrastructure Layer            │
│  ├── Repository 구현체                   │
│  └── Specification→Expression 변환       │
└──────────────────────────────────────────┘
```

**핵심 원칙:**
- Specification은 **도메인 계층**에 정의
- Repository **인터페이스**(Port)는 도메인 계층에 정의
- Repository **구현체**(Adapter)는 인프라 계층에 정의
- Specification의 Expression 변환은 인프라 계층에서 처리

---

## Functorium 타입 계층

이 책에서 사용하는 Functorium의 Specification 타입 계층입니다:

```
Specification<T> (추상 클래스)
├── IsSatisfiedBy(T) : bool
├── And() / Or() / Not()
├── & / | / ! 연산자
└── All (항등원)

ExpressionSpecification<T> (Expression Tree 지원)
├── ToExpression() : Expression<Func<T, bool>>
└── sealed IsSatisfiedBy (컴파일 + 캐싱)

SpecificationExpressionResolver (Expression 합성)
PropertyMap<TEntity, TModel> (Entity→Model 변환)
```

### Specification<T>

가장 기본적인 추상 클래스입니다. `IsSatisfiedBy` 메서드를 구현하여 비즈니스 규칙을 정의합니다.

```csharp
public sealed class ActiveProductSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product candidate) =>
        candidate.IsActive && !candidate.IsDiscontinued;
}
```

### ExpressionSpecification<T>

Expression Tree를 지원하는 Specification입니다. `ToExpression`을 구현하면 `IsSatisfiedBy`는 자동으로 컴파일된 델리게이트를 캐싱하여 제공합니다.

```csharp
public sealed class ActiveProductSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.IsActive && !product.IsDiscontinued;
}
```

### SpecificationExpressionResolver

여러 Specification의 Expression을 재귀적으로 합성하는 유틸리티입니다. And, Or, Not 조합의 Expression Tree를 하나로 병합합니다.

### PropertyMap<TEntity, TModel>

도메인 모델(Value Object 포함)과 데이터베이스 엔티티 간의 속성 매핑을 정의합니다. EF Core와 같은 ORM에서 Specification의 Expression을 데이터베이스 쿼리로 변환할 때 사용합니다.

---

## 이 책의 학습 흐름

```
Part 1: Specification 기초
├── Specification<T> 상속과 IsSatisfiedBy 구현
├── And, Or, Not 메서드 조합
├── &, |, ! 연산자 오버로딩
└── All 항등원과 동적 필터 체이닝

Part 2: Expression Specification
├── Expression Tree 개념과 필요성
├── ExpressionSpecification<T> 구현
├── Value Object→primitive 변환 패턴
└── SpecificationExpressionResolver

Part 3: Repository 통합
├── Repository 메서드 폭발 방지
├── InMemory 어댑터 구현
├── PropertyMap과 TranslatingVisitor
└── EF Core 구현

Part 4: 실전 패턴
├── CQRS에서 Specification 활용
├── 동적 필터 빌더 패턴
├── 테스트 전략
└── 아키텍처 규칙

Part 5: 도메인별 실전 예제
├── 이커머스 상품 필터링
└── 고객 관리
```

---

## 다음 단계

Specification 패턴의 개요를 이해했다면, Part 1의 첫 번째 장으로 이동하세요.

→ [1장: 첫 번째 Specification](../Part1-Specification-Basics/01-First-Specification/)
