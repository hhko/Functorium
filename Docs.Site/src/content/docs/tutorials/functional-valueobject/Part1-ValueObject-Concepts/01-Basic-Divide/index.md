---
title: "기본 나눗셈"
---
## 개요

`10 / 0`을 실행하면 어떻게 될까요? `DivideByZeroException`이 발생하고 프로그램이 중단됩니다. 그런데 0으로 나눌 수 없다는 사실은 예외적인 상황이 아니라 누구나 아는 수학적 규칙입니다. 이 장에서는 **예상 가능한 실패를 예외로 처리하는 것이 왜 문제인지** 확인하고, 값 객체의 필요성을 이해합니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. 기본 나눗셈 함수에서 `DivideByZeroException`이 발생하는 문제점을 설명할 수 있습니다
2. 예외적인 상황과 예상 가능한 실패의 차이를 구분할 수 있습니다
3. 도메인 규칙이 `int` 타입에 표현되지 않는 한계를 인식할 수 있습니다

## 왜 필요한가?

다음 나눗셈 함수를 보세요.

```csharp
public int Divide(int numerator, int denominator)
{
    return numerator / denominator;
}
```

이 함수에는 두 가지 문제가 있습니다.

`denominator`에 0이 들어오면 `DivideByZeroException`이 발생합니다. 예외는 네트워크 오류나 메모리 부족 같은 예측 불가능한 상황을 위한 도구인데, "0으로 나눌 수 없다"는 예상 가능한 도메인 규칙입니다. 예외를 발생시키면 프로그램 흐름이 중단되는 부작용이 생기고, 이는 순수 함수의 원칙도 위반합니다.

또한 "0이 아닌 정수"라는 도메인 규칙이 `int` 타입에는 전혀 표현되지 않습니다. 코드를 읽는 사람은 이 함수가 0에 대해 어떻게 동작하는지 시그니처만으로는 알 수 없습니다.

이 문제들을 해결하려면 도메인 규칙을 타입으로 표현하는 값 객체가 필요합니다.

## 핵심 개념

### 순수 함수와 부작용

순수 함수는 **동일한 입력에 대해 항상 동일한 출력을 반환**하고, **부작용이 없는** 함수입니다. 예외를 발생시키면 프로그램 흐름을 중단시키는 부작용이 생기므로 순수 함수가 아닙니다.

```csharp
// 순수하지 않음 - 예외 발생이라는 부작용
public int Divide(int x, int y)
{
    // y가 0이면 예외 발생!
    return x / y;
}

// 순수 함수 - 항상 유효한 값으로 안전한 연산
public int Divide(int x, Denominator y)
{
    // y는 항상 유효함을 보장 (부작용 없음)
    return x / y.Value;
}
```

### 예외 vs 도메인 타입

예외는 시스템 레벨의 예측 불가능한 오류(네트워크 실패, 메모리 부족)에 사용해야 합니다. 사용자 입력 오류나 비즈니스 규칙 위반처럼 예상 가능한 실패는 도메인 타입으로 표현하는 것이 적절합니다.

```csharp
// 예외 사용 (부적절) - 예상 가능한 실패를 예외로 처리
var result = Divide(10, 0);  // 런타임에서 예외 발생!

// 도메인 타입 사용 (적절) - 컴파일 타임에 오류 방지
var result = Divide(10, new Denominator(2));  // 안전한 연산
```

타입 시스템이 제약을 강제하면 개발자가 규칙을 기억할 필요가 없어지고, 컴파일러가 잘못된 사용을 사전에 차단합니다.

## 실전 지침

### 예상 출력
```
=== 기본 나눗셈 함수 ===

정상 케이스:
10 / 2 = 5

예외 케이스:
10 / 0 = System.DivideByZeroException: Attempted to divide by zero.
   at BasicDivide.MathOperations.Divide(Int32 numerator, Int32 denominator) in ...\MathOperations.cs:line 16
   at BasicDivide.Program.DemonstrateExceptionalDivide() in ...\Program.cs:line 43
```

## 프로젝트 설명

### 프로젝트 구조
```
BasicDivide/                        # 메인 프로젝트
├── Program.cs                      # 메인 실행 파일
├── MathOperations.cs               # 나눗셈 함수 구현
├── BasicDivide.csproj              # 프로젝트 파일
└── README.md                       # 메인 문서
```

### 핵심 코드

#### MathOperations.cs
```csharp
public static class MathOperations
{
    // 문제가 있는 기본 나눗셈 함수
    public static int Divide(int numerator, int denominator)
    {
        // denominator가 0이면 예외 발생!
        return numerator / denominator;
    }
}
```

#### Program.cs
```csharp
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 기본 나눗셈 함수 테스트 ===\n");

        // 정상 케이스
        Console.WriteLine("정상 케이스:");
        try
        {
            var result = MathOperations.Divide(10, 2);
            Console.WriteLine($"10 / 2 = {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"예외 발생: {ex.Message}");
        }

        Console.WriteLine();

        // 예외 케이스
        Console.WriteLine("예외 케이스:");
        try
        {
            var result = MathOperations.Divide(10, 0);
            Console.WriteLine($"10 / 0 = {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"10 / 0 = {ex}");
        }
    }
}
```

## 한눈에 보는 정리

다음 표는 기본 나눗셈 함수의 문제점을 정리합니다.

### 기본 나눗셈 함수의 문제점
| 문제점 | 설명 | 영향 |
|--------|------|------|
| **예외 기반 오류 처리** | 예상 가능한 실패를 예외로 처리하여 프로그램 흐름 중단 | 프로그램 안정성 저하 |
| **부작용** | 예외를 발생시켜 프로그램 흐름을 중단 | 예측 불가능한 동작 |
| **호출자 책임** | 함수를 사용하는 쪽에서 예외 처리를 해야 함 | 사용 복잡성 증가 |
| **도메인 표현 부족** | 비즈니스 규칙이 코드에 드러나지 않음 | 코드 가독성 저하 |
| **타입 안전성 부족** | 컴파일 타임에 유효하지 않은 입력을 검증할 수 없음 | 런타임 오류 위험 |

다음 표는 예외와 도메인 타입의 차이를 비교합니다.

### 예외 vs 도메인 타입 비교
| 구분 | 예외 | 도메인 타입 |
|------|------------------|------------------|
| **발견 시점** | 런타임 | 컴파일 타임 |
| **디버깅 비용** | 높음 (실행 → 에러 → 분석 → 수정) | 낮음 (명시적 실패 처리) |
| **배포 위험** | 높음 (테스트에서 놓칠 수 있음) | 낮음 (예외로 인한 중단 없음) |
| **개발자 경험** | 나쁨 (갑작스러운 중단) | 좋음 (예측 가능한 처리) |

## FAQ

### Q1: 예외가 항상 나쁜 것은 아닌가요?
**A**: 예외는 **예외적인 상황**(파일 삭제, 네트워크 끊김, 메모리 부족)을 처리하기 위한 도구입니다. 0으로 나누기처럼 **예상 가능한 실패**는 예외보다 명시적인 처리가 더 적합합니다.

### Q2: 이 문제를 어떻게 해결하나요?
**A**: 다음 장들에서 단계적으로 해결합니다. 2장에서 방어적 프로그래밍으로 시작해, 3장에서 함수형 결과 타입을, 4장에서 값 객체를 도입합니다.

### Q3: 실제 프로젝트에서도 이런 문제가 자주 발생하나요?
**A**: 네. 이메일 주소, 나이, 금액 등 비즈니스 규칙이 있는 값을 `string`이나 `int`로만 다루면 동일한 문제가 발생합니다. 검증 코드가 여러 곳에 흩어지고, 어디선가 빠뜨리면 런타임 오류로 이어집니다.

---

다음 장에서는 이 문제를 해결하기 위한 첫 번째 시도로 **방어적 프로그래밍**을 살펴봅니다. 사전 검증으로 예외를 더 명확하게 만들 수 있지만, 근본적인 한계도 함께 확인합니다.

→ [2장: 방어적 프로그래밍](../02-Defensive-Programming/)
