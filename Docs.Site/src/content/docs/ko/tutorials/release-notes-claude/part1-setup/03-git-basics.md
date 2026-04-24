---
title: "Git 기초"
---

릴리스 노트 자동화 시스템의 원재료는 Git 저장소에 있습니다. 커밋 히스토리에서 "무엇이 변경되었는가"를 읽어내고, diff에서 "어떤 코드가 달라졌는가"를 파악하며, 브랜치 비교로 "이전 릴리스 이후 어떤 범위가 변경되었는가"를 결정합니다. 이 세 가지 정보가 결합되어야 비로소 "새로운 기능 N개, 버그 수정 M개, Breaking Changes K개"라는 릴리스 노트가 만들어집니다.

이 절에서는 릴리스 노트 자동화에 필요한 Git 명령어와, 자동 분류의 핵심인 Conventional Commits 규칙을 살펴보겠습니다.

## 기본 Git 명령어

### git log - 커밋 히스토리 확인

`git log`는 누가, 언제, 무엇을 변경했는지를 보여줍니다. 릴리스 노트 자동화에서는 커밋 메시지의 타입(`feat`, `fix` 등)을 파싱하여 기능을 분류하는 데 사용됩니다.

```bash
# 기본 로그
git log

# 한 줄로 요약
git log --oneline

# 특정 개수만 표시
git log --oneline -10

# 특정 폴더의 커밋만
git log --oneline -- Src/Functorium/

# 날짜 범위 지정
git log --oneline --since="2025-01-01" --until="2025-12-31"
```

**출력 예시:**
```
51533b1 refactor(observability): Observability 추상화 및 구조 개선
4683281 feat(linq): TraverseSerial 메서드 추가
93ff9e1 chore(api): Public API 파일 타임스탬프 업데이트
a8ec763 fix(build): NuGet 패키지 아이콘 경로 수정
```

이 출력에서 `feat(linq)`는 "새로운 기능" 섹션으로, `fix(build)`는 "버그 수정" 섹션으로 자동 분류됩니다.

### git diff - 변경 내용 확인

`git diff`는 실제로 어떤 코드가 추가되고 삭제되었는지를 보여줍니다. 릴리스 노트에서 특히 중요한 역할은 **Breaking Changes 감지**입니다. Public API 파일의 diff를 분석하면 삭제되거나 변경된 API를 자동으로 찾아낼 수 있습니다.

```bash
# 작업 디렉토리와 스테이징 영역 비교
git diff

# 두 브랜치 비교
git diff main..feature-branch

# 특정 커밋 비교
git diff abc123..def456

# 변경된 파일 목록만
git diff --name-only

# 통계 요약
git diff --stat
```

**출력 예시 (--stat):**
```
Src/Functorium/Abstractions/Errors/ErrorFactory.cs | 50 +++++++++++
Src/Functorium/Applications/Linq/FinTUtilites.cs      | 30 +++++++
2 files changed, 80 insertions(+)
```

### git branch - 브랜치 관리

```bash
# 로컬 브랜치 목록
git branch

# 원격 브랜치 포함
git branch -a

# 원격 브랜치만
git branch -r

# 특정 브랜치 존재 확인
git branch -r | grep "release/1.0"
```

## 릴리스 노트 자동화에 사용되는 Git 명령어

이제 자동화 시스템이 실제로 사용하는 명령어를 구체적으로 살펴보겠습니다. 각 명령어가 워크플로우의 어느 단계에서, 왜 필요한지를 이해하는 것이 중요합니다.

### 1. Base Branch 결정

릴리스 노트는 "이전 릴리스 이후의 변경사항"을 다루므로, 비교 기준점(Base Branch)을 먼저 결정해야 합니다.

```bash
# release 브랜치 존재 확인
git branch -r | grep "origin/release/1.0"

# 초기 커밋 찾기 (첫 배포 시)
git rev-list --max-parents=0 HEAD
```

### 2. 컴포넌트별 변경사항 수집

Base Branch가 결정되면 그 이후의 변경사항을 컴포넌트(폴더) 단위로 수집합니다.

```bash
# 두 브랜치 간 특정 폴더의 변경 통계
git diff --stat origin/release/1.0..HEAD -- Src/Functorium/

# 두 브랜치 간 특정 폴더의 커밋 목록
git log --oneline origin/release/1.0..HEAD -- Src/Functorium/
```

### 3. Breaking Changes 감지 (API diff)

커밋 메시지의 `!` 표기만으로는 Breaking Changes를 완벽하게 잡아내기 어렵습니다. `.api` 폴더의 Public API 파일을 직접 diff하면 더 정확하게 감지할 수 있습니다.

```bash
# .api 폴더의 변경사항 확인
git diff HEAD -- 'Src/*/.api/*.cs'

# 삭제된 줄만 확인 (Breaking Changes 후보)
git diff HEAD -- 'Src/*/.api/*.cs' | grep "^-.*public"
```

### 4. 기여자 통계

```bash
# 특정 범위의 기여자별 커밋 수
git shortlog -sn origin/release/1.0..HEAD -- Src/Functorium/
```

**출력 예시:**
```
    23  hhko
     5  contributor1
     2  contributor2
```

## Conventional Commits

Conventional Commits는 커밋 메시지의 표준 형식입니다. 릴리스 노트 자동화 시스템이 커밋을 자동으로 분류하려면, 커밋 메시지가 기계가 파싱할 수 있는 일정한 형식을 따라야 합니다. 이것이 Conventional Commits가 필요한 이유입니다.

### 기본 형식

```txt
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

### 커밋 타입

| 타입 | 설명 | 릴리스 노트 분류 |
|------|------|-----------------|
| `feat` | 새로운 기능 | 새로운 기능 |
| `fix` | 버그 수정 | 버그 수정 |
| `docs` | 문서 변경 | (보통 생략) |
| `style` | 코드 스타일 변경 | (생략) |
| `refactor` | 리팩토링 | (보통 생략) |
| `perf` | 성능 개선 | 개선사항 |
| `test` | 테스트 추가/수정 | (생략) |
| `build` | 빌드 시스템 변경 | (보통 생략) |
| `ci` | CI 설정 변경 | (생략) |
| `chore` | 기타 변경 | (생략) |

릴리스 노트에서 가장 중요한 타입은 `feat`과 `fix`입니다. `docs`, `style`, `test` 같은 타입은 사용자에게 직접적인 영향이 없으므로 보통 릴리스 노트에서 생략됩니다.

### Breaking Changes 표기

Breaking Changes는 두 가지 방법으로 표기합니다.

**방법 1: 타입 뒤에 느낌표(!)**
```
feat!: 사용자 인증 방식 변경
fix!(api): 응답 형식 수정
```

**방법 2: 푸터에 BREAKING CHANGE**
```
feat(api): 새로운 인증 시스템 도입

BREAKING CHANGE: 기존 토큰 형식이 변경되었습니다.
마이그레이션이 필요합니다.
```

### 좋은 커밋 메시지 예시

```bash
# 새로운 기능
feat(linq): TraverseSerial 메서드 및 Activity Context 유틸리티 추가

# 버그 수정
fix(build): NuGet 패키지 아이콘 경로 수정

# 리팩토링
refactor(observability): Observability 추상화 및 구조 개선

# Breaking Change
feat!(api): ErrorFactory.CreateExpected 메서드 시그니처 변경

BREAKING CHANGE: errorMessage 매개변수가 필수로 변경되었습니다.
```

### 나쁜 커밋 메시지 예시

```bash
# 너무 모호함
fix: 버그 수정
update: 업데이트

# 타입 누락
사용자 서비스 추가

# 설명 없음
feat:
```

이런 커밋 메시지는 자동 분류가 불가능하거나, 분류되더라도 릴리스 노트에 의미 있는 내용을 담지 못합니다.

## 자동화 시스템의 커밋 분류 로직

릴리스 노트 자동화 시스템은 커밋 메시지를 파싱하여 자동으로 분류합니다. Feature 커밋은 `feat(...)`, `feature(...)`, `add(...)` 패턴으로, Bug Fix 커밋은 `fix(...)`, `bug(...)` 패턴으로 감지합니다.

Breaking Changes 감지는 두 가지 방법을 병행합니다. 커밋 메시지의 `feat!:`, `fix!:`, `BREAKING` 같은 패턴을 보조적으로 확인하되, 더 정확한 방법은 `.api` 폴더의 Git Diff를 직접 분석하는 것입니다. 삭제된 public 클래스, 삭제된 public 메서드, 변경된 메서드 시그니처, 변경된 타입 이름 등을 diff에서 직접 찾아내면 커밋 메시지에 `!`를 빠뜨렸더라도 Breaking Changes를 놓치지 않습니다.

## 실습: Git 명령어 연습

### 1. 커밋 히스토리 확인

```bash
# Functorium 프로젝트 클론
git clone https://github.com/hhko/Functorium.git
cd Functorium

# 최근 10개 커밋 확인
git log --oneline -10

# Src/Functorium 폴더의 커밋만
git log --oneline -10 -- Src/Functorium/
```

### 2. 변경 통계 확인

```bash
# 초기 커밋부터 현재까지의 변경 통계
git diff --stat $(git rev-list --max-parents=0 HEAD)..HEAD -- Src/Functorium/
```

### 3. API 변경사항 확인

```bash
# .api 폴더의 변경 내용
git diff HEAD~10..HEAD -- 'Src/*/.api/*.cs'
```

## FAQ

### Q1: Conventional Commits를 따르지 않는 기존 커밋 히스토리가 있어도 자동화를 사용할 수 있나요?
**A**: 사용할 수 있지만 자동 분류의 정확도가 낮아집니다. `feat`, `fix` 같은 타입 접두사가 없는 커밋은 "other"로 분류되어 Phase 3에서 수동 판단이 필요합니다. 향후 커밋부터 Conventional Commits를 적용하면, 새로운 릴리스의 자동 분류 정확도가 점진적으로 개선됩니다.

### Q2: Breaking Changes 감지에서 `.api` 폴더의 Git diff가 커밋 메시지보다 정확한 이유는 무엇인가요?
**A**: 커밋 메시지에 `!` 표기를 누락하면 커밋 메시지 기반 감지는 실패합니다. 반면 `.api` 폴더에는 PublicApiGenerator가 생성한 Public API 정의가 Git으로 추적되므로, `git diff`로 **삭제된 클래스, 변경된 메서드 시그니처를** 객관적으로 감지할 수 있습니다. 표기 실수와 무관하게 실제 API 변경을 잡아냅니다.

### Q3: `git log --oneline`과 `git diff --stat`은 릴리스 노트 자동화에서 각각 어떤 역할을 하나요?
**A**: `git log --oneline`은 커밋 메시지를 수집하여 `feat`, `fix` 등의 타입별 분류에 사용됩니다. `git diff --stat`은 변경된 파일 수와 추가/삭제 라인 수를 요약하여, 릴리스 노트의 통계 섹션과 컴포넌트별 변경 규모를 파악하는 데 사용됩니다.

Git이 제공하는 커밋 히스토리와 diff 정보를 이해했으니, 이제 이 데이터를 활용하는 Claude Code의 사용자 정의 Command를 본격적으로 살펴보겠습니다.
