# 에릭 에반스 DDD 전술적 설계 관점 갭 분석 및 개선 로드맵

에릭 에반스의 DDD 전술적 설계 패턴 관점에서 현재 Functorium 가이드의 갭을 분석하고 개선 방향을 제시합니다.

## §0. 갭별 영향도 요약

| § | 갭 | 영향도 | 상태 |
|---|-----|--------|------|
| §1 | Factory 패턴, Domain Service, EF Core 통합 | — | ✅ 완료 |
| §2 | 유비쿼터스 언어 일관성 | LOW | 미완 |
| §3 | Bounded Context 경계 정의 | MEDIUM | 미완 |
| §4 | Aggregate 설계 심화 | MEDIUM | 미완 |
| §5 | Domain Event 고급 패턴 | MEDIUM | 미완 |
| §6 | Repository 고급 패턴 | LOW | 미완 |
| §7 | Specification 패턴 고도화 | LOW | 미완 |
| §8 | 모듈 패키징과 DDD 빌딩블록 매핑 | LOW | 미완 |

## §1. 완료 항목

이전 placeholder에 "예정"으로 남아 있던 항목들로, 현재 모두 구현 및 문서화가 완료되었습니다.

### Factory 패턴 — ✅ 완료

`Create()` / `CreateFromValidated()` 이원화가 완료되었습니다.

- `Create()`: 새 Entity 생성, 검증 수행, 새 ID 생성, 도메인 이벤트 발행
- `CreateFromValidated()`: ORM/Repository 복원용, 검증 생략, 기존 ID 사용

[dto-strategy-review.md](../dto-strategy-review.md) §2.3에서 "✅ 우수" 평가를 받았습니다.

> 참고: [06-entities-and-aggregates.md](./06-entities-and-aggregates.md) §8 팩토리 패턴

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

## §4. Aggregate 설계 심화 — MEDIUM

### 현재 상태

[06-entities-and-aggregates.md](./06-entities-and-aggregates.md)에서 4가지 Aggregate 설계 규칙을 제공합니다:

1. Aggregate Root를 통한 접근
2. 트랜잭션 일관성 경계
3. ID 참조를 통한 Aggregate 간 연결
4. 결과적 일관성 (Domain Event)

### 갭

- 복잡한 Aggregate **분할/병합 판단** 사례 부족
- 성능 기반 경계 조정 가이드 없음 (큰 Aggregate → 분할 기준)
- 동시성 충돌이 잦은 경우의 경계 재설계 전략 미기술

### 개선 방향

- Aggregate 경계 재설계 의사결정 트리
- 분할 사례: 락 경합이 높은 큰 Aggregate → 작은 Aggregate + Domain Event
- 병합 사례: 항상 함께 변경되는 분리된 Aggregate → 병합 고려

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

## §6. Repository 고급 패턴 — LOW

### 현재 상태

[12-ports-and-adapters.md](./12-ports-and-adapters.md)에서 `IRepository<TAggregate, TId>` 기반 CRUD + Specification 조회를 다룹니다.

### 갭

- **페이지네이션**: 대량 데이터 조회 시 오프셋/커서 기반 패턴 가이드 없음
- **복합 정렬**: 다중 필드 정렬 표현 방식 미정의
- **읽기 전용 최적화**: Read Model (CQRS의 Q 측) 별도 조회 경로 가이드 없음

### 개선 방향

- 페이지네이션 표현 Value Object 설계 가이드
- Read Model 분리 시점 판단 기준 (Specification으로 충분한 경우 vs 별도 Read Model 필요)

## §7. Specification 패턴 고도화 — LOW

### 현재 상태

[10-specifications.md](./10-specifications.md)에서 And/Or/Not 조합 + Adapter에서 switch 기반 SQL 번역 패턴을 다룹니다.

### 인지된 Trade-off

새 Specification 추가 시 Adapter의 switch 케이스 누락 가능성이 있으며, 누락 시 인메모리 폴백으로 성능 문제가 발생할 수 있습니다. 이 trade-off는 의도적 설계 결정이며, 아키텍처 테스트로 누락을 감지할 수 있습니다.

### 개선 방향

- Expression 기반 자동 번역 고려 (장기 과제)
- 아키텍처 테스트로 switch 케이스 누락 감지 강화

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
