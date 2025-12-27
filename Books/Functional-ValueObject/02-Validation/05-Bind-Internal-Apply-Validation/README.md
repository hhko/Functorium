# Bind 내부 Apply 중첩 검증 (Bind Internal Apply Validation)

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

이 프로젝트는 ValueObject에서 **외부에서는 순차 검증을 수행하되, 특정 검증 단계 내부에서는 병렬 검증을 수행**하는 중첩 검증 패턴을 학습합니다. 전화번호 값 객체를 통해 전제 조건 검증 후 구성 요소들을 병렬로 검증하는 효율적인 검증 전략을 알아봅니다.

## 학습 목표

### **핵심 학습 목표**
1. **역방향 중첩 검증 패턴 구현**: Bind 내부에서 Apply를 사용하여 전제 조건 검증 후 구성 요소들을 병렬 검증합니다.
2. **전제 조건 기반 검증 이해**: 특정 조건이 만족되어야만 후속 검증이 의미를 가지는 상황을 처리합니다.
3. **구성 요소 병렬 검증**: 복합 데이터의 각 구성 요소를 동시에 검증하여 성능을 최적화합니다.

### **실습을 통해 확인할 내용**
- **전제 조건 검증**: 전화번호 형식을 먼저 검증
- **구성 요소 병렬 검증**: 국가코드, 지역코드, 로컬번호를 동시에 검증
- **복합 에러 수집**: 여러 구성 요소에서 동시 발생하는 에러 수집

## 왜 필요한가?

이전 단계인 `Apply-Internal-Bind-Validation`에서는 외부에서 병렬 검증을 수행하고 내부에서 순차 검증을 수행했습니다. 하지만 실제로 **전제 조건이 만족되어야만 후속 검증이 의미를 가지는** 상황에서는 다른 접근이 필요합니다.

**첫 번째 문제는 전제 조건의 중요성입니다.** 마치 상태 머신처럼, 특정 조건이 만족되어야만 다음 단계로 진행할 수 있습니다. 예를 들어, 전화번호 검증에서 전체 형식이 유효해야만 국가코드, 지역코드, 로컬번호를 개별적으로 검증할 수 있습니다.

**두 번째 문제는 구성 요소 검증의 효율성입니다.** 마치 병렬 처리 아키텍처처럼, 전제 조건이 만족되면 각 구성 요소들은 독립적으로 검증할 수 있습니다. 전화번호 형식이 유효하다면 국가코드, 지역코드, 로컬번호를 동시에 검증하여 성능을 최적화할 수 있습니다.

**세 번째 문제는 복합 데이터의 구조적 검증입니다.** 마치 파서 컴포지터처럼, 전체 구조를 먼저 검증한 후 각 구성 요소를 세밀하게 검증해야 합니다. 이는 마치 XML 파싱처럼, 전체 형식이 유효해야만 각 요소를 검증할 수 있는 것과 같습니다.

이러한 문제들을 해결하기 위해 **Bind 내부 Apply 중첩 검증 패턴**을 도입했습니다. 이 패턴을 사용하면 전제 조건 기반의 효율적인 검증을 구현할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 2가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 전제 조건 기반 검증

**핵심 아이디어는 "단계적 검증 아키텍처"입니다.** 마치 상태 머신처럼, 특정 조건이 만족되어야만 다음 단계로 진행할 수 있습니다.

예를 들어, 전화번호 검증을 생각해보세요. 마치 파서 컴포지터처럼, 전체 형식을 먼저 검증한 후 각 구성 요소를 세밀하게 검증합니다.

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

이 방식의 장점은 전제 조건이 만족되지 않으면 불필요한 후속 검증을 피할 수 있다는 것입니다.

### 구성 요소 병렬 검증

**핵심 아이디어는 "독립적 구성 요소의 동시 검증"입니다.** 마치 병렬 처리 아키텍처처럼, 전제 조건이 만족되면 각 구성 요소들을 동시에 검증합니다.

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

이 방식의 장점은 전제 조건이 만족되면 각 구성 요소를 동시에 검증하여 성능을 최적화할 수 있다는 것입니다.

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
1. **전제 조건 검증**: Bind로 전화번호 형식을 먼저 검증
2. **구성 요소 병렬 검증**: Apply로 국가코드, 지역코드, 로컬번호를 동시에 검증
3. **에러 구분**: Bind 단계는 단일 에러, Apply 단계는 ManyErrors

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

**PhoneNumber.cs - 역방향 중첩 검증 패턴 구현**
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

### 비교 표
| 구분 | Apply 내부 Bind 중첩 검증 | Bind 내부 Apply 중첩 검증 |
|------|-------------------------|-------------------------|
| **외부 구조** | 필드들을 병렬로 검증 | 전제 조건을 순차로 검증 |
| **내부 구조** | 각 필드 내부에서 순차 검증 | 구성 요소들을 병렬로 검증 |
| **적용 상황** | 각 필드별 복잡한 검증 | 전제 조건 기반 구성 요소 검증 |
| **성능** | 필드별 병렬 처리 | 전제 조건 후 구성 요소 병렬 처리 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **효율성** | 전제 조건 불만족 시 불필요한 검증 생략 | **복잡성** | 검증 구조가 복잡해짐 |
| **성능 최적화** | 전제 조건 후 구성 요소 병렬 검증 | **디버깅** | 중첩된 검증 로직 디버깅 어려움 |
| **논리적 일관성** | 전제 조건 기반의 논리적 검증 흐름 | **설계** | 전제 조건을 신중히 설계해야 함 |
| **실용성** | 복잡한 데이터 구조에서 자주 사용되는 패턴 | **테스트** | 각 검증 단계별 테스트 필요 |

## FAQ

### Q1: 언제 역방향 중첩 검증 패턴을 사용해야 하나요?
**A**: 전제 조건이 만족되어야만 후속 검증이 의미를 가지는 경우에 사용하세요. 마치 상태 머신처럼, 특정 조건이 만족되어야만 다음 단계로 진행할 수 있는 경우에 적합합니다. 예를 들어, 전화번호 검증에서 전체 형식이 유효해야만 국가코드, 지역코드, 로컬번호를 개별적으로 검증할 수 있는 경우입니다.

### Q2: 전제 조건과 구성 요소 검증의 차이점은?
**A**: 전제 조건은 전체 구조나 형식을 검증하는 것이고, 구성 요소 검증은 각 부분을 세밀하게 검증하는 것입니다. 마치 파서 컴포지터처럼, 전체 형식을 먼저 검증한 후 각 구성 요소를 세밀하게 검증합니다. 전제 조건이 만족되지 않으면 구성 요소 검증을 실행할 필요가 없습니다.

### Q3: Bind와 Apply의 순서는 어떻게 결정하나요?
**A**: 전제 조건이 되는 검증을 Bind로 먼저 실행하고, 그 결과가 성공하면 구성 요소들을 Apply로 병렬 검증합니다. 마치 파이프라인 아키텍처처럼, 전제 조건이 되는 검증을 먼저 수행하고, 그 결과를 바탕으로 복잡한 구성 요소 검증을 병렬로 수행합니다. 이렇게 하면 불필요한 복잡한 검증을 피할 수 있습니다.

### Q4: 에러 처리는 어떻게 구분하나요?
**A**: Bind 단계에서는 단일 Error 타입으로 전제 조건 실패를 처리하고, Apply 단계에서는 ManyErrors 타입으로 구성 요소들의 실패를 수집합니다. 마치 계층적 에러 처리처럼, 각 단계에서 발생하는 에러의 특성을 고려하여 적절히 처리해야 합니다. 사용자에게는 어떤 단계에서 실패했는지 명확하게 알려주는 것이 중요합니다.
