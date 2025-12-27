# 7.1 첫 번째 릴리스 노트 생성

> 이 절에서는 릴리스 노트 자동화 시스템을 처음부터 끝까지 실행해봅니다.

---

## 실습 목표

이 실습을 완료하면:

- `/release-note` 명령어 실행 방법 이해
- 5-Phase 워크플로우 전체 흐름 체험
- 생성된 릴리스 노트 검토 및 수정

---

## 사전 준비

### 환경 확인

```bash
# .NET 10 설치 확인
dotnet --version
# 출력: 10.0.100 이상

# Git 저장소 확인
git status
# 출력: On branch main

# Claude Code 실행
claude
```

### 프로젝트 구조 확인

```bash
# 필수 폴더 확인
ls .release-notes/scripts/
# 출력: AnalyzeAllComponents.cs, ExtractApiChanges.cs, ...

ls .release-notes/
# 출력: TEMPLATE.md, scripts/
```

---

## Step 1: 명령어 실행

Claude Code 대화형 모드에서 명령어를 실행합니다:

```bash
> /release-note v1.0.0
```

### 버전 형식 예시

| 버전 | 설명 |
|------|------|
| `v1.0.0` | 정규 릴리스 |
| `v1.0.0-alpha.1` | 알파 릴리스 |
| `v1.0.0-beta.2` | 베타 릴리스 |
| `v1.0.0-rc.1` | 릴리스 후보 |

---

## Step 2: Phase 1 - 환경 검증

Claude가 환경을 검증합니다:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: 환경 검증
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

전제조건:
  Git 저장소
  .NET SDK 10.x
  스크립트 디렉터리

비교 범위:
  Base: abc1234 (초기 커밋)
  Target: HEAD
  버전: v1.0.0
```

### 첫 배포 vs 후속 배포

**첫 배포** (release 브랜치 없음):
- Base: 초기 커밋 (전체 히스토리 분석)

**후속 배포** (release 브랜치 있음):
- Base: origin/release/1.0 (이전 릴리스 이후만 분석)

---

## Step 3: Phase 2 - 데이터 수집

C# 스크립트가 실행됩니다:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 2: 데이터 수집
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

컴포넌트 분석 중...
  Functorium.md (31 files, 19 commits)
  Functorium.Testing.md (18 files, 13 commits)

API 추출 중...
  all-api-changes.txt (Uber 파일)
  api-changes-diff.txt (Git Diff)
```

### 생성된 파일 확인

```bash
# 컴포넌트 분석 파일
ls .release-notes/scripts/.analysis-output/
# Functorium.md
# Functorium.Testing.md
# analysis-summary.md

# API 파일
ls .release-notes/scripts/.analysis-output/api-changes-build-current/
# all-api-changes.txt
# api-changes-diff.txt
```

---

## Step 4: Phase 3 - 커밋 분석

Claude가 커밋을 분석하고 분류합니다:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 3: 커밋 분석
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

분석 결과:
  Breaking Changes: 0개
  Feature Commits: 6개
  Bug Fixes: 1개
  기능 그룹: 8개

식별된 주요 기능:
  1. 함수형 오류 처리
  2. OpenTelemetry 통합
  3. 테스트 픽스처
  ...
```

---

## Step 5: Phase 4 - 문서 작성

릴리스 노트가 생성됩니다:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 4: 문서 작성
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

작성 중...
  개요 섹션
  Breaking Changes
  새로운 기능 (8개)
  버그 수정 (1개)
  API 변경사항
  설치 가이드

출력 파일:
  .release-notes/RELEASE-v1.0.0.md
```

---

## Step 6: Phase 5 - 검증

생성된 문서를 검증합니다:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: 검증
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

검증 항목:
  API 정확성 - 통과
  Breaking Changes - 통과
  Markdown 포맷 - 통과
  Why this matters 섹션 - 통과

상태: 게시 가능
```

---

## Step 7: 결과 확인

### 완료 메시지

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
릴리스 노트 생성 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

버전: v1.0.0
파일: .release-notes/RELEASE-v1.0.0.md

통계 요약
| 항목 | 값 |
|------|-----|
| Functorium | 31 files, 19 commits |
| Functorium.Testing | 18 files, 13 commits |
| Breaking Changes | 0개 |

다음 단계
1. 생성된 릴리스 노트 검토
2. 필요시 수동 수정
3. Git에 커밋
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 생성된 파일 열기

```bash
# 릴리스 노트 확인
cat .release-notes/RELEASE-v1.0.0.md

# 또는 편집기로 열기
code .release-notes/RELEASE-v1.0.0.md
```

---

## Step 8: 수동 검토 및 수정

생성된 릴리스 노트를 검토합니다:

### 확인 항목

- [ ] 프론트매터 (title, description, date) 정확한가?
- [ ] 개요가 이 버전의 목표를 잘 설명하는가?
- [ ] Breaking Changes가 정확한가?
- [ ] 모든 주요 기능이 포함되었는가?
- [ ] "Why this matters" 섹션이 모든 기능에 있는가?
- [ ] 코드 샘플이 올바른가?

### 수정 예시

필요시 내용을 보완합니다:

````markdown
## 개요

<!-- 수정 전 -->
Functorium v1.0.0은 첫 번째 릴리스입니다.

<!-- 수정 후 -->
Functorium v1.0.0은 .NET 애플리케이션을 위한 함수형 프로그래밍 도구 모음의
첫 번째 정식 릴리스입니다. 이 버전에서는 오류 처리, 관측성, 테스트 지원에
중점을 두었습니다.
````

---

## Step 9: Git 커밋

검토가 완료되면 커밋합니다:

```bash
# 릴리스 노트 커밋
git add .release-notes/RELEASE-v1.0.0.md
git commit -m "docs(release): v1.0.0 릴리스 노트 추가"

# 분석 결과도 함께 커밋 (선택적)
git add .release-notes/scripts/.analysis-output/
git commit -m "chore(release): v1.0.0 분석 결과 저장"
```

---

## 문제 해결

### 환경 검증 실패

```txt
오류: .NET 10 SDK가 필요합니다
```

**해결:**
```bash
# .NET 10 설치
# https://dotnet.microsoft.com/download/dotnet/10.0
```

### 스크립트 실행 실패

```txt
스크립트 실행 실패: AnalyzeAllComponents.cs
```

**해결:**
```bash
cd .release-notes/scripts

# NuGet 캐시 정리
dotnet nuget locals all --clear

# 출력 폴더 삭제 후 재시도
rm -rf .analysis-output
```

### Base Branch 없음

```txt
Base branch origin/release/1.0 does not exist
```

**해결 (첫 배포):**
```bash
# 초기 커밋부터 분석하도록 명령어가 자동 조정됨
# 또는 수동으로 실행:
cd .release-notes/scripts
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

---

## 실습 완료

축하합니다! 첫 번째 릴리스 노트를 성공적으로 생성했습니다.

### 배운 내용

- `/release-note` 명령어 사용법
- 5-Phase 워크플로우 실행 흐름
- 생성된 파일 구조
- 검토 및 수정 방법
- Git 커밋 절차

---

## 다음 단계

- [7.2 나만의 스크립트 작성](02-custom-script.md)
