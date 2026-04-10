---
title: "LINQ 표현식"
---
## 개요

두 개의 `Fin<T>` 값을 조합하려면 `Match`를 중첩해야 하고, 단계가 늘어날수록 코드는 오른쪽으로 깊어집니다. LINQ 표현식의 `from`/`select` 구문을 사용하면 중첩된 `Match` 체인을 평탄하게 펼치면서 에러를 자동으로 전파할 수 있습니다.

## 학습 목표

이 장을 마치면 다음을 할 수 있습니다.

- `from` 키워드를 사용하여 **`Fin<T>` 타입의 체이닝 연산을 단순화할 수 있습니다.**
- 명시적 연산자 오버로딩으로 `Denominator` 타입 간의 **다양한 연산을 구현할 수 있습니다.**
- LINQ 표현식을 통해 **복합 연산에서 에러 전파를 자동화할 수 있습니다.**

### **실습을 통해 확인할 내용**
- **LINQ 표현식 활용**: `from` 키워드로 복잡한 `Match` 체인 단순화
- **연산자 오버로딩 강화**: `Denominator` 타입 간의 다양한 연산자 지원
- **에러 처리 개선**: 암시적 변환 없이도 안전한 타입 변환과 연산 수행

## 왜 필요한가?

이전 단계인 `05-Operator-Overloading`에서는 연산자 오버로딩을 통해 자연스러운 수학 연산을 구현했습니다. 하지만 실제로 복잡한 연산을 수행하려고 할 때 몇 가지 문제가 발생했습니다.

두 개의 분모를 사용해서 연쇄 연산을 수행하려면 `Match` 메서드를 중첩해야 합니다. 단계가 늘어날수록 코드가 안쪽으로 계속 깊어지면서 가독성이 급격히 떨어집니다. 각 단계에서 성공/실패를 확인하고 에러를 다음 단계로 전달하는 과정도 번거롭고 실수하기 쉬우며, 정작 수행하려는 연산의 의도가 중첩된 `Match` 호출에 묻혀버립니다.

**LINQ 표현식**을 도입하면 이 모든 문제가 해결됩니다. `from`/`select` 구문으로 코드를 평탄하게 작성하면서, 에러 전파는 프레임워크가 자동으로 처리합니다.

## 핵심 개념

### LINQ 표현식을 통한 함수형 에러 처리

LINQ 표현식은 **모나드 체이닝(Monadic Chaining)을** 구현하는 C#의 기능입니다. `from` 키워드를 사용해서 여러 단계의 연산을 체이닝하면, 각 단계의 성공/실패 처리가 자동으로 이루어집니다.

이전 방식의 중첩 `Match`와 LINQ 표현식을 비교합니다.

```csharp
// 이전 방식 (Match 체인) - 복잡하고 읽기 어려움
var result = Denominator.Create(5).Match(
    Succ: denom => Denominator.Create(3).Match(
        Succ: denom2 => denom / denom2,
        Fail: error => error
    ),
    Fail: error => error
);

// 개선된 방식 (LINQ 표현식) - 직관적이고 읽기 쉬움
var result = from denom in Denominator.Create(5)
             from denom2 in Denominator.Create(3)
             select denom / denom2;
```

LINQ 표현식에서는 "5로 분모를 만들고, 3으로 또 다른 분모를 만들고, 그 둘을 나누어라"라는 의도가 코드에 그대로 드러납니다. 이것이 **선언적 프로그래밍(Declarative Programming)의** 핵심입니다.

### 연산자 오버로딩을 통한 명시적 타입 변환

이전에는 암시적 변환에 의존해서 타입 안전성 문제가 발생할 수 있었습니다. 이번에는 `Denominator` 타입끼리도 연산할 수 있도록 필요한 연산자를 명시적으로 정의합니다.

```csharp
// Denominator 간의 연산자 오버로딩
public static int operator /(Denominator numerator, Denominator denominator) =>
    numerator._value / denominator._value;
```

### 에러 전파 자동화

LINQ 표현식에서 에러가 발생하면 나머지 단계를 건너뛰고 자동으로 최종 결과에 반영됩니다. 개발자가 각 단계마다 에러를 확인하고 전달할 필요가 없습니다.

예를 들어, 세 단계의 연산 중 두 번째에서 에러가 발생하면 세 번째 단계는 실행되지 않고, 두 번째 단계의 에러가 최종 결과로 전파됩니다. 이 덕분에 코드가 훨씬 단순해지고 에러 처리 로직의 반복이 사라집니다.

## 실전 지침

### 예상 출력
```
=== LINQ 표현식을 통한 코드 단순화 ===

1. 핵심 개선사항: LINQ 표현식을 통한 단순화
  Before (05-Operator-Overloading): Match 사용
  After  (06-Linq-Expression): from 키워드 사용
  15 / 5 = 3 (LINQ 표현식)

2. 복합 연산에서의 LINQ 표현식 활용:
  (10 / 5) * 2 = 1

3. 변환 연산자와 LINQ 표현식:
    변환 성공: LinqExpression.ValueObjects.Denominator
    변환 실패: 0은 허용되지 않습니다

4. 에러 처리:
  LINQ 표현식을 통한 에러 처리:
    에러: 0은 허용되지 않습니다
    연쇄 연산 에러: 0은 허용되지 않습니다
```

### 핵심 구현 포인트
1. **LINQ 표현식 구문**: `from` 키워드를 사용한 모나딕 연산 체이닝
2. **연산자 오버로딩 확장**: `Denominator` 타입 간의 다양한 연산자 구현 (int/Denominator, Denominator/Denominator)
3. **에러 처리 패턴**: `Match` 메서드를 통한 성공/실패 케이스 처리
4. **명시적 변환 연산자**: `explicit operator`를 통한 안전한 타입 변환
5. **테스트 기반 구현**: `LinqExpressionBasicTests`와 `LinqExpressionAdvancedTests`를 통한 기능 검증

## 프로젝트 설명

### 프로젝트 구조
```
LinqExpression/                         # 메인 프로젝트
├── Program.cs                          # 메인 실행 파일
├── MathOperations.cs                   # 수학 연산 클래스 (LINQ 표현식 활용)
├── ValueObjects/                       # 값 객체 디렉토리
│   └── Denominator.cs                  # 분모 값 객체 (연산자 오버로딩 포함)
├── LinqExpression.csproj               # 프로젝트 파일
└── README.md                           # 메인 문서
```

### 핵심 코드

#### Denominator 클래스의 연산자 오버로딩
```csharp
public sealed class Denominator
{
    private readonly int _value;

    // Private constructor - 직접 인스턴스 생성 방지
    private Denominator(int value) => _value = value;

    /// <summary>
    /// Denominator를 생성합니다. 0인 경우 실패를 반환합니다.
    /// </summary>
    public static Fin<Denominator> Create(int value)
    {
        if (value == 0)
            return Error.New("0은 허용되지 않습니다");
        return new Denominator(value);
    }

    // 기본 연산자들
    public static int operator /(int numerator, Denominator denominator) =>
        numerator / denominator._value;

    public static int operator /(Denominator denominator, int divisor) =>
        denominator._value / divisor;

    public static int operator /(Denominator numerator, Denominator denominator) =>
        numerator._value / denominator._value;

    // 변환 연산자
    public static explicit operator Denominator(int value) =>
        Create(value).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidCastException("0은 Denominator로 변환할 수 없습니다")
        );
}
```

#### LINQ 표현식을 통한 복합 연산

단일 연산부터 복합 연산, 에러 처리까지 LINQ 표현식의 활용 범위를 보여줍니다.

```csharp
// LINQ 표현식을 사용한 자연스러운 나눗셈 연산
var result = from denominator in Denominator.Create(5)
             select MathOperations.Divide(15, denominator);

// 복합 연산에서의 LINQ 표현식 활용
var complexResult = from a in Denominator.Create(10)
                    from b in Denominator.Create(5)
                    from c in Denominator.Create(2)
                    select a / b / c;

// 변환 연산자와 LINQ 표현식
var successResult = from value in Denominator.Create(15)
                    select $"변환 성공: {value}";

var failureResult = from value in Denominator.Create(0)
                    select $"변환 성공: {value}";
```

## 한눈에 보는 정리

### 비교 표

이전 단계와 현재 단계의 에러 처리 방식과 코드 구조 차이를 정리합니다.

| 구분 | 이전 방식 (05-Operator-Overloading) | 현재 방식 (06-Linq-Expression) |
|------|-------------------------------------|--------------------------------|
| **에러 처리** | 중첩된 Match 체인 | LINQ 표현식을 통한 단순화 |
| **코드 가독성** | 복잡한 중첩 구조 | 직관적인 from-select 구문 |
| **에러 전파** | 명시적 에러 처리 | 자동 에러 전파 |
| **연산자 지원** | 기본 연산자만 | 확장된 연산자 세트 (int/Denominator, Denominator/Denominator) |
| **타입 변환** | 암시적 변환 의존 | 명시적 연산자 오버로딩 |

### 장단점 표

LINQ 표현식 도입 시 얻는 이점과 주의할 점을 정리합니다.

| 장점 | 단점 |
|------|------|
| **코드 가독성 향상** | LINQ 표현식 학습 곡선 |
| **에러 처리 자동화** | 디버깅 시 추적 복잡성 |
| **타입 안전성 강화** | 연산자 오버로딩 복잡성 |
| **함수형 프로그래밍** | 기존 명령형 코드와의 차이 |

## FAQ

### Q1: LINQ 표현식과 Match 메서드 중 어떤 것을 사용해야 하나요?
**A**: 단일 `Fin<T>`에서 성공/실패를 분기 처리할 때는 `Match`가 적합합니다. 여러 `Fin<T>` 값을 조합하는 복합 연산에서는 LINQ 표현식이 중첩을 제거하고 에러 전파를 자동화하므로 더 간결합니다.

```csharp
// 단일 처리 - Match 사용
var result = Denominator.Create(5).Match(
    Succ: value => $"성공: {value}",
    Fail: error => $"실패: {error}"
);

// 복합 처리 - LINQ 표현식 사용
var result = from a in Denominator.Create(10)
             from b in Denominator.Create(5)
             select a / b;

// 실제 프로젝트에서 사용된 예시
var result = from denominator in Denominator.Create(5)
             select MathOperations.Divide(15, denominator);
```

### Q2: LINQ 표현식에서 에러가 발생하면 어떻게 처리하나요?
**A**: 체이닝 중간에 실패가 발생하면 나머지 단계를 건너뛰고 최종 `Fin<T>` 결과에 에러가 반영됩니다. 원본 에러 메시지는 그대로 보존되므로, 최종 결과를 `Match`로 처리하면 됩니다.

```csharp
// 실제 프로젝트에서 사용된 에러 처리 예시
var divisionResult = from ten in Denominator.Create(10)
                     from zero in Denominator.Create(0)  // 실패 발생
                     select ten / zero;

divisionResult.Match(
    Succ: value => Console.WriteLine($"결과: {value}"),
    Fail: error => Console.WriteLine($"에러: {error}")  // 에러 처리
);

// 연쇄 에러 처리 예시
var chainResult = from a in Denominator.Create(20)
                  from b in Denominator.Create(4)
                  from c in Denominator.Create(0)  // 실패 발생
                  select a / b / c;

chainResult.Match(
    Succ: value => Console.WriteLine($"연쇄 연산 결과: {value}"),
    Fail: error => Console.WriteLine($"연쇄 연산 에러: {error}")
);
```

### Q3: 연산자 오버로딩을 많이 추가하면 성능에 영향을 주나요?
**A**: 성능 영향은 미미합니다. 연산자 오버로딩은 컴파일러에 의해 메서드 호출로 변환되며, JIT 컴파일러가 인라인 최적화를 적용할 수 있으므로 일반 메서드 호출과 동일한 수준의 오버헤드만 발생합니다.

---

여기까지 Part 1의 기본 개념(유효성 보장, 연산자 오버로딩, LINQ 표현식)을 모두 다루었습니다. 이 세 가지가 결합되어 "생성 시점에 유효성을 확보하고, 자연스럽게 연산하며, 에러를 자동으로 전파하는" 값 객체의 기초가 완성되었습니다. 다음 장에서는 **값 동등성(Value Equality)을** 구현하여, 같은 값을 가진 두 객체를 동일하게 취급하는 방법을 학습합니다.

→ [7장: 값 동등성](../07-Value-Equality/)
