# Chapter 5: 메서드 검증

메서드 수준의 아키텍처 규칙을 검증하는 방법을 학습합니다. **팩토리 메서드(Factory Method),** 인스턴스 메서드, **확장 메서드(Extension Method)에** 대한 가시성, 정적 여부 등을 테스트로 강제할 수 있습니다.

## 학습 목표

- `RequireMethod`로 특정 메서드의 존재와 규칙을 검증하는 방법
- `RequireMethodIfExists`로 선택적 메서드를 검증하는 방법
- `RequireAllMethods`로 모든 메서드에 공통 규칙을 적용하는 방법
- `RequireStatic()`, `RequireNotStatic()`, `RequireExtensionMethod()`의 활용

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

## 핵심 개념

| API | 설명 |
|-----|------|
| `RequireMethod(name, validation)` | 지정된 이름의 메서드가 반드시 존재해야 하며, 검증 규칙을 적용 |
| `RequireMethodIfExists(name, validation)` | 메서드가 존재하는 경우에만 검증 규칙을 적용 |
| `RequireAllMethods(validation)` | 모든 메서드에 검증 규칙을 적용 |
| `RequireAllMethods(filter, validation)` | 필터 조건에 맞는 메서드에만 검증 규칙을 적용 |
| `RequireStatic()` / `RequireNotStatic()` | 정적/비정적 메서드 여부 검증 |
| `RequireExtensionMethod()` | 확장 메서드 여부 검증 |

---

[이전: Chapter 4 - 상속과 인터페이스 검증](../../Part1-ClassValidator-Basics/04-Inheritance-And-Interface/README.md) | [다음: Chapter 6 - 반환 타입 검증](../02-Return-Type-Validation/README.md)
