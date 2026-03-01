# 8.1 Quick Reference

> 릴리스 노트 자동화 명령어와 워크플로우를 빠르게 참조할 수 있는 가이드입니다.

---

## 명령어 사용법

### 기본 사용법

```bash
/release-note <version>
```

### 버전 파라미터 예시

| 명령어 | 설명 |
|--------|------|
| `/release-note v1.0.0` | 첫 배포 |
| `/release-note v1.2.0` | 정규 릴리스 |
| `/release-note v1.2.0-alpha.1` | 알파 릴리스 |
| `/release-note v1.2.0-beta.1` | 베타 릴리스 |
| `/release-note v1.2.0-rc.1` | 릴리스 후보 |

---

## 5-Phase 워크플로우 요약

| Phase | 목표 | 핵심 명령/작업 | 출력 |
|-------|------|---------------|------|
| **1** | 환경 검증 | `git status`, `dotnet --version` | Base/Target 결정 |
| **2** | 데이터 수집 | `dotnet AnalyzeAllComponents.cs` | `.analysis-output/*.md` |
| **3** | 커밋 분석 | Breaking Changes, Feature 분류 | `work/phase3-*.md` |
| **4** | 문서 작성 | TEMPLATE.md → RELEASE-*.md | `RELEASE-{VERSION}.md` |
| **5** | 검증 | API 정확성, "Why this matters" 섹션 확인 | `work/phase5-*.md` |

### Phase 1: 환경 검증

```bash
# 필수 확인
git status              # Git 저장소 확인
dotnet --version        # .NET 10.x 이상 필요
ls .release-notes/scripts  # 스크립트 디렉터리 확인
```

**Base Branch 결정:**
- `origin/release/1.0` 존재 시 → Base로 사용
- 없으면 (첫 배포) → `git rev-list --max-parents=0 HEAD`

### Phase 2: 데이터 수집

```bash
cd .release-notes/scripts

# 컴포넌트 분석
dotnet AnalyzeAllComponents.cs --base <base-branch> --target HEAD

# API 변경사항 추출
dotnet ExtractApiChanges.cs
```

### Phase 3: 커밋 분석

**Breaking Changes 감지 방법:**
1. **Git Diff 분석 (권장)**: `api-changes-diff.txt`에서 삭제/변경된 API
2. **커밋 메시지 패턴**: `타입!:`, `BREAKING CHANGE` 키워드

### Phase 4: 문서 작성

```bash
# 템플릿 복사
cp .release-notes/TEMPLATE.md .release-notes/RELEASE-v1.2.0.md

# Placeholder 교체
# {VERSION} → v1.2.0
# {DATE} → 2025-12-22
```

### Phase 5: 검증

```bash
# 프론트매터 확인
head -5 .release-notes/RELEASE-v1.2.0.md

# "Why this matters" 섹션 확인
grep -c "**Why this matters (왜 중요한가):**" .release-notes/RELEASE-v1.2.0.md

# Markdown 검증 (선택)
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-v1.2.0.md --disable MD013
```

---

## 핵심 원칙 4가지

### 1. 정확성 우선

> **Uber 파일에 없는 API는 절대 문서화하지 않습니다.**

```bash
# API 존재 확인
grep -n "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. 가치 전달 필수

> **모든 주요 기능에 "Why this matters (왜 중요한가):" 섹션을 포함합니다.**

```markdown
### 새로운 기능: TraverseSerial

컬렉션을 순차적으로 처리하는 메서드입니다.

**Why this matters (왜 중요한가):**
- 순서 보장이 필요한 작업에 적합
- 메모리 효율적인 처리
```

### 3. Breaking Changes 자동 감지

> **Git Diff 분석이 커밋 메시지 패턴보다 우선합니다.**

- `.api` 폴더의 Git diff 분석 (객관적)
- 커밋 메시지 패턴은 보조 수단

### 4. 추적성

> **모든 기능을 실제 커밋으로 추적합니다.**

```markdown
<!-- 관련 커밋: abc1234 -->
### 새로운 기능: ErrorCodeFactory
```

---

## 주요 출력 파일

### 최종 결과물

| 파일 | 설명 |
|------|------|
| `.release-notes/RELEASE-{VERSION}.md` | 릴리스 노트 |

### 분석 결과

| 파일 | 설명 |
|------|------|
| `.analysis-output/Functorium.md` | 핵심 라이브러리 분석 |
| `.analysis-output/Functorium.Testing.md` | 테스트 라이브러리 분석 |
| `.analysis-output/analysis-summary.md` | 전체 요약 |

### API 관련

| 파일 | 설명 |
|------|------|
| `api-changes-build-current/all-api-changes.txt` | Uber 파일 (모든 Public API) |
| `api-changes-build-current/api-changes-diff.txt` | API 변경사항 (Git diff) |

### 중간 결과물

| 파일 | 설명 |
|------|------|
| `work/phase3-commit-analysis.md` | 커밋 분류 결과 |
| `work/phase3-feature-groups.md` | 기능 그룹화 |
| `work/phase4-draft.md` | 릴리스 노트 초안 |
| `work/phase5-validation-report.md` | 검증 결과 |

---

## 트러블슈팅

### 일반적인 문제

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

### 전체 초기화 (macOS/Linux)

```bash
pkill -f dotnet
rm -rf .release-notes/scripts/.analysis-output
dotnet nuget locals all --clear
```

---

## 체크리스트

### 릴리스 노트 생성 전

- [ ] Git 저장소 확인됨
- [ ] .NET 10.x SDK 설치됨
- [ ] `.release-notes/scripts/` 디렉터리 존재

### 릴리스 노트 생성 후

- [ ] 프론트매터 포함됨
- [ ] 모든 필수 섹션 포함됨
- [ ] 모든 주요 기능에 "Why this matters" 섹션 포함됨
- [ ] 모든 코드 샘플이 Uber 파일에서 검증됨
- [ ] Breaking Changes 문서화됨 (있는 경우)
- [ ] 마이그레이션 가이드 포함됨 (Breaking Changes 있는 경우)

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [release-note.md](/.claude/commands/release-note.md) | 릴리스 노트 Command 정의 |
| [TEMPLATE.md](/.release-notes/TEMPLATE.md) | 릴리스 노트 템플릿 |
| [phase1-setup.md](/.release-notes/scripts/docs/phase1-setup.md) | Phase 1 상세 |
| [phase2-collection.md](/.release-notes/scripts/docs/phase2-collection.md) | Phase 2 상세 |
| [phase3-analysis.md](/.release-notes/scripts/docs/phase3-analysis.md) | Phase 3 상세 |
| [phase4-writing.md](/.release-notes/scripts/docs/phase4-writing.md) | Phase 4 상세 |
| [phase5-validation.md](/.release-notes/scripts/docs/phase5-validation.md) | Phase 5 상세 |

---

## 다음 단계

- [부록 A: 용어사전](../appendix/A-glossary.md)
