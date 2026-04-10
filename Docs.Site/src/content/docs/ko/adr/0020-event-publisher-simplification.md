---
title: "ADR-0020: Event - 도메인 이벤트 발행기 단순화 (BatchHandler 제거)"
status: "accepted"
date: 2026-03-24
---

## 맥락과 문제

상품 1,000건을 일괄 가격 변경하는 벌크 연산에서 `PriceChangedEvent` 처리가 실패했을 때, 스택 트레이스가 리플렉션 기반 디스패치 로직 내부를 7단계나 거치면서 실제 원인을 찾는 데 불필요한 시간이 소요되었다. `IDomainEventBatchHandler`는 이벤트 타입별 제네릭 핸들러를 `MakeGenericType`과 `Invoke`로 동적으로 찾아 호출하는 100줄의 리플렉션 로직을 포함하고 있었고, 타입 불일치 시 "Object does not match target type" 같은 불명확한 에러 메시지만 남겼다. 벌크 이벤트 묶음 처리라는 편의를 위해 디버깅 가능성과 타입 안전성을 희생하고 있었다.

리플렉션의 근본적인 복잡도를 제거하면서도 벌크 연산의 이벤트 처리 요구를 충족할 수 있는 단순하고 명시적인 구조가 필요했다.

## 검토한 옵션

- **옵션 1**: IDomainEventBatchHandler 유지 및 리플렉션 최적화
- **옵션 2**: BatchHandler 제거 + 벌크 로직을 Domain Service로 이동
- **옵션 3**: 이벤트 발행 자체를 제거

## 결정

**옵션 2: BatchHandler를 제거하고, 벌크 로직을 Domain Service로 이동한다. (BREAKING CHANGE)**

BREAKING CHANGE를 감수하는 이유는 명확하다. 리플렉션 100줄의 "자동화 편의"보다 디버깅 가능성과 타입 안전성이 더 가치 있다. 벌크 연산의 이벤트 묶음 처리는 프레임워크가 암묵적으로 할 일이 아니라, 비즈니스 로직이 명시적으로 표현해야 할 책임이다.

- `IDomainEventBatchHandler` 인터페이스와 리플렉션 기반 디스패치 로직을 완전히 제거한다.
- 벌크 연산에서 발생하는 이벤트 처리는 명시적인 Domain Service(예: `ProductBulkOperations`)에서 담당한다.
- 도메인 이벤트 발행기는 단일 이벤트 발행에만 집중하여 책임이 명확해진다.
- Domain Service에서 벌크 연산의 결과를 집계하고, 필요한 경우 요약 이벤트를 발행한다.

### 결과

- **긍정적**: 리플렉션 100줄이 제거되어 이벤트 발행기의 코드가 절반 이하로 줄었고, 실패 시 스택 트레이스가 비즈니스 코드를 직접 가리킨다. 벌크 가격 변경 같은 연산의 의도가 `ProductBulkOperations` Domain Service에 명시적으로 드러나, 코드를 읽는 것만으로 "무엇을 묶어서 처리하는지"를 파악할 수 있다. 모든 이벤트 디스패치가 컴파일 타임에 타입 검증된다.
- **부정적**: BREAKING CHANGE로 기존 `IDomainEventBatchHandler` 구현체를 모두 Domain Service로 마이그레이션해야 한다. 벌크 연산마다 전용 Domain Service 클래스를 작성하는 보일러플레이트가 발생할 수 있다.

### 확인

- 기존 BatchHandler를 사용하던 모든 코드가 Domain Service로 마이그레이션되었는지 확인한다.
- 벌크 연산에서 개별 이벤트가 누락 없이 발행되는지 통합 테스트로 검증한다.
- Domain Service의 벌크 로직이 트랜잭션 경계 내에서 올바르게 동작하는지 확인한다.

## 옵션별 장단점

### 옵션 1: IDomainEventBatchHandler 유지 및 리플렉션 최적화

- **장점**: 기존 코드 변경이 최소화된다. 벌크 이벤트 묶음 처리가 프레임워크 수준에서 자동으로 이루어지므로 호출부 코드가 간결하다.
- **단점**: `MakeGenericType`/`Invoke` 기반 디스패치의 근본적인 복잡도는 최적화로 해결되지 않는다. 타입 불일치 시 "Object does not match target type" 같은 불명확한 에러가 지속된다. 매 벌크 연산마다 리플렉션 호출의 성능 오버헤드가 발생한다. 스택 트레이스가 리플렉션 내부를 관통하여 실제 원인까지 도달하기 어렵다.

### 옵션 2: BatchHandler 제거 + Domain Service로 이동

- **장점**: 리플렉션 100줄이 완전히 제거되어 이벤트 발행기 코드가 단순해지고, 실패 시 스택 트레이스가 비즈니스 코드를 직접 가리킨다. `ProductBulkOperations` 같은 Domain Service에 벌크 로직이 명시적으로 드러나 코드만 읽어도 비즈니스 의도를 파악할 수 있다. 모든 이벤트 디스패치가 컴파일 타임에 타입 검증된다.
- **단점**: BREAKING CHANGE로 기존 BatchHandler 구현체를 모두 Domain Service로 마이그레이션해야 한다. 벌크 연산마다 전용 Domain Service를 작성하는 보일러플레이트가 생기며, 프레임워크 수준의 자동 묶음 처리가 사라진다.

### 옵션 3: 이벤트 발행 자체를 제거

- **장점**: 가장 단순하다. 이벤트 발행기, 핸들러, 구독 인프라가 모두 불필요하다.
- **단점**: 주문 완료 시 알림 발송, 감사 로그 기록, 캐시 무효화 같은 부수 효과를 도메인 이벤트 없이 처리해야 하므로 Aggregate 간 직접 의존이 생긴다. 느슨한 결합과 최종 일관성(eventual consistency) 메커니즘을 포기하게 된다.

## 관련 정보

- 커밋: e48330b3 (BREAKING CHANGE), 2731059d, 774ff4dd, bad1d541
