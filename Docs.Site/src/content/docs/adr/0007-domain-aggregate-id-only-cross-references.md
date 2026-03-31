---
title: "ADR-0007: Aggregate 간 ID 전용 참조"
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

- <span class="adr-good">Good</span>, because `Order` 저장 시 `Customer`의 변경 추적이 포함되지 않아 트랜잭션 경계가 Aggregate 단위로 물리적으로 격리됩니다.
- <span class="adr-good">Good</span>, because `CustomerId`를 기대하는 곳에 `ProductId`를 전달하면 컴파일 오류가 발생하여 잘못된 ID 할당이 런타임 전에 차단됩니다.
- <span class="adr-good">Good</span>, because `Order` 로드 시 `Customer`, `Address` 등 관련 테이블 조인이 발생하지 않아 쿼리가 단일 테이블 범위에 한정됩니다.
- <span class="adr-good">Good</span>, because Aggregate 간 의존이 ID 값뿐이므로 고객 서비스를 별도 마이크로서비스로 분리할 때 코드 변경 없이 경계를 자를 수 있습니다.
- <span class="adr-bad">Bad</span>, because "주문과 고객 정보를 함께 표시"하려면 `OrderId`로 주문을 조회한 뒤 `CustomerId`로 별도 쿼리를 수행하거나 Read Model을 구성해야 합니다.
- <span class="adr-bad">Bad</span>, because 즉시 일관성(Strong Consistency) 대신 최종적 일관성(Eventual Consistency)을 채택하므로, 이벤트 처리 지연 동안의 일시적 불일치를 비즈니스적으로 허용할 수 있는지 판단이 필요합니다.

### 확인

- Aggregate Root가 다른 Aggregate의 엔티티를 직접 참조하지 않는지 아키텍처 규칙 테스트로 검증합니다.
- 교차 Aggregate 참조가 ID 값 타입으로만 이루어지는지 코드 리뷰에서 점검합니다.

## 옵션별 장단점

### ID(값 타입) 전용 참조 + 도메인 이벤트

- <span class="adr-good">Good</span>, because 각 Aggregate가 자신의 테이블만 잠그므로 동시성 충돌 범위가 최소화됩니다.
- <span class="adr-good">Good</span>, because `OrderId`, `CustomerId` 등 강타입 ID가 원시 타입 혼용을 컴파일 타임에 차단합니다.
- <span class="adr-good">Good</span>, because Aggregate 간 통신이 도메인 이벤트를 통해 이루어져 느슨한 결합과 독립적 배포가 가능합니다.
- <span class="adr-bad">Bad</span>, because "주문 목록에 고객명 표시" 같은 교차 Aggregate 읽기에 별도 쿼리 또는 비정규화된 Read Model이 필요하여 읽기 복잡도가 증가합니다.

### 직접 객체 참조

- <span class="adr-good">Good</span>, because `order.Customer.Name` 같은 네비게이션 프로퍼티로 관련 데이터에 직관적으로 접근할 수 있습니다.
- <span class="adr-bad">Bad</span>, because `Order` 저장 시 EF Core가 `Customer` 변경까지 추적하여 의도하지 않은 데이터 변경이 같은 트랜잭션에 포함되고, 서로 다른 주문을 수정하는 트랜잭션이 `Customer` 행에서 동시성 충돌을 일으킵니다.
- <span class="adr-bad">Bad</span>, because `Order` → `Customer` → `Address` → `Region` 같은 연쇄 로딩이 발생하여 단순 주문 조회에 다중 테이블 조인이 수반됩니다.
- <span class="adr-bad">Bad</span>, because `Order`가 `Customer` 타입에 컴파일 타임 의존하므로, 고객 도메인을 별도 서비스로 분리할 때 Aggregate 경계에서 코드를 분리할 수 없습니다.

### Lazy Loading

- <span class="adr-good">Good</span>, because 초기 로드 시점에는 관련 Aggregate를 로드하지 않아 즉각적인 성능 비용이 없습니다.
- <span class="adr-bad">Bad</span>, because 주문 목록 100건을 순회하며 각 `order.Customer`에 접근하면 100개의 개별 SELECT 쿼리가 발생하는 N+1 문제가 나타납니다.
- <span class="adr-bad">Bad</span>, because Lazy Loading 프록시가 도메인 모델에 EF Core 의존성을 주입하여 도메인 레이어의 순수성이 훼손됩니다.
- <span class="adr-bad">Bad</span>, because 지연 로드된 `Customer` 객체가 여전히 같은 DbContext에서 변경 추적되므로 트랜잭션 경계 침범 문제는 해소되지 않습니다.

### Saga 패턴

- <span class="adr-good">Good</span>, because 서로 다른 데이터베이스에 걸친 장기 실행 비즈니스 프로세스를 보상 트랜잭션으로 관리할 수 있습니다.
- <span class="adr-bad">Bad</span>, because 동일 데이터베이스 내에서 Aggregate 간 참조를 끊는 문제에 Saga를 적용하면, 메시지 브로커와 Saga 오케스트레이터 같은 인프라가 추가로 필요하여 복잡도가 과도합니다.
- <span class="adr-bad">Bad</span>, because 각 단계의 보상 로직과 상태 머신을 설계/테스트해야 하므로 ID 전용 참조 + 도메인 이벤트 방식 대비 구현 비용이 크게 증가합니다.

## 관련 정보

- 관련 커밋: `71272343`
- 관련 가이드: `Docs.Site/src/content/docs/guides/domain/06a-aggregate-design`
- 참고: Vaughn Vernon, Implementing Domain-Driven Design — Chapter 10, Aggregates
