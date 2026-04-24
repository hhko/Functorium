---
title: "복합 값 객체"
---

> `ValueObject`

## 개요

이메일 주소를 문자열 하나로 다루다 보면 로컬 부분(`user`)과 도메인 부분(`example.com`)의 검증 로직이 뒤섞이고, 각 부분을 독립적으로 재사용하기 어려워집니다. 복합 값 객체(Composite Value Object)는 작은 값 객체들을 조합하여 이런 복잡한 도메인 개념을 구조적으로 표현합니다.

## 학습 목표

1. 여러 값 객체를 조합하여 복잡한 도메인 개념을 표현할 수 있습니다.
2. LINQ Expression으로 계층적 검증 로직을 구현할 수 있습니다.
3. `GetEqualityComponents()`를 오버라이드하여 복합 동등성을 정의할 수 있습니다.
4. 작은 값 객체들을 모듈화하여 다른 맥락에서 재사용할 수 있습니다.

## 왜 필요한가?

이전 단계인 `04-ComparableValueObject-Primitive`에서는 여러 primitive 타입을 직접 조합하여 복합 데이터를 표현했습니다. 하지만 실제 애플리케이션에서는 더 복잡한 도메인 개념들이 등장합니다.

이메일 주소처럼 로컬 부분과 도메인 부분이 각각 다른 규칙을 가지는 경우, 이를 하나의 단위로 다루어야 합니다. 형식 검증, 로컬 부분 검증, 도메인 검증이 순차적으로 이루어져야 하고, 이메일 로컬 부분이나 도메인은 다른 곳에서도 독립적으로 재사용될 수 있어야 합니다.

복합 값 객체(Composite Value Object)는 이런 요구를 충족합니다. `EmailLocalPart`와 `EmailDomain`이라는 독립적인 값 객체를 정의하고, 이를 조합하여 `Email`이라는 상위 개념을 만듭니다. 각 구성 요소는 자체 검증 로직을 가지며, 전체 `Email`은 구성 요소들의 조합으로 동등성을 판단합니다.

## 핵심 개념

### 값 객체 컴포지션

`EmailLocalPart`와 `EmailDomain`이라는 두 개의 작은 값 객체를 조합하여 `Email`이라는 더 큰 개념을 만듭니다. 각 부분은 독립적인 값 객체로 존재하지만, 함께 조합되어 이메일이라는 더 큰 개념을 형성합니다.

```csharp
// 개별 값 객체들
EmailLocalPart localPart = EmailLocalPart.Create("user");
EmailDomain domain = EmailDomain.Create("example.com");

// 복합 값 객체
Email email = Email.Create("user@example.com");
```

이러한 컴포지션은 코드의 모듈성과 재사용성을 크게 향상시킵니다. 작은 값 객체들은 다른 맥락에서도 재사용될 수 있습니다.

### 계층적 검증 로직

복합 값 객체는 여러 단계의 검증 로직을 가지고 있습니다. 이메일 검증은 형식 검증, 분할 검증, 로컬 부분 검증, 도메인 검증이 계층적으로 진행됩니다.

LINQ Expression의 `from-in` 체인을 사용하면 이 검증 단계들을 선언적으로 표현할 수 있습니다.

```csharp
// 계층적 검증
public static Validation<Error, (EmailLocalPart, EmailDomain)> Validate(string email) =>
    from validEmail in ValidateEmailFormat(email)        // 1. 형식 검증
    from validParts in ValidateEmailParts(validEmail)     // 2. 분할 검증
    select validParts;                                     // 결과 조합
```

각 단계에서 실패하면 이후 단계는 실행되지 않으므로, 복잡한 비즈니스 규칙을 체계적으로 구현할 수 있습니다.

### 복합 동등성

복합 값 객체의 동등성은 모든 구성 요소들의 동등성을 종합적으로 판단합니다. 이메일 주소의 동등성은 로컬 부분과 도메인 부분이 모두 같을 때 성립합니다.

`GetEqualityComponents()`에서 반환하는 모든 요소가 pairwise로 같아야 두 객체가 동일합니다.

```csharp
protected override IEnumerable<object> GetEqualityComponents()
{
    yield return LocalPart;  // 로컬 부분 비교
    yield return Domain;     // 도메인 부분 비교
}
```

## 실전 지침

### 예상 출력
```
=== 5. 비교 불가능한 복합 값 객체 - ValueObject ===
부모 클래스: ValueObject
예시: Email (이메일 주소) - EmailLocalPart + EmailDomain 조합

📋 특징:
   ✅ 복잡한 검증 로직을 가진 값 객체
   ✅ 동등성 비교만 제공
   ✅ 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현
   ✅ EmailLocalPart + EmailDomain = Email

🔍 성공 케이스:
   ✅ Email: user@example.com
     - LocalPart: user
     - Domain: example.com

   ✅ Email: user@example.com
     - LocalPart: user
     - Domain: example.com

   ✅ Email: admin@test.org
     - LocalPart: admin
     - Domain: test.org

📊 동등성 비교:
   user@example.com == user@example.com = True
   user@example.com == admin@test.org = False

🔢 해시코드:
   user@example.com.GetHashCode() = -1711187277
   user@example.com.GetHashCode() = -1711187277
   동일한 값의 해시코드가 같은가? True

❌ 실패 케이스:
   Email("invalid-email"): InvalidEmailFormat
   Email("@example.com"): EmptyOrOutOfRange
   Email("user@"): EmptyOrInvalidFormat

💡 복합 값 객체의 특징:
   - EmailLocalPart와 EmailDomain은 각각 독립적인 값 객체
   - Email은 이 두 값 객체를 조합하여 더 복잡한 도메인 개념 표현
   - 각 구성 요소는 자체적인 검증 로직을 가짐
   - 전체 Email은 구성 요소들의 조합으로 동등성 비교

✅ 데모가 성공적으로 완료되었습니다!
```

### 핵심 구현 포인트

다음 네 가지가 복합 값 객체 구현의 핵심입니다.

| 포인트 | 설명 |
|--------|------|
| **계층적 값 객체 구조** | EmailLocalPart + EmailDomain -> Email |
| **LINQ Expression 계층적 검증** | from-in 체인으로 복합 검증 구현 |
| **GetEqualityComponents() 복합 구현** | 여러 구성 요소의 동등성 정의 |
| **모듈성** | 작은 값 객체들의 재사용성 보장 |

## 프로젝트 설명

### 프로젝트 구조
```
05-ValueObject-Composite/
├── Program.cs                    # 메인 실행 파일
├── ValueObjectComposite.csproj  # 프로젝트 파일
├── ValueObjects/
│   ├── Email.cs                 # 복합 이메일 값 객체
│   ├── EmailLocalPart.cs        # 이메일 로컬 부분 값 객체
│   └── EmailDomain.cs           # 이메일 도메인 값 객체
└── README.md                    # 프로젝트 문서
```

### 핵심 코드

`EmailLocalPart`는 이메일 로컬 부분을 독립된 값 객체로 표현합니다.

**EmailLocalPart.cs - 기본 값 객체**
```csharp
public sealed class EmailLocalPart : SimpleValueObject<string>
{
    private EmailLocalPart(string value) : base(value) { }

    public static Fin<EmailLocalPart> Create(string value) =>
        CreateFromValidation(Validate(value), v => new EmailLocalPart(v));

    public static EmailLocalPart CreateFromValidated(string validatedValue) =>
        new(validatedValue);

    public static Validation<Error, string> Validate(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 1 && value.Length <= 64
            ? value
            : DomainError.For<EmailLocalPart>(new DomainErrorKind.WrongLength(), value,
                $"Email local part is empty or out of range. Must be 1-64 characters. Current value: '{value}'");

    public override string ToString() => Value;
}
```

`Email`은 `EmailLocalPart`와 `EmailDomain`을 조합하여 복합 동등성과 계층적 검증을 제공합니다.

**Email.cs - 복합 값 객체**
```csharp
public sealed class Email : ValueObject
{
    public EmailLocalPart LocalPart { get; }
    public EmailDomain Domain { get; }

    private Email(EmailLocalPart localPart, EmailDomain domain)
    {
        LocalPart = localPart;
        Domain = domain;
    }

    // 계층적 검증
    public static Validation<Error, (EmailLocalPart, EmailDomain)> Validate(string email) =>
        from validEmail in ValidateEmailFormat(email)      // 1. 형식 검증
        from validParts in ValidateEmailParts(validEmail)   // 2. 분할 검증
        select validParts;                                   // 결과 조합

    // 복합 동등성
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LocalPart;
        yield return Domain;
    }
}
```

**Program.cs - 복합 값 객체 데모**
```csharp
// 복합 값 객체 생성
var email1 = Email.Create("user@example.com");
var email2 = Email.Create("user@example.com");

// 동등성 비교
var e1 = email1.Match(Succ: x => x, Fail: _ => default!);
var e2 = email2.Match(Succ: x => x, Fail: _ => default!);
Console.WriteLine($"   {e1} == {e2} = {e1 == e2}");
```

## 한눈에 보는 정리

Primitive 직접 조합 방식과 값 객체 컴포지션 방식의 차이를 비교합니다.

### 비교 표
| 구분 | ValueObject-Primitive | ValueObject-Composite |
|------|----------------------|---------------------|
| **구성 요소** | Primitive 타입 직접 사용 | 값 객체 컴포지션 |
| **검증 복잡성** | 단일 단계 검증 | 계층적 검증 |
| **재사용성** | 제한적 | 높음 (컴포넌트 재사용) |
| **모듈성** | 낮음 | 높음 |
| **유지보수성** | 보통 | 높음 |

### 장단점 표
| 장점 | 단점 |
|------|------|
| **높은 모듈성** | 구현 복잡도 증가 |
| **컴포넌트 재사용** | 계층 구조 복잡 |
| **유지보수성 향상** | 학습 곡선 존재 |
| **도메인 표현력** | 성능 오버헤드 |

## FAQ

### Q1: 복합 값 객체와 일반 클래스의 차이점은 무엇인가요?
**A**: 복합 값 객체는 구성 요소들의 불변성과 값 기반 동등성을 강제합니다. 생성 후 변경할 수 없으며, `GetEqualityComponents()`를 통해 동등성 비교 방식을 명시적으로 정의합니다. 일반 클래스는 이러한 제약이 없어 구성 요소를 자유롭게 변경할 수 있지만, 동등성과 불변성을 보장하지 않습니다.

### Q2: 왜 계층적 검증을 사용하나요?
**A**: 복잡한 비즈니스 규칙을 단계별로 명확하게 구현할 수 있기 때문입니다. 이메일 검증에서 먼저 기본 형식을 확인하고, 그 다음에 각 부분의 유효성을 검증하면 디버깅과 유지보수가 용이합니다. 각 검증 단계는 독립적으로 테스트하고 재사용할 수 있습니다.

### Q3: GetEqualityComponents()는 어떻게 복합 동등성을 구현하나요?
**A**: `GetEqualityComponents()`는 복합 값 객체의 모든 구성 요소를 순차적으로 반환합니다. 두 복합 값 객체가 동일하려면 반환된 모든 요소가 pairwise로 같아야 합니다. 이메일의 경우 로컬 부분과 도메인 부분이 모두 같아야 동일한 이메일로 취급됩니다.

지금까지 동등성만 지원하는 복합 값 객체를 살펴보았습니다. 다음 장에서는 `ComparableValueObject`를 상속하여 복합 값 객체에 정렬과 비교 기능을 추가하는 방법을 다룹니다.

---

→ [6장: ComparableValueObject (Composite)](../06-ComparableValueObject-Composite/)
