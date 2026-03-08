---
title: "검증된 Contact"
---

## 개요

지금까지 만든 타입들을 조립하여 완전한 type-safe Contact 모델을 구성합니다. raw string에서 시작하여 완전한 Contact를 생성하는 통합 테스트를 작성합니다.

## 학습 목표

1. 개별 값 객체들을 조합하여 완전한 도메인 모델 구성
2. raw string → 검증된 Contact 변환 흐름 이해
3. Part 1-2의 모든 패턴이 결합되는 방식 체험

## 완성된 Contact

```csharp
public sealed record Contact
{
    public required PersonalName Name { get; init; }
    public required ContactInfo ContactInfo { get; init; }
}
```

## 요약

- 타입 안전한 도메인 모델은 작은 타입의 조합으로 구성됩니다
- 각 구성요소가 자체 검증을 담당하므로, 조합 시 추가 검증이 불필요합니다
- raw string → 완전한 Contact 변환은 `Fin<T>` 체인으로 자연스럽게 표현됩니다
