---
title: "Before vs After"
---

## 나이브 Contact (Before)

```csharp
public class Contact
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Address1 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
}
```

**문제점:**
- 모든 필드가 `string` — 타입 혼동 가능
- 빈 문자열, null 허용 — 유효하지 않은 값 존재
- boolean 플래그 — 불법 상태 허용
- 상태와 데이터 분리 — 무효 조합 가능

## 타입 안전 Contact (After)

```csharp
public sealed record Contact
{
    public required PersonalName Name { get; init; }
    public required ContactInfo ContactInfo { get; init; }
}

public sealed record PersonalName
{
    public required String50 FirstName { get; init; }
    public required String50 LastName { get; init; }
    public string? MiddleInitial { get; init; }
}

public abstract record ContactInfo
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
    public sealed record EmailAndPostal(EmailVerificationState EmailState, PostalAddress Address) : ContactInfo;
    private ContactInfo() { }
}

public abstract record EmailVerificationState
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;
    private EmailVerificationState() { }
}

public sealed record PostalAddress
{
    public required String50 Address1 { get; init; }
    public required String50 City { get; init; }
    public required StateCode State { get; init; }
    public required ZipCode Zip { get; init; }
}
```

**개선점:**
- 각 필드가 고유 타입 — 타입 혼동 컴파일 에러
- 생성 시 검증 강제 — 유효하지 않은 값 존재 불가
- Union type — 불법 상태 표현 불가
- `EmailVerificationState`로 인증 상태를 타입에 인코딩 — `bool` 플래그 제거
- `PostalAddress`에 검증된 컴포넌트(`StateCode`, `ZipCode`) 사용

## 패턴 적용 요약

| 문제 | 해결 패턴 | Part |
|------|----------|:---:|
| 원시 타입 혼동 | SimpleValueObject 래핑 | 1 |
| 길이/범위 무시 | 제약된 타입 (String50, NonNegativeInt) | 1 |
| 관련 필드 분리 | record 복합 타입 | 1 |
| nullable 불법 상태 | sealed record union | 2 |
| boolean 플래그 | Discriminated union | 2 |
| enum + nullable | 상태별 sealed record | 3 |
| 무효 상태 전이 | 전이 함수 + Fin<T> | 3 |

## 핵심 교훈

1. **타입은 문서다** — 코드를 읽는 것만으로 비즈니스 규칙을 이해할 수 있습니다
2. **컴파일러는 동료다** — 규칙 위반을 컴파일 타임에 알려줍니다
3. **타입이 도메인을 드러낸다** — 리팩터링이 숨겨진 비즈니스 개념을 발견하게 합니다
