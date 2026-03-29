---
title: "아키텍처 의사결정 기록"
---

## 개요

이 섹션은 Functorium 프레임워크의 주요 아키텍처 의사결정을 기록합니다. 각 결정은 **MADR(Markdown Any Decision Records) v4.0** 템플릿을 따르며, 결정의 맥락, 검토한 대안, 선택 근거, 그리고 예상되는 결과를 구조화합니다.

### 왜 ADR인가

아키텍처 결정은 코드와 커밋 메시지에 흩어져 있으면 시간이 지날수록 "왜 이렇게 했는가"를 추적하기 어렵습니다. ADR은 결정 시점의 맥락을 보존하여, 새로운 팀원이 합류하거나 기존 결정을 재검토할 때 판단 근거를 빠르게 파악할 수 있게 합니다.

### 상태 범례

| 상태 | 설명 |
|------|------|
| `accepted` | 채택되어 현재 적용 중인 결정 |
| `proposed` | 제안되었으나 아직 합의되지 않은 결정 |
| `deprecated` | 더 이상 유효하지 않은 결정 |
| `superseded` | 새로운 ADR로 대체된 결정 |

### ADR 목록

| 번호 | 제목 | 상태 | 날짜 |
|------|------|------|------|
| [ADR-0001](./0001-adopt-architecture-decision-records/) | 아키텍처 의사결정 기록(ADR) 도입 | accepted | 2026-03-30 |
| [ADR-0002](./0002-use-fin-over-exceptions/) | 예외 대신 Fin 타입으로 실패 표현 | accepted | 2026-03-26 |
| [ADR-0003](./0003-languageext-as-functional-foundation/) | LanguageExt를 함수형 기반 라이브러리로 채택 | accepted | 2026-03-15 |
| [ADR-0004](./0004-cqrs-read-write-separation/) | CQRS 읽기/쓰기 분리 | accepted | 2026-03-18 |
| [ADR-0005](./0005-pipeline-execution-order/) | 파이프라인 실행 순서 | accepted | 2026-03-22 |
| [ADR-0006](./0006-observable-port-source-generator/) | Observable Port 소스 제너레이터 | accepted | 2026-03-20 |
| [ADR-0007](./0007-ctx-enricher-pillar-targeting/) | CtxEnricher Pillar 타겟팅 전략 | accepted | 2026-03-24 |
| [ADR-0008](./0008-ulid-based-entity-id/) | Ulid 기반 Entity ID | accepted | 2026-03-16 |
| [ADR-0009](./0009-error-classification-three-types/) | 오류 분류 3-Type 체계 | accepted | 2026-03-22 |
| [ADR-0010](./0010-suffix-naming-for-persistence/) | 영속성 클래스 접미사 네이밍 | accepted | 2026-03-27 |
| [ADR-0011](./0011-architecture-test-suite-framework/) | 아키텍처 테스트 Suite 프레임워크 | accepted | 2026-03-20 |
| [ADR-0012](./0012-finresponse-pipeline-type-constraints/) | FinResponse 파이프라인 타입 제약 | accepted | 2026-03-18 |
| [ADR-0013](./0013-domain-event-publisher-simplification/) | 도메인 이벤트 Publisher 단순화 | accepted | 2026-03-24 |
| [ADR-0014](./0014-validation-normalize-then-maxlength/) | 검증 순서 Normalize → MaxLength | accepted | 2026-03-26 |
| [ADR-0015](./0015-union-valueoject-state-machine/) | UnionValueObject 상태 머신 | accepted | 2026-03-20 |
| [ADR-0016](./0016-observability-field-naming-snake-case-dot/) | 관측성 필드 네이밍 snake_case + dot | accepted | 2026-03-22 |
| [ADR-0017](./0017-specification-pattern-expression-tree/) | Specification + Expression Tree | accepted | 2026-03-20 |
| [ADR-0018](./0018-explicit-transaction-support/) | 명시적 트랜잭션 지원 | accepted | 2026-03-22 |
| [ADR-0019](./0019-domain-event-tracing-correlation/) | DomainEvent 추적 및 상관관계 ID | accepted | 2026-03-23 |
| [ADR-0020](./0020-aggregate-id-only-cross-references/) | Aggregate 간 ID 전용 참조 | accepted | 2026-03-22 |
| [ADR-0021](./0021-domain-service-pure-vs-repository/) | Domain Service 순수 vs Repository | accepted | 2026-03-24 |
| [ADR-0022](./0022-value-object-class-record-duality/) | Value Object Class/Record 이중 계층 | accepted | 2026-03-20 |
| [ADR-0023](./0023-error-code-sealed-record-hierarchy/) | 에러 코드 sealed record 계층 | accepted | 2026-03-20 |

### 참고

- [MADR v4.0 — Markdown Any Decision Records](https://adr.github.io/madr/)
- [Michael Nygard의 원본 ADR 제안](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
