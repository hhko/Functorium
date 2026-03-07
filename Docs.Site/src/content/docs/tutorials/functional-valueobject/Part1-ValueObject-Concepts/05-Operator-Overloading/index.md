---
title: "연산자 오버로딩"
---
## 개요

`numerator / denominator.Value` -- 값 객체를 도입했지만 `.Value` 속성을 매번 꺼내야 한다면, 도메인 언어와 코드 사이의 괴리가 여전히 남아 있습니다. 연산자 오버로딩을 활용하면 `.Value` 없이 `15 / denominator`와 같은 자연스러운 수학적 표현을 코드에서 직접 사용할 수 있습니다.

## 학습 목표

이 장을 마치면 다음을 할 수 있습니다.

- C#에서 사용자 정의 타입에 대한 **연산자 오버로딩을 구현할 수 있습니다.**
- 값 객체를 사용할 때 `.Value` 없이도 **자연스러운 연산이 가능하도록 만들 수 있습니다.**
- **명시적 변환 연산자를** 통해 타입 간 안전한 변환을 구현할 수 있습니다.

### **실습을 통해 확인할 내용**
- **자연스러운 나눗셈**: `15 / denominator` 형태로 직관적인 연산
- **타입 변환**: `(Denominator)15` 형태의 명시적 변환과 `int value = (int)denominator` 형태의 명시적 변환
- **개선된 사용성**: 이전 단계 대비 `.Value` 속성 접근 불필요

## 왜 필요한가?

이전 단계인 `04-Always-Valid`에서는 값 객체를 도입하여 컴파일 타임에 유효성을 보장할 수 있게 되었지만, 여전히 `.Value` 속성을 통해 내부 값에 접근해야 한다는 제약이 있었습니다.

`numerator / denominator.Value`라는 표현은 수학적 직관과 맞지 않습니다. 수학에서는 "15 / 5"라고 쓰지 "15 / 5의 값"이라고 쓰지 않습니다. 이 불일치는 도메인 전문가가 코드를 읽기 어렵게 만들고, **공통 언어(Ubiquitous Language)** 원칙을 위반합니다. 또한 값 객체를 사용할 때마다 `.Value`에 접근해야 하므로 코드가 불필요하게 장황해지고, 캡슐화의 장점을 제대로 활용하지 못합니다.

**연산자 오버로딩(Operator Overloading)을** 도입하면 `15 / denominator`와 같은 자연스러운 수학적 표현을 코드에서 직접 사용할 수 있고, 도메인 언어를 더 직관적으로 표현할 수 있습니다.

## 핵심 개념

### 연산자 오버로딩

연산자 오버로딩은 **기존 연산자의 동작을 사용자 정의 타입에 맞게 재정의**하는 C#의 기능입니다. `numerator / denominator.Value` 대신 `numerator / denominator`로 쓸 수 있게 됩니다.

이전 방식과 개선된 방식을 비교합니다.

```csharp
// 이전 방식 (문제가 있는 방식) - .Value 속성 접근 필요
public static int Divide(int numerator, Denominator denominator)
{
    return numerator / denominator.Value;  // .Value 필요
}

// 개선된 방식 (연산자 오버로딩) - 자연스러운 연산
public static int operator /(int numerator, Denominator denominator)
{
    return numerator / denominator._value;  // .Value 불필요
}
```

코드가 수학적 표현과 동일해지므로, 도메인 전문가도 쉽게 이해할 수 있습니다.

### 변환 연산자

변환 연산자는 **타입 간의 안전한 변환**을 구현합니다. `int`에서 `Denominator`로의 변환은 유효성 검증이 필요하므로 명시적(explicit) 변환으로 정의합니다.

```csharp
// 명시적 변환 - 안전한 변환
public static explicit operator Denominator(int value)
{
    return Denominator.Create(value).Match(
        Succ: x => x,
        Fail: _ => throw new InvalidCastException("0은 Denominator로 변환할 수 없습니다")
    );
}

// 명시적 변환 - 자동 변환
public static explicit operator int(Denominator denominator)
{
    return denominator._value;  // 안전한 변환
}
```

명시적 변환을 사용하면 컴파일러가 타입 변환 지점을 명확하게 표시하도록 강제하므로, 암시적 변환으로 인한 의도치 않은 변환을 방지할 수 있습니다.

### 도메인 언어의 자연스러운 표현

연산자 오버로딩의 궁극적 목표는 코드가 도메인 전문가의 언어와 일치하도록 만드는 것입니다.

```csharp
// 이전 방식 - 도메인 언어와 다름
var result = numerator / denominator.Value;  // "15 나누기 5의 값"

// 개선된 방식 - 도메인 언어와 일치
var result = numerator / denominator;        // "15 나누기 5"
```

## 실전 지침

### 예상 출력
```
=== 연산자 오버로딩을 통한 자연스러운 나눗셈 연산 ===

1. 핵심 개선사항: 자연스러운 나눗셈 연산
  Before (04-Always-Valid): numerator / denominator.Value
  After  (05-Operator-Overloading): numerator / denominator
  15 / OperatorOverloading.ValueObjects.Denominator = 3
  15 / OperatorOverloading.ValueObjects.Denominator = 3 (직접 연산자)

2. 변환 연산자:
  int에서 Denominator로 변환:
    15 -> Denominator: OperatorOverloading.ValueObjects.Denominator
    Denominator -> int: 15
    0 -> Denominator(변환 실패): 0은 Denominator로 변환할 수 없습니다

3. 에러 처리:
  연산 중 에러 처리:
    Denominator 생성 실패: 0은 허용되지 않습니다
```

### **핵심 구현 포인트**
1. **연산자 오버로딩**: `public static int operator /(int numerator, Denominator denominator)` 구현
2. **변환 연산자**: `explicit operator Denominator(int value)`와 `implicit operator int(Denominator value)` 구현
3. **에러 처리**: 0값 변환 시도 시 `InvalidCastException` 발생

## 프로젝트 설명

### **프로젝트 구조**
```
OperatorOverloading/                        # 메인 프로젝트
├── Program.cs                              # 메인 실행 파일
├── MathOperations.cs                       # 연산자 오버로딩 활용 수학 연산
├── ValueObjects/
│   └── Denominator.cs                      # 연산자 오버로딩이 구현된 분모 값 객체
├── OperatorOverloading.csproj              # 프로젝트 파일
└── README.md                               # 메인 문서
```

### **핵심 코드**

#### **Denominator 클래스 - 연산자 오버로딩**
```csharp
public sealed class Denominator
{
    private readonly int _value;

    // 핵심: int와 Denominator 간의 나눗셈 연산자
    public static int operator /(int numerator, Denominator denominator) =>
        numerator / denominator._value;

    // 변환 연산자들
    // 명시적 변환 연산자
    public static explicit operator Denominator(int value) =>
        Create(value).Match(
            Succ: x => x,
            Fail: _ => throw new InvalidCastException("0은 Denominator로 변환할 수 없습니다")
        );

    // 명시적 변환 연산자
    public static explicit operator int(Denominator value) =>
        value._value;
}
```

#### **MathOperations - 자연스러운 연산**
```csharp
public static class MathOperations
{
    public static int Divide(int numerator, Denominator denominator)
    {
        // 핵심 개선사항: .Value 없이 자연스러운 연산
        return numerator / denominator;
    }
}
```

#### **테스트 - 연산자 동작 확인**
```csharp
// 자연스러운 나눗셈 연산
int result = MathOperations.Divide(15, denom);
int directResult = 15 / denom;  // 직접 연산자 사용

// 변환 연산자 테스트
var nonZero = (Denominator)15;  // 명시적 변환: Denominator <- int
int intValue = (int)nonZero;    // 명시적 변환: int         <- Denominator
```

## 한눈에 보는 정리

### **개선점 비교**

이전 단계와 현재 단계의 사용성 차이를 보여줍니다.

| 구분 | 이전 방식 (04-Always-Valid) | 현재 방식 (05-Operator-Overloading) |
|------|------------------------------|-------------------------------------|
| **연산 표현** | `numerator / denominator.Value` | `numerator / denominator` |
| **가독성** | `.Value` 속성 접근으로 복잡 | 자연스러운 수학적 표현 |
| **도메인 언어** | 프로그래밍 언어 중심 | 도메인 중심의 직관적 표현 |
| **사용성** | 내부 값 추출 필요 | 직접 연산 가능 |

### **장단점**

연산자 오버로딩 도입 시 얻는 이점과 주의할 점을 정리합니다.

| 장점 | 단점 |
|------|------|
| **자연스러운 도메인 언어** | **구현 복잡성 증가** |
| **향상된 가독성** | **디버깅 시 내부 값 접근 제한** |
| **직관적인 연산** | **연산자 의미 재정의 필요** |
| **타입 안전성 유지** | **잘못된 연산자 오버로딩 시 혼란** |

### **핵심 기술**
- **연산자 오버로딩**: `/` 연산자 재정의
- **변환 연산자**: `explicit`/`implicit` 변환 지원
- **값 객체 패턴**: 불변성과 유효성 검증 유지
- **함수형 프로그래밍**: `LanguageExt`의 `Fin<T>` 활용

## FAQ

### Q1: 연산자 오버로딩이 성능에 영향을 주나요?
**A**: 거의 없습니다. 연산자 오버로딩은 컴파일 시점에 메서드 호출로 변환되며, JIT 컴파일러가 인라인 최적화를 적용할 수 있으므로 런타임 오버헤드는 일반 메서드 호출과 동일한 수준입니다.

### Q2: 모든 연산자에 대해 오버로딩이 가능한가요?
**A**: 대부분의 산술/비교 연산자(`+`, `-`, `*`, `/`, `==`, `!=` 등)는 오버로딩이 가능하지만, 할당 연산자(`=`)나 멤버 접근 연산자(`.`)는 오버로딩할 수 없습니다.

```csharp
// 가능한 연산자들
public static T operator +(T a, T b)     // 덧셈
public static T operator -(T a, T b)     // 뺄셈
public static T operator *(T a, T b)     // 곱셈
public static T operator /(T a, T b)     // 나눗셈
public static bool operator ==(T a, T b) // 동등 비교
public static bool operator !=(T a, T b) // 부등 비교

// 불가능한 연산자들
// public static T operator =(T a, T b)  // 할당 연산자
// public static T operator .(T a, T b)  // 멤버 접근 연산자
```

### Q3: 연산자 오버로딩과 메서드 오버로딩의 차이점은?
**A**: 연산자 오버로딩은 `+`, `/` 등의 연산자 동작을 재정의하여 수학적 표현을 코드에서 직접 사용할 수 있게 합니다. 메서드 오버로딩은 같은 이름의 메서드를 매개변수만 다르게 정의하여 다양한 입력을 처리합니다. 도메인 연산을 직관적으로 표현해야 할 때는 연산자 오버로딩이 더 자연스럽습니다.

```csharp
// 연산자 오버로딩
public static int operator /(int a, Denominator b) => a / b._value;

// 메서드 오버로딩
public int Divide(int a) => a / _value;
public int Divide(double a) => (int)(a / _value);
```

---

연산자 오버로딩으로 자연스러운 수학적 표현을 얻었지만, 여러 `Fin<T>` 값을 조합하는 복합 연산에서는 `Match` 메서드를 중첩해야 하는 불편함이 남아 있습니다. 다음 장에서는 **LINQ 표현식**을 도입하여 `from`/`select` 구문으로 복합 연산과 에러 전파를 간결하게 처리합니다.
