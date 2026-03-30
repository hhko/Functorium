---
title: "ADR-0020: Aggregate 간 ID 전용 참조"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

`Order` Aggregate가 `Customer` 객체를 직접 참조(`public Customer Customer { get; }`)한다고 가정합니다. 주문을 조회할 때 EF Core가 `Customer`를 함께 로드하고, `Customer`가 참조하는 `Address`, `LoyaltyProgram`까지 연쇄적으로 로드됩니다. 주문 하나를 읽으려 했을 뿐인데 4개 테이블 조인이 발생합니다.

문제는 성능에 그치지 않습니다. 주문을 저장할 때 EF Core의 변경 추적이 `Customer` 객체까지 포함하여, 의도하지 않은 고객 정보 변경이 같은 트랜잭션에 포함될 수 있습니다. 두 사용자가 동시에 같은 고객의 서로 다른 주문을 수정하면, `Customer` 객체 참조를 통해 동시성 충돌이 발생합니다. 또한 `Order`가 `Customer` 타입에 직접 의존하므로, 향후 고객 서비스를 별도 마이크로서비스로 분리할 때 Aggregate 경계를 따라 자를 수 없습니다.

## 검토한 옵션

1. ID(값 타입) 전용 참조 + 도메인 이벤트
2. 직접 객체 참조
3. Lazy Loading
4. Saga 패턴

## 결정

**선택한 옵션: "ID(값 타입) 전용 참조 + 도메인 이벤트"**, Aggregate의 트랜잭션 경계를 물리적으로 강제하여 경계 침범을 원천 차단하기 위해서입니다. `Order`는 `Customer` 객체 대신 `CustomerId` 값 타입만 보유하므로 연쇄 로딩, 의도치 않은 변경 추적, 동시성 충돌이 구조적으로 발생할 수 없습니다.

교차 Aggregate 간 일관성이 필요한 경우(예: 주문 생성 후 고객 포인트 적립)에는 도메인 이벤트를 통해 최종적 일관성(Eventual Consistency)으로 처리합니다. 강타입 ID(`OrderId`, `CustomerId`)가 `string`이나 `Ulid` 원시 타입과 구분되어 `Order`에 실수로 `ProductId`를 할당하면 컴파일 오류가 발생합니다.

### 결과

- Good, because Aggregate의 트랜잭션 경계가 명확히 유지됩니다.
- Good, because 강타입 ID(Ulid 기반)가 컴파일 타임에 잘못된 참조를 방지합니다.
- Good, because Aggregate 로드 시 연쇄 로딩이 발생하지 않습니다.
- Good, because 마이크로서비스 분리 시 Aggregate 간 결합이 최소화됩니다.
- Bad, because 교차 Aggregate 조회 시 별도 쿼리가 필요합니다.
- Bad, because 최종적 일관성 모델에 대한 이해가 필요합니다.

### 확인

- Aggregate Root가 다른 Aggregate의 엔티티를 직접 참조하지 않는지 아키텍처 규칙 테스트로 검증합니다.
- 교차 Aggregate 참조가 ID 값 타입으로만 이루어지는지 코드 리뷰에서 점검합니다.

## 옵션별 장단점

### ID(값 타입) 전용 참조 + 도메인 이벤트

- Good, because 트랜잭션 경계가 Aggregate 단위로 명확합니다.
- Good, because 강타입 ID가 타입 안전성을 보장합니다.
- Good, because 도메인 이벤트를 통한 느슨한 결합을 촉진합니다.
- Bad, because 교차 Aggregate 데이터가 필요한 경우 별도 쿼리나 Read Model이 필요합니다.

### 직접 객체 참조

- Good, because 네비게이션이 직관적이고 구현이 단순합니다.
- Bad, because 트랜잭션 경계가 침범되어 동시성 충돌이 증가합니다.
- Bad, because Aggregate 로드 시 연쇄 로딩으로 성능이 저하됩니다.
- Bad, because Aggregate 간 강결합으로 독립적 배포가 어렵습니다.

### Lazy Loading

- Good, because 필요한 시점에만 관련 Aggregate를 로드합니다.
- Bad, because N+1 쿼리 문제가 발생합니다.
- Bad, because 도메인 모델이 영속성 메커니즘에 의존하게 됩니다.
- Bad, because 트랜잭션 경계 침범 문제는 여전히 존재합니다.

### Saga 패턴

- Good, because 분산 환경에서 장기 실행 트랜잭션을 관리할 수 있습니다.
- Bad, because 소규모 단일 서비스에서는 복잡도가 과도합니다.
- Bad, because 보상 트랜잭션 설계와 상태 머신 관리가 필요합니다.

## 관련 정보

- 관련 커밋: `71272343`
- 관련 가이드: `Docs.Site/src/content/docs/guides/domain/06a-aggregate-design`
- 참고: Vaughn Vernon, Implementing Domain-Driven Design — Chapter 10, Aggregates
