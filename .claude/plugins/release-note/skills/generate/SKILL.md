---
name: generate
description: "Functorium 릴리스 노트를 자동으로 생성합니다. 환경 검증 → 데이터 수집 → 커밋 분석 → 릴리스 노트 작성 → 검증까지 5단계 워크플로를 실행합니다. '릴리스 노트 생성', 'release note 만들어줘', '릴리스 노트 작성', '새 버전 릴리스' 등의 요청에 반응합니다."
---

## 버전 파라미터

사용자에게 버전을 확인합니다. 지정되지 않으면 요청합니다.

```
/generate v1.2.0        # 정규 릴리스
/generate v1.0.0        # 첫 배포
/generate v1.0.0-beta.1 # 프리릴리스
```

## 핵심 원칙

1. **정확성 우선**: Uber 파일(`all-api-changes.txt`)에 없는 API는 절대 문서화하지 않음
2. **가치 전달 필수**: 모든 주요 기능에 "Why this matters (왜 중요한가):" 섹션 포함
3. **Breaking Changes 자동 감지**: Git Diff 분석이 커밋 메시지 패턴보다 우선
4. **추적성**: 모든 기능을 커밋 SHA로 추적

## 5단계 워크플로

### Phase 1: 환경 검증

**목표**: 전제조건 확인, Base Branch 결정

```bash
git status                                        # Git 저장소 확인
dotnet --version                                  # .NET 10.x 이상 필요
ls .release-notes/scripts                         # 스크립트 디렉터리 확인
```

**Base Branch 결정**:
1. `git rev-parse --verify origin/release/1.0` — 존재하면 Base로 사용
2. 없으면 (첫 배포): `git rev-list --max-parents=0 HEAD` 사용

**성공 기준**:
- [ ] Git 저장소 확인됨
- [ ] .NET SDK 버전 확인됨
- [ ] Base/Target 결정됨

**상세**: `.release-notes/scripts/docs/phase1-setup.md`

---

### Phase 2: 데이터 수집

**목표**: C# 스크립트로 컴포넌트/API 변경사항 분석

```bash
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base <base-branch> --target HEAD
dotnet ExtractApiChanges.cs
```

**성공 기준**:
- [ ] `.analysis-output/*.md` 파일 생성됨
- [ ] `all-api-changes.txt` Uber 파일 생성됨
- [ ] `api-changes-diff.txt` Git Diff 파일 생성됨

**상세**: `.release-notes/scripts/docs/phase2-collection.md`

---

### Phase 3: 커밋 분석

**목표**: 수집된 데이터를 분석하여 릴리스 노트용 기능 추출

**입력 파일**:
- `.analysis-output/Functorium.md`
- `.analysis-output/Functorium.Testing.md`
- `.analysis-output/api-changes-build-current/api-changes-diff.txt`

**Breaking Changes 감지** (두 가지 방법):
1. **Git Diff 분석 (우선)**: `api-changes-diff.txt`에서 삭제/변경된 API 감지
2. **커밋 메시지 패턴 (보조)**: `!:`, `breaking`, `BREAKING` 패턴

**GitHub 이슈/PR 참조가 있는 커밋**:
- GitHub API로 PR/Issue 세부 정보 조회
- 문제 설명, 구현 세부사항, 사용자 영향 추출

**중간 결과 저장** (필수):
```
.analysis-output/work/phase3-commit-analysis.md   # Breaking Changes, Feature/Fix 커밋 목록
.analysis-output/work/phase3-feature-groups.md     # 기능별 그룹화 결과
```

**성공 기준**:
- [ ] Breaking Changes 식별됨
- [ ] Feature Commits 분류됨
- [ ] 기능 그룹화 완료됨
- [ ] 중간 결과 저장됨

**상세**: `.release-notes/scripts/docs/phase3-analysis.md`

---

### Phase 4: 릴리스 노트 작성

**목표**: 분석 결과를 바탕으로 릴리스 노트 작성

**템플릿**: `.release-notes/TEMPLATE.md`
**출력**: `.release-notes/RELEASE-{VERSION}.md`

**작성 절차**:
1. `TEMPLATE.md`를 `RELEASE-{VERSION}.md`로 복사
2. `{VERSION}`, `{DATE}` 등 placeholder 교체
3. Phase 3 분석 결과를 바탕으로 각 섹션 작성
4. 모든 코드 예제를 Uber 파일에서 검증
5. 템플릿 가이드 주석 삭제

**API 검증**:
```bash
grep -n "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

**필수 섹션 구조**:

| 섹션 | 필수 | 설명 |
|------|:----:|------|
| 프론트매터 | O | YAML header (title, description, date) |
| 개요 | O | 버전 소개, 주요 변경 요약 |
| Breaking Changes | O | API 호환성 파괴 (없으면 "없음" 명시) |
| 새로운 기능 | O | feat 커밋 기반, 코드 예제 + "Why this matters" |
| 버그 수정 | - | fix 커밋 기반 (없으면 섹션 삭제) |
| API 변경사항 | O | 새로운/수정된 public API 요약 |
| 설치 | O | NuGet 패키지 설치 방법 |

**기능 문서화 구조** (각 기능마다):
```markdown
#### {N}. {기능명}

{기능 설명 — What: 무엇을 하는가?}

\`\`\`csharp
{코드 예제 — How: 어떻게 사용하는가?}
\`\`\`

**Why this matters (왜 중요한가):**
- {해결하는 문제}
- {개발자 생산성}
- {코드 품질 향상}
- {정량적 이점 (가능한 경우)}

<!-- 관련 커밋: {SHA} {커밋 메시지} -->
```

**중간 결과 저장** (필수):
```
.analysis-output/work/phase4-draft.md              # 릴리스 노트 초안
.analysis-output/work/phase4-api-references.md     # 사용된 API 목록 및 검증 결과
```

**성공 기준**:
- [ ] 프론트매터 포함됨
- [ ] 모든 필수 섹션 포함됨
- [ ] 모든 주요 기능에 "Why this matters" 섹션 포함됨
- [ ] 모든 코드 예제가 Uber 파일에서 검증됨

**상세**: `.release-notes/scripts/docs/phase4-writing.md`

---

### Phase 5: 검증

**목표**: 생성된 릴리스 노트의 품질 및 정확성 검증

**검증 항목**:

| 항목 | 검증 방법 |
|------|----------|
| 프론트매터 | `head -5 .release-notes/RELEASE-{VERSION}.md` |
| "Why this matters" | `grep -c "Why this matters" RELEASE-{VERSION}.md` |
| Breaking Changes | `api-changes-diff.txt`와 대조 |
| API 정확성 | 코드 예제의 API가 Uber 파일에 존재 |
| GitHub Release 크기 | `validate-release-notes.ps1` (125,000자 제한) |

**검증 명령**:
```bash
# 프론트매터 확인
head -5 .release-notes/RELEASE-{VERSION}.md

# "Why this matters" 섹션 존재 확인
grep -c "Why this matters" .release-notes/RELEASE-{VERSION}.md

# Breaking Changes Git Diff 확인
grep "^-.*public" .analysis-output/api-changes-build-current/api-changes-diff.txt

# GitHub Release 크기 검증
powershell.exe -File .release-notes/validate-release-notes.ps1 -FilePath ".release-notes/RELEASE-{VERSION}.md"

# Markdown 검증 (선택)
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-{VERSION}.md --disable MD013
```

**중간 결과 저장** (필수):
```
.analysis-output/work/phase5-validation-report.md  # 검증 결과 요약
.analysis-output/work/phase5-api-validation.md     # API 검증 상세
```

**성공 기준**:
- [ ] 프론트매터 포함됨
- [ ] 모든 필수 섹션 포함됨
- [ ] 모든 주요 기능에 "Why this matters" 섹션 포함됨
- [ ] Uber 파일에 없는 API 사용: 0개
- [ ] Git Diff에서 감지된 모든 Breaking Changes 문서화됨
- [ ] 각 Breaking Change에 마이그레이션 가이드 포함됨
- [ ] GitHub Release 크기 제한(125,000자) 미만

**상세**: `.release-notes/scripts/docs/phase5-validation.md`

---

## 완료 메시지

릴리스 노트 생성 완료 시 다음 형식으로 표시합니다:

```
릴리스 노트 생성 완료

버전: {VERSION}
파일: .release-notes/RELEASE-{VERSION}.md

| 항목 | 값 |
|------|-----|
| Functorium | [N files, N commits] |
| Functorium.Testing | [N files, N commits] |
| 릴리스 노트 | [N lines] |
| Breaking Changes | [N개] |

생성된 파일:
- .release-notes/RELEASE-{VERSION}.md
- .analysis-output/Functorium.md
- .analysis-output/Functorium.Testing.md
- .analysis-output/api-changes-build-current/all-api-changes.txt
- .analysis-output/work/phase3-*.md
- .analysis-output/work/phase4-*.md
- .analysis-output/work/phase5-*.md

다음 단계:
1. 생성된 릴리스 노트 검토
2. 필요시 수동 수정
3. Git에 커밋
4. GitHub Release 생성 (선택적)
```

## 트러블슈팅

| 문제 | 해결 방법 |
|------|----------|
| Base Branch 없음 | 첫 배포로 자동 감지, 초기 커밋부터 분석 |
| .NET SDK 버전 오류 | .NET 10.x 설치 필요 |
| 파일 잠금 문제 | `taskkill /F /IM dotnet.exe` (Windows) |
| API 검증 실패 | Uber 파일에서 올바른 API 이름 확인 |
| runfile 캐시 오류 | `./Build-CleanRunFileCache.ps1` 실행 |

### 전체 초기화 (Windows)

```powershell
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .release-notes\scripts\.analysis-output -ErrorAction SilentlyContinue
dotnet nuget locals all --clear
```

## 참조 문서

| 문서 | 경로 | 설명 |
|------|------|------|
| 전체 프로세스 개요 | `.release-notes/scripts/docs/README.md` | 5-Phase 워크플로 |
| 릴리스 노트 템플릿 | `.release-notes/TEMPLATE.md` | 복사용 템플릿 |
| Phase 1 상세 | `.release-notes/scripts/docs/phase1-setup.md` | 환경 검증 |
| Phase 2 상세 | `.release-notes/scripts/docs/phase2-collection.md` | 데이터 수집 |
| Phase 3 상세 | `.release-notes/scripts/docs/phase3-analysis.md` | 커밋 분석 |
| Phase 4 상세 | `.release-notes/scripts/docs/phase4-writing.md` | 릴리스 노트 작성 |
| Phase 5 상세 | `.release-notes/scripts/docs/phase5-validation.md` | 검증 기준 |
| 크기 검증 스크립트 | `.release-notes/validate-release-notes.ps1` | GitHub Release 크기 제한 |
