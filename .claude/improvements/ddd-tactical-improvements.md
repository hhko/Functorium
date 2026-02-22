# 에릭 에반스 DDD 전술적 설계 관점 갭 분석 및 개선 로드맵

에릭 에반스의 DDD 전술적 설계 패턴 관점에서 현재 Functorium 가이드의 갭을 분석하고 개선 방향을 제시합니다.

## §0. 갭별 영향도 요약

| § | 갭 | 영향도 | 상태 |
|---|-----|--------|------|
| §1 | Factory 패턴, Domain Service, EF Core 통합 | — | ✅ 완료 |
| §2 | 유비쿼터스 언어 일관성 | LOW | ✅ 완료 |
| §3 | Bounded Context 경계 정의 | MEDIUM | ✅ 완료 |
| §4 | Aggregate 설계 심화 | MEDIUM | ✅ 완료 |
| §5 | Domain Event 고급 패턴 | MEDIUM | 미완 |
| §6 | Repository 고급 패턴 | LOW | ✅ 완료 |
| §7 | Specification 패턴 고도화 | LOW | ✅ 완료 |
| §8 | 모듈 패키징과 DDD 빌딩블록 매핑 | LOW | ✅ 완료 |

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

> 참고: [13-adapters.md](./13-adapters.md) §2.8 EF Core Repository

## §2. 유비쿼터스 언어 일관성 — ✅ 완료

### 완료 내용

모든 빌딩블록의 네이밍 패턴 참조 테이블, 용어집 템플릿, 도메인 전문가 협업 원칙을 중앙 참조 문서로 통합했습니다.

> 참고: [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) §7 유비쿼터스 언어와 네이밍 가이드

## §3. Bounded Context 경계 정의 — ✅ 완료

### 완료 내용

Context Map 7개 표준 패턴과 Functorium 매핑, SingleHost 선행 패턴 식별, Multi-Context 프로젝트 구조, §6 진화 경로(WHEN)와의 관계(HOW)를 문서화했습니다.

> 참고: [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) §8 Bounded Context와 Context Map

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
- **`IQueryPort<TEntity, TDto>`**: 프레임워크 제네릭 인터페이스 — Search 시그니처 자동 제공
- **CQRS 기술 분리**: Command 측은 EF Core (변경 추적, UnitOfWork), Query 측은 **Dapper** (성능, SQL 튜닝)
- **`DapperQueryAdapterBase<TEntity, TDto>`**: 프레임워크 베이스 클래스 (`Functorium.Adapters.Repositories`) — Search 실행, ORDER BY, 페이지네이션, Params 헬퍼를 제공하고, 서브클래스는 SQL 선언(SelectSql, CountSql)과 BuildWhereClause만 담당
- **Dapper Query Adapter**: 베이스 클래스 상속 + 명시적 SQL 선언, Specification → WHERE 패턴 매칭, AllowedSortColumns로 SQL 인젝션 방지. JOIN 쿼리도 SelectSql/CountSql로 자유롭게 작성 가능
- **InMemory Query Adapter**: Repository 위임 후 인메모리 정렬/페이지네이션
- **SearchProductsQuery**: `IProductQueryAdapter` 사용, 페이지네이션/정렬 파라미터 지원
- **SearchInventoryQuery**: `IInventoryQueryAdapter` 사용, LowStock 필터 + 페이지네이션
- **Endpoint**: `GET /api/products/search`, `GET /api/inventories/search`

> 참고: [13-adapters.md](./13-adapters.md) §2.6 Query Adapter 패턴

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

## §8. 모듈 패키징과 DDD 빌딩블록 매핑 — ✅ 완료

### 완료 내용

Evans의 Module 개념과 Functorium .NET 프로젝트 구조의 매핑 근거를 문서화했습니다.

- **Evans의 Module 개념**: 정의(응집도 기반 그룹화), 핵심 원칙(높은 응집도, 낮은 결합도, 커뮤니케이션)
- **이중 축 매핑**: Layer(수평, .csproj) × Module(수직, 폴더/네임스페이스) ASCII 그리드 다이어그램
- **SingleHost 모듈 경계**: Products/Inventories/Orders/Customers/SharedKernel 각 모듈의 Domain·Application·Adapter 요소 매핑
- **모듈 응집도 규칙**: Module 내부 배치 vs SharedKernel/프로젝트 루트 이동 기준
- **Multi-Aggregate 확장 가이드**: 3단계 진화 경로(단일 Aggregate → Multi-Aggregate 동일 서비스 → 별도 Bounded Context) + 분리 판단 기준 테이블

> 참고: [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) §6 모듈과 프로젝트 구조 매핑

## 참고 문서

- [04-ddd-tactical-overview.md](./04-ddd-tactical-overview.md) — DDD 전술적 설계 개요
- [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) — Entity와 Aggregate
- [07-domain-events.md](./07-domain-events.md) — 도메인 이벤트
- [09-domain-services.md](./09-domain-services.md) — 도메인 서비스
- [10-specifications.md](./10-specifications.md) — Specification 패턴
- [12-ports.md](./12-ports.md) — Port 아키텍처
- [architecture-improvements.md](./architecture-improvements.md) — 헥사고날 아키텍처 갭 분석
