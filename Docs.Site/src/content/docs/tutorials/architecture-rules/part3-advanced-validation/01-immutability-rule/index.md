---
title: "Immutability Rules"
---

## Overview

코드 리뷰에서 "이 Value Object에 왜 public setter가 있죠?"라는 코멘트를 남겨본 적이 있나요? 도메인 객체에 `public set`이 하나라도 있으면 immutability이 깨지고, 동시성 버그나 예측 불가능한 상태 변경이 발생합니다. 하지만 매번 눈으로 확인하기엔 클래스가 너무 많습니다.

이 챕터에서는 `RequireImmutable()` 메서드를 사용하여 클래스의 immutability을 **6가지 차원**에서 종합적으로 검증하는 방법을 학습합니다. 한 줄의 테스트 코드로, 도메인 전체의 immutability을 자동으로 보장할 수 있습니다.

> **"setter를 막는 것은 시작일 뿐입니다. 생성자, 필드, 컬렉션 타입, 상태 변경 메서드까지 — 진정한 immutability은 6가지 차원 모두를 통과해야 합니다."**

## Learning Objectives

### 핵심 학습 목표

1. **`RequireImmutable()`의 6가지 검증 차원 이해**
   - 기본 Writability, 생성자, 프로퍼티, 필드, 가변 컬렉션, 상태 변경 메서드
   - 각 차원이 왜 필요한지, 어떤 위반을 잡아내는지

2. **올바른 불변 클래스 설계 패턴 학습**
   - private 생성자 + factory method 패턴
   - getter-only 속성과 변환 메서드 패턴

3. **읽기 전용 컬렉션을 활용한 불변 클래스 구현**
   - `IReadOnlyList<T>` vs `List<T>`의 차이
   - 가변 컬렉션이 immutability 검증에 위반되는 이유

### 실습을 통해 확인할 내용
- **Temperature**: private 생성자, getter-only 속성, factory method를 갖춘 기본 불변 클래스
- **Palette**: `IReadOnlyList<string>`을 사용한 컬렉션 포함 불변 클래스
- **전체 도메인 검증**: 네임스페이스 기반으로 모든 도메인 클래스를 한 번에 검증

## 도메인 코드

### Temperature - 기본 불변 클래스

private 생성자, getter-only 속성, factory method 패턴을 사용한 불변 클래스입니다.

```csharp
public sealed class Temperature
{
    public double Value { get; }
    public string Unit { get; }

    private Temperature(double value, string unit)
    {
        Value = value;
        Unit = unit;
    }

    public static Temperature Create(double value, string unit)
        => new(value, unit);

    public Temperature ToCelsius()
        => Unit == "F" ? Create((Value - 32) * 5 / 9, "C") : this;

    public override string ToString() => $"{Value}°{Unit}";
}
```

`ToCelsius()` 메서드는 기존 객체를 변경하지 않고 새로운 `Temperature` 인스턴스를 returns.
이것이 불변 객체의 핵심 패턴입니다 — 상태를 바꾸는 대신, 새로운 상태를 가진 객체를 만듭니다.

### Palette - 읽기 전용 컬렉션을 사용한 불변 클래스

```csharp
public sealed class Palette
{
    public string Name { get; }
    public IReadOnlyList<string> Colors { get; }

    private Palette(string name, IReadOnlyList<string> colors)
    {
        Name = name;
        Colors = colors;
    }

    public static Palette Create(string name, params string[] colors)
        => new(name, colors.ToList().AsReadOnly());
}
```

`IReadOnlyList<string>`을 사용하여 컬렉션의 immutability을 guarantees.
`List<string>`을 직접 노출하면 `ImmutabilityRule`의 가변 컬렉션 검증에 위반됩니다.

## 테스트 코드

### RequireImmutable()의 6가지 검증 차원

`RequireImmutable()`은 내부적으로 `ImmutabilityRule`을 적용하며, 다음 6가지 차원에서 클래스의 immutability을 검증합니다:

1. **기본 Writability 검증** - 멤버가 immutable인지 확인
2. **생성자 검증** - 모든 생성자가 private인지 확인
3. **프로퍼티 검증** - public setter가 없는지 확인
4. **필드 검증** - public 필드가 없는지 확인
5. **가변 컬렉션 타입 검증** - `List<>`, `Dictionary<>` 등 가변 컬렉션 사용 금지
6. **상태 변경 메서드 검증** - 허용된 메서드(팩토리, getter, `ToString` 등) 외 금지

### 전체 도메인 클래스 immutability 검증

```csharp
[Fact]
public void DomainClasses_ShouldBe_Immutable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .ValidateAllClasses(Architecture, @class => @class
            .RequireImmutable(),
            verbose: true)
        .ThrowIfAnyFailures("Domain Immutability Rule");
}
```

### 개별 클래스 검증 (Sealed + Immutable)

```csharp
[Fact]
public void Temperature_ShouldBe_SealedAndImmutable()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(DomainNamespace)
        .And()
        .HaveName("Temperature")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireSealed()
            .RequireImmutable(),
            verbose: true)
        .ThrowIfAnyFailures("Temperature Sealed Immutability Rule");
}
```

`RequireSealed()`과 `RequireImmutable()`을 체이닝하여 sealed이면서 불변인 클래스를 검증합니다.

## Summary at a Glance

The following table `RequireImmutable()`이 검증하는 6가지 차원을 요약합니다.

### RequireImmutable() 검증 차원 요약

| 검증 차원 | 검증 내용 | 위반 예시 |
|-----------|-----------|-----------|
| **기본 Writability** | 멤버가 immutable인지 확인 | 쓰기 가능한 멤버 존재 |
| **생성자** | 모든 생성자가 private인지 확인 | `public Temperature(...)` |
| **프로퍼티** | public setter가 없는지 확인 | `public double Value { get; set; }` |
| **필드** | public 필드가 없는지 확인 | `public double value;` |
| **가변 컬렉션** | `List<>`, `Dictionary<>` 등 사용 금지 | `public List<string> Colors { get; }` |
| **상태 변경 메서드** | 허용된 메서드 외 금지 | 내부 상태를 변경하는 void 메서드 |

The following table 올바른 불변 클래스 설계 패턴을 정리합니다.

### 불변 클래스 설계 패턴

| 패턴 | Description | Example |
|------|------|------|
| **private 생성자** | 외부에서 직접 인스턴스 생성 방지 | `private Temperature(...)` |
| **getter-only 속성** | 속성 값 변경 방지 | `public double Value { get; }` |
| **factory method** | `Create` 정적 메서드로 인스턴스 생성 | `Temperature.Create(36.5, "C")` |
| **`IReadOnlyList<T>`** | 가변 컬렉션 대신 읽기 전용 사용 | `IReadOnlyList<string> Colors` |
| **변환 메서드** | 기존 객체 변경 없이 새 인스턴스 반환 | `ToCelsius()` -> 새 Temperature |

## FAQ

### Q1: `RequireImmutable()`과 `RequireNoPublicSetters()`는 어떻게 다른가요?
**A**: `RequireNoPublicSetters()`는 프로퍼티의 public setter만 검사합니다. `RequireImmutable()`은 그보다 훨씬 포괄적으로, 생성자 접근성, 필드, 가변 컬렉션 타입, 상태 변경 메서드까지 6가지 차원을 모두 검증합니다. 단순히 setter를 막는 것이 아니라 "진정한 immutability"을 guarantees.

### Q2: record 타입도 `RequireImmutable()` 검증을 통과하나요?
**A**: `record` 타입은 기본적으로 `init` 전용 프로퍼티를 생성하므로 프로퍼티 차원에서는 통과합니다. 하지만 public 생성자를 가지므로 생성자 검증에서 위반될 수 있습니다. record를 사용할 때는 `RequireRecord()`와 `RequireSealed()`을 조합하는 것이 더 적합합니다.

### Q3: `List<T>`를 private 필드로만 사용하고 외부에 노출하지 않아도 위반인가요?
**A**: `RequireImmutable()`은 타입 수준에서 가변 컬렉션의 존재 자체를 검사합니다. private 필드라도 `List<T>` 타입이면 위반으로 보고됩니다. 내부 저장소로도 `IReadOnlyList<T>`나 불변 컬렉션을 사용하는 것이 권장됩니다.

### Q4: `ToCelsius()` 같은 변환 메서드는 왜 허용되나요?
**A**: `RequireImmutable()`의 상태 변경 메서드 검증은 허용 목록(factory method, getter, `ToString`, `Equals`, `GetHashCode` 등) 기반으로 동작합니다. 반환 타입이 자기 자신(`Temperature`)인 메서드는 새 인스턴스를 반환하는 변환 메서드로 간주되어 허용됩니다.

---

immutability은 도메인 객체의 가장 기본적인 안전장치입니다. Next chapter에서는 한 단계 더 나아가, Command/Query 패턴에서 중첩 클래스의 존재와 구조를 검증하는 방법을 examines.

→ [2장: 중첩 클래스 검증](../02-Nested-Class-Validation/)
