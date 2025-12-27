# 3.1 사용자 정의 Command란?

> Claude Code의 사용자 정의 Command는 복잡한 작업을 간단한 명령어로 실행할 수 있게 해주는 기능입니다. 이 절에서는 Command의 개념과 구조를 알아봅니다.

---

## 사용자 정의 Command 개요

사용자 정의 Command는 **Markdown 파일**로 작성된 **재사용 가능한 프롬프트**입니다. 복잡한 작업 지침을 파일로 저장해두고, 필요할 때 간단한 명령어로 실행합니다.

### 비유: 레시피 북

```txt
수동 요리:
├── 재료 준비 방법 기억하기
├── 조리 순서 기억하기
├── 불 세기 기억하기
└── 매번 처음부터 생각하기

레시피 북 사용:
└── 레시피 펼치고 따라하기
    └── 항상 같은 결과
```

사용자 정의 Command는 개발 작업의 "레시피"입니다.

---

## Command 파일 위치

Command 파일은 프로젝트 루트의 `.claude/commands/` 폴더에 저장합니다:

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

**명명 규칙:**
- 파일명 = 명령어 이름
- 확장자는 `.md`
- 파일명에 공백 대신 하이픈(`-`) 사용

| 파일명 | 실행 명령어 |
|--------|------------|
| `release-note.md` | `/release-note` |
| `commit.md` | `/commit` |
| `code-review.md` | `/code-review` |

---

## 왜 Command를 사용하는가?

### 1. 반복 작업 자동화

**수동 작업:**
```txt
릴리스 노트 작성 시:
1. "git log를 확인해줘"
2. "커밋을 분류해줘"
3. "새로운 기능을 문서화해줘"
4. "Breaking Changes를 확인해줘"
5. "마크다운으로 정리해줘"
...
매번 같은 지시를 반복
```

**Command 사용:**
```bash
/release-note v1.2.0
# 위의 모든 작업이 자동으로 실행
```

### 2. 품질 일관성

Command 파일에 모든 규칙과 기준을 명시하면, 매번 같은 품질의 결과물을 얻습니다:

```markdown
## 작성 규칙

1. 모든 기능에 "Why this matters" 섹션 필수
2. 코드 샘플은 실행 가능해야 함
3. API는 Uber 파일에서 검증해야 함
```

### 3. 지식 공유

Command 파일을 Git에 커밋하면:
- 팀 전체가 같은 워크플로우 사용
- 신규 팀원도 바로 작업 가능
- 워크플로우 개선 이력 추적

```bash
git add .claude/commands/release-note.md
git commit -m "feat(claude): 릴리스 노트 자동화 Command 추가"
git push
```

### 4. 점진적 개선

문제가 발견되면 Command 파일을 수정하여 개선:

```markdown
## Phase 5: 검증 (v1.1에서 추가됨)

**검증 항목:**
1. 프론트매터 존재 확인
2. 필수 섹션 포함 확인
3. "Why this matters" 섹션 확인  # 새로 추가
4. API 검증             # 새로 추가
```

---

## Command 파일 기본 구조

Command 파일은 세 부분으로 구성됩니다:

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

파일 최상단에 `---`로 감싼 메타데이터:

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

프론트매터 다음에 Markdown 제목:

```markdown
# 릴리스 노트 자동 생성 규칙
```

### 3. 본문

Claude에게 전달되는 상세 지침:

```markdown
## 워크플로우

1. 환경 검증
2. 데이터 수집
3. 문서 작성

## 규칙

- 모든 API는 검증해야 함
- 코드 샘플 필수
```

---

## $ARGUMENTS 변수

Command 실행 시 전달된 인자는 `$ARGUMENTS` 변수로 접근합니다.

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

인자가 전달되지 않으면 `$ARGUMENTS`는 빈 문자열:

```bash
/release-note
# $ARGUMENTS = ""
```

Command에서 인자 유무를 확인하는 로직을 포함할 수 있습니다:

```markdown
## 버전 파라미터 ($ARGUMENTS)

**버전이 지정된 경우:** $ARGUMENTS

버전 파라미터는 필수입니다. 지정되지 않은 경우 오류 메시지를 출력하고 중단합니다.
```

---

## Command 실행 방법

### 대화형 모드에서 실행

```bash
claude
> /release-note v1.2.0
> /commit
> /review src/main.cs
```

### 슬래시 필수

Command는 반드시 슬래시(`/`)로 시작합니다:

```bash
# 올바른 실행
> /release-note v1.2.0

# 잘못된 실행 (일반 질문으로 처리됨)
> release-note v1.2.0
```

### 자동 완성

대화형 모드에서 `/`를 입력하면 사용 가능한 Command 목록이 표시됩니다:

```bash
> /
Available commands:
  /release-note - 릴리스 노트를 자동으로 생성합니다
  /commit       - Conventional Commits 규격에 따라 커밋합니다
  /review       - 코드 리뷰를 수행합니다
```

---

## 간단한 Command 작성 예시

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

---

## 정리

| 개념 | 설명 |
|------|------|
| 위치 | `.claude/commands/*.md` |
| 파일명 | 명령어 이름 (예: `release-note.md` → `/release-note`) |
| 구조 | YAML 프론트매터 + Markdown 본문 |
| 인자 | `$ARGUMENTS` 변수로 접근 |
| 실행 | 대화형 모드에서 `/명령어 [인자]` |

---

## 다음 단계

- [3.2 Command 문법 및 구조](02-command-syntax.md)
