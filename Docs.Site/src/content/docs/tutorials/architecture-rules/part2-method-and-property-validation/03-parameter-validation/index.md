---
title: "파라미터 검증"
---

## Overview

`Address.Create(city, street, zipCode)` — 3개의 파라미터를 받는 factory method입니다. 누군가 리팩토링하면서 `zipCode` 파라미터를 빼면 어떻게 될까요? 컴파일은 호출부를 고치면 성공하고, 기존 테스트도 수정하면 통과합니다. 하지만 주소의 필수 정보가 빠진 채로 객체가 생성되는, 설계 의도와 다른 코드가 되어버립니다. In this chapter, 메서드의 파라미터 개수와 타입을 아키텍처 테스트로 검증하여, factory method의 시그니처를 일관되게 유지하는 방법을 학습합니다.

> **"파라미터 시그니처는 API 계약입니다. 계약 변경을 테스트로 감지하면, 의도하지 않은 시그니처 변경이 코드 리뷰를 통과하는 것을 막을 수 있습니다."**

## Learning Objectives

### 핵심 학습 목표
1. **정확한 파라미터 개수 검증**
   - `RequireParameterCount(n)`으로 factory method의 시그니처 고정
   - 파라미터가 추가되거나 제거되면 즉시 테스트 실패

2. **최소 파라미터 개수 검증**
   - `RequireParameterCountAtLeast(n)`으로 하한선 보장
   - 여러 클래스에 공통 적용할 때 유용

3. **파라미터 타입 검증**
   - `RequireFirstParameterTypeContaining`으로 첫 번째 파라미터의 타입 확인
   - `RequireAnyParameterTypeContaining`으로 특정 타입의 파라미터 존재 여부 확인

### 실습을 통해 확인할 내용
- **Address.Create**: 정확히 3개의 `string` 파라미터 강제
- **Coordinate.Create**: `double` 타입 파라미터 존재 검증
- 모든 factory method에 최소 1개 이상 파라미터 보장

## 도메인 코드

### Address 클래스

3개의 문자열 파라미터를 받는 factory method를 가집니다.

```csharp
public sealed class Address
{
    public string City { get; }
    public string Street { get; }
    public string ZipCode { get; }

    private Address(string city, string street, string zipCode)
    {
        City = city;
        Street = street;
        ZipCode = zipCode;
    }

    public static Address Create(string city, string street, string zipCode)
        => new(city, street, zipCode);
}
```

### Coordinate 클래스

2개의 `double` 파라미터를 받는 factory method를 가집니다.

```csharp
public sealed class Coordinate
{
    public double Latitude { get; }
    public double Longitude { get; }

    private Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Coordinate Create(double latitude, double longitude)
        => new(latitude, longitude);
}
```

## 테스트 코드

### 정확한 파라미터 개수 검증

```csharp
[Fact]
public void AddressCreate_ShouldHave_ThreeParameters()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Address")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireParameterCount(3)),
            verbose: true)
        .ThrowIfAnyFailures("Address Parameter Count Rule");
}
```

### 최소 파라미터 개수 검증

```csharp
[Fact]
public void FactoryMethods_ShouldHave_AtLeastOneParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireParameterCountAtLeast(1)),
            verbose: true)
        .ThrowIfAnyFailures("Factory Method Minimum Parameter Rule");
}
```

### 첫 번째 파라미터 타입 검증

```csharp
[Fact]
public void AddressCreate_ShouldHave_StringFirstParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Address")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireFirstParameterTypeContaining("String")),
            verbose: true)
        .ThrowIfAnyFailures("Address First Parameter Type Rule");
}
```

### 특정 타입 파라미터 존재 검증

```csharp
[Fact]
public void CoordinateCreate_ShouldHave_DoubleParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Coordinate")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireAnyParameterTypeContaining("Double")),
            verbose: true)
        .ThrowIfAnyFailures("Coordinate Double Parameter Rule");
}
```

## Summary at a Glance

The following table 파라미터 검증 API와 각각의 검증 방식을 요약합니다.

### 파라미터 검증 API 요약
| API | 검증 대상 | 사용 시나리오 |
|-----|-----------|---------------|
| `RequireParameterCount(n)` | 정확히 n개 | 시그니처가 고정된 factory method |
| `RequireParameterCountAtLeast(n)` | 최소 n개 이상 | 여러 클래스에 공통 하한선 적용 |
| `RequireFirstParameterTypeContaining(fragment)` | 첫 번째 파라미터의 타입 이름 | 파라미터 순서까지 강제할 때 |
| `RequireAnyParameterTypeContaining(fragment)` | 하나 이상의 파라미터 타입 이름 | 특정 타입의 파라미터가 있는지만 확인할 때 |

### 개수 검증 vs 타입 검증
| Aspect | 개수 검증 | 타입 검증 |
|------|-----------|-----------|
| **강도** | 파라미터 추가/제거 감지 | 타입 변경 감지 |
| **유연성** | 정확한 수 또는 최솟값 | 문자열 기반 매칭 |
| **조합** | 단독 사용 가능 | 개수 검증과 함께 사용 권장 |

## FAQ

### Q1: RequireParameterCount와 RequireParameterCountAtLeast는 어떻게 구분해서 사용하나요?
**A**: `RequireParameterCount(3)`은 정확히 3개여야 통과합니다. `Address.Create`처럼 시그니처가 확정된 메서드에 적합합니다. `RequireParameterCountAtLeast(1)`은 1개 이상이면 통과하므로, "파라미터 없는 factory method는 허용하지 않는다"는 공통 규칙을 여러 클래스에 일괄 적용할 때 유용합니다.

### Q2: RequireFirstParameterTypeContaining과 RequireAnyParameterTypeContaining의 차이는?
**A**: `RequireFirstParameterTypeContaining`은 첫 번째 파라미터만 검사하여 파라미터 순서까지 강제합니다. `RequireAnyParameterTypeContaining`은 순서와 관계없이 해당 타입의 파라미터가 하나라도 존재하면 통과합니다. 예를 들어 `Coordinate.Create(double, double)`에서 `RequireAnyParameterTypeContaining("Double")`은 어느 위치든 `double` 파라미터가 있으면 성공합니다.

### Q3: 타입 이름 매칭에서 "String"과 "string"은 다른가요?
**A**: 매칭은 CLR 타입의 전체 이름(`System.String`, `System.Double` 등)에 대해 수행됩니다. C# 키워드(`string`, `double`)가 아니라 CLR 타입 이름의 일부를 사용해야 합니다. 대소문자가 구분되므로 `"String"`은 매칭되지만 `"string"`은 매칭되지 않습니다.

### Q4: 파라미터 개수 검증과 타입 검증을 함께 사용할 수 있나요?
**A**: 네, 체이닝으로 조합할 수 있습니다. `m.RequireParameterCount(3).RequireFirstParameterTypeContaining("String")`처럼 작성하면 "정확히 3개의 파라미터가 있고, 첫 번째는 `String` 타입"이라는 복합 규칙을 적용할 수 있습니다.

---

파라미터 시그니처까지 검증할 수 있게 되었습니다. Next chapter에서는 클래스의 **프로퍼티와 필드를** 검증하여, 도메인 클래스의 immutability이 깨지지 않도록 `public setter` 금지와 인스턴스 필드 금지 규칙을 강제하는 방법을 학습합니다.

→ [4장: 프로퍼티와 필드 검증](../04-Property-And-Field-Validation/)
