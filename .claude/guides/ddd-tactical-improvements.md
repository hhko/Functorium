# 에릭 에반스 DDD 전술적 설계 관점 갭 분석 및 개선 로드맵

에릭 에반스의 DDD 전술적 설계 패턴 관점에서 현재 Functorium 가이드의 갭을 분석하고 개선 방향을 제시합니다.

## §0. 갭별 영향도 요약

| § | 갭 | 영향도 | 상태 |
|---|-----|--------|------|
| §1 | Factory 패턴, Domain Service, EF Core 통합 | — | ✅ 완료 |
| §2 | 유비쿼터스 언어 일관성 | LOW | 미완 |
| §3 | Bounded Context 경계 정의 | MEDIUM | 미완 |
| §4 | Aggregate 설계 심화 | MEDIUM | ✅ 완료 |
| §5 | Domain Event 고급 패턴 | MEDIUM | 미완 |
| §6 | Repository 고급 패턴 | LOW | ✅ 완료 |
| §7 | Specification 패턴 고도화 | LOW | 미완 |
| §8 | 모듈 패키징과 DDD 빌딩블록 매핑 | LOW | 미완 |

## §1. 완료 항목

이전 placeholder에 "예정"으로 남아 있던 항목들로, 현재 모두 구현 및 문서화가 완료되었습니다.

### Factory 패턴 — ✅ 완료

`Create()` / `CreateFromValidated()` 이원화가 완료되었습니다.

- `Create()`: 새 Entity 생성, 검증 수행, 새 ID 생성, 도메인 이벤트 발행
- `CreateFromValidated()`: ORM/Repository 복원용, 검증 생략, 기존 ID 사용

[dto-strategy-review.md](../dto-strategy-review.md) §2.3에서 "✅ 우수" 평가를 받았습니다.

> 참고: [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) §8 생성 패턴

### Domain Service — ✅ 완료

`IDomainService` 마커 인터페이스와 `OrderCreditCheckService` 구현이 완료되었습니다.

- 마커 인터페이스: `Functorium.Domains.Services.IDomainService`
- 구현 예시: 교차 Aggregate 순수 도메인 로직
- DI 등록: Singleton 패턴
- Usecase 통합: 생성자 주입 + `FinT<IO, T>` LINQ auto-lifting

> 참고: [09-domain-services.md](./09-domain-services.md)

### EF Core 통합 — ✅ 완료

Persistence Model 분리 + Mapper 패턴이 완료되었습니다.

- Persistence Model: POCO, primitive 타입만 사용
- Mapper: Domain Entity ↔ Persistence Model 변환 (확장 메서드)
- Repository: `IO.liftAsync` 패턴, `ToModel()`/`ToDomain()` 변환

> 참고: [12-ports-and-adapters.md](./12-ports-and-adapters.md) §2.8 EF Core Repository

## §2. 유비쿼터스 언어 일관성 — LOW

### 현재 상태

코드에서 도메인 타입명을 직접 사용하고 있어 양호합니다 (`Email`, `Money`, `Quantity`, `ProductName` 등의 Value Object).

### 갭

가이드 문서에서 유비쿼터스 언어 **수립 프로세스**가 기술되어 있지 않습니다.

- 도메인 용어집(Glossary) 템플릿 없음
- 도메인 전문가 협업 패턴 미기술
- 코드 네이밍과 도메인 용어의 매핑 가이드라인 없음

### 개선 방향

- 용어집 템플릿과 네이밍 가이드라인 추가 고려
- 도메인 전문가와의 용어 합의 프로세스 가이드

## §3. Bounded Context 경계 정의 — MEDIUM

### 현재 상태

모든 가이드가 **단일 서비스 내 3-Layer 구조**만 다루고 있습니다. [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md)의 레이어 아키텍처 섹션은 Domain → Application → Adapter 단일 서비스 관점입니다.

### 갭

- 서비스 간 **Bounded Context 경계 정의** 부재
- Context Map 패턴 미기술 (Shared Kernel, Customer-Supplier, ACL 등)
- 서비스 간 통합 전략 가이드 없음

### 개선 방향

- Context Map 패턴 가이드 작성 (ACL, Shared Kernel, Open Host Service, Published Language)
- 서비스 간 Bounded Context 경계 설계 기준
- Functorium에서 Multi-Context 구현 시 프로젝트 구조 가이드

## §4. Aggregate 설계 심화 — ✅ 완료

### 완료 내용

Product Aggregate를 Product(카탈로그) + Inventory(재고)로 분할하여 구체적 Before/After 사례를 구현했습니다.

- **프레임워크**: `IConcurrencyAware` 인터페이스 추가 (낙관적 동시성 제어 믹스인)
- **분할 구현**: Product에서 `StockQuantity`/`DeductStock()`/`HasLowStock()` 제거, Inventory Aggregate 신설
- **Inventory Aggregate**: `IConcurrencyAware` 구현, `RowVersion` 보유, `DeductStock()`/`AddStock()` 메서드
- **Application 계층**: `CreateProductCommand`가 Product + Inventory 동시 생성, `DeductStockCommand`가 Inventory 사용
- **Persistence 계층**: `InventoryModel`, `InventoryConfiguration`(IsRowVersion), EfCore/InMemory Repository
- **가이드 문서**: [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) §4에 분할/병합 의사결정 트리, 동시성 가이드 통합
- **트랜잭션 경계 가이드라인**: 동시 생성 허용 조건, 실제 코드 대조
- **동시성 충돌 처리 전략**: Fail-Fast 전략 문서화, 에러 흐름 정리

> 참고: [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) §4 Aggregate 경계 설정 실전 예제

## §5. Domain Event 고급 패턴 — MEDIUM

### 현재 상태

현재 구현 및 문서화된 항목:

- `IDomainEvent` / `DomainEvent` 기반 클래스
- Event Handler (`IEventHandler<TEvent>`)
- CorrelationId / CausationId 추적
- Usecase Pipeline에서 자동 발행

> 참고: [07-domain-events.md](./07-domain-events.md)

### 갭

서비스 성숙도가 높아질 때 필요한 고급 패턴이 미기술되어 있습니다.

- **Event Versioning**: 이벤트 스키마 변경 시 하위 호환 전략
- **Saga / Process Manager**: 다중 Aggregate 간 장기 트랜잭션 조율
- **Outbox 패턴**: DB 트랜잭션과 이벤트 발행의 원자성 보장
- **이벤트 재처리 전략**: 멱등성(Idempotency) 보장 패턴

### 개선 방향

서비스 성숙도에 따라 단계적으로 가이드를 추가합니다:

1. Outbox 패턴 (트랜잭션 안정성)
2. Event Versioning (스키마 진화)
3. Saga / Process Manager (복잡한 워크플로우)

## §6. Repository 고급 패턴 — ✅ 완료

### 완료 내용

페이지네이션, 복합 정렬, 읽기 전용 최적화(Read Model)를 프레임워크 타입 + SingleHost 적용으로 구현했습니다.

- **프레임워크 타입**: `PageRequest`, `PagedResult<T>`, `SortExpression`, `SortField`, `SortDirection` (`Functorium.Applications.Queries`)
- **Query Adapter 패턴**: Repository(Command 측)와 분리된 Query 전용 포트 (`IProductQueryAdapter`, `IInventoryQueryAdapter`)
- **`IQueryAdapter<TEntity, TDto>`**: 프레임워크 제네릭 인터페이스 — Search 시그니처 자동 제공
- **CQRS 기술 분리**: Command 측은 EF Core (변경 추적, UnitOfWork), Query 측은 **Dapper** (성능, SQL 튜닝)
- **`DapperQueryAdapterBase<TEntity, TDto>`**: 프레임워크 베이스 클래스 (`Functorium.Adapters.Repositories`) — Search 실행, ORDER BY, 페이지네이션, Params 헬퍼를 제공하고, 서브클래스는 SQL 선언(SelectSql, CountSql)과 BuildWhereClause만 담당
- **Dapper Query Adapter**: 베이스 클래스 상속 + 명시적 SQL 선언, Specification → WHERE 패턴 매칭, AllowedSortColumns로 SQL 인젝션 방지. JOIN 쿼리도 SelectSql/CountSql로 자유롭게 작성 가능
- **InMemory Query Adapter**: Repository 위임 후 인메모리 정렬/페이지네이션
- **SearchProductsQuery**: `IProductQueryAdapter` 사용, 페이지네이션/정렬 파라미터 지원
- **SearchInventoryQuery**: `IInventoryQueryAdapter` 사용, LowStock 필터 + 페이지네이션
- **Endpoint**: `GET /api/products/search`, `GET /api/inventories/search`

> 참고: [12-ports-and-adapters.md](./12-ports-and-adapters.md) §2.6 Query Adapter 패턴

## §7. Specification 패턴 고도화 — ✅ 완료

### 이전 상태

switch 기반 pattern-match SQL 번역 → 새 Specification 추가 시 Adapter switch 케이스 수동 추가 필요, 누락 시 런타임 오류.

### 개선 내용

Expression 기반 자동 번역으로 개선 완료:

- **`ExpressionSpecification<T>`**: `ToExpression()` 구현으로 `IsSatisfiedBy()` 자동 제공
- **`SpecificationExpressionResolver`**: And/Or/Not 조합 시 Expression 자동 합성
- **`PropertyMap<TEntity, TModel>`**: Entity Expression → Model Expression 자동 변환
- **EF Core Repository (Command 측)**: `BuildQuery()`에서 `PropertyMap.Translate()`로 자동 SQL 번역
- **Dapper Query Adapter (Query 측)**: Specification 패턴 매칭으로 명시적 SQL WHERE 변환 — 새 Specification 추가 시 switch 케이스 추가 필요

> **참고**: Command 측(EF Core)은 Expression 자동 변환의 이점을, Query 측(Dapper)은 명시적 SQL의 성능/튜닝 이점을 각각 취합니다.

> 참고: [10-specifications.md](./10-specifications.md)

## §8. 모듈 패키징과 DDD 빌딩블록 매핑 — LOW

### 현재 상태

[01-project-structure.md](./01-project-structure.md)에서 8-프로젝트 매핑을 정의하고, [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) §5에서 레이어별 빌딩블록 배치를 다룹니다.

### 갭

에릭 에반스의 **Module** 개념과 .NET 프로젝트 구조의 매핑 근거가 명시되어 있지 않습니다. 현재는 기술적 관심사(Layer)로 프로젝트를 분할하지만, DDD의 Module은 도메인 개념의 응집도를 기준으로 합니다.

### 개선 방향

- Evans의 Module 패턴과 현재 프로젝트 구조의 매핑 근거 문서화
- 단일 Aggregate 서비스 → Multi-Aggregate 서비스 확장 시 모듈 분리 기준

## 참고 문서

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) — Entity와 Aggregate
- [07-domain-events.md](./07-domain-events.md) — 도메인 이벤트
- [09-domain-services.md](./09-domain-services.md) — 도메인 서비스
- [10-specifications.md](./10-specifications.md) — Specification 패턴
- [12-ports-and-adapters.md](./12-ports-and-adapters.md) — Port와 Adapter
- [architecture-improvements.md](./architecture-improvements.md) — 헥사고날 아키텍처 갭 분석
