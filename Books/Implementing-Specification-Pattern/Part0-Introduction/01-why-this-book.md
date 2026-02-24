# 0.1 이 책을 읽어야 하는 이유

> **Part 0: 서론** | [목차](../README.md) | [다음: 0.2 사전 준비와 환경 설정 →](02-prerequisites-and-setup.md)

---

## 개요

이 책은 **Specification 패턴을 활용한 도메인 규칙 구현**을 단계별로 학습할 수 있도록 구성된 종합적인 교육 과정입니다. 기본적인 Specification 클래스에서 시작하여 Expression Tree 기반 Repository 통합까지, **18개의 실습 프로젝트**를 통해 Specification 패턴의 모든 측면을 체계적으로 학습할 수 있습니다.

---

## 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 🟢 **초급** | C# 기본 문법을 알고 Specification 패턴에 입문하려는 개발자 | Part 1 (1장~4장) |
| 🟡 **중급** | 패턴을 이해하고 실전 적용을 원하는 개발자 | Part 1~3 (1장~12장) |
| 🔴 **고급** | 아키텍처 설계와 도메인 모델링에 관심 있는 개발자 | Part 4~5 + 부록 |

---

## 학습 전제 조건

이 책을 효과적으로 학습하기 위해 다음 지식이 필요합니다:

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

이 책을 완료하면 다음을 할 수 있습니다:

### 1. 비즈니스 규칙을 재사용 가능한 Specification으로 캡슐화

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

### 2. And, Or, Not 조합으로 복합 규칙 표현

```csharp
// 개별 Specification 정의
var isActive = new ActiveProductSpec();
var isInStock = new InStockSpec();
var isPremium = new PremiumProductSpec();

// 조합으로 복합 규칙 표현
var availablePremium = isActive & isInStock & isPremium;
var discountTarget = isActive & (isPremium | !isInStock);
```

### 3. Expression Tree를 활용한 ORM 호환 Specification 구현

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

### 4. Repository와 Specification 통합으로 유연한 데이터 조회

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
    spec &= new CategorySpec(filter.Category);
if (filter.MinPrice is not null)
    spec &= new MinPriceSpec(filter.MinPrice.Value);
```

---

## Tutorial과의 차이점

| 구분 | Tutorial | 이 책 |
|------|----------|-------|
| **목적** | 빠른 실습과 결과 확인 | 개념 이해와 설계 원리 학습 |
| **깊이** | 핵심 사용법 중심 | 내부 구현과 원리 심화 |
| **범위** | Specification 기본 사용 | Expression Tree, Repository 통합, 테스트 전략 |
| **대상** | 바로 적용하려는 개발자 | 패턴을 깊이 이해하려는 개발자 |

---

## 학습 경로

```
┌──────────────────────────────────────────────────────────────┐
│                       학습 로드맵                              │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  🟢 초급 (Part 1: 1~4장)                                     │
│  ├── 첫 번째 Specification 구현                               │
│  ├── And, Or, Not 조합                                        │
│  ├── 연산자 오버로딩                                          │
│  └── All 항등원과 동적 체이닝                                 │
│                                                               │
│  🟡 중급 (Part 2~3: 5~12장)                                  │
│  ├── Expression Tree 기반 Specification                       │
│  ├── Value Object 변환 패턴                                   │
│  ├── Repository 통합                                          │
│  └── EF Core 구현                                             │
│                                                               │
│  🔴 고급 (Part 4~5 + 부록)                                   │
│  ├── CQRS + Specification                                     │
│  ├── 동적 필터 빌더                                           │
│  ├── 테스트 전략                                              │
│  └── 도메인별 실전 예제                                       │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

---

## 다음 단계

환경 설정을 완료하고 프로젝트를 준비하세요.

→ [0.2 사전 준비와 환경 설정](02-prerequisites-and-setup.md)
