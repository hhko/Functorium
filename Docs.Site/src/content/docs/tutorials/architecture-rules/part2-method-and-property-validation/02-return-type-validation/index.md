---
title: "Return Type Validation"
---

## Overview

`Email.Create`가 `Fin<Email>`을 반환해야 한다는 규칙, 누군가 그냥 `Email`을 반환하면 어떻게 될까요? 컴파일은 문제없이 통과합니다. 호출하는 쪽에서 `IsSucc`/`IsFail` 분기를 기대하지만, 실제로는 예외가 던져지는 코드가 되어 runtime에 비로소 장애가 발생합니다. In this chapter, 메서드의 반환 타입을 아키텍처 테스트로 검증하여, **`Fin<T>`와** 같은 함수형 결과 타입의 사용을 강제하거나, factory method가 자신의 클래스 타입을 반환하는지 확인하는 방법을 학습합니다.

> **"반환 타입 규칙을 테스트로 강제하면, 함수형 오류 처리 패턴이 깨지는 것을 코드 리뷰 전에 잡을 수 있습니다."**

## Learning Objectives

### 핵심 학습 목표
1. **오픈 제네릭 반환 타입 검증**
   - `RequireReturnType(typeof(Fin<>))`로 `Fin<Email>`, `Fin<PhoneNumber>` 등 모든 닫힌 제네릭과 매칭
   - 오픈 제네릭은 접두사 비교 방식으로 동작

2. **자기 타입 반환 검증**
   - `RequireReturnTypeOfDeclaringClass()`로 factory method가 선언 클래스 타입을 반환하는지 확인
   - 이미 검증된 값을 조합하는 팩토리 패턴에 적합

3. **반환 타입 이름 기반 검증**
   - `RequireReturnTypeContaining("Fin")`으로 타입 이름의 일부만으로 유연하게 검증
   - 정확한 타입 참조 없이도 규칙 적용 가능

### 실습을 통해 확인할 내용
- **Email.Create / PhoneNumber.Create**: `Fin<T>` 반환 타입 강제
- **Customer.CreateFromValidated**: 자기 타입(`Customer`) 반환 검증
- 오픈 제네릭 매칭과 문자열 기반 매칭의 차이

## 도메인 코드

### Email / PhoneNumber 클래스

`Create` 메서드가 `Fin<T>`를 반환하여 생성 실패를 안전하게 표현합니다.

```csharp
public sealed class Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value)
        => string.IsNullOrWhiteSpace(value) || !value.Contains('@')
            ? Fin.Fail<Email>(Error.New("Invalid email"))
            : Fin.Succ(new Email(value));
}
```

### Customer 클래스

`CreateFromValidated`는 이미 검증된 값을 받아 자기 자신의 타입(`Customer`)을 직접 returns.

```csharp
public sealed class Customer
{
    public string Name { get; }
    public Email Email { get; }

    private Customer(string name, Email email)
    {
        Name = name;
        Email = email;
    }

    public static Customer CreateFromValidated(string name, Email email)
        => new(name, email);
}
```

## 테스트 코드

### 오픈 제네릭 반환 타입 검증

`typeof(Fin<>)`를 전달하면 `Fin<Email>`, `Fin<PhoneNumber>` 등 모든 닫힌 제네릭 타입과 매칭됩니다.

```csharp
[Fact]
public void CreateMethods_ShouldReturn_FinOpenGeneric()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireReturnType(typeof(Fin<>))),
            verbose: true)
        .ThrowIfAnyFailures("Fin Return Type Rule");
}
```

### 자기 타입 반환 검증

`RequireReturnTypeOfDeclaringClass()`는 메서드의 반환 타입이 선언 클래스와 동일한지 검증합니다.

```csharp
[Fact]
public void CreateFromValidated_ShouldReturn_DeclaringClass()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Customer")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("CreateFromValidated", m => m
                .RequireReturnTypeOfDeclaringClass()),
            verbose: true)
        .ThrowIfAnyFailures("Factory Return Type Rule");
}
```

### 반환 타입 이름 포함 검증

`RequireReturnTypeContaining`은 반환 타입의 전체 이름에 지정된 문자열이 포함되어 있는지 검증합니다.

```csharp
[Fact]
public void CreateMethods_ShouldReturn_TypeContainingFin()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireReturnTypeContaining("Fin")),
            verbose: true)
        .ThrowIfAnyFailures("Fin Return Type Containing Rule");
}
```

## Summary at a Glance

The following table 반환 타입 검증 API의 매칭 방식과 사용 시나리오를 요약합니다.

### 반환 타입 검증 API 요약
| API | 매칭 방식 | 사용 시나리오 |
|-----|-----------|---------------|
| `RequireReturnType(typeof(Fin<>))` | 오픈 제네릭 접두사 비교 | `Fin<Email>`, `Fin<PhoneNumber>` 등 제네릭 패밀리 검증 |
| `RequireReturnType(typeof(string))` | 정확한 타입 매칭 | 특정 타입을 반환해야 하는 경우 |
| `RequireReturnTypeOfDeclaringClass()` | 선언 클래스와 동일 | 자기 타입을 반환하는 팩토리 패턴 |
| `RequireReturnTypeContaining("Fin")` | 타입 이름 문자열 포함 | 타입 참조 없이 유연하게 검증 |

### 세 가지 검증 방식 비교
| Aspect | 오픈 제네릭 | 자기 타입 | 문자열 포함 |
|------|-------------|-----------|-------------|
| **정확성** | 높음 (타입 시스템 기반) | 높음 (선언 클래스 기반) | 보통 (문자열 매칭) |
| **유연성** | 제네릭 패밀리 전체 매칭 | 클래스별 자동 매칭 | 가장 유연 |
| **적합한 경우** | `Fin<T>`, `Result<T>` 등 | 빌더/팩토리 패턴 | 타입 참조가 어려운 경우 |

## FAQ

### Q1: RequireReturnType에 오픈 제네릭 typeof(Fin<>)를 전달하면 어떻게 매칭되나요?
**A**: 오픈 제네릭은 접두사 비교 방식으로 동작합니다. `typeof(Fin<>)`의 FullName 접두사가 실제 반환 타입의 FullName과 비교되어, `Fin<Email>`, `Fin<PhoneNumber>` 등 모든 닫힌 제네릭 변형과 매칭됩니다.

### Q2: RequireReturnTypeOfDeclaringClass는 상속 관계도 고려하나요?
**A**: 아닙니다. 정확히 선언 클래스와 동일한 타입인지만 검증합니다. `Customer` 클래스의 메서드가 `Customer`를 반환하면 통과하지만, `Customer`의 하위 타입을 반환하면 실패합니다. 이는 factory method가 정확한 자기 타입을 반환하도록 강제하는 데 적합합니다.

### Q3: RequireReturnTypeContaining은 어떤 경우에 사용하나요?
**A**: 대상 타입의 어셈블리를 직접 참조하기 어렵거나, 타입 이름 규칙만으로 충분한 경우에 유용합니다. 예를 들어 외부 라이브러리의 제네릭 타입을 검증할 때 `typeof()`로 참조하기 번거로우면 `RequireReturnTypeContaining`으로 문자열 기반 매칭을 할 수 있습니다. 다만 "Fin"이라는 문자열이 다른 타입에도 포함될 수 있으므로 오탐에 주의해야 합니다.

### Q4: Fin<T> 대신 다른 결과 타입(Result<T> 등)에도 같은 방식을 쓸 수 있나요?
**A**: 네, `RequireReturnType(typeof(Result<>))`처럼 어떤 제네릭 타입이든 동일하게 적용할 수 있습니다. 프로젝트에서 사용하는 결과 타입이 무엇이든 오픈 제네릭 매칭으로 일관되게 강제할 수 있습니다.

---

반환 타입 검증으로 함수형 결과 패턴의 일관성을 보장할 수 있게 되었습니다. Next chapter에서는 메서드의 **파라미터 개수와 타입을** 검증하여, factory method의 시그니처가 임의로 변경되는 것을 방지하는 방법을 학습합니다.

→ [3장: 파라미터 검증](../03-Parameter-Validation/)
