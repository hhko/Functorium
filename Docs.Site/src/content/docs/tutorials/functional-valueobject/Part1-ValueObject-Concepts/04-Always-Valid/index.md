---
title: "항상 유효한 타입"
---
## 개요

`Divide` 함수를 호출할 때마다 `denominator`가 0인지 확인해야 한다면, 그 검증을 아예 생략할 수 있는 방법은 없을까요? **항상 유효한 타입(Always Valid Type)을** 사용하면 컴파일 타임에 유효성을 보장하여, 런타임 검증 자체를 불필요하게 만들 수 있습니다.

> **런타임 검증 대신 컴파일 타임에 유효성을 보장해보자!**

## 학습 목표

이 장을 마치면 다음을 할 수 있습니다.

- 도메인 개념을 **값 객체(Value Object)로** 표현하고, 생성 시점에 유효성을 보장할 수 있습니다.
- **Private 생성자와 정적 팩토리 메서드를** 조합하여 항상 유효한 타입을 구현할 수 있습니다.
- 함수형 결과 타입(`Fin<T>`)과 값 객체를 결합하여 **컴파일 타임 안전성을** 확보할 수 있습니다.
- 비즈니스 규칙을 타입 시스템에 반영하여 **런타임 오류를 방지할 수 있습니다.**

### **실습을 통해 확인할 내용**
- **값 객체 생성**: `Denominator.Create(5)` → `Fin<Denominator>.Succ(denominator)` 반환
- **유효하지 않은 값**: `Denominator.Create(0)` → `Fin<Denominator>.Fail(Error)` 반환
- **안전한 함수**: `Divide(10, denominator)` → 검증 불필요, 항상 안전
- **컴파일 타임 보장**: 유효하지 않은 값은 컴파일 타임에 거부

## 왜 필요한가?

이전 단계인 `03-Functional-Result`에서는 함수형 결과 타입을 사용하여 예외 없이도 안전한 실패 처리를 할 수 있게 되었지만, 여전히 런타임에 유효성 검증을 해야 한다는 한계가 있었습니다.

`Divide` 함수를 호출할 때마다 `denominator`가 0인지 확인해야 하고, 이 검증 로직은 함수마다 반복됩니다. `Denominator.Create`에서 한 번 검증했더라도 `Divide` 함수에서 다시 검증해야 하므로 **DRY 원칙(Don't Repeat Yourself)에** 어긋납니다. 또한 "0이 아닌 정수"라는 비즈니스 규칙이 단순한 `int` 타입으로는 표현되지 않아, 코드를 읽는 사람이 이 제약을 파악하기 어렵습니다.

이러한 문제들을 해결하기 위해 **값 객체(Value Object)를** 도입했습니다. 값 객체를 사용하면 **컴파일 타임에 유효성을 보장**할 수 있고, **검증 로직을 한 곳에 집중**시킬 수 있으며, **도메인 개념을 코드에 명확하게 표현**할 수 있습니다.

## 핵심 개념

### 값 객체(Value Object)
- **도메인 개념의 표현**: 비즈니스 규칙을 타입으로 표현
- **불변성(Immutability)**: 생성 후 값이 변경되지 않음
- **캡슐화(Encapsulation)**: 내부 상태를 외부로부터 보호
- **유효성 보장**: 생성 시점에 모든 비즈니스 규칙 검증

### 항상 유효한 타입 패턴

Private 생성자로 직접 인스턴스 생성을 막고, 정적 팩토리 메서드에서 유효성을 검증합니다.

```csharp
public sealed class Denominator
{
    private readonly int _value;

    // Private 생성자 - 직접 인스턴스 생성 방지
    private Denominator(int value) =>
        _value = value;

    // 정적 팩토리 메서드 - 유효성 검증 후 생성
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("0은 허용되지 않습니다");

        return new Denominator(value);
    }

    // 안전한 값 접근
    public int Value =>
        _value;
}
```

### 컴파일 타임 vs 런타임 검증

이전 방식과 현재 방식의 차이를 비교합니다.

```csharp
// 런타임 검증 (이전 방식)
public static Fin<int> Divide(int numerator, int denominator)
{
    if (denominator == 0) // 런타임에 검증
        return Error.New("0은 허용되지 않습니다");

    return numerator / denominator;
}

// 컴파일 타임 보장 (현재 방식)
public static int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value; // 검증 불필요!
}
```

컴파일러가 `Denominator` 타입을 요구하므로, `int` 값을 직접 전달할 수 없습니다. 유효하지 않은 값이 함수에 도달할 가능성 자체가 사라집니다.

### 도메인 주도 설계(DDD) 관점
- **도메인 개념의 명확한 표현**: `int` 대신 `Denominator` 사용
- **비즈니스 규칙의 타입 시스템 반영**: 0이 아닌 정수를 타입으로 표현
- **의도 명확화**: 함수 시그니처만으로도 안전성 보장
- **도메인 전문가와의 소통**: 비즈니스 언어를 코드로 표현

함수 시그니처에 `Denominator`가 드러나면, 코드를 읽는 사람은 별도의 문서 없이도 "이 매개변수는 0이 아닌 정수여야 한다"는 규칙을 즉시 파악할 수 있습니다.

## 실전 지침

### 예상 출력
```
=== 항상 유효한 타입 ===

유효한 값: AlwaysValid.ValueObjects.Denominator
잘못된 값: 오류: 0은 허용되지 않습니다
나눗셈 함수 테스트:
10 / 5 = 2
```

### 값 객체 사용 패턴

값 객체를 생성한 뒤 안전한 함수를 호출하는 기본 흐름입니다.

```csharp
// 1. 값 객체 생성 (유효성 검증 포함)
var denominatorResult = Denominator.Create(5);
var denominator = denominatorResult.Match(
    Succ: value => value,
    Fail: error => throw new Exception($"유효하지 않은 분모: {error.Message}")
);

// 2. 안전한 함수 호출 (검증 불필요)
var result = MathOperations.Divide(10, denominator);
Console.WriteLine($"결과: {result}"); // 항상 안전!
```

## 프로젝트 설명

### 프로젝트 구조
```
AlwaysValid/                          # 메인 프로젝트
├── Program.cs                        # 메인 실행 파일
├── MathOperations.cs                 # 안전한 나눗셈 함수
├── ValueObjects/                     # 값 객체 디렉토리
│   └── Denominator.cs                # 분모 값 객체
├── AlwaysValid.csproj                # 프로젝트 파일
└── README.md                         # 프로젝트 설명
```

### 핵심 코드

#### Denominator.cs (값 객체)
```csharp
using LanguageExt;
using LanguageExt.Common;

namespace AlwaysValid.ValueObjects;

/// <summary>
/// 0이 아닌 정수를 표현하는 분모 값 객체
/// 생성 시점에 유효성 검사를 수행하여 항상 유효한 값만 보장합니다.
/// </summary>
public sealed class Denominator
{
    private readonly int _value;

    // Private constructor - 직접 인스턴스 생성 방지
    private Denominator(int value) =>
        _value = value;

    /// <summary>
    /// Denominator를 생성합니다. 0인 경우 실패를 반환합니다.
    /// </summary>
    /// <param name="value">0이 아닌 정수 값</param>
    /// <returns>성공 시 Denominator, 실패 시 Error</returns>
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("0은 허용되지 않습니다");

        return new Denominator(value);
    }

    /// <summary>
    /// 내부 값을 안전하게 반환합니다.
    /// </summary>
    public int Value =>
        _value;
}
```

#### MathOperations.cs (안전한 함수)
```csharp
using AlwaysValid.ValueObjects;

namespace AlwaysValid;

public static class MathOperations
{
    /// <summary>
    /// 값 객체를 사용한 안전한 나눗셈 함수
    /// denominator는 항상 유효한 Denominator이므로 검증이 불필요합니다.
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모 (항상 0이 아님을 보장)</param>
    /// <returns>나눗셈 결과</returns>
    public static int Divide(int numerator, Denominator denominator)
    {
        // 검증 불필요! 항상 유효함!
        return numerator / denominator.Value;
    }
}
```

#### Program.cs (사용 예시)
```csharp
using AlwaysValid.ValueObjects;

namespace AlwaysValid;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 항상 유효한 타입 테스트 ===\n");

        // Denominator 생성 테스트
        Console.WriteLine("Denominator 생성 케이스:");

        var validResult = Denominator.Create(5);
        validResult.Match(
            Succ: value => Console.WriteLine($"유효한 값: {value}"),
            Fail: error => Console.WriteLine($"오류: {error.Message}")
        );

        var invalidResult = Denominator.Create(0);
        invalidResult.Match(
            Succ: value => Console.WriteLine($"유효한 값: {value}"),
            Fail: error => Console.WriteLine($"잘못된 값: 오류: {error.Message}")
        );

        Console.WriteLine();

        // 나눗셈 함수 테스트
        Console.WriteLine("나눗셈 함수 테스트:");
        var denominator = Denominator.Create(5);
        var result = MathOperations.Divide(10, (Denominator)denominator);
        Console.WriteLine($"10 / 5 = {result}");
    }
}
```

### 값 객체 패턴의 핵심 요소

네 가지 요소가 결합되어 항상 유효한 타입을 구성합니다.

```csharp
// 1. Private 생성자
private Denominator(int value) => _value = value;

// 2. 정적 팩토리 메서드
public static Fin<Denominator> Create(int value)
{
    if (value == 0)
        return Error.New("0은 허용되지 않습니다");

    return new Denominator(value);
}

// 3. 불변성 보장
public int Value => _value; // 읽기 전용 프로퍼티

// 4. 도메인 개념 표현
public static int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value; // 검증 불필요!
}
```

## 한눈에 보는 정리

### 값 객체의 장단점

값 객체 도입 시 얻는 이점과 감수해야 할 비용을 정리합니다.

| 장점 | 단점 |
|------|------|
| **컴파일 타임 보장** | **추가적인 타입 정의 필요** |
| **검증 불필요** | **초기 학습 곡선** |
| **도메인 표현** | **간단한 경우 과도한 복잡성** |
| **타입 안전성** | **메모리 오버헤드** |
| **의도 명확화** | **리팩토링 비용** |

### 발전 과정 비교

1장부터 4장까지 검증 방식이 어떻게 발전해왔는지 보여줍니다.

| 단계 | 접근법 | 검증 시점 | 안전성 | 복잡성 |
|------|--------|-----------|--------|--------|
| **01-Basic-Divide** | 기본 함수 | 런타임 예외 | 낮음 | 낮음 |
| **02-Exception-Handling** | 방어적 프로그래밍 | 런타임 검증 | 중간 | 중간 |
| **03-Functional-Result** | 함수형 결과 타입 | 런타임 검증 | 높음 | 높음 |
| **04-Always-Valid** | 값 객체 | 컴파일 타임 | 최고 | 최고 |

### 도메인 개념의 타입 표현

분모 외에도 다양한 도메인 개념을 값 객체로 표현할 수 있습니다.

| 도메인 개념 | 기본 타입 | 값 객체 | 비즈니스 규칙 |
|-------------|-----------|---------|---------------|
| **분모** | `int` | `Denominator` | 0이 아님 |
| **이메일** | `string` | `EmailAddress` | 유효한 형식 |
| **나이** | `int` | `Age` | 0 이상 150 이하 |
| **금액** | `decimal` | `Money` | 양수, 소수점 2자리 |

## FAQ

### Q1: 값 객체가 함수형 결과 타입보다 좋은 이유가 뭔가요?
**A**: 함수형 결과 타입은 여전히 런타임에 매번 검증해야 하고, 개발자가 검증을 누락할 수 있습니다. 반면 값 객체는 생성 단계에서 유효성을 확보하므로, 이후 사용하는 모든 함수에서 검증 로직이 불필요합니다. 컴파일러가 타입 수준에서 안전성을 강제하기 때문입니다.

### Q2: 언제 값 객체를 사용해야 하나요?
**A**: "이 값에 비즈니스 규칙이 있는가?"를 기준으로 판단합니다. 이메일 주소, 나이, 금액처럼 특정 형식이나 범위 제약이 있는 값은 값 객체로 표현하는 것이 적절합니다. 반면 단순한 계산 결과나 임시 데이터처럼 비즈니스 규칙이 없는 경우에는 기본 타입을 사용하는 것이 낫습니다.

### Q3: 값 객체와 함수형 결과 타입을 함께 사용하는 이유가 뭔가요?
**A**: 생성 시점에는 `Fin<T>`로 유효성 검증 결과를 명시적으로 표현하고, 사용 시점에는 이미 유효한 값 객체이므로 검증이 불필요합니다. 이 조합 덕분에 생성 단계에서는 실패를 안전하게 처리하면서, 이후 비즈니스 로직에서는 검증 없이 값을 신뢰할 수 있습니다.

---

값 객체를 통해 컴파일 타임 유효성을 확보했지만, `denominator.Value`처럼 내부 값을 꺼내야 하는 불편함이 남아 있습니다. 다음 장에서는 **연산자 오버로딩**을 도입하여 `15 / denominator`와 같은 자연스러운 수학적 표현을 구현합니다.
