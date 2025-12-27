# Apply 병렬 검증 (Apply Parallel Validation)

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

이 프로젝트는 ValueObject에서 **서로 독립적인 검증 규칙들을 병렬로 실행**하는 Apply 패턴을 학습합니다. 사용자 등록 값 객체를 통해 모든 검증을 동시에 실행하여 모든 에러를 한 번에 수집하는 효율적인 검증 방식을 알아봅니다.

## 학습 목표

### **핵심 학습 목표**
1. **Apply 연산자의 병렬 실행 메커니즘 이해**: 서로 독립적인 검증 규칙들을 동시에 실행하는 구조를 파악합니다.
2. **에러 수집 패턴 구현**: 모든 검증 실패를 한 번에 수집하여 사용자에게 완전한 피드백을 제공합니다.
3. **ManyErrors 타입 활용**: 복수개의 에러를 구조화된 방식으로 처리하는 방법을 학습합니다.

### **실습을 통해 확인할 내용**
- **병렬 실행**: 이메일, 비밀번호, 이름, 나이 검증이 동시에 실행
- **에러 수집**: 모든 검증 실패를 한 번에 수집하여 표시
- **사용자 경험**: 모든 문제점을 한 번에 확인 가능

## 왜 필요한가?

이전 단계인 `Bind-Sequential-Validation`에서는 의존적인 검증 규칙들을 순차적으로 실행하는 방법을 학습했습니다. 하지만 실제로 **서로 독립적인 정보들을 검증**해야 하는 상황에서는 다른 접근이 필요합니다.

**첫 번째 문제는 사용자 경험의 개선입니다.** 마치 웹 폼의 유효성 검사처럼, 사용자가 여러 필드에 잘못된 값을 입력했을 때 모든 문제점을 한 번에 보여주는 것이 더 효율적입니다. 이메일 형식이 틀렸다고 해서 비밀번호나 이름 검증을 중단할 필요는 없습니다.

**두 번째 문제는 성능 최적화입니다.** 마치 병렬 처리 아키텍처처럼, 서로 독립적인 검증들을 동시에 실행하면 전체 검증 시간을 단축할 수 있습니다. 각 검증이 독립적이라면 순차 실행보다 병렬 실행이 더 효율적입니다.

**세 번째 문제는 에러 정보의 완전성입니다.** 마치 로깅 시스템처럼, 모든 검증 실패를 수집하여 구조화된 에러 정보를 제공해야 합니다. 사용자가 한 번에 모든 문제를 파악하고 수정할 수 있도록 도와야 합니다.

이러한 문제들을 해결하기 위해 **Apply 병렬 검증 패턴**을 도입했습니다. Apply를 사용하면 독립적인 검증 규칙들을 병렬로 실행하여 모든 에러를 수집할 수 있습니다.

## 핵심 개념

이 프로젝트의 핵심은 크게 2가지 개념으로 나눌 수 있습니다. 각각이 어떻게 작동하는지 쉽게 설명해드리겠습니다.

### Apply 병렬 실행 메커니즘

**핵심 아이디어는 "독립적 검증의 동시 실행"입니다.** 마치 병렬 처리 아키텍처처럼, 서로 의존성이 없는 검증 규칙들을 동시에 실행합니다.

예를 들어, 사용자 등록 검증을 생각해보세요. 마치 웹 폼의 유효성 검사처럼 각 필드가 독립적으로 검증되어야 합니다.

```csharp
// 이전 방식 (문제가 있는 방식) - 순차적으로 실행하여 비효율적
public static Validation<Error, UserRegistration> ValidateOld(string email, string password, string name, string ageInput)
{
    var emailResult = ValidateEmail(email);
    if (emailResult.IsFail) return emailResult; // 조기 중단으로 다른 검증 생략
    
    var passwordResult = ValidatePassword(password);
    if (passwordResult.IsFail) return passwordResult; // 조기 중단으로 다른 검증 생략
    // 사용자가 모든 문제를 한 번에 파악할 수 없음
}

// 개선된 방식 (현재 방식) - Apply를 통한 병렬 실행
public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
    string email, string password, string name, string ageInput) =>
    (ValidateEmailFormat(email), ValidatePasswordStrength(password), ValidateNameFormat(name), ValidateAgeFormat(ageInput))
        .Apply((validEmail, validPassword, validName, validAge) => 
            (Email: validEmail, Password: validPassword, Name: validName, Age: validAge))
        .As();
```

이 방식의 장점은 모든 검증을 동시에 실행하여 사용자가 모든 문제점을 한 번에 확인할 수 있다는 것입니다.

### 에러 수집 및 ManyErrors 처리

**핵심 아이디어는 "구조화된 에러 정보 제공"입니다.** 마치 로깅 시스템처럼, 모든 검증 실패를 체계적으로 수집하여 사용자에게 제공합니다.

```csharp
// ManyErrors를 통한 복수개 에러 처리
if (error is ManyErrors manyErrors)
{
    Console.WriteLine($"   → 총 {manyErrors.Errors.Count}개의 검증 실패:");
    for (int i = 0; i < manyErrors.Errors.Count; i++)
    {
        var individualError = manyErrors.Errors[i];
        if (individualError is ErrorCodeExpected errorCodeExpected)
        {
            Console.WriteLine($"     {i + 1}. 에러 코드: {errorCodeExpected.ErrorCode}");
            Console.WriteLine($"        현재 값: '{errorCodeExpected.ErrorCurrentValue}'");
        }
    }
}
```

이 방식의 장점은 사용자가 모든 문제점을 한 번에 파악하고 수정할 수 있다는 것입니다.

## 실전 지침

### 예상 출력
```
=== 독립 검증 (Independent Validation) 예제 ===
사용자 등록 값 객체의 모든 검증 규칙을 병렬로 실행합니다.

--- 유효한 사용자 등록 ---
이메일: 'newuser@example.com'
비밀번호: 'newpass123'
이름: '홍길동'
나이: '25'
성공: 사용자 등록이 유효합니다.
   → 등록된 사용자: 홍길동 (newuser@example.com)
   → 모든 독립 검증 규칙을 통과했습니다.

--- 모든 검증 동시 실패 (Apply의 핵심) ---
이메일: ''
비밀번호: 'short'
이름: 'A'
나이: 'abc'
실패:
   → 총 4개의 검증 실패:
     1. 에러 코드: DomainErrors.UserRegistration.EmailMissingAt
        현재 값: ''
     2. 에러 코드: DomainErrors.UserRegistration.PasswordTooShort
        현재 값: 'short'
     3. 에러 코드: DomainErrors.UserRegistration.NameTooShort
        현재 값: 'A'
     4. 에러 코드: DomainErrors.UserRegistration.AgeNotNumeric
        현재 값: 'abc'
```

### 핵심 구현 포인트
1. **Apply 튜플 구조**: 여러 검증을 튜플로 묶어서 병렬 실행
2. **에러 수집**: 모든 검증 실패를 ManyErrors로 수집
3. **사용자 경험**: 모든 문제점을 한 번에 표시

## 프로젝트 설명

### 프로젝트 구조
```
02-Apply-Parallel-Validation/
├── Program.cs              # 메인 실행 파일
├── ValueObjects/
│   └── UserRegistration.cs # 사용자 등록 값 객체 (Apply 패턴 구현)
├── ApplyParallelValidation.csproj
└── README.md               # 메인 문서
```

### 핵심 코드

**UserRegistration.cs - Apply 병렬 검증 구현**
```csharp
public sealed class UserRegistration : ValueObject
{
    public string Email { get; }
    public string Password { get; }
    public string Name { get; }
    public int Age { get; }

    // Apply를 통한 병렬 검증 구현
    public static Validation<Error, (string Email, string Password, string Name, int Age)> Validate(
        string email, string password, string name, string ageInput) =>
        // 핵심 검증 규칙들을 병렬로 실행 (독립적 유효성 검사)
        (ValidateEmailFormat(email), ValidatePasswordStrength(password), ValidateNameFormat(name), ValidateAgeFormat(ageInput))
            .Apply((validEmail, validPassword, validName, validAge) => 
                (Email: validEmail, Password: validPassword, Name: validName, Age: validAge))
            .As();

    // 독립적인 검증 메서드들
    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains("@") && email.Contains(".")
            ? email
            : DomainErrors.EmailMissingAt(email);

    private static Validation<Error, string> ValidatePasswordStrength(string password) =>
        password.Length >= 8
            ? password
            : DomainErrors.PasswordTooShort(password);
}
```

## 한눈에 보는 정리

### 비교 표
| 구분 | Bind 순차 검증 | Apply 병렬 검증 |
|------|----------------|----------------|
| **실행 방식** | 순차적으로 체이닝하여 실행 | 모든 검증을 동시에 실행 |
| **에러 처리** | 첫 번째 실패에서 조기 중단 | 모든 실패를 수집하여 반환 |
| **성능** | 조기 중단으로 효율적 | 병렬 실행으로 빠름 |
| **사용자 경험** | 한 번에 하나의 문제만 확인 | 모든 문제를 한 번에 확인 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **사용자 경험** | 모든 문제점을 한 번에 확인 가능 | **복잡성** | 에러 처리가 복잡함 |
| **성능** | 병렬 실행으로 빠른 검증 | **메모리** | 모든 에러를 메모리에 보관 |
| **완전성** | 모든 검증 실패를 수집 | **의존성** | 검증 규칙이 독립적이어야 함 |

## FAQ

### Q1: Apply와 Bind를 언제 선택해야 하나요?
**A**: 검증 규칙들 간의 의존성을 확인하세요. 서로 독립적이라면 Apply를, 이전 결과가 다음 검증에 영향을 미친다면 Bind를 사용하세요. 마치 병렬 처리와 순차 처리의 선택처럼, 독립적인 작업들은 병렬로, 의존적인 작업들은 순차로 처리하는 것이 효율적입니다.

### Q2: ManyErrors는 어떻게 처리하나요?
**A**: ManyErrors 타입을 확인하여 복수개의 에러를 순회하면서 각각을 처리합니다. 마치 컬렉션 처리처럼, 각 에러를 개별적으로 처리하여 사용자에게 구조화된 피드백을 제공합니다. ErrorCodeExpected 타입인지 확인하여 에러 코드와 현재 값을 표시하는 것이 좋습니다.

### Q3: Apply의 성능상 이점은 무엇인가요?
**A**: 서로 독립적인 검증들을 동시에 실행하여 전체 검증 시간을 단축할 수 있습니다. 마치 병렬 처리 아키텍처처럼, CPU의 여러 코어를 활용하거나 I/O 대기 시간을 최적화할 수 있습니다. 특히 외부 API 호출이나 복잡한 계산이 포함된 검증에서는 그 효과가 더욱 클 것입니다.

### Q4: 모든 검증이 실패하는 경우는 어떻게 처리하나요?
**A**: ManyErrors를 통해 모든 실패를 수집하여 사용자에게 완전한 피드백을 제공합니다. 마치 웹 폼의 유효성 검사처럼, 사용자가 모든 문제점을 한 번에 파악하고 수정할 수 있도록 도와야 합니다. 각 에러를 명확하게 구분하여 표시하는 것이 중요합니다.
