---
title: "Bind 내부 Apply 검증"
---

## 개요

전화번호를 검증한다고 가정합니다. 전체 형식이 유효해야만 국가코드, 지역코드, 로컬번호를 개별적으로 검증할 수 있습니다. 형식이 "+86abc123def"처럼 잘못되었다면 구성 요소를 파싱하는 것 자체가 무의미합니다. 먼저 Bind로 전제 조건(형식)을 검증하고, 통과하면 Apply로 구성 요소들을 병렬 검증하는 역방향 중첩 패턴이 필요합니다.

## 학습 목표

- Bind 내부에서 Apply를 사용하여 전제 조건 검증 후 구성 요소들을 병렬 검증하는 **역방향 중첩 검증 패턴을** 구현할 수 있습니다.
- 특정 조건이 만족되어야만 후속 검증이 의미를 가지는 **전제 조건 기반 검증을** 설계할 수 있습니다.
- 복합 데이터의 각 구성 요소를 동시에 검증하여 성능을 최적화하는 **구성 요소 병렬 검증을** 적용할 수 있습니다.

## 왜 필요한가?

이전 단계에서는 외부 Apply, 내부 Bind 구조를 다뤘습니다. 하지만 전제 조건이 만족되어야만 후속 검증이 의미를 가지는 상황에서는 반대 방향의 접근이 필요합니다.

전화번호 검증이 대표적입니다. 전체 형식이 유효하지 않으면 국가코드, 지역코드, 로컬번호를 개별적으로 검증하는 것이 무의미합니다. 반면, 형식이 유효하다면 세 구성 요소는 서로 독립적이므로 동시에 검증할 수 있습니다. 이 패턴에서 Bind 단계가 실패하면 단일 에러가 발생하고, Apply 단계가 실패하면 여러 구성 요소의 에러가 동시에 수집됩니다.

**Bind 내부 Apply 중첩 검증 패턴은** 전제 조건 기반의 효율적인 검증을 구현합니다.

## 핵심 개념

### 전제 조건 기반 검증

전제 조건을 Bind로 먼저 검증하고, 통과한 경우에만 구성 요소들을 Apply로 병렬 검증합니다. 전제 조건이 실패하면 후속 검증 자체를 실행하지 않습니다.

다음 코드는 전제 조건 없이 검증하는 방식과 전제 조건 기반 검증 방식을 비교합니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 모든 검증을 동시에 실행
public static Validation<Error, PhoneNumber> ValidateOld(string phoneNumber)
{
    // 전제 조건 없이 모든 검증을 동시에 실행하여 비효율적
    var countryResult = ValidateCountryCode(phoneNumber);
    var areaResult = ValidateAreaCode(phoneNumber);
    var localResult = ValidateLocalNumber(phoneNumber);
    // 형식이 유효하지 않아도 구성 요소 검증을 실행하여 불필요한 연산
}

// 개선된 방식 (현재 방식) - 전제 조건 기반 검증
public static Validation<Error, (string CountryCode, string AreaCode, string LocalNumber)> Validate(string phoneNumber) =>
    // 1단계: 전제 조건 검증 (Bind) - 전화번호 형식을 먼저 검증
    ValidatePhoneNumberFormat(phoneNumber)
        // 2단계: 구성 요소 병렬 검증 (Apply) - 형식이 유효하면 구성 요소들을 병렬로 검증
        .Bind(validFormat =>
            (ValidateCountryCode(validFormat), ValidateAreaCode(validFormat), ValidateLocalNumber(validFormat))
                .Apply((countryCode, areaCode, localNumber) => (countryCode, areaCode, localNumber))
                .As());
```

전제 조건이 만족되지 않으면 불필요한 후속 검증을 완전히 건너뜁니다.

### 구성 요소 병렬 검증

전제 조건이 통과하면, 각 구성 요소는 서로 독립적이므로 Apply로 동시에 검증합니다. 국가코드, 지역코드, 로컬번호 검증이 모두 독립적으로 실행되어 실패한 구성 요소의 에러가 모두 수집됩니다.

```csharp
// 전제 조건 검증 - 먼저 실행되어야 함
private static Validation<Error, string> ValidatePhoneNumberFormat(string phoneNumber) =>
    !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10
        ? phoneNumber
        : DomainErrors.PhoneNumberTooShort(phoneNumber);

// 구성 요소 병렬 검증 - Apply 내부에서 병렬 실행
private static Validation<Error, string> ValidateCountryCode(string phoneNumber) =>
    phoneNumber.StartsWith("+82") || phoneNumber.StartsWith("+1")
        ? phoneNumber.Substring(0, 3)
        : DomainErrors.CountryCodeUnsupported(phoneNumber);

private static Validation<Error, string> ValidateAreaCode(string phoneNumber) =>
    phoneNumber.Length >= 6 && phoneNumber.Substring(3, 3).All(char.IsDigit)
        ? phoneNumber.Substring(3, 3)
        : DomainErrors.AreaCodeInvalid(phoneNumber);

private static Validation<Error, string> ValidateLocalNumber(string phoneNumber) =>
    phoneNumber.Length >= 10 && phoneNumber.Substring(6).All(char.IsDigit)
        ? phoneNumber.Substring(6)
        : DomainErrors.LocalNumberInvalid(phoneNumber);
```

## 실전 지침

### 예상 출력
```
=== Bind 내부 Apply 중첩 검증 (Bind Internal Apply) 예제 ===
전화번호 값 객체의 Bind 내부에서 Apply를 사용한 중첩 검증 예제입니다.

--- 유효한 한국 전화번호 ---
전화번호: '+821012345678'
성공: 전화번호가 유효합니다.
   → 국가코드: +82
   → 지역코드: 101
   → 로컬번호: 2345678
   → 모든 중첩 검증 규칙을 통과했습니다.

--- 전화번호 형식 오류 ---
전화번호: '123'
실패:
   → 에러 코드: DomainErrors.PhoneNumber.PhoneNumberTooShort
   → 현재 값: '123'

--- 복수개 오류 (Apply 단계에서 실패) ---
전화번호: '+86abc123def'
실패:
   → 총 3개의 검증 실패:
     1. 에러 코드: DomainErrors.PhoneNumber.CountryCodeUnsupported
        현재 값: '+86abc123def'
     2. 에러 코드: DomainErrors.PhoneNumber.AreaCodeInvalid
        현재 값: '+86abc123def'
     3. 에러 코드: DomainErrors.PhoneNumber.LocalNumberInvalid
        현재 값: '+86abc123def'
```

### 핵심 구현 포인트

구현 시 세 가지 포인트에 주목합니다. Bind로 전화번호 형식을 먼저 검증하여 전제 조건을 확인하고, Apply로 국가코드, 지역코드, 로컬번호를 동시에 검증하며, Bind 단계는 단일 에러를, Apply 단계는 ManyErrors를 반환하도록 에러를 구분합니다.

## 프로젝트 설명

### 프로젝트 구조
```
05-Bind-Internal-Apply-Validation/
├── Program.cs              # 메인 실행 파일
├── ValueObjects/
│   └── PhoneNumber.cs      # 전화번호 값 객체 (역방향 중첩 검증 패턴 구현)
├── BindInternalApplyValidation.csproj
└── README.md               # 메인 문서
```

### 핵심 코드

PhoneNumber 값 객체는 Bind로 형식을 먼저 검증하고, Apply로 국가코드, 지역코드, 로컬번호를 병렬 검증합니다.

```csharp
public sealed class PhoneNumber : ValueObject
{
    public string CountryCode { get; }
    public string AreaCode { get; }
    public string LocalNumber { get; }

    // 역방향 중첩 검증 패턴 구현 (Bind 내부에서 Apply 사용)
    public static Validation<Error, (string CountryCode, string AreaCode, string LocalNumber)> Validate(string phoneNumber) =>
        // 1단계: 전제 조건 검증 (Bind) - 전화번호 형식을 먼저 검증
        ValidatePhoneNumberFormat(phoneNumber)
            // 2단계: 구성 요소 병렬 검증 (Apply) - 형식이 유효하면 구성 요소들을 병렬로 검증
            .Bind(validFormat =>
                (ValidateCountryCode(validFormat), ValidateAreaCode(validFormat), ValidateLocalNumber(validFormat))
                    .Apply((countryCode, areaCode, localNumber) => (countryCode, areaCode, localNumber))
                    .As());

    // 전제 조건 검증 - 먼저 실행되어야 함
    private static Validation<Error, string> ValidatePhoneNumberFormat(string phoneNumber) =>
        !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10
            ? phoneNumber
            : DomainErrors.PhoneNumberTooShort(phoneNumber);

    // 구성 요소 병렬 검증 - Apply 내부에서 병렬 실행
    private static Validation<Error, string> ValidateCountryCode(string phoneNumber) =>
        phoneNumber.StartsWith("+82") || phoneNumber.StartsWith("+1")
            ? phoneNumber.Substring(0, 3)
            : DomainErrors.CountryCodeUnsupported(phoneNumber);

    private static Validation<Error, string> ValidateAreaCode(string phoneNumber) =>
        phoneNumber.Length >= 6 && phoneNumber.Substring(3, 3).All(char.IsDigit)
            ? phoneNumber.Substring(3, 3)
            : DomainErrors.AreaCodeInvalid(phoneNumber);

    private static Validation<Error, string> ValidateLocalNumber(string phoneNumber) =>
        phoneNumber.Length >= 10 && phoneNumber.Substring(6).All(char.IsDigit)
            ? phoneNumber.Substring(6)
            : DomainErrors.LocalNumberInvalid(phoneNumber);
}
```

## 한눈에 보는 정리

다음 표는 두 가지 중첩 검증 패턴의 차이를 비교합니다.

| 구분 | Apply 내부 Bind 중첩 검증 | Bind 내부 Apply 중첩 검증 |
|------|-------------------------|-------------------------|
| **외부 구조** | 필드들을 병렬로 검증 | 전제 조건을 순차로 검증 |
| **내부 구조** | 각 필드 내부에서 순차 검증 | 구성 요소들을 병렬로 검증 |
| **적용 상황** | 각 필드별 복잡한 검증 | 전제 조건 기반 구성 요소 검증 |
| **성능** | 필드별 병렬 처리 | 전제 조건 후 구성 요소 병렬 처리 |

다음 표는 이 패턴의 장단점을 정리합니다.

| 장점 | 단점 |
|------|------|
| 전제 조건 불만족 시 불필요한 검증 생략 | 검증 구조가 복잡해짐 |
| 전제 조건 후 구성 요소 병렬 검증 | 중첩된 검증 로직 디버깅 어려움 |
| 전제 조건 기반의 논리적 검증 흐름 | 전제 조건을 신중히 설계해야 함 |

## FAQ

### Q1: 언제 역방향 중첩 검증 패턴을 사용해야 하나요?
**A:** 전제 조건이 만족되어야만 후속 검증이 의미를 가지는 경우에 사용합니다. 전화번호 검증에서 전체 형식이 유효해야만 국가코드, 지역코드, 로컬번호를 개별적으로 검증할 수 있는 경우가 대표적입니다.

### Q2: 전제 조건과 구성 요소 검증의 차이점은?
**A:** 전제 조건은 전체 구조나 형식을 검증하는 것이고, 구성 요소 검증은 각 부분을 세밀하게 검증하는 것입니다. 전제 조건이 만족되지 않으면 구성 요소 검증을 실행할 필요가 없습니다.

### Q3: Bind와 Apply의 순서는 어떻게 결정하나요?
**A:** 전제 조건이 되는 검증을 Bind로 먼저 실행하고, 그 결과가 성공하면 구성 요소들을 Apply로 병렬 검증합니다. 전제 조건을 먼저 수행하고 그 결과를 바탕으로 구성 요소 검증을 병렬로 수행하면 불필요한 연산을 피할 수 있습니다.

Part 2에서 Bind와 Apply의 조합 패턴을 모두 학습했습니다. Part 3에서는 이 개념들을 Functorium 프레임워크의 기본 클래스로 조립하여, 실전에서 바로 사용할 수 있는 값 객체 패턴을 완성합니다.

---

→ [6장: 컨텍스트 기반 검증](../06-Contextual-Validation/)
