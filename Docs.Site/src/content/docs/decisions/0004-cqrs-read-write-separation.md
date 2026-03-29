---
title: "ADR-0004: CQRS 읽기/쓰기 분리"
status: "accepted"
date: 2026-03-18
---

## 맥락과 문제

단일 ORM(예: EF Core)으로 읽기(Query)와 쓰기(Command)를 모두 처리하면, 읽기 성능을 위한 최적화(프로젝션, 인덱스 힌트, Raw SQL)가 도메인 모델의 무결성을 해치거나 불필요한 변경 추적 오버헤드를 유발합니다. 반대로 Dapper만 사용하면 Aggregate 일관성 경계, 변경 추적, 도메인 이벤트 발행 등 쓰기 측 요구를 충족하기 어렵습니다.

CQRS(Command Query Responsibility Segregation)는 읽기와 쓰기를 분리하여 각각에 최적화된 기술을 적용하는 패턴이지만, 포트 정의 위치와 구현 기술 선택에 대한 명확한 기준이 필요합니다.

## 검토한 옵션

1. Command=IRepository(Domain 정의)+EF Core, Query=IQueryPort(Application 정의)+Dapper
2. EF Core 단일 사용
3. Dapper 단일 사용
4. CQRS 미적용 (현상 유지)

## 결정

**선택한 옵션: "Command=IRepository(Domain 정의)+EF Core, Query=IQueryPort(Application 정의)+Dapper"**, 쓰기는 Aggregate 일관성과 도메인 이벤트를 EF Core의 변경 추적으로 보호하고, 읽기는 Dapper의 Raw SQL로 프로젝션 최적화하여 각 측면에 최적의 도구를 적용하기 때문입니다.

### 결과

- Good, because 쓰기 측은 EF Core의 변경 추적으로 Aggregate 일관성을 보장하고 도메인 이벤트를 자동 발행합니다.
- Good, because 읽기 측은 Dapper로 필요한 컬럼만 프로젝션하여 성능을 최적화합니다.
- Good, because IRepository는 Domain 레이어, IQueryPort는 Application 레이어에 정의되어 의존 방향이 명확합니다.
- Bad, because 두 가지 데이터 접근 기술을 유지해야 하므로 학습 비용이 증가합니다.
- Bad, because 동일 엔티티에 대해 EF Core 매핑과 Dapper 쿼리를 별도로 관리해야 합니다.

### 확인

- Command 핸들러가 `IRepository<T>`를 통해 EF Core로 영속하는지 확인합니다.
- Query 핸들러가 `IQueryPort`를 통해 Dapper로 조회하는지 확인합니다.
- IRepository가 Domain 프로젝트, IQueryPort가 Application 프로젝트에 위치하는지 아키텍처 테스트로 검증합니다.

## 옵션별 장단점

### Command=IRepository+EF Core, Query=IQueryPort+Dapper

- Good, because 각 측면에 최적화된 기술을 적용하여 성능과 일관성을 동시에 달성합니다.
- Good, because 포트 정의 위치가 레이어 책임에 맞게 분리됩니다(Domain vs Application).
- Bad, because 두 기술의 학습 및 운영 비용이 발생합니다.

### EF Core 단일 사용

- Good, because 단일 기술로 통일되어 학습 비용이 낮습니다.
- Good, because 변경 추적, 마이그레이션, 도메인 이벤트가 자연스럽게 통합됩니다.
- Bad, because 읽기 쿼리에서 불필요한 변경 추적 오버헤드가 발생합니다.
- Bad, because 복잡한 프로젝션이나 Raw SQL 최적화가 제한됩니다.

### Dapper 단일 사용

- Good, because SQL을 직접 작성하여 최대 성능을 달성합니다.
- Bad, because 변경 추적이 없어 Aggregate 일관성을 수동으로 관리해야 합니다.
- Bad, because 도메인 이벤트 발행을 인프라에서 직접 구현해야 합니다.

### CQRS 미적용 (현상 유지)

- Good, because 단일 모델로 구조가 단순합니다.
- Bad, because 읽기/쓰기 최적화가 서로 충돌하여 양쪽 모두 타협해야 합니다.

## 관련 정보

- 관련 커밋: `074e2475` feat(books/cqrs): CQRS Repository와 Query 패턴 학습 Book 추가
- 관련 커밋: `6b027c31` refactor(cqrs-repository): Part4 Usecase 코드 최신 Mediator/IQueryPort API로 동기화
- 관련 문서: `Docs.Site/src/content/docs/tutorials/cqrs-repository/`
