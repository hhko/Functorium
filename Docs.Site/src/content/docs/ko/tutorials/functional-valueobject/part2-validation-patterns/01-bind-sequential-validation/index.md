---
title: "Bind 순차 검증"
---

## 개요

Part 1에서 단일 값 객체의 유효성을 보장하는 방법을 다루었습니다. 하지만 실제 애플리케이션에서는 여러 필드를 동시에 검증해야 하고, 필드 간에 의존 관계가 존재하는 경우가 흔합니다. 예를 들어 주소를 검증할 때, 국가 코드가 "KR"이면 우편번호는 5자리여야 하고 "JP"이면 7자리여야 합니다. 이런 의존적 검증 규칙을 Bind 패턴으로 어떻게 체이닝하는지 살펴봅니다.

## 학습 목표

- Bind 연산자가 이전 검증 결과를 다음 검증에 전달하는 **순차 실행 메커니즘을** 이해할 수 있습니다.
- 국가와 우편번호 간의 의존성을 가진 **검증 규칙을 Bind로** 구현할 수 있습니다.
- 첫 번째 실패에서 즉시 중단되는 **조기 중단(Short-circuit) 동작을** 활용할 수 있습니다.

## 왜 필요한가?

복잡한 도메인 규칙을 가진 값 객체를 구현하면 곧 세 가지 현실적 문제에 부딪힙니다.

검증 규칙들 간의 의존성 문제가 가장 먼저 드러납니다. 주소 검증에서 국가 코드가 "KR"이면 우편번호는 5자리 숫자여야 하고, "JP"이면 7자리 숫자여야 합니다. 이전 검증 결과가 다음 검증의 전제 조건이 되는 것입니다.

불필요한 검증 비용도 문제입니다. 도로명이 빈 값이면 도시나 우편번호를 검증하는 것은 의미가 없습니다. 첫 번째 조건이 실패하면 후속 조건들을 검사할 필요가 없습니다.

검증 순서의 중요성도 간과할 수 없습니다. 기본 형식 검증이 먼저 이루어져야 복잡한 비즈니스 규칙 검증이 의미를 가집니다.

**Bind 순차 검증 패턴은** 이 세 가지 문제를 의존성 체인을 통해 한꺼번에 해결합니다.

## 핵심 개념

### Bind 순차 실행 메커니즘

Bind는 이전 검증의 결과를 다음 검증의 입력으로 전달합니다. 각 단계가 성공해야만 다음 단계가 실행되므로, 첫 번째 실패 시점에서 체인이 즉시 중단됩니다.

다음 코드는 모든 검증을 독립적으로 실행하는 방식과 Bind 체이닝 방식을 비교합니다.

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

이 방식에서는 첫 번째 검증이 실패하면 즉시 중단되어 불필요한 검증 비용을 절약할 수 있습니다.

### 의존성 검증 패턴

특정 조건이 만족되어야 다음 단계로 진행할 수 있는 비즈니스 규칙을 Bind로 자연스럽게 표현할 수 있습니다.

```csharp
// 국가와 우편번호 간의 의존성 검증
private static Validation<Error, string> ValidateCountryAndPostalCodeMatch(string country, string postalCode) =>
    (country, postalCode) switch
    {
        ("KR", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
        ("US", var code) when code.Length == 5 && code.All(char.IsDigit) => country,
        ("JP", var code) when code.Length == 7 && code.All(char.IsDigit) => country,
        _ => Domain.CountryPostalCodeMismatch(country, postalCode)
    };
```

이 패턴을 사용하면 복잡한 비즈니스 규칙을 명확하고 타입 안전하게 표현할 수 있습니다.

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
   → 에러 코드: Domain.Address.StreetTooShort
   → 현재 값: ''
```

### 핵심 구현 포인트

구현 시 주의할 세 가지 포인트가 있습니다. 각 검증 메서드를 `.Bind()`로 연결하여 순차 실행 체인을 구성합니다. 첫 번째 실패에서 즉시 중단되는 특성을 활용하여 불필요한 연산을 피합니다. 마지막으로 `.Map()`을 통해 최종 결과를 원본 매개변수로 구성합니다.

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

Address 값 객체는 Bind를 사용하여 도로명, 도시, 우편번호, 국가일치를 순차적으로 검증합니다.

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
            _ => Domain.CountryPostalCodeMismatch(country, postalCode)
        };
}
```

## 한눈에 보는 정리

다음 표는 독립 실행 방식과 Bind 순차 검증 방식의 차이를 비교합니다.

| 구분 | 이전 방식 | Bind 순차 검증 |
|------|-----------|----------------|
| **실행 방식** | 모든 검증을 독립적으로 실행 | 순차적으로 체이닝하여 실행 |
| **의존성 처리** | 검증 규칙 간 의존성 고려 안함 | 이전 결과가 다음 검증에 전달 |
| **성능** | 모든 검증을 실행하여 비효율적 | 첫 번째 실패에서 조기 중단 |
| **비즈니스 규칙** | 단순한 개별 검증 | 복잡한 의존성 규칙 표현 가능 |

다음 표는 Bind 순차 검증의 장단점을 정리합니다.

| 장점 | 단점 |
|------|------|
| 첫 번째 실패에서 즉시 중단하여 효율적 | 검증 순서를 신중히 설계해야 함 |
| 복잡한 비즈니스 규칙을 명확히 표현 | 모든 검증을 동시에 실행할 수 없음 |
| 검증 순서가 비즈니스 로직과 일치 | 중간 단계에서 실패 시 원인 파악 필요 |

## FAQ

### Q1: Bind와 Apply의 차이점은 무엇인가요?
**A:** Bind는 순차 실행과 조기 중단을 제공하고, Apply는 병렬 실행과 모든 에러 수집을 제공합니다. 이전 결과가 다음 검증에 전달되어야 하는 의존성 검증에는 Bind가, 서로 독립적인 검증 규칙들을 동시에 실행할 때는 Apply가 적합합니다.

### Q2: 언제 Bind 패턴을 사용해야 하나요?
**A:** 검증 규칙들 간에 의존성이 있고, 특정 순서로 실행되어야 하는 경우에 사용합니다. 주소 검증에서 도로명이 유효해야 도시 검증이 의미가 있고, 도시가 유효해야 우편번호 검증이 의미가 있는 경우가 대표적입니다.

### Q3: Map을 왜 사용하나요?
**A:** Bind 체인에서 중간 결과들을 무시하고 원본 매개변수로 최종 결과를 구성하기 위해서입니다. 검증 과정에서 변환된 값이 아닌 원본 입력값들을 사용하여 최종 객체를 생성하므로, 검증과 객체 생성이 깔끔하게 분리됩니다.

그러나 Bind는 한 번에 하나의 에러만 보고합니다. 사용자에게 모든 문제를 한꺼번에 알려주려면 어떻게 해야 할까요? 다음 장에서 독립적인 검증을 병렬로 실행하는 Apply 패턴을 살펴봅니다.

---

→ [2장: 병렬 검증 (Apply)](../02-Apply-Parallel-Validation/)
