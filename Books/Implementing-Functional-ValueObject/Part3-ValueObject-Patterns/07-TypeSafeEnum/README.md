# 유형 값 객체

## 개요

이 프로젝트는 **SmartEnum을 사용한 Type Safe Enum 구현** 방법을 학습합니다. 기존 C# enum의 한계를 극복하고, 타입 안전성과 도메인 로직을 강화한 열거형을 구현하는 방법을 다룹니다.

## 학습 목표

### **핵심 학습 목표**
1. **SmartEnum 이해**: Ardalis.SmartEnum 라이브러리를 활용한 강력한 열거형 구현
2. **Type Safety 확보**: 컴파일 타임에 타입 안전성을 보장하는 열거형 설계
3. **도메인 로직 내장**: 열거형 값에 비즈니스 로직과 속성을 직접 포함하는 방법

### **실습을 통해 확인할 내용**
- **SmartEnum 기본 사용법**: 정적 인스턴스, FromValue, FromName 메서드 활용
- **검증 기능**: LanguageExt를 활용한 함수형 검증 패턴 적용
- **비즈니스 로직**: 통화별 포맷팅, 기호, 한글 이름 등 도메인 특화 기능
- **에러 처리**: 구조화된 에러 메시지와 예외 처리

## 왜 필요한가?

기존 C# enum은 다음과 같은 한계가 있습니다:

### **1. 타입 안전성 부족**
```csharp
// 기존 enum의 문제점
public enum Currency { KRW, USD, EUR }

// 컴파일 타임에 잡히지 않는 오류
Currency currency = (Currency)999; // 유효하지 않은 값
```

### **2. 도메인 로직 표현 제약**
```csharp
// 기존 enum으로는 복잡한 로직 표현이 어려움
public enum Currency { KRW, USD, EUR }

// 통화별 기호나 이름을 별도로 관리해야 함
private static readonly Dictionary<Currency, string> Symbols = new()
{
    { Currency.KRW, "₩" },
    { Currency.USD, "$" },
    { Currency.EUR, "€" }
};
```

### **3. 확장성 문제**
```csharp
// 새로운 속성 추가 시 기존 코드 수정 필요
// 메서드 추가 불가능
// 상속 불가능
```

## 핵심 개념

### **1. SmartEnum 기본 구조**
```csharp
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");
    
    public string KoreanName { get; }
    public string Symbol { get; }
    
    private Currency(string name, string value, string koreanName, string symbol) 
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }
}
```

### **2. 함수형 검증 패턴**
```csharp
public static Validation<Error, string> Validate(string currencyCode) =>
    ValidateNotEmpty(currencyCode)
        .Bind(ValidateFormat)
        .Bind(ValidateSupported);
```

### **3. 도메인 로직 내장**
```csharp
public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
public string FormatAmountWithoutDecimals(decimal amount) => $"{Symbol}{amount:N0}";
```

## 실전 지침

### **1. SmartEnum 구현 패턴**
- **정적 인스턴스**: `public static readonly`로 각 열거형 값 정의
- **Private 생성자**: 외부에서 직접 생성 방지
- **추가 속성**: 도메인에 필요한 속성들을 생성자에서 초기화

### **2. 검증 로직 설계**
- **순차 검증**: `Bind`를 사용한 체이닝 패턴
- **구체적 에러**: 각 검증 단계별 명확한 에러 메시지
- **예외 처리**: `SmartEnumNotFoundException` 등 라이브러리 예외 처리

### **3. 비즈니스 로직 통합**
- **포맷팅 메서드**: 도메인별 데이터 포맷팅 로직
- **계산 메서드**: 열거형 값에 따른 계산 로직
- **검증 메서드**: 도메인 규칙에 따른 검증 로직

## 프로젝트 구조

```
07-TypeSafeEnum/
├── TypeSafeEnum/
│   ├── ValueObjects/
│   │   └── Currency.cs          # SmartEnum 기반 통화 열거형
│   ├── Program.cs               # 데모 프로그램
│   └── TypeSafeEnum.csproj      # 프로젝트 파일
└── README.md                    # 프로젝트 문서
```

## 핵심 코드

### **1. Currency SmartEnum 구현**
```csharp
public sealed class Currency : SmartEnum<Currency, string>, IValueObject
{
    // 정적 인스턴스들
    public static readonly Currency KRW = new(nameof(KRW), "KRW", "한국 원화", "₩");
    public static readonly Currency USD = new(nameof(USD), "USD", "미국 달러", "$");
    
    // 도메인 속성
    public string KoreanName { get; }
    public string Symbol { get; }
    
    // Private 생성자
    private Currency(string name, string value, string koreanName, string symbol) 
        : base(name, value)
    {
        KoreanName = koreanName;
        Symbol = symbol;
    }
    
    // 팩토리 메서드
    public static Fin<Currency> Create(string currencyCode) =>
        Validate(currencyCode).Map(FromValue).ToFin();
    
    // 검증 로직 - ValueObject 규칙 준수
    public static Validation<Error, string> Validate(string currencyCode) =>
        ValidateNotEmpty(currencyCode)
            .Bind(ValidateFormat)
            .Bind(ValidateSupported);
    
    // 개별 검증 메서드들 - 삼항 연산자 패턴 적용
    private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode)
            ? currencyCode
            : DomainErrors.Empty(currencyCode);
    
    private static Validation<Error, string> ValidateFormat(string currencyCode) =>
        currencyCode.Length == 3 && currencyCode.All(char.IsLetter)
            ? currencyCode.ToUpperInvariant()
            : DomainErrors.NotThreeLetters(currencyCode);
    
    // 비즈니스 로직
    public string FormatAmount(decimal amount) => $"{Symbol}{amount:N2}";
    
    // DomainErrors 중첩 클래스 - ValueObject 규칙 준수
    // ErrorCodeFactory를 사용한 구조화된 에러 처리
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Empty)}",
                errorCurrentValue: value);
        
        public static Error NotThreeLetters(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(NotThreeLetters)}",
                errorCurrentValue: value);
        
        public static Error Unsupported(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Unsupported)}",
                errorCurrentValue: value);
    }
}
```

### **2. 데모 프로그램**
```csharp
// 기본 사용법
var krw = Currency.KRW;
var usd = Currency.FromValue("USD");

// 검증 기능
var result = Currency.Create("INVALID");
result.Match(
    Succ: currency => Console.WriteLine($"성공: {currency}"),
    Fail: error => Console.WriteLine($"실패: {error.Message}")
);

// 비즈니스 로직
Console.WriteLine(Currency.USD.FormatAmount(1000)); // $1,000.00
```

## 예상 출력

```
=== Type Safe Enum 데모 ===

1. 기본 사용법
================
KRW: KRW (한국 원화) ₩
USD: USD (미국 달러) $
EUR: EUR (유로) €
JPY from value: JPY (일본 엔) ¥
GBP from name: GBP (영국 파운드) £

2. 검증 기능
=============
✅ 유효한 통화: USD (미국 달러) $
❌ 에러: 통화 코드는 3자리 영문자여야 합니다: US
❌ 에러: 지원하지 않는 통화 코드입니다: XYZ
❌ 에러: 통화 코드는 비어있을 수 없습니다: 

3. 비교 기능
=============
KRW == KRW: True
KRW == USD: False
KRW < USD: True
USD > EUR: False
KRW HashCode: 1234567890
USD HashCode: 9876543210

4. 비즈니스 로직
=================
KRW: ₩1,000.00
KRW: ₩1,000
USD: $1,000.00
USD: $1,000
EUR: €1,000.00
EUR: €1,000
JPY: ¥1,000.00
JPY: ¥1,000

5. 에러 처리
=============
✅ USD → USD (미국 달러) $
❌ INVALID → 지원하지 않는 통화 코드입니다: INVALID
❌ KR → 통화 코드는 3자리 영문자여야 합니다: KR
❌ XYZ → 지원하지 않는 통화 코드입니다: XYZ
✅ EUR → EUR (유로) €

6. 모든 지원 통화
==================
총 10개 통화 지원:
  - KRW (한국 원화) ₩
  - USD (미국 달러) $
  - EUR (유로) €
  - JPY (일본 엔) ¥
  - CNY (중국 위안) ¥
  - GBP (영국 파운드) £
  - AUD (호주 달러) A$
  - CAD (캐나다 달러) C$
  - CHF (스위스 프랑) CHF
  - SGD (싱가포르 달러) S$

=== 데모 완료 ===
```

## 한눈에 보는 정리

### SmartEnum vs 기존 Enum 비교
| 특징 | 기존 Enum | SmartEnum |
|------|-----------|-----------|
| **타입 안전성** | 제한적 | 완전한 타입 안전성 |
| **도메인 로직** | 별도 관리 필요 | 직접 내장 가능 |
| **속성 추가** | 불가능 | 자유롭게 추가 가능 |
| **메서드 추가** | 불가능 | 자유롭게 추가 가능 |
| **상속** | 불가능 | 가능 |
| **검증** | 수동 구현 필요 | 자동 제공 + 커스텀 가능 |
| **비교 기능** | 기본 제공 | 고급 비교 기능 제공 |
| **확장성** | 제한적 | 높은 확장성 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **완전한 타입 안전성** | **외부 라이브러리 의존성** |
| **도메인 로직 내장** | **기존 enum 대비 복잡성** |
| **강력한 검증 기능** | **성능 오버헤드 (미미)** |
| **확장성과 유연성** | **학습 곡선** |
| **비즈니스 규칙 표현** | **메모리 사용량 증가** |

## FAQ

### Q1. SmartEnum과 기존 enum의 가장 큰 차이점은 무엇인가요?
**A1.** SmartEnum은 **도메인 로직을 직접 내장**할 수 있다는 점이 가장 큰 차이입니다. 기존 enum은 단순한 정수/문자열 값만 저장할 수 있지만, SmartEnum은 속성, 메서드, 비즈니스 로직을 포함할 수 있습니다.

### Q2. 언제 SmartEnum을 사용해야 하나요?
**A2.** 다음 상황에서 SmartEnum 사용을 권장합니다:
- **도메인 로직이 필요한 열거형**: 포맷팅, 계산, 검증 등
- **타입 안전성이 중요한 경우**: 잘못된 값 입력 방지
- **확장 가능한 열거형**: 새로운 속성이나 메서드 추가 예정
- **복잡한 비즈니스 규칙**: 단순한 값 이상의 로직 필요

### Q3. SmartEnum의 성능은 어떤가요?
**A3.** SmartEnum은 기존 enum 대비 **미미한 성능 오버헤드**가 있습니다:
- **메모리**: 각 인스턴스가 객체이므로 약간 더 많은 메모리 사용
- **속도**: 메서드 호출 오버헤드가 있지만 실용적으로 무시할 수준
- **장점**: 타입 안전성과 도메인 로직으로 인한 **전체적인 코드 품질 향상**

### Q4. SmartEnum에서 검증 로직을 어떻게 구현하나요?
**A4.** LanguageExt의 `Validation<Error, T>` 타입을 사용하여 **함수형 검증 패턴**을 구현합니다:
```csharp
public static Validation<Error, string> Validate(string currencyCode) =>
    ValidateNotEmpty(currencyCode)
        .Bind(ValidateFormat)
        .Bind(ValidateSupported);
```

### Q5. SmartEnum을 상속받아 확장할 수 있나요?
**A5.** 네, SmartEnum은 **상속을 지원**합니다. 하지만 일반적으로는 **컴포지션 패턴**을 권장합니다:
```csharp
// 상속보다는 컴포지션 권장
public class ExtendedCurrency : Currency
{
    public string Region { get; }
    // ...
}
```

### Q6. 기존 enum에서 SmartEnum으로 마이그레이션하는 방법은?
**A6.** 단계적 마이그레이션을 권장합니다:
1. **새로운 SmartEnum 클래스 생성**
2. **기존 enum과 호환되는 정적 인스턴스 제공**
3. **점진적으로 기존 코드 교체**
4. **도메인 로직 점진적 추가**

### Q7. SmartEnum에서 ValueObject 규칙을 어떻게 준수하나요?
**A7.** SmartEnum도 ValueObject 규칙을 준수해야 합니다:
- **IValueObject 인터페이스 구현**: Framework의 IValueObject 사용
- **DomainErrors 중첩 클래스**: ErrorCodeFactory를 사용한 구조화된 에러 처리
- **삼항 연산자 패턴**: `조건 ? 성공값 : 실패에러` 형식
- **Bind 순차 검증**: 의존성이 있는 검증 단계들을 순차적으로 실행
- **1:1 매핑**: 각 Validate 메서드마다 대응하는 DomainErrors 메서드
- **InternalsVisibleTo**: Framework의 internal 클래스에 접근하기 위한 설정

```csharp
// ValueObject 규칙 준수 예시
private static Validation<Error, string> ValidateNotEmpty(string currencyCode) =>
    !string.IsNullOrWhiteSpace(currencyCode)
        ? currencyCode                    // 성공: 값 반환
        : DomainErrors.Empty(currencyCode); // 실패: 에러 반환

internal static class DomainErrors
{
    public static Error Empty(string value) =>
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(DomainErrors)}.{nameof(Currency)}.{nameof(Empty)}",
            errorCurrentValue: value);
}
```

### Q8. ErrorCodeFactory를 사용하는 이유는 무엇인가요?
**A8.** ErrorCodeFactory를 사용하는 이유는 다음과 같습니다:
- **구조화된 에러 코드**: 일관된 형식의 에러 코드 생성 (`DomainErrors.Currency.Empty`)
- **타입 안전성**: 컴파일 타임에 에러 코드 형식 검증
- **디버깅 효율성**: 에러 코드만으로도 어떤 ValueObject의 어떤 에러인지 즉시 파악
- **로깅 및 모니터링**: 구조화된 에러 정보로 시스템 모니터링 용이
- **프로젝트 간 일관성**: 모든 프로젝트에서 동일한 에러 처리 방식 적용

```csharp
// 에러 출력 예시
❌ 에러: 코드: DomainErrors.Currency.NotThreeLetters, 값: US
❌ 에러: 코드: DomainErrors.Currency.Unsupported, 값: XYZ
```
