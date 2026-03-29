---
title: "ADR-0019: 도메인 이벤트 추적 및 상관관계 ID"
status: "accepted"
date: 2026-03-23
---

## 맥락과 문제

분산 환경에서 도메인 이벤트가 여러 서비스를 거쳐 전파될 때, 이벤트 간 인과관계를 추적할 수 없으면 디버깅과 장애 분석이 극도로 어려워집니다. 예를 들어 "주문 생성 이벤트가 재고 차감 이벤트를 유발했다"는 관계를 코드와 로그에서 복원할 수 없습니다.

기존 IDomainEvent 인터페이스에는 이벤트 식별자와 추적 ID가 없어, 이벤트 발행 순서와 인과 체인을 파악하려면 타임스탬프와 로그 메시지에 의존해야 했습니다.

## 검토한 옵션

1. Ulid EventId + CorrelationId + CausationId + OccurredAt 필수 속성
2. Guid 기반 EventId
3. 추적 ID 미포함
4. 단일 TraceId만 포함

## 결정

**선택한 옵션: "Ulid EventId + CorrelationId + CausationId + OccurredAt 필수 속성"**, Ulid가 시간순 정렬을 보장하면서 고유성을 제공하고, CorrelationId와 CausationId 조합으로 요청 추적과 인과 체인을 모두 표현할 수 있기 때문입니다.

- **EventId (Ulid)**: 이벤트의 고유 식별자. 시간순 정렬이 가능하여 이벤트 발생 순서를 자연스럽게 보존합니다.
- **CorrelationId**: 하나의 사용자 요청에서 파생된 모든 이벤트를 그룹화합니다.
- **CausationId**: 이 이벤트를 직접 유발한 선행 이벤트의 EventId를 참조하여 인과 체인을 구성합니다.
- **OccurredAt**: 이벤트 발생 시각을 기록합니다.

### 결과

- Good, because 이벤트 인과 체인을 CorrelationId → CausationId로 완전히 추적할 수 있습니다.
- Good, because Ulid 기반 EventId가 시간순 정렬을 보장하여 이벤트 순서 파악이 용이합니다.
- Good, because 분산 추적(OpenTelemetry)과 자연스럽게 통합됩니다.
- Bad, because 모든 도메인 이벤트에 4개 필수 속성이 추가되어 보일러플레이트가 증가합니다.

### 확인

- IDomainEvent 구현체가 4개 필수 속성을 모두 포함하는지 아키텍처 규칙 테스트로 확인합니다.
- 이벤트 체인에서 CausationId가 선행 이벤트의 EventId와 일치하는지 통합 테스트로 검증합니다.

## 옵션별 장단점

### Ulid EventId + CorrelationId + CausationId + OccurredAt 필수 속성

- Good, because Ulid가 시간순 정렬과 고유성을 동시에 보장합니다.
- Good, because CorrelationId로 요청 단위 추적, CausationId로 인과 체인 추적이 가능합니다.
- Good, because OccurredAt이 이벤트 발생 시각을 명시적으로 기록합니다.
- Bad, because 모든 이벤트 생성 시 4개 속성을 설정해야 합니다.

### Guid 기반 EventId

- Good, because .NET 표준 타입으로 별도 의존성이 없습니다.
- Bad, because Guid v4는 비순차적이라 이벤트 발생 순서를 ID만으로 파악할 수 없습니다.
- Bad, because 정렬을 위해 별도 타임스탬프 비교가 필요합니다.

### 추적 ID 미포함

- Good, because 이벤트 구조가 단순합니다.
- Bad, because 분산 환경에서 이벤트 간 인과관계를 전혀 추적할 수 없습니다.
- Bad, because 장애 분석 시 로그 타임스탬프에만 의존해야 합니다.

### 단일 TraceId만 포함

- Good, because 구현이 비교적 단순합니다.
- Good, because 요청 단위 추적은 가능합니다.
- Bad, because 인과 체인(어떤 이벤트가 어떤 이벤트를 유발했는가)을 표현할 수 없습니다.
- Bad, because 동일 요청 내 여러 이벤트의 선후 관계가 불분명합니다.

## 관련 정보

- 관련 커밋: `58b2719c`, `292a5850`
- 관련 가이드: `Docs.Site/src/content/docs/guides/domain/`
- 참고: OpenTelemetry Trace Context, W3C Trace Context specification
