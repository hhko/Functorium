---
title: "첫 릴리스 노트 생성"
---

지금까지 릴리스 노트 자동화의 아키텍처, 5-Phase 워크플로우, 그리고 각 스크립트의 역할을 살펴봤습니다. 이제 실제로 명령어를 실행해서 릴리스 노트를 처음부터 끝까지 생성해보겠습니다.

하나의 명령어를 입력하면 환경 검증부터 문서 작성까지 전체 파이프라인이 자동으로 흘러갑니다. 각 Phase가 실행될 때 화면에 무엇이 나타나는지, 그 뒤에서 어떤 일이 벌어지는지를 함께 확인하면서 진행하겠습니다.

## 사전 준비

실습을 시작하기 전에 세 가지 환경 요소를 확인해야 합니다. .NET 10 SDK가 설치되어 있어야 스크립트를 실행할 수 있고, Git 저장소 안에 있어야 커밋 히스토리를 분석할 수 있으며, Claude Code가 실행 가능한 상태여야 합니다.

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

스크립트 디렉터리도 확인해봅시다. `.release-notes/scripts/` 폴더에 분석 스크립트들이 있어야 Phase 2에서 데이터를 수집할 수 있습니다.

```bash
# 필수 폴더 확인
ls .release-notes/scripts/
# 출력: AnalyzeAllComponents.cs, ExtractApiChanges.cs, ...

ls .release-notes/
# 출력: TEMPLATE.md, scripts/
```

## Step 1: 명령어 실행

준비가 끝났으면 Claude Code 대화형 모드에서 명령어를 실행합니다.

```bash
> /release-note v1.0.0
```

버전 문자열은 SemVer 형식을 따릅니다. 정규 릴리스뿐 아니라 프리릴리스 태그도 사용할 수 있습니다.

| 버전 | 설명 |
|------|------|
| `v1.0.0` | 정규 릴리스 |
| `v1.0.0-alpha.1` | 알파 릴리스 |
| `v1.0.0-beta.2` | 베타 릴리스 |
| `v1.0.0-rc.1` | 릴리스 후보 |

명령어를 실행하는 순간, Claude는 5-Phase 워크플로우를 순서대로 진행하기 시작합니다. 각 Phase에서 어떤 출력이 나타나는지 살펴보겠습니다.

## Step 2: Phase 1 - 환경 검증

가장 먼저 Claude는 Git 저장소가 존재하는지, .NET SDK가 설치되어 있는지, 스크립트 디렉터리가 있는지 확인합니다. 모든 전제조건이 충족되면 비교 범위(Base와 Target)를 결정합니다.

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

여기서 주목할 부분은 **비교 범위입니다.** 첫 배포인지, 후속 배포인지에 따라 Base가 달라집니다.

- **첫 배포** (release 브랜치 없음): Base가 초기 커밋으로 설정되어 전체 히스토리를 분석합니다.
- **후속 배포** (release 브랜치 있음): Base가 `origin/release/1.0`처럼 이전 릴리스 시점으로 설정되어, 그 이후의 변경사항만 분석합니다.

## Step 3: Phase 2 - 데이터 수집

환경이 검증되면 C# 스크립트가 실행됩니다. `AnalyzeAllComponents.cs`가 각 컴포넌트의 변경 파일과 커밋을 수집하고, `ExtractApiChanges.cs`가 Public API의 변경사항을 추출합니다.

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

이 Phase가 끝나면 `.analysis-output/` 폴더에 분석 결과가 저장됩니다. 실제 파일이 생성되었는지 확인해볼 수 있습니다.

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

## Step 4: Phase 3 - 커밋 분석

수집된 데이터를 기반으로 Claude가 커밋을 분석하고 분류합니다. Breaking Changes가 있는지, 새로운 기능은 몇 개인지, 버그 수정은 몇 건인지 파악하고, 관련 커밋들을 기능 단위로 그룹화합니다.

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

## Step 5: Phase 4 - 문서 작성

분석이 완료되면 릴리스 노트를 작성합니다. TEMPLATE.md를 기반으로 개요, Breaking Changes, 새로운 기능, 버그 수정, API 변경사항, 설치 가이드 순으로 각 섹션을 채워나갑니다.

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

## Step 6: Phase 5 - 검증

마지막으로 생성된 문서의 품질을 검증합니다. 릴리스 노트에 언급된 API가 실제 Uber 파일과 일치하는지, Breaking Changes가 빠지지 않았는지, Markdown 포맷이 올바른지, 모든 주요 기능에 "Why this matters" 섹션이 포함되었는지 확인합니다.

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

## Step 7: 결과 확인

5개 Phase가 모두 완료되면 최종 요약이 표시됩니다. 분석 대상 컴포넌트별 통계와 함께 생성된 파일의 경로가 나타납니다.

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
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

첫 번째 릴리스 노트가 생성되었습니다. 이제 결과물을 열어서 내용을 확인해봅시다.

```bash
# 릴리스 노트 확인
cat .release-notes/RELEASE-v1.0.0.md

# 또는 편집기로 열기
code .release-notes/RELEASE-v1.0.0.md
```

## Step 8: 수동 검토 및 수정

자동 생성된 릴리스 노트가 항상 완벽한 것은 아닙니다. 사람의 눈으로 검토하고 필요한 부분을 보완하는 단계가 중요합니다. 다음 항목들을 하나씩 확인해봅시다.

- [ ] 프론트매터 (title, description, date) 정확한가?
- [ ] 개요가 이 버전의 목표를 잘 설명하는가?
- [ ] Breaking Changes가 정확한가?
- [ ] 모든 주요 기능이 포함되었는가?
- [ ] "Why this matters" 섹션이 모든 기능에 있는가?
- [ ] 코드 샘플이 올바른가?

특히 개요 섹션은 자동 생성된 문장이 다소 평이할 수 있습니다. 프로젝트의 맥락을 아는 사람이 직접 다듬으면 훨씬 나은 결과물이 됩니다.

````markdown
## 개요

<!-- 수정 전 -->
Functorium v1.0.0은 첫 번째 릴리스입니다.

<!-- 수정 후 -->
Functorium v1.0.0은 .NET 애플리케이션을 위한 함수형 프로그래밍 도구 모음의
첫 번째 정식 릴리스입니다. 이 버전에서는 오류 처리, 관측성, 테스트 지원에
중점을 두었습니다.
````

## Step 9: Git 커밋

검토와 수정이 끝나면 결과물을 Git에 커밋합니다. 릴리스 노트 파일은 반드시 커밋하고, 분석 결과 파일은 필요에 따라 함께 저장할 수 있습니다.

```bash
# 릴리스 노트 커밋
git add .release-notes/RELEASE-v1.0.0.md
git commit -m "docs(release): v1.0.0 릴리스 노트 추가"

# 분석 결과도 함께 커밋 (선택적)
git add .release-notes/scripts/.analysis-output/
git commit -m "chore(release): v1.0.0 분석 결과 저장"
```

## 문제 해결

실습 중에 문제가 발생할 수 있습니다. 가장 흔한 세 가지 상황과 해결 방법을 정리합니다.

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

NuGet 패키지 캐시가 손상되었거나 이전 실행의 출력 파일이 잠겨 있을 때 발생합니다.

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

첫 배포에서는 release 브랜치가 아직 없으므로 이 메시지가 나타날 수 있습니다. 명령어가 자동으로 초기 커밋부터 분석하도록 조정되지만, 수동으로 실행해야 할 경우 다음과 같이 합니다.

**해결:**
```bash
cd .release-notes/scripts
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

## 실습 완료

## FAQ

### Q1: `/release-note` 명령어 실행 중 Phase 하나가 실패하면 어떻게 되나요?
**A**: 워크플로우는 순차적으로 진행되므로, **실패한 Phase에서 멈추고 오류 메시지를 표시합니다.** 이전 Phase의 출력 파일은 그대로 보존되므로, 문제를 수정한 뒤 명령어를 다시 실행하면 됩니다. Phase 2의 데이터 수집이 이미 완료된 상태라면 `.analysis-output/` 폴더를 삭제하지 않는 한 재수집하지 않습니다.

### Q2: 첫 배포와 후속 배포에서 `/release-note` 명령어의 사용법이 다른가요?
**A**: 명령어 자체는 동일합니다(`/release-note v1.0.0`). 차이는 **Base Branch 결정 로직에** 있습니다. `origin/release/1.0` 같은 이전 릴리스 브랜치가 존재하면 해당 시점부터, 없으면 초기 커밋부터 분석합니다. 사용자가 별도로 분기 처리할 필요는 없습니다.

### Q3: 자동 생성된 릴리스 노트에서 수동으로 반드시 확인해야 할 부분은 무엇인가요?
**A**: 세 가지를 중점적으로 확인하세요. 첫째, **개요 섹션이** 이번 릴리스의 맥락과 목표를 정확히 전달하는지 확인합니다. 둘째, **"Why this matters" 섹션이** 각 기능의 실질적 가치를 잘 설명하는지 검토합니다. 셋째, **코드 샘플이** 실제 사용 시나리오에 맞는지 확인합니다. API 정확성은 Phase 5에서 자동 검증되지만, 맥락과 가치 전달은 사람의 판단이 필요합니다.

### Q4: 분석 결과 파일(`.analysis-output/`)도 함께 커밋하는 것이 좋은가요?
**A**: 프로젝트 정책에 따라 다릅니다. 커밋하면 **릴리스 노트가 어떤 데이터를 기반으로 작성되었는지 추적할 수** 있어 감사(Audit) 목적에 유용합니다. 반면 매번 재생성 가능한 파일이므로, 저장소 크기를 줄이고 싶다면 `.gitignore`에 추가해도 됩니다.

첫 번째 릴리스 노트를 성공적으로 생성하고, 검토하고, 커밋까지 마쳤습니다. 하나의 명령어로 시작된 5-Phase 워크플로우가 환경 검증부터 최종 검증까지 자동으로 진행되는 과정을 직접 확인했습니다. 다음 절에서는 이 시스템의 기반이 되는 .NET 10 File-based App을 직접 작성해봅니다.

- [나만의 스크립트 작성](02-custom-script.md)
