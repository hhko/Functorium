# Claude Git Commit 명령 매뉴얼

이 문서는 Conventional Commits 규격에 따라 Git 커밋 메시지를 작성하는 방법을 설명합니다.

## 목차
- [Conventional Commits 개요](#conventional-commits-개요)
- [요약](#요약)
- [커밋 타입](#커밋-타입)
- [커밋 메시지 작성 규칙](#커밋-메시지-작성-규칙)
- [커밋 메시지 예시](#커밋-메시지-예시)
- [커밋 조건 및 원칙](#커밋-조건-및-원칙)
- [커밋 절차](#커밋-절차)
- [커밋 금지 사항](#커밋-금지-사항)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## Conventional Commits 개요

Conventional Commits는 커밋 메시지에 일관된 규칙을 적용하여 변경 이력을 명확하게 관리하는 규격입니다.

### 기본 형식

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### 구성 요소

| 요소 | 필수 여부 | 설명 |
|------|----------|------|
| `type` | 필수 | 커밋의 유형 (feat, fix, docs 등) |
| `scope` | 선택 | 영향받는 코드 영역 (auth, api 등) |
| `description` | 필수 | 변경 사항에 대한 간략한 설명 (72자 이내) |
| `body` | 선택 | 변경 사항의 상세 설명 |
| `footer` | 선택 | Breaking Change, 이슈 참조 등 |

<br/>

## 요약

### 주요 명령

**기본 명령:**
```bash
git status
git diff
git add <파일>
git commit -m "feat: 새로운 기능 추가"
```

**타입별 예시:**
```bash
git commit -m "feat(auth): 소셜 로그인 지원 추가"
git commit -m "fix(api): 타임아웃 오류 처리"
git commit -m "refactor: 중복 코드 메서드로 추출"
git commit -m "docs: README 설치 가이드 추가"
```

**수정:**
```bash
git commit --amend -m "수정된 메시지"
git add <파일> && git commit --amend --no-edit
```

### 주요 절차

**1. 기본 커밋 절차:**
```bash
# 1. 변경사항 확인
git status
git diff

# 2. 최근 커밋 스타일 확인
git log --oneline -5

# 3. 파일 스테이징
git add <파일>

# 4. 커밋
git commit -m "feat: 새로운 기능 추가"

# 5. 커밋 확인
git status
```

**2. Topic별 선택적 커밋:**
```bash
# 1. 변경사항 확인
git status

# 2. Topic 관련 파일만 스테이징
git add src/Calculator.cs
git add tests/CalculatorTests.cs

# 3. Topic별 커밋
git commit -m "feat(calculator): 나눗셈 기능 추가"
```

### 주요 개념

**1. Conventional Commits 형식**
```
<type>[scope]: <description>

[body]

[footer]
```
- `type`: 필수 (feat, fix, docs 등)
- `scope`: 선택 (영향받는 영역)
- `description`: 필수 (72자 이내)

**2. 커밋 타입**

| 타입 | 사용 시점 | 테스트 필수 |
|------|----------|------------|
| `feat` | 새 기능 추가 | O |
| `fix` | 버그 수정 | O |
| `refactor` | 리팩터링 | O |
| `docs` | 문서만 변경 | X |
| `chore` | 설정 변경 | X |

**3. 커밋 원칙**
- **원자적 커밋**: 하나의 커밋 = 하나의 논리적 변경
- **작고 잦은 커밋**: TDD 사이클마다 커밋
- **리팩터링 분리**: 기능 변경과 리팩터링은 별도 커밋

**4. 금지 사항**
- Claude/AI 관련 메시지
- Co-Authored-By 태그
- 이모지 사용

<br/>

## 커밋 타입

### 코드 변경 타입 (테스트/빌드 통과 필수)

| 타입 | 설명 | 예시 |
|------|------|------|
| `feat` | 새로운 기능 추가 | `feat: 사용자 로그인 기능 추가` |
| `fix` | 버그 수정 | `fix: null 참조 예외 처리` |
| `refactor` | 리팩터링 (기능/버그 수정 아님) | `refactor: 중복 코드 메서드로 추출` |
| `perf` | 성능 개선 | `perf: 쿼리 최적화` |
| `test` | 테스트 추가/수정 | `test: 로그인 실패 케이스 추가` |
| `build` | 빌드 시스템/의존성 변경 | `build: NuGet 패키지 업데이트` |

> **중요**: 이 타입들을 사용할 때는 반드시 모든 테스트가 통과하고 빌드 경고가 없어야 합니다.

### 코드 외 변경 타입 (테스트/빌드 조건 불필요)

| 타입 | 설명 | 예시 |
|------|------|------|
| `docs` | 문서 변경 | `docs: README 설치 가이드 추가` |
| `style` | 코드 포맷팅만 변경 (로직 변경 없음) | `style: 들여쓰기 통일` |
| `chore` | 기타 변경 (설정 파일 등) | `chore: .gitignore 업데이트` |
| `ci` | CI 설정 변경 | `ci: GitHub Actions 워크플로우 추가` |

<br/>

## 커밋 메시지 작성 규칙

### 1. 제목 (첫 줄)

| 규칙 | 설명 | 예시 |
|------|------|------|
| 길이 | 72자 이내 | `feat: 로그인 기능 추가` |
| 어조 | 명령형으로 작성 | "추가" (O), "추가한다" (X) |
| 마침표 | 사용 금지 | `feat: 로그인 추가` (O), `feat: 로그인 추가.` (X) |
| 언어 | 한글 사용 | `feat: 로그인 추가` |

### 2. 본문 (선택)

- 제목과 빈 줄로 구분
- "무엇을", "왜" 변경했는지 설명
- 72자마다 줄바꿈 권장

### 3. 푸터 (선택)

- Breaking Change 정보
- 관련 이슈 참조 (예: `Closes #123`)

### 4. 스코프 사용법

영향받는 코드 영역을 괄호 안에 명시합니다:

```
feat(auth): 소셜 로그인 지원 추가
fix(api): 타임아웃 오류 처리
refactor(calculator): 연산 로직 분리
```

### 5. Breaking Changes

호환성을 깨는 변경은 느낌표(!)를 타입 뒤에 추가하거나 푸터에 명시합니다:

```
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답 JSON 구조가 변경되었습니다.
```

<br/>

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

### Breaking Change

```
feat(api)!: 응답 형식 변경

BREAKING CHANGE: API 응답이 배열에서 객체로 변경되었습니다.
이전: [{ id: 1 }, { id: 2 }]
이후: { data: [{ id: 1 }, { id: 2 }], total: 2 }
```

<br/>

## 커밋 조건 및 원칙

### 커밋 조건

다음 조건을 **모두** 충족할 때만 커밋하십시오:

| 조건 | 설명 | 적용 범위 |
|------|------|----------|
| 테스트 통과 | 모든 테스트가 성공적으로 통과 | 코드 변경 시 (feat, fix, refactor, perf, test, build) |
| 경고 해결 | 컴파일러/린터 경고가 없음 | 모든 커밋 |
| 논리적 단위 | 하나의 논리적 작업 단위만 포함 | 모든 커밋 |

### 커밋 원칙

| 원칙 | 설명 | 예시 |
|------|------|------|
| 원자적 커밋 | 하나의 커밋은 하나의 논리적 변경만 포함 | 기능 추가 1개 (O), 기능 추가 + 버그 수정 (X) |
| 작고 잦은 커밋 | 크고 드문 커밋보다 작고 잦은 커밋 선호 | TDD 사이클마다 커밋 권장 |
| 리팩터링 분리 | 기능 변경과 리팩터링은 별도 커밋으로 분리 | 1. refactor: 코드 정리<br>2. feat: 새 기능 추가 |

<br/>

## 커밋 절차

### 기본 커밋 절차 (Topic 미지정)

```bash
# 1. 변경사항 확인
git status

# 2. 변경 내용 검토
git diff

# 3. 최근 커밋 스타일 확인
git log --oneline -5

# 4. 논리적 단위로 분리하여 스테이징 및 커밋
git add <파일>
git commit -m "feat: 새로운 기능 추가"

# 5. 커밋 확인
git status
```

### Topic 파라미터 사용 시 커밋 절차

Topic 파라미터를 사용하면 특정 topic과 관련된 변경사항만 선별하여 커밋할 수 있습니다.

**파일 선별 기준:**
- 파일명에 topic 키워드 포함
- 파일 내용이 topic과 직접 관련
- 디렉토리 경로에 topic 키워드 포함
- 변경 내용이 topic 주제와 연관

**필수 절차:**

```bash
# 1. 변경사항 확인
git status
git diff

# 2. Topic 관련 파일 선별
# - 지정된 topic과 관련된 파일만 식별
# - 파일명, 경로, 내용을 기준으로 판단
# - 불확실한 경우 git diff <파일>로 변경 내용 검토

# 3. 선별된 파일만 스테이징
git add <topic-관련-파일1> <topic-관련-파일2> ...

# 4. 단일 커밋 생성
git commit -m "feat(topic): 관련 기능 추가"

# 5. 검증
# - git status로 topic과 무관한 파일이 unstaged 상태인지 확인
git status
```

**예시:**
```bash
# 변경된 파일: Build-Local.ps1, Directory.Build.props, README.md
# Topic: MinVer

# 1. 변경사항 확인
git status
# Build-Local.ps1 (MinVer 관련)
# Directory.Build.props (MinVer 설정)
# README.md (MinVer와 무관)

# 2. MinVer 관련 파일만 스테이징
git add Build-Local.ps1 Directory.Build.props

# 3. 커밋 (README.md는 제외)
git commit -m "feat(minver): MinVer 버전 정보 표시 추가"

# 4. 확인 (README.md는 여전히 unstaged)
git status
# modified: README.md
```

**중요:**
- Topic과 무관한 파일은 절대 커밋하지 않음
- 하나의 topic = 하나의 커밋
- Topic 관련 파일만 선별하여 스테이징

### `/commit` 커맨드 사용

Claude Code를 사용하는 경우 `/commit` 커맨드를 활용할 수 있습니다:

```bash
# 모든 변경사항을 논리적 단위로 분리하여 커밋
/commit

# 특정 topic 관련 변경만 커밋
/commit Calculator
/commit 테스트 리팩터링
/commit API 엔드포인트
/commit MinVer
```

### 완료 메시지

커밋 완료 시 다음 형식으로 표시됩니다:

```
커밋 완료

커밋 정보:
  - 타입: feat/fix/docs/style/refactor/perf/test/build/ci/chore
  - 메시지: {커밋 메시지 첫 줄}
  - 변경 파일: N개
```

<br/>

## 커밋 금지 사항

### 절대 포함하지 말아야 할 내용

커밋 메시지는 순수하게 변경 내용만 설명해야 합니다. 다음 내용은 **절대** 포함하지 마십시오:

| 금지 항목 | 예시 | 이유 |
|----------|------|------|
| Claude/AI 관련 메시지 | `Generated with [Claude Code]` | 도구 관련 정보는 커밋 메시지와 무관 |
| 공동 저자 표시 | `Co-Authored-By: Claude` | 사람이 아닌 도구는 저자가 될 수 없음 |
| 이모지 | 등 | 일관성 없고 전문적이지 않음 |
| 자동 생성 관련 언급 | `AI가 생성함` | 커밋 내용과 무관 |

### 코드 변경 시 금지 사항

| 금지 항목 | 설명 | 해결 방법 |
|----------|------|----------|
| 실패한 테스트 | 테스트가 실패한 상태로 커밋 금지 | 테스트 수정 후 커밋 |
| 컴파일/린터 경고 | 경고가 있는 상태로 커밋 금지 | 경고 해결 후 커밋 |
| 혼합 커밋 | 코드 변경과 문서 변경을 같은 커밋에 포함 금지 | 별도 커밋으로 분리 |

<br/>

## 트러블슈팅

### 커밋 메시지를 잘못 작성했을 때

아직 push하지 않은 경우:
```bash
git commit --amend -m "올바른 메시지"
```

이미 push한 경우:
```bash
git commit --amend -m "올바른 메시지"
git push --force-with-lease  # 주의: 팀원과 협의 필요
```

> **주의**: force push는 공유 브랜치에서 사용하지 마세요.

### 테스트가 실패한 상태로 커밋하려 했을 때

```bash
# 1. 테스트 실행하여 실패 원인 파악
dotnet test

# 2. 테스트 통과시키기
# (코드 수정)

# 3. 테스트 재실행
dotnet test

# 4. 모든 테스트 통과 후 커밋
git commit -m "fix: 테스트 실패 수정"
```

### 여러 변경사항을 한 커밋에 포함하려 했을 때

논리적 단위로 분리하여 커밋:

```bash
# 1. 첫 번째 기능만 스테이징
git add src/Feature1.cs tests/Feature1Tests.cs
git commit -m "feat: 기능1 추가"

# 2. 두 번째 기능 스테이징
git add src/Feature2.cs tests/Feature2Tests.cs
git commit -m "feat: 기능2 추가"
```

### 이미 스테이징한 파일을 제외하고 싶을 때

```bash
# 특정 파일 스테이징 취소
git restore --staged <파일>

# 모든 스테이징 취소
git restore --staged .
```

### 리팩터링과 기능 추가를 분리하지 않았을 때

```bash
# 1. 마지막 커밋 취소 (변경사항 유지)
git reset --soft HEAD~1

# 2. 리팩터링 부분만 스테이징
git add src/RefactoredFile.cs
git commit -m "refactor: 코드 정리"

# 3. 기능 추가 부분 스테이징
git add src/NewFeature.cs
git commit -m "feat: 새 기능 추가"
```

<br/>

## FAQ

### Q1. Conventional Commits를 왜 사용해야 하나요?

| 장점 | 설명 |
|------|------|
| 명확한 이력 | 커밋 타입으로 변경 종류를 즉시 파악 |
| 자동화 | Changelog 자동 생성, 버전 관리 자동화 |
| 협업 향상 | 팀원 간 일관된 커밋 스타일 유지 |
| 코드 리뷰 | 변경 의도를 명확히 전달 |

### Q2. 스코프는 언제 사용하나요?

스코프는 선택사항이지만, 다음 경우에 유용합니다:
- 프로젝트가 여러 모듈로 구성된 경우
- 특정 영역의 변경임을 강조하고 싶을 때
- 팀 내에서 스코프 규칙을 정한 경우

```bash
# 좋은 예시
feat(auth): OAuth 로그인 추가
fix(payment): 결제 금액 계산 오류 수정
refactor(database): 연결 풀 관리 개선
```

### Q3. Breaking Change는 언제 표시하나요?

다음과 같이 호환성이 깨지는 경우:
- API 응답 형식 변경
- 함수 시그니처 변경
- 공개 인터페이스 변경
- 설정 파일 형식 변경

```bash
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답이 배열에서 객체로 변경됨
```

### Q4. 커밋은 얼마나 자주 해야 하나요?

| 권장 사항 | 설명 |
|----------|------|
| TDD 사이클마다 | Red -> Green -> Refactor 각 단계에서 커밋 |
| 논리적 단위마다 | 하나의 완결된 작업이 끝나면 커밋 |
| 작고 자주 | 큰 커밋 1개보다 작은 커밋 여러 개가 좋음 |

### Q5. 리팩터링과 기능 추가를 어떻게 분리하나요?

**나쁜 예시** (혼합 커밋):
```bash
git commit -m "feat: 로그인 기능 추가 및 코드 정리"
```

**좋은 예시** (분리 커밋):
```bash
git commit -m "refactor: 인증 관련 코드 정리"
git commit -m "feat: 로그인 기능 추가"
```

리팩터링은 항상 기능 추가 **이전**에 커밋하세요.

### Q6. 여러 파일을 변경했는데 하나의 커밋으로 묶어야 하나요?

논리적으로 하나의 변경이라면 여러 파일을 하나의 커밋에 포함할 수 있습니다:

```bash
# 좋은 예시: 하나의 기능이지만 여러 파일 변경
git add src/User.cs src/UserService.cs tests/UserTests.cs
git commit -m "feat: 사용자 프로필 수정 기능 추가"
```

단, 각 파일이 독립적인 변경이라면 분리하세요:

```bash
# 좋은 예시: 독립적인 변경
git add src/User.cs
git commit -m "feat: 사용자 이름 유효성 검사 추가"

git add src/Order.cs
git commit -m "fix: 주문 금액 계산 오류 수정"
```

### Q7. 테스트를 먼저 작성하고 커밋해야 하나요?

TDD를 따르는 경우:

```bash
# 1. 실패하는 테스트 작성 및 커밋
git add tests/CalculatorTests.cs
git commit -m "test: 나눗셈 테스트 추가"

# 2. 구현 코드 작성 및 커밋
git add src/Calculator.cs
git commit -m "feat: 나눗셈 기능 구현"

# 3. 리팩터링 및 커밋
git add src/Calculator.cs
git commit -m "refactor: 나눗셈 로직 최적화"
```

### Q8. 문서 변경과 코드 변경을 같이 커밋해도 되나요?

**금지**: 문서 변경과 코드 변경은 분리하세요.

```bash
# 나쁜 예시
git commit -m "feat: 로그인 기능 추가 및 README 업데이트"

# 좋은 예시
git commit -m "feat: 로그인 기능 추가"
git commit -m "docs: 로그인 API 문서 추가"
```

### Q9. 커밋 메시지를 영어로 써야 하나요?

이 프로젝트에서는 **한글**을 사용합니다:
- 팀원 모두가 한국어 사용자
- 명확한 의사소통
- 일관성 유지

```bash
# 이 프로젝트의 규칙
git commit -m "feat: 로그인 기능 추가"  # O
git commit -m "feat: add login feature"  # X
```

### Q10. 커밋을 잘못했을 때 어떻게 수정하나요?

| 상황 | 해결 방법 |
|------|----------|
| 커밋 메시지만 수정 | `git commit --amend -m "새 메시지"` |
| 파일 추가 누락 | `git add 파일 && git commit --amend --no-edit` |
| 커밋 자체를 취소 | `git reset --soft HEAD~1` |
| 이미 push한 경우 | 새 커밋으로 수정 또는 팀원 협의 후 `--force-with-lease` |

### Q11. Topic 파라미터는 어떻게 사용하나요?

`/commit` 커맨드에 topic을 지정하면 해당 topic과 관련된 파일만 선별하여 커밋합니다:

```bash
# 모든 변경사항 커밋 (topic 미지정)
/commit

# Calculator 관련 파일만 커밋
/commit Calculator

# MinVer 관련 파일만 커밋
/commit MinVer

# 테스트 리팩터링 관련 파일만 커밋
/commit 테스트 리팩터링
```

**주의사항:**
- Topic이 지정되면 해당 topic과 관련된 파일만 커밋됩니다
- Topic과 무관한 파일은 unstaged 상태로 남습니다
- 하나의 topic = 하나의 커밋

## 참고 문서

- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/) - Conventional Commits 공식 문서
- [Git 명령어 가이드](./Manual-Git.md) - Git 명령어 사용법
