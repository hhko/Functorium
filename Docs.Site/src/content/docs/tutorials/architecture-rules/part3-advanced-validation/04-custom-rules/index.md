---
title: "Custom Rules"
---

## Overview

"모든 도메인 클래스에 `Create` factory method가 있어야 한다", "Service 접미사를 가진 클래스는 금지한다" — 이런 팀 고유의 규칙은 프레임워크가 기본으로 제공하지 않습니다. 하지만 직접 만들 수 있다면 어떨까요?

이 챕터에서는 `DelegateArchRule`, `CompositeArchRule`, 그리고 `Apply()` 메서드를 사용하여 **프로젝트에 특화된 커스텀 규칙을 정의하고 합성**하는 방법을 학습합니다. 내장 규칙으로 충분하지 않을 때, 무한한 확장이 가능합니다.

> **"좋은 아키텍처 테스트 프레임워크는 내장 규칙이 풍부한 것이 아니라, 내장 규칙으로 부족할 때 쉽게 확장할 수 있는 것입니다."**

## Learning Objectives

### 핵심 학습 목표

1. **`DelegateArchRule<T>`로 람다 기반 커스텀 규칙 작성**
   - 규칙 설명과 검증 함수를 받는 생성자 패턴
   - `RuleViolation`을 반환하여 위반을 보고하는 방법

2. **`CompositeArchRule<T>`로 여러 규칙을 AND 합성**
   - 개별 규칙을 조합하여 복합 규칙을 만드는 패턴
   - 모든 규칙의 위반을 수집하는 동작 방식

3. **`Apply()`로 커스텀 규칙을 기존 검증 체인에 통합**
   - 내장 규칙(`RequireSealed()`, `RequireImmutable()`)과 자유롭게 혼합
   - 하나의 검증 체인에서 내장 + 커스텀 규칙을 함께 적용

### 실습을 통해 확인할 내용
- **factory method 규칙**: 모든 도메인 클래스에 static `Create` 메서드가 있는지 검증
- **Service 접미사 금지 규칙**: 도메인 클래스 이름이 `Service`로 끝나지 않는지 검증
- **복합 규칙 합성**: 두 커스텀 규칙을 AND로 결합하여 한 번에 적용

## 도메인 코드

### Invoice / Payment - 도메인 클래스

```csharp
public sealed class Invoice
{
    public string InvoiceNo { get; }
    public decimal Amount { get; }

    private Invoice(string invoiceNo, decimal amount)
    {
        InvoiceNo = invoiceNo;
        Amount = amount;
    }

    public static Invoice Create(string invoiceNo, decimal amount)
        => new(invoiceNo, amount);
}

public sealed class Payment
{
    public string PaymentId { get; }
    public decimal Amount { get; }
    public string Method { get; }

    private Payment(string paymentId, decimal amount, string method)
    {
        PaymentId = paymentId;
        Amount = amount;
        Method = method;
    }

    public static Payment Create(string paymentId, decimal amount, string method)
        => new(paymentId, amount, method);
}
```

## 테스트 코드

### DelegateArchRule - 람다 기반 커스텀 규칙

`DelegateArchRule<T>`는 람다 함수로 규칙을 defines. 생성자는 규칙 설명과 검증 함수를 받습니다.

```csharp
private static readonly DelegateArchRule<Class> s_factoryMethodRule = new(
    "All domain classes must have a static Create factory method",
    (target, _) =>
    {
        var hasCreate = target.Members
            .OfType<MethodMember>()
            .Any(m => m.Name.StartsWith("Create(") && m.IsStatic == true);
        return hasCreate
            ? []
            : [new RuleViolation(target.FullName, "FactoryMethodRequired",
                $"Class '{target.Name}' must have a static Create method.")];
    });
```

검증 함수는 `(TType target, Architecture architecture)` 매개변수를 받아 `IReadOnlyList<RuleViolation>`을 returns.
위반이 없으면 빈 리스트를, 위반이 있으면 `RuleViolation` 목록을 returns.

### CompositeArchRule - AND 합성

`CompositeArchRule<T>`는 여러 `IArchRule<T>`를 AND로 합성합니다. 모든 규칙의 위반을 수집합니다.

```csharp
private static readonly CompositeArchRule<Class> s_domainClassRule = new(
    s_factoryMethodRule,
    s_noServiceSuffixRule);
```

### Apply - 커스텀 규칙 적용

`Apply()` 메서드로 커스텀 규칙을 검증 체인에 통합합니다.

```csharp
[Fact]
public void DomainClasses_ShouldSatisfy_CompositeRule()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .Apply(s_domainClassRule),
            verbose: true)
        .ThrowIfAnyFailures("Domain Composite Rule");
}
```

### 내장 규칙과 커스텀 규칙 혼합

`RequireSealed()`, `RequireImmutable()` 같은 내장 메서드와 `Apply()`를 자유롭게 체이닝할 수 있습니다.

```csharp
[Fact]
public void DomainClasses_ShouldSatisfy_CompositeRuleWithBuiltIn()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireSealed()
            .RequireImmutable()
            .Apply(s_domainClassRule),
            verbose: true)
        .ThrowIfAnyFailures("Domain Full Composite Rule");
}
```

## Summary at a Glance

The following table 커스텀 규칙 작성에 사용하는 핵심 타입을 요약합니다.

### 커스텀 규칙 핵심 타입 요약

| 타입 | 역할 | 사용 방법 |
|------|------|-----------|
| **`IArchRule<T>`** | 커스텀 규칙의 인터페이스 | `Description`과 `Validate()` 정의 |
| **`DelegateArchRule<T>`** | 람다 함수로 규칙 정의 | `new DelegateArchRule<Class>("설명", (target, arch) => ...)` |
| **`CompositeArchRule<T>`** | 여러 규칙을 AND 합성 | `new CompositeArchRule<Class>(rule1, rule2)` |
| **`RuleViolation`** | 위반 정보를 담는 sealed record | `(TargetName, RuleName, Description)` |
| **`Apply(rule)`** | 커스텀 규칙을 검증 체인에 통합 | `.Apply(s_domainClassRule)` |

The following table 내장 규칙과 커스텀 규칙의 역할을 compares.

### 내장 규칙 vs 커스텀 규칙

| Aspect | 내장 규칙 | 커스텀 규칙 |
|------|-----------|-------------|
| **정의 방법** | `RequireXxx()` 메서드 호출 | `DelegateArchRule` 또는 `IArchRule` 구현 |
| **적용 방법** | 직접 체이닝 | `Apply(rule)` |
| **합성** | 체이닝으로 자동 AND | `CompositeArchRule`로 명시적 AND |
| **재사용** | 프레임워크 제공 | 프로젝트 내 공유 가능 |

## FAQ

### Q1: `DelegateArchRule`과 `IArchRule` 직접 구현의 차이는 무엇인가요?
**A**: `DelegateArchRule`은 간단한 규칙을 람다로 빠르게 정의할 때 적합합니다. 규칙 로직이 복잡하거나, 상태(필드)가 필요하거나, 여러 곳에서 재사용해야 할 때는 `IArchRule<T>` 인터페이스를 직접 구현하는 클래스를 만드는 것이 더 적합합니다.

### Q2: `CompositeArchRule`은 OR 합성도 지원하나요?
**A**: 아닙니다. `CompositeArchRule`은 AND 합성만 지원합니다 — 모든 규칙의 위반을 수집하여 returns. OR 합성이 필요하면 `DelegateArchRule` 안에서 직접 OR 로직을 구현해야 합니다.

### Q3: 커스텀 규칙에서 `Architecture` 매개변수는 언제 사용하나요?
**A**: `Architecture` 매개변수는 프로젝트 전체의 타입 정보에 접근할 때 uses. 예를 들어 "이 클래스가 특정 인터페이스를 구현하는 다른 클래스에 의존하는가?"처럼 타입 간 관계를 분석할 때 필요합니다. 단순 멤버 검사에서는 `_`로 무시해도 됩니다.

### Q4: `Apply()`를 여러 번 호출할 수 있나요?
**A**: 네, `.Apply(rule1).Apply(rule2)`처럼 여러 커스텀 규칙을 순차적으로 적용할 수 있습니다. `CompositeArchRule`로 묶는 것과 동일한 효과이지만, 체이닝 스타일로 더 읽기 쉽게 표현할 수 있습니다.

---

커스텀 규칙을 작성할 수 있다는 것은, 프레임워크의 한계가 곧 프로젝트의 한계가 되지 않는다는 뜻입니다. Part 3의 고급 검증 기법을 모두 배웠으니, 다음 Part 4에서는 이 모든 기법을 실전 레이어별 아키텍처 규칙에 적용합니다.

→ [Part 4: 실전 패턴](../../Part4-Real-World-Patterns/)
