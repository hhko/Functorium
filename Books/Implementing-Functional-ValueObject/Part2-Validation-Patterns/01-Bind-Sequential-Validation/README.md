# Bind 순차 검증 (Bind Sequential Validation)

## 목차
- [개요](#개요)
- [학습 목표](#학습-목표)
- [왜 필요한가?](#왜-필요한가)
- [핵심 개념](#핵심-개념)
- [실전 지침](#실전-지침)
- [프로젝트 설명](#프로젝트-설명)
- [한눈에 보는 정리](#한눈에-보는-정리)
- [FAQ](#faq)

## 개요

이 프로젝트는 ValueObject에서 **의존적인 검증 규칙들을 순차적으로 실행**하는 Bind 패턴을 학습합니다. 주소 값 객체를 통해 이전 검증 결과가 다음 검증에 영향을 미치는 상황에서 어떻게 체이닝을 통해 효율적으로 검증하는지 알아봅니다.

## 학습 목표

### **핵심 학습 목표**
1. **Bind 연산자의 순차 실행 메커니즘 이해**: 이전 검증 결과가 다음 검증에 전달되는 체이닝 구조를 파악합니다.
2. **의존성 검증 패턴 구현**: 국가와 우편번호 간의 의존성을 가진 검증 규칙을 Bind로 구현합니다.
3. **조기 중단(Short-circuit) 동작 이해**: 첫 번째 실패에서 즉시 중단되는 Bind의 효율성을 경험합니다.

### **실습을 통해 확인할 내용**
- **순차 실행**: 도로명 → 도시 → 우편번호 → 국가일치 순서로 검증 실행
- **조기 중단**: 첫 번째 실패 시 후속 검증이 실행되지 않음
- **의존성 체인**: 국가와 우편번호 간의 비즈니스 규칙 검증

## 왜 필요한가?

이전 단계인 `BasicDivide`에서는 단순한 수학 연산을 통해 기본적인 ValueObject 개념을 구현했습니다. 하지만 실제로 **복잡한 도메인 규칙을 가진 값 객체**를 구현하려고 할 때 몇 가지 문제가 발생했습니다.

**첫 번째 문제는 검증 규칙들 간의 의존성입니다.** 마치 함수형 프로그래밍의 모나드 체이닝처럼, 이전 검증 결과가 다음 검증의 전제 조건이 되는 경우가 있습니다. 예를 들어, 주소 검증에서 국가 코드가 "KR"이면 우편번호는 5자리 숫자여야 하고, "JP"이면 7자리 숫자여야 합니다.

**두 번째 문제는 불필요한 검증 비용입니다.** 마치 데이터베이스의 인덱스 스캔처럼, 첫 번째 조건이 실패하면 후속 조건들을 검사할 필요가 없습니다. 도로명이 빈 값이면 도시나 우편번호를 검증하는 것은 의미가 없습니다.

**세 번째 문제는 검증 순서의 중요성입니다.** 마치 파이프라인 아키텍처처럼, 검증 규칙들이 특정 순서로 실행되어야 하는 경우가 있습니다. 기본 형식 검증이 먼저 이루어져야 복잡한 비즈니스 규칙 검증이 의미를 가집니다.

이러한 문제들을 해결하기 위해 **Bind 순차 검증 패턴**을 도입했습니다. Bind를 사용하면 의존성 체인을 통해 효율적이고 논리적인 검증 흐름을 구현할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 2가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### Bind 순차 실행 메커니즘

**핵심 아이디어는 "체이닝을 통한 의존성 전달"입니다.** 마치 함수형 프로그래밍의 모나드 체이닝처럼, 이전 검증의 결과가 다음 검증의 입력으로 전달됩니다.

예를 들어, 주소 검증을 생각해보세요. 마치 파이프라인 아키텍처처럼 각 단계가 순차적으로 실행되어야 합니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 모든 검증을 독립적으로 실행
public static Validation<Error, Address> ValidateOld(string street, string city, string postalCode, string country)
{
    var streetResult = ValidateStreet(street);
    var cityResult = ValidateCity(city);
    var postalCodeResult = ValidatePostalCode(postalCode);
    var countryResult = ValidateCountry(country);
    // 모든 검증을 동시에 실행하여 비효율적
}

// 개선된 방식 (현재 방식) - Bind를 통한 순차 실행
public static Validation<Error, (string, string, string, string)> Validate(string street, string city, string postalCode, string country) =>
    ValidateStreetFormat(street)
        .Bind(_ => ValidateCityFormat(city))
        .Bind(_ => ValidatePostalCodeFormat(postalCode))
        .Bind(_ => ValidateCountryAndPostalCodeMatch(country, postalCode))
        .Map(_ => (street, city, postalCode, country));
```

이 방식의 장점은 첫 번째 검증이 실패하면 즉시 중단되어 불필요한 검증 비용을 절약할 수 있다는 것입니다.

### 의존성 검증 패턴

**핵심 아이디어는 "비즈니스 규칙의 논리적 연결"입니다.** 마치 상태 머신처럼, 특정 조건이 만족되어야 다음 단계로 진행할 수 있습니다.

```csharp
// 국가와 우편번호 간의 의존성 검증
private static Validation<Error, string> ValidateCountryAndPostalCodeMatch(string country, string postalCode) =>
    (country, postalCode) switch
    {
        ("KR", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
        ("US", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
        ("JP", var code) when code.Length == 7 && code.All(char.IsDigit) => country,
        _ => DomainErrors.CountryPostalCodeMismatch(country, postalCode)
    };
```

이 방식의 장점은 복잡한 비즈니스 규칙을 명확하고 타입 안전하게 표현할 수 있다는 것입니다.

## 실전 지침

### 예상 출력
```
=== 의존 검증 (Dependent Validation) 예제 ===
주소 값 객체의 의존적인 검증 규칙들을 순차적으로 실행합니다.

--- 유효한 한국 주소 ---
도로명: '강남대로 123'
도시: '서울'
우편번호: '12345'
국가: 'KR'
성공: 주소가 유효합니다.
   → 완전한 주소: 강남대로 123, 서울 12345, KR
   → 모든 의존 검증 규칙을 순차적으로 통과했습니다.

--- 도로명이 빈 값 ---
도로명: ''
도시: '서울'
우편번호: '12345'
국가: 'KR'
실패:
   → 에러 코드: DomainErrors.Address.StreetTooShort
   → 현재 값: ''
```

### 핵심 구현 포인트
1. **Bind 체이닝 구조**: 각 검증 메서드를 `.Bind()`로 연결하여 순차 실행
2. **조기 중단 활용**: 첫 번째 실패에서 즉시 중단되는 특성 활용
3. **Map을 통한 결과 구성**: 최종 결과를 원본 매개변수로 구성

## 프로젝트 설명

### 프로젝트 구조
```
01-Bind-Sequential-Validation/
├── Program.cs              # 메인 실행 파일
├── ValueObjects/
│   └── Address.cs          # 주소 값 객체 (Bind 패턴 구현)
├── BindSequentialValidation.csproj
└── README.md               # 메인 문서
```

### 핵심 코드

**Address.cs - Bind 순차 검증 구현**
```csharp
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    // Bind를 통한 순차 검증 구현
    public static Validation<Error, (string Street, string City, string PostalCode, string Country)> Validate(
        string street, string city, string postalCode, string country) =>
        ValidateStreetFormat(street)
            .Bind(_ => ValidateCityFormat(city))
            .Bind(_ => ValidatePostalCodeFormat(postalCode))
            .Bind(_ => ValidateCountryAndPostalCodeMatch(country, postalCode))
            .Map(_ => (street, city, postalCode, country));

    // 의존성 검증 - 국가와 우편번호 간의 비즈니스 규칙
    private static Validation<Error, string> ValidateCountryAndPostalCodeMatch(string country, string postalCode) =>
        (country, postalCode) switch
        {
            ("KR", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
            ("US", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
            ("JP", var code) when code.Length == 7 && code.All(char.IsDigit) => country,
            _ => DomainErrors.CountryPostalCodeMismatch(country, postalCode)
        };
}
```

## 한눈에 보는 정리

### 비교 표
| 구분 | 이전 방식 | Bind 순차 검증 |
|------|-----------|----------------|
| **실행 방식** | 모든 검증을 독립적으로 실행 | 순차적으로 체이닝하여 실행 |
| **의존성 처리** | 검증 규칙 간 의존성 고려 안함 | 이전 결과가 다음 검증에 전달 |
| **성능** | 모든 검증을 실행하여 비효율적 | 첫 번째 실패에서 조기 중단 |
| **비즈니스 규칙** | 단순한 개별 검증 | 복잡한 의존성 규칙 표현 가능 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **효율성** | 첫 번째 실패에서 즉시 중단 | **복잡성** | 검증 순서를 신중히 설계해야 함 |
| **의존성 표현** | 복잡한 비즈니스 규칙을 명확히 표현 | **유연성** | 모든 검증을 동시에 실행할 수 없음 |
| **논리적 흐름** | 검증 순서가 비즈니스 로직과 일치 | **디버깅** | 중간 단계에서 실패 시 원인 파악 필요 |

## FAQ

### Q1: Bind와 Apply의 차이점은 무엇인가요?
**A**: Bind는 순차 실행과 조기 중단을 제공하는 반면, Apply는 병렬 실행과 모든 에러 수집을 제공합니다. Bind는 마치 함수형 프로그래밍의 모나드 체이닝처럼 이전 결과가 다음 검증에 전달되어야 하는 의존성 검증에 적합합니다. Apply는 서로 독립적인 검증 규칙들을 동시에 실행하여 성능을 최적화할 때 사용합니다.

### Q2: 언제 Bind 패턴을 사용해야 하나요?
**A**: 검증 규칙들 간에 의존성이 있고, 특정 순서로 실행되어야 하는 경우에 사용하세요. 예를 들어, 주소 검증에서 도로명이 유효해야 도시 검증이 의미가 있고, 도시가 유효해야 우편번호 검증이 의미가 있는 경우입니다. 마치 파이프라인 아키텍처처럼 각 단계가 이전 단계의 성공을 전제로 하는 경우에 적합합니다.

### Q3: Map을 왜 사용하나요?
**A**: Bind 체인에서 중간 결과들을 무시하고 원본 매개변수로 최종 결과를 구성하기 위해서입니다. 마치 함수형 프로그래밍의 합성처럼, 검증 과정에서 변환된 값이 아닌 원본 입력값들을 사용하여 최종 객체를 생성합니다. 이는 검증과 객체 생성을 분리하여 코드의 명확성을 높입니다.

### Q4: 조기 중단이 성능에 미치는 영향은?
**A**: 첫 번째 검증이 실패하는 경우 후속 검증을 실행하지 않아 상당한 성능 향상을 가져옵니다. 마치 데이터베이스의 인덱스 스캔처럼, 불필요한 연산을 피할 수 있습니다. 특히 복잡한 비즈니스 규칙이나 외부 API 호출이 포함된 검증에서는 그 효과가 더욱 클 것입니다.
