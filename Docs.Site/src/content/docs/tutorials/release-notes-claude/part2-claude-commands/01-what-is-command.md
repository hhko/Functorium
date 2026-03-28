---
title: "사용자 정의 Command"
---

개발 작업에는 반복되는 패턴이 있습니다. 릴리스 노트를 작성할 때마다 Git 로그를 확인하고, 커밋을 분류하고, 문서를 작성하고, 검증하는 과정을 거칩니다. Claude에게 이 작업을 맡기더라도, 매번 같은 지시를 처음부터 타이핑해야 한다면 여전히 비효율적입니다.

요리에 비유하면, 레시피 없이 매번 재료 준비 방법과 조리 순서를 기억에서 꺼내는 것과 같습니다. 레시피 북을 펼쳐놓으면 누구든 같은 순서로 같은 결과를 만들어낼 수 있듯이, Claude Code의 **사용자 정의 Command는** 복잡한 작업 지침을 Markdown 파일로 저장해두고 간단한 명령어로 실행할 수 있게 해줍니다. 개발 작업의 "레시피"인 셈입니다.

## Command 파일 위치

Command 파일은 프로젝트 루트의 `.claude/commands/` 폴더에 저장합니다.

```txt
프로젝트 루트/
├── .claude/
│   ├── commands/
│   │   ├── release-note.md    # /release-note 명령어
│   │   ├── commit.md          # /commit 명령어
│   │   ├── review.md          # /review 명령어
│   │   └── test.md            # /test 명령어
│   └── settings.json          # Claude Code 설정
├── src/
└── ...
```

파일명이 곧 명령어 이름이 되며, 확장자는 `.md`, 공백 대신 하이픈(`-`)을 사용합니다.

| 파일명 | 실행 명령어 |
|--------|------------|
| `release-note.md` | `/release-note` |
| `commit.md` | `/commit` |
| `code-review.md` | `/code-review` |

## 왜 Command를 사용하는가?

**반복 작업이 한 줄로 줄어듭니다.** 릴리스 노트를 수동으로 작성하려면 "Git 로그 확인해줘", "커밋을 분류해줘", "새로운 기능을 문서화해줘", "Breaking Changes를 확인해줘", "마크다운으로 정리해줘"라고 매번 같은 지시를 반복해야 합니다. Command를 사용하면 `/release-note v1.2.0` 한 줄로 이 모든 과정이 자동으로 실행됩니다.

**품질이 일관됩니다.** Command 파일에 모든 규칙과 기준을 명시해두면, 매번 같은 품질의 결과물을 얻습니다.

```markdown
## 작성 규칙

1. 모든 기능에 "Why this matters" 섹션 필수
2. 코드 샘플은 실행 가능해야 함
3. API는 Uber 파일에서 검증해야 함
```

**지식이 팀 전체에 공유됩니다.** Command 파일을 Git에 커밋하면 팀 전체가 같은 워크플로우를 사용하고, 신규 팀원도 바로 작업할 수 있으며, 워크플로우 개선 이력도 추적됩니다.

```bash
git add .claude/commands/release-note.md
git commit -m "feat(claude): 릴리스 노트 자동화 Command 추가"
git push
```

**점진적으로 개선할 수 있습니다.** 문제가 발견되면 Command 파일을 수정하면 됩니다. 예를 들어 검증 항목에 "Why this matters" 확인과 API 검증을 추가하는 것은 Markdown 몇 줄을 수정하는 것만큼 간단합니다.

## Command 파일 기본 구조

Command 파일은 세 부분으로 구성됩니다. YAML 프론트매터, 제목, 그리고 본문입니다.

```markdown
---
title: COMMAND-NAME
description: 명령어 설명
argument-hint: "<arg> 인자 설명"
---

# 명령어 제목

## 섹션 1

작업 지침...

## 섹션 2

더 많은 지침...
```

### 1. YAML 프론트매터

파일 최상단에 `---`로 감싼 메타데이터입니다.

```yaml
---
title: RELEASE-NOTES
description: 릴리스 노트를 자동으로 생성합니다
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"
---
```

| 필드 | 필수 | 설명 |
|------|:----:|------|
| `title` | O | Command 이름 (표시용) |
| `description` | O | Command 설명 |
| `argument-hint` | X | 인자 힌트 메시지 |

### 2. 제목

프론트매터 다음에 Markdown 제목을 작성합니다:

```markdown
# 릴리스 노트 자동 생성 규칙
```

### 3. 본문

Claude에게 전달되는 상세 지침입니다. 워크플로우 단계, 따라야 할 규칙, 출력 형식 등을 자유롭게 Markdown으로 작성합니다.

## $ARGUMENTS 변수

Command 실행 시 전달된 인자는 `$ARGUMENTS` 변수로 접근합니다. Claude가 Command를 실행할 때, `$ARGUMENTS`가 실제 인자 값으로 치환됩니다.

### 예시 1: 버전 파라미터

**Command 파일 (release-note.md):**
```markdown
---
title: RELEASE-NOTES
argument-hint: "<version> 릴리스 버전"
---

# 릴리스 노트 생성

**버전:** $ARGUMENTS

버전 $ARGUMENTS의 릴리스 노트를 생성하세요.
출력 파일: RELEASE-$ARGUMENTS.md
```

**실행:**
```bash
/release-note v1.2.0
```

**Claude가 받는 프롬프트:**
```markdown
# 릴리스 노트 생성

**버전:** v1.2.0

버전 v1.2.0의 릴리스 노트를 생성하세요.
출력 파일: RELEASE-v1.2.0.md
```

### 예시 2: 파일 경로

**Command 파일 (analyze.md):**
```markdown
---
title: ANALYZE
argument-hint: "<path> 분석할 폴더 경로"
---

$ARGUMENTS 폴더를 분석하세요.
```

**실행:**
```bash
/analyze Src/Functorium
```

### 예시 3: 인자 없음

인자가 전달되지 않으면 `$ARGUMENTS`는 빈 문자열이 됩니다. Command에서 인자 유무를 확인하는 로직을 포함할 수 있습니다:

```markdown
## 버전 파라미터 ($ARGUMENTS)

**버전이 지정된 경우:** $ARGUMENTS

버전 파라미터는 필수입니다. 지정되지 않은 경우 오류 메시지를 출력하고 중단합니다.
```

## Command 실행 방법

### 대화형 모드에서 실행

```bash
claude
> /release-note v1.2.0
> /commit
> /review src/main.cs
```

Command는 반드시 슬래시(`/`)로 시작해야 합니다. 슬래시 없이 입력하면 일반 질문으로 처리됩니다.

### 자동 완성

대화형 모드에서 `/`를 입력하면 사용 가능한 Command 목록이 표시됩니다:

```bash
> /
Available commands:
  /release-note - 릴리스 노트를 자동으로 생성합니다
  /commit       - Conventional Commits 규격에 따라 커밋합니다
  /review       - 코드 리뷰를 수행합니다
```

## 간단한 Command 작성 예시

Command의 구조를 파악했으니, 간단한 예시 세 가지를 통해 감을 잡아봅시다.

### 예시 1: 인사 Command

**파일:** `.claude/commands/greet.md`
```markdown
---
title: GREET
description: 사용자에게 인사합니다
argument-hint: "<name> 이름"
---

# 인사하기

"$ARGUMENTS"님에게 친근하고 재미있게 인사해주세요.
한국어로 작성하세요.
```

**실행:**
```bash
> /greet 홍길동
```

### 예시 2: 코드 설명 Command

**파일:** `.claude/commands/explain.md`
```markdown
---
title: EXPLAIN
description: 코드를 설명합니다
argument-hint: "<file> 설명할 파일"
---

# 코드 설명

$ARGUMENTS 파일의 코드를 설명해주세요.

## 설명 형식

1. **파일 개요**: 이 파일의 목적
2. **주요 클래스/함수**: 각각의 역할
3. **흐름**: 코드 실행 흐름
4. **의존성**: 외부 의존성
```

**실행:**
```bash
> /explain src/main.cs
```

### 예시 3: 테스트 생성 Command

**파일:** `.claude/commands/test.md`
```markdown
---
title: TEST
description: 단위 테스트를 생성합니다
argument-hint: "<class> 테스트할 클래스"
---

# 테스트 생성

$ARGUMENTS 클래스에 대한 단위 테스트를 생성해주세요.

## 규칙

- xUnit 사용
- AAA 패턴 (Arrange-Act-Assert) 적용
- 모든 public 메서드 테스트
- 엣지 케이스 포함
```

**실행:**
```bash
> /test UserService
```

## FAQ

### Q1: Command 파일명에 규칙이 있나요?
**A**: 파일명이 곧 명령어 이름이 됩니다. 확장자는 `.md`를 사용하고, 공백 대신 하이픈(`-`)을 씁니다. 예를 들어 `release-note.md`는 `/release-note`로, `code-review.md`는 `/code-review`로 실행됩니다. 대소문자는 구분하지 않습니다.

### Q2: 하나의 Command에서 여러 파일의 지침을 참조할 수 있나요?
**A**: 가능합니다. Command 본문에서 다른 Markdown 파일을 링크로 참조하면 Claude가 필요에 따라 해당 문서를 읽고 지침을 따릅니다. `release-note.md`가 Phase별 상세 문서(`.release-notes/scripts/docs/phase*.md`)를 참조하는 것이 이 패턴의 대표적인 예입니다.

### Q3: Command 파일을 팀에서 공유하려면 어떻게 하나요?
**A**: `.claude/commands/` 폴더를 Git에 커밋하면 됩니다. `git add .claude/commands/release-note.md && git push`를 실행하면 팀원이 `git pull` 후 바로 `/release-note` 명령어를 사용할 수 있습니다. Command 개선 이력도 Git 히스토리로 추적됩니다.

이제 Command의 기본 개념을 이해했으니, 다음 절에서 Command 파일의 상세 문법과 효과적인 프롬프트 작성 기법을 살펴보겠습니다.
