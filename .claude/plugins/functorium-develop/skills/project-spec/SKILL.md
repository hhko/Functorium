---
name: project-spec
description: "Functorium 프레임워크 기반 프로젝트의 요구사항 명세(PRD)를 작성합니다. 프로젝트 비전, 유비쿼터스 언어, Aggregate 후보, 비즈니스 규칙, 유스케이스를 정의하고 레이어별 개발의 입력 문서를 생성합니다. 'PRD 작성', '요구사항 정의', '프로젝트 기획', '스펙 작성', '비즈니스 요구사항', '프로젝트 시작' 등의 요청에 반응합니다."
---

## 개요

Functorium 프레임워크 기반 프로젝트의 요구사항 명세를 작성합니다.
사용자와 대화를 통해 비전에서 시작하여 구현 가능한 스펙 문서를 생성합니다.
이 문서는 `domain-develop`, `application-develop`, `adapter-develop` 스킬의 입력이 됩니다.

## 후속 스킬

이 스킬의 출력은 다음 스킬의 입력으로 연결됩니다:

```
project-spec → architecture-design → domain-develop → application-develop → adapter-develop → test-develop
```

## 워크플로우

### Phase 1: 비전 수집

사용자에게 다음을 질문합니다:

**프로젝트 기본 정보:**
- 프로젝트 이름은?
- 한 줄로 설명하면?
- 해결하려는 비즈니스 문제는?

**사용자 정보:**
- 대상 사용자(페르소나)는 누구인가?
- 사용자의 핵심 목표는?

**비즈니스 정보:**
- 핵심 성공 지표(KPI)는?
- 기존 시스템과의 연동 제약은?
- 기술 제약 조건은? (.NET 10, 단일/마이크로서비스 등)

### Phase 2: 도메인 분석

비전에서 도메인 모델 후보를 추출합니다:

**유비쿼터스 언어 추출:**
- 비즈니스 설명에서 핵심 명사 → 엔티티/VO 후보
- 핵심 동사 → 유스케이스(Command/Query) 후보
- 상태 변화 → 도메인 이벤트 후보

**비즈니스 규칙 분류:**

| 규칙 유형 | 설명 | 예시 |
|-----------|------|------|
| 불변식 | 항상 참이어야 하는 조건 | "가격은 0보다 커야 한다" |
| 상태 전이 | 허용된 전이만 가능 | "주문은 Pending → Confirmed만 가능" |
| 교차 규칙 | 여러 Aggregate 참조 | "주문 총액이 고객 신용 한도 초과 불가" |
| 금지 상태 | 구조적으로 불가능해야 하는 상태 | "연락처 없는 고객" |

### Phase 3: Aggregate 경계 도출

Evans의 기준에 따라 Aggregate 후보를 식별합니다:

**Aggregate 경계 결정 기준:**
1. 트랜잭션 일관성 — 같은 트랜잭션에서 변경되는 데이터는 같은 Aggregate
2. 불변식 범위 — 불변식이 보장해야 하는 데이터 범위
3. 독립적 생명주기 — 다른 Aggregate 없이 독립적으로 생성/삭제 가능
4. Aggregate 간 참조 — ID로만 참조 (직접 객체 참조 금지)

**Aggregate 간 조율:**
- 동기 조율 → Domain Service (같은 트랜잭션 내)
- 비동기 조율 → Domain Event + Event Handler (최종 일관성)

### Phase 4: 문서 생성

**출력:** `{context}/00-project-spec.md`

문서 구조는 `references/spec-template.md`를 읽어 참고합니다.

출력 문서에 포함할 내용:
- 프로젝트 개요 (이름, 배경, 목표, 대상 사용자)
- 유비쿼터스 언어 테이블 (한글 | 영문 | 정의)
- Aggregate 후보 목록 (이름 | 핵심 책임 | 상태 전이 | 주요 이벤트)
- 비즈니스 규칙 카탈로그 (Aggregate별 분류)
- 유스케이스 개요 (Command | Query | EventHandler 후보)
- 금지 상태 목록
- MVP 범위 결정
- 다음 단계 가이드

### Phase 4 출력 후 안내

문서 생성 후 사용자에게 다음 단계를 안내합니다:

> 프로젝트 스펙이 완성되었습니다.
>
> **다음 단계:**
> 1. `architecture-design` 스킬로 프로젝트 구조와 인프라를 설계하세요
> 2. `domain-develop` 스킬로 각 Aggregate를 상세 설계하고 구현하세요
> 3. `application-develop` 스킬로 유스케이스를 구현하세요
> 4. `adapter-develop` 스킬로 영속성과 API를 구현하세요
> 5. `test-develop` 스킬로 테스트를 작성하세요

## 핵심 원칙

- 비즈니스 언어로 시작, 기술 언어로 끝내지 않음
- Aggregate 경계는 트랜잭션 일관성 기준으로 결정
- Aggregate 간 참조는 ID만 (Evans Blue Book Ch.6)
- 금지 상태는 가능하면 타입 시스템으로 구조적 제거
- MVP 범위를 명확히 하여 첫 구현 스코프를 제한

## References

- PRD 템플릿: `references/spec-template.md`를 읽으세요
