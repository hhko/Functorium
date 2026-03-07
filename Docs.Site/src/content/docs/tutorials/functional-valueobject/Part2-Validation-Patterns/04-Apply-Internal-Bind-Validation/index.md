---
title: "Apply 내부 Bind 검증"
---

## 개요

회원가입 폼에서 사용자명, 이메일, 비밀번호를 동시에 검증하고 싶습니다. 그런데 사용자명은 형식 검증 후 가용성 검증을, 이메일은 형식 검증 후 도메인 검증을, 비밀번호는 강도 검증 후 히스토리 검증을 거쳐야 합니다. 필드 간에는 독립적이지만, 각 필드 내부에서는 순차적 검증이 필요한 상황입니다. 외부에서는 Apply로 병렬 처리하고, 각 필드 내부에서는 Bind로 순차 처리하는 중첩 패턴으로 이 문제를 해결합니다.

## 학습 목표

- Apply 내부에서 Bind를 사용하여 복잡한 검증 로직을 단계별로 처리하는 **중첩 검증 패턴을** 구현할 수 있습니다.
- 각 필드마다 독립적인 복잡한 검증 로직을 구현하는 **필드별 세밀한 제어를** 적용할 수 있습니다.
- 병렬과 순차 검증의 장점을 조합하여 **성능과 정확성의 균형을** 달성할 수 있습니다.

## 왜 필요한가?

이전 단계에서는 독립적인 정보와 의존적인 정보를 2단계로 나누어 검증했습니다. 하지만 각 필드마다 복잡한 다단계 검증이 필요한 상황에서는 다른 접근이 필요합니다.

사용자명은 형식 검증 후 가용성 검증을 거쳐야 하고, 이메일은 형식 검증 후 도메인 검증을, 비밀번호는 강도 검증 후 히스토리 검증을 거쳐야 합니다. 필드들은 서로 독립적이므로 병렬로 검증할 수 있지만, 각 필드 내부에서는 형식이 유효해야만 비즈니스 규칙 검증이 의미를 가지므로 순차적 검증이 필요합니다. 또한 여러 필드에서 동시에 발생하는 에러를 수집하되, 각 필드의 에러가 형식 오류인지 비즈니스 규칙 위반인지 구분할 수 있어야 합니다.

**Apply 내부 Bind 중첩 검증 패턴은** 이러한 복합적인 필드별 검증 요구사항을 효율적으로 처리합니다.

## 핵심 개념

### 중첩 검증 구조

외부 Apply가 필드들을 병렬로 검증하고, 각 필드 내부의 Bind가 단계별 순차 검증을 수행합니다.

다음 코드는 단순 처리 방식과 중첩 검증 방식을 비교합니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 모든 검증을 단순하게 처리
public static Validation<Error, MemberRegistration> ValidateOld(string username, string email, string password)
{
    // 각 필드의 복잡한 검증을 하나의 메서드로 처리하여 복잡함
    var usernameResult = ValidateUsernameComplex(username);
    var emailResult = ValidateEmailComplex(email);
    var passwordResult = ValidatePasswordComplex(password);
    // 복잡한 로직이 하나의 메서드에 집중되어 가독성 저하
}

// 개선된 방식 (현재 방식) - 중첩 검증 구조
public static Validation<Error, (string Username, string Email, string Password)> Validate(
    string username, string email, string password) =>
    // 외부 Apply - 3개 필드를 병렬로 검증하되, 각각 내부에서 Bind를 사용
    (ValidateUsername(username), ValidateEmail(email), ValidatePassword(password))
        .Apply((validUsername, validEmail, validPassword) =>
            (Username: validUsername, Email: validEmail, Password: validPassword))
        .As();
```

외부 Apply가 세 필드를 동시에 검증하므로, 사용자명과 이메일 모두 형식 오류가 있다면 두 에러가 함께 수집됩니다. 동시에 각 필드 내부에서는 Bind가 형식 검증 실패 시 비즈니스 규칙 검증을 건너뜁니다.

### 필드별 세밀한 검증 제어

각 필드의 검증을 형식 검증과 비즈니스 규칙 검증이라는 두 단계로 분해합니다. Bind 덕분에 형식이 유효할 때만 비즈니스 규칙 검증이 실행됩니다.

```csharp
// 사용자명 검증 - 내부에서 Bind 사용 (2단계 검증)
private static Validation<Error, string> ValidateUsername(string username) =>
    ValidateUsernameFormat(username)
        .Bind(_ => ValidateUsernameAvailability(username));

// 이메일 검증 - 내부에서 Bind 사용 (2단계 검증)
private static Validation<Error, string> ValidateEmail(string email) =>
    ValidateEmailFormat(email)
        .Bind(_ => ValidateEmailDomain(email));

// 비밀번호 검증 - 내부에서 Bind 사용 (2단계 검증)
private static Validation<Error, string> ValidatePassword(string password) =>
    ValidatePasswordStrength(password)
        .Bind(_ => ValidatePasswordHistory(password));
```

각 필드의 검증 로직을 독립적으로 관리하고 테스트할 수 있다는 것이 이 구조의 장점입니다.

## 실전 지침

### 예상 출력
```
=== 중첩 검증 (Nested Validation) 예제 ===
회원가입 정보 값 객체의 Apply 내부에서 Bind를 사용한 중첩 검증 예제입니다.

--- 유효한 회원가입 정보 ---
사용자명: 'john_doe'
이메일: 'john@example.com'
비밀번호: 'SecurePass123'
 ✅ 성공: 회원가입 정보가 유효합니다.
   → 사용자: john_doe (john@example.com)
   → 모든 중첩 검증 규칙을 통과했습니다.

--- 사용자명과 이메일 형식 오류 ---
사용자명: 'ab'
이메일: 'invalid-email'
비밀번호: 'SecurePass123'
 ❌ 실패:
   → 총 2개의 검증 실패:
     1. 에러 코드: DomainErrors.MemberRegistration.UsernameTooShort
        현재 값: 'ab'
     2. 에러 코드: DomainErrors.MemberRegistration.EmailMissingAt
        현재 값: 'invalid-email'
```

### 핵심 구현 포인트

구현 시 세 가지 포인트에 주목합니다. 외부 Apply로 3개 필드를 병렬 검증하고, 각 필드 내부에서 Bind를 사용하여 2단계 순차 검증을 수행하며, 여러 필드에서 동시 발생하는 에러를 ManyErrors로 수집합니다.

## 프로젝트 설명

### 프로젝트 구조
```
04-Apply-Internal-Bind-Validation/
├── Program.cs              # 메인 실행 파일
├── ValueObjects/
│   └── MemberRegistration.cs # 회원가입 값 객체 (중첩 검증 패턴 구현)
├── ApplyInternalBindValidation.csproj
└── README.md               # 메인 문서
```

### 핵심 코드

MemberRegistration 값 객체는 외부 Apply로 필드를 병렬 검증하고, 각 필드 내부에서 Bind로 형식과 비즈니스 규칙을 순차 검증합니다.

```csharp
public sealed class MemberRegistration : ValueObject
{
    public string Username { get; }
    public string Email { get; }
    public string Password { get; }

    // 중첩 검증 패턴 구현 (Apply 내부에서 Bind 사용)
    public static Validation<Error, (string Username, string Email, string Password)> Validate(
        string username, string email, string password) =>
        // 외부 Apply - 3개 필드를 병렬로 검증하되, 각각 내부에서 Bind를 사용
        (ValidateUsername(username), ValidateEmail(email), ValidatePassword(password))
            .Apply((validUsername, validEmail, validPassword) =>
                (Username: validUsername, Email: validEmail, Password: validPassword))
            .As();

    // 사용자명 검증 (독립) - 내부에서 Bind 사용 (2단계 검증)
    private static Validation<Error, string> ValidateUsername(string username) =>
        ValidateUsernameFormat(username)
            .Bind(_ => ValidateUsernameAvailability(username));

    // 이메일 검증 (독립) - 내부에서 Bind 사용 (2단계 검증)
    private static Validation<Error, string> ValidateEmail(string email) =>
        ValidateEmailFormat(email)
            .Bind(_ => ValidateEmailDomain(email));

    // 비밀번호 검증 (독립) - 내부에서 Bind 사용 (2단계 검증)
    private static Validation<Error, string> ValidatePassword(string password) =>
        ValidatePasswordStrength(password)
            .Bind(_ => ValidatePasswordHistory(password));

    // 세부 검증 메서드들
    private static Validation<Error, string> ValidateUsernameFormat(string username) =>
        !string.IsNullOrWhiteSpace(username) && username.Length >= 3
            ? username
            : DomainErrors.UsernameTooShort(username);

    private static Validation<Error, string> ValidateUsernameAvailability(string username) =>
        !username.StartsWith("admin")
            ? username
            : DomainErrors.UsernameNotAvailable(username);
}
```

## 한눈에 보는 정리

다음 표는 Apply 내부 Bind 중첩 검증과 기존 패턴의 차이를 비교합니다.

| 구분 | Apply 병렬 검증 | Bind 순차 검증 | Apply 내부 Bind 중첩 검증 |
|------|----------------|----------------|-------------------------|
| **외부 구조** | 모든 검증을 병렬 실행 | 모든 검증을 순차 실행 | 필드들을 병렬로 검증 |
| **내부 구조** | 단순한 개별 검증 | 단순한 개별 검증 | 각 필드 내부에서 순차 검증 |
| **복잡성** | 낮음 | 낮음 | 높음 (필드별 복잡한 로직) |
| **성능** | 병렬 실행으로 빠름 | 조기 중단으로 효율적 | 병렬과 순차의 조합 |

다음 표는 이 패턴의 장단점을 정리합니다.

| 장점 | 단점 |
|------|------|
| 각 필드별로 복잡한 검증 로직 구현 가능 | 검증 구조가 복잡해짐 |
| 병렬과 순차 검증의 장점을 조합 | 중첩된 검증 로직 디버깅 어려움 |
| 새로운 필드나 검증 단계 추가 용이 | 검증 계층을 신중히 설계해야 함 |

## FAQ

### Q1: 언제 중첩 검증 패턴을 사용해야 하나요?
**A:** 각 필드마다 복잡한 다단계 검증이 필요할 때 사용합니다. 사용자명은 형식 검증 후 가용성 검증을, 이메일은 형식 검증 후 도메인 검증을 거쳐야 하는 경우가 대표적입니다.

### Q2: 외부 Apply와 내부 Bind의 역할은 무엇인가요?
**A:** 외부 Apply는 필드들을 병렬로 검증하여 성능을 최적화하고, 내부 Bind는 각 필드의 검증 로직을 단계별로 처리합니다. 여러 필드를 동시에 처리하되 각 필드 내부에서는 순차적으로 처리하여 성능과 정확성을 모두 확보합니다.

### Q3: 에러 처리는 어떻게 하나요?
**A:** ManyErrors 타입을 통해 여러 필드에서 동시에 발생하는 에러를 수집합니다. 형식 오류와 비즈니스 규칙 위반을 구분하여 사용자에게 명확한 피드백을 제공하는 것이 중요합니다.

이 장에서는 외부 Apply, 내부 Bind 구조를 다뤘습니다. 그렇다면 반대 방향 -- 외부 Bind로 전제 조건을 먼저 검증하고, 내부 Apply로 구성 요소를 병렬 검증하는 구조는 어떤 상황에서 유용할까요? 다음 장에서 이 역방향 중첩 패턴을 살펴봅니다.
