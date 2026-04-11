---
title: "Introduction to Claude Code"
---

릴리스 노트를 작성할 때마다 같은 과정을 반복하게 됩니다. Git 로그를 확인하고, 커밋을 분류하고, Breaking Changes를 찾아내고, 마크다운 문서로 정리합니다. 이 작업 자체는 어렵지 않지만, 매번 수동으로 하면 시간이 걸리고 빠뜨리는 항목이 생기기 마련입니다.

**Claude Code는** Anthropic에서 개발한 AI 기반 CLI 도구로, 터미널에서 직접 Claude AI와 상호작용할 수 있게 해줍니다. 코드를 읽고, 파일을 편집하고, Git이나 dotnet 같은 명령어를 실행할 수 있습니다. 그런데 이 도구의 진짜 힘은 **사용자 정의 Command에** 있습니다. 복잡한 작업 지침을 Markdown 파일로 미리 정의해두면, 간단한 명령어 하나로 전체 워크플로우를 실행할 수 있습니다. 마치 레시피 북을 펼치고 따라하듯이 말입니다.

매번 "git log 확인해줘", "커밋 분류해줘", "문서 작성해줘"라고 반복하는 대신, `/release-note v1.2.0` 한 줄이면 됩니다. 레시피가 있으니 항상 같은 품질의 결과를 얻을 수 있고, 그 레시피를 Git에 커밋하면 팀 전체가 동일한 워크플로우를 공유하게 됩니다.

## Claude Code 설치

### npm을 통한 설치

```bash
npm install -g @anthropic-ai/claude-code
```

### 설치 확인

```bash
claude --version
```

### 초기 설정

처음 실행 시 Anthropic API 키를 설정해야 합니다:

```bash
claude
# 프롬프트에 따라 API 키 입력
```

## 기본 사용법

### 대화형 모드

터미널에서 `claude`를 입력하면 대화형 모드로 진입합니다:

```bash
claude
> 이 프로젝트의 구조를 설명해줘
```

### 단일 질문

```bash
claude "package.json의 의존성을 확인해줘"
```

### 파일 참조

특정 파일을 참조하며 질문할 수 있습니다:

```bash
claude @src/main.cs "이 파일의 역할을 설명해줘"
```

Claude Code는 요청에 따라 자동으로 적절한 내장 도구를 선택합니다. 파일을 찾으라고 하면 Glob을, 코드 내 패턴을 검색하라고 하면 Grep을, git이나 dotnet 명령이 필요하면 Bash를 사용합니다. 아래 표는 주요 내장 도구입니다:

| 도구 | 용도 | 예시 |
|------|------|------|
| Read | 파일 읽기 | 소스 코드 확인 |
| Write | 파일 쓰기 | 새 파일 생성 |
| Edit | 파일 편집 | 코드 수정 |
| Bash | 명령어 실행 | `git`, `dotnet` 등 |
| Glob | 파일 검색 | 패턴으로 파일 찾기 |
| Grep | 내용 검색 | 코드 내 패턴 찾기 |
| Task | 하위 작업 | 복잡한 작업 분할 |

## 사용자 정의 Command

이 튜토리얼의 핵심인 사용자 정의 Command를 살펴보겠습니다. Command 파일은 프로젝트 루트의 `.claude/commands/` 폴더에 Markdown 파일로 저장하며, 파일명이 곧 명령어 이름이 됩니다.

```txt
프로젝트 루트/
├── .claude/
│   └── commands/
│       ├── release-note.md    # /release-note 명령어
│       ├── commit.md          # /commit 명령어
│       └── my-command.md      # /my-command 명령어
└── ...
```

### Command 파일 구조

Command 파일은 YAML 프론트매터와 Markdown 본문으로 구성됩니다. 프론트매터에는 Command의 메타데이터를, 본문에는 Claude에게 전달할 작업 지침을 작성합니다.

```markdown
---
title: MY-COMMAND
description: 이 명령어에 대한 설명
argument-hint: "<arg> 인자 설명"
---

# 명령어 이름

명령어 실행 시 Claude에게 전달되는 프롬프트 내용입니다.

## 작업 지침

1. 첫 번째 단계
2. 두 번째 단계
3. 세 번째 단계
```

### Command 실행

대화형 모드에서 슬래시(/)로 시작하는 명령어를 입력합니다:

```bash
claude
> /release-note v1.2.0
> /commit
> /my-command argument
```

### $ARGUMENTS 변수

Command에 전달된 인자는 `$ARGUMENTS` 변수로 접근할 수 있습니다:

```markdown
---
title: GREET
description: 인사 메시지 출력
argument-hint: "<name> 이름"
---

# 인사하기

"$ARGUMENTS"님에게 친근하게 인사해주세요.
```

**실행:**
```bash
> /greet 홍길동
# 결과: "홍길동님, 안녕하세요!" 형태의 응답
```

## 릴리스 노트 Command 예시

이 튜토리얼에서 다루는 `release-note` Command의 핵심 구조를 미리 살펴보겠습니다. 전체 워크플로우는 5단계로 나뉘며, 환경 검증부터 최종 검증까지 순차적으로 실행됩니다.

```markdown
---
title: RELEASE-NOTES
description: 릴리스 노트를 자동으로 생성합니다
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"
---

# 릴리스 노트 자동 생성 규칙

## 버전 파라미터 ($ARGUMENTS)

**버전이 지정된 경우:** $ARGUMENTS

## 자동화 워크플로우

| Phase | 목표 |
|-------|------|
| 1 | 환경 검증 |
| 2 | 데이터 수집 |
| 3 | 커밋 분석 |
| 4 | 문서 작성 |
| 5 | 검증 |

## Phase 1: 환경 검증

**전제조건 확인**:
```bash
git status
dotnet --version
```
...
```

**실행:**
```bash
> /release-note v1.2.0
```

Claude는 이 프롬프트를 읽고 5단계 워크플로우를 순차적으로 실행합니다.

## Command가 가져오는 변화

레시피 비유를 다시 떠올려봅시다. 매번 재료 준비 방법과 조리 순서를 기억에서 꺼내는 것과, 레시피를 펼쳐놓고 따라하는 것 사이에는 큰 차이가 있습니다. Command도 마찬가지입니다.

**반복 작업이 자동화됩니다.** 릴리스 노트 작성에 필요한 모든 과정(Git 로그 확인, 변경사항 분류, 문서 작성, 검증)을 `/release-note v1.2.0` 한 줄로 실행할 수 있습니다. 매번 같은 지시를 반복할 필요가 없습니다.

**품질이 일관됩니다.** Command 파일에 모든 규칙과 기준을 명시해두면, 누가 실행하든 같은 수준의 결과물을 얻습니다. "Why this matters" 섹션이 빠지거나, Breaking Changes를 놓치는 일이 줄어듭니다.

**팀 전체가 같은 워크플로우를 공유합니다.** Command 파일을 Git에 커밋하면 신규 팀원도 즉시 같은 자동화를 사용할 수 있습니다.

```bash
git add .claude/commands/release-note.md
git commit -m "feat(claude): 릴리스 노트 자동화 Command 추가"
```

**점진적으로 개선할 수 있습니다.** 문제가 발견되면 Command 파일을 수정하면 됩니다. 검증 항목을 추가하거나, 새로운 규칙을 반영하는 것이 코드 한 줄 수정하는 것만큼 쉽습니다.

## Claude Code 설정

### .claude/settings.json

프로젝트별 Claude Code 설정:

```json
{
  "permissions": {
    "allow": [
      "Bash(git:*)",
      "Bash(dotnet:*)",
      "Read",
      "Write",
      "Edit"
    ]
  }
}
```

### CLAUDE.md

프로젝트 루트에 `CLAUDE.md` 파일을 만들어 Claude에게 컨텍스트를 제공합니다:

```markdown
# 프로젝트 가이드

## 커밋 규칙
커밋 시 `.claude/commands/commit.md`의 규칙을 준수하십시오.

## 코드 스타일
- C# 10 문법 사용
- nullable reference types 활성화
```

## FAQ

### Q1: Claude Code의 사용자 정의 Command와 일반 대화의 차이는 무엇인가요?
**A**: 일반 대화에서는 매번 같은 지시를 반복 입력해야 하지만, **사용자 정의 Command는** 복잡한 작업 지침을 Markdown 파일로 저장하여 `/release-note v1.2.0`처럼 한 줄로 실행합니다. Command 파일에 규칙과 검증 기준이 명시되어 있어 누가 실행하든 일관된 품질의 결과물을 얻을 수 있습니다.

### Q2: `$ARGUMENTS` 변수는 여러 개의 인자를 받을 수 있나요?
**A**: `$ARGUMENTS`는 Command에 전달된 모든 인자를 하나의 문자열로 받습니다. `/release-note v1.2.0`을 실행하면 `$ARGUMENTS`는 `v1.2.0`이 됩니다. 여러 값을 전달하면 공백으로 구분된 하나의 문자열이 되므로, Command 본문에서 파싱 방법을 안내해야 합니다.

### Q3: Command 파일을 수정하면 즉시 반영되나요?
**A**: 반영됩니다. Command 파일은 실행 시점에 읽히므로, `.claude/commands/release-note.md`를 수정하면 다음 `/release-note` 실행부터 변경사항이 적용됩니다. Git에 커밋하면 팀 전체가 동일한 업데이트를 공유하게 됩니다.

이제 Claude Code가 무엇이고 어떻게 동작하는지 파악했으니, 다음으로 자동화의 데이터 소스인 Git의 기초를 살펴보겠습니다.
