# 부록 A: 용어 사전

> 이 부록에서는 릴리스 노트 자동화 시스템에서 사용하는 주요 용어를 정리합니다.

---

## A

### API (Application Programming Interface)
**애플리케이션 프로그래밍 인터페이스**

소프트웨어 구성 요소 간의 상호작용을 정의하는 인터페이스입니다. 이 도서에서는 주로 Public API(외부에 노출되는 클래스, 메서드, 속성)를 의미합니다.

### Argument
**인자**

CLI 명령어에서 위치 기반으로 전달되는 값입니다. Option과 달리 플래그 없이 순서대로 전달됩니다.

```bash
dotnet greet.cs Alice    # "Alice"가 Argument
```

### Assembly
**어셈블리**

.NET에서 컴파일된 코드의 단위입니다. 보통 `.dll` 또는 `.exe` 파일 형태입니다.

---

## B

### Base Branch
**기준 브랜치**

비교의 시작점이 되는 Git 브랜치입니다. 릴리스 노트에서는 이전 릴리스 브랜치(예: `origin/release/1.0`)가 Base Branch가 됩니다.

### Breaking Change
**호환성 파괴 변경**

기존 코드와의 호환성을 깨뜨리는 API 변경입니다. 메서드 삭제, 시그니처 변경, 인터페이스 변경 등이 해당됩니다.

```diff
- public void Process(string data)      // 삭제됨 (Breaking Change)
+ public void Process(string data, bool validate)  // 새 파라미터 추가
```

---

## C

### CLI (Command Line Interface)
**명령줄 인터페이스**

텍스트 기반으로 명령어를 입력하여 프로그램을 실행하는 인터페이스입니다.

### Claude Code
**클로드 코드**

Anthropic에서 제공하는 AI 기반 코딩 어시스턴트입니다. 사용자 정의 Command를 통해 작업을 자동화할 수 있습니다.

### Command (Claude Code)
**사용자 정의 명령어**

`.claude/commands/` 폴더에 정의된 Markdown 파일로, Claude Code에서 슬래시(`/`) 명령으로 실행할 수 있습니다.

```bash
> /release-note v1.0.0
```

### Component
**컴포넌트**

분석 단위가 되는 프로젝트 또는 폴더입니다. `component-priority.json`에서 정의됩니다.

### Conventional Commits
**규약적 커밋**

커밋 메시지의 표준 형식입니다. `type(scope): description` 형태를 따릅니다.

```txt
feat(auth): 로그인 기능 추가
fix(api): null 참조 오류 수정
docs(readme): 설치 가이드 업데이트
```

---

## D

### Directive
**지시어**

.NET 10 file-based app에서 사용하는 특수 주석입니다. `#:` 접두사로 시작합니다.

```csharp
#:package System.CommandLine@2.0.1
#:sdk Microsoft.NET.Sdk.Web
```

### DLL (Dynamic Link Library)
**동적 링크 라이브러리**

.NET에서 컴파일된 어셈블리 파일 형식입니다.

---

## F

### Feature Commit
**기능 커밋**

새로운 기능을 추가하는 커밋입니다. `feat` 타입으로 시작합니다.

### File-based App
**파일 기반 앱**

.NET 10에서 도입된 기능으로, 단일 `.cs` 파일만으로 실행 가능한 애플리케이션입니다.

```bash
dotnet hello.cs
```

### Frontmatter
**프론트매터**

Markdown 파일 상단에 위치하는 YAML 형식의 메타데이터입니다.

```yaml
---
title: Release v1.0.0
date: 2025-01-15
---
```

---

## G

### Git Diff
**깃 디프**

두 커밋 또는 브랜치 간의 차이를 보여주는 Git 명령어입니다.

```bash
git diff origin/release/1.0 HEAD
```

### Glob Pattern
**글롭 패턴**

파일 경로 매칭에 사용되는 와일드카드 패턴입니다.

```txt
*.cs       - 모든 C# 파일
**/*.md    - 모든 하위 폴더의 Markdown 파일
src/**     - src 폴더 아래 모든 파일
```

---

## H

### Handler
**핸들러**

System.CommandLine에서 명령어 실행 시 호출되는 함수입니다.

```csharp
rootCommand.SetAction((parseResult, ct) => {
    // 핸들러 로직
    return 0;
});
```

---

## M

### Markdown
**마크다운**

텍스트 기반의 경량 마크업 언어입니다. `.md` 확장자를 사용합니다.

---

## N

### NuGet
**누겟**

.NET 생태계의 패키지 관리자입니다.

```csharp
#:package Spectre.Console@0.54.0
```

---

## O

### Option
**옵션**

CLI 명령어에서 플래그와 함께 전달되는 선택적 값입니다.

```bash
dotnet script.cs --output result.txt -v    # --output, -v가 Option
```

---

## P

### Phase
**페이즈 (단계)**

릴리스 노트 생성 워크플로우의 각 단계입니다.

| Phase | 이름 | 역할 |
|-------|------|------|
| 1 | 환경 검증 | 전제조건 확인 |
| 2 | 데이터 수집 | C# 스크립트 실행 |
| 3 | 커밋 분석 | 변경사항 분류 |
| 4 | 문서 작성 | 릴리스 노트 생성 |
| 5 | 검증 | 품질 검증 |

### Public API
**공개 API**

외부에서 접근 가능한 클래스, 메서드, 속성입니다. `public` 접근 제한자가 적용된 요소들입니다.

### PublicApiGenerator
**퍼블릭 API 제너레이터**

DLL에서 Public API를 추출하는 .NET 라이브러리입니다.

---

## R

### Release Notes
**릴리스 노트**

소프트웨어 버전의 변경사항을 문서화한 것입니다. 새 기능, 버그 수정, Breaking Changes 등을 포함합니다.

### RootCommand
**루트 커맨드**

System.CommandLine에서 최상위 명령어를 나타내는 클래스입니다.

---

## S

### Scope
**스코프 (범위)**

Conventional Commits에서 변경 영역을 나타내는 부분입니다.

```txt
feat(auth): ...    # scope = auth
fix(api): ...      # scope = api
```

### SDK (Software Development Kit)
**소프트웨어 개발 키트**

소프트웨어 개발에 필요한 도구 모음입니다. .NET SDK는 .NET 애플리케이션 개발에 필요합니다.

### Shebang
**셔뱅**

스크립트 파일의 첫 줄에 위치하는 인터프리터 지정 구문입니다.

```csharp
#!/usr/bin/env dotnet
```

### Spectre.Console
**스펙터 콘솔**

.NET용 리치 콘솔 UI 라이브러리입니다. 테이블, 패널, 프로그레스 바 등을 지원합니다.

### System.CommandLine
**시스템 커맨드라인**

.NET용 CLI 인자 파싱 라이브러리입니다. Microsoft에서 제공합니다.

---

## T

### Target Branch
**대상 브랜치**

비교의 끝점이 되는 Git 브랜치입니다. 보통 `HEAD` 또는 현재 브랜치를 의미합니다.

### Template
**템플릿**

릴리스 노트의 표준 형식을 정의하는 Markdown 파일입니다. `TEMPLATE.md`

### TRX (Test Results XML)
**테스트 결과 XML**

.NET 테스트 결과를 저장하는 XML 파일 형식입니다.

```bash
dotnet test --logger "trx"
```

### Type (Commit)
**타입 (커밋)**

Conventional Commits에서 변경 유형을 나타내는 접두사입니다.

| Type | 설명 |
|------|------|
| `feat` | 새 기능 |
| `fix` | 버그 수정 |
| `docs` | 문서 변경 |
| `refactor` | 리팩토링 |
| `test` | 테스트 |
| `chore` | 기타 작업 |

---

## U

### Uber File
**우버 파일 (통합 파일)**

모든 API 변경사항을 하나로 모은 파일입니다. `all-api-changes.txt`

**역할**: API 검증의 단일 진실 소스(Single Source of Truth)

---

## W

### Workflow
**워크플로우**

작업의 흐름을 정의한 것입니다. 릴리스 노트 생성은 5-Phase 워크플로우를 따릅니다.

---

## 한글 용어

### Why this matters 섹션
**Why this matters Section**

릴리스 노트에서 각 기능의 사용자 가치를 설명하는 섹션입니다. Phase 4 필수 규칙입니다.

```markdown
**Why this matters (왜 중요한가):** 구조화된 오류 정보로 디버깅 시간을 단축합니다.
```

### 첫 배포
**First Release**

이전 릴리스 브랜치가 없는 최초 배포입니다. Git 저장소의 첫 커밋부터 분석합니다.

### 후속 배포
**Subsequent Release**

이전 릴리스 이후의 배포입니다. 이전 릴리스 브랜치부터 현재까지만 분석합니다.

---

## 다음 단계

- [부록 B: API 레퍼런스](B-api-reference.md)
