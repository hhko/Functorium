---
title: "Chapter 12: 커스텀 규칙 (Custom Rules)"
---

## 소개

기본 제공되는 검증 메서드로 충분하지 않을 때, **커스텀 규칙(Custom Rule)을** 작성하여 프로젝트 고유의 아키텍처 규칙을 적용할 수 있습니다.
이 챕터에서는 `DelegateArchRule`, `CompositeArchRule`, 그리고 `Apply()` 메서드를 사용하여 커스텀 규칙을 정의하고 합성하는 방법을 학습합니다.

## 학습 목표

- `DelegateArchRule<T>`로 람다 기반 커스텀 규칙 작성
- `CompositeArchRule<T>`로 여러 규칙을 AND 합성
- `Apply()`로 커스텀 규칙을 기존 검증 체인에 통합

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

`DelegateArchRule<T>`는 람다 함수로 규칙을 정의합니다. 생성자는 규칙 설명과 검증 함수를 받습니다.

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

검증 함수는 `(TType target, Architecture architecture)` 매개변수를 받아 `IReadOnlyList<RuleViolation>`을 반환합니다.
위반이 없으면 빈 리스트를, 위반이 있으면 `RuleViolation` 목록을 반환합니다.

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

## 핵심 정리

| 개념 | 설명 |
|------|------|
| `IArchRule<T>` | 커스텀 규칙의 인터페이스 (`Description`, `Validate()`) |
| `DelegateArchRule<T>` | 람다 함수로 규칙 정의 |
| `CompositeArchRule<T>` | 여러 규칙을 AND 합성 |
| `RuleViolation` | 위반 정보를 담는 sealed record (`TargetName`, `RuleName`, `Description`) |
| `Apply(rule)` | 커스텀 규칙을 검증 체인에 통합 |

---

[이전: Chapter 11 - Interface Validation](../03-Interface-Validation/) | [다음: Part 4 - Real-World Patterns](../../Part4-Real-World-Patterns/)
