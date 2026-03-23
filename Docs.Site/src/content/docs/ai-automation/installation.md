---
title: "설치 및 설정"
---

## 설치 방법

functorium-develop 플러그인은 프로젝트 로컬 플러그인으로 제공됩니다.

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

플러그인이 정상 로드되면 다음 스킬을 호출할 수 있습니다:

```text
도메인 구현해줘
```

`domain-develop` 스킬이 자동으로 트리거됩니다.

## 플러그인 구조

```
.claude/plugins/functorium-develop/
├── .claude-plugin/plugin.json      # 매니페스트
├── skills/                         # 5개 스킬
│   ├── domain-develop/
│   ├── application-develop/
│   ├── adapter-develop/
│   ├── test-develop/
│   └── domain-review/
├── agents/                         # 4개 전문 에이전트
│   ├── domain-architect.md
│   ├── application-architect.md
│   ├── adapter-engineer.md
│   └── test-engineer.md
└── README.md
```

### 스킬 구조

각 스킬은 `SKILL.md`와 `references/` 폴더로 구성됩니다:

```
skills/domain-develop/
├── SKILL.md                        # 워크플로우 정의 (500줄 이내)
└── references/
    ├── pattern-catalog.md          # DDD 패턴 카탈로그
    └── type-strategy.md            # 타입 매핑 전략 가이드
```

- `SKILL.md`: 스킬이 트리거되면 컨텍스트에 로드
- `references/`: 필요할 때만 읽어서 컨텍스트 절약 (Progressive Disclosure)
