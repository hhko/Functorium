# 3.3 release-note.md 상세 분석

> 이 절에서는 릴리스 노트 자동화의 핵심인 `release-note.md` Command 파일을 상세히 분석합니다.

---

## 파일 위치

```txt
.claude/commands/release-note.md
```

---

## 프론트매터 분석

```yaml
---
title: RELEASE-NOTES
description: 릴리스 노트를 자동으로 생성합니다 (데이터 수집, 분석, 작성, 검증).
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"
---
```

| 필드 | 값 | 설명 |
|------|-----|------|
| title | RELEASE-NOTES | 명령어 표시 이름 |
| description | 릴리스 노트를 자동으로... | 전체 프로세스 요약 |
| argument-hint | `<version>` | 버전 파라미터 필요 |

---

## 전체 구조 개요

release-note.md는 다음과 같은 구조로 되어 있습니다:

```txt
release-note.md
├── 프론트매터
├── 제목 및 개요
├── 버전 파라미터 ($ARGUMENTS)
├── 자동화 워크플로우 개요
├── Phase 1: 환경 검증 및 준비
├── Phase 2: 데이터 수집
├── Phase 3: 커밋 분석 및 기능 추출
├── Phase 4: 릴리스 노트 작성
├── Phase 5: 검증
├── 완료 메시지
├── 핵심 원칙
├── 참고 문서
└── 트러블슈팅
```

---

## 버전 파라미터 처리

````markdown
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
````

### 분석

- `$ARGUMENTS`로 버전 인자 접근
- 버전 미지정 시 오류 처리 명시
- 다양한 버전 형식 예시 제공

---

## 워크플로우 개요 테이블

```markdown
| Phase | 목표 | 상세 문서 |
|-------|------|----------|
| **1. 환경 검증** | 전제조건 확인, Base Branch 결정 | [phase1-setup.md](...) |
| **2. 데이터 수집** | 컴포넌트/API 변경사항 분석 | [phase2-collection.md](...) |
| **3. 커밋 분석** | 기능 추출, Breaking Changes 감지 | [phase3-analysis.md](...) |
| **4. 문서 작성** | 릴리스 노트 작성 | [phase4-writing.md](...) |
| **5. 검증** | 품질 및 정확성 검증 | [phase5-validation.md](...) |
```

### 분석

- 5단계 워크플로우를 테이블로 요약
- 각 Phase별 목표 명시
- 상세 문서 링크 제공 (모듈화)

---

## Phase 1: 환경 검증 분석

````markdown
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
````

### 주요 포인트

1. **구체적인 명령어**: 실행할 bash 명령어 제공
2. **조건부 로직**: Base Branch 자동 결정 규칙
3. **체크리스트**: 성공 기준 명시

---

## Phase 2: 데이터 수집 분석

````markdown
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

# 3. 테스트 결과 요약 (선택적)
dotnet SummarizeSlowestTests.cs
```

**성공 기준**:
- [ ] `.analysis-output/*.md` 파일 생성됨
- [ ] `all-api-changes.txt` Uber 파일 생성됨
- [ ] `api-changes-diff.txt` Git Diff 파일 생성됨
````

### 주요 포인트

1. **실행 순서**: 번호로 순서 명시
2. **구체적인 출력**: 생성될 파일명 명시
3. **선택적 단계**: "선택적" 표시로 필수/선택 구분

---

## Phase 3: 커밋 분석 분석

````markdown
## Phase 3: 커밋 분석 및 기능 추출

**목표**: 수집된 데이터를 분석하여 릴리스 노트용 기능 추출

**입력 파일**:
- `.analysis-output/Functorium.md`
- `.analysis-output/Functorium.Testing.md`
- `.analysis-output/api-changes-build-current/api-changes-diff.txt`

**Breaking Changes 감지** (두 가지 방법):
1. **Git Diff 분석 (권장)**: `api-changes-diff.txt`에서 삭제/변경된 API 감지
2. **커밋 메시지 패턴**: `!:`, `breaking`, `BREAKING` 패턴

**중간 결과 저장** (필수):
- `.analysis-output/work/phase3-commit-analysis.md`
- `.analysis-output/work/phase3-feature-groups.md`
````

### 주요 포인트

1. **입력/출력 명시**: Phase 간 데이터 흐름
2. **우선순위**: "(권장)" 표시로 선호 방법 표시
3. **중간 결과**: 추적 가능한 작업 결과

---

## Phase 4: 문서 작성 분석

````markdown
## Phase 4: 릴리스 노트 작성

**목표**: 분석 결과를 바탕으로 전문적인 릴리스 노트 작성

**템플릿 파일**: `.release-notes/TEMPLATE.md`
**출력 파일**: `.release-notes/RELEASE-$ARGUMENTS.md`

### 작성 원칙 (필수 준수)

1. **정확성 우선**: Uber 파일에 없는 API는 절대 문서화하지 않음
2. **코드 샘플 필수**: 모든 주요 기능에 실행 가능한 코드 샘플 포함
3. **추적성**: 커밋 SHA를 주석으로 포함 (`<!-- 관련 커밋: SHA -->`)
4. **가치 전달 필수**: 모든 주요 기능에 **"Why this matters (왜 중요한가):"** 섹션 포함

> **중요**: "Why this matters" 섹션이 없는 기능 문서화는 불완전한 것으로 간주됩니다.
````

### 주요 포인트

1. **$ARGUMENTS 활용**: 출력 파일명에 버전 포함
2. **강조 구문**: "필수 준수", "중요" 등으로 핵심 규칙 강조
3. **품질 기준**: 불완전의 정의 명시

---

## Phase 5: 검증 분석

````markdown
## Phase 5: 검증

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
grep -c "**Why this matters" .release-notes/RELEASE-$ARGUMENTS.md

# Breaking Changes Git Diff 확인
cat .analysis-output/api-changes-build-current/api-changes-diff.txt
```
````

### 주요 포인트

1. **자동화된 검증**: 명령어로 자동 확인
2. **정량적 기준**: grep -c로 개수 확인
3. **교차 검증**: Git Diff와 문서 대조

---

## 완료 메시지 형식

````markdown
## 완료 메시지 (필수)

릴리스 노트 생성 완료 시 **반드시 다음 형식으로 표시**합니다:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
릴리스 노트 생성 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

버전: $ARGUMENTS
파일: .release-notes/RELEASE-$ARGUMENTS.md

통계 요약
| 항목 | 값 |
|------|-----|
| Functorium | [N files, N commits] |
| Functorium.Testing | [N files, N commits] |
| Breaking Changes | [N개] |

다음 단계
1. 생성된 릴리스 노트 검토
2. 필요시 수동 수정
3. Git에 커밋
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```
````

### 주요 포인트

1. **일관된 형식**: 항상 같은 출력 형식
2. **통계 포함**: 작업 결과 요약
3. **다음 단계 안내**: 사용자 액션 가이드

---

## 핵심 원칙 섹션

```markdown
## 핵심 원칙

### 1. 정확성 우선

> **Uber 파일에 없는 API는 절대 문서화하지 않습니다.**

### 2. 가치 전달 필수

> **모든 주요 기능에 "Why this matters" 섹션을 포함합니다.**

### 3. Breaking Changes는 Git Diff로 자동 감지

> **Git Diff 분석이 커밋 메시지 패턴보다 더 정확합니다.**

### 4. 추적성

- 모든 기능을 실제 커밋으로 추적
- 커밋 SHA 참조
```

### 분석

핵심 원칙을 명확한 규칙으로 정리하여:
- Claude가 우선순위를 이해
- 일관된 품질 유지
- 검증 기준 제공

---

## 설계 패턴 분석

release-note.md에서 사용된 효과적인 설계 패턴:

### 1. 모듈화 (Modularization)

```txt
release-note.md (마스터)
├── phase1-setup.md (상세)
├── phase2-collection.md (상세)
├── phase3-analysis.md (상세)
├── phase4-writing.md (상세)
└── phase5-validation.md (상세)
```

- 핵심 규칙은 마스터 파일에
- 상세 지침은 별도 파일로 분리

### 2. 체크리스트 패턴

```markdown
**성공 기준**:
- [ ] 항목 1
- [ ] 항목 2
- [ ] 항목 3
```

- 진행 상황 추적 용이
- 누락 방지

### 3. 입출력 명시

```markdown
**입력 파일**:
- file1.md
- file2.txt

**출력 파일**:
- output.md
```

- Phase 간 데이터 흐름 명확
- 디버깅 용이

### 4. 조건부 처리

```markdown
**버전이 지정된 경우:** ...
**버전이 지정되지 않은 경우:** ...

**브랜치 존재 시:** ...
**없으면:** ...
```

- 다양한 상황 대응
- 오류 처리 명시

---

## 정리

release-note.md의 핵심 설계:

| 요소 | 목적 |
|------|------|
| 프론트매터 | Command 메타데이터 |
| 워크플로우 테이블 | 전체 프로세스 요약 |
| Phase별 섹션 | 단계별 상세 지침 |
| 성공 기준 | 품질 체크리스트 |
| 핵심 원칙 | 우선순위 및 규칙 |
| 완료 메시지 | 일관된 출력 형식 |
| 문서 참조 | 상세 지침 모듈화 |

---

## 다음 단계

- [3.4 commit.md 소개](04-commit-command.md)
