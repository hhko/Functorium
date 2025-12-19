# 2.3 Git 기초

> 릴리스 노트 자동화 시스템은 Git의 커밋 히스토리와 diff 기능을 활용합니다. 이 절에서는 필요한 Git 명령어와 Conventional Commits 규칙을 알아봅니다.

---

## Git이란?

Git은 분산 버전 관리 시스템입니다. 소스 코드의 변경 이력을 추적하고 관리합니다.

### 릴리스 노트 자동화에서 Git의 역할

```txt
Git이 제공하는 정보:
├── 커밋 히스토리 (git log)
│   ├── 언제 변경되었는가?
│   ├── 누가 변경했는가?
│   └── 무엇이 변경되었는가?
│
├── 변경 내용 (git diff)
│   ├── 어떤 파일이 변경되었는가?
│   ├── 어떤 코드가 추가/삭제되었는가?
│   └── Breaking Changes 감지
│
└── 브랜치 비교
    ├── 이전 릴리스와 현재 비교
    └── 변경 범위 결정
```

---

## 기본 Git 명령어

### git log - 커밋 히스토리 확인

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

### git diff - 변경 내용 확인

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
Src/Functorium/Abstractions/Errors/ErrorCodeFactory.cs | 50 +++++++++++
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

---

## 릴리스 노트 자동화에 사용되는 Git 명령어

### 1. Base Branch 결정

```bash
# release 브랜치 존재 확인
git branch -r | grep "origin/release/1.0"

# 초기 커밋 찾기 (첫 배포 시)
git rev-list --max-parents=0 HEAD
```

### 2. 컴포넌트별 변경사항 수집

```bash
# 두 브랜치 간 특정 폴더의 변경 통계
git diff --stat origin/release/1.0..HEAD -- Src/Functorium/

# 두 브랜치 간 특정 폴더의 커밋 목록
git log --oneline origin/release/1.0..HEAD -- Src/Functorium/
```

### 3. Breaking Changes 감지 (API diff)

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

---

## Conventional Commits

Conventional Commits는 커밋 메시지에 대한 표준 형식입니다. 릴리스 노트 자동화 시스템은 이 형식을 파싱하여 커밋을 분류합니다.

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

### Breaking Changes 표기

Breaking Changes는 두 가지 방법으로 표기합니다:

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
feat!(api): ErrorCodeFactory.Create 메서드 시그니처 변경

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

---

## 자동화 시스템의 커밋 분류 로직

릴리스 노트 자동화 시스템은 커밋 메시지를 파싱하여 자동으로 분류합니다:

### Feature 커밋 감지

```txt
패턴:
├── feat(...): ...
├── feature(...): ...
└── add(...): ...
```

### Bug Fix 커밋 감지

```txt
패턴:
├── fix(...): ...
└── bug(...): ...
```

### Breaking Changes 감지

**방법 1: 커밋 메시지 패턴 (보조)**
```txt
패턴:
├── feat!: ...
├── fix!: ...
├── <type>!(...): ...
├── breaking ...
└── BREAKING ...
```

**방법 2: Git Diff 분석 (권장)**
```txt
.api 폴더의 변경사항 분석:
├── 삭제된 public 클래스/인터페이스
├── 삭제된 public 메서드
├── 변경된 메서드 시그니처
└── 변경된 타입 이름
```

---

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

---

## 정리

| 명령어 | 용도 | 자동화 시스템 활용 |
|--------|------|-------------------|
| `git log` | 커밋 히스토리 | Feature/Fix 분류 |
| `git diff` | 변경 내용 | Breaking Changes 감지 |
| `git branch` | 브랜치 관리 | Base Branch 결정 |
| `git rev-list` | 커밋 범위 | 첫 배포 감지 |
| `git shortlog` | 기여자 통계 | 기여자 목록 생성 |

---

## 다음 단계

- [3.1 사용자 정의 Command란?](../03-claude-commands/01-what-is-command.md)
