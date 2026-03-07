---
title: "테스트 전략"
---

## 개요

Specification이 정확한 비즈니스 규칙을 표현하는지 어떻게 보장할 수 있을까요? 단위 테스트 하나로 충분할까요, 아니면 조합과 Repository 통합까지 검증해야 할까요? 이 장에서는 Specification의 세 가지 테스트 수준 — 개별 Spec, 조합, Usecase 통합 — 을 체계적으로 다룹니다.

## 학습 목표

1. **Level 1: Spec 자체 테스트** - 개별 Specification의 `IsSatisfiedBy()` 경계값 테스트
2. **Level 2: 조합 테스트** - `And`, `Or`, `Not` 조합의 정확한 동작 검증
3. **Level 3: Usecase 테스트** - Mock Repository를 통한 Specification 통합 검증

## 핵심 개념

### 3-Level 테스트 피라미드

```
         /  Level 3  \       Usecase 테스트 (통합)
        / ----------- \      Mock Repository로 Spec이 올바르게 사용되는지 검증
       /   Level 2     \     조합 테스트 (And/Or/Not)
      / --------------- \    복합 조건의 정확한 동작 검증
     /     Level 1       \   Spec 자체 테스트 (경계값)
    / ------------------- \  IsSatisfiedBy()의 만족/불만족 경계 검증
```

### Level 1: Spec 자체 테스트

`Theory` + `InlineData`로 경계값을 검증합니다.

```csharp
[Theory]
[InlineData(0, false)]     // 경계: 재고 0
[InlineData(1, true)]      // 경계: 재고 1
[InlineData(100, true)]    // 일반 케이스
public void ProductInStockSpec_ShouldReturnExpected_WhenStockIs(int stock, bool expected)
{
    var product = new Product("Test", 1000, stock, "Test");
    var spec = new ProductInStockSpec();
    spec.IsSatisfiedBy(product).ShouldBe(expected);
}
```

### Level 2: 조합 테스트

실제 데이터로 `And`, `Or`, `Not` 조합을 검증합니다.

```csharp
var spec = new ProductCategorySpec("Electronics") & new ProductInStockSpec();
spec.IsSatisfiedBy(inStockElectronics).ShouldBeTrue();
spec.IsSatisfiedBy(outOfStockElectronics).ShouldBeFalse();
```

### Level 3: Usecase 테스트

Mock Repository를 사용하여 Specification이 Usecase에서 올바르게 전달되는지 검증합니다.

```csharp
public class MockProductRepository : IProductRepository
{
    public Specification<Product>? LastSpec { get; private set; }
    private readonly List<Product> _products;
    // ...
}
```

## 프로젝트 설명

### 프로젝트 구조

```
TestingStrategies/
├── Product.cs
├── IProductRepository.cs
├── Specifications/
│   ├── ProductInStockSpec.cs
│   ├── ProductPriceRangeSpec.cs
│   ├── ProductCategorySpec.cs
│   └── ProductNameUniqueSpec.cs
└── Program.cs

TestingStrategies.Tests.Unit/
├── Level1_SpecSelfTests.cs         # Spec 경계값 테스트
├── Level2_CompositionTests.cs      # And/Or/Not 조합 테스트
└── Level3_UsecaseTests.cs          # Mock Repository 통합 테스트
```

## 한눈에 보는 정리

각 테스트 레벨이 무엇을 검증하고 어떤 기법을 사용하는지 정리합니다.

| 레벨 | 대상 | 검증 내용 | 기법 |
|------|------|----------|------|
| **Level 1** | 개별 Spec | `IsSatisfiedBy()` 경계값 | `Theory` + `InlineData` |
| **Level 2** | Spec 조합 | `And`, `Or`, `Not` 동작 | 실제 데이터 + 연산자 |
| **Level 3** | Usecase | Spec이 Repository에 올바르게 전달 | Mock Repository |

## FAQ

### Q1: 3가지 레벨을 모두 작성해야 하나요?
**A**: Level 1은 필수입니다. 모든 Specification에 대해 경계값 테스트를 작성해야 합니다. Level 2는 복잡한 조합이 있을 때, Level 3는 Usecase가 Specification을 사용할 때 작성합니다.

### Q2: Level 3에서 NSubstitute 같은 Mocking 프레임워크를 사용해도 되나요?
**A**: 네, 프로젝트에서 이미 사용 중이라면 Mocking 프레임워크를 사용해도 됩니다. 이 예제에서는 외부 의존성 없이 Mock 클래스를 직접 구현하여 패턴을 명확히 보여줍니다.

### Q3: Specification 테스트에서 가장 흔한 실수는 무엇인가요?
**A**: 경계값 누락입니다. 예를 들어 `ProductPriceRangeSpec(1000, 10000)`에서 정확히 1000과 10000인 경우를 테스트하지 않으면, `>=`와 `>`의 차이로 인한 버그를 놓칠 수 있습니다.
