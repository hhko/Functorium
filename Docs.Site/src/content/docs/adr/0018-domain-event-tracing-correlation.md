---
title: "ADR-0018: 도메인 이벤트 추적 및 상관관계 ID"
status: "accepted"
date: 2026-03-23
---

## 맥락과 문제

새벽에 결제 실패 알림이 발생했다고 가정합니다. 로그에는 `OrderCreated`, `InventoryReserved`, `PaymentFailed`, `InventoryReleased` 등 수십 개의 도메인 이벤트가 뒤섞여 있습니다. "이 `PaymentFailed`가 어떤 `OrderCreated`에서 시작된 흐름인가?", "`InventoryReleased`는 `PaymentFailed`의 보상 이벤트인가, 아니면 별개 요청인가?" 같은 인과관계를 파악하려면 타임스탬프를 밀리초 단위로 대조하며 수작업으로 이벤트 체인을 복원해야 합니다.

기존 `IDomainEvent` 인터페이스에는 이벤트 고유 식별자도, 요청 추적 ID도, 선행 이벤트 참조도 없었습니다. 동일 시각에 발생한 서로 다른 요청의 이벤트를 구분할 수 없고, 이벤트 간 인과 체인을 구조적으로 표현할 방법이 없어 장애 분석이 추측에 의존하게 됩니다.

## 검토한 옵션

1. Ulid EventId + CorrelationId + CausationId + OccurredAt 필수 속성
2. Guid 기반 EventId
3. 추적 ID 미포함
4. 단일 TraceId만 포함

## 결정

**선택한 옵션: "Ulid EventId + CorrelationId + CausationId + OccurredAt 필수 속성"**, 이벤트 간 인과관계를 구조적으로 추적하여 장애 분석을 추측이 아닌 데이터 기반으로 전환하기 위해서입니다.

- **EventId (Ulid)**: 이벤트의 고유 식별자입니다. Ulid는 타임스탬프가 상위 비트에 인코딩되어 있어 문자열 정렬만으로 이벤트 발생 순서를 복원할 수 있습니다. Guid v4와 달리 별도 정렬 키가 필요 없습니다.
- **CorrelationId**: 하나의 사용자 요청에서 파생된 모든 이벤트를 그룹화합니다. "주문 #A의 전체 이벤트 흐름"을 단일 쿼리로 조회할 수 있습니다.
- **CausationId**: 이 이벤트를 직접 유발한 선행 이벤트의 EventId를 참조합니다. `OrderCreated → InventoryReserved → PaymentRequested` 같은 인과 체인을 트리 구조로 복원할 수 있습니다.
- **OccurredAt**: 이벤트 발생 시각을 명시적으로 기록하여 Ulid의 시간 정보와 독립적인 감사 추적을 제공합니다.

### 결과

- <span class="adr-good">Good</span>, because CorrelationId로 "이 요청에서 발생한 모든 이벤트"를 조회하고, CausationId로 "이 이벤트의 원인 체인"을 트리 구조로 복원할 수 있어 장애 분석 시간이 대폭 단축됩니다.
- <span class="adr-good">Good</span>, because Ulid 기반 EventId의 사전식 정렬이 곧 시간순 정렬이므로, 이벤트 저장소에서 별도 인덱스 없이 발생 순서를 자연스럽게 보존합니다.
- <span class="adr-good">Good</span>, because CorrelationId를 OpenTelemetry의 TraceId와 매핑하여 도메인 이벤트 추적과 인프라 분산 추적을 하나의 대시보드에서 통합 조회할 수 있습니다.
- <span class="adr-bad">Bad</span>, because 모든 도메인 이벤트 record에 `EventId`, `CorrelationId`, `CausationId`, `OccurredAt` 4개 속성이 필수로 추가되어 이벤트 정의의 보일러플레이트가 증가합니다.

### 확인

- IDomainEvent 구현체가 4개 필수 속성을 모두 포함하는지 아키텍처 규칙 테스트로 확인합니다.
- 이벤트 체인에서 CausationId가 선행 이벤트의 EventId와 일치하는지 통합 테스트로 검증합니다.

## 옵션별 장단점

### Ulid EventId + CorrelationId + CausationId + OccurredAt 필수 속성

- <span class="adr-good">Good</span>, because Ulid의 128비트 중 상위 48비트가 밀리초 타임스탬프이므로, 고유성과 시간순 정렬을 별도 필드 없이 하나의 ID로 해결합니다.
- <span class="adr-good">Good</span>, because CorrelationId는 요청 전체를 수평 조회하고, CausationId는 특정 이벤트의 원인을 수직 추적하여 두 축의 분석을 모두 지원합니다.
- <span class="adr-good">Good</span>, because OccurredAt이 Ulid의 타임스탬프와 독립적으로 감사(audit) 목적의 정밀 시각을 기록합니다.
- <span class="adr-bad">Bad</span>, because 모든 이벤트 생성 시 4개 속성을 반드시 설정해야 하며, 누락하면 컴파일 오류가 발생하도록 인터페이스로 강제해야 합니다.

### Guid 기반 EventId

- <span class="adr-good">Good</span>, because `System.Guid`는 .NET 표준 타입이므로 외부 라이브러리 의존 없이 사용할 수 있습니다.
- <span class="adr-bad">Bad</span>, because Guid v4는 랜덤 생성이므로 두 EventId를 비교해도 어느 이벤트가 먼저 발생했는지 알 수 없어, 이벤트 순서 파악에 항상 별도 타임스탬프 비교가 필요합니다.
- <span class="adr-bad">Bad</span>, because 비순차적 Guid를 DB 인덱스 키로 사용하면 페이지 분할(page split)이 빈번하여 쓰기 성능이 저하됩니다.

### 추적 ID 미포함

- <span class="adr-good">Good</span>, because 이벤트가 비즈니스 페이로드만 포함하여 구조가 가장 단순합니다.
- <span class="adr-bad">Bad</span>, because 운영 환경에서 수백 개 이벤트가 동시에 발생할 때, 특정 요청에 속하는 이벤트를 필터링할 방법이 없어 장애 분석이 타임스탬프 밀리초 대조에 의존합니다.
- <span class="adr-bad">Bad</span>, because `PaymentFailed` 이벤트가 어떤 `OrderCreated`에서 시작된 흐름인지 구조적으로 파악할 수 없어 인과관계 복원이 추측에 머뭅니다.

### 단일 TraceId만 포함

- <span class="adr-good">Good</span>, because 속성 하나만 추가하면 되어 구현이 단순합니다.
- <span class="adr-good">Good</span>, because TraceId로 "이 요청에서 발생한 모든 이벤트"를 그룹화할 수 있습니다.
- <span class="adr-bad">Bad</span>, because 동일 요청 내에서 `OrderCreated`가 `InventoryReserved`를 유발했다는 인과 관계를 표현할 수 없어, 이벤트 목록은 보이지만 흐름 방향은 알 수 없습니다.
- <span class="adr-bad">Bad</span>, because 하나의 요청에서 10개 이벤트가 발생했을 때 어떤 순서로, 어떤 이벤트가 어떤 이벤트를 촉발했는지 트리 구조를 복원할 수 없습니다.

## 관련 정보

- 관련 커밋: `58b2719c`, `292a5850`
- 관련 가이드: `Docs.Site/src/content/docs/guides/domain/`
- 참고: OpenTelemetry Trace Context, W3C Trace Context specification
