---
title: "아키텍처 규칙"
---

## 개요

팀이 커지면 Specification의 네이밍이 제각각이 됩니다. 누군가는 `ActiveProductSpec`으로, 다른 사람은 `IsActiveSpecification`으로 만듭니다. 폴더 위치도 제멋대로입니다. 이 장에서는 네이밍 규칙, 폴더 배치, 그리고 ArchUnitNET을 활용한 자동 검증으로 팀 전체의 일관성을 유지하는 방법을 다룹니다.

## 학습 목표

1. **네이밍 규칙** - `{Aggregate}{Condition}Spec` 형식의 일관된 네이밍
2. **폴더 배치 규칙** - Aggregate 하위 `Specifications/` 폴더에 배치
3. **ArchUnitNET 검증** - 아키텍처 테스트로 규칙 자동 검증

## 핵심 개념

### 네이밍 규칙: `{Aggregate}{Condition}Spec`

Specification 이름은 대상 Aggregate와 조건을 조합하여 명명합니다.

| Specification | Aggregate | Condition | 설명 |
|--------------|-----------|-----------|------|
| `ProductInStockSpec` | Product | InStock | 재고 있는 상품 |
| `ProductPriceRangeSpec` | Product | PriceRange | 가격 범위 내 상품 |
| `ProductLowStockSpec` | Product | LowStock | 재고 부족 상품 |

### 폴더 배치

```
Domain/AggregateRoots/Products/
├── Product.cs
└── Specifications/
    ├── ProductInStockSpec.cs
    ├── ProductPriceRangeSpec.cs
    └── ProductLowStockSpec.cs
```

### ArchUnitNET 자동 검증

ArchUnitNET은 코드의 아키텍처 규칙을 테스트로 검증하는 라이브러리입니다.

```csharp
// Specifications 네임스페이스의 클래스는 Spec으로 끝나야 함
var rule = Classes()
    .That()
    .ResideInNamespace("Specifications", useRegularExpressions: false)
    .Should()
    .HaveNameEndingWith("Spec");

rule.Check(Architecture);
```

## 프로젝트 설명

### 프로젝트 구조

```
ArchitectureRules/
├── Domain/
│   └── AggregateRoots/
│       └── Products/
│           ├── Product.cs
│           └── Specifications/
│               ├── ProductInStockSpec.cs
│               ├── ProductPriceRangeSpec.cs
│               └── ProductLowStockSpec.cs
└── Program.cs

ArchitectureRules.Tests.Unit/
├── SpecificationNamingTests.cs     # ArchUnitNET 아키텍처 테스트
└── ProductSpecTests.cs             # Spec 자체 테스트
```

## 한눈에 보는 정리

| 규칙 | 설명 | 검증 방법 |
|------|------|----------|
| **네이밍** | `{Aggregate}{Condition}Spec` | ArchUnitNET: `HaveNameEndingWith("Spec")` |
| **배치** | `Specifications/` 네임스페이스 | ArchUnitNET: `ResideInNamespace("Specifications")` |
| **접근 제한** | `sealed class` | 코드 리뷰 |
| **단일 책임** | 하나의 Spec = 하나의 조건 | 코드 리뷰 |

### 양방향 규칙의 중요성

| 규칙 방향 | 의미 | 방지하는 문제 |
|-----------|------|-------------|
| Specifications -> Spec | 네임스페이스 내 클래스는 Spec으로 끝남 | 네임스페이스에 관련 없는 클래스 배치 |
| Spec -> Specifications | Spec으로 끝나는 클래스는 네임스페이스 안에 있음 | Spec이 잘못된 위치에 배치 |

## FAQ

### Q1: ArchUnitNET은 무엇인가요?
**A**: ArchUnitNET은 .NET 프로젝트의 아키텍처 규칙을 단위 테스트로 검증하는 라이브러리입니다. Java의 ArchUnit에서 영감을 받았으며, Fluent API로 규칙을 정의하고 `Check()`로 검증합니다.

### Q2: 아키텍처 테스트가 필요한 이유는 무엇인가요?
**A**: 코드 리뷰만으로는 네이밍 규칙이나 폴더 배치 규칙을 100% 보장하기 어렵습니다. 아키텍처 테스트를 CI/CD 파이프라인에 포함하면, 규칙 위반이 자동으로 감지되어 일관성을 유지할 수 있습니다.

### Q3: 다른 Aggregate의 Specification도 같은 규칙을 적용할 수 있나요?
**A**: 네, ArchUnitNET 규칙은 어셈블리 전체에 적용됩니다. `Customer` Aggregate에 `CustomerEmailUniqueSpec`을 추가해도 동일한 규칙이 자동으로 검증됩니다.

---

Part 4에서는 Specification 패턴의 실전 활용법을 살펴보았습니다 — CQRS 통합, 동적 필터 빌더, 테스트 전략, 아키텍처 규칙까지. Part 5에서는 이 모든 것을 종합하여 실제 도메인 시나리오에 적용합니다.

→ [17장: 이커머스 상품 필터링](../../Part5-Domain-Examples/01-Ecommerce-Product-Filtering/)
