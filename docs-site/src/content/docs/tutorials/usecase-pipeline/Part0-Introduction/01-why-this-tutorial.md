---
title: "왜 타입 안전 파이프라인인가"
---

## Mediator Pipeline의 딜레마

Mediator 패턴에서 Pipeline은 모든 요청/응답을 가로채는 **교차 관심사(Cross-Cutting Concern)** 처리기입니다. Logging, Validation, Exception Handling, Transaction 관리 등을 Usecase 코드와 분리하여 일관되게 적용할 수 있습니다.

```csharp
// Pipeline이 모든 요청/응답을 가로챔
Request → [Validation] → [Logging] → [Transaction] → Handler → [Logging] → Response
```

그런데 Pipeline은 **모든 타입의 응답**을 처리해야 합니다. Logging Pipeline은 응답이 성공인지 실패인지 알아야 하고, Validation Pipeline은 검증 실패 시 응답 객체를 **직접 생성**해야 합니다.

## 핵심 문제: sealed struct는 제약이 불가능하다

LanguageExt의 `Fin<T>`는 성공/실패를 표현하는 모나드입니다. 하지만 `Fin<T>`는 **sealed struct**이기 때문에:

```csharp
// 이것은 불가능합니다
where TResponse : Fin<T>  // 컴파일 에러! sealed struct는 제약 조건이 될 수 없음
```

이 한 줄의 제약 때문에 Pipeline에서 `Fin<T>`를 직접 다룰 수 없습니다. 결과적으로 **리플렉션에 의존**하게 됩니다.

## 리플렉션의 3가지 비용

리플렉션으로 `Fin<T>`를 다루면:

1. **런타임 성능 저하**: 매 요청마다 타입 정보를 동적으로 조회
2. **컴파일 타임 안전성 상실**: 오타나 타입 불일치가 런타임에야 발견됨
3. **유지보수 복잡성**: 타입 변경 시 리플렉션 코드를 수동으로 동기화해야 함

## 이 튜토리얼이 제시하는 해결책

이 튜토리얼은 **인터페이스 계층 설계**와 **C# 11 static abstract 멤버**를 활용하여 리플렉션을 완전히 제거하는 과정을 다룹니다.

```
리플렉션 3곳 → 인터페이스 계층 → 리플렉션 0곳
```

구체적으로:

| 문제 | 해결 |
|------|------|
| 성공/실패 읽기 | `IFinResponse` 비제네릭 마커 인터페이스 |
| 공변적 접근 | `IFinResponse<out A>` 공변 인터페이스 |
| 실패 응답 생성 | `IFinResponseFactory<TSelf>` CRTP 팩토리 |
| 에러 정보 접근 | `IFinResponseWithError` 에러 접근 인터페이스 |
| 전체 통합 | `FinResponse<A>` Discriminated Union record |

## 이 튜토리얼의 여정

1. **Part 1**: C# 제네릭 변성의 기초를 다집니다
2. **Part 2**: `Fin<T>`와 Mediator Pipeline의 충돌 문제를 정확히 정의합니다
3. **Part 3**: IFinResponse 인터페이스 계층을 하나씩 설계합니다
4. **Part 4**: 각 Pipeline에 최소 제약 조건을 적용합니다
5. **Part 5**: 실전 Command/Query Usecase에서 전체 흐름을 통합합니다

