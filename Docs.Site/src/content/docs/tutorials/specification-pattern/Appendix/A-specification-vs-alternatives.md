---
title: "명세 vs 대안 비교"
---
## 개요

비즈니스 규칙을 표현하는 여러 접근법을 Specification 패턴과 비교합니다. 각 접근법의 장단점을 이해하면 상황에 맞는 올바른 선택을 할 수 있습니다.

---

## 접근법 비교

| 접근법 | 장점 | 단점 | 적합한 상황 |
|--------|------|------|-------------|
| **인라인 Predicate** | 간단, 별도 클래스 불필요 | 재사용 불가, 테스트 어려움 | 일회성 필터, 단순 조건 |
| **Strategy 패턴** | 알고리즘 교체 가능 | 조합 불가, bool 반환에 과도 | 알고리즘 교체가 목적인 경우 |
| **Repository-per-Query** | 직관적, 타입 안전 | 메서드 폭발, 중복 코드 | 조건 조합이 적은 경우 |
| **동적 LINQ** | 유연한 문자열 기반 쿼리 | 컴파일 타임 검증 불가, 보안 위험 | 관리자용 임시 쿼리 |
| **Specification 패턴** | 조합 가능, 재사용, 테스트 용이 | 초기 구현 비용, 학습 곡선 | 복합 비즈니스 규칙, DDD |

---

## 상세 비교

### 1. 인라인 Predicate

```csharp
// 인라인 predicate
var activeProducts = products.Where(p => p.IsActive && !p.IsDiscontinued);
```

**장점:**
- 추가 클래스 없이 즉시 사용
- 간단한 조건에 적합
- 학습 비용 없음

**단점:**
- 동일 조건을 여러 곳에서 반복
- 규칙 변경 시 모든 사용처 수정 필요
- 조건에 이름을 부여할 수 없음
- 단위 테스트로 조건만 독립 검증 불가

---

### 2. Strategy 패턴

```csharp
public interface IProductFilter
{
    IEnumerable<Product> Filter(IEnumerable<Product> products);
}

public class ActiveProductFilter : IProductFilter
{
    public IEnumerable<Product> Filter(IEnumerable<Product> products) =>
        products.Where(p => p.IsActive);
}
```

**장점:**
- 필터 로직을 캡슐화
- 런타임에 필터 교체 가능

**단점:**
- 컬렉션 전체를 대상으로 동작 (개별 항목 판단 아님)
- 두 Strategy를 And/Or로 조합하는 표준 방법 없음
- Expression Tree 변환 불가

---

### 3. Repository-per-Query

```csharp
public interface IProductRepository
{
    Task<List<Product>> GetActiveProductsAsync();
    Task<List<Product>> GetActiveProductsByCategoryAsync(string category);
    Task<List<Product>> GetPremiumActiveProductsByCategoryAsync(string category);
    // ... 조합마다 새 메서드
}
```

**장점:**
- 직관적이고 타입 안전
- IDE 자동 완성 지원

**단점:**
- N개 조건 → 최대 2^N개 메서드
- 조건 로직이 Repository 구현체에 분산
- 새 조건 추가 시 인터페이스 변경 필요

---

### 4. 동적 LINQ

```csharp
// 문자열 기반 동적 쿼리 (System.Linq.Dynamic.Core)
var result = products.AsQueryable()
    .Where("IsActive == true && Price > @0", minPrice);
```

**장점:**
- 매우 유연한 런타임 쿼리 구성
- 사용자 정의 필터에 적합

**단점:**
- 컴파일 타임 타입 검증 불가
- 오타로 인한 런타임 에러
- SQL 인젝션과 유사한 보안 위험
- 리팩토링 시 문자열 추적 어려움

---

### 5. Specification 패턴

```csharp
var spec = new ActiveProductSpec() & new ProductCategorySpec("Electronics");
var products = await repository.FindAsync(spec);
```

**장점:**
- 비즈니스 규칙에 이름 부여 (유비쿼터스 언어)
- And, Or, Not으로 자유로운 조합
- 개별 Specification 독립 테스트 가능
- Expression Tree를 통한 ORM 통합
- Repository 메서드 폭발 방지

**단점:**
- 초기 프레임워크 구현 비용 (Functorium 사용 시 해결)
- 단순한 조건에는 과도할 수 있음
- Expression Tree 이해 필요 (Part 2에서 학습)

---

## 선택 가이드

```
조건이 한 곳에서만 사용?
├── Yes → 인라인 Predicate
└── No → 조건 조합이 필요?
    ├── No → Strategy 패턴 또는 Repository-per-Query
    └── Yes → ORM 통합이 필요?
        ├── No → Specification (메모리 기반)
        └── Yes → ExpressionSpecification (Expression Tree)
```

---

## 다음 단계

Specification 패턴의 안티패턴을 확인합니다.

→ [B. 안티패턴](B-anti-patterns.md)
