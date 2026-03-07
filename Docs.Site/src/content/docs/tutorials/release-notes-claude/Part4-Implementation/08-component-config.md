---
title: "component-priority.json"
---

프로젝트에 소스 코드, 테스트, 문서 등 다양한 폴더가 있는데, 어떤 폴더를 분석 대상으로 삼아야 할까요? 모든 폴더를 분석하면 불필요한 노이즈가 섞이고, 핵심 라이브러리의 변경사항이 묻힐 수 있습니다. component-priority.json은 이 질문에 답하는 설정 파일입니다. **분석 대상 컴포넌트와 우선순위를** 정의하여, 릴리스 노트에 어떤 내용이 어떤 순서로 나타날지 결정합니다.

이 파일은 `.release-notes/scripts/config/component-priority.json`에 위치합니다.

## 파일 구조

파일 내용은 단순합니다. `analysis_priorities`라는 하나의 배열에 분석할 폴더 경로를 나열합니다.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs",
    ".release-notes/scripts"
  ]
}
```

## 속성 설명

### analysis_priorities

`analysis_priorities`는 분석할 폴더 경로의 배열입니다. 경로는 Git 저장소 루트를 기준으로 한 상대 경로이며, Windows에서도 슬래시(`/`)를 사용합니다. 대소문자는 실제 폴더명과 일치해야 합니다.

## 예시

가장 기본적인 설정은 핵심 라이브러리만 분석하는 것입니다.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing"
  ]
}
```

문서 변경사항도 릴리스 노트에 포함하고 싶다면 Docs 폴더를 추가합니다.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs"
  ]
}
```

프로젝트 규모가 크다면 여러 프로젝트를 나열할 수도 있습니다.

```json
{
  "analysis_priorities": [
    "Src/Core",
    "Src/Api",
    "Src/Web",
    "Src/Infrastructure",
    "Tests/UnitTests",
    "Tests/IntegrationTests"
  ]
}
```

---

## 우선순위 동작

배열의 순서는 단순한 나열이 아닙니다. **순서가 곧 우선순위입니다.** 배열에서 앞에 위치한 컴포넌트가 먼저 분석되고, 출력 파일에서도 먼저 나타납니다.

```json
{
  "analysis_priorities": [
    "Src/Functorium",        // 1순위 - 먼저 분석 및 출력
    "Src/Functorium.Testing", // 2순위
    "Docs"                    // 3순위 - 마지막
  ]
}
```

이 순서는 분석 출력 파일의 나열 순서에 그대로 반영됩니다.

```txt
.analysis-output/
├── Functorium.md          # 1순위 컴포넌트
├── Functorium.Testing.md  # 2순위 컴포넌트
├── Docs.md                # 3순위 컴포넌트
└── analysis-summary.md    # 요약 (우선순위 순으로 나열)
```

릴리스 노트의 "새로운 기능" 섹션도 같은 순서를 따릅니다. 핵심 라이브러리가 가장 먼저 나오고, 부가적인 내용은 뒤에 배치됩니다.

```markdown
## 새로운 기능

### Functorium 라이브러리     (1순위)
...

### Functorium.Testing 라이브러리  (2순위)
...

### Docs                      (3순위)
...
```

## 폴더 없는 경우 처리

설정 파일이 없거나 비어 있으면 기본값이 적용됩니다.

```csharp
// AnalyzeAllComponents.cs의 기본값
if (components.Count == 0)
{
    components = new List<string>
    {
        "Src/Functorium",
        "Src/Functorium.Testing",
        "Docs"
    };
}
```

존재하지 않는 폴더가 배열에 포함되어 있어도 문제없습니다. 자동으로 건너뜁니다.

---

## 새 프로젝트 추가

새 프로젝트를 분석 대상에 추가하는 과정은 세 단계입니다.

먼저 추가할 프로젝트 폴더가 실제로 존재하는지 확인합니다.

```bash
# 프로젝트 폴더 확인
ls Src/

# 출력 예시:
# Functorium/
# Functorium.Testing/
# Functorium.Web/       <- 새로 추가할 프로젝트
```

확인했으면 설정 파일에 해당 경로를 추가합니다.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Src/Functorium.Web"
  ]
}
```

그리고 분석을 실행하면 새 컴포넌트에 대한 결과 파일이 생성됩니다.

```bash
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

```txt
.analysis-output/
├── Functorium.md
├── Functorium.Testing.md
├── Functorium.Web.md      <- 새로 생성됨
└── analysis-summary.md
```

## 고급 설정 예시

다양한 프로젝트 구조에 맞게 설정할 수 있습니다. 모노레포 구조라면 패키지와 앱을 구분하여 나열합니다.

```json
{
  "analysis_priorities": [
    "packages/core",
    "packages/ui",
    "packages/api",
    "apps/web",
    "apps/mobile",
    "docs"
  ]
}
```

마이크로서비스 구조라면 서비스별로 나열합니다.

```json
{
  "analysis_priorities": [
    "services/auth",
    "services/user",
    "services/order",
    "services/payment",
    "shared/common",
    "shared/contracts"
  ]
}
```

특정 컴포넌트만 집중적으로 분석하고 싶다면, 필요한 폴더만 남기면 됩니다. 예를 들어 테스트를 제외하려면 다음과 같이 설정합니다.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Docs"
  ]
}
```

## 출력 파일명 규칙

폴더 경로에서 출력 파일명이 자동으로 생성됩니다. 경로의 마지막 부분이 파일명이 되며, 슬래시는 하이픈으로 변환됩니다.

| 폴더 경로 | 출력 파일명 |
|-----------|------------|
| `Src/Functorium` | `Functorium.md` |
| `Src/Functorium.Testing` | `Functorium.Testing.md` |
| `Docs` | `Docs.md` |
| `packages/core` | `packages-core.md` |

---

component-priority.json은 `analysis_priorities`라는 단일 배열로 분석 대상과 순서를 제어합니다. 파일 위치는 `.release-notes/scripts/config/component-priority.json`이고, 배열의 순서가 분석 및 출력 우선순위를 결정합니다. 설정이 없으면 Functorium, Functorium.Testing, Docs가 기본값으로 적용됩니다. 단순한 JSON 파일 하나지만, 릴리스 노트에 어떤 내용이 어떤 순서로 담길지를 결정하는 중요한 역할을 합니다.

## 다음 단계

- [출력 파일 형식](09-output-formats.md)
