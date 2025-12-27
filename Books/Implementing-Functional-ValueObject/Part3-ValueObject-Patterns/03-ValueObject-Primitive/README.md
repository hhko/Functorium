# 값 객체
> `ValueObject`

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에 보는 정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 여러 기본 타입(primitive types)을 조합하여 복합적인 도메인 개념을 표현하는 `ValueObject` 패턴을 이해하고 실습하는 것을 목표로 합니다. 값 객체의 확장된 개념으로, 단일 값이 아닌 여러 값들의 조합을 하나의 의미 있는 단위로 다룹니다.

## 학습 목표

### **핵심 학습 목표**
1. **복합 값 객체 이해**: 여러 primitive 타입을 조합하여 복잡한 도메인 개념을 표현하는 방법을 학습합니다.
2. **GetEqualityComponents() 구현**: 동등성 비교를 위한 구성 요소 반환 방법을 실습합니다.
3. **복합 검증 로직**: 여러 값에 대한 개별 검증과 통합 검증을 구현하는 방법을 학습합니다.
4. **LINQ Expression 활용**: 복합 검증에서 LINQ 표현식을 사용하는 방법을 체험합니다.

### **실습을 통해 확인할 내용**
- `ValueObject`의 상속과 구현
- `GetEqualityComponents()` 메서드의 역할
- 여러 primitive 값들의 개별 검증
- LINQ를 활용한 검증 로직

## 왜 필요한가?

이전 단계인 `02-ComparableSimpleValueObject`에서는 단일 primitive 타입을 래핑하여 값 객체를 만들었습니다. 하지만 실제 애플리케이션에서 도메인 개념들은 단일 값으로 표현되지 않는 경우가 많습니다.

**첫 번째 문제는 복합 데이터의 표현입니다.** 예를 들어, 2D 좌표는 X와 Y 두 개의 값으로 구성되지만, 이를 별도의 변수로 관리하면 데이터의 일관성이 깨지기 쉽습니다. 이는 마치 객체지향에서 관련 데이터를 하나의 클래스로 묶지 않고 개별 필드로 관리하는 것처럼 데이터 구조의 응집성이 떨어집니다.

**두 번째 문제는 복합 검증의 어려움입니다.** 여러 값들이 서로 관련되어 검증해야 하는 경우, 각 값을 개별적으로 검증하기 어렵습니다. 이는 마치 데이터베이스에서 참조 무결성을 유지하기 어려운 것처럼 데이터의 유효성을 보장하기가 복잡합니다.

**세 번째 문제는 동등성 비교의 복잡성입니다.** 복합 데이터를 비교할 때 모든 구성 요소를 고려해야 하는데, 이를 수동으로 구현하기가 번거롭습니다. 이는 마치 여러 필드를 가진 객체의 동등성을 직접 구현하는 것처럼 오류가 발생하기 쉽습니다.

이러한 문제들을 해결하기 위해 `ValueObject`를 도입했습니다. `ValueObject`는 여러 primitive 타입을 하나의 의미 있는 단위로 묶어 관리할 수 있게 해줍니다. 이는 마치 데이터베이스에서 복합 키를 사용하는 것처럼 여러 값들의 조합을 하나의 개념으로 다룰 수 있게 합니다.

## 핵심 개념

이 프로젝트의 핵심은 여러 primitive 타입을 조합하여 복합적인 도메인 개념을 표현하는 `ValueObject`입니다. 크게 세 가지 개념으로 나눌 수 있습니다.

### 첫 번째 개념: Primitive 타입 조합

`ValueObject`는 여러 기본 타입을 하나의 의미 있는 단위로 조합할 수 있습니다. 이는 단일 값으로는 표현할 수 없는 복잡한 도메인 개념을 표현할 때 유용합니다.

**핵심 아이디어는 "여러 값을 하나의 개념으로"입니다.** 일반적으로 분산되어 있는 관련 데이터를 하나의 객체로 묶어 관리할 수 있습니다.

예를 들어, 2D 좌표는 X와 Y 두 개의 정수로 구성됩니다. 이 두 값을 별도로 관리하는 대신 하나의 `Coordinate` 객체로 표현할 수 있습니다. 이는 마치 수학에서 좌표를 (x, y)로 표현하는 것처럼 자연스러운 방식입니다.

```csharp
// 분산된 데이터 (문제가 있음)
int x = 100;
int y = 200;

// 조합된 데이터 (해결됨)
Coordinate coord = Coordinate.Create(100, 200);
```

이러한 조합은 데이터의 응집성을 높이고 관련 로직을 한 곳에 집중시킬 수 있습니다. 마치 객체지향에서 관련 메서드와 데이터를 하나의 클래스로 묶는 것처럼, 관련 값들을 하나의 값 객체로 묶을 수 있습니다.

### 두 번째 개념: GetEqualityComponents() 구현

`ValueObject`는 동등성 비교를 위해 `GetEqualityComponents()` 메서드를 구현해야 합니다. 이 메서드는 객체의 동등성을 결정하는 모든 구성 요소를 반환합니다.

**핵심 아이디어는 "구성 요소 기반 동등성"입니다.** 두 `ValueObject` 인스턴스가 동일하려면 모든 구성 요소가 같아야 합니다.

예를 들어, `Coordinate`의 경우 X와 Y 값이 모두 같아야 동일한 좌표로 취급됩니다. 이는 마치 데이터베이스에서 복합 키의 동등성을 비교하는 것과 유사합니다.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return X;  // X 좌표를 비교 요소로
    yield return Y;  // Y 좌표를 비교 요소로
}
```

이러한 구성 요소 기반 비교는 복합 객체의 동등성을 정확하게 정의할 수 있게 합니다. 이는 마치 집합에서 원소의 동일성을 정의하는 것처럼, 객체의 동일성을 명확히 지정할 수 있습니다.

### 세 번째 개념: 복합 검증 로직

`ValueObject`는 여러 값에 대한 복합적인 검증 로직을 구현할 수 있습니다. 각 구성 요소에 대한 개별 검증과 전체적인 유효성 검증을 모두 수행할 수 있습니다.

**핵심 아이디어는 "각각과 전체의 검증"입니다.** 개별 값들의 유효성과 값들 간의 관계 유효성을 모두 검증할 수 있습니다.

예를 들어, 좌표의 경우 X와 Y가 각각 유효한 범위 내에 있어야 하고, 추가적으로 특정 영역 내에 있어야 하는 등의 복합적인 검증이 가능합니다. 이는 마치 폼 검증에서 필드별 검증과 전체 폼 검증을 모두 수행하는 것과 유사합니다.

```csharp
public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
    from validX in ValidateX(x)      // X 좌표 개별 검증
    from validY in ValidateY(y)      // Y 좌표 개별 검증
    select (x: validX, y: validY);   // 검증된 값들 조합
```

이러한 복합 검증은 도메인 규칙을 정확하게 반영할 수 있습니다. 이는 마치 비즈니스 로직에서 여러 조건을 종합적으로 평가하는 것처럼, 복잡한 유효성 규칙을 구현할 수 있습니다.

## 실전 지침

### 예상 출력
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

### 핵심 구현 포인트
1. **ValueObject 상속**: 복합 값 객체의 기본 기능 상속
2. **GetEqualityComponents() 구현**: 동등성 비교를 위한 구성 요소 정의
3. **LINQ Expression 검증**: from-in-select 패턴을 활용한 복합 검증
4. **개별 검증 메서드**: 각 primitive 값에 대한 독립적 검증

## 프로젝트 설명

### 프로젝트 구조
```
03-ValueObject-Primitive/
├── Program.cs                    # 메인 실행 파일
├── ValueObjectPrimitive.csproj  # 프로젝트 파일
├── ValueObjects/
│   └── Coordinate.cs            # 2D 좌표 값 객체
└── README.md                    # 프로젝트 문서
```

### 핵심 코드

**Coordinate.cs - 복합 primitive 값 객체 구현**
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
        CreateFromValidation(
            Validate(x, y),
            validValues => new Coordinate(validValues.x, validValues.y));

    internal static Coordinate CreateFromValidated((int x, int y) validatedValues) =>
        new Coordinate(validatedValues.x, validatedValues.y);

    // LINQ Expression을 활용한 복합 검증
    public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (x: validX, y: validY);

    // 개별 검증 메서드들
    private static Validation<Error, int> ValidateX(int x) =>
        x >= 0 ? x : DomainErrors.XOutOfRange(x);

    private static Validation<Error, int> ValidateY(int y) =>
        y >= 0 && y <= 1000 ? y : DomainErrors.YOutOfRange(y);

    // 동등성 비교를 위한 구성 요소
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    internal static class DomainErrors
    {
        public static Error XOutOfRange(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(XOutOfRange)}",
                errorCurrentValue: value);

        public static Error YOutOfRange(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(YOutOfRange)}",
                errorCurrentValue: value);
    }
}
```

**Program.cs - 복합 값 객체 데모**
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

## 한눈에 보는 정리

### 비교 표
| 구분 | `SimpleValueObject<T>` | ValueObject |
|------|---------------------|-------------|
| **값 개수** | 단일 primitive | 복합 primitive |
| **GetEqualityComponents()** | 자동 구현 | 수동 구현 필요 |
| **검증 로직** | 단순 검증 | 복합 검증 가능 |
| **LINQ 활용** | 불필요 | 복합 검증에 유용 |
| **용도** | 단순 값 래핑 | 복합 도메인 개념 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **복합 데이터 표현** | 구현 복잡도 증가 |
| **개별 값 검증** | GetEqualityComponents() 구현 필요 |
| **LINQ 검증 활용** | 여러 값 관리 복잡 |
| **의미 있는 조합** | 디버깅 어려움 증가 |

## FAQ

### Q1: ValueObject와 일반 클래스의 차이점은 무엇인가요?
**A**: `ValueObject`는 불변성과 값 기반 동등성을 강제하는 반면, 일반 클래스는 이러한 제약이 없습니다. `ValueObject`는 생성 후에는 값을 변경할 수 없고, 동등성은 값으로 판단합니다.

일반 클래스는 참조 동등성을 사용하고 값을 변경할 수 있습니다. 이는 마치 함수형 프로그래밍에서 불변 데이터 구조를 사용하는 것과 유사합니다. `ValueObject`는 데이터의 일관성과 예측 가능성을 보장합니다.

또한 `ValueObject`는 `GetEqualityComponents()`를 통해 동등성 비교 방식을 명시적으로 정의할 수 있습니다. 이는 일반 클래스에서 `Equals()`를 오버라이드하는 것보다 더 명확하고 안전한 방식입니다.

### Q2: GetEqualityComponents()는 왜 필요한가요?
**A**: `GetEqualityComponents()`는 복합 값 객체의 동등성을 정의하기 위해 필요합니다. 이 메서드는 객체를 구성하는 모든 요소를 반환하여, 두 객체가 동일한지 비교할 수 있게 합니다.

예를 들어, 좌표의 경우 X와 Y 값이 모두 같아야 동일한 좌표로 취급됩니다. 이 메서드를 통해 이러한 비교 로직을 명시적으로 정의할 수 있습니다. 이는 마치 데이터베이스에서 복합 키를 정의하는 것과 유사합니다.

이러한 명시적 정의는 코드의 의도를 명확히 하고, 실수를 방지합니다. 만약 특정 필드를 동등성 비교에서 제외하고 싶다면, 해당 필드를 `GetEqualityComponents()`에서 반환하지 않으면 됩니다.

### Q3: LINQ Expression을 왜 사용하는가요?
**A**: LINQ Expression은 복합 검증 로직을 더 가독성 있게 표현할 수 있습니다. 특히 여러 값에 대한 검증이 순차적으로 이루어져야 하는 경우에 유용합니다.

예를 들어, X 좌표 검증이 실패하면 Y 좌표 검증을 수행하지 않는 '단락 평가' 방식으로 검증할 수 있습니다. 이는 마치 조건문 체이닝을 하는 것처럼 직관적입니다.

```csharp
// LINQ Expression 활용
from validX in ValidateX(x)    // X 검증
from validY in ValidateY(y)    // Y 검증 (X가 유효할 때만)
select (x: validX, y: validY)  // 결과 조합
```

이러한 방식은 전통적인 if-else 체인보다 더 선언적이고 읽기 쉽습니다. 이는 마치 함수형 프로그래밍에서 모나드를 사용하는 것처럼, 복잡한 로직을 간결하게 표현할 수 있습니다.

### Q4: 언제 ValueObject 대신 일반 클래스를 사용해야 하나요?
**A**: 값이 변경되어야 하거나, 참조 동등성이 필요한 경우 일반 클래스를 사용합니다. `ValueObject`는 값 기반 동등성과 불변성이 필요한 경우에만 사용해야 합니다.

예를 들어, 은행 계좌 잔고처럼 값이 자주 변경되는 경우 일반 클래스를 사용하는 것이 좋습니다. `ValueObject`는 이벤트, 로그, 설정과 같이 한 번 생성된 후 변경되지 않는 값에 적합합니다.

이는 마치 데이터베이스에서 정규화된 테이블을 사용할지, 비정규화된 뷰를 사용할지 결정하는 것과 유사합니다. 변경이 빈번한 데이터는 일반 클래스로, 변경이 드문 값은 `ValueObject`로 표현하는 것이 좋습니다.

### Q5: 복합 값 객체의 성능에 영향이 있나요?
**A**: `ValueObject`는 일반 클래스보다 약간의 오버헤드가 있지만, 실제 애플리케이션에서는 큰 성능 차이가 없습니다. `GetEqualityComponents()`는 LINQ를 사용하므로 약간의 할당 오버헤드가 있을 수 있습니다.

그러나 대부분의 경우 이러한 오버헤드는 무시할 만합니다. 값 객체는 일반적으로 자주 생성되지 않고, 동등성 비교도 빈번하지 않습니다. 이는 마치 마이크로 최적화가 큰 의미가 없는 것처럼, `ValueObject`의 성능 오버헤드는 실제 bottleneck이 되지 않습니다.

성능이 정말 중요한 경우에만 일반 클래스를 고려하고, 대부분의 경우 `ValueObject`가 제공하는 안정성과 가독성의 이점이 더 큽니다.

### Q6: 좌표 평면에서 원점을 기준으로 좌표를 검증하려면 어떻게 하나요?
**A**: `ValidateX`와 `ValidateY` 메서드에서 검증 로직을 수정하여 원점 기준 검증을 구현할 수 있습니다. 이는 마치 수학에서 좌표 평면의 특정 영역을 제한하는 것과 유사합니다.

예를 들어, X 좌표를 양수로 제한하려면:
```csharp
private static Validation<Error, int> ValidateX(int x) =>
    x > 0 ? x : DomainErrors.XMustBePositive(x);
```

또는 1사분면(양수 X, 양수 Y)만 허용하려면:
```csharp
private static Validation<Error, int> ValidateX(int x) =>
    x >= 0 ? x : DomainErrors.XMustBeNonNegative(x);

private static Validation<Error, int> ValidateY(int y) =>
    y >= 0 ? y : DomainErrors.YMustBeNonNegative(y);
```

이렇게 함으로써 도메인 규칙을 코드에 명확히 반영할 수 있습니다. 이는 마치 비즈니스 요구사항을 코드로 직접 표현하는 것처럼, 도메인 전문가가 코드를 읽고 이해할 수 있게 합니다. 실제 프로젝트에서는 이러한 검증 로직을 통해 UI에서 잘못된 좌표 입력을 방지하거나, 데이터베이스 저장 전에 유효성을 보장할 수 있습니다.
