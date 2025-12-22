---
title: COMMIT
description: Conventional Commits 규격에 따라 변경사항을 커밋합니다.
argument-hint: "[topic]을 전달하면 해당 topic 관련 파일만 선별하여 커밋합니다"
---

# Git 커밋 규칙

Conventional Commits 규격을 따르며, 변경사항을 명확하고 일관된 규칙에 따라 커밋합니다.

## Topic 파라미터 (`$ARGUMENTS`)

**Topic이 지정된 경우:** $ARGUMENTS

Topic 파라미터를 사용하면 특정 topic과 관련된 변경사항만 선별하여 커밋할 수 있습니다.

### Topic 지정 시 동작

**중요:** Topic이 지정되면 해당 topic과 관련된 파일만 커밋해야 합니다.

1. `git status`로 모든 변경사항 확인
2. 지정된 topic과 관련된 파일만 선별
3. 선별된 파일만 스테이징 (`git add`)
4. 단일 커밋 생성
5. **Topic과 무관한 파일은 절대 커밋하지 않음**

**파일 선별 기준:**
- 파일명에 topic 키워드 포함
- 파일 내용이 topic과 직접 관련
- 디렉토리 경로에 topic 키워드 포함
- 변경 내용이 topic 주제와 연관

**Topic이 지정되지 않은 경우:**

모든 변경사항을 대상으로 커밋을 진행합니다. 변경사항이 여러 논리적 단위로 구성된 경우, 각 단위별로 분리하여 커밋합니다.

**사용 예시:**
```
/commit                   # 모든 변경사항을 논리적 단위로 분리하여 커밋
/commit Calculator        # Calculator 관련 파일만 선별하여 커밋
/commit 테스트 리팩터링    # 테스트 리팩터링 관련 파일만 커밋
/commit API 엔드포인트     # API 엔드포인트 관련 파일만 커밋
```

## 적용 범위

이 커밋 규칙은 모든 변경에 적용됩니다. 변경 유형에 따라 적절한 커밋 타입을 선택하십시오.

**코드 변경** (테스트 통과/경고 해결 필수):
- `feat`, `fix`, `refactor`, `perf`, `test`, `build`
- 프로덕션 코드, 테스트 코드, 빌드 스크립트 등

**코드 외 변경** (테스트/빌드 조건 불필요):
- `docs`: 문서 수정 (README, .md 파일 등)
- `style`: 코드 포맷팅만 변경 (로직 변경 없음)
- `chore`: .gitignore, .editorconfig 등 설정 파일 변경
- `ci`: CI 설정 변경

## Conventional Commits 형식

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### 커밋 타입 (type)

| 타입 | 설명 | 예시 |
|------|------|------|
| `feat` | 새로운 기능 추가 | `feat: 사용자 로그인 기능 추가` |
| `fix` | 버그 수정 | `fix: null 참조 예외 처리` |
| `docs` | 문서 변경 | `docs: README 설치 가이드 추가` |
| `style` | 코드 포맷팅 (동작 변경 없음) | `style: 들여쓰기 통일` |
| `refactor` | 리팩터링 (기능/버그 수정 아님) | `refactor: 중복 코드 메서드로 추출` |
| `perf` | 성능 개선 | `perf: 쿼리 최적화` |
| `test` | 테스트 추가/수정 | `test: 로그인 실패 케이스 추가` |
| `build` | 빌드 시스템/의존성 변경 | `build: NuGet 패키지 업데이트` |
| `ci` | CI 설정 변경 | `ci: GitHub Actions 워크플로우 추가` |
| `chore` | 기타 변경 (src/test 미포함) | `chore: .gitignore 업데이트` |

### 스코프 (scope)

영향 받는 코드 영역을 괄호 안에 명시합니다 (선택사항):

```
feat(auth): 소셜 로그인 지원 추가
fix(api): 타임아웃 오류 처리
refactor(calculator): 연산 로직 분리
```

### Breaking Changes

호환성을 깨는 변경은 느낌표(!)를 타입 뒤에 추가하거나 푸터에 명시합니다:

```
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답 JSON 구조가 변경되었습니다.
```

## 커밋 메시지 작성 규칙

1. **제목 (첫 줄)**
   - 72자 이내로 작성
   - 명령형으로 작성 ("추가한다" X, "추가" O)
   - 마침표 사용 금지
   - 한글로 작성

2. **본문 (선택)**
   - 제목과 빈 줄로 구분
   - "무엇을", "왜" 변경했는지 설명
   - 72자마다 줄바꿈 권장

3. **푸터 (선택)**
   - Breaking Change 정보
   - 관련 이슈 참조 (예: `Closes #123`)

## 릴리스 노트 분석을 위한 키워드

커밋 메시지는 릴리스 노트 자동 생성 시 분석됩니다. 다음 키워드를 제목에 포함하면 정확한 분류가 가능합니다.

| 기능 유형 | 권장 키워드 | 예시 |
|---------|-----------|------|
| 새 기능 | 추가, 구현, 지원 | `feat: 로깅 기능 추가` |
| 개선 | 개선, 향상, 최적화 | `perf: 쿼리 성능 개선` |
| 수정 | 수정, 해결, 처리 | `fix: null 참조 예외 수정` |
| 제거 | 제거, 삭제 | `refactor: 미사용 코드 제거` |
| 이름 변경 | 이름 변경 | `refactor: 메서드 이름 변경` |

**참고**: GitHub 이슈/PR 참조 (예: `#123`)를 포함하면 릴리스 노트 생성 시 추가 컨텍스트를 추출할 수 있습니다.

## 커밋 메시지 예시

### 새 기능 추가
```
feat(calculator): 나눗셈 기능 구현

- Divide 메서드 추가
- 0으로 나누기 예외 처리 포함
```

### 버그 수정
```
fix(auth): 토큰 만료 시 자동 갱신 실패 수정

만료된 리프레시 토큰으로 갱신 시도 시
무한 루프에 빠지는 문제 해결

Closes #42
```

### 리팩터링
```
refactor: 테스트 픽스처에 공통 설정 추출

각 테스트 클래스에서 중복된 초기화 코드를
BaseTestFixture로 이동
```

### 문서 수정
```
docs: API 엔드포인트 문서 추가
```

### 빌드/의존성
```
build: LanguageExt.Core 4.4.9 패키지 추가
```

## 커밋 조건

다음 조건을 **모두** 충족할 때만 커밋하십시오:
- 모든 테스트가 통과했을 때 (코드 변경 시)
- 모든 컴파일러/린터 경고가 해결되었을 때
- 변경이 하나의 논리적 작업 단위를 나타낼 때

## 커밋 원칙

1. **원자적 커밋**: 하나의 커밋은 하나의 논리적 변경만 포함
2. **작고 잦은 커밋**: 크고 드문 커밋보다 작고 잦은 커밋 선호 (TDD 사이클마다 커밋 권장)
3. **리팩터링 분리**: 기능 변경과 리팩터링은 별도 커밋으로 분리

## 커밋 메시지 금지 사항

**커밋 메시지는 순수하게 변경 내용만 설명해야 합니다.**

다음 내용은 절대 포함하지 마십시오:
- `Generated with [Claude Code]` 등 Claude/AI 생성 관련 메시지
- `Co-Authored-By: Claude` 등 공동 저자 표시
- Claude, AI, 자동 생성 관련 모든 언급
- 이모지

## 커밋 금지 사항

**코드 변경 시:**
- 테스트가 실패한 상태로 커밋하지 마십시오
- 컴파일러/린터 경고가 있는 상태로 커밋하지 마십시오
- 코드 변경과 문서 변경을 같은 커밋에 포함하지 마십시오

## 커밋 절차

### 기본 절차 (Topic 미지정)

1. `git status`로 변경사항 확인
2. `git diff`로 변경 내용 검토
3. `git log --oneline -5`로 최근 커밋 스타일 확인
4. 논리적 단위로 분리하여 스테이징 및 커밋

### Topic 파라미터 사용 시 (`$ARGUMENTS` 지정)

**필수 절차:**

1. **변경사항 확인**
   ```bash
   git status
   git diff
   ```

2. **Topic 관련 파일 선별**
   - 지정된 topic(`$ARGUMENTS`)과 관련된 파일만 식별
   - 파일명, 경로, 내용을 기준으로 판단
   - 불확실한 경우 `git diff <파일>`로 변경 내용 검토

3. **선별된 파일만 스테이징**
   ```bash
   # Topic 관련 파일만 추가
   git add <topic-관련-파일1> <topic-관련-파일2> ...
   ```

4. **단일 커밋 생성**
   - 선별된 파일들로 하나의 커밋 생성
   - 커밋 메시지에 topic 반영
   - Conventional Commits 형식 준수

5. **검증**
   - `git status`로 topic과 무관한 파일이 unstaged 상태인지 확인
   - Topic과 무관한 파일이 커밋에 포함되지 않았는지 확인

**예시:**
```bash
# 변경된 파일: Build-Local.ps1, Directory.Build.props, README.md
# Topic: 빌드

# 1. 변경사항 확인
git status
# Build-Local.ps1 (빌드 관련)
# Directory.Build.props (빌드 설정)
# README.md (빌드와 무관)

# 2. 빌드 관련 파일만 스테이징
git add Build-Local.ps1 Directory.Build.props

# 3. 커밋 (README.md는 제외)
git commit -m "feat(build): 버전 정보 표시 추가"

# 4. 확인 (README.md는 여전히 unstaged)
git status
# modified: README.md
```

**중요:**
- Topic과 무관한 파일은 절대 커밋하지 않음
- 하나의 topic = 하나의 커밋
- Topic 관련 파일만 선별하여 스테이징

## 완료 메시지

커밋 완료 시 다음 형식으로 표시합니다:

```
커밋 완료

커밋 정보:
  - 타입: feat/fix/docs/style/refactor/perf/test/build/ci/chore
  - 메시지: {커밋 메시지 첫 줄}
  - 변경 파일: N개
```

## 참고 자료

- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
- [Claude Code Best Practices](https://www.anthropic.com/engineering/claude-code-best-practices)
