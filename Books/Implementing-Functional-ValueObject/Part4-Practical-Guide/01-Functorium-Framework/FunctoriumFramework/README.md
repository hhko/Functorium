# 4.1 Functorium 프레임워크 통합 🔴

> **Part 4: 실전 가이드** | [← 목차로](../../../README.md) | [다음: 4.2 ORM 통합 패턴 →](../../02-ORM-Integration/OrmIntegration/README.md)

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
IValueObject (인터페이스 - 명명 규칙)
    │
    └── AbstractValueObject (기본 클래스 - 동등성, 해시코드)
        │
        ├── ValueObject (검증 헬퍼 메서드)
        │   │
        │   ├── SimpleValueObject<T> (단일 값 래퍼)
        │   │   └── ComparableSimpleValueObject<T> (비교 가능)
        │   │
        │   └── ComparableValueObject (복합 비교 가능)
        │
        └── SmartEnum<TValue, TKey> + IValueObject (열거형)
```

---

## 실행 방법

```bash
cd Books/Functional-ValueObject/04-practical-guide/01-Functorium-Framework/FunctoriumFramework
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

### AbstractValueObject (기본 클래스)

```csharp
public abstract class AbstractValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (AbstractValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
```

### SimpleValueObject<T>

```csharp
public abstract class SimpleValueObject<T> : AbstractValueObject
{
    public T Value { get; }

    protected SimpleValueObject(T value) => Value = value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value!;
    }
}
```

### ComparableSimpleValueObject<T>

```csharp
public abstract class ComparableSimpleValueObject<T> : SimpleValueObject<T>,
    IComparable<ComparableSimpleValueObject<T>>
    where T : IComparable<T>
{
    protected ComparableSimpleValueObject(T value) : base(value) { }

    public int CompareTo(ComparableSimpleValueObject<T>? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }
}
```

---

## 다음 단계

ORM 통합 패턴을 학습합니다.

→ [4.2 ORM 통합 패턴](../../02-ORM-Integration/OrmIntegration/README.md)
