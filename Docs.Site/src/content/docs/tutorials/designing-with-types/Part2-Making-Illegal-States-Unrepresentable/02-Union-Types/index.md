---
title: "Union Types"
---

## 개요

sealed record 계층으로 `ContactInfo`를 `EmailOnly | PostalOnly | EmailAndPostal`로 표현하여, 불법 상태를 타입 수준에서 제거합니다.

## 학습 목표

1. C#에서 Discriminated Union을 sealed record 계층으로 구현
2. switch 식의 완전성 검사(exhaustiveness check) 활용
3. 불법 상태가 타입 수준에서 불가능함을 증명

## 핵심 패턴: Sealed Record Hierarchy

```csharp
public abstract record ContactInfo
{
    public sealed record EmailOnly(string Email) : ContactInfo;
    public sealed record PostalOnly(string Address) : ContactInfo;
    public sealed record EmailAndPostal(string Email, string Address) : ContactInfo;
    private ContactInfo() { }
}
```

## 요약

- sealed record 계층은 C#의 Discriminated Union입니다
- private 생성자로 외부 상속을 차단합니다
- switch 식에서 모든 케이스를 처리해야 컴파일됩니다
