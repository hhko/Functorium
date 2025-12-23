---
title: RELEASE-NOTES
description: 릴리스 노트를 자동으로 생성합니다 (데이터 수집, 분석, 작성, 검증).
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"
---

# 릴리스 노트 자동 생성 규칙

Functorium 프로젝트의 전문적이고 정확한 릴리스 노트를 자동으로 생성합니다.

## 버전 파라미터 (`$ARGUMENTS`)

**버전이 지정된 경우:** $ARGUMENTS

버전 파라미터는 필수입니다. 생성할 릴리스 노트의 버전을 지정하십시오.

**사용 예시:**
```
/release-notes v1.2.0        # 정규 릴리스
/release-notes v1.0.0        # 첫 배포
/release-notes v1.2.0-beta.1 # 프리릴리스
```

**버전이 지정되지 않은 경우:** 오류 메시지를 출력하고 중단합니다.

## 자동화 워크플로우 개요

이 명령은 5단계로 구성된 완전 자동화 프로세스를 실행합니다:

| Phase | 목표 | 상세 문서 |
|-------|------|----------|
| **1. 환경 검증** | 전제조건 확인, Base Branch 결정 | [phase1-setup.md](.release-notes/scripts/docs/phase1-setup.md) |
| **2. 데이터 수집** | 컴포넌트/API 변경사항 분석 | [phase2-collection.md](.release-notes/scripts/docs/phase2-collection.md) |
| **3. 커밋 분석** | 기능 추출, Breaking Changes 감지 | [phase3-analysis.md](.release-notes/scripts/docs/phase3-analysis.md) |
| **4. 문서 작성** | 릴리스 노트 작성 | [phase4-writing.md](.release-notes/scripts/docs/phase4-writing.md) |
| **5. 검증** | 품질 및 정확성 검증 | [phase5-validation.md](.release-notes/scripts/docs/phase5-validation.md) |

---

## Phase 1: 환경 검증 및 준비

**목표**: 릴리스 노트 생성 전 필수 환경 검증

**전제조건 확인**:
```bash
git status              # Git 저장소 확인
dotnet --version        # .NET 10.x 이상 필요
ls .release-notes/scripts  # 스크립트 디렉터리 확인
```

**Base Branch 결정**:
- `origin/release/1.0` 존재 시: Base로 사용
- 없으면 (첫 배포): `git rev-list --max-parents=0 HEAD` 사용

**성공 기준**:
- [ ] Git 저장소 확인됨
- [ ] .NET SDK 버전 확인됨
- [ ] Base/Target 결정됨

**상세**: [phase1-setup.md](.release-notes/scripts/docs/phase1-setup.md)

---

## Phase 2: 데이터 수집

**목표**: C# 스크립트로 컴포넌트/API 변경사항 분석

**작업 디렉터리 변경**:
```bash
cd .release-notes/scripts
```

**핵심 명령**:
```bash
# 1. 컴포넌트 분석
dotnet AnalyzeAllComponents.cs --base <base-branch> --target HEAD

# 2. API 변경사항 추출
dotnet ExtractApiChanges.cs
```

**성공 기준**:
- [ ] `.analysis-output/*.md` 파일 생성됨
- [ ] `all-api-changes.txt` Uber 파일 생성됨
- [ ] `api-changes-diff.txt` Git Diff 파일 생성됨

**상세**: [phase2-collection.md](.release-notes/scripts/docs/phase2-collection.md)

---

## Phase 3: 커밋 분석 및 기능 추출

**목표**: 수집된 데이터를 분석하여 릴리스 노트용 기능 추출

**입력 파일**:
- `.analysis-output/Functorium.md`
- `.analysis-output/Functorium.Testing.md`
- `.analysis-output/api-changes-build-current/api-changes-diff.txt`

**Breaking Changes 감지** (두 가지 방법):
1. **Git Diff 분석 (권장)**: `api-changes-diff.txt`에서 삭제/변경된 API 감지
2. **커밋 메시지 패턴**: `!:`, `breaking`, `BREAKING` 패턴

**성공 기준**:
- [ ] Breaking Changes 식별됨
- [ ] Feature Commits 분류됨
- [ ] 기능 그룹화 완료됨
- [ ] 중간 결과 저장됨

**중간 결과 저장** (필수):
- `.analysis-output/work/phase3-commit-analysis.md` - Breaking Changes, Feature/Fix 커밋 목록
- `.analysis-output/work/phase3-feature-groups.md` - 기능별 그룹화 결과

**상세**: [phase3-analysis.md](.release-notes/scripts/docs/phase3-analysis.md)

---

## Phase 4: 릴리스 노트 작성

**목표**: 분석 결과를 바탕으로 전문적인 릴리스 노트 작성

**템플릿 파일**: `.release-notes/TEMPLATE.md`
**출력 파일**: `.release-notes/RELEASE-$ARGUMENTS.md`

### 작성 절차

1. **템플릿 복사**: `.release-notes/TEMPLATE.md`를 `RELEASE-$ARGUMENTS.md`로 복사
2. **placeholder 교체**: `{VERSION}`, `{DATE}` 등을 실제 값으로 교체
3. **섹션 채우기**: Phase 3 분석 결과를 바탕으로 각 섹션 작성
4. **API 검증**: 모든 코드 샘플을 Uber 파일에서 검증
5. **주석 정리**: 템플릿 가이드 주석 삭제

### 작성 원칙 (필수 준수)

1. **정확성 우선**: Uber 파일에 없는 API는 절대 문서화하지 않음
2. **코드 샘플 필수**: 모든 주요 기능에 실행 가능한 코드 샘플 포함
3. **추적성**: 커밋 SHA를 주석으로 포함 (`<!-- 관련 커밋: SHA -->`)
4. **가치 전달 필수**: 모든 주요 기능에 **"Why this matters (왜 중요한가):"** 섹션 포함

> **중요**: "Why this matters" 섹션이 없는 기능 문서화는 불완전한 것으로 간주됩니다.

### API 검증

코드 샘플 작성 전 반드시 Uber 파일에서 API 존재 여부를 확인합니다:

```bash
grep -n "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 성공 기준

- [ ] 프론트매터 포함됨
- [ ] 모든 필수 섹션 포함됨
- [ ] 모든 주요 기능에 "Why this matters" 섹션 포함됨
- [ ] 모든 코드 샘플이 Uber 파일에서 검증됨
- [ ] 중간 결과 저장됨

### 중간 결과 저장 (필수)

- `.analysis-output/work/phase4-draft.md` - 릴리스 노트 초안
- `.analysis-output/work/phase4-api-references.md` - 사용된 API 목록 및 검증 결과

**상세**: [phase4-writing.md](.release-notes/scripts/docs/phase4-writing.md)

---

## Phase 5: 검증

**목표**: 생성된 릴리스 노트의 품질 및 정확성 검증

**검증 항목**:
1. **프론트매터 존재**: YAML 프론트매터 포함 여부
2. **필수 섹션 존재**: 개요, Breaking Changes, 새로운 기능, 설치
3. **"Why this matters" 섹션 존재**: 모든 주요 기능에 가치 설명 포함
4. **API 정확성**: 모든 코드 샘플이 Uber 파일에서 검증됨
5. **Breaking Changes 완전성**: Git Diff 결과와 대조

**검증 명령**:
```bash
# 프론트매터 확인
head -5 .release-notes/RELEASE-$ARGUMENTS.md

# "Why this matters" 섹션 존재 확인
grep -c "**Why this matters (왜 중요한가):**" .release-notes/RELEASE-$ARGUMENTS.md

# Breaking Changes Git Diff 확인
cat .analysis-output/api-changes-build-current/api-changes-diff.txt
grep "^-.*public" api-changes-diff.txt

# Markdown 검증 (선택적)
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-$ARGUMENTS.md --disable MD013
```

**통과 기준**:
- [ ] 프론트매터 포함됨
- [ ] 모든 필수 섹션 포함됨
- [ ] 모든 주요 기능에 "Why this matters" 섹션 포함됨
- [ ] Uber 파일에 없는 API 사용: 0개
- [ ] Git Diff에서 감지된 모든 Breaking Changes 문서화됨
- [ ] 각 Breaking Change에 마이그레이션 가이드 포함
- [ ] 검증 결과 저장됨

**중간 결과 저장** (필수):
- `.analysis-output/work/phase5-validation-report.md` - 검증 결과 요약
- `.analysis-output/work/phase5-api-validation.md` - API 검증 상세 (검증한 API 목록)

**상세**: [phase5-validation.md](.release-notes/scripts/docs/phase5-validation.md)

---

## 완료 메시지 (필수)

릴리스 노트 생성 완료 시 **반드시 다음 형식으로 표시**합니다:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
릴리스 노트 생성 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

버전: $ARGUMENTS
파일: .release-notes/RELEASE-$ARGUMENTS.md

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

통계 요약
| 항목 | 값 |
|------|-----|
| Functorium | [N files, N commits] |
| Functorium.Testing | [N files, N commits] |
| 릴리스 노트 | [N lines] |
| Breaking Changes | [N개] |

생성된 파일
- .release-notes/RELEASE-$ARGUMENTS.md (릴리스 노트)
- .analysis-output/Functorium.md (컴포넌트 분석)
- .analysis-output/Functorium.Testing.md (컴포넌트 분석)
- .analysis-output/api-changes-build-current/all-api-changes.txt (Uber 파일)
- .analysis-output/work/phase3-*.md (중간 결과)
- .analysis-output/work/phase4-*.md (중간 결과)
- .analysis-output/work/phase5-*.md (검증 결과)

다음 단계
1. 생성된 릴리스 노트 검토
2. 필요시 수동 수정
3. Git에 커밋
4. GitHub Release 생성 (선택적)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

> **중요**: 이 출력 형식은 필수입니다. 각 Phase별 상세 문서의 콘솔 출력 형식도 준수해야 합니다.

---

## 핵심 원칙

### 1. 정확성 우선

> **Uber 파일에 없는 API는 절대 문서화하지 않습니다.**

- 모든 API를 Uber 파일에서 검증
- 매개변수 이름 및 타입 정확히 일치
- 추측 금지, 검증된 정보만 사용

### 2. 가치 전달 필수

> **모든 주요 기능에 "Why this matters" 섹션을 포함합니다.**

- 단순 사실 나열("~를 제공합니다")을 넘어 가치 명시
- 해결하는 문제, 개발자 생산성, 코드 품질 향상 설명
- 가능한 경우 정량적 이점 포함 (50줄 → 5줄)
- "Why this matters" 섹션이 없는 기능 문서화는 불완전

### 3. Breaking Changes는 Git Diff로 자동 감지

> **Git Diff 분석이 커밋 메시지 패턴보다 더 정확합니다.**

- `.api` 폴더의 Git diff 분석 우선 (객관적)
- 커밋 메시지 패턴은 보조 수단 (주관적)
- 삭제/변경된 API는 모두 Breaking Change로 처리
- 개발자 실수 방지 (자동 감지)

### 4. 추적성

- 모든 기능을 실제 커밋으로 추적
- GitHub 이슈/PR 링크 포함 (가능한 경우)
- 커밋 SHA 참조

---

## 참고 문서

| Phase | 문서 | 설명 |
|-------|------|------|
| - | [README.md](.release-notes/scripts/docs/README.md) | 전체 프로세스 개요 |
| - | [TEMPLATE.md](.release-notes/TEMPLATE.md) | 릴리스 노트 템플릿 (복사용) |
| 1 | [phase1-setup.md](.release-notes/scripts/docs/phase1-setup.md) | 환경 검증 및 준비 |
| 2 | [phase2-collection.md](.release-notes/scripts/docs/phase2-collection.md) | 데이터 수집 |
| 3 | [phase3-analysis.md](.release-notes/scripts/docs/phase3-analysis.md) | 커밋 분석 방법론 |
| 4 | [phase4-writing.md](.release-notes/scripts/docs/phase4-writing.md) | 릴리스 노트 작성 규칙 |
| 5 | [phase5-validation.md](.release-notes/scripts/docs/phase5-validation.md) | 검증 기준 |

---

## 트러블슈팅

### 일반적인 문제 해결

| 문제 | 해결 방법 |
|------|----------|
| Base Branch 없음 | 첫 배포로 자동 감지, 초기 커밋부터 분석 |
| .NET SDK 버전 오류 | .NET 10.x 설치 필요 |
| 파일 잠금 문제 | `taskkill /F /IM dotnet.exe` (Windows) |
| API 검증 실패 | Uber 파일에서 올바른 API 이름 확인 |

### 전체 초기화 (Windows)

```powershell
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .release-notes\scripts\.analysis-output -ErrorAction SilentlyContinue
dotnet nuget locals all --clear
```

**상세**: [README.md#트러블슈팅](.release-notes/scripts/docs/README.md#트러블슈팅)
