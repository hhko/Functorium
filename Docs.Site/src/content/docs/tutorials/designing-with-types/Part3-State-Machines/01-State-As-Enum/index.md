---
title: "Enum 상태"
---

## 개요

enum + nullable 필드로 이메일 인증 상태를 표현합니다. "미인증인데 인증일이 있는" 불법 상태가 발생합니다.

## 학습 목표

1. enum으로 상태를 표현할 때의 한계 이해
2. 상태별 데이터가 nullable 필드가 되는 문제 인식
3. 불법 상태가 타입으로 방지되지 않는 문제 확인

## 문제: 상태와 데이터의 분리

```csharp
public enum VerificationStatus { Unverified, Verified }

public class EmailState
{
    public string Email { get; set; }
    public VerificationStatus Status { get; set; }
    public DateTime? VerifiedAt { get; set; } // Verified일 때만 유효
}
```

`Status`가 `Unverified`인데 `VerifiedAt`에 값이 있을 수 있습니다.

## 요약

- enum은 상태를 표현하지만 상태별 데이터를 강제하지 못합니다
- nullable 필드는 불법 상태를 허용합니다
- 상태와 데이터를 하나의 타입으로 묶어야 합니다
