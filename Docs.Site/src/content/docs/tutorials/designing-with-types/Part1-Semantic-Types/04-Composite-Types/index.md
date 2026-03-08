---
title: "복합 타입"
---

## 개요

`PersonalName`과 `PostalAddress`를 record로 구성하여, 관련 데이터를 원자적으로 그룹화합니다. 부분적으로만 유효한 데이터가 존재할 수 없습니다.

## 학습 목표

1. 관련 필드를 record로 그룹화하는 이유 이해
2. Apply 패턴으로 복합 타입의 모든 필드를 동시 검증
3. 부분 유효성이 불가능한 타입 구조 설계

## 핵심 개념: 원자적 그룹화

이름의 세 구성요소(FirstName, MiddleInitial, LastName)는 항상 함께 다뤄져야 합니다. record로 그룹화하면 부분적으로만 유효한 이름이 존재할 수 없습니다.

```csharp
public sealed record PersonalName
{
    public required String50 FirstName { get; init; }
    public required String50 LastName { get; init; }
    public string? MiddleInitial { get; init; }
}
```

## 요약

- 관련 데이터는 하나의 record로 그룹화합니다
- 복합 타입은 모든 구성요소가 유효해야만 생성됩니다
- Apply 패턴으로 여러 검증을 병렬로 수행합니다
