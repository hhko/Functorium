---
title: "Union 상태"
---

## 개요

sealed record 상태 타입으로 이메일 인증을 표현합니다. Verified는 항상 인증일을 보유하고, Unverified는 절대 보유하지 않습니다.

## 학습 목표

1. 상태를 sealed record union으로 표현하는 방법
2. 상태별 데이터가 타입에 의해 강제되는 원리 이해
3. 불법 상태가 구조적으로 불가능함을 확인

## 핵심 패턴

```csharp
public abstract record EmailVerificationState
{
    public sealed record Unverified(string Email) : EmailVerificationState;
    public sealed record Verified(string Email, DateTime VerifiedAt) : EmailVerificationState;
    private EmailVerificationState() { }
}
```

## 요약

- 상태별 데이터를 각 record에 포함하면 불법 상태가 구조적으로 불가능합니다
- Unverified에는 인증일이 없고, Verified에는 항상 있습니다
- nullable 필드가 사라지므로 null 체크가 불필요합니다
