# Apply 내부 Bind 중첩 검증 (Apply Internal Bind Validation)

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

이 프로젝트는 ValueObject에서 **외부에서는 병렬 검증을 수행하되, 각 검증 내부에서는 순차적 검증을 수행**하는 중첩 검증 패턴을 학습합니다. 회원가입 값 객체를 통해 각 필드별로 복잡한 검증 로직을 가진 상황에서 어떻게 효율적으로 검증하는지 알아봅니다.

## 학습 목표

### **핵심 학습 목표**
1. **중첩 검증 패턴 구현**: Apply 내부에서 Bind를 사용하여 복잡한 검증 로직을 단계별로 처리합니다.
2. **필드별 세밀한 제어**: 각 필드마다 독립적인 복잡한 검증 로직을 구현합니다.
3. **성능과 정확성의 균형**: 병렬과 순차 검증의 장점을 조합하여 최적화합니다.

### **실습을 통해 확인할 내용**
- **외부 병렬**: 사용자명, 이메일, 비밀번호를 동시에 검증
- **내부 순차**: 각 필드 내부에서 2단계 검증 (형식 → 비즈니스 규칙)
- **복합 에러**: 여러 필드에서 동시에 발생하는 에러 수집

## 왜 필요한가?

이전 단계인 `Apply-Bind-Combined-Validation`에서는 독립적인 정보와 의존적인 정보를 2단계로 나누어 검증했습니다. 하지만 실제로 **각 필드마다 복잡한 다단계 검증이 필요한** 상황에서는 다른 접근이 필요합니다.

**첫 번째 문제는 필드별 복잡한 검증 요구사항입니다.** 마치 계층적 아키텍처처럼, 각 필드가 여러 단계의 검증을 거쳐야 합니다. 예를 들어, 사용자명은 형식 검증 후 가용성 검증을, 이메일은 형식 검증 후 도메인 검증을, 비밀번호는 강도 검증 후 히스토리 검증을 거쳐야 합니다.

**두 번째 문제는 검증 성능의 최적화입니다.** 마치 병렬 처리 아키텍처처럼, 필드들은 독립적이므로 병렬로 검증할 수 있지만, 각 필드 내부에서는 순차적 검증이 필요합니다. 이는 마치 웹 서버의 스레드 풀처럼, 여러 요청을 동시에 처리하되 각 요청 내부에서는 순차적으로 처리하는 것과 같습니다.

**세 번째 문제는 에러 정보의 세밀함입니다.** 마치 로깅 시스템처럼, 각 필드에서 발생하는 다양한 종류의 에러를 구분하여 사용자에게 명확한 피드백을 제공해야 합니다. 형식 오류와 비즈니스 규칙 위반을 구분하여 표시해야 합니다.

이러한 문제들을 해결하기 위해 **Apply 내부 Bind 중첩 검증 패턴**을 도입했습니다. 이 패턴을 사용하면 복잡한 필드별 검증 로직을 효율적으로 처리할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 2가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### 중첩 검증 구조

**핵심 아이디어는 "계층적 검증 아키텍처"입니다.** 마치 마이크로서비스 아키텍처처럼, 외부에서는 병렬로 처리하되 내부에서는 순차적으로 처리합니다.

예를 들어, 회원가입 검증을 생각해보세요. 마치 웹 서버의 스레드 풀처럼, 여러 필드를 동시에 처리하되 각 필드 내부에서는 단계별로 검증합니다.

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

이 방식의 장점은 각 필드의 복잡한 검증 로직을 단계별로 분해하여 관리할 수 있다는 것입니다.

### 필드별 세밀한 검증 제어

**핵심 아이디어는 "검증 로직의 모듈화"입니다.** 마치 함수형 프로그래밍의 합성처럼, 복잡한 검증을 단순한 검증들의 조합으로 구성합니다.

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

이 방식의 장점은 각 필드의 검증 로직을 독립적으로 관리하고 테스트할 수 있다는 것입니다.

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
1. **외부 Apply**: 3개 필드를 병렬로 검증
2. **내부 Bind**: 각 필드 내부에서 2단계 순차 검증
3. **에러 수집**: 여러 필드에서 동시 발생하는 에러를 ManyErrors로 수집

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

**MemberRegistration.cs - 중첩 검증 패턴 구현**
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

### 비교 표
| 구분 | Apply 병렬 검증 | Bind 순차 검증 | Apply 내부 Bind 중첩 검증 |
|------|----------------|----------------|-------------------------|
| **외부 구조** | 모든 검증을 병렬 실행 | 모든 검증을 순차 실행 | 필드들을 병렬로 검증 |
| **내부 구조** | 단순한 개별 검증 | 단순한 개별 검증 | 각 필드 내부에서 순차 검증 |
| **복잡성** | 낮음 | 낮음 | 높음 (필드별 복잡한 로직) |
| **성능** | 병렬 실행으로 빠름 | 조기 중단으로 효율적 | 병렬과 순차의 조합 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **세밀한 제어** | 각 필드별로 복잡한 검증 로직 구현 가능 | **복잡성** | 검증 구조가 복잡해짐 |
| **성능 최적화** | 병렬과 순차 검증의 장점을 조합 | **디버깅** | 중첩된 검증 로직 디버깅 어려움 |
| **확장성** | 새로운 필드나 검증 단계 추가 용이 | **설계** | 검증 계층을 신중히 설계해야 함 |
| **실용성** | 복잡한 도메인 로직에서 자주 사용되는 패턴 | **테스트** | 각 검증 단계별 테스트 필요 |

## FAQ

### Q1: 언제 중첩 검증 패턴을 사용해야 하나요?
**A**: 각 필드마다 복잡한 다단계 검증이 필요할 때 사용하세요. 마치 계층적 아키텍처처럼, 외부에서는 독립적으로 처리하되 내부에서는 순차적으로 처리해야 하는 경우에 적합합니다. 예를 들어, 사용자명은 형식 검증 후 가용성 검증을, 이메일은 형식 검증 후 도메인 검증을 거쳐야 하는 경우입니다.

### Q2: 외부 Apply와 내부 Bind의 역할은 무엇인가요?
**A**: 외부 Apply는 필드들을 병렬로 검증하여 성능을 최적화하고, 내부 Bind는 각 필드의 복잡한 검증 로직을 단계별로 처리합니다. 마치 웹 서버의 스레드 풀처럼, 여러 요청을 동시에 처리하되 각 요청 내부에서는 순차적으로 처리하는 것과 같습니다. 이렇게 하면 성능과 정확성을 모두 확보할 수 있습니다.

### Q3: 에러 처리는 어떻게 하나요?
**A**: ManyErrors 타입을 통해 여러 필드에서 동시에 발생하는 에러를 수집합니다. 마치 로깅 시스템처럼, 각 필드에서 발생하는 다양한 종류의 에러를 구분하여 사용자에게 명확한 피드백을 제공합니다. 형식 오류와 비즈니스 규칙 위반을 구분하여 표시하는 것이 중요합니다.

### Q4: 검증 로직을 어떻게 설계해야 하나요?
**A**: 각 필드의 검증을 형식 검증과 비즈니스 규칙 검증으로 나누어 설계하세요. 마치 함수형 프로그래밍의 합성처럼, 복잡한 검증을 단순한 검증들의 조합으로 구성합니다. 이렇게 하면 각 검증 로직을 독립적으로 관리하고 테스트할 수 있으며, 새로운 검증 단계를 추가하기도 쉬워집니다.
