---
title: "다음 단계"
---

## 이 튜토리얼에서 배운 것

| Part | 핵심 교훈 |
|:---:|----------|
| 1 | 원시 타입을 의미 있는 타입으로 래핑하면 컴파일러가 타입 혼동을 방지합니다 |
| 2 | sealed record union으로 불법 상태를 구조적으로 제거할 수 있습니다 |
| 3 | 상태 기계를 타입으로 표현하면 무효 전이를 방지할 수 있습니다 |

## 다음 학습 경로

### 값 객체 심화 → [함수형 값 객체 구현](../../functional-valueobject/)

이 튜토리얼에서 사용한 `SimpleValueObject<T>`, `ValidationRules`, `DomainError` 패턴의 **구현 원리를** 깊이 있게 학습합니다. Validation 합성, Apply/Bind 패턴, 아키텍처 테스트까지 다룹니다.

### 비즈니스 규칙 캡슐화 → [명세 패턴](../../specification-pattern/)

타입으로 표현하기 어려운 복잡한 비즈니스 규칙(예: "VIP 고객이면서 3개월 내 구매 이력이 있는")을 독립적인 객체로 캡슐화하고 조합하는 방법을 학습합니다.

### CQRS 통합 → [CQRS 리포지토리 패턴](../../cqrs-repository/)

타입 안전한 도메인 모델을 영속성 계층과 통합합니다. Command/Query 분리, Repository 패턴, Entity Framework Core 통합을 다룹니다.

## 패턴 선택 의사결정 가이드

```
원시 타입을 도메인 타입으로 바꾸고 싶은가?
├── 단일 값 래핑 → SimpleValueObject<T>
├── 복합 값 그룹화 → sealed record
├── 여러 변형 중 하나 → sealed record union (Discriminated Union)
└── 상태별 동작 → 상태 기계 (sealed record + 전이 함수)
```
