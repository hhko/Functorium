---
title: "Boolean Blindness"
---

## 개요

optional email + optional postal address 조합에서, boolean/nullable로 표현하면 4가지 상태 중 1가지가 무효합니다. 이 장에서는 그 불법 상태가 실제로 생성 가능함을 증명합니다.

## 학습 목표

1. Boolean blindness 개념 이해
2. optional 필드 조합이 만드는 상태 공간 분석
3. 불법 상태가 타입 시스템으로 방지되지 않는 문제 인식

## 문제: 4가지 상태, 1가지가 무효

연락처는 이메일 또는 우편 주소 중 **최소 하나는** 있어야 합니다.

| 이메일 | 우편 주소 | 유효? |
|--------|----------|-------|
| O | O | 유효 |
| O | X | 유효 |
| X | O | 유효 |
| X | X | **무효** |

nullable로 표현하면 "둘 다 없는" 상태가 허용됩니다.

```csharp
public class ContactInfo
{
    public string? EmailAddress { get; set; }
    public string? PostalAddress { get; set; }
}
```

## 요약

- nullable 필드 조합은 불법 상태를 허용합니다
- if 문으로 검증하는 것은 누락 위험이 있습니다
- 타입 구조 자체로 불법 상태를 방지해야 합니다
