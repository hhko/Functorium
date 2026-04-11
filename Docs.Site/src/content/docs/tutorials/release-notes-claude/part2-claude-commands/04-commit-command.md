---
title: "Introduction to commit.md"
---

릴리스 노트 자동화 시스템은 커밋 메시지를 파싱하여 기능을 분류합니다. `feat(api): 새로운 엔드포인트 추가`는 "새로운 기능" 섹션으로, `fix(auth): 토큰 만료 오류 수정`은 "버그 수정" 섹션으로, `feat!: API 응답 형식 변경`은 "Breaking Changes" 섹션으로 자동 분류됩니다. 이 자동 분류가 동작하려면 커밋 메시지가 일관된 형식을 따라야 합니다. 형식이 제각각이면 파싱이 불가능하고, 릴리스 노트 자동화의 전제가 무너집니다.

`commit.md` Command는 이 문제를 해결합니다. Conventional Commits 규격에 따라 커밋 메시지를 작성하도록 Claude에게 지시하여, 사람이 실수로 형식을 어기는 것을 방지합니다. 말하자면, `commit.md`는 `release-note.md`가 제대로 동작하기 위한 **토대에** 해당합니다.

파일 위치는 `.claude/commands/commit.md`입니다.

## 프론트매터

```yaml
---
title: COMMIT
description: Conventional Commits 규격에 따라 변경사항을 커밋합니다.
argument-hint: "[topic]을 전달하면 해당 topic 관련 파일만 선별하여 커밋합니다"
---
```

argument-hint에서 `[topic]`을 대괄호로 감싼 것은 선택적 인자임을 나타냅니다. Topic 없이 `/commit`만 실행할 수도 있고, `/commit 빌드`처럼 특정 Topic을 지정할 수도 있습니다.

## Conventional Commits 형식

commit.md는 Conventional Commits 규격을 따릅니다.

```txt
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### 예시

```txt
feat(calculator): 나눗셈 기능 구현

- Divide 메서드 추가
- 0으로 나누기 예외 처리 포함

Closes #42
```

## 커밋 타입

어떤 커밋 타입이 릴리스 노트의 어느 섹션으로 매핑되는지를 이해하는 것이 중요합니다. `feat`과 `fix`는 사용자에게 직접적인 영향이 있으므로 릴리스 노트에 포함되지만, `docs`, `style`, `test` 같은 타입은 내부 변경이므로 보통 생략됩니다.

| 타입 | 설명 | 릴리스 노트 분류 |
|------|------|-----------------|
| `feat` | 새로운 기능 추가 | 새로운 기능 |
| `fix` | 버그 수정 | 버그 수정 |
| `docs` | 문서 변경 | (보통 생략) |
| `style` | 코드 포맷팅 | (생략) |
| `refactor` | 리팩터링 | (보통 생략) |
| `perf` | 성능 개선 | 개선사항 |
| `test` | 테스트 추가/수정 | (생략) |
| `build` | 빌드 시스템 변경 | (보통 생략) |
| `ci` | CI 설정 변경 | (생략) |
| `chore` | 기타 변경 | (생략) |

## Topic 파라미터

commit Command의 핵심 기능 중 하나는 **Topic 필터링입니다.** 하나의 작업 세션에서 여러 종류의 파일을 수정하는 경우가 흔한데, 이때 모든 변경사항을 하나의 커밋에 담으면 커밋 히스토리가 지저분해집니다.

### Topic 미지정

Topic 없이 `/commit`을 실행하면, Claude가 모든 변경사항을 분석하여 논리적 단위로 분리한 뒤 여러 커밋을 생성합니다.

```bash
/commit
```

예를 들어 `UserService.cs`, `UserServiceTests.cs`, `README.md`, `.gitignore`가 변경된 상태라면, Claude는 기능 변경, 테스트, 문서, 설정 파일을 각각 분리하여 `feat(user): 사용자 서비스 추가`, `test(user): 사용자 서비스 테스트 추가`, `docs: README 업데이트`, `chore: .gitignore 업데이트` 같은 개별 커밋을 생성합니다.

### Topic 지정

특정 Topic을 지정하면, Claude가 해당 Topic과 관련된 파일만 선별하여 단일 커밋을 생성합니다.

```bash
/commit 빌드
```

변경 파일 중 `Build-Local.ps1`과 `Directory.Build.props`는 빌드 관련이므로 선택되고, `README.md`와 `UserService.cs`는 빌드와 무관하므로 제외됩니다. 결과적으로 빌드 관련 파일만 포함된 `feat(build): 빌드 설정 개선` 커밋이 생성되고, 나머지 파일은 unstaged 상태로 남습니다.

Topic 선별은 파일명의 키워드, 파일 내용과의 관련성, 디렉토리 경로의 키워드, 변경 내용의 주제를 종합적으로 판단하여 이루어집니다.

## 커밋 메시지 작성 규칙

커밋 메시지의 각 부분에는 따라야 할 규칙이 있습니다.

**제목(첫 줄)은** 72자 이내로 작성하고, 명령형으로 작성합니다("추가한다"가 아닌 "추가"). 마침표는 사용하지 않으며, 한글로 작성합니다.

**본문(선택)은** 제목과 빈 줄로 구분하고, "무엇을", "왜" 변경했는지를 설명합니다. 72자마다 줄바꿈을 권장합니다.

**푸터(선택)는** Breaking Change 정보나 관련 이슈 참조(`Closes #123`)에 사용합니다.

## 커밋 메시지 예시

### 새 기능 추가

```txt
feat(calculator): 나눗셈 기능 구현

- Divide 메서드 추가
- 0으로 나누기 예외 처리 포함
```

### 버그 수정

```txt
fix(auth): 토큰 만료 시 자동 갱신 실패 수정

만료된 리프레시 토큰으로 갱신 시도 시
무한 루프에 빠지는 문제 해결

Closes #42
```

### Breaking Change

```txt
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답 JSON 구조가 변경되었습니다.
```

### 리팩터링

```txt
refactor: 테스트 픽스처에 공통 설정 추출

각 테스트 클래스에서 중복된 초기화 코드를
BaseTestFixture로 이동
```

## 커밋 조건 및 금지사항

코드를 변경한 경우, 커밋 전에 모든 테스트가 통과하고, 모든 컴파일러 경고가 해결되어 있어야 하며, 하나의 논리적 작업 단위여야 합니다.

커밋 메시지에 절대 포함하지 않아야 할 것도 있습니다. Claude/AI 생성 관련 메시지(`Co-Authored-By: Claude` 등), 이모지, 그리고 테스트 실패나 경고가 있는 상태에서의 커밋이 해당됩니다.

## 커밋 절차

### 기본 절차 (Topic 미지정)

```bash
# 1. 변경사항 확인
git status

# 2. 변경 내용 검토
git diff

# 3. 최근 커밋 스타일 확인
git log --oneline -5

# 4. 논리적 단위로 분리하여 스테이징 및 커밋
git add <files>
git commit -m "type(scope): description"
```

### Topic 지정 시 절차

```bash
# 1. 변경사항 확인
git status
git diff

# 2. Topic 관련 파일 선별
# 예: /commit 빌드

# 3. 선별된 파일만 스테이징
git add Build-Local.ps1 Directory.Build.props

# 4. 단일 커밋 생성
git commit -m "feat(build): 빌드 설정 개선"

# 5. 검증 (topic 무관 파일은 unstaged 상태)
git status
```

## 완료 메시지 형식

```txt
커밋 완료

커밋 정보:
  - 타입: feat
  - 메시지: 나눗셈 기능 구현
  - 변경 파일: 3개
```

## release-note.md와의 관계

```txt
commit.md                            release-note.md
    │                                      │
    │  Conventional Commits 형식           │
    │  ─────────────────────────▶          │
    │                                      │
    │  커밋 히스토리                       │
    │  ─────────────────────────▶          │
    │                                      │
    ▼                                      ▼
일관된 커밋 메시지          ────▶   자동 분류 및 릴리스 노트 생성
```

이 다이어그램이 두 Command의 관계를 잘 보여줍니다. `commit.md`가 생성하는 일관된 커밋 메시지는 `release-note.md`의 자동 분류 엔진이 소비하는 데이터입니다. Conventional Commits 형식을 따르는 커밋 히스토리가 쌓여야 비로소 `release-note.md`가 커밋을 "새로운 기능", "버그 수정", "Breaking Changes"로 정확하게 분류할 수 있습니다. 두 Command는 독립적으로 존재하지만, 함께 사용할 때 자동화 시스템의 전체 가치가 실현됩니다.

## FAQ

### Q1: `/commit`을 Topic 없이 실행하면 모든 변경사항이 하나의 커밋에 담기나요?
**A**: 아닙니다. Topic 없이 `/commit`을 실행하면 Claude가 모든 변경사항을 분석하여 **논리적 단위로 분리한 뒤 여러 커밋을 생성합니다.** 예를 들어 기능 변경, 테스트, 문서, 설정 파일을 각각 별도의 커밋으로 만듭니다.

### Q2: `commit.md`와 `release-note.md`는 왜 함께 사용해야 하나요?
**A**: `commit.md`가 생성하는 일관된 Conventional Commits 형식의 커밋 메시지가 `release-note.md`의 자동 분류 엔진이 소비하는 데이터입니다. `feat(api):` 같은 형식이 지켜져야 Phase 3에서 커밋을 "새로운 기능", "버그 수정", "Breaking Changes"로 정확하게 분류할 수 있습니다. 두 Command는 **생산자-소비자 관계입니다.**

### Q3: 커밋 메시지에 `Co-Authored-By: Claude` 같은 AI 생성 관련 메시지를 포함하면 안 되는 이유는 무엇인가요?
**A**: 릴리스 노트 자동화에서 커밋 메시지를 파싱할 때 노이즈가 됩니다. Conventional Commits 형식의 `type(scope): description`만으로 커밋을 분류해야 하는데, 불필요한 메타데이터가 포함되면 파싱 정확도가 떨어지고 커밋 히스토리가 지저분해집니다.

Part 2에서 살펴본 네 가지 주제(Command 개념, 문법, release-note.md 구조, commit.md 구조)를 바탕으로, 다음 Part에서는 5단계 워크플로우의 실제 동작을 하나씩 살펴보겠습니다.
