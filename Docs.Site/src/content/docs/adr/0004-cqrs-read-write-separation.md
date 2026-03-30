---
title: "ADR-0004: CQRS 읽기/쓰기 분리"
status: "accepted"
date: 2026-03-18
---

## 맥락과 문제

상품 목록 조회 API를 생각해 봅니다. 화면에 필요한 것은 이름, 가격, 카테고리 세 컬럼뿐인데, EF Core로 조회하면 Aggregate 전체가 로드되고 변경 추적까지 활성화됩니다. `AsNoTracking`을 붙여도 도메인 모델을 통째로 매핑하는 오버헤드는 남습니다. 반대로 주문 생성 시에는 Aggregate 일관성 경계를 지키고, OrderItem 추가 시 도메인 이벤트를 발행해야 하는데, Dapper의 Raw SQL로는 변경 추적도, 이벤트 자동 수집도 불가능합니다.

읽기와 쓰기가 요구하는 최적화 방향이 정반대이므로, 단일 기술로 양쪽을 만족시키면 필연적으로 한쪽이 타협합니다. CQRS 패턴을 적용하되, 쓰기 포트(IRepository)와 읽기 포트(IQueryPort)를 어느 레이어에 정의하고 어떤 구현 기술과 짝지을지에 대한 명확한 기준이 필요합니다.

## 검토한 옵션

1. Command=IRepository(Domain 정의)+EF Core, Query=IQueryPort(Application 정의)+Dapper
2. EF Core 단일 사용
3. Dapper 단일 사용
4. CQRS 미적용 (현상 유지)

## 결정

**선택한 옵션: "Command=IRepository(Domain 정의)+EF Core, Query=IQueryPort(Application 정의)+Dapper"**. 쓰기 측에서는 EF Core의 변경 추적으로 Aggregate 일관성 경계를 보호하고 도메인 이벤트를 `SaveChanges` 시점에 자동 수집합니다. 읽기 측에서는 Dapper의 Raw SQL로 필요한 컬럼만 프로젝션하여 불필요한 매핑과 변경 추적을 제거합니다. 각 측면에 최적의 도구를 적용하여 성능과 일관성 모두를 달성합니다.

### 결과

- Good, because 주문 생성 시 EF Core의 변경 추적이 Aggregate 루트와 하위 엔티티의 일관성을 보장하고, `SaveChanges` 시점에 도메인 이벤트를 자동 수집하여 발행합니다.
- Good, because 상품 목록 조회 시 Dapper로 `SELECT Name, Price, Category` 수준의 프로젝션만 실행하여, Aggregate 전체 로드 대비 불필요한 매핑과 메모리 할당을 제거합니다.
- Good, because IRepository는 Domain 레이어(Aggregate 생명주기 관리), IQueryPort는 Application 레이어(유스케이스 조회 요구)에 정의되어 각 포트의 의존 방향이 레이어 책임과 일치합니다.
- Bad, because EF Core 매핑 설정과 Dapper SQL 쿼리를 모두 다룰 수 있는 팀 역량이 필요하며, 기술 스택이 이원화됩니다.
- Bad, because 동일 엔티티의 컬럼이 변경되면 EF Core의 Fluent API 매핑과 Dapper의 SQL 쿼리를 각각 수정해야 하므로 동기화 누락 위험이 있습니다.

### 확인

- Command 핸들러가 `IRepository<T>`를 통해 EF Core로 영속하는지 확인합니다.
- Query 핸들러가 `IQueryPort`를 통해 Dapper로 조회하는지 확인합니다.
- IRepository가 Domain 프로젝트, IQueryPort가 Application 프로젝트에 위치하는지 아키텍처 테스트로 검증합니다.

## 옵션별 장단점

### Command=IRepository+EF Core, Query=IQueryPort+Dapper

- Good, because 쓰기는 EF Core의 변경 추적/이벤트 수집, 읽기는 Dapper의 Raw SQL 프로젝션으로, 각 측면에 최적화된 기술이 서로를 방해하지 않습니다.
- Good, because IRepository가 Domain 레이어에, IQueryPort가 Application 레이어에 위치하여, 읽기 최적화가 도메인 모델을 오염시키지 않습니다.
- Bad, because 팀이 EF Core(마이그레이션, Fluent API)와 Dapper(Raw SQL, 파라미터 매핑)를 모두 숙지해야 하므로 운영 비용이 이원화됩니다.

### EF Core 단일 사용

- Good, because 단일 기술로 통일되어 팀 온보딩이 빠르고 학습 비용이 낮습니다.
- Good, because 변경 추적, 마이그레이션, 도메인 이벤트 수집이 하나의 DbContext 안에서 자연스럽게 통합됩니다.
- Bad, because 상품 목록처럼 읽기 전용 조회에서도 변경 추적기(ChangeTracker)가 활성화되어 불필요한 메모리와 CPU를 소모합니다.
- Bad, because 다중 테이블 조인이나 윈도우 함수 같은 복잡한 프로젝션에서 LINQ 표현력이 부족하여, 결국 `FromSqlRaw`로 우회하게 되고 EF Core의 이점이 반감됩니다.

### Dapper 단일 사용

- Good, because SQL을 직접 작성하여 데이터베이스 수준의 최대 성능을 달성하며, 쿼리 실행 계획을 완전히 제어할 수 있습니다.
- Bad, because 변경 추적이 없어, 주문과 OrderItem을 함께 저장할 때 Aggregate 일관성 경계를 개발자가 수동 SQL로 보장해야 합니다.
- Bad, because 도메인 이벤트 수집 메커니즘이 없어, `SaveChanges` 인터셉터 같은 자동화 대신 이벤트 발행 코드를 매 Repository 메서드마다 직접 작성해야 합니다.

### CQRS 미적용 (현상 유지)

- Good, because 읽기와 쓰기가 동일 모델을 공유하여 구조가 단순하고 이해하기 쉽습니다.
- Bad, because 읽기 최적화(프로젝션, 인덱스 힌트)를 적용하면 도메인 모델이 조회 요구에 맞춰 왜곡되고, 쓰기 최적화(변경 추적, 이벤트)를 유지하면 읽기 성능이 희생되어 양쪽 모두 타협하게 됩니다.

## 관련 정보

- 관련 커밋: `074e2475` feat(books/cqrs): CQRS Repository와 Query 패턴 학습 Book 추가
- 관련 커밋: `6b027c31` refactor(cqrs-repository): Part4 Usecase 코드 최신 Mediator/IQueryPort API로 동기화
- 관련 문서: `Docs.Site/src/content/docs/tutorials/cqrs-repository/`
