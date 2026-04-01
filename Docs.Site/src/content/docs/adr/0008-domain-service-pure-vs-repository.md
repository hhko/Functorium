---
title: "ADR-0008: Domain - Domain Service 순수 함수 vs Repository 패턴 이중 전략"
status: "accepted"
date: 2026-03-24
---

## 맥락과 문제

교차 Aggregate 비즈니스 로직을 Domain Service로 구현할 때, 외부 데이터 접근이 필요한 경우와 불필요한 경우의 성격이 근본적으로 다릅니다.

"주문 금액에 고객 등급별 할인율을 적용"하는 로직은 `Order`와 `CustomerGrade`를 매개변수로 받아 순수하게 계산할 수 있습니다. 입력이 같으면 결과가 항상 같고, mock 없이 단위 테스트를 작성할 수 있습니다. 반면 "고객 이메일 중복 검사"는 수십만 건의 기존 고객 이메일을 DB에서 조회해야 합니다. Application Service에서 전체 이메일 목록을 미리 조회하여 Domain Service에 전달하면 순수 함수를 유지할 수 있지만, 불필요한 데이터를 대량으로 메모리에 로드하게 되고, 이런 패턴이 누적되면 Application Service가 "데이터 조회 허브"로 비대해지며 도메인 빈혈 모델로 퇴화합니다.

## 검토한 옵션

1. 순수 함수 기본 + Evans Ch.9 Repository 패턴 전환
2. 항상 순수 함수 (모든 데이터를 매개변수로 전달)
3. 항상 Repository 의존
4. Application Service에서 로직 처리

## 결정

**선택한 옵션: "순수 함수 기본 + Evans Ch.9 Repository 패턴 전환"**, 순수 함수의 테스트 용이성을 기본값으로 삼되, 대량 데이터 조회가 불가피한 경우에만 Repository 의존을 허용하여 실용성을 확보하기 위해서입니다. 전환 기준은 명확합니다. "매개변수로 전달하기에 데이터가 너무 많은가?"라는 단일 질문으로 판단합니다.

- **순수 Domain Service**: `static` 클래스, 모든 입력이 매개변수, `Fin<T>` 반환. 할인율 계산, 금액 검증 등 계산 중심 로직에 사용합니다. mock 없이 입력-출력만으로 테스트합니다.
- **Repository Domain Service**: 생성자에서 Repository 인터페이스를 주입받아 `FinT<IO, T>` 반환. 이메일 중복 검사, 재고 가용량 확인 등 대량 데이터 조회가 필요한 경우에 사용합니다.

### 결과

- <span class="adr-good">Good</span>, because 할인율 계산, 금액 검증 등 계산 중심 Domain Service가 `static` 순수 함수로 유지되어 mock 없이 입력-출력만으로 테스트할 수 있습니다.
- <span class="adr-good">Good</span>, because 이메일 중복 검사 같은 대량 데이터 조회가 필요한 경우, 수십만 건을 메모리에 올리는 대신 Repository를 통해 DB에서 직접 확인하여 효율성을 확보합니다.
- <span class="adr-good">Good</span>, because "매개변수로 전달하기에 데이터가 너무 많은가?"라는 단일 기준으로 두 스타일 중 어느 것을 선택할지 명확히 판단할 수 있습니다.
- <span class="adr-bad">Bad</span>, because 순수 함수와 Repository 의존 두 스타일이 공존하므로, 코드 리뷰에서 "이 로직은 순수 함수로 충분한데 왜 Repository를 주입했는가" 같은 설계 판단이 반복적으로 필요합니다.

### 확인

- 순수 Domain Service가 외부 의존성 없이 `static`으로 구현되었는지 아키텍처 규칙 테스트로 확인합니다.
- Repository Domain Service가 도메인 레이어의 Repository 인터페이스만 의존하는지 검증합니다.

## 옵션별 장단점

### 순수 함수 기본 + Evans Ch.9 Repository 패턴 전환

- <span class="adr-good">Good</span>, because 계산 로직은 순수 함수로 mock 없이 테스트하고, 대량 조회 로직은 Repository로 DB 효율성을 확보하여 양쪽의 장점을 취합니다.
- <span class="adr-good">Good</span>, because "이 데이터를 매개변수로 전달할 수 있는가?"라는 단일 기준이 스타일 선택의 모호함을 제거합니다.
- <span class="adr-good">Good</span>, because Eric Evans의 DDD 원전(Chapter 9)에서 제시한 "순수 Domain Service와 Repository 접근 Domain Service의 공존"과 정확히 일치합니다.
- <span class="adr-bad">Bad</span>, because 두 스타일의 코드가 같은 도메인 레이어에 공존하므로 신규 팀원을 위한 선택 가이드라인 문서화가 필요합니다.

### 항상 순수 함수 (모든 데이터를 매개변수로 전달)

- <span class="adr-good">Good</span>, because 모든 Domain Service가 `static` 순수 함수로 통일되어 스타일 선택 판단이 불필요합니다.
- <span class="adr-good">Good</span>, because 테스트 시 Repository mock이 전혀 필요 없어 테스트 코드가 입력-출력 검증으로 단순화됩니다.
- <span class="adr-bad">Bad</span>, because 이메일 중복 검사를 위해 Application Service에서 전체 고객 이메일 목록(수십만 건)을 미리 조회하여 매개변수로 전달해야 하는 N+1 쿼리와 메모리 낭비가 발생합니다.
- <span class="adr-bad">Bad</span>, because Application Service가 "Domain Service에 전달할 데이터를 미리 모두 조회하는 역할"로 비대해지면서 비즈니스 로직 경계가 흐려집니다.

### 항상 Repository 의존

- <span class="adr-good">Good</span>, because 모든 Domain Service가 생성자 주입 + `FinT<IO, T>` 반환으로 통일되어 구조적 일관성이 보장됩니다.
- <span class="adr-good">Good</span>, because 필요한 데이터를 필요한 시점에 DB에서 직접 조회하므로 불필요한 사전 로딩이 없습니다.
- <span class="adr-bad">Bad</span>, because `CalculateDiscount(order, grade)` 같은 순수 계산에도 사용하지 않는 Repository를 mock해야 하여, 테스트가 "Repository가 빈 결과를 반환하면..."같은 불필요한 설정으로 오염됩니다.
- <span class="adr-bad">Bad</span>, because 모든 Domain Service가 외부 의존성을 갖게 되어 단위 테스트의 격리성과 실행 속도가 저하됩니다.

### Application Service에서 로직 처리

- <span class="adr-good">Good</span>, because Domain Service 레이어 없이 Application Service에서 직접 처리하므로 아키텍처 계층이 하나 줄어듭니다.
- <span class="adr-bad">Bad</span>, because 할인율 계산 같은 핵심 비즈니스 로직이 Application 레이어에 위치하여 도메인 모델이 데이터 구조로만 남는 빈혈 모델(Anemic Domain Model)이 됩니다.
- <span class="adr-bad">Bad</span>, because "주문 생성"과 "주문 수정" 두 Usecase에서 동일한 할인율 계산 로직을 각각 구현하게 되어 규칙 변경 시 양쪽을 동시에 수정해야 합니다.

## 관련 정보

- 관련 커밋: `2731059d`, `d446fcfa`
- 관련 가이드: `Docs.Site/src/content/docs/guides/domain/`
- 참고: Eric Evans, Domain-Driven Design — Chapter 9, Services
