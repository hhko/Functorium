---
title: "메서드 검증"
---

## 개요

팩토리 메서드가 반드시 `public static`이어야 한다는 규칙은 코드 리뷰에서 쉽게 놓칩니다. 새로운 팀원이 `Money.Create`를 인스턴스 메서드로 바꾸거나 `internal`로 선언해도, 컴파일은 성공하고 기존 테스트도 통과합니다. 문제는 다른 어셈블리에서 호출할 때 비로소 드러나죠. 이 장에서는 메서드 수준의 아키텍처 규칙을 테스트로 강제하여, **팩토리 메서드(Factory Method),** 인스턴스 메서드, **확장 메서드(Extension Method)의** 가시성과 정적 여부를 자동으로 검증하는 방법을 학습합니다.

> **"팩토리 메서드가 public static이어야 한다는 규칙, 코드 리뷰 대신 컴파일 타임 테스트로 강제할 수 있습니다."**

## 학습 목표

### 핵심 학습 목표
1. **`RequireMethod`로 필수 메서드 검증**
   - 지정된 이름의 메서드가 반드시 존재하는지 확인
   - 가시성(`Visibility.Public`)과 정적 여부를 동시에 검증

2. **`RequireMethodIfExists`로 선택적 메서드 검증**
   - 메서드가 존재할 경우에만 규칙을 적용하는 유연한 접근
   - 모든 클래스에 동일 메서드가 없을 때 유용

3. **`RequireAllMethods`로 공통 규칙 일괄 적용**
   - 필터 조건으로 대상 메서드를 선별
   - `RequireExtensionMethod()`로 확장 메서드 여부 검증

### 실습을 통해 확인할 내용
- **Money.Create**: `public static` 팩토리 메서드 규칙 검증
- **Money.Add**: 인스턴스 메서드의 비정적 규칙 검증
- **MoneyExtensions**: 모든 일반 메서드가 확장 메서드인지 검증

## 도메인 코드

### Money 클래스

`Money`는 팩토리 메서드 패턴을 사용하는 값 객체입니다. `Create`는 정적 팩토리 메서드이고, `Add`는 인스턴스 메서드입니다.

```csharp
public sealed class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
        => new(amount, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new InvalidOperationException("Currency mismatch");
        return new Money(Amount + other.Amount, Currency);
    }
}
```

### MoneyExtensions 확장 메서드

```csharp
public static class MoneyExtensions
{
    public static string FormatKrw(this Money money)
        => $"₩{money.Amount:N0}";

    public static Money ApplyDiscount(this Money money, Discount discount)
        => Money.Create(money.Amount * (1 - discount.Percentage / 100), money.Currency);
}
```

## 테스트 코드

### 팩토리 메서드 검증

`RequireMethod`로 `Create` 메서드가 `public static`인지 검증합니다.

```csharp
[Fact]
public void FactoryMethods_ShouldBe_PublicAndStatic()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("MethodValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireVisibility(Visibility.Public)
                .RequireStatic()),
            verbose: true)
        .ThrowIfAnyFailures("Factory Method Rule");
}
```

### 인스턴스 메서드 검증

`RequireMethodIfExists`로 `Add` 메서드가 존재할 경우 정적이 아닌지 검증합니다.

```csharp
[Fact]
public void InstanceMethods_ShouldNotBe_Static()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("MethodValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethodIfExists("Add", m => m
                .RequireNotStatic()),
            verbose: true)
        .ThrowIfAnyFailures("Instance Method Rule");
}
```

### 확장 메서드 검증

`RequireAllMethods`에 필터를 적용하여 일반 메서드만 선택한 후 `RequireExtensionMethod()`로 검증합니다.

```csharp
[Fact]
public void ExtensionMethods_ShouldBe_ExtensionMethods()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("MethodValidation.Extensions")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireAllMethods(
                m => !m.Name.StartsWith(".") && m.MethodForm == MethodForm.Normal,
                m => m
                    .RequireStatic()
                    .RequireExtensionMethod()),
            verbose: true)
        .ThrowIfAnyFailures("Extension Method Rule");
}
```

## 한눈에 보는 정리

다음 표는 메서드 검증 API와 각각의 용도를 요약합니다.

### 메서드 검증 API 요약
| API | 설명 |
|-----|------|
| `RequireMethod(name, validation)` | 지정된 이름의 메서드가 반드시 존재해야 하며, 검증 규칙을 적용 |
| `RequireMethodIfExists(name, validation)` | 메서드가 존재하는 경우에만 검증 규칙을 적용 |
| `RequireAllMethods(validation)` | 모든 메서드에 검증 규칙을 적용 |
| `RequireAllMethods(filter, validation)` | 필터 조건에 맞는 메서드에만 검증 규칙을 적용 |
| `RequireStatic()` / `RequireNotStatic()` | 정적/비정적 메서드 여부 검증 |
| `RequireExtensionMethod()` | 확장 메서드 여부 검증 |

### RequireMethod vs RequireMethodIfExists
| 구분 | `RequireMethod` | `RequireMethodIfExists` |
|------|-----------------|-------------------------|
| **메서드 부재 시** | 실패 (메서드 필수) | 통과 (검증 건너뜀) |
| **메서드 존재 시** | 규칙 검증 | 규칙 검증 |
| **사용 시나리오** | 팩토리 메서드처럼 반드시 있어야 하는 메서드 | 일부 클래스에만 존재하는 선택적 메서드 |

## FAQ

### Q1: RequireMethod와 RequireMethodIfExists는 언제 구분해서 사용하나요?
**A**: `RequireMethod`는 메서드가 존재하지 않으면 테스트가 실패합니다. 팩토리 메서드 `Create`처럼 모든 대상 클래스에 반드시 있어야 하는 메서드에 사용합니다. `RequireMethodIfExists`는 메서드가 없으면 그냥 통과하므로, `Add`처럼 일부 클래스에만 있을 수 있는 메서드의 규칙을 검증할 때 적합합니다.

### Q2: RequireAllMethods의 필터는 왜 필요한가요?
**A**: 클래스에는 생성자(`.ctor`), 컴파일러 생성 메서드 등 검증 대상이 아닌 메서드가 포함됩니다. `m => !m.Name.StartsWith(".") && m.MethodForm == MethodForm.Normal` 같은 필터로 이런 메서드를 제외하고, 개발자가 직접 작성한 일반 메서드만 검증 대상으로 선별합니다.

### Q3: RequireVisibility와 RequireStatic을 동시에 적용할 수 있나요?
**A**: 네, 메서드 검증은 체이닝 방식으로 동작합니다. `m.RequireVisibility(Visibility.Public).RequireStatic()`처럼 여러 조건을 연결하면 모든 조건이 AND로 결합됩니다. 하나라도 충족하지 않으면 검증이 실패합니다.

### Q4: 확장 메서드 검증에서 RequireStatic()과 RequireExtensionMethod()를 모두 사용하는 이유는?
**A**: C#에서 확장 메서드는 반드시 `static`이어야 하므로 `RequireExtensionMethod()`만으로도 정적 여부가 간접적으로 보장됩니다. 하지만 명시적으로 `RequireStatic()`을 함께 사용하면 테스트의 의도를 더 분명히 전달하고, 검증 실패 시 어떤 조건이 위반되었는지 구체적인 오류 메시지를 얻을 수 있습니다.

### Q5: 메서드 이름 대신 시그니처로 검증할 수 있나요?
**A**: `RequireMethod`는 이름으로 메서드를 찾습니다. 오버로드가 있는 경우 동일 이름의 모든 메서드가 검증 대상이 됩니다. 시그니처 수준의 세밀한 검증이 필요하면 다음 장에서 배울 반환 타입 검증이나 파라미터 검증과 조합하여 사용할 수 있습니다.

---

메서드의 존재와 정적 여부를 검증할 수 있게 되었습니다. 다음 장에서는 메서드의 **반환 타입을** 검증하여, 팩토리 메서드가 `Fin<T>` 같은 함수형 결과 타입을 반드시 사용하도록 강제하는 방법을 학습합니다.

[이전: Chapter 4 - 상속과 인터페이스 검증](../../Part1-ClassValidator-Basics/04-Inheritance-And-Interface/) | [다음: Chapter 6 - 반환 타입 검증](../02-Return-Type-Validation/)
