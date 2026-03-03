---
title: "Functorium 프레임워크 통합"
---

> **Part 4: 실전 가이드** | [← 목차로](../../../) | [다음: 4.2 ORM 통합 패턴 →](../../02-ORM-Integration/OrmIntegration/)

---

## 개요

Functorium 프레임워크의 값 객체 타입 계층 구조를 학습하고 실전에서 활용하는 방법을 다룹니다.

---

## 학습 목표

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

> `Value` 속성은 `protected`이므로, 외부에서 값에 접근하려면 명시적 변환 연산자(`explicit operator T`)를 사용하거나 별도의 public 속성을 정의합니다.

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
    .ThenMaxLength(320)
    .ThenMatches(EmailRegex(), "Invalid email format")
    .ThenNormalize(v => v.Trim().ToLowerInvariant());

// DomainError.For<T>() 패턴
DomainError.For<Email>(new Empty(), value, "Email cannot be empty");
DomainError.For<Password>(new TooShort(MinLength: 8), value, "Password too short");
```

---

## 다음 단계

ORM 통합 패턴을 학습합니다.

→ [4.2 ORM 통합 패턴](../../02-ORM-Integration/OrmIntegration/)
