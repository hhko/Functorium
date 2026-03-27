---
title: "AX (AI Transformation)"
description: "PRD 작성부터 테스트까지, DDD 개발 전 과정을 AI가 안내합니다"
---

AX는 Functorium 프레임워크 기반 DDD 프로젝트의 전체 개발 워크플로를 AI가 안내하는 Claude Code 플러그인입니다.

## 왜 필요한가

DDD 프로젝트를 수작업으로 진행하면 반복되는 문제에 직면합니다. 유비쿼터스 언어가 문서에만 남아 코드와 괴리가 생기고, Aggregate 경계를 결정할 때 기준이 모호해지며, 레이어별로 같은 패턴을 반복 작성하면서 일관성이 깨집니다.

AX는 이 문제를 6단계 워크플로로 해결합니다. 각 단계는 이전 단계의 출력을 입력으로 받아, 비전에서 테스트까지 끊기지 않는 흐름을 만듭니다.

### Before: 수작업 DDD

| 문제 | 증상 |
|------|------|
| 패턴 누락 | sealed 누락, private 생성자 빠뜨림, 이벤트 발행 누락 |
| 문서-코드 괴리 | 유비쿼터스 언어 테이블은 있지만 코드에 반영되지 않음 |
| 레이어 간 불일관 | Domain의 VO 검증과 Application의 Validator가 서로 다른 규칙 적용 |
| 반복 작업 | Command/Query마다 동일한 중첩 클래스 구조를 수동 작성 |

### After: AX 워크플로

| 개선 | 방법 |
|------|------|
| 패턴 일관성 | 각 스킬이 Functorium 패턴 카탈로그를 참조하여 코드 생성 |
| 문서-코드 연결 | 4단계 문서(요구사항 -> 타입 설계 -> 코드 설계 -> 구현)가 코드와 함께 생성 |
| 레이어 정합 | 이전 단계 문서를 자동으로 읽어 일관된 타입/네이밍 적용 |
| 자동화 | 자연어 요구사항에서 코드 + 테스트 + 문서를 한 번에 생성 |

## 6단계 워크플로

AX는 PRD 작성부터 테스트까지 6단계로 이어지는 개발 워크플로를 제공합니다.

```
project-spec -> architecture-design -> domain-develop -> application-develop -> adapter-develop -> test-develop
```

| 단계 | 스킬 | 역할 | 주요 산출물 |
|------|------|------|------------|
| 1 | [project-spec](./skills/project-spec/) | 요구사항 명세(PRD) | `00-project-spec.md` |
| 2 | [architecture-design](./skills/architecture-design/) | 프로젝트 구조와 인프라 설계 | `01-architecture-design.md` |
| 3 | [domain-develop](./skills/domain-develop/) | 도메인 모델 설계와 구현 | `domain/00~03` + 소스 코드 |
| 4 | [application-develop](./skills/application-develop/) | CQRS 유스케이스 구현 | `application/00~03` + 소스 코드 |
| 5 | [adapter-develop](./skills/adapter-develop/) | Repository, Endpoint, DI 구현 | `adapter/00~03` + 소스 코드 |
| 6 | [test-develop](./skills/test-develop/) | 단위/통합/아키텍처 테스트 | 테스트 코드 |

각 스킬은 이전 스킬의 출력을 입력으로 사용합니다. 어느 단계에서든 시작할 수 있으며, 선행 문서가 없으면 사용자에게 직접 질문합니다.

워크플로 전체 흐름은 [워크플로](./workflow/) 페이지에서 상세히 다룹니다.

## 핵심 구성 요소

### 7개 스킬

스킬은 정해진 워크플로를 따라 문서와 코드를 단계적으로 생성하는 자동화 도구입니다.

| 스킬 | 레이어 | 트리거 예시 |
|------|--------|------------|
| [project-spec](./skills/project-spec/) | 기획 | "PRD 작성해줘", "요구사항 정의해줘" |
| [architecture-design](./skills/architecture-design/) | 설계 | "아키텍처 설계해줘", "프로젝트 구조 잡아줘" |
| [domain-develop](./skills/domain-develop/) | 도메인 | "도메인 구현해줘", "Aggregate 만들어줘" |
| [application-develop](./skills/application-develop/) | 애플리케이션 | "유스케이스 구현해줘", "Command 만들어줘" |
| [adapter-develop](./skills/adapter-develop/) | 어댑터 | "Repository 구현해줘", "엔드포인트 만들어줘" |
| [test-develop](./skills/test-develop/) | 테스트 | "테스트 작성해줘", "통합 테스트 추가해줘" |
| [domain-review](./skills/domain-review/) | 리뷰 | "DDD 리뷰해줘", "아키텍처 리뷰해줘" |

### 4개 전문 에이전트

에이전트는 특정 레이어의 전문가로, 설계 결정에 대한 심층 대화가 필요할 때 활용합니다. 스킬이 "자동 워크플로"라면, 에이전트는 "전문가 상담"입니다.

| 에이전트 | 전문 영역 |
|---------|-----------|
| domain-architect | 유비쿼터스 언어, Aggregate 경계, 타입 전략 |
| application-architect | CQRS 설계, 포트 식별, FinT 합성, CtxEnricher 3-Pillar 설계 |
| adapter-engineer | Repository, Endpoint, DI 등록, CtxEnricherPipeline 통합 |
| test-engineer | 단위/통합/아키텍처 테스트, ctx 3-Pillar 스냅샷 테스트 |

에이전트의 역할과 활용 예시는 [전문 에이전트](./agents/) 페이지에서 상세히 다룹니다.

## 시작하기

### 1. 설치

[설치 가이드](./installation/)를 따라 functorium-develop 플러그인을 활성화합니다.

### 2. 워크플로 이해

[워크플로](./workflow/) 페이지에서 6단계 흐름과 각 단계의 입출력 관계를 파악합니다.

### 3. 첫 스킬 실행

프로젝트 시작이라면 PRD부터:

```text
PRD 작성해줘. AI 모델 거버넌스 플랫폼을 만들고 싶어.
```

이미 요구사항이 있다면 도메인 개발부터:

```text
도메인 구현해줘. 상품(Product) Aggregate를 설계하고 싶어.
```

기존 코드가 있다면 리뷰부터:

```text
현재 도메인 코드를 DDD 관점에서 리뷰해줘.
```

각 스킬은 대화형으로 동작합니다. 필요한 정보가 부족하면 질문을 통해 수집하므로, 세부 내용을 미리 준비하지 않아도 됩니다.
