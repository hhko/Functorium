---
title: "Functorium 프레임워크 통합"
---
## Overview

Functorium 프레임워크의 value object 타입 계층 구조를 학습하고 실전에서 활용하는 방법을 다룹니다.

---

## Learning Objectives

- 프레임워크 타입 계층 구조 이해
- `SimpleValueObject<T>` 활용법
- `ComparableSimpleValueObject<T>` 활용법
- 복합 `ValueObject` 구현

---

## 프레임워크 타입 계층 구조

```
IValueObject (인터페이스 — 명명 규칙 상수)
    └── AbstractValueObject (기본 클래스 — 동등성, 해시코드, ORM 프록시)
        ├── ValueObject (CreateFromValidation<TVO, TValue> 헬퍼)
        │   └── SimpleValueObject<T> (단일 값 래퍼, CreateFromValidation<TVO> 헬퍼, protected T Value)
        └── ComparableValueObject (IComparable, 비교 연산자)
            └── ComparableSimpleValueObject<T> (단일 비교 가능 값 래퍼, protected T Value)
```

---

## 실행 방법

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/01-Functorium-Framework/FunctoriumFramework
dotnet run
```

---

## 예상 출력

```
=== Functorium 프레임워크 통합 ===

1. SimpleValueObject<T> 사용 예시
────────────────────────────────────────
   유효한 이메일: user@example.com
   오류: 유효한 이메일 형식이 아닙니다.

2. ComparableSimpleValueObject<T> 사용 예시
────────────────────────────────────────
   정렬 전: 30, 25, 35
   정렬 후: 25, 30, 35

3. ValueObject (복합) 사용 예시
────────────────────────────────────────
   주소: 서울 강남구 테헤란로 123 (06234)

4. 프레임워크 타입 계층 구조
────────────────────────────────────────
   ...
```

---

## 핵심 코드 설명

### SimpleValueObject\<T\>

> `Value` 속성은 `protected`이므로, 외부에서 값에 접근하려면 명시적 변환 연산자(`explicit operator T`)를 사용하거나 별도의 public 속성을 defines.

```csharp
public abstract class SimpleValueObject<T> : ValueObject
    where T : notnull
{
    protected T Value { get; }

    protected SimpleValueObject(T value) { Value = value; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    // 명시적 변환 연산자 (외부에서 값 접근)
    public static explicit operator T(SimpleValueObject<T>? valueObject) => ...;

    // CreateFromValidation 헬퍼
    public static Fin<TVO> CreateFromValidation<TVO>(
        Validation<Error, T> validation, Func<T, TVO> factory)
        where TVO : SimpleValueObject<T>;
}
```

### ComparableSimpleValueObject\<T\>

> `ComparableValueObject`를 상속하며, `SimpleValueObject<T>`와는 별도의 계층입니다.

```csharp
public abstract class ComparableSimpleValueObject<T> : ComparableValueObject
    where T : notnull, IComparable
{
    protected T Value { get; }

    protected ComparableSimpleValueObject(T value) { Value = value; }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        yield return Value;
    }

    // 명시적 변환 연산자 (외부에서 값 접근)
    public static explicit operator T(ComparableSimpleValueObject<T>? valueObject) => ...;
}
```

### ValidationRules\<T\> 시스템

```csharp
// 타입 파라미터를 한 번만 지정하는 검증 시작점
ValidationRules<Email>.NotNull(value)
    .ThenNotEmpty()
    .ThenNormalize(v => v.Trim().ToLowerInvariant())
    .ThenMaxLength(MaxLength)    // public const int MaxLength = 320;
    .ThenMatches(EmailRegex(), "Invalid email format");

// DomainError.For<T>() 패턴
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
```

## FAQ

### Q1: `SimpleValueObject<T>`와 `ComparableSimpleValueObject<T>`는 어떤 기준으로 선택하나요?
**A**: 값의 대소 비교나 정렬이 필요하면 `ComparableSimpleValueObject<T>`를, 동등성 비교만 필요하면 `SimpleValueObject<T>`를 uses. 예를 들어 `Email`은 정렬이 불필요하므로 `SimpleValueObject<string>`을, `Age`는 비교가 필요하므로 `ComparableSimpleValueObject<int>`를 상속합니다.

### Q2: `Value` 속성이 `protected`인 이유는 무엇인가요?
**A**: 외부에서 내부 값을 직접 접근하면 value object의 캡슐화가 깨질 수 있기 때문입니다. 외부에서 값이 필요한 경우 `explicit operator T` 변환 연산자를 사용하거나, 도메인에 맞는 public 속성을 별도로 defines.

### Q3: `ValidationRules<T>` 시스템은 반드시 사용해야 하나요?
**A**: 아닙니다. `if` 문과 `DomainError.For<T>()` 패턴으로 직접 검증해도 됩니다. `ValidationRules<T>`는 `NotNull`, `ThenNotEmpty`, `ThenMaxLength` 같은 공통 검증을 체이닝으로 간결하게 표현하고 싶을 때 사용하는 편의 시스템입니다.

---

## Next Steps

ORM 통합 패턴을 학습합니다.

→ [4.2 ORM 통합 패턴](../../02-ORM-Integration/OrmIntegration/)
