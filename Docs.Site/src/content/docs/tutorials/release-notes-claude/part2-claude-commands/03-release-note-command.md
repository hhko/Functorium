---
title: "release-note.md 분석"
---

지금까지 Command의 개념과 문법을 살펴보았습니다. 이제 릴리스 노트 자동화의 핵심인 `release-note.md` Command 파일을 열어보겠습니다. 이 파일은 자동화 시스템의 "두뇌"에 해당합니다. Claude가 `/release-note v1.2.0`을 실행했을 때 어떤 순서로 무엇을 하는지, 그리고 왜 그렇게 설계되었는지를 이해하면 자신만의 복잡한 Command도 설계할 수 있게 됩니다.

파일 위치는 `.claude/commands/release-note.md`입니다.

## 프론트매터 분석

```yaml
---
title: RELEASE-NOTES
description: 릴리스 노트를 자동으로 생성합니다 (데이터 수집, 분석, 작성, 검증).
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"
---
```

description에 "(데이터 수집, 분석, 작성, 검증)"을 포함한 것은 의도적입니다. `/`를 입력했을 때 자동완성 목록에서 이 Command가 무엇을 하는지 한눈에 파악할 수 있도록 전체 프로세스를 요약해둔 것입니다. argument-hint의 `<version>`은 필수 인자임을 표시하며, 예시(`v1.2.0`)를 함께 제공하여 어떤 형식으로 전달해야 하는지를 안내합니다.

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

버전 파라미터 섹션이 워크플로우보다 먼저 오는 이유가 있습니다. 인자가 없으면 나머지 작업이 무의미하므로, 가장 먼저 검증하고 실패 시 즉시 중단하는 것입니다. 다양한 버전 형식 예시를 제공하여 Claude가 어떤 형식이든 올바르게 처리하도록 유도합니다.

## 워크플로우 개요

release-note.md는 5단계 Phase로 구성된 워크플로우를 정의합니다. 각 Phase별 목표를 테이블로 요약하고, 상세 지침은 별도 문서로 분리하는 **모듈화 패턴을** 사용합니다.

```markdown
| Phase | 목표 | 상세 문서 |
|-------|------|----------|
| **1. 환경 검증** | 전제조건 확인, Base Branch 결정 | [phase1-setup.md](...) |
| **2. 데이터 수집** | 컴포넌트/API 변경사항 분석 | [phase2-collection.md](...) |
| **3. 커밋 분석** | 기능 추출, Breaking Changes 감지 | [phase3-analysis.md](...) |
| **4. 문서 작성** | 릴리스 노트 작성 | [phase4-writing.md](...) |
| **5. 검증** | 품질 및 정확성 검증 | [phase5-validation.md](...) |
```

이 설계에는 두 가지 의도가 있습니다. 첫째, 마스터 파일(release-note.md)에는 전체 흐름과 핵심 규칙만 담고, 각 Phase의 상세 지침은 별도 파일로 분리하여 유지보수를 쉽게 합니다. 둘째, Claude가 필요한 Phase의 상세 문서만 선택적으로 읽을 수 있어 컨텍스트 윈도우를 효율적으로 사용합니다.

## Phase 1: 환경 검증

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

Phase 1이 환경 검증으로 시작하는 것은 "실패를 빨리 발견하라"는 원칙을 따른 것입니다. Git 저장소가 아니거나, .NET SDK가 없거나, 스크립트 디렉터리가 없으면 이후 Phase가 모두 실패합니다. 미리 확인하여 불필요한 작업을 방지합니다.

Base Branch 결정에서 **조건부 처리 패턴을** 사용한 것도 주목할 점입니다. release 브랜치가 있는 일반적인 경우와 첫 배포인 경우를 모두 처리하여, 어떤 상황에서든 Command가 동작하도록 합니다.

## Phase 2: 데이터 수집

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
```

**성공 기준**:
- [ ] `.analysis-output/*.md` 파일 생성됨
- [ ] `all-api-changes.txt` Uber 파일 생성됨
- [ ] `api-changes-diff.txt` Git Diff 파일 생성됨
````

Phase 2에서는 실행 순서를 번호로 명시하고, 생성될 파일명을 구체적으로 나열합니다. 이렇게 **입출력을 명시하는 패턴은** Phase 간 데이터 흐름을 명확하게 하여, Claude가 다음 Phase에서 어떤 파일을 읽어야 하는지 혼동하지 않도록 합니다.

## Phase 3: 커밋 분석

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

Breaking Changes 감지에서 두 가지 방법을 제시하되 "(권장)"으로 **우선순위를 표시하는 패턴은** Claude에게 선호하는 접근 방식을 알려주면서도 대안을 열어두는 전략입니다. Git Diff 분석이 더 정확하지만, 커밋 메시지 패턴도 보조적으로 활용할 수 있습니다.

중간 결과를 파일로 저장하도록 한 것은 **추적성을 위한 설계입니다.** 문제가 발생했을 때 Phase 3의 분석 결과를 직접 확인하여 원인을 파악할 수 있고, Phase 4에서 이 파일들을 입력으로 사용합니다.

## Phase 4: 문서 작성

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

출력 파일명에 `$ARGUMENTS`를 사용하여 버전이 자동으로 반영되도록 한 것은 파일 충돌을 방지하는 실용적인 설계입니다.

작성 원칙에서 "절대", "필수", "불완전한 것으로 간주"와 같은 **강한 어조를** 사용한 것도 의도적입니다. LLM은 약한 지시("가능하면", "추천")를 종종 무시하지만, 강한 지시는 더 잘 따릅니다. 특히 "정확성 우선" 원칙은 AI가 존재하지 않는 API를 만들어내는(hallucination) 것을 방지하는 핵심 가드레일입니다.

## Phase 5: 검증

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

검증을 별도 Phase로 분리한 것은 "작성자가 스스로 검토"하는 패턴입니다. Phase 4에서 문서를 작성한 뒤, Phase 5에서 기계적으로 검증하면 빠뜨린 항목을 잡아낼 수 있습니다. `grep -c`로 개수를 세는 정량적 검증과, Git Diff와 문서를 대조하는 교차 검증을 병행하여 정확도를 높입니다.

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

완료 메시지를 고정 형식으로 정의한 이유는 두 가지입니다. 통계 요약으로 작업 결과를 즉시 확인할 수 있고, "다음 단계" 안내로 사용자가 후속 작업을 바로 이어갈 수 있습니다.

## 핵심 원칙

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

핵심 원칙을 Command 말미에 다시 정리한 것은 의도적인 반복입니다. Claude의 컨텍스트 윈도우에서 마지막에 읽은 내용이 더 강하게 작용하므로, 가장 중요한 원칙들을 다시 한 번 강조하는 효과가 있습니다.

## 설계 패턴 정리

release-note.md에서 사용된 설계 패턴을 정리하면 다음과 같습니다. 자신만의 Command를 만들 때 참고하면 됩니다.

**모듈화 패턴.** 마스터 파일에 핵심 규칙과 전체 흐름을, 별도 파일에 상세 지침을 분리합니다. 유지보수가 쉬워지고, Claude가 필요한 부분만 선택적으로 읽을 수 있습니다.

**체크리스트 패턴.** 각 Phase의 성공 기준을 체크리스트로 명시합니다. 진행 상황을 추적하기 쉽고, 누락을 방지합니다.

**입출력 명시 패턴.** 각 Phase에서 읽는 파일(입력)과 생성하는 파일(출력)을 명확히 나열합니다. Phase 간 데이터 흐름이 명확해지고, 문제 발생 시 디버깅이 쉽습니다.

**조건부 처리 패턴.** "~인 경우"와 "~이 아닌 경우"를 모두 정의하여 다양한 상황에 대응합니다. 오류 처리도 이 패턴의 일부입니다.

## FAQ

### Q1: release-note.md에서 핵심 원칙을 Command 말미에 다시 정리한 이유는 무엇인가요?
**A**: LLM의 컨텍스트 윈도우에서 **마지막에 읽은 내용이 더 강하게 작용하기** 때문입니다. "정확성 우선", "가치 전달 필수" 같은 핵심 원칙을 마지막에 반복하면 Claude가 Phase 4~5에서 이 원칙을 더 잘 준수합니다. 의도적인 반복 전략입니다.

### Q2: Phase 문서를 별도 파일로 분리한 모듈화 패턴의 장점은 무엇인가요?
**A**: 두 가지 장점이 있습니다. 첫째, 마스터 파일(`release-note.md`)은 전체 흐름만 담고 상세 지침은 별도 파일로 분리하여 **유지보수가 쉬워집니다.** 둘째, Claude가 필요한 Phase의 상세 문서만 선택적으로 읽어 **컨텍스트 윈도우를 효율적으로 사용합니다.**

### Q3: "절대", "필수"와 같은 강한 어조를 사용한 것은 실제로 효과가 있나요?
**A**: 효과가 있습니다. LLM은 "가능하면", "추천" 같은 약한 지시를 종종 무시하지만, "절대 문서화하지 않음", "필수" 같은 강한 지시는 더 높은 확률로 따릅니다. 특히 **"Uber 파일에 없는 API는 절대 문서화하지 않음"은** AI의 hallucination을 방지하는 핵심 가드레일입니다.

다음 절에서는 릴리스 노트 자동화의 기반이 되는 또 다른 Command인 `commit.md`를 살펴보겠습니다.
