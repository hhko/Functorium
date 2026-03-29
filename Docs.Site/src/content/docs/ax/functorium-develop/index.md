---
title: "functorium-develop"
description: "PRD 작성부터 테스트까지, DDD 개발 전 과정을 AI가 안내하는 Claude Code 플러그인"
---

functorium-develop는 Functorium 프레임워크 기반 DDD 프로젝트의 7단계 개발 워크플로를 AI가 안내하는 Claude Code 플러그인입니다.

## 설치

### settings.local.json (권장)

`.claude/settings.local.json`에 다음을 추가합니다:

```json
{
  "extraKnownMarketplaces": {
    "functorium-develop": {
      "source": {
        "source": "directory",
        "path": "./.claude/plugins/functorium-develop"
      }
    }
  },
  "enabledPlugins": {
    "functorium-develop": true
  }
}
```

### CLI 플래그 (일시적)

```bash
claude --plugin-dir .claude/plugins/functorium-develop
```

## 7단계 워크플로

```
project-spec → architecture-design → domain-develop → application-develop → adapter-develop → observability-develop → test-develop
```

| 단계 | 스킬 | 역할 | 주요 산출물 |
|------|------|------|------------|
| 1 | [project-spec](./skills/project-spec/) | 요구사항 명세(PRD) | `00-project-spec.md` |
| 2 | [architecture-design](./skills/architecture-design/) | 프로젝트 구조와 인프라 설계 | `01-architecture-design.md` |
| 3 | [domain-develop](./skills/domain-develop/) | 도메인 모델 설계와 구현 | `domain/00~03` + 소스 코드 |
| 4 | [application-develop](./skills/application-develop/) | CQRS 유스케이스 구현 | `application/00~03` + 소스 코드 |
| 5 | [adapter-develop](./skills/adapter-develop/) | Repository, Endpoint, DI 구현 | `adapter/00~03` + 소스 코드 |
| 6 | [observability-develop](./skills/observability-develop/) | 관측성 전략 (KPI 매핑, 대시보드, 알림) | 관측성 전략 문서 |
| 7 | [test-develop](./skills/test-develop/) | 단위/통합/아키텍처 테스트 | 테스트 코드 |

별도로 [domain-review](./skills/domain-review/) 스킬은 어느 시점에서든 기존 코드를 DDD 관점에서 리뷰합니다.

각 단계의 상세 흐름과 단계 간 연결은 [워크플로](./workflow/) 페이지에서 다룹니다.

## 8개 스킬

| 스킬 | 레이어 | 트리거 예시 |
|------|--------|------------|
| [project-spec](./skills/project-spec/) | 기획 | "PRD 작성해줘", "요구사항 정의해줘" |
| [architecture-design](./skills/architecture-design/) | 설계 | "아키텍처 설계해줘", "프로젝트 구조 잡아줘" |
| [domain-develop](./skills/domain-develop/) | 도메인 | "도메인 구현해줘", "Aggregate 만들어줘" |
| [application-develop](./skills/application-develop/) | 애플리케이션 | "유스케이스 구현해줘", "Command 만들어줘" |
| [adapter-develop](./skills/adapter-develop/) | 어댑터 | "Repository 구현해줘", "엔드포인트 만들어줘" |
| [observability-develop](./skills/observability-develop/) | 관측성 | "관측성 설계해줘", "대시보드 설계해줘" |
| [test-develop](./skills/test-develop/) | 테스트 | "테스트 작성해줘", "통합 테스트 추가해줘" |
| [domain-review](./skills/domain-review/) | 리뷰 | "DDD 리뷰해줘", "아키텍처 리뷰해줘" |

## 6개 전문 에이전트

에이전트는 특정 레이어의 전문가로, 설계 결정에 대한 심층 대화가 필요할 때 활용합니다. 스킬이 "자동 워크플로"라면, 에이전트는 "전문가 상담"입니다.

| 에이전트 | 전문 영역 |
|---------|-----------|
| product-analyst | PRD 작성, 요구사항 분석, 사용자 스토리, Aggregate 경계 도출 |
| domain-architect | 유비쿼터스 언어, Aggregate 경계, 타입 전략 |
| application-architect | CQRS 설계, 포트 식별, FinT 합성, CtxEnricher 3-Pillar 설계 |
| adapter-engineer | Repository, Endpoint, DI 등록, CtxEnricherPipeline 통합 |
| observability-engineer | KPI→메트릭 매핑, 대시보드, 알림, ctx.* 전파, 분산 추적 |
| test-engineer | 단위/통합/아키텍처 테스트, ctx 3-Pillar 스냅샷 테스트 |

에이전트의 상세 역할과 활용 예시는 [전문 에이전트](./agents/) 페이지에서 다룹니다.

## 플러그인 구조

```
.claude/plugins/functorium-develop/
├── .claude-plugin/plugin.json      # 매니페스트 (v0.4.0)
├── skills/                         # 8개 스킬
│   ├── project-spec/
│   ├── architecture-design/
│   ├── domain-develop/
│   ├── application-develop/
│   ├── adapter-develop/
│   ├── observability-develop/
│   ├── test-develop/
│   └── domain-review/
└── agents/                         # 6개 전문 에이전트
    ├── product-analyst.md
    ├── domain-architect.md
    ├── application-architect.md
    ├── adapter-engineer.md
    ├── observability-engineer.md
    └── test-engineer.md
```

## 시작하기

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
