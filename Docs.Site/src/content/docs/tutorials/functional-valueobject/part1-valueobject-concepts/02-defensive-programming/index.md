---
title: "방어적 프로그래밍"
---
## 개요

`01-Basic-Divide`에서 기본 나눗셈 함수가 0으로 나누기를 시도할 때 `DivideByZeroException`으로 프로그램이 중단되는 문제를 확인했습니다. 그런데 "0으로 나누기"는 진짜 예외적인 상황일까요? 수학적으로 분모가 0이 될 수 없다는 것은 **예상 가능한 도메인 규칙**입니다. 예상 가능한 실패를 예외로 처리하는 것은 설계상 적절하지 않습니다.

이 단계에서는 방어적 프로그래밍의 두 가지 구현 방법을 비교합니다.

1. **사전 검증을 통한 정의된(의도된) 예외 Divide**: 예외를 더 명확하고 의도적으로 만드는 방법
2. **사전 검증을 통한 예외 없이 bool 반환을 활용한 TryDivide**: 예외 없이도 안전한 실패 처리를 하는 방법

## 학습 목표

- 사전 검증을 통한 정의된 예외 처리와 Try 패턴의 차이를 설명할 수 있습니다.
- 예외 기반 방식과 Try 패턴의 장단점을 비교하고, 상황에 맞는 방법을 선택할 수 있습니다.
- Try 패턴의 `out` 매개변수가 함수형 프로그래밍 관점에서 여전히 부작용임을 인식할 수 있습니다.
- .NET Framework의 TryParse, TryGetValue 등 실무 표준 패턴과의 연관성을 이해할 수 있습니다.

## 핵심 개념

### 사전 검증을 통한 정의된(의도된) 예외 처리

입력값을 사전에 검증하고, 유효하지 않은 경우 명확한 예외를 발생시키는 방법입니다. 시스템이 던지는 `DivideByZeroException` 대신, 개발자가 의도한 `ArgumentException`을 사용하여 오류 메시지와 디버깅 정보를 명확히 합니다.

```csharp
// 방어적 프로그래밍 - 사전 검증을 통한 정의된 예외
public static int Divide(int numerator, int denominator)
{
    if (denominator == 0)
        throw new ArgumentException("0으로 나눌 수 없습니다");

    return numerator / denominator;
}
```

명확한 오류 메시지와 스택 트레이스를 제공하지만, 여전히 예외를 발생시켜 프로그램 흐름을 중단시킵니다. 호출자는 반드시 try-catch 블록을 사용해야 하며, 이는 **부작용(Side Effect)에** 해당합니다.

### TryDivide 패턴 (Try Pattern)

예외를 발생시키지 않고 성공/실패를 `bool` 반환값으로 표현하는 방법입니다. .NET Framework의 `int.TryParse`, `Dictionary.TryGetValue` 등에서 널리 사용되는 표준 패턴과 동일한 방식입니다.

```csharp
// 방어적 프로그래밍 - 예외 없이 bool 반환
public static bool TryDivide(int numerator, int denominator, out int result)
{
    if (denominator == 0)
    {
        result = default;
        return false;
    }

    result = numerator / denominator;
    return true;
}
```

예외 처리 오버헤드가 없어 성능이 향상되고, 실패 시에도 프로그램이 중단되지 않습니다. 그러나 `out` 매개변수를 통해 함수 외부 상태를 변경하는 **부작용이 여전히 존재**합니다. 또한 성공/실패 여부만 알 수 있을 뿐 구체적인 오류 정보가 없고, 여러 Try 패턴을 조합하면 중첩된 if 문으로 코드가 복잡해집니다.

### 두 방법의 공통적 한계

두 방법 모두 부작용을 가지고 있다는 점이 중요합니다. 예외 기반 Divide는 프로그램 흐름을 중단시키는 부작용을, TryDivide는 `out` 매개변수로 외부 상태를 변경하는 부작용을 가집니다. 부작용의 대상만 변경되었을 뿐, 근본적인 문제는 여전히 남아 있습니다.

다음 표는 두 방법의 특성을 항목별로 비교합니다.

| 구분 | 예외 기반 Divide | Try 패턴 TryDivide |
|------|------------------------|----------------|
| **방식** | 사전 검증 후 정의된 예외 발생 | 사전 검증 후 bool 반환 |
| **성공 시** | 결과값 직접 반환 | `true` 반환, `out` 매개변수에 결과 |
| **실패 시** | `ArgumentException` 발생 | `false` 반환, `out` 매개변수는 기본값 |
| **예외 처리** | try-catch 블록 필요 | 불필요 |
| **성능** | 예외 처리 오버헤드 존재 | 빠름 (예외 오버헤드 없음) |
| **부작용** | 프로그램 흐름 중단 | 외부 상태 변경 (`out`) |
| **오류 정보** | 상세한 예외 메시지, 스택 트레이스 | 성공/실패 여부만 |

### 실무 표준 패턴과의 연관성

Try 패턴은 .NET Framework 전반에서 사용되는 표준 패턴입니다. `int.TryParse`, `Dictionary.TryGetValue`, `ConcurrentDictionary.TryAdd`, `ConcurrentDictionary.TryRemove` 등이 모두 동일한 원칙을 따릅니다. 성공 시 `true`를 반환하고 결과를 `out` 매개변수로 전달하며, 실패 시 `false`를 반환하고 예외를 발생시키지 않습니다.

## 실전 지침

### 예상 출력
```
=== 방어적 프로그래밍의 두 가지 구현 방법 ===

=== 10 / 2 계산 시도 ===

방법 1: 예외 기반 Divide
 성공: 5

방법 2: Try 패턴 TryDivide
 성공: 5

=== 10 / 0 계산 시도 ===

방법 1: 예외 기반 Divide
 실패: 0으로 나눌 수 없습니다 (Parameter 'denominator') (프로그램 흐름 중단 부작용)

방법 2: Try 패턴 TryDivide
 실패: 계산할 수 없음 (외부 상태 변경 부작용: result = 0)
```

## 프로젝트 설명

### 프로젝트 구조
```
DefensiveProgramming/                          # 메인 프로젝트
├── Program.cs                                  # 메인 실행 파일
├── MathOperations.cs                           # TryDivide 패턴 구현
├── DefensiveProgramming.csproj                 # 프로젝트 파일
└── README.md                                   # 메인 문서
```

### 핵심 코드

#### MathOperations.cs
```csharp
namespace DefensiveProgramming;

public static class MathOperations
{
    /// <summary>
    /// TryDivide 패턴을 사용한 방어적 프로그래밍 나눗셈 함수
    /// denominator가 0일 경우 false를 반환하고 result는 기본값을 가집니다.
    /// </summary>
    public static bool TryDivide(int numerator, int denominator, out int result)
    {
        // denominator가 0일 경우 false 반환 (예외 발생 없음!)
        if (denominator == 0)
        {
            result = default; // 기본값 설정
            return false;     // 실패를 명시적으로 반환
        }

        result = numerator / denominator;
        return true;          // 성공을 명시적으로 반환
    }

    /// <summary>
    /// 기존 Divide 메서드 (하위 호환성을 위해 유지)
    /// denominator가 0일 경우 ArgumentException을 발생시킵니다.
    /// </summary>
    public static int Divide(int numerator, int denominator)
    {
        if (denominator == 0)
            throw new ArgumentException("0으로 나눌 수 없습니다");

        return numerator / denominator;
    }
}
```

#### Program.cs
```csharp
namespace DefensiveProgramming;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 방어적 프로그래밍 TryDivide 패턴 테스트 ===\n");

        // TryDivide 패턴 사용 (권장 방식)
        Console.WriteLine("1. TryDivide 패턴 사용 (권장):");
        DemonstrateTryDividePattern();

        Console.WriteLine();

        // 기존 Divide 메서드와 비교
        Console.WriteLine("2. 기존 Divide 메서드와 비교:");
        DemonstrateTraditionalDivideMethod();

        Console.WriteLine();

        // 방어적 프로그래밍의 장점 시연
        Console.WriteLine("3. 방어적 프로그래밍의 장점:");
        DemonstrateDefensiveProgrammingBenefits();
    }

    static void DemonstrateTryDividePattern()
    {
        // 정상 케이스
        if (MathOperations.TryDivide(10, 2, out int result1))
        {
            Console.WriteLine($"✓ 10 / 2 = {result1} (성공)");
        }
        else
        {
            Console.WriteLine("✗ 10 / 2 = 실패");
        }

        // 예외 케이스 (예외 발생 없음!)
        if (MathOperations.TryDivide(10, 0, out int result2))
        {
            Console.WriteLine($"✓ 10 / 0 = {result2} (성공)");
        }
        else
        {
            Console.WriteLine("✗ 10 / 0 = 실패 (예외 없이 안전하게 처리됨)");
        }
    }
}
```

### Try 패턴 사용법
```csharp
// 1. 기본 사용법
if (MathOperations.TryDivide(10, 2, out int result))
{
    Console.WriteLine($"결과: {result}");  // 성공 시
}
else
{
    Console.WriteLine("계산할 수 없음");   // 실패 시
}

// 2. 변수 선언과 함께 사용
int result;
if (MathOperations.TryDivide(10, 0, out result))
{
    Console.WriteLine($"결과: {result}");
}
else
{
    Console.WriteLine($"실패, result 값: {result}");  // default(int) = 0
}

// 3. 무시하고 싶은 경우
if (MathOperations.TryDivide(10, 2, out _))  // _ 사용
{
    Console.WriteLine("계산 성공");
}
else
{
    Console.WriteLine("계산 실패");
}
```

## 한눈에 보는 정리

### Try 패턴의 핵심 원칙
1. **성공 시**: `true` 반환, 결과값을 `out` 매개변수로 반환
2. **실패 시**: `false` 반환, `out` 매개변수는 기본값 또는 의미 없는 값
3. **예외 없음**: 어떤 경우에도 예외를 발생시키지 않음
4. **명시적 처리**: 호출자가 성공/실패를 명시적으로 처리해야 함

### 각 방법의 한계점과 다음 단계

다음 표는 두 방법의 한계점과 함수형 결과 타입으로의 개선 방향을 정리합니다.

| 구분 | 예외 기반 Divide | Try 패턴 TryDivide |
|------|------------------------|----------------|
| **한계점** | 예외 발생으로 프로그램 흐름 중단 (부작용) | 타입 안전성 부족, `out` 매개변수를 통한 부작용 존재 |
| **부작용 유형** | 프로그램 흐름 중단 부작용 | 외부 상태 변경 부작용 |
| **개선 방향** | 예외 없이 안전한 실패 처리 | 타입 안전성과 완전한 순수성 확보 |
| **다음 단계** | Functional Result로 자연스럽게 연결 | Functional Result 타입으로 모든 부작용 해결 |

### Try 패턴의 5가지 한계점

다음 표는 Try 패턴의 한계와 함수형 프로그래밍이 제시하는 해결 방향을 대비합니다.

| 한계점 | 문제 | 함수형 해결책 |
|--------|------|---------------|
| **타입 안전성 부족** | 런타임 검증으로 컴파일 타임 검증 불가 | 도메인 특화 타입으로 컴파일 타임 차단 |
| **부작용 존재** | `out` 매개변수로 외부 상태 변경 | 불변 객체 반환으로 모든 부작용 해결 |
| **명시적 오류 처리 부족** | 불린 반환값만으로 구체적 오류 정보 부족 | 결과 타입으로 구체적 오류 정보 제공 |
| **합성성 제한** | 중첩된 if 문으로 복잡성 증가 | 모나드 체이닝으로 간결한 합성 |
| **타입 시스템 활용 부족** | 기본 타입에 의존하여 도메인 특화 타입 활용 불가 | 강력한 타입 시스템으로 표현력 향상 |

## FAQ

### Q1: Try 패턴이 예외 기반 방식보다 항상 좋은가요?
**A**: 아닙니다. 0으로 나누기처럼 **예상 가능한 실패**에는 Try 패턴이 적합하지만, 파일 없음이나 네트워크 오류처럼 **예측 불가능한 시스템 오류**에는 예외 기반 방식이 적합합니다. 실패 빈도가 높고 프로그램 계속 실행이 필요한 경우 Try 패턴을, 복구 불가능한 심각한 오류에는 예외를 선택하세요.

### Q2: `out` 매개변수 대신 다른 방법은 없나요?
**A**: 튜플 반환 `(bool Success, int Result)`이나 결과 객체 반환 방식도 가능합니다. 하지만 `out` 매개변수 방식이 .NET Framework의 표준이며 메모리 할당이 최소화되어 성능상 유리합니다. 다음 단계에서는 이들 대안의 공통 방향인 **함수형 결과 타입(Functional Result)을** 살펴봅니다.

### Q3: Try 패턴의 근본적인 한계는 무엇인가요?
**A**: 예외 발생이라는 부작용은 제거했지만, `out` 매개변수를 통한 외부 상태 변경이라는 부작용은 여전히 존재합니다. 또한 `bool` 반환값만으로는 구체적인 오류 원인을 알 수 없고, 여러 Try 패턴을 조합하면 중첩된 if 문으로 복잡해집니다. 이러한 한계는 다음 단계의 함수형 결과 타입에서 해결합니다.

---

방어적 프로그래밍은 예외를 더 안전하게 다루는 방법을 제시했지만, 두 가지 구현 방법 모두 부작용이라는 근본적 한계를 벗어나지 못합니다. 다음 장 **함수형 결과 타입**에서는 예외도 `out` 매개변수도 없이, 성공과 실패를 하나의 타입으로 표현하는 방법을 살펴봅니다.

→ [3장: 함수형 결과 타입](../03-Functional-Result/)
