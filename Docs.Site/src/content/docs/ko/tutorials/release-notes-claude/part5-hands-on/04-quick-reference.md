---
title: "빠른 참조"
---

이 페이지는 릴리스 노트를 생성할 때 필요한 핵심 정보를 한곳에 모았습니다. 명령어 사용법, 워크플로우 요약, 출력 파일 경로, 트러블슈팅까지 빠르게 찾아볼 수 있도록 구성했습니다. 전체 맥락이 필요한 경우 해당 절의 본문을 참고하세요.

## 명령어 사용법

기본 형식은 다음과 같습니다.

```bash
/release-note <version>
```

버전 파라미터는 SemVer 형식을 따르며, 프리릴리스 태그도 지원합니다.

| 명령어 | 설명 |
|--------|------|
| `/release-note v1.0.0` | 첫 배포 |
| `/release-note v1.2.0` | 정규 릴리스 |
| `/release-note v1.2.0-alpha.1` | 알파 릴리스 |
| `/release-note v1.2.0-beta.1` | 베타 릴리스 |
| `/release-note v1.2.0-rc.1` | 릴리스 후보 |

## 5-Phase 워크플로우 요약

하나의 명령어가 다섯 개의 Phase를 순서대로 실행합니다. 각 Phase의 목표, 실행 내용, 출력물을 정리합니다.

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

Breaking Changes를 감지하는 두 가지 방법이 있으며, Git Diff 분석이 우선합니다.

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

## 핵심 원칙 4가지

릴리스 노트의 품질을 결정하는 네 가지 원칙입니다. 각 원칙이 존재하는 이유를 이해하면 검토 시 무엇을 확인해야 하는지 명확해집니다.

### 1. 정확성 우선

> **Uber 파일에 없는 API는 절대 문서화하지 않습니다.**

LLM이 API 시그니처를 "그럴듯하게" 생성할 수 있기 때문입니다. Uber 파일(`all-api-changes.txt`)은 실제 빌드된 DLL에서 추출한 것이므로, 이것을 진실의 원천으로 삼아야 합니다.

```bash
# API 존재 확인
grep -n "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. 가치 전달 필수

> **모든 주요 기능에 "Why this matters (왜 중요한가):" 섹션을 포함합니다.**

API 변경 목록만으로는 사용자가 업그레이드 여부를 판단하기 어렵습니다. 각 기능이 어떤 문제를 해결하고 어떤 이점을 제공하는지 설명해야 릴리스 노트가 실질적인 가치를 전달합니다.

```markdown
### 새로운 기능: TraverseSerial

컬렉션을 순차적으로 처리하는 메서드입니다.

**Why this matters (왜 중요한가):**
- 순서 보장이 필요한 작업에 적합
- 메모리 효율적인 처리
```

### 3. Breaking Changes 자동 감지

> **Git Diff 분석이 커밋 메시지 패턴보다 우선합니다.**

커밋 메시지에 `BREAKING CHANGE`를 깜빡 표기하지 않을 수 있습니다. 반면 `.api` 폴더의 Git diff는 실제 API 변경을 객관적으로 보여주므로, 누락 없이 Breaking Changes를 감지할 수 있습니다.

- `.api` 폴더의 Git diff 분석 (객관적)
- 커밋 메시지 패턴은 보조 수단

### 4. 추적성

> **모든 기능을 실제 커밋으로 추적합니다.**

릴리스 노트에 기술된 기능이 어떤 커밋에서 구현되었는지 추적할 수 있어야 합니다. 나중에 해당 기능의 구현 세부사항을 확인하거나 문제를 조사할 때 출발점이 됩니다.

```markdown
<!-- 관련 커밋: abc1234 -->
### 새로운 기능: ErrorFactory
```

## 주요 출력 파일

워크플로우가 생성하는 파일들을 용도별로 정리합니다.

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

각 Phase에서 생성되는 작업 파일입니다. 문제 발생 시 어느 Phase까지 정상 진행되었는지 확인하는 데 유용합니다.

| 파일 | 설명 |
|------|------|
| `work/phase3-commit-analysis.md` | 커밋 분류 결과 |
| `work/phase3-feature-groups.md` | 기능 그룹화 |
| `work/phase4-draft.md` | 릴리스 노트 초안 |
| `work/phase5-validation-report.md` | 검증 결과 |

## 트러블슈팅

자주 발생하는 문제와 해결 방법을 표로 정리합니다. 상세한 설명은 [문제 해결 가이드](03-troubleshooting.md)를 참고하세요.

| 문제 | 해결 방법 |
|------|----------|
| Base Branch 없음 | 첫 배포로 자동 감지, 초기 커밋부터 분석 |
| .NET SDK 버전 오류 | .NET 10.x 설치 필요 |
| 파일 잠금 문제 | `taskkill /F /IM dotnet.exe` (Windows) |
| API 검증 실패 | Uber 파일에서 올바른 API 이름 확인 |

환경을 완전히 초기화해야 할 때는 다음 명령을 사용합니다.

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

## 체크리스트

릴리스 노트 생성 전후로 확인할 항목입니다.

### 생성 전

- [ ] Git 저장소 확인됨
- [ ] .NET 10.x SDK 설치됨
- [ ] `.release-notes/scripts/` 디렉터리 존재

### 생성 후

- [ ] 프론트매터 포함됨
- [ ] 모든 필수 섹션 포함됨
- [ ] 모든 주요 기능에 "Why this matters" 섹션 포함됨
- [ ] 모든 코드 예제이 Uber 파일에서 검증됨
- [ ] Breaking Changes 문서화됨 (있는 경우)
- [ ] 마이그레이션 가이드 포함됨 (Breaking Changes 있는 경우)

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

## FAQ

### Q1: "정확성 우선" 원칙에서 Uber 파일에 없는 API를 문서화하지 않는다면, 아직 빌드되지 않은 새 기능은 어떻게 처리하나요?
**A**: 릴리스 노트는 **이미 구현이 완료된 기능만** 문서화합니다. Phase 2에서 `dotnet publish`로 빌드한 뒤 DLL에서 API를 추출하므로, 코드가 컴파일되지 않는 기능은 Uber 파일에 포함되지 않습니다. 아직 개발 중인 기능은 다음 릴리스에서 문서화해야 합니다.

### Q2: 체크리스트의 "모든 코드 예제이 Uber 파일에서 검증됨" 항목은 구체적으로 무엇을 확인하나요?
**A**: 릴리스 노트의 코드 예제에 등장하는 **클래스명, 메서드명, 파라미터 타입이 Uber 파일의 API 시그니처와 정확히 일치하는지** 확인합니다. 예를 들어 코드 예제에서 `ErrorFactory.CreateExpected()`를 사용했다면, Uber 파일에 해당 메서드가 동일한 시그니처로 존재해야 합니다.

### Q3: 전체 초기화 명령에서 dotnet 프로세스를 강제 종료하는 것이 안전한가요?
**A**: 릴리스 노트 스크립트 실행과 관련된 dotnet 프로세스만 종료하는 것이 이상적이지만, 다른 dotnet 애플리케이션이 동시에 실행 중이 아니라면 **`taskkill /F /IM dotnet.exe`(Windows) 또는 `pkill -f dotnet`(macOS/Linux)으로** 전체 종료해도 문제없습니다. 실행 중인 다른 .NET 서비스가 있다면 해당 프로세스의 PID를 확인하여 선택적으로 종료하세요.

### Q4: 이 빠른 참조 가이드는 버전이 올라갈 때마다 업데이트해야 하나요?
**A**: 워크플로우 구조나 명령어가 변경될 때만 업데이트하면 됩니다. `/release-note` 명령어의 사용법, 5-Phase 구조, 출력 파일 경로 등은 **버전과 무관한 시스템 구조 정보이므로,** 릴리스 노트 자동화 시스템 자체를 개선하지 않는 한 그대로 사용할 수 있습니다.

이것으로 릴리스 노트 자동화의 실습 파트를 마칩니다. 이어지는 부록에서는 용어 사전과 추가 참고 자료를 제공합니다.

- [부록 A: 용어사전](../Appendix/A-glossary.md)
