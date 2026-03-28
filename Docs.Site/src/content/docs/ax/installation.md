---
title: "설치 및 설정"
description: "functorium-develop 플러그인 설치와 구조 안내"
---

functorium-develop 플러그인은 프로젝트 로컬 플러그인으로 제공됩니다.

## 설치 방법

### 방법 1: settings.local.json (권장)

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

다음 세션부터 자동으로 로드됩니다.

### 방법 2: CLI 플래그 (일시적)

```bash
claude --plugin-dir .claude/plugins/functorium-develop
```

현재 세션에서만 유효합니다.

### 확인

플러그인이 정상 로드되면 다음과 같이 스킬을 호출할 수 있습니다:

```text
도메인 구현해줘
```

`domain-develop` 스킬이 자동으로 트리거됩니다.

## 플러그인 구조

```
.claude/plugins/functorium-develop/
├── .claude-plugin/plugin.json      # 매니페스트
├── skills/                         # 8개 스킬
│   ├── project-spec/               # PRD 작성
│   ├── architecture-design/        # 아키텍처 설계
│   ├── domain-develop/             # 도메인 개발
│   ├── application-develop/        # 애플리케이션 개발
│   ├── adapter-develop/            # 어댑터 개발
│   ├── observability-develop/      # 관측성 전략
│   ├── test-develop/               # 테스트 개발
│   └── domain-review/              # DDD 코드 리뷰
├── agents/                         # 6개 전문 에이전트
│   ├── product-analyst.md
│   ├── domain-architect.md
│   ├── application-architect.md
│   ├── adapter-engineer.md
│   ├── observability-engineer.md
│   └── test-engineer.md
└── README.md
```

### 스킬 구조

각 스킬은 `SKILL.md`와 `references/` 폴더로 구성됩니다:

```
skills/domain-develop/
├── SKILL.md                        # 워크플로 정의 (500줄 이내)
└── references/
    ├── pattern-catalog.md          # DDD 패턴 카탈로그
    └── type-strategy.md            # 타입 매핑 전략 가이드
```

- **SKILL.md**: 스킬이 트리거되면 컨텍스트에 로드됩니다. 워크플로 단계, 질문 목록, 출력 문서 형식을 정의합니다.
- **references/**: 필요할 때만 읽어서 컨텍스트를 절약합니다(Progressive Disclosure). 패턴 카탈로그, 코드 템플릿, 체크리스트 등 상세 참조 자료를 포함합니다.

### 에이전트 구조

각 에이전트는 하나의 `.md` 파일로 정의됩니다. 에이전트 파일에는 전문 영역, 작업 방식, 핵심 원칙이 기술되어 있어, 대화에서 해당 전문성이 필요할 때 활성화됩니다.

## 다음 단계

- [워크플로](./workflow/) -- 7단계 개발 워크플로 이해
- [Project Spec 스킬](./skills/project-spec/) -- 첫 번째 단계: PRD 작성
- [전문 에이전트](./agents/) -- 설계 결정에 전문가 활용
