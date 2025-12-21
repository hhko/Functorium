# 2.2 Claude Code 소개

> Claude Code는 Anthropic에서 개발한 AI 기반 CLI(Command Line Interface) 도구입니다. 이 절에서는 Claude Code의 기본 개념과 사용자 정의 Command 기능을 소개합니다.

---

## Claude Code란?

Claude Code는 터미널에서 직접 Claude AI와 상호작용할 수 있는 도구입니다. 코드 작성, 파일 편집, 명령어 실행 등 다양한 개발 작업을 AI의 도움을 받아 수행할 수 있습니다.

### 주요 특징

```txt
Claude Code의 핵심 기능:

├── 코드 이해 및 생성
│   ├── 코드베이스 탐색
│   ├── 버그 수정
│   └── 새로운 기능 구현
│
├── 파일 작업
│   ├── 파일 읽기/쓰기
│   ├── 파일 검색 (Glob, Grep)
│   └── 파일 편집
│
├── 명령어 실행
│   ├── 터미널 명령 실행
│   ├── Git 작업
│   └── 빌드/테스트 실행
│
└── 사용자 정의 Command
    ├── 반복 작업 자동화
    ├── 복잡한 워크플로우 정의
    └── 팀 간 지식 공유
```

---

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

---

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

---

## 사용자 정의 Command

Claude Code의 가장 강력한 기능 중 하나는 **사용자 정의 Command**입니다. 복잡한 작업을 미리 정의해두고 간단한 명령어로 실행할 수 있습니다.

### Command 파일 위치

```txt
프로젝트 구조:
├── .claude/
│   └── commands/
│       ├── release-note.md    # /release-note 명령어
│       ├── commit.md          # /commit 명령어
│       └── my-command.md      # /my-command 명령어
└── ...
```

### Command 파일 구조

Command 파일은 YAML 프론트매터와 Markdown 본문으로 구성됩니다:

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

---

## 릴리스 노트 Command 예시

이 책에서 다루는 `release-note` Command의 핵심 구조:

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

---

## Command의 장점

### 1. 반복 작업 자동화

```txt
수동 작업:
├── Git 로그 확인
├── 변경사항 분류
├── 문서 작성
├── 검증
└── 매번 같은 과정 반복

Command 사용:
└── /release-note v1.2.0
    └── 모든 과정 자동 실행
```

### 2. 지식 공유

Command 파일을 Git에 커밋하면 팀 전체가 동일한 워크플로우를 공유합니다:

```bash
git add .claude/commands/release-note.md
git commit -m "feat(claude): 릴리스 노트 자동화 Command 추가"
```

### 3. 품질 일관성

모든 팀원이 동일한 프롬프트를 사용하므로 결과물의 품질이 일관됩니다.

### 4. 점진적 개선

Command 파일을 수정하여 워크플로우를 지속적으로 개선할 수 있습니다.

---

## 주요 내장 도구

Claude Code는 다양한 내장 도구를 제공합니다:

| 도구 | 용도 | 예시 |
|------|------|------|
| Read | 파일 읽기 | 소스 코드 확인 |
| Write | 파일 쓰기 | 새 파일 생성 |
| Edit | 파일 편집 | 코드 수정 |
| Bash | 명령어 실행 | `git`, `dotnet` 등 |
| Glob | 파일 검색 | 패턴으로 파일 찾기 |
| Grep | 내용 검색 | 코드 내 패턴 찾기 |
| Task | 하위 작업 | 복잡한 작업 분할 |

### 도구 사용 예시

Claude에게 요청하면 자동으로 적절한 도구를 선택합니다:

```bash
> 프로젝트의 모든 .cs 파일을 찾아줘
# Claude: Glob 도구 사용

> ErrorCodeFactory 클래스가 어디에 있는지 찾아줘
# Claude: Grep 도구 사용

> git log를 확인해줘
# Claude: Bash 도구 사용
```

---

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

---

## 정리

| 개념 | 설명 |
|------|------|
| Claude Code | AI 기반 CLI 개발 도구 |
| 사용자 정의 Command | `.claude/commands/*.md` 파일 |
| 프론트매터 | title, description, argument-hint |
| $ARGUMENTS | Command 인자 접근 변수 |
| 실행 방법 | `/command-name [args]` |

---

## 다음 단계

- [2.3 Git 기초](03-git-basics.md)
