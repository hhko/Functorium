---
title: "아키텍처 의사결정 기록"
---

## 개요

Functorium 프레임워크의 주요 아키텍처 의사결정을 기록합니다. 각 결정은 [MADR v4.0](https://adr.github.io/madr/) 템플릿을 따르며, 결정의 맥락, 검토한 대안, 선택 근거, 예상되는 결과를 구조화합니다.

### 왜 ADR인가

아키텍처 결정은 코드와 커밋 메시지에 흩어지면 시간이 지날수록 "왜 이렇게 했는가"를 추적하기 어렵습니다. ADR은 결정 시점의 맥락을 보존하여, 새로운 팀원이 합류하거나 기존 결정을 재검토할 때 판단 근거를 빠르게 파악할 수 있게 합니다.

### 상태 범례

| 상태 | 설명 |
|------|------|
| `accepted` | 채택되어 현재 적용 중인 결정 |
| `proposed` | 제안되었으나 아직 합의되지 않은 결정 |
| `deprecated` | 더 이상 유효하지 않은 결정 |
| `superseded` | 새로운 ADR로 대체된 결정 |

### ADR 목록

| 번호 | 제목 | 상태 |
|------|------|------|
| [ADR-0001](./0001-foundation-use-fin-over-exceptions/) | 예외 대신 Fin 타입으로 실패 표현 | accepted |
| [ADR-0002](./0002-foundation-languageext/) | LanguageExt를 함수형 기반 라이브러리로 채택 | accepted |
| [ADR-0003](./0003-domain-cqrs-read-write-separation/) | CQRS 읽기/쓰기 경로 분리 | accepted |
| [ADR-0004](./0004-domain-ulid-based-entity-id/) | Ulid 기반 Entity ID | accepted |
| [ADR-0005](./0005-domain-union-valueobject-state-machine/) | UnionValueObject 상태 머신 | accepted |
| [ADR-0006](./0006-domain-specification-expression-tree/) | Specification + Expression Tree | accepted |
| [ADR-0007](./0007-domain-aggregate-id-only-cross-references/) | Aggregate 간 ID 전용 참조 | accepted |
| [ADR-0008](./0008-domain-service-pure-vs-repository/) | Domain Service 순수 vs Repository | accepted |
| [ADR-0009](./0009-domain-value-object-class-record-duality/) | Value Object Class/Record 이중 계층 | accepted |
| [ADR-0010](./0010-domain-error-code-sealed-record-hierarchy/) | 에러 코드 sealed record 계층 | accepted |
| [ADR-0011](./0011-application-pipeline-execution-order/) | Pipeline 실행 순서 설계 | accepted |
| [ADR-0012](./0012-application-finresponse-pipeline-constraints/) | FinResponse Pipeline 타입 제약 | accepted |
| [ADR-0013](./0013-application-validation-normalize-maxlength/) | 검증 순서 Normalize → MaxLength | accepted |
| [ADR-0014](./0014-application-explicit-transaction/) | 명시적 트랜잭션 지원 | accepted |
| [ADR-0015](./0015-adapter-observable-port-source-generator/) | Observable Port Source Generator 도입 | accepted |
| [ADR-0016](./0016-adapter-suffix-naming-persistence/) | 영속성 접미사 네이밍 패턴 | accepted |
| [ADR-0017](./0017-observability-ctx-enricher-pillar-targeting/) | ctx.* Pillar 타겟팅 기본값 | accepted |
| [ADR-0018](./0018-observability-error-classification/) | 에러 3종 자동 분류 | accepted |
| [ADR-0019](./0019-observability-field-naming-snake-case-dot/) | 관측성 필드 네이밍 snake_case + dot | accepted |
| [ADR-0020](./0020-event-publisher-simplification/) | DomainEvent Publisher 단순화 | accepted |
| [ADR-0021](./0021-event-tracing-correlation/) | DomainEvent 추적 상관관계 ID | accepted |
| [ADR-0022](./0022-testing-architecture-test-suite/) | 아키텍처 테스트 Suite 프레임워크 | accepted |

### 참고

- [MADR v4.0 — Markdown Any Decision Records](https://adr.github.io/madr/)
- [Michael Nygard의 원본 ADR 제안](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
