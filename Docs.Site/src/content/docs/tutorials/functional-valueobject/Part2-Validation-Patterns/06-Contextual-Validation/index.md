---
title: "컨텍스트 기반 검증 (ContextualValidation)"
---

## 개요

Part 2의 1~5장에서는 `ValidationRules<Price>.NotEmpty(value)` 처럼 **타입 기반** 검증을 배웠습니다. 이 패턴은 도메인 레이어에서 Value Object를 검증할 때 이상적입니다 — 에러 메시지에 `Price`라는 타입 정보가 자동으로 포함되기 때문입니다.

하지만 Application 레이어에서 DTO를 검증할 때는 상황이 다릅니다. `CreateOrderCommand`의 `Price` 필드와 `ShippingCost` 필드가 둘 다 `decimal`이라면? 타입 정보만으로는 어떤 필드에서 오류가 발생했는지 구분할 수 없습니다.

`ContextualValidation`은 **필드 이름(컨텍스트)을** 에러에 포함시켜 이 문제를 해결합니다.

> **"Domain Layer는 타입이 컨텍스트, Application Layer는 필드 이름이 컨텍스트."**

## 학습 목표

1. **`ValidationRules.For("fieldName")`** — Named Context 검증 진입점
2. **`ContextualValidation<T>`** — 컨텍스트를 체이닝 중 전파하는 wrapper
3. **TypedValidation vs ContextualValidation** 비교
4. **Apply 조합**으로 다중 필드 병렬 검증

### 실습을 통해 확인할 내용
- **PhoneNumber**: 단일 필드 검증 — NotNull → NotEmpty → MinLength → MaxLength
- **Address**: 다중 필드 병렬 검증 — City + Street + PostalCode를 Apply로 조합

## TypedValidation vs ContextualValidation

| 특성 | TypedValidation | ContextualValidation |
|------|-----------------|---------------------|
| **진입점** | `ValidationRules<Price>.NotEmpty(value)` | `ValidationRules.For("price").NotEmpty(value)` |
| **컨텍스트** | 타입 이름 (`Price`) | 필드 이름 (`"price"`) |
| **에러 식별** | `DomainError.For<Price>(...)` | `DomainError.ForContext("price", ...)` |
| **주 사용처** | Domain Layer — Value Object 검증 | Application Layer — DTO/Command 검증 |
| **장점** | 컴파일 타임 타입 안전성 | 동적 필드 이름 지정 |

동일한 검증 규칙 카테고리(Presence, Length, Numeric, Format, DateTime)를 공유합니다.

## 코드 구조

### ValidationRules.For — 진입점

```csharp
// Named Context 검증 시작
ValidationContext context = ValidationRules.For("PhoneNumber");

// 체이닝 — 컨텍스트가 자동 전파
ContextualValidation<string> result = context
    .NotNull(phoneNumber)   // ContextualValidation<string> 반환
    .ThenNotEmpty()         // Then* 메서드로 체이닝
    .ThenMinLength(10)
    .ThenMaxLength(15);

// Validation<Error, string>으로 암시적 변환
Validation<Error, string> validation = result;
```

### ContextualValidation<T> — 컨텍스트 전파 wrapper

```csharp
public readonly struct ContextualValidation<T>
{
    public Validation<Error, T> Value { get; }
    public string ContextName { get; }

    // Validation<Error, T>로 암시적 변환
    public static implicit operator Validation<Error, T>(
        ContextualValidation<T> contextual) => contextual.Value;
}
```

`ContextualValidation<T>`는 `Validation<Error, T>`를 감싸면서 `ContextName`을 체이닝 메서드에 전달합니다. `ThenNotEmpty()`, `ThenMinLength()` 등의 체이닝 메서드는 이 컨텍스트를 사용하여 에러 메시지를 생성합니다.

### 단일 필드 검증

```csharp
public static Validation<Error, string> Validate(string? phoneNumber)
    => ValidationRules.For("PhoneNumber")
        .NotNull(phoneNumber)
        .ThenNotEmpty()
        .ThenMinLength(10)
        .ThenMaxLength(15);
// 실패 시 에러: "PhoneNumber cannot be null."
```

### 다중 필드 Apply 조합

```csharp
public static Validation<Error, AddressDto> Validate(
    string? city, string? street, string? postalCode)
    => (ValidateCity(city), ValidateStreet(street), ValidatePostalCode(postalCode))
        .Apply((c, s, p) => new AddressDto(c, s, p));

private static Validation<Error, string> ValidateCity(string? value)
    => ValidationRules.For("City")
        .NotNull(value)
        .ThenNotEmpty()
        .ThenMaxLength(100);
```

각 필드가 독립적으로 검증되고, **모든 오류가 한 번에 수집**됩니다. City가 null이고 PostalCode가 너무 짧으면 두 오류가 모두 반환됩니다.

## 검증 규칙 카테고리

`ValidationContext`는 TypedValidation과 동일한 규칙 카테고리를 제공합니다:

| 카테고리 | 메서드 예시 | 대상 |
|---------|-----------|------|
| **Presence** | `NotNull()` | 모든 타입 |
| **Length** | `NotEmpty()`, `MinLength()`, `MaxLength()`, `ExactLength()` | string |
| **Numeric** | `NotZero()`, `Positive()`, `NonNegative()`, `Between()` | INumber\<T\> |
| **Format** | `Matches(Regex)`, `IsUpperCase()`, `IsLowerCase()` | string |
| **DateTime** | `NotDefault()`, `InPast()`, `InFuture()`, `Before()` | DateTime |
| **Custom** | `Must(predicate, errorType, message)` | 모든 타입 |

## 한눈에 보는 정리

| 구성 요소 | 역할 |
|-----------|------|
| `ValidationRules.For(name)` | Named Context 검증 시작, `ValidationContext` 반환 |
| `ValidationContext` | 컨텍스트 이름을 보유, 검증 메서드 제공 |
| `ContextualValidation<T>` | 검증 결과 + 컨텍스트 이름을 체이닝 중 전파 |
| `Then*` 체이닝 | `ThenNotEmpty()`, `ThenMinLength()` 등 — Bind 기반 순차 검증 |
| `Apply` 조합 | 2~4개 필드의 병렬 검증 결과 조합 |

## FAQ

### Q1: FluentValidation과 어떻게 다른가요?
**A**: FluentValidation은 클래스 단위로 Validator를 정의하고 DI로 주입합니다. ContextualValidation은 함수형 — `Validation<Error, T>` 모나드 기반으로, 다른 검증 결과와 Apply/Bind로 합성할 수 있습니다. 두 접근법은 공존 가능합니다.

### Q2: Domain Layer에서도 ContextualValidation을 쓸 수 있나요?
**A**: 가능하지만 권장하지 않습니다. Domain Layer에서는 `ValidationRules<Price>.NotEmpty(value)`로 타입 기반 검증을 사용하는 것이 DDD 원칙에 부합합니다. ContextualValidation은 Value Object가 없는 Application/Presentation Layer에 적합합니다.

### Q3: 에러 메시지를 커스텀할 수 있나요?
**A**: `Must(predicate, errorType, message)` 커스텀 검증으로 자유로운 메시지를 지정할 수 있습니다. 내장 규칙(NotNull, NotEmpty 등)은 컨텍스트 이름을 자동으로 포함하는 표준화된 메시지를 생성합니다.

---

ContextualValidation은 Application Layer에서 DTO 필드를 검증할 때, **TypedValidation과 동일한 함수형 합성**을 필드 이름 기반으로 제공합니다.
