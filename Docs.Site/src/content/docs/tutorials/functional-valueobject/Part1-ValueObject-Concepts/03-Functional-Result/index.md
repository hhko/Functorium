---
title: "함수형 결과 타입"
---
## 개요

이전 단계에서 방어적 프로그래밍의 두 가지 구현 방법을 비교했지만, 예외 기반 Divide는 프로그램 흐름을 중단시키고 TryDivide는 `out` 매개변수로 외부 상태를 변경하는 문제가 남아 있었습니다. 예외도 `out` 매개변수도 없이 성공과 실패를 표현할 수는 없을까요?

**함수형 결과 타입(Functional Result Type)은** 이 질문에 대한 답입니다. 함수의 반환 타입 자체에 성공/실패 정보를 담아, 부작용 없이 명시적으로 결과를 표현합니다.

## 학습 목표

- 함수형 결과 타입이 예외 기반 접근법의 어떤 한계를 해결하는지 설명할 수 있습니다.
- `Fin<T>` 타입과 Match 패턴을 사용하여 성공/실패를 명시적으로 처리할 수 있습니다.
- 순수 함수의 조건(동일 입력-동일 출력, 부작용 없음)을 이해하고 적용할 수 있습니다.

## 핵심 개념

### 함수형 결과 타입(Functional Result Type)

방어적 프로그래밍에서 남아 있던 세 가지 문제를 함수형 결과 타입이 해결합니다. 사전 검증 후에도 `ArgumentException`을 발생시켜 프로그램 흐름을 중단시키는 것은 예상 가능한 도메인 규칙 위반을 예외적 상황으로 처리하는 것이므로 설계상 적절하지 않습니다. 예외를 발생시키는 함수는 부작용이 있어 순수하지 않으며, 호출자가 try-catch를 빠뜨리면 프로그램이 중단될 수 있습니다.

함수형 결과 타입은 **Either 타입**의 C# 구현체로, 성공과 실패를 모두 타입으로 표현합니다. 다음 코드는 예외 기반 방식과 함수형 결과 타입 방식의 차이를 보여줍니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 예외 발생으로 프로그램 중단
public int Divide(int x, int y)
{
    if (y == 0)
        throw new ArgumentException("0으로 나눌 수 없습니다");  // 예외 발생!

    return x / y;
}

// 개선된 방식 (함수형 결과 타입) - 명시적 성공/실패 표현
public Fin<int> Divide(int x, int y)
{
    if (y == 0)
        return Error.New("0은 허용되지 않습니다");  // 명시적 실패

    return x / y;  // 명시적 성공
}
```

개선된 방식에서는 함수가 예외 없이 안전하게 동작하고, 호출자가 성공/실패를 명시적으로 처리할 수 있습니다.

### `Fin<T>` 타입과 Match 패턴

`Fin<T>`는 LanguageExt 라이브러리에서 제공하는 결과 타입으로, 성공(`Succ`)과 실패(`Fail`) 두 가지 상태를 가집니다. Match 패턴을 사용하면 두 상태를 모두 처리하도록 강제되므로, 개발자가 실패 처리를 깜빡하는 실수를 방지합니다.

```csharp
// Fin<T> 타입 사용
var result = Divide(10, 0);  // Fin<int> 타입 반환

// Match 패턴으로 성공/실패 처리 (강제됨)
result.Match(
    Succ: value => Console.WriteLine($"결과: {value}"),      // 성공 처리
    Fail: error => Console.WriteLine($"오류: {error.Message}") // 실패 처리
);
```

try-catch와 달리 Match 패턴은 컴파일 타임에 성공/실패 처리를 강제합니다. 이를 통해 타입 안전성을 확보하고 런타임 오류를 줄일 수 있습니다.

### 순수 함수의 완성

순수 함수는 동일한 입력에 대해 항상 동일한 출력을 반환하고, 부작용이 없어야 합니다. 예외를 발생시키는 함수는 이 조건을 위반하지만, 함수형 결과 타입을 반환하는 함수는 순수 함수의 조건을 충족합니다.

```csharp
// 예외 기반 함수 (순수하지 않음) - 부작용 발생
public int Divide(int x, int y)
{
    if (y == 0)
        throw new ArgumentException("0으로 나눌 수 없습니다");  // 부작용!

    return x / y;
}

// 함수형 결과 타입 함수 (순수 함수) - 부작용 없음
public Fin<int> Divide(int x, int y)
{
    if (y == 0)
        return Error.New("0은 허용되지 않습니다");  // 부작용 없음

    return x / y;
}
```

순수 함수는 테스트하기 쉽고, 조합하기 쉬우며, **참조 투명성(Referential Transparency)을** 보장하여 함수 호출을 그 결과값으로 대체할 수 있습니다.

## 실전 지침

### 예상 출력
```
=== 함수형 결과 타입 ===

성공 케이스:
10 / 2 = 5

실패 케이스:
10 / 0 = 오류: 0은 허용되지 않습니다
```

### 핵심 구현 포인트
1. **LanguageExt 라이브러리 사용**: `using LanguageExt;` 및 `using LanguageExt.Common;`
2. **`Fin<T>` 반환 타입**: 성공/실패를 명시적으로 표현
3. **Error.New() 사용**: 실패 시 Error 객체 생성
4. **Match 패턴 활용**: 성공/실패를 명시적으로 처리

## 프로젝트 설명

### 프로젝트 구조
```
FunctionalResult/                       # 메인 프로젝트
├── Program.cs                          # 메인 실행 파일
├── MathOperations.cs                   # 함수형 결과 타입 함수 구현
├── FunctionalResult.csproj             # 프로젝트 파일
└── README.md                           # 메인 문서
```

### 핵심 코드

#### MathOperations.cs
```csharp
using LanguageExt;
using LanguageExt.Common;

namespace FunctionalResult;

public static class MathOperations
{
    /// <summary>
    /// 함수형 결과 타입을 사용한 나눗셈 함수
    /// 성공 시 Fin<int>.Succ(결과), 실패 시 Fin<int>.Fail(오류)를 반환합니다.
    /// </summary>
    /// <param name="numerator">분자</param>
    /// <param name="denominator">분모</param>
    /// <returns>성공/실패를 명시적으로 표현하는 Fin<int> 타입</returns>
    public static Fin<int> Divide(int numerator, int denominator)
    {
        if (denominator == 0)
            return Error.New("0은 허용되지 않습니다");

        return numerator / denominator;
    }
}
```

#### Program.cs
```csharp
namespace FunctionalResult;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 함수형 결과 타입 테스트 ===\n");

        // 성공 케이스
        Console.WriteLine("성공 케이스:");
        var successResult = MathOperations.Divide(10, 2);
        successResult.Match(
            Succ: value => Console.WriteLine($"10 / 2 = {value}"),
            Fail: error => Console.WriteLine($"오류: {error.Message}")
        );

        Console.WriteLine();

        // 실패 케이스
        Console.WriteLine("실패 케이스:");
        var failureResult = MathOperations.Divide(10, 0);
        failureResult.Match(
            Succ: value => Console.WriteLine($"10 / 0 = {value}"),
            Fail: error => Console.WriteLine($"10 / 0 = 오류: {error.Message}")
        );
    }
}
```

### 주요 패키지
- **LanguageExt.Core**: 함수형 프로그래밍 라이브러리
  - `Fin<T>`: 성공/실패를 표현하는 결과 타입
  - `Error`: 오류 정보를 담는 타입
  - `Match`: 패턴 매칭을 통한 결과 처리

## 한눈에 보는 정리

### 예외 기반 vs 함수형 결과 타입 비교

다음 표는 예외 기반 접근법과 함수형 결과 타입의 특성을 항목별로 비교합니다.

| 구분 | 예외 기반 | 함수형 결과 타입 |
|------|-----------|------------------|
| **성공/실패 표현** | 함수 시그니처에 불명확 | 함수 시그니처에 명확 |
| **처리 강제성** | 선택적 (try-catch) | 필수적 (Match) |
| **부작용** | 있음 (예외 발생) | 없음 |
| **예측 가능성** | 낮음 (예외 발생 가능) | 높음 (항상 값 반환) |
| **타입 안전성** | 낮음 (런타임 예외) | 높음 (컴파일 타임 검증) |

### 개선 방향
1. **값 객체 도입**: "0이 아닌 정수"를 표현하는 도메인 타입 생성
2. **타입 안전성 확보**: 컴파일 타임에 유효성 검증
3. **도메인 중심 설계**: 비즈니스 규칙을 타입으로 표현

## FAQ

### Q1: 모든 함수를 함수형 결과 타입으로 바꿔야 하나요?
**A**: 아닙니다. 0으로 나누기나 입력값 검증처럼 **예측 가능한 도메인 규칙 위반**에는 함수형 결과 타입을 사용하고, 네트워크 장애나 메모리 부족처럼 **예측 불가능한 시스템 오류**에는 예외를 사용합니다. 도메인 계층에서는 결과 타입을, 인프라 계층에서는 예외를 사용하는 것이 일반적인 구분입니다.

### Q2: `Fin<T>` 타입은 무엇인가요?
**A**: LanguageExt 라이브러리에서 제공하는 함수형 결과 타입으로, `Succ(T value)`와 `Fail(Error error)` 두 가지 상태를 가집니다. Match 메서드로 두 상태를 모두 처리하도록 강제하며, 불변이고 다른 함수형 타입과 조합하여 사용할 수 있습니다.

### Q3: LanguageExt 없이도 사용할 수 있나요?
**A**: 자체 결과 타입을 구현할 수 있습니다. 아래와 같이 간단한 `Result<T>`를 만들 수 있으며, 학습이나 작은 프로젝트에 적합합니다. 다만 프로덕션에서는 검증된 LanguageExt 사용을 권장합니다.

```csharp
public class Result<T>
{
    private readonly T? _value;
    private readonly string? _error;
    private readonly bool _isSucc;

    private Result(T value)
    {
        _value = value;
        _isSucc= true;
    }

    private Result(string error)
    {
        _error = error;
        _isSucc = false;
    }

    public static Result<T> Succ(T value) => new Result<T>(value);
    public static Result<T> Fail(string error) => new Result<T>(error);

    public R Match<R>(Func<T, R> onSucc, Func<string, R> onFail)
    {
        return _isSucc ? onSucc(_value!) : onFail(_error!);
    }
}
```

---

함수형 결과 타입은 예외와 `out` 매개변수의 부작용을 모두 제거하고, 성공과 실패를 타입으로 명시적으로 표현하는 방법을 제시했습니다. 그러나 `Divide(10, 0)` 호출 자체를 컴파일 타임에 막을 수는 없었습니다. 다음 장 **항상 유효한 타입**에서는 "0이 아닌 정수"를 도메인 타입으로 정의하여, 잘못된 입력이 함수에 전달되는 것 자체를 타입 시스템으로 차단하는 방법을 살펴봅니다.

→ [4장: 항상 유효한 값 객체](../04-Always-Valid/)
