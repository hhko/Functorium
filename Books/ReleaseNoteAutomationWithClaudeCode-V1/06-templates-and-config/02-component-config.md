# 6.2 component-priority.json 설정

> 이 절에서는 분석 대상 컴포넌트를 설정하는 component-priority.json 파일을 알아봅니다.

---

## 개요

component-priority.json은 **분석 대상 컴포넌트와 우선순위**를 정의하는 설정 파일입니다.

```txt
위치: .release-notes/scripts/config/component-priority.json

역할:
├── 분석할 폴더 목록 정의
├── 분석 우선순위 결정
└── 커스텀 컴포넌트 추가
```

---

## 파일 구조

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

---

## 속성 설명

### analysis_priorities

분석할 폴더 경로의 배열입니다.

| 속성 | 타입 | 설명 |
|------|------|------|
| `analysis_priorities` | string[] | 분석할 폴더 경로 목록 |

### 경로 형식

- **상대 경로**: Git 저장소 루트 기준
- **슬래시 사용**: `/` (Windows에서도 동일)
- **대소문자 구분**: 실제 폴더명과 일치

---

## 예시

### 기본 설정

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing"
  ]
}
```

### 문서 포함

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs"
  ]
}
```

### 여러 프로젝트

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

배열의 순서가 분석 및 출력 우선순위를 결정합니다:

```json
{
  "analysis_priorities": [
    "Src/Functorium",        // 1순위 - 먼저 분석 및 출력
    "Src/Functorium.Testing", // 2순위
    "Docs"                    // 3순위 - 마지막
  ]
}
```

### 출력 파일 순서

```txt
.analysis-output/
├── Functorium.md          # 1순위 컴포넌트
├── Functorium.Testing.md  # 2순위 컴포넌트
├── Docs.md                # 3순위 컴포넌트
└── analysis-summary.md    # 요약 (우선순위 순으로 나열)
```

### 릴리스 노트 순서

릴리스 노트의 "새로운 기능" 섹션도 이 순서를 따릅니다:

```markdown
## 새로운 기능

### Functorium 라이브러리     (1순위)
...

### Functorium.Testing 라이브러리  (2순위)
...

### Docs                      (3순위)
...
```

---

## 폴더 없는 경우 처리

설정 파일이 없거나 빈 경우, 기본값이 적용됩니다:

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

존재하지 않는 폴더는 자동으로 건너뜁니다.

---

## 새 프로젝트 추가

### 1. 폴더 확인

```bash
# 프로젝트 폴더 확인
ls Src/

# 출력 예시:
# Functorium/
# Functorium.Testing/
# Functorium.Web/       <- 새로 추가할 프로젝트
```

### 2. 설정 파일 수정

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Src/Functorium.Web"
  ]
}
```

### 3. 분석 실행

```bash
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

### 4. 결과 확인

```txt
.analysis-output/
├── Functorium.md
├── Functorium.Testing.md
├── Functorium.Web.md      <- 새로 생성됨
└── analysis-summary.md
```

---

## 고급 설정 예시

### 모노레포 구조

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

### 마이크로서비스 구조

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

### 특정 컴포넌트만 분석

테스트 제외:

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Docs"
  ]
}
```

---

## 출력 파일명 규칙

폴더 경로에서 출력 파일명이 생성됩니다:

| 폴더 경로 | 출력 파일명 |
|-----------|------------|
| `Src/Functorium` | `Functorium.md` |
| `Src/Functorium.Testing` | `Functorium.Testing.md` |
| `Docs` | `Docs.md` |
| `packages/core` | `packages-core.md` |

경로의 마지막 부분이 파일명이 됩니다. 슬래시는 하이픈으로 변환됩니다.

---

## 요약

| 항목 | 설명 |
|------|------|
| 파일 위치 | `.release-notes/scripts/config/component-priority.json` |
| 형식 | JSON |
| 주요 속성 | `analysis_priorities` (string[]) |
| 순서 의미 | 분석 및 출력 우선순위 |
| 기본값 | Functorium, Functorium.Testing, Docs |

---

## 다음 단계

- [6.3 출력 파일 형식](03-output-formats.md)
