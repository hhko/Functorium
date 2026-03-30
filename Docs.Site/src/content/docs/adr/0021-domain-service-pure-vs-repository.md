---
title: "ADR-0021: Domain Service 순수 함수 vs Repository 패턴 이중 전략"
status: "accepted"
date: 2026-03-24
---

## 맥락과 문제

교차 Aggregate 비즈니스 로직을 Domain Service로 구현할 때, 외부 데이터(다른 Aggregate 조회, 중복 검사 등)가 필요한 경우 순수 함수로 유지할 수 없는 딜레마가 발생합니다.

예를 들어 "주문 금액 할인율 계산"은 주문과 고객 등급 정보만으로 순수하게 계산할 수 있지만, "고객 이메일 중복 검사"는 전체 고객 목록을 메모리에 올려야 순수 함수로 구현할 수 있어 비효율적입니다. Application Service에서 모든 외부 데이터를 미리 조회하여 전달하는 방식은 N+1 문제와 도메인 빈혈 모델로 이어집니다.

## 검토한 옵션

1. 순수 함수 기본 + Evans Ch.9 Repository 패턴 전환
2. 항상 순수 함수 (모든 데이터를 매개변수로 전달)
3. 항상 Repository 의존
4. Application Service에서 로직 처리

## 결정

**선택한 옵션: "순수 함수 기본 + Evans Ch.9 Repository 패턴 전환"**, 가능한 한 순수 함수(`static` 메서드, `Fin<T>` 반환)로 구현하되, 대량 데이터 조회가 필요한 경우에만 Evans의 Domain-Driven Design Chapter 9에서 제시한 Repository 접근 방식(`FinT<IO, T>` 반환)으로 전환하기 때문입니다.

- **순수 Domain Service**: `static` 클래스, 모든 입력이 매개변수, `Fin<T>` 반환. 테스트 시 mock 불필요.
- **Repository Domain Service**: 생성자에서 Repository 인터페이스를 주입받아 `FinT<IO, T>` 반환. 대량 데이터 조회가 필요한 경우에 사용.

### 결과

- Good, because 대부분의 Domain Service가 순수 함수로 유지되어 테스트가 간단합니다.
- Good, because 대량 데이터가 필요한 경우 Repository 패턴으로 자연스럽게 전환됩니다.
- Good, because 두 가지 스타일의 선택 기준이 명확합니다 (데이터 크기/범위 기준).
- Bad, because 두 가지 스타일이 공존하여 팀 내 일관성 판단이 필요합니다.

### 확인

- 순수 Domain Service가 외부 의존성 없이 `static`으로 구현되었는지 아키텍처 규칙 테스트로 확인합니다.
- Repository Domain Service가 도메인 레이어의 Repository 인터페이스만 의존하는지 검증합니다.

## 옵션별 장단점

### 순수 함수 기본 + Evans Ch.9 Repository 패턴 전환

- Good, because 순수 함수의 테스트 용이성과 Repository의 효율성을 모두 확보합니다.
- Good, because 전환 기준이 "매개변수로 전달하기에 데이터가 너무 많은가"로 명확합니다.
- Good, because Eric Evans의 DDD 원전과 일치하는 설계입니다.
- Bad, because 두 가지 스타일이 공존하므로 선택 가이드라인이 필요합니다.

### 항상 순수 함수 (모든 데이터를 매개변수로 전달)

- Good, because 모든 Domain Service가 동일한 스타일로 일관됩니다.
- Good, because 테스트 시 mock이 전혀 필요 없습니다.
- Bad, because 대량 데이터 조회 시 Application Service에서 N+1 쿼리가 발생합니다.
- Bad, because 불필요한 데이터까지 미리 로드해야 합니다.

### 항상 Repository 의존

- Good, because 모든 Domain Service가 동일한 스타일로 일관됩니다.
- Good, because 필요한 데이터를 필요한 시점에 조회할 수 있습니다.
- Bad, because 단순한 계산 로직에도 Repository mock이 필요하여 테스트 복잡도가 증가합니다.
- Bad, because 모든 Domain Service가 외부 의존성을 가지게 됩니다.

### Application Service에서 로직 처리

- Good, because Domain Service 없이 구조가 단순합니다.
- Bad, because 비즈니스 로직이 Application 레이어로 유출되어 도메인 빈혈 모델이 됩니다.
- Bad, because 동일한 비즈니스 로직이 여러 Application Service에 중복됩니다.

## 관련 정보

- 관련 커밋: `2731059d`, `d446fcfa`
- 관련 가이드: `Docs.Site/src/content/docs/guides/domain/`
- 참고: Eric Evans, Domain-Driven Design — Chapter 9, Services
