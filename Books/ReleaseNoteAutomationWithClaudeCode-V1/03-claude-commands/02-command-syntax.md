# 3.2 Command 문법 및 구조

> 이 절에서는 Claude Code 사용자 정의 Command의 상세 문법과 효과적인 구조화 방법을 알아봅니다.

---

## YAML 프론트매터

프론트매터는 Command 파일의 메타데이터를 정의합니다. 파일 최상단에 `---`로 감싸서 작성합니다.

### 기본 형식

```yaml
---
title: COMMAND-TITLE
description: 명령어에 대한 설명
argument-hint: "<arg> 인자에 대한 설명"
---
```

### 필드 상세

#### title (필수)

Command의 표시 이름입니다. 대문자와 하이픈 사용이 일반적입니다.

```yaml
title: RELEASE-NOTES
title: CODE-REVIEW
title: GENERATE-TEST
```

#### description (필수)

Command가 수행하는 작업에 대한 간단한 설명입니다. `/` 입력 시 자동완성 목록에 표시됩니다.

```yaml
description: 릴리스 노트를 자동으로 생성합니다
description: 코드를 분석하고 리뷰 의견을 제시합니다
```

#### argument-hint (선택)

인자에 대한 힌트 메시지입니다. `<>`로 인자 이름을, 그 뒤에 설명을 작성합니다.

```yaml
# 단일 인자
argument-hint: "<version> 릴리스 버전 (예: v1.2.0)"

# 선택적 인자 표현
argument-hint: "[topic] 선택적 토픽 필터"

# 복수 인자 예시
argument-hint: "<source> <target> 소스와 타겟 경로"
```

---

## Markdown 본문 구조

프론트매터 이후의 본문은 Markdown 형식으로 작성합니다. Claude에게 전달되는 실제 프롬프트입니다.

### 권장 구조

```markdown
---
(프론트매터)
---

# 명령어 제목

명령어 개요 설명

## 파라미터

파라미터 설명 및 검증 규칙

## 워크플로우/단계

수행할 작업 단계

## 규칙/가이드라인

따라야 할 규칙

## 출력 형식

결과물 형식 정의
```

### 예시: 완전한 Command 파일

```markdown
---
title: API-DOC
description: API 문서를 생성합니다
argument-hint: "<class> 문서화할 클래스명"
---

# API 문서 생성

$ARGUMENTS 클래스에 대한 API 문서를 생성합니다.

## 파라미터 검증

**클래스명:** $ARGUMENTS

클래스명이 지정되지 않은 경우 오류를 출력하고 중단합니다.

## 워크플로우

1. **클래스 검색**: 프로젝트에서 해당 클래스 찾기
2. **분석**: public 멤버 추출
3. **문서 생성**: Markdown 형식으로 작성

## 문서 규칙

- 모든 public 메서드 문서화
- 매개변수와 반환값 설명 포함
- 사용 예제 코드 포함

## 출력 형식

```markdown
# {ClassName}

## 개요
{클래스 설명}

## 메서드
### {MethodName}
{메서드 설명}

**매개변수:**
- `{param}`: {설명}

**반환값:** {설명}

**예제:**
```csharp
{코드}
```
```
```

---

## 효과적인 프롬프트 작성 기법

### 1. 명확한 지시어 사용

```markdown
# 좋은 예
다음 단계를 **순서대로** 실행하세요:
1. 먼저 A를 수행
2. 그 다음 B를 수행
3. 마지막으로 C를 수행

# 피해야 할 예
A, B, C를 해주세요.
```

### 2. 조건문 활용

```markdown
## 버전 파라미터 ($ARGUMENTS)

**버전이 지정된 경우:** $ARGUMENTS

버전 파라미터는 필수입니다.

**버전이 지정되지 않은 경우:**
다음 오류 메시지를 출력하고 중단합니다:
> 버전을 지정해주세요. 예: /release-note v1.2.0
```

### 3. 체크리스트 형식

```markdown
## 검증 체크리스트

다음 항목을 모두 확인하세요:

- [ ] 프론트매터가 포함되어 있는가?
- [ ] 모든 필수 섹션이 있는가?
- [ ] 코드 샘플이 검증되었는가?
- [ ] Breaking Changes가 문서화되었는가?
```

### 4. 표 활용

```markdown
## Phase별 목표

| Phase | 목표 | 출력 |
|-------|------|------|
| 1 | 환경 검증 | Base/Target 결정 |
| 2 | 데이터 수집 | .analysis-output/*.md |
| 3 | 커밋 분석 | phase3-*.md |
| 4 | 문서 작성 | RELEASE-*.md |
| 5 | 검증 | 검증 보고서 |
```

### 5. 코드 블록 지정

````markdown
## 실행할 명령어

```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

## 출력 예시

```markdown
# Analysis for Src/Functorium

## Change Summary
37 files changed
```
````

---

## 문서 참조 방식

다른 문서를 참조하여 상세 정보를 분리할 수 있습니다.

### 상대 경로 참조

```markdown
## Phase 1: 환경 검증

**상세**: [phase1-setup.md](.release-notes/scripts/docs/phase1-setup.md)

위 문서의 지침을 따라 환경을 검증하세요.
```

### 참조 테이블

```markdown
## 참고 문서

| Phase | 문서 | 설명 |
|-------|------|------|
| 1 | [phase1-setup.md](...) | 환경 검증 |
| 2 | [phase2-collection.md](...) | 데이터 수집 |
| 3 | [phase3-analysis.md](...) | 커밋 분석 |
```

Claude는 참조된 문서를 필요에 따라 읽고 지침을 따릅니다.

---

## 출력 형식 정의

Command의 결과물 형식을 명확히 정의하면 일관된 출력을 얻을 수 있습니다.

### 파일 출력

```markdown
## 출력 파일

**파일명:** `.release-notes/RELEASE-$ARGUMENTS.md`

**형식:**
```markdown
---
title: Functorium $ARGUMENTS 새로운 기능
date: {오늘 날짜}
---

# Functorium Release $ARGUMENTS

## 개요
...
```
```

### 콘솔 출력

````markdown
## 완료 메시지

작업 완료 시 다음 형식으로 출력하세요:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
릴리스 노트 생성 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

버전: $ARGUMENTS
파일: .release-notes/RELEASE-$ARGUMENTS.md

통계:
| 항목 | 값 |
|------|-----|
| 새로운 기능 | N개 |
| 버그 수정 | N개 |
| Breaking Changes | N개 |
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```
````

---

## 고급 기법

### 1. 단계별 중간 결과 저장

```markdown
## 중간 결과 저장 (필수)

각 Phase 완료 후 중간 결과를 저장하세요:

- `.analysis-output/work/phase3-commit-analysis.md`
- `.analysis-output/work/phase3-feature-groups.md`

이 파일들은 다음 Phase의 입력으로 사용됩니다.
```

### 2. 오류 처리

```markdown
## 오류 처리

다음 상황에서는 작업을 중단하고 사용자에게 알리세요:

1. **Base Branch 없음**:
   > origin/release/1.0 브랜치가 없습니다.
   > 첫 배포인 경우 초기 커밋부터 분석합니다.

2. **.NET SDK 없음**:
   > .NET 10 SDK가 설치되어 있지 않습니다.
   > 설치 후 다시 시도하세요.
```

### 3. 성공 기준 명시

```markdown
## 성공 기준

다음 조건을 모두 만족해야 Phase가 완료된 것입니다:

- [ ] 프론트매터 포함됨
- [ ] 모든 필수 섹션 포함됨
- [ ] 모든 주요 기능에 "장점:" 섹션 포함됨
- [ ] Uber 파일에 없는 API 사용: 0개
```

---

## 정리

| 요소 | 설명 | 예시 |
|------|------|------|
| 프론트매터 | 메타데이터 | title, description, argument-hint |
| 제목 | Command 목적 | `# 릴리스 노트 생성` |
| 파라미터 | 인자 검증 | `$ARGUMENTS` |
| 워크플로우 | 작업 단계 | 번호 목록 |
| 규칙 | 따라야 할 기준 | 체크리스트 |
| 출력 형식 | 결과물 형태 | 코드 블록 |
| 참조 | 외부 문서 링크 | 상대 경로 |

---

## 다음 단계

- [3.3 release-note.md 상세 분석](03-release-note-command.md)
