---
title: "Why the Specification Pattern"
---
## 개요

Repository에 메서드를 하나 더 추가해야 할 때마다 한숨이 나온 적이 있나요? `GetActiveProducts`, `GetActiveProductsByCategory`, `GetActiveProductsByCategoryAndPrice`... 조건 조합이 늘어날수록 Repository 인터페이스는 비대해지고, 같은 조건이 여러 메서드에 중복됩니다.

이 튜토리얼은 이 문제를 **Specification 패턴으로 해결하는 전 과정을** 다룹니다. 기본적인 Specification 클래스에서 시작하여 Expression Tree 기반 Repository 통합까지, **18개의 실습 프로젝트**를 통해 Specification 패턴의 모든 측면을 체계적으로 학습할 수 있습니다.

---

## 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | C# 기본 문법을 알고 Specification 패턴에 입문하려는 개발자 | Part 1 |
| **중급** | 패턴을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 |
| **고급** | 아키텍처 설계와 도메인 모델링에 관심 있는 개발자 | Part 4~5 + 부록 |

---

## 학습 전제 조건

이 튜토리얼을 효과적으로 학습하기 위해 다음 지식이 필요합니다:

### 필수
- C# 기본 문법 이해 (클래스, 인터페이스, 제네릭)
- 객체지향 프로그래밍 기초 개념
- .NET 프로젝트 실행 경험

### 권장 (있으면 좋음)
- LINQ 기본 문법
- 단위 테스트 경험
- 도메인 주도 설계(DDD) 기초 개념
- Expression Tree 기본 이해

---

## 기대 효과

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

### 1. 비즈니스 규칙을 재사용 가능한 Specification으로 캡슐화할 수 있습니다

```csharp
// ❌ 조건이 서비스 로직에 흩어져 있는 방식
public List<Product> GetActiveProducts(List<Product> products)
{
    return products.Where(p => p.IsActive && !p.IsDiscontinued).ToList();
}

// ✅ Specification으로 도메인 규칙 캡슐화
var spec = new ActiveProductSpec();
var activeProducts = products.Where(spec.IsSatisfiedBy).ToList();
```

### 2. And, Or, Not 조합으로 복합 규칙을 표현할 수 있습니다

```csharp
// 개별 Specification 정의
var isActive = new ActiveProductSpec();
var isInStock = new ProductInStockSpec();
var isPremium = new PremiumProductSpec();

// 조합으로 복합 규칙 표현
var availablePremium = isActive & isInStock & isPremium;
var discountTarget = isActive & (isPremium | !isInStock);
```

### 3. Expression Tree를 활용하여 ORM 호환 Specification을 구현할 수 있습니다

```csharp
// Expression 기반 Specification → EF Core에서 SQL로 변환
public sealed class ActiveProductSpec : ExpressionSpecification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        product => product.IsActive && !product.IsDiscontinued;
}

// Repository에서 직접 사용
var products = await repository.FindAsync(new ActiveProductSpec());
```

### 4. Repository와 Specification을 통합하여 유연하게 데이터를 조회할 수 있습니다

```csharp
// Specification을 받는 Repository 메서드
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> FindAsync(Specification<Product> spec);
    Task<int> CountAsync(Specification<Product> spec);
}

// 동적 필터 체이닝
var spec = Specification<Product>.All;
if (filter.Category is not null)
    spec &= new ProductCategorySpec(filter.Category);
if (filter.MinPrice is not null)
    spec &= new MinPriceSpec(filter.MinPrice.Value);
```

---

## Tutorial과의 차이점

다음 표는 빠른 실습 중심의 Tutorial과 이 심화 튜토리얼의 차이를 비교합니다.

| 구분 | Tutorial | 이 튜토리얼 |
|------|----------|-------|
| **목적** | 빠른 실습과 결과 확인 | 개념 이해와 설계 원리 학습 |
| **깊이** | 핵심 사용법 중심 | 내부 구현과 원리 심화 |
| **범위** | Specification 기본 사용 | Expression Tree, Repository 통합, 테스트 전략 |
| **대상** | 바로 적용하려는 개발자 | 패턴을 깊이 이해하려는 개발자 |

---

## 학습 경로

```
초급 (Part 1: 1~4장)
├── 첫 번째 Specification 구현
├── And, Or, Not 조합
├── 연산자 오버로딩
└── All 항등원과 동적 체이닝

중급 (Part 2~3: 각 1~4장)
├── Expression Tree 기반 Specification
├── Value Object 변환 패턴
├── Repository 통합
└── EF Core 구현

고급 (Part 4~5 + 부록)
├── CQRS + Specification
├── 동적 필터 빌더
├── 테스트 전략
└── 도메인별 실전 예제
```

---

## FAQ

### Q1: Specification 패턴은 모든 프로젝트에 필요한가요?
**A**: 아닙니다. 조회 조건이 1~2개로 고정된 단순 CRUD 애플리케이션에서는 오히려 과도한 추상화가 될 수 있습니다. 조건 조합이 다양하고, 동적 필터링이 필요하며, 검색 로직의 재사용이 중요한 도메인에서 가치를 발휘합니다.

### Q2: Specification과 LINQ Where의 차이는 무엇인가요?
**A**: LINQ `Where`에 직접 람다를 전달하면 조건이 호출 지점에 흩어져 재사용이 어렵습니다. Specification은 조건을 독립적인 클래스로 캡슐화하여 이름으로 의도를 전달하고, `And`, `Or`, `Not`으로 조합할 수 있으며, 개별 단위 테스트가 가능합니다.

### Q3: `ExpressionSpecification<T>`은 왜 별도로 존재하나요?
**A**: `Specification<T>`의 `IsSatisfiedBy`는 C# 메모리 내에서만 평가할 수 있습니다. `ExpressionSpecification<T>`은 `Expression<Func<T, bool>>`을 제공하여 EF Core 같은 ORM이 SQL로 변환할 수 있게 합니다. 이를 통해 데이터베이스 수준에서 필터링이 가능합니다.

### Q4: 이 튜토리얼은 어떤 순서로 학습하면 좋을까요?
**A**: 초급자는 Part 1(Specification 기초)부터 순서대로 진행하세요. `IsSatisfiedBy`, `And`/`Or`/`Not` 조합, 연산자 오버로딩을 익힌 뒤 Part 2(Expression Specification)로 넘어가면 자연스럽습니다. Repository 통합 경험이 있다면 Part 3부터 시작해도 됩니다.

---

## 다음 단계

이 튜토리얼의 구성과 목표를 확인했으니, 먼저 실습 환경을 준비하겠습니다.

→ [0.2 사전 준비와 환경 설정](02-prerequisites-and-setup.md)
