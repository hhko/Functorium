---
title: "값 객체: Union 타입"
---

이 문서는 Discriminated Union 패턴으로 도메인 상태를 안전하게 표현하는 Union 값 객체의 설계와 구현을 다룹니다. 값 객체 핵심 개념은 [05a-value-objects](../05a-value-objects), 열거형·검증 패턴은 [05b-value-objects-validation](../05b-value-objects-validation)을 참고하세요.

## 들어가며

"연락처에 이메일도 없고 주소도 없는 상태가 왜 가능한가?"
"이미 인증된 이메일에 다시 인증 요청이 들어와도 코드가 허용한다."
"새 연락처 유형을 추가했는데, 기존 분기문 중 하나를 업데이트하지 않아서 런타임 오류가 발생했다."

이러한 문제들은 열거형이나 nullable 필드로 도메인 상태를 표현할 때 반복적으로 발생합니다. Union 값 객체는 **허용되는 상태 조합만 타입으로 표현하여** 잘못된 상태를 컴파일 타임에 차단합니다.

### 이 문서에서 배우는 내용

이 문서를 통해 다음을 학습합니다:

1. **Discriminated Union이 필요한 이유** - enum, SmartEnum과의 차이점
2. **기반 클래스 선택 기준** - `UnionValueObject`와 `UnionValueObject<TSelf>`의 사용 시나리오
3. **순수 데이터 Union 구현** - 상태 전이 없이 허용 조합만 표현하는 패턴
4. **상태 전이 Union 구현** - `TransitionFrom` 헬퍼로 안전한 상태 전이
5. **`[UnionType]` 소스 생성기** - Match/Switch 자동 생성으로 exhaustiveness 보장
6. **Aggregate에서 Union 활용** - 가드 + 전이 위임 패턴

### 사전 지식

이 문서를 이해하기 위해 다음 개념에 대한 기본적인 이해가 필요합니다:

- [값 객체 (Value Object)](../05a-value-objects)의 Create/Validate 분리 패턴
- C# record 타입과 패턴 매칭
- LanguageExt의 `Fin<T>` 기본 개념

> Union 값 객체는 **"잘못된 상태를 표현 불가능하게 만드는"** DDD 설계 원칙의 핵심 구현입니다. 허용되는 상태 조합만 타입으로 정의하면, 런타임 검증 없이 컴파일 타임에 안전성을 확보할 수 있습니다.

## 요약

### 주요 명령

```csharp
// 순수 데이터 Union 정의
[UnionType]
public abstract partial record ContactInfo : UnionValueObject { ... }

// 상태 전이 Union 정의
[UnionType]
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState> { ... }

// 상태 전이 실행
Fin<Verified> result = emailState.Verify(verifiedAt);

// Match로 모든 케이스 처리
string display = contactInfo.Match(
    emailOnly: eo => eo.EmailState.ToString(),
    postalOnly: po => po.Address.ToString(),
    emailAndPostal: ep => $"{ep.EmailState}, {ep.Address}");
```

### 주요 절차

**1. Union 값 객체 정의:**
1. 순수 데이터 / 상태 전이 여부에 따라 기반 클래스 선택
2. `abstract partial record` + `[UnionType]` 선언
3. `sealed record` 케이스 정의 + `private` 생성자
4. (상태 전이 시) 전이 메서드에서 `TransitionFrom` 호출

**2. Aggregate에서 활용:**
1. Aggregate 메서드에서 가드 조건 검증 (삭제 상태 등)
2. `Match`로 Union에서 필요한 상태 추출
3. 상태 전이를 Union 객체에 위임

### 주요 개념

| 개념 | 설명 |
|------|------|
| `UnionValueObject` | 순수 데이터 Union의 기반 클래스 |
| `UnionValueObject<TSelf>` | 상태 전이 지원 Union의 기반 클래스 (CRTP) |
| `[UnionType]` | Match/Switch/Is/As 메서드를 자동 생성하는 소스 생성기 |
| `TransitionFrom` | 타입 안전한 상태 전이 헬퍼 |
| `Match<TResult>` | 모든 케이스를 빠짐없이 처리하도록 강제하는 메서드 |

---

## Discriminated Union이 필요한 이유

도메인에서 **고정된 선택지**를 표현할 때 여러 선택지가 있습니다. 각 선택지마다 **서로 다른 데이터 구조**를 가져야 하는지가 핵심 기준입니다.

| 특성 | C# `enum` | `SmartEnum` | `UnionValueObject` |
|------|-----------|------------|---------------------|
| 값마다 다른 데이터 | 불가 | 고정 속성만 | 케이스별 고유 필드 |
| 상태 전이 로직 | 외부에서 처리 | 외부에서 처리 | 내부 `TransitionFrom` |
| 컴파일 타임 exhaustiveness | `switch` 경고 | 불가 | `Match` 메서드 강제 |
| 케이스별 행위 | 불가 | 메서드 오버라이드 | 패턴 매칭 |
| 사용 시나리오 | 단순 플래그 | 값 + 속성 | 구조적 상태 분기 |

**선택 기준:**
- 모든 값이 **같은 데이터 구조**를 공유 → `enum` 또는 `SmartEnum`
- 값마다 **서로 다른 데이터**를 가짐 → `UnionValueObject`

---

## 기반 클래스 선택

`IUnionValueObject` → `UnionValueObject` → `UnionValueObject<TSelf>` 계층에서, 상태 전이 필요 여부로 기반 클래스를 선택합니다.

```
IUnionValueObject (마커 인터페이스)
  └─ UnionValueObject (순수 데이터 Union)
       └─ UnionValueObject<TSelf> (상태 전이 Union, CRTP)
```

| 조건 | 선택 |
|------|------|
| 허용 조합만 표현 (상태 전이 없음) | `UnionValueObject` |
| 상태 전이 로직 필요 | `UnionValueObject<TSelf>` |

`UnionValueObject<TSelf>`는 CRTP(Curiously Recurring Template Pattern)를 사용하여, `TransitionFrom` 헬퍼가 `DomainError`에 정확한 타입 정보를 포함할 수 있게 합니다.

---

## 순수 데이터 Union 구현

상태 전이 없이 **허용되는 조합만 타입으로 표현**하는 패턴입니다.

### 구현 규칙

1. `abstract partial record` + `[UnionType]` 선언
2. `UnionValueObject` 상속
3. 케이스를 `sealed record`로 정의
4. `private` 생성자로 외부 확장 차단

### 예제: ContactInfo

연락처 정보는 "이메일만", "우편만", "이메일+우편" 중 하나여야 합니다. "연락 수단 없음"은 구조적으로 불가능합니다.

```csharp
[UnionType]
public abstract partial record ContactInfo : UnionValueObject
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
    public sealed record EmailAndPostal(EmailVerificationState EmailState, PostalAddress Address) : ContactInfo;

    private ContactInfo() { }
}
```

- `private` 생성자로 외부에서 새 케이스를 추가할 수 없습니다
- 세 케이스 중 하나만 선택 가능하므로 "연락 수단 없음" 상태가 불가능합니다
- `record`이므로 값 기반 동등성이 자동으로 제공됩니다

---

## 상태 전이 Union 구현

상태 간 **유효한 전이만 허용**하는 패턴입니다. `UnionValueObject<TSelf>`를 상속하여 `TransitionFrom` 헬퍼를 사용합니다.

### TransitionFrom 헬퍼

```csharp
protected Fin<TTarget> TransitionFrom<TSource, TTarget>(
    Func<TSource, TTarget> transition,
    string? message = null)
```

| 상황 | 결과 |
|------|------|
| `this`가 `TSource`인 경우 | 전이 함수 적용 → `Fin.Succ(결과)` |
| `this`가 `TSource`가 아닌 경우 | `Fin.Fail(DomainError(InvalidTransition))` |

`DomainError`에는 CRTP로 전달된 `TSelf` 타입 정보와 `FromState`/`ToState` 정보가 포함됩니다.

**InvalidTransition 에러 타입:**

```csharp
// DomainErrorKind.Transition.cs에 정의
public sealed record InvalidTransition(string? FromState = null, string? ToState = null) : DomainErrorKind;
```

전이 실패 시 생성되는 에러 JSON 구조 예시:

```json
{
  "ErrorCode": "Domain.EmailVerificationState.InvalidTransition",
  "ErrorCurrentValue": "Verified { Email = user@example.com, VerifiedAt = 2026-01-15 }",
  "Message": "Invalid transition from Verified to Verified"
}
```

> **참고**: `InvalidTransition` 에러 타입은 [에러 시스템: Domain/Application 에러](../08b-error-system-domain-app)의 Transition 범주를 참조하세요.

### 예제: EmailVerificationState

이메일 인증은 `Unverified → Verified` 단방향 전이만 허용합니다.

```csharp
[UnionType]
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState>
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;

    private EmailVerificationState() { }

    /// Unverified → Verified 전이. Verified 상태에서는 실패를 반환합니다.
    public Fin<Verified> Verify(DateTime verifiedAt) =>
        TransitionFrom<Unverified, Verified>(
            u => new Verified(u.Email, verifiedAt));
}
```

- `Verify`의 반환 타입은 `Fin<Verified>` — 성공하면 반드시 `Verified` 상태입니다
- `Verified` 상태에서 `Verify` 호출 시 `InvalidTransition` 에러가 자동 반환됩니다
- 전이 함수 `u => new Verified(u.Email, verifiedAt)`는 `Unverified`에서 이메일을 유지하면서 인증 시각을 추가합니다

---

## [UnionType] 소스 생성기

`[UnionType]` 어트리뷰트를 `abstract partial record`에 적용하면, 소스 생성기가 다음 4종의 멤버를 자동 생성합니다.

### 생성 대상

| 생성 멤버 | 시그니처 | 용도 |
|-----------|---------|------|
| `Match<TResult>` | `Func<Case, TResult>` 파라미터 (케이스 수만큼) | 모든 케이스를 빠짐없이 처리하여 값 반환 |
| `Switch` | `Action<Case>` 파라미터 (케이스 수만큼) | 모든 케이스를 빠짐없이 처리 (반환 없음) |
| `Is{Case}` | `bool` 속성 | 특정 케이스인지 확인 |
| `As{Case}()` | `Case?` 반환 메서드 | 특정 케이스로 안전한 캐스팅 |

### 생성 예시

`ContactInfo`에 대해 다음 코드가 자동 생성됩니다:

```csharp
public abstract partial record ContactInfo
{
    public TResult Match<TResult>(
        Func<EmailOnly, TResult> emailOnly,
        Func<PostalOnly, TResult> postalOnly,
        Func<EmailAndPostal, TResult> emailAndPostal)
    {
        return this switch
        {
            EmailOnly __case => emailOnly(__case),
            PostalOnly __case => postalOnly(__case),
            EmailAndPostal __case => emailAndPostal(__case),
            _ => throw new UnreachableCaseException(this)
        };
    }

    public void Switch(
        Action<EmailOnly> emailOnly,
        Action<PostalOnly> postalOnly,
        Action<EmailAndPostal> emailAndPostal) { ... }

    public bool IsEmailOnly => this is EmailOnly;
    public bool IsPostalOnly => this is PostalOnly;
    public bool IsEmailAndPostal => this is EmailAndPostal;

    public EmailOnly? AsEmailOnly() => this as EmailOnly;
    public PostalOnly? AsPostalOnly() => this as PostalOnly;
    public EmailAndPostal? AsEmailAndPostal() => this as EmailAndPostal;
}
```

### 요구사항

- `abstract partial record` 선언 필수
- `[UnionType]` 어트리뷰트 적용
- 케이스는 `sealed record`로 정의하고 Union 타입을 직접 상속

### UnreachableCaseException

`Match`/`Switch`의 기본 분기(`_ =>`)에서 사용됩니다. 모든 케이스가 `sealed record`로 닫혀 있으므로 정상적으로는 도달하지 않지만, 컴파일러의 exhaustiveness 경고를 해소하기 위해 포함됩니다.

```csharp
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
```

---

## Aggregate에서 Union 활용

### 가드 + 전이 위임 패턴

Aggregate는 전이 자체를 수행하지 않고, **가드 조건 검증 후 Union 객체에 위임**합니다.

```csharp
// Error type definitions
public sealed record AlreadyDeleted : DomainErrorKind.Custom;
public sealed record NoEmailToVerify : DomainErrorKind.Custom;

// Contact Aggregate의 VerifyEmail 메서드
public Fin<Unit> VerifyEmail(DateTime verifiedAt)
{
    // 1. 가드: 삭제 상태 확인
    if (DeletedAt.IsSome)
        return DomainError.For<Contact>(
            new AlreadyDeleted(), Id.ToString(),
            "Cannot verify email of a deleted contact");

    // 2. Match로 이메일 상태 추출
    var emailState = ContactInfo.Match<EmailVerificationState?>(
        emailOnly: eo => eo.EmailState,
        postalOnly: _ => null,
        emailAndPostal: ep => ep.EmailState);

    // 3. 가드: 이메일 존재 확인
    if (emailState is null)
        return DomainError.For<Contact>(
            new NoEmailToVerify(), Id.ToString(),
            "Contact does not have an email");

    // 4. 상태 전이를 EmailVerificationState에 위임
    return emailState.Verify(verifiedAt).Map(verified =>
    {
        ContactInfo = ContactInfo.Match(
            emailOnly: _ => (ContactInfo)new ContactInfo.EmailOnly(verified),
            postalOnly: _ => throw new InvalidOperationException(),
            emailAndPostal: ep => new ContactInfo.EmailAndPostal(verified, ep.Address));
        UpdatedAt = verifiedAt;
        AddDomainEvent(new EmailVerifiedEvent(Id, verified.Email, verifiedAt));
        return unit;
    });
}
```

**패턴 요약:**

| 단계 | 역할 | 담당 |
|------|------|------|
| 가드 | 선행 조건 검증 | Aggregate |
| 상태 추출 | `Match`로 현재 상태 가져오기 | Aggregate |
| 전이 실행 | `TransitionFrom`으로 상태 변경 | Union 객체 |
| 결과 반영 | 새 상태 저장 + 이벤트 발행 | Aggregate |

### 투영 속성 패턴

Union 내부의 값을 쿼리에서 사용해야 할 때, Aggregate에 투영 속성(projection property)을 정의합니다.

```csharp
public sealed class Contact : AggregateRoot<ContactId>
{
    // ContactInfo 설정 시 EmailValue 자동 동기화
    private ContactInfo _contactInfo = null!;
    public ContactInfo ContactInfo
    {
        get => _contactInfo;
        private set
        {
            _contactInfo = value;
            EmailValue = ExtractEmail(value);
        }
    }

    // 이메일 투영 속성 (Specification 지원용)
    public string? EmailValue { get; private set; }

    private static string? ExtractEmail(ContactInfo contactInfo) => contactInfo.Match(
        emailOnly: eo => GetEmailString(eo.EmailState),
        postalOnly: _ => (string?)null,
        emailAndPostal: ep => GetEmailString(ep.EmailState));
}
```

이 패턴으로 `ExpressionSpecification`에서 `EmailValue` 속성을 직접 쿼리할 수 있습니다.

---

## ValueObject와 UnionValueObject 비교

| 항목 | `sealed class : ValueObject` | `abstract partial record : UnionValueObject` |
|------|------------------------------|----------------------------------------------|
| 용도 | 복합 VO (PersonalName, PostalAddress) | Discriminated Union (ContactInfo, EmailVerificationState) |
| 동등성 | `GetEqualityComponents()` 명시 구현 | 컴파일러 자동 생성 (record) |
| 불변성 | private 생성자 + `{ get; }` | record positional 파라미터 |
| VO 계층 | `ValueObject` 계층 참여 | `IUnionValueObject` 계층 참여 |
| ORM 호환 | 프록시 타입 자동 처리 | 프록시 미지원 |
| 해시코드 | 캐시된 해시코드 | 컴파일러 생성 (record) |
| Source Generator | — | `[UnionType]`으로 Match/Switch 자동 생성 |

---

## 트러블슈팅

### Match에서 새 케이스 추가 시 컴파일 오류

**원인:** 정상 동작입니다. `Match<TResult>`는 모든 케이스에 대한 `Func` 파라미터를 요구하므로, 새 케이스 추가 시 기존 `Match` 호출부에서 인자 수 불일치로 컴파일 오류가 발생합니다.

**해결:** 모든 `Match`/`Switch` 호출부에 새 케이스에 대한 핸들러를 추가하세요. 이것이 exhaustiveness 보장의 핵심 이점입니다.

### TransitionFrom에서 InvalidTransition 에러

**원인:** 현재 상태가 전이 소스 타입과 일치하지 않습니다. 예를 들어, 이미 `Verified` 상태에서 다시 `Verify`를 호출한 경우입니다.

**해결:** Aggregate에서 전이를 호출하기 전에 현재 상태를 확인하거나, `InvalidTransition` 에러를 상위 레이어에서 적절히 처리하세요.

```csharp
// 에러에 FromState, ToState 정보가 포함됩니다
// "Invalid transition from Verified to Verified"
```

### partial 키워드 누락 시 소스 생성기 미작동

**원인:** `[UnionType]` 소스 생성기는 `partial` 키워드가 있는 record만 인식합니다. `partial`이 없으면 생성기가 코드를 추가할 수 없습니다.

**해결:** `abstract partial record`로 선언하세요.

```csharp
// 올바른 선언
[UnionType]
public abstract partial record ContactInfo : UnionValueObject { ... }

// partial 누락 — Match/Switch가 생성되지 않음
[UnionType]
public abstract record ContactInfo : UnionValueObject { ... }
```

### record는 class를 상속할 수 없음

**원인:** C#에서 record는 다른 class를 상속할 수 없습니다. 이 때문에 `ValueObject`(class)를 상속하는 대신 `IUnionValueObject`(인터페이스) 기반으로 설계되었습니다.

**해결:** Union 타입은 `UnionValueObject`(abstract record)를 상속하세요. `ValueObject`(class)는 사용할 수 없습니다.

---

## FAQ

### Q1. SmartEnum vs UnionValueObject 선택 기준은?

**모든 값이 같은 데이터 구조를 공유하면** `SmartEnum`, **값마다 서로 다른 데이터를 가지면** `UnionValueObject`를 사용합니다.

```csharp
// SmartEnum: 모든 통화가 동일한 구조 (Name, Value, Symbol, KoreanName)
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency KRW = new("KRW", "KRW", "₩", "한국 원화");
    public static readonly Currency USD = new("USD", "USD", "$", "미국 달러");
}

// UnionValueObject: 케이스별 데이터 구조가 다름
public abstract partial record ContactInfo : UnionValueObject
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
}
```

| 상황 | 선택 |
|------|------|
| 고정 목록 + 같은 속성 | `SmartEnum` |
| 케이스별 고유 데이터 | `UnionValueObject` |
| 상태 전이 로직 필요 | `UnionValueObject<TSelf>` |

### Q2. Union에 Validate/Create 패턴이 있는가?

Union 값 객체는 **Validate/Create 패턴을 사용하지 않습니다.** Union의 각 케이스는 이미 검증된 VO를 파라미터로 받으므로, Union 자체의 검증은 "어떤 케이스인가"를 결정하는 비즈니스 로직에 해당합니다. 이는 Aggregate 또는 Application Layer에서 처리합니다.

```csharp
// Union 케이스는 이미 검증된 VO를 받음
var contactInfo = new ContactInfo.EmailOnly(
    new EmailVerificationState.Unverified(email));  // email은 이미 검증된 EmailAddress VO
```

### Q3. Match 대신 C# switch를 사용해도 되는가?

**가능하지만 권장하지 않습니다.** C# `switch`는 기본 분기(`_`)를 요구하지 않으므로, 새 케이스 추가 시 누락을 컴파일 타임에 잡지 못합니다. `Match`는 모든 케이스에 대한 핸들러를 강제합니다.

```csharp
// Match: 새 케이스 추가 시 컴파일 오류 (안전)
contactInfo.Match(
    emailOnly: eo => ...,
    postalOnly: po => ...,
    emailAndPostal: ep => ...);

// C# switch: 새 케이스 추가 시 _ 분기로 빠짐 (위험)
var result = contactInfo switch
{
    ContactInfo.EmailOnly eo => ...,
    ContactInfo.PostalOnly po => ...,
    _ => ...  // 새 케이스가 여기로 빠질 수 있음
};
```

### Q4. Union 케이스에 행위 메서드를 정의할 수 있는가?

**가능하지만, 상태 전이 메서드는 Union 루트에 정의하는 것을 권장합니다.** `TransitionFrom`은 `UnionValueObject<TSelf>`에 정의되어 있으므로, 루트 record에서 호출해야 합니다. 케이스별 유틸리티 메서드는 개별 케이스에 정의할 수 있습니다.

```csharp
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState>
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState
    {
        // 케이스별 유틸리티는 가능
        public bool IsExpired(DateTime now) => (now - VerifiedAt).TotalDays > 365;
    }

    // 상태 전이 메서드는 루트에 정의
    public Fin<Verified> Verify(DateTime verifiedAt) =>
        TransitionFrom<Unverified, Verified>(u => new Verified(u.Email, verifiedAt));
}
```

---

## 참고 문서

- [값 객체 (Value Object)](../05a-value-objects) - 값 객체 핵심 개념과 기반 클래스 선택
- [값 객체: 열거형·검증·실전 패턴](../05b-value-objects-validation) - SmartEnum, Application Layer 검증 병합
- [에러 시스템: 기초와 네이밍](../08a-error-system) - DomainError, DomainErrorKind
- [에러 시스템: Domain/Application 에러](../08b-error-system-domain-app) - InvalidTransition 에러 타입
- [단위 테스트 가이드](../testing/15a-unit-testing)
