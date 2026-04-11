---
title: "value object"
---

> `ValueObject`

## Overview

2D 좌표는 X와 Y 두 값이 항상 함께 다녀야 의미가 있습니다. 지금까지 배운 `SimpleValueObject<T>`는 단일 값만 래핑할 수 있었는데, 이처럼 여러 primitive 타입을 조합한 도메인 개념은 어떻게 표현할까요? `ValueObject`는 여러 값을 하나의 불변 단위로 묶고, `GetEqualityComponents()`를 통해 구성 요소 기반의 동등성 비교를 provides.

## Learning Objectives

- 여러 primitive 타입을 조합하여 복합 value object를 구현할 수 있습니다
- `GetEqualityComponents()` 메서드로 동등성 비교 기준을 정의할 수 있습니다
- LINQ Expression을 활용하여 복합 validation logic을 구현할 수 있습니다
- 각 구성 요소에 대한 개별 검증과 통합 검증을 구분하여 적용할 수 있습니다

## Why Is This Needed?

실제 도메인에서는 단일 값으로 표현되지 않는 개념이 많습니다. 2D 좌표의 X와 Y를 별도 변수로 관리하면 한쪽만 업데이트되어 데이터 일관성이 깨지기 쉽습니다. 여러 값이 서로 관련되어 함께 검증해야 하는 경우, 개별 변수로는 유효성 보장이 복잡해집니다. 또한 복합 데이터의 동등성 비교를 수동으로 구현하면 구성 요소 누락 등의 오류가 발생하기 쉽습니다.

`ValueObject`는 관련 값들을 하나의 불변 객체로 캡슐화하고, 동등성 비교와 validation logic을 한 곳에서 관리할 수 있게 합니다.

## Core Concepts

### Primitive 타입 조합

`ValueObject`는 여러 기본 타입을 하나의 의미 있는 단위로 조합합니다. 분산된 관련 데이터를 하나의 객체로 묶어 응집성을 높이고 관련 로직을 집중시킵니다.

```csharp
// 분산된 데이터 (문제가 있음)
int x = 100;
int y = 200;

// 조합된 데이터 (해결됨)
Coordinate coord = Coordinate.Create(100, 200);
```

### GetEqualityComponents() 구현

`ValueObject`는 동등성 비교를 위해 `GetEqualityComponents()` 메서드를 구현해야 합니다. 이 메서드가 반환하는 모든 구성 요소가 같아야 두 인스턴스가 동일한 것으로 판단됩니다.

`Coordinate`의 경우 X와 Y 값이 모두 같아야 동일한 좌표로 취급됩니다. 특정 필드를 동등성 비교에서 제외하고 싶다면 해당 필드를 반환하지 않으면 됩니다.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return X;  // X 좌표를 비교 요소로
    yield return Y;  // Y 좌표를 비교 요소로
}
```

### 복합 validation logic

`ValueObject`는 각 구성 요소에 대한 개별 검증과 전체적인 유효성 검증을 모두 수행할 수 있습니다. LINQ Expression의 `from-in-select` 패턴을 사용하면 순차적 검증을 선언적으로 표현할 수 있습니다.

```csharp
public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
    from validX in ValidateX(x)      // X 좌표 개별 검증
    from validY in ValidateY(y)      // Y 좌표 개별 검증
    select (x: validX, y: validY);   // 검증된 값들 조합
```

## Practical Guidelines

### Expected Output
```
=== 3. 비교 불가능한 복합 primitive 값 객체 - ValueObject ===
부모 클래스: ValueObject
예시: Coordinate (2D 좌표)

📋 특징:
   ✅ 여러 primitive 값을 조합
   ✅ 동등성 비교만 제공
   ✅ 비교 기능은 제공되지 않음 (의도적으로)

🔍 성공 케이스:
   ✅ Coordinate: (100, 200) (X: 100, Y: 200)
   ✅ Coordinate: (100, 200) (X: 100, Y: 200)
   ✅ Coordinate: (300, 400) (X: 300, Y: 400)

📊 동등성 비교:
   (100, 200) == (100, 200) = True
   (100, 200) == (300, 400) = False

🔢 해시코드:
   (100, 200).GetHashCode() = -1711187277
   (100, 200).GetHashCode() = -1711187277
   동일한 값의 해시코드가 같은가? True

📊 비교 기능:
   비교 기능은 제공되지 않음 (의도적으로)
   정렬이나 크기 비교가 필요한 경우 ComparableValueObject 사용

❌ 실패 케이스:
   Coordinate(-1, 200): XOutOfRange
   Coordinate(100, 2000): YOutOfRange

💡 primitive 조합 값 객체의 특징:
   - 여러 primitive 타입(int, string, decimal 등)을 조합
   - 각 primitive 값에 대한 개별 검증 로직
   - 동등성 비교만 제공 (비교 기능 없음)
   - 복잡한 도메인 개념을 단순한 primitive 조합으로 표현

✅ 데모가 성공적으로 완료되었습니다!
```

### Key Implementation Points

`ValueObject` 기반 복합 value object 구현의 필수 요소를 정리합니다.

| 포인트 | Description |
|--------|------|
| **ValueObject 상속** | 복합 value object의 기본 기능 상속 |
| **GetEqualityComponents() 구현** | 동등성 비교를 위한 구성 요소 정의 |
| **LINQ Expression 검증** | from-in-select 패턴을 활용한 복합 검증 |
| **개별 검증 메서드** | 각 primitive 값에 대한 독립적 검증 |

## Project Description

### Project Structure
```
03-ValueObject-Primitive/
├── Program.cs                    # 메인 실행 파일
├── ValueObjectPrimitive.csproj  # 프로젝트 파일
├── ValueObjects/
│   └── Coordinate.cs            # 2D 좌표 값 객체
└── README.md                    # 프로젝트 문서
```

### Core Code

`Coordinate`는 `ValueObject`를 상속하여 X, Y 두 정수를 하나의 2D 좌표로 표현합니다.

**Coordinate.cs - 복합 primitive value object 구현**
```csharp
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(Validate(x, y), v => new Coordinate(v.x, v.y));

    public static Coordinate CreateFromValidated((int x, int y) validatedValues) =>
        new(validatedValues.x, validatedValues.y);

    // LINQ Expression을 활용한 복합 검증
    public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (x: validX, y: validY);

    // ValidationRules<T>를 활용한 개별 검증
    private static Validation<Error, int> ValidateX(int x) =>
        ValidationRules<Coordinate>.NonNegative(x);

    private static Validation<Error, int> ValidateY(int y) =>
        ValidationRules<Coordinate>.Between(y, 0, 1000);

    // 동등성 비교를 위한 구성 요소
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() => $"({X}, {Y})";
}
```

동등성 비교와 해시코드를 확인하는 데모 코드입니다.

**Program.cs - 복합 value object 데모**
```csharp
// 복합 값 객체 생성
var coord1 = Coordinate.Create(100, 200);
var coord2 = Coordinate.Create(100, 200);
var coord3 = Coordinate.Create(300, 400);

// 동등성 비교
var c1 = coord1.Match(Succ: x => x, Fail: _ => default!);
var c2 = coord2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {c1} == {c2} = {c1 == c2}");

// 해시코드 확인
Console.WriteLine($"   {c1}.GetHashCode() = {c1.GetHashCode()}");
Console.WriteLine($"   {c2}.GetHashCode() = {c2.GetHashCode()}");
```

## Summary at a Glance

단일 값 래핑과 복합 값 조합의 차이를 compares.

### Comparison Table
| Aspect | `SimpleValueObject<T>` | ValueObject |
|------|---------------------|-------------|
| **값 개수** | 단일 primitive | 복합 primitive |
| **GetEqualityComponents()** | 자동 구현 | 수동 구현 필요 |
| **validation logic** | 단순 검증 | 복합 검증 가능 |
| **LINQ 활용** | 불필요 | 복합 검증에 유용 |
| **용도** | 단순 값 래핑 | 복합 도메인 개념 |

## FAQ

### Q1: GetEqualityComponents()는 왜 필요한가요?
**A**: 복합 value object의 동등성을 정의하기 위해 필요합니다. 좌표의 경우 X와 Y가 모두 같아야 동일한 좌표이므로, 두 값 모두를 returns. 특정 필드를 비교에서 제외하려면 해당 필드를 반환하지 않으면 됩니다.

### Q2: LINQ Expression을 왜 사용하나요?
**A**: `from-in-select` 패턴으로 복합 검증을 선언적으로 표현할 수 있습니다. X 검증이 실패하면 Y 검증을 건너뛰는 단락 평가가 자연스럽게 구현되며, if-else 체인보다 읽기 쉽습니다.

### Q3: 언제 ValueObject 대신 일반 클래스를 사용해야 하나요?
**A**: 값이 변경되어야 하거나 reference equality이 필요한 경우입니다. 은행 계좌 잔고처럼 자주 변경되는 데이터는 일반 클래스가 적합합니다. 이벤트, 설정값처럼 생성 후 변경되지 않는 값에 `ValueObject`를 uses.

Next chapter에서는 `ValueObject`에 비교 기능을 추가한 `ComparableValueObject`를 학습합니다. 날짜 범위처럼 복합 데이터에도 자연스러운 순서가 필요한 경우를 다룹니다.

---

→ [4장: ComparableValueObject (Primitive)](../04-ComparableValueObject-Primitive/)
