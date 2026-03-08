---
title: "상태 전이"
---

## 개요

전이 함수를 정의하여 유효한 상태 전이만 허용합니다. 무효 전이는 `Fin.Fail`을 반환합니다.

## 학습 목표

1. 상태 전이 함수를 타입 안전하게 구현
2. 무효 전이를 `Fin<T>`로 표현
3. DomainErrorType.InvalidTransition 활용

## 핵심 패턴

```csharp
public static Fin<EmailVerificationState> Verify(
    EmailVerificationState state, DateTime verifiedAt) => state switch
{
    Unverified u => new Verified(u.Email, verifiedAt),
    Verified   => Fin<EmailVerificationState>.Fail(
        DomainError.For<EmailVerificationState>(
            new DomainErrorType.InvalidTransition(FromState: "Verified", ToState: "Verified"),
            state.ToString()!,
            "이미 인증된 이메일입니다")),
    _ => throw new InvalidOperationException()
};
```

## 요약

- 전이 함수는 현재 상태에서 다음 상태로의 변환을 정의합니다
- 유효하지 않은 전이는 `Fin.Fail`로 명시적으로 실패합니다
- `DomainErrorType.InvalidTransition`으로 에러 원인을 타입 안전하게 표현합니다
