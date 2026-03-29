---
title: "ADR-0020: Aggregate 간 ID 전용 참조"
status: "accepted"
date: 2026-03-22
---

## 맥락과 문제

DDD에서 Aggregate는 트랜잭션 일관성의 경계입니다. Aggregate 간 직접 객체 참조를 허용하면 다음과 같은 문제가 발생합니다.

- **트랜잭션 경계 침범**: 한 Aggregate를 저장할 때 참조된 다른 Aggregate도 함께 변경될 수 있어 트랜잭션 범위가 불명확해집니다.
- **동시성 충돌**: 서로 다른 Aggregate를 수정하는 트랜잭션이 객체 참조를 통해 간접적으로 충돌합니다.
- **로딩 비용**: 하나의 Aggregate를 로드할 때 참조된 Aggregate까지 연쇄적으로 로드되어 성능이 저하됩니다.
- **배포 결합**: Aggregate 간 직접 의존이 마이크로서비스 분리를 어렵게 만듭니다.

## 검토한 옵션

1. ID(값 타입) 전용 참조 + 도메인 이벤트
2. 직접 객체 참조
3. Lazy Loading
4. Saga 패턴

## 결정

**선택한 옵션: "ID(값 타입) 전용 참조 + 도메인 이벤트"**, Aggregate 간 참조를 ID 값 타입(예: `OrderId`, `ProductId`)으로 제한하고, 교차 Aggregate 일관성은 도메인 이벤트를 통해 최종적 일관성(Eventual Consistency)으로 처리하기 때문입니다.

이 방식은 각 Aggregate의 트랜잭션 경계를 명확히 유지하면서, 강타입 ID가 잘못된 ID 할당을 컴파일 타임에 방지합니다.

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
