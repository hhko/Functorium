---
title: "UnionValueObject"
---

## 개요

`OrderStatus`가 `Pending`, `Confirmed`, `Shipped`, `Delivered` 중 **정확히 하나**여야 한다면? `Shape`가 `Circle`, `Rectangle`, `Triangle` 중 하나여야 한다면? 일반적인 상속은 "열린 계층"이라 새 하위 타입이 언제든 추가될 수 있습니다. Discriminated Union은 "닫힌 계층"으로, **컴파일 타임에 모든 케이스를 알 수 있고** 패턴 매칭으로 빠짐없이 처리할 수 있습니다.

이 챕터에서는 `UnionValueObject` — abstract record 기반의 Discriminated Union 값 객체를 구현합니다.

> **"값 객체가 여러 변형 중 정확히 하나일 때, abstract record 계층과 Match/Switch로 타입 안전한 분기를 보장합니다."**

## 학습 목표

1. **`UnionValueObject`** — abstract record 기반 Discriminated Union 패턴
2. **`IUnionValueObject`** — IValueObject를 확장하는 마커 인터페이스
3. **Match/Switch** — 수동 구현으로 원리 이해 + `[UnionType]` 소스 생성기 소개
4. **`UnreachableCaseException`** — 도달 불가능한 기본 케이스 안전장치
5. **`UnionValueObject<TSelf>`** — `TransitionFrom`으로 상태 전이를 지원하는 CRTP 패턴

### 실습을 통해 확인할 내용
- **Shape**: Circle | Rectangle | Triangle — 면적, 둘레 계산
- **PaymentMethod**: CreditCard | BankTransfer | Cash — 수수료 계산
- **OrderStatus**: Pending → Confirmed — Functorium `UnionValueObject<TSelf>` 상태 전이

## 핵심 타입 구조

```
IUnionValueObject (마커 인터페이스)
    ├── UnionValueObject (abstract record) — 순수 데이터 유니온
    │       ├── Shape (abstract record)
    │       │   ├── Circle(Radius)       — sealed record
    │       │   ├── Rectangle(W, H)      — sealed record
    │       │   └── Triangle(Base, H)    — sealed record
    │       └── PaymentMethod (abstract record)
    │           ├── CreditCard(CardNo, Expiry) — sealed record
    │           ├── BankTransfer(AccNo, Bank)  — sealed record
    │           └── Cash()                     — sealed record
    └── UnionValueObject<TSelf> (abstract record) — 상태 전이 유니온
            └── OrderStatus (abstract record)
                ├── Pending(OrderId)              — sealed record
                └── Confirmed(OrderId, ConfirmedAt) — sealed record
```

## Match와 Switch 패턴

### Match — 값을 반환하는 분기

모든 케이스에 대해 변환 함수를 제공하고 결과를 반환합니다:

```csharp
public TResult Match<TResult>(
    Func<Circle, TResult> circle,
    Func<Rectangle, TResult> rectangle,
    Func<Triangle, TResult> triangle) => this switch
{
    Circle c => circle(c),
    Rectangle r => rectangle(r),
    Triangle t => triangle(t),
    _ => throw new UnreachableCaseException(this)
};

// 사용
double area = shape.Match(
    circle: c => Math.PI * c.Radius * c.Radius,
    rectangle: r => r.Width * r.Height,
    triangle: t => 0.5 * t.Base * t.Height);
```

### Switch — 부수 효과를 위한 분기

반환 값 없이 각 케이스에 대한 액션을 실행합니다:

```csharp
shape.Switch(
    circle: c => Console.WriteLine($"원: 반지름={c.Radius}"),
    rectangle: r => Console.WriteLine($"사각형: {r.Width}×{r.Height}"),
    triangle: t => Console.WriteLine($"삼각형: 밑변={t.Base}"));
```

### UnreachableCaseException

`default` 분기의 안전장치입니다. sealed record만 사용하므로 이론적으로 도달할 수 없지만, 런타임 안전을 위해 명시합니다:

```csharp
_ => throw new UnreachableCaseException(this)
// "Unreachable case: Shape+Circle" 형태의 메시지
```

## 도메인 로직 예제

### Shape — 면적과 둘레

```csharp
public abstract record Shape : UnionValueObject
{
    public sealed record Circle(double Radius) : Shape;
    public sealed record Rectangle(double Width, double Height) : Shape;
    public sealed record Triangle(double Base, double Height) : Shape;

    public double Area => Match(
        circle: c => Math.PI * c.Radius * c.Radius,
        rectangle: r => r.Width * r.Height,
        triangle: t => 0.5 * t.Base * t.Height);
}
```

### PaymentMethod — 수수료 계산

```csharp
public abstract record PaymentMethod : UnionValueObject
{
    public sealed record CreditCard(string CardNumber, string ExpiryDate) : PaymentMethod;
    public sealed record BankTransfer(string AccountNumber, string BankCode) : PaymentMethod;
    public sealed record Cash() : PaymentMethod;

    public decimal CalculateFee(decimal amount) => Match(
        creditCard: _ => amount * 0.03m,
        bankTransfer: _ => 1000m,
        cash: _ => 0m);
}
```

## record 기반 동등성

`UnionValueObject`는 abstract record이므로 **값 기반 동등성**이 자동으로 보장됩니다:

```csharp
Shape a = new Shape.Circle(5.0);
Shape b = new Shape.Circle(5.0);
a == b  // true — 같은 Radius면 같은 도형

Shape c = new Shape.Rectangle(5.0, 5.0);
a == c  // false — 다른 케이스는 다른 값
```

## Functorium의 소스 생성기

이 튜토리얼에서는 Match/Switch를 수동으로 구현했지만, Functorium에서는 `[UnionType]` 어트리뷰트를 사용하면 **소스 생성기가 자동으로 생성**합니다:

```csharp
// Functorium 프레임워크 사용 시
[UnionType]
public abstract partial record Shape : UnionValueObject
{
    public sealed record Circle(double Radius) : Shape;
    public sealed record Rectangle(double Width, double Height) : Shape;
    public sealed record Triangle(double Base, double Height) : Shape;
    // Match, Switch 메서드가 자동 생성됩니다
}
```

`[UnionType]`은 내부 sealed record 케이스를 분석하여 타입 안전한 Match/Switch 메서드를 생성합니다.

## 상태 전이가 있는 Union

지금까지의 Shape, PaymentMethod는 **순수 데이터 유니온**입니다. 케이스 간 전이 개념이 없고, 현재 값에 따라 계산만 수행합니다.

하지만 `OrderStatus`처럼 **Pending → Confirmed → Shipped** 같은 상태 전이가 필요한 경우, 잘못된 전이(예: Confirmed에서 다시 Confirmed)를 방지해야 합니다. Functorium의 `UnionValueObject<TSelf>`는 `TransitionFrom` 헬퍼를 제공하여 이 문제를 해결합니다.

### UnionValueObject&lt;TSelf&gt;

CRTP(Curiously Recurring Template Pattern)를 사용하여 `DomainError`에 정확한 타입 정보를 전달합니다:

```csharp
using Functorium.Domains.ValueObjects.Unions;
using LanguageExt;

public abstract record OrderStatus : UnionValueObject<OrderStatus>
{
    public sealed record Pending(string OrderId) : OrderStatus;
    public sealed record Confirmed(string OrderId, DateTime ConfirmedAt) : OrderStatus;
    private OrderStatus() { }

    public Fin<Confirmed> Confirm(DateTime confirmedAt) =>
        TransitionFrom<Pending, Confirmed>(
            p => new Confirmed(p.OrderId, confirmedAt));
}
```

### TransitionFrom 동작 방식

`TransitionFrom<TSource, TTarget>`는 다음과 같이 동작합니다:

1. `this`가 `TSource` 타입이면 → 전이 함수를 적용하여 `TTarget` 반환 (성공)
2. `this`가 `TSource` 타입이 아니면 → `InvalidTransition` 에러 반환 (실패)

```csharp
// 성공: Pending → Confirmed
OrderStatus order = new OrderStatus.Pending("ORD-001");
var result = order.Confirm(DateTime.Now);
// result.IsSucc == true

// 실패: Confirmed → Confirmed (잘못된 전이)
OrderStatus confirmed = new OrderStatus.Confirmed("ORD-001", DateTime.Now);
var fail = confirmed.Confirm(DateTime.Now);
// fail.IsFail == true, 에러 메시지: "Invalid transition from Confirmed to Confirmed"
```

### UnionValueObject vs UnionValueObject&lt;TSelf&gt; 선택 기준

| 기준 | `UnionValueObject` | `UnionValueObject<TSelf>` |
|------|:-------------------:|:-------------------------:|
| 순수 데이터 변형 | ✅ | ✅ |
| 상태 전이 | ❌ | ✅ (`TransitionFrom`) |
| DomainError 타입 정보 | ❌ | ✅ (CRTP) |
| 사용 예 | Shape, PaymentMethod | OrderStatus, PaymentState |

## 한눈에 보는 정리

| 구성 요소 | 역할 |
|-----------|------|
| `UnionValueObject` | abstract record 기반 클래스 — DU의 루트 |
| `IUnionValueObject` | 마커 인터페이스 — IValueObject 확장, 아키텍처 테스트 필터용 |
| `Match<TResult>` | 모든 케이스를 처리하고 값을 반환 |
| `Switch` | 모든 케이스를 처리하고 부수 효과 실행 |
| `UnreachableCaseException` | default 분기 안전장치 |
| `[UnionType]` | 소스 생성기 트리거 (Match/Switch 자동 생성) |
| `UnionValueObject<TSelf>` | 상태 전이 지원 — `TransitionFrom` 헬퍼 제공 |

### 값 객체 타입 선택에서의 위치

| 조건 | 선택 |
|------|------|
| 단일 값, 비교 불필요 | `SimpleValueObject<T>` |
| 단일 값, 비교 필요 | `ComparableSimpleValueObject<T>` |
| 복합 값 | `ValueObject` / `ComparableValueObject` |
| 제한된 열거형 + 행위 | `SmartEnum + IValueObject` |
| **여러 변형 중 하나** | **`UnionValueObject`** |
| **상태 전이가 필요한 변형** | **`UnionValueObject<TSelf>`** |

## FAQ

### Q1: 일반 상속과 UnionValueObject의 차이는 무엇인가요?
**A**: 일반 상속은 "열린 계층"으로 누구나 새 하위 타입을 추가할 수 있습니다. UnionValueObject는 sealed record로 케이스를 닫아 **컴파일 타임에 모든 케이스를 알 수 있고**, Match/Switch에서 빠짐없이 처리를 강제합니다.

### Q2: record 대신 class로 구현하면 안 되나요?
**A**: 가능하지만 record를 사용하면 값 기반 동등성, `ToString()`, 디컨스트럭션이 자동으로 제공됩니다. 값 객체의 핵심 속성인 "값이 같으면 같은 객체"를 record가 무료로 보장합니다.

### Q3: `[UnionType]`과 수동 Match의 차이는 무엇인가요?
**A**: 기능은 동일합니다. `[UnionType]`은 케이스가 추가/삭제될 때 Match/Switch를 자동으로 업데이트하므로 실수를 방지합니다. 이 튜토리얼에서는 원리를 이해하기 위해 수동으로 구현했습니다.

### Q4: `UnionValueObject`와 `UnionValueObject<TSelf>`는 언제 구분하나요?
**A**: 케이스 간 전이가 없는 순수 데이터 유니온(Shape, PaymentMethod)은 `UnionValueObject`를, 상태 전이가 필요한 유니온(OrderStatus, PaymentState)은 `UnionValueObject<TSelf>`를 사용합니다. `UnionValueObject<TSelf>`는 CRTP로 `DomainError`에 정확한 타입 정보를 전달하며, `TransitionFrom` 헬퍼로 유효한 전이만 허용합니다.

---

UnionValueObject는 "여러 변형 중 정확히 하나"를 타입으로 표현합니다.

→ [부록 B: 타입 선택 가이드](../../Appendix/B-type-selection-guide.md)에서 전체 타입 선택 기준을 확인하세요.
