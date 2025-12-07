# 코드 품질 가이드

이 문서는 EditorConfig와 MSBuild를 사용한 코드 스타일 및 코드 분석 규칙 설정 방법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [EditorConfig 설정](#editorconfig-설정)
- [Directory.Build.props 설정](#directorybuildprops-설정)
- [코드 스타일 규칙](#코드-스타일-규칙)
- [빌드 시 코드 분석](#빌드-시-코드-분석)
- [검증 방법](#검증-방법)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

일관된 코드 스타일과 높은 코드 품질을 유지하기 위해 자동화된 검증 도구를 설정합니다.

### 주요 도구

| 도구 | 역할 | 적용 시점 |
|------|------|----------|
| EditorConfig | 코드 스타일 규칙 정의 | IDE 실시간, 빌드 시 |
| Directory.Build.props | 프로젝트 공통 설정 | 빌드 시 |
| .NET Analyzers | 코드 분석 규칙 | 빌드 시 |

### 파일 위치

```
프로젝트루트/
├── .editorconfig                 # 코드 스타일 규칙
├── Directory.Build.props          # 공통 빌드 설정
└── Src/
    └── Functorium/
        └── Functorium.csproj     # 프로젝트 파일
```

<br/>

## 요약

### 주요 명령

**빌드:**
```bash
dotnet build --no-incremental
dotnet clean && dotnet build --no-incremental
dotnet build /p:TreatWarningsAsErrors=true
```

**검증:**
```bash
dotnet build --no-incremental  # 경고 확인
```

### 주요 절차

**1. 초기 설정:**
```bash
# 1. .editorconfig 파일 생성 (프로젝트 루트)
# [*.{cs,vb}]
# csharp_style_namespace_declarations = file_scoped:warning
# dotnet_diagnostic.IDE0161.severity = warning

# 2. Directory.Build.props 생성 (프로젝트 루트)
# <PropertyGroup>
#   <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
# </PropertyGroup>

# 3. 검증
dotnet build --no-incremental
```

**2. 문제 해결:**
```bash
# 1단계: 경고가 안 보이면
dotnet build --no-incremental

# 2단계: 그래도 안 되면
dotnet clean && dotnet build --no-incremental

# 3단계: 여전히 안 되면 트러블슈팅 섹션 참조
```

### 주요 개념

**1. 코드 품질 자동화**
- EditorConfig로 코드 스타일 규칙 정의
- Directory.Build.props로 빌드 시 검증 활성화
- IDE와 빌드 모두에서 일관된 규칙 적용

**2. 필수 설정 파일**

| 파일 | 역할 | 핵심 설정 |
|------|------|----------|
| `.editorconfig` | 코드 스타일 규칙 정의 | `csharp_style_namespace_declarations = file_scoped:warning` |
| `Directory.Build.props` | 빌드 시 검증 활성화 | `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` |

**3. 증분 빌드 주의사항**
- 설정 파일 변경 후에는 반드시 `--no-incremental` 사용
- 증분 빌드는 캐시를 재사용하여 설정 변경을 감지하지 못함
- 의심스러운 빌드 결과는 항상 `dotnet clean` 후 재빌드

<br/>

## EditorConfig 설정

### 기본 구조

`.editorconfig` 파일은 코드 스타일 규칙을 정의합니다.

```ini
root = true

# All files
[*]
indent_style = space

# C# files
[*.cs]
indent_size = 4
tab_width = 4
insert_final_newline = false
```

### 파일 기반 네임스페이스 규칙

파일 기반 네임스페이스를 강제하는 설정:

```ini
[*.{cs,vb}]
# 파일 기반 네임스페이스 사용
csharp_style_namespace_declarations = file_scoped:warning
dotnet_diagnostic.IDE0161.severity = warning
```

| 설정 | 값 | 설명 |
|------|----|----- |
| `csharp_style_namespace_declarations` | `file_scoped` | 파일 기반 네임스페이스 사용 |
| 심각도 | `warning` | 경고로 표시 |
| `dotnet_diagnostic.IDE0161.severity` | `warning` | IDE0161 규칙 경고 수준 |

### 코드 예시

**권장 (file-scoped):**
```csharp
namespace Functorium;

public class Calculator
{
    public int Add(int a, int b) => a + b;
}
```

**비권장 (block-scoped):**
```csharp
namespace Functorium
{
    public class Calculator
    {
        public int Add(int a, int b) => a + b;
    }
}
```

<br/>

## Directory.Build.props 설정

### 목적

모든 프로젝트에 공통으로 적용되는 빌드 설정을 한 곳에서 관리합니다.

### 기본 구조

```xml
<Project>
  <PropertyGroup>
    <!-- Target Framework -->
    <TargetFramework>net10.0</TargetFramework>

    <!-- Language Features -->
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Code Quality -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### 설정 항목

| 설정 | 값 | 설명 |
|------|----|----- |
| `TargetFramework` | `net10.0` | .NET 10.0 타겟 프레임워크 |
| `ImplicitUsings` | `enable` | 암시적 using 활성화 |
| `Nullable` | `enable` | Nullable 참조 형식 활성화 |
| `EnforceCodeStyleInBuild` | `true` | 빌드 시 코드 스타일 검사 |

### EnforceCodeStyleInBuild의 역할

| 설정 | IDE | 빌드 |
|------|-----|------|
| `false` (기본) | ✓ 실시간 경고 | ✗ 무시 |
| `true` | ✓ 실시간 경고 | ✓ 빌드 경고 |

<br/>

## 코드 스타일 규칙

### 지원되는 규칙

`.editorconfig`에서 설정할 수 있는 주요 규칙:

| 규칙 ID | 설명 | 예시 |
|---------|------|------|
| IDE0160 | 블록 기반 네임스페이스 사용 | `namespace Foo { }` |
| IDE0161 | 파일 기반 네임스페이스 사용 | `namespace Foo;` |
| IDE0055 | 서식 지정 규칙 | 들여쓰기, 공백 등 |

### 심각도 수준

| 수준 | 설명 | IDE 표시 | 빌드 영향 |
|------|------|----------|----------|
| `none` | 무시 | 표시 안 함 | 영향 없음 |
| `silent` | 제안 | 흐리게 표시 | 영향 없음 |
| `suggestion` | 제안 | 점선 표시 | 영향 없음 |
| `warning` | 경고 | 물결선 표시 | 경고 발생 |
| `error` | 오류 | 빨간 표시 | 빌드 실패 |

### 규칙 설정 방법

```ini
# 방법 1: 코드 스타일 규칙
csharp_style_namespace_declarations = file_scoped:warning

# 방법 2: 진단 규칙 (더 명시적)
dotnet_diagnostic.IDE0161.severity = warning

# 방법 3: 모든 IDE 규칙
dotnet_analyzer_diagnostic.category-Style.severity = warning
```

<br/>

## 빌드 시 코드 분석

### 활성화 과정

1. `.editorconfig`에 규칙 정의
2. `Directory.Build.props`에 `EnforceCodeStyleInBuild` 활성화
3. 빌드 시 자동으로 검사

### 빌드 워크플로우

```
dotnet build
    ↓
Directory.Build.props 로드
    ↓
EnforceCodeStyleInBuild 확인
    ↓
.editorconfig 규칙 적용
    ↓
코드 스타일 검사
    ↓
경고/오류 출력
```

### 검사 대상

| 파일 타입 | 검사 여부 | 규칙 |
|----------|----------|------|
| `*.cs` | ✓ | C# 코드 스타일 규칙 |
| `*.vb` | ✓ | VB.NET 코드 스타일 규칙 |
| `*.xml` | ✗ | 서식만 적용 |
| `*.md` | ✗ | 검사 안 함 |

<br/>

## 검증 방법

### 1. IDE에서 확인

Visual Studio, Rider, VS Code에서 실시간으로 경고 표시:

```csharp
// 경고: IDE0161
namespace Functorium
{
    public class Test { }
}
```

### 2. 빌드 명령어

```bash
# 기본 빌드
dotnet build

# 증분 빌드 비활성화 (설정 변경 후 권장)
dotnet build --no-incremental

# 완전히 새로 빌드
dotnet clean && dotnet build --no-incremental

# 상세 로그
dotnet build -v:n

# 경고를 오류로 처리
dotnet build /p:TreatWarningsAsErrors=true
```

#### 증분 빌드와 --no-incremental

| 빌드 방식 | 속도 | 사용 시기 |
|----------|------|----------|
| `dotnet build` | 빠름 | 일반적인 개발 |
| `dotnet build --no-incremental` | 느림 | 설정 변경 후, 의심스러운 결과 |
| `dotnet clean && dotnet build` | 가장 느림 | 완전히 새로 시작 |

**중요**: `.editorconfig`나 `Directory.Build.props`를 변경한 후에는 반드시 `--no-incremental` 옵션을 사용하세요.

### 3. 예상 출력

**정상:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**규칙 위반:**
```
Class1.cs(1,1): warning IDE0161: Convert to file-scoped namespace

Build succeeded.
    1 Warning(s)
    0 Error(s)
```

### 4. 자동 수정

일부 규칙은 자동 수정 가능:

```bash
# Visual Studio
Ctrl + . (빠른 작업)

# Rider
Alt + Enter

# VS Code
Ctrl + . (코드 작업)
```

<br/>

## 트러블슈팅

### 빌드 시 경고가 표시되지 않을 때

**원인 1**: `EnforceCodeStyleInBuild`가 활성화되지 않음

**해결**:

`Directory.Build.props`에 설정 추가:
```xml
<PropertyGroup>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
</PropertyGroup>
```

**원인 2**: 증분 빌드로 인한 캐시 문제

**해결**:

```bash
# 증분 빌드 비활성화
dotnet build --no-incremental

# 또는 완전히 새로 빌드
dotnet clean
dotnet build --no-incremental
```

### 설정 변경 후 빌드 결과가 바뀌지 않을 때

**원인**: 증분 빌드가 이전 결과를 재사용

**증상**:
- `.editorconfig` 수정했는데 경고가 그대로
- `Directory.Build.props` 변경했는데 적용 안 됨
- 코드는 수정 안 했는데 빌드는 성공

**해결**:

```bash
# 방법 1: 증분 빌드 비활성화
dotnet build --no-incremental

# 방법 2: 캐시 완전 삭제
dotnet clean
dotnet build --no-incremental

# 방법 3: bin/obj 폴더 직접 삭제
rm -rf bin obj  # Linux/Mac
rd /s /q bin obj  # Windows
dotnet build
```

**예방**:

설정 파일 변경 후 항상 `--no-incremental` 사용:
```bash
# .editorconfig 수정 후
dotnet build --no-incremental

# Directory.Build.props 수정 후
dotnet build --no-incremental
```

### IDE에서는 경고가 표시되는데 빌드에서는 안 나올 때

**원인**: IDE와 빌드가 다른 설정 사용

**해결**:

1. `.editorconfig` 위치 확인 (프로젝트 루트)
2. `root = true` 설정 확인
3. 빌드 캐시 정리:
   ```bash
   dotnet clean
   dotnet build --no-incremental
   ```

### Directory.Build.props가 적용되지 않을 때

**원인**: 파일 위치가 잘못됨

**해결**:

파일 위치 확인:
```
올바름: 프로젝트루트/Directory.Build.props
잘못됨: Src/Directory.Build.props
```

MSBuild는 프로젝트 폴더부터 상위로 올라가며 첫 번째 `Directory.Build.props`를 사용합니다.

### 특정 규칙만 비활성화하고 싶을 때

**해결**:

`.editorconfig`에서 심각도를 `none`으로 설정:

```ini
# IDE0161 규칙 비활성화
dotnet_diagnostic.IDE0161.severity = none
```

프로젝트별로 다르게 설정하려면 `.csproj`에서 재정의:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);IDE0161</NoWarn>
</PropertyGroup>
```

### 경고가 너무 많을 때

**원인**: 레거시 코드에 새 규칙 적용

**해결 방법 1**: 점진적 적용

```xml
<!-- 새 파일만 검사 -->
<PropertyGroup>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <!-- 기존 경고 무시 -->
  <NoWarn>$(NoWarn);IDE0161</NoWarn>
</PropertyGroup>
```

**해결 방법 2**: 심각도 낮추기

```ini
# 경고 대신 제안
csharp_style_namespace_declarations = file_scoped:suggestion
```

**해결 방법 3**: `.editorconfig` 계층 구조

```
프로젝트루트/.editorconfig          # 엄격한 규칙
Src/Legacy/.editorconfig            # 완화된 규칙
```

<br/>

## FAQ

### Q1. EditorConfig와 Directory.Build.props의 차이는 무엇인가요?

| 항목 | EditorConfig | Directory.Build.props |
|------|--------------|----------------------|
| 목적 | 코드 스타일 규칙 정의 | 빌드 설정 정의 |
| 형식 | INI 형식 | XML 형식 |
| 적용 범위 | IDE + 빌드 | 빌드만 |
| 예시 | 네임스페이스 스타일 | TargetFramework |

**함께 사용**: EditorConfig가 "무엇을" 검사할지 정의하고, Directory.Build.props가 "언제" 검사할지 결정합니다.

### Q2. EnforceCodeStyleInBuild를 활성화하면 빌드가 느려지나요?

**A:** 네, 약간 느려질 수 있습니다.

| 프로젝트 크기 | 예상 증가 시간 |
|--------------|---------------|
| 소형 (10개 파일) | +0.5초 |
| 중형 (100개 파일) | +2~3초 |
| 대형 (1000개 파일) | +10~20초 |

**권장**:
- 로컬 개발: 활성화
- CI/CD: 활성화 (코드 품질 보장)
- 빠른 테스트: 비활성화 가능

### Q3. 파일 기반 네임스페이스를 사용해야 하는 이유는?

**A:** 여러 장점이 있습니다:

**파일 기반 (권장):**
```csharp
namespace Functorium;  // 1줄

public class Calculator
{
    public int Add(int a, int b) => a + b;
}
```

**블록 기반 (비권장):**
```csharp
namespace Functorium  // 네임스페이스 블록
{
    public class Calculator  // 추가 들여쓰기
    {
        public int Add(int a, int b) => a + b;
    }
}  // 닫는 괄호
```

| 장점 | 설명 |
|------|------|
| 간결함 | 코드 줄 수 감소 |
| 가독성 | 불필요한 들여쓰기 제거 |
| 일관성 | C# 10+ 표준 스타일 |

### Q4. 모든 IDE 규칙을 한 번에 활성화할 수 있나요?

**A:** 네, 카테고리별로 활성화 가능합니다:

```ini
# 모든 스타일 규칙
dotnet_analyzer_diagnostic.category-Style.severity = warning

# 모든 성능 규칙
dotnet_analyzer_diagnostic.category-Performance.severity = warning

# 모든 보안 규칙
dotnet_analyzer_diagnostic.category-Security.severity = error
```

**주의**: 처음부터 모두 활성화하면 경고가 많을 수 있습니다. 점진적으로 적용하세요.

### Q5. Directory.Build.props는 어디에 위치해야 하나요?

**A:** 솔루션 루트 또는 소스 루트에 배치합니다:

```
방법 1 (권장): 솔루션 루트
SolutionRoot/
├── Directory.Build.props    ← 여기
├── Functorium.sln
└── Src/
    ├── Project1/
    └── Project2/

방법 2: 소스 루트
SolutionRoot/
├── Functorium.sln
└── Src/
    ├── Directory.Build.props  ← 또는 여기
    ├── Project1/
    └── Project2/
```

MSBuild는 프로젝트에서 상위 폴더로 올라가며 첫 번째 파일을 찾습니다.

### Q6. 특정 프로젝트에서만 다른 설정을 사용하려면?

**A:** 프로젝트 파일에서 설정을 재정의합니다:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>

<!-- 특정 프로젝트.csproj -->
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>  <!-- 재정의 -->
</PropertyGroup>
```

프로젝트 파일의 설정이 우선순위가 높습니다.

### Q7. CI/CD에서 코드 스타일 검사를 강제하려면?

**A:** 경고를 오류로 처리하도록 설정합니다:

**방법 1**: Directory.Build.props
```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

**방법 2**: 빌드 명령어
```bash
dotnet build /p:TreatWarningsAsErrors=true
```

**방법 3**: CI 스크립트
```yaml
# GitHub Actions 예시
- name: Build
  run: dotnet build --configuration Release /p:TreatWarningsAsErrors=true
```

### Q8. .editorconfig의 설정이 IDE에 즉시 적용되지 않을 때?

**A:** IDE를 재시작하거나 캐시를 정리하세요:

**Visual Studio:**
- 솔루션 닫기 → 재열기
- 또는: Tools → Options → Text Editor → C# → Advanced → "Reanalyze Entire Solution"

**Rider:**
- File → Invalidate Caches / Restart

**VS Code:**
- Reload Window (Ctrl+Shift+P → "Reload Window")

### Q8-1. 증분 빌드(Incremental Build)란 무엇인가요?

**A:** 변경된 파일만 다시 컴파일하여 빌드 속도를 높이는 기능입니다.

| 항목 | 전체 빌드 | 증분 빌드 |
|------|----------|----------|
| 속도 | 느림 | 빠름 |
| 정확성 | 항상 정확 | 캐시 문제 가능 |
| 사용 | 설정 변경 후 | 일반 개발 |

**증분 빌드 동작:**
```
첫 빌드: File1.cs ✓ File2.cs ✓ File3.cs ✓
File1.cs 수정 후: File1.cs ✓ File2.cs (캐시) File3.cs (캐시)
```

**문제 상황:**
```bash
# .editorconfig 수정
vim .editorconfig

# 증분 빌드 - 설정 변경 감지 못함
dotnet build  # ✗ 이전 결과 사용

# 증분 빌드 비활성화 - 모든 파일 재검사
dotnet build --no-incremental  # ✓ 새 설정 적용
```

**언제 --no-incremental을 사용해야 하나요?**

| 상황 | --no-incremental 필요 |
|------|---------------------|
| 코드 수정 | ✗ 불필요 |
| .editorconfig 수정 | ✓ 필요 |
| Directory.Build.props 수정 | ✓ 필요 |
| NuGet 패키지 업데이트 | △ 권장 |
| 빌드 결과가 이상할 때 | ✓ 필요 |

### Q9. 코드 스타일 규칙을 팀 전체에 적용하려면?

**A:** 다음 파일을 버전 관리에 포함하세요:

```bash
git add .editorconfig
git add Directory.Build.props
git commit -m "chore: 코드 품질 설정 추가"
git push
```

팀원들에게 안내:
1. 최신 코드 pull
2. IDE 재시작
3. `dotnet build`로 검증

### Q10. 어떤 규칙부터 적용해야 하나요?

**A:** 단계적으로 적용하세요:

**1단계 (필수):**
```ini
# 네임스페이스
csharp_style_namespace_declarations = file_scoped:warning
```

**2단계 (권장):**
```ini
# Nullable 참조 형식
csharp_style_prefer_null_check_over_type_check = true:warning
```

**3단계 (선택):**
```ini
# 코드 포맷팅
csharp_new_line_before_open_brace = all
csharp_indent_case_contents = true
```

각 단계마다 팀 전체가 적응한 후 다음 단계로 진행하세요.

## 참고 문서

- [EditorConfig 공식 문서](https://editorconfig.org/)
- [.NET 코드 스타일 규칙](https://learn.microsoft.com/ko-kr/dotnet/fundamentals/code-analysis/style-rules/)
- [MSBuild Directory.Build.props](https://learn.microsoft.com/ko-kr/visualstudio/msbuild/customize-by-directory)
- [IDE0161: 파일 범위 네임스페이스](https://learn.microsoft.com/ko-kr/dotnet/fundamentals/code-analysis/style-rules/ide0161)
