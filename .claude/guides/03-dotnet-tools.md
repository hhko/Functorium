# .NET 도구 가이드

## 목차

- [개요](#개요)
- [CLI 도구 (.config/dotnet-tools.json)](#cli-도구-configdotnet-toolsjson)
- [소스 생성기](#소스-생성기)
- [.NET 10 파일 기반 프로그램](#net-10-파일-기반-프로그램)
- [도구 전체 맵](#도구-전체-맵)
- [새 도구 추가 체크리스트](#새-도구-추가-체크리스트)
- [트러블슈팅](#트러블슈팅)
- [참고 문서](#참고-문서)

---

## 개요

이 가이드는 프로젝트에서 사용하는 .NET 도구의 **상세 사용법**을 다룹니다. 도구는 세 가지 카테고리로 분류됩니다.

| 카테고리 | 설명 | 예시 |
|---------|------|------|
| CLI 도구 | `dotnet tool` 매니페스트 관리 | ReportGenerator, Verify.Tool, Siren |
| 소스 생성기 | 컴파일 시 코드 자동 생성 | EntityIdGenerator, ObservablePortGenerator |
| .NET 10 스크립트 | `.cs` 파일 직접 실행 | SummarizeSlowestTests, ApiGenerator |

> **02-solution-configuration.md와의 관계**: `dotnet-tools.json` 매니페스트 생성/관리 방법과 빌드 스크립트 파이프라인 개요는 [02-solution-configuration.md](./02-solution-configuration.md)를 참조하세요. 이 문서는 각 도구의 목적, 명령어, 파라미터, 실행 예시를 다룹니다.

## CLI 도구 (.config/dotnet-tools.json)

### 도구 관리 기본

CLI 도구는 `.config/dotnet-tools.json` 매니페스트로 관리됩니다. 매니페스트 생성, 도구 설치/복원 방법은 [02-solution-configuration.md §.config/dotnet-tools.json](./02-solution-configuration.md#configdotnet-toolsjson)을 참조하세요.

**rollForward 설정:**

| 값 | 동작 | 사용 시기 |
|----|------|----------|
| `false` (기본) | 도구의 대상 프레임워크와 정확히 일치하는 런타임 필요 | 도구와 SDK 버전이 일치할 때 |
| `true` | 상위 버전 런타임에서 실행 허용 | 도구가 이전 버전 대상일 때 (예: .NET 9 도구를 .NET 10 SDK에서 실행) |

> `Build-Local.ps1` Step 1에서 `dotnet tool restore`를 자동으로 수행합니다.

### ReportGenerator (코드 커버리지)

| 항목 | 값 |
|------|-----|
| 패키지 | `dotnet-reportgenerator-globaltool` |
| 명령 | `reportgenerator` |
| 용도 | Cobertura XML → HTML/Markdown 커버리지 리포트 변환 |

**독립 실행:**

```powershell
dotnet reportgenerator `
  -reports:.coverage/reports/**/*.cobertura.xml `
  -targetdir:.coverage/reports/html `
  -reporttypes:"Html;Cobertura;MarkdownSummaryGithub"
```

**주요 파라미터:**

| 파라미터 | 설명 | 예시 |
|---------|------|------|
| `-reports` | 입력 커버리지 파일 (glob) | `**/*.cobertura.xml` |
| `-targetdir` | 출력 디렉토리 | `.coverage/reports/html` |
| `-reporttypes` | 리포트 형식 | `Html;Cobertura;MarkdownSummaryGithub` |
| `-assemblyfilters` | 어셈블리 포함/제외 | `+MyApp*;-*.Tests*` |
| `-filefilters` | 소스 파일 포함/제외 | `-**/AssemblyReference.cs` |

> `Build-Local.ps1` Step 7에서 자동으로 HTML + Cobertura + Markdown 리포트를 생성합니다.

### Verify.Tool (스냅샷 관리)

| 항목 | 값 |
|------|-----|
| 패키지 | `verify.tool` |
| 명령 | `dotnet-verify` |
| 용도 | Verify.Xunit 스냅샷 `.received` → `.verified` 승인 |

**실행:**

```powershell
dotnet verify accept -y
```

**사용 시기:**
- 스냅샷 테스트 실행 후 `*.received.*` 파일이 생성되었을 때
- 의도적으로 출력이 변경되어 새 스냅샷을 승인해야 할 때

> `Build-VerifyAccept.ps1`이 이 명령을 자동으로 수행합니다.

### Siren (ER 다이어그램)

| 항목 | 값 |
|------|-----|
| 패키지 | `gman.siren` |
| 명령 | `siren-gen` |
| 용도 | EF Core DbContext → Mermaid ER 다이어그램 생성 |
| rollForward | `true` (.NET 9 도구를 .NET 10에서 실행) |

**입력 모드:**

| 모드 | 플래그 | 설명 |
|------|--------|------|
| 어셈블리 | `-a <dll 경로>` | Migration이 포함된 어셈블리에서 스키마 추출 |
| 연결 문자열 | `-c <connection string>` | 기존 데이터베이스에서 스키마 읽기 |

**실행 예시:**

```powershell
# 어셈블리 모드 (Migration 사용 프로젝트)
dotnet siren-gen `
  -a bin/Release/net10.0/MyApp.Persistence.dll `
  -o ER-Diagram.md

# 연결 문자열 모드 (기존 DB)
dotnet siren-gen `
  -c "Data Source=myapp.db" `
  -o ER-Diagram.md
```

**주요 파라미터:**

| 파라미터 | 설명 |
|---------|------|
| `-o, --outputPath` | 출력 Markdown 파일 경로 (필수) |
| `-a, --assemblyPath` | Migration 어셈블리 DLL 경로 |
| `-c, --connectionString` | 데이터베이스 연결 문자열 |
| `-f, --filterEntities` | 포함할 Entity 이름 필터 (쉼표 구분) |
| `-s, --skipEntities` | 제외할 Entity 이름 (쉼표 구분) |
| `-h, --filterSchemas` | 포함할 스키마 필터 |
| `-x, --skipSchemas` | 제외할 스키마 |
| `-t, --template` | 렌더링 템플릿 (기본: `default`) |

**제약 사항:**
- 어셈블리 모드(`-a`): EF Core Migrations 필수. `EnsureCreated()` 패턴 프로젝트에서는 동작하지 않음
- 연결 문자열 모드(`-c`): SQL Server 전용. SQLite/InMemory 미지원

> Siren은 Mermaid 다이어그램을 이미지로 렌더링하는 범용 도구이지만, 여기서는 EF Core → Mermaid ER 다이어그램 생성 기능만 사용합니다.

이 제약으로 인해 프로젝트에서는 `Build-ERDiagram.ps1` 스크립트(§Build-ERDiagram.ps1 참조)로 EF Core Configuration 기반 Mermaid ER 다이어그램을 직접 생성합니다. 예시: [Tests.Hosts/01-SingleHost/ER-Diagram.md](../../Tests.Hosts/01-SingleHost/ER-Diagram.md)

### Build-ERDiagram.ps1 (ER 다이어그램 직접 생성)

| 항목 | 값 |
|------|-----|
| 위치 | `Tests.Hosts/01-SingleHost/Build-ERDiagram.ps1` |
| 용도 | EF Core Configuration 기반 Mermaid ER 다이어그램 생성 |
| 출력 | `Tests.Hosts/01-SingleHost/ER-Diagram.md` |

Siren 도구의 제약(Migration 필수 또는 SQL Server 전용)을 우회하여, 스크립트 내부에 정의된 ER 다이어그램 템플릿을 `ER-Diagram.md`로 출력합니다. 스키마 변경 시 스크립트 내부의 `$erDiagram` 변수를 수동으로 업데이트해야 합니다.

**실행:**

```powershell
# Tests.Hosts/01-SingleHost/ 디렉토리에서 실행
./Build-ERDiagram.ps1

# 도움말
./Build-ERDiagram.ps1 -Help
```

**참조 파일**: EF Core Configuration 변경 시 다음 파일을 참고하여 스크립트를 업데이트하세요:
- `Src/LayeredArch.Adapters.Persistence/Repositories/EfCore/Configurations/`

## 소스 생성기

### Functorium.SourceGenerators (자체)

| 항목 | 값 |
|------|-----|
| 프로젝트 | `Src/Functorium.SourceGenerators` |
| 대상 | `netstandard2.0` (Roslyn 요구사항) |
| NuGet 패키징 | `analyzers/dotnet/cs` 경로에 배치 |

**제공 생성기:**

| 생성기 | 트리거 어트리뷰트 | 생성 결과 |
|--------|------------------|----------|
| `EntityIdGenerator` | `[GenerateEntityId]` | EntityId struct + EF Core Converter/Comparer |
| `ObservablePortGenerator` | `[GenerateObservablePort]` | Observability 래핑 Pipeline 클래스 |

#### EntityIdGenerator

`[GenerateEntityId]`를 Entity/AggregateRoot 클래스에 적용하면 Ulid 기반 EntityId를 자동 생성합니다.

```csharp
[GenerateEntityId]
public sealed class Product : AggregateRoot<ProductId> { ... }
```

**생성되는 코드:**
- `ProductId` record struct — `IEntityId<ProductId>`, `IParsable<ProductId>` 구현
- `ProductIdConverter` — EF Core `ValueConverter<ProductId, string>`
- `ProductIdComparer` — EF Core `ValueComparer<ProductId>`
- JSON 직렬화/역직렬화 (`JsonConverter`)
- 비교 연산자 (`<`, `>`, `<=`, `>=`)

#### ObservablePortGenerator

`[GenerateObservablePort]`을 IObservablePort 구현 클래스에 적용하면 Observability 래핑 Pipeline을 자동 생성합니다.

```csharp
[GenerateObservablePort]
public class EfCoreProductRepository : IProductRepository { ... }
```

**생성되는 코드:**
- `EfCoreProductRepositoryPipeline` 클래스 — 원본 클래스를 상속
- 각 메서드를 override하여 추가:
  - `ActivitySource` 분산 추적 (span 생성)
  - `ILogger` 구조화된 로깅 (요청/응답/에러)
  - `IMeterFactory` 메트릭 (카운터, 히스토그램)
  - 에러 분류 (Expected vs Exceptional)

### Mediator.SourceGenerator

| 항목 | 값 |
|------|-----|
| 패키지 | `Mediator.SourceGenerator` (v3.0.1) |
| 용도 | Mediator 패턴 핸들러 코드 자동 생성 |

**주의 사항:** 테스트 프로젝트에서 호스트 프로젝트를 참조할 때, Mediator.SourceGenerator가 중복 실행되어 빌드 오류가 발생할 수 있습니다. 이 경우 호스트 프로젝트 참조에 `ExcludeAssets="analyzers"`를 추가합니다.

```xml
<ProjectReference Include="..." ExcludeAssets="analyzers" />
```

## .NET 10 파일 기반 프로그램

.NET 10은 `.cs` 파일을 직접 실행하는 "파일 기반 프로그램"을 지원합니다. NuGet 의존성은 `#:package` 지시문으로 선언합니다.

> 각 스크립트 폴더에는 루트 `Directory.Build.props`의 Source Link 의존성을 차단하는 자체 `Directory.Build.props`가 있습니다. 자세한 내용은 [02-solution-configuration.md §중첩 구성 파일](./02-solution-configuration.md#중첩-구성-파일)을 참조하세요.

### SummarizeSlowestTests.cs

| 항목 | 값 |
|------|-----|
| 위치 | `.coverage/scripts/SummarizeSlowestTests.cs` |
| 용도 | TRX 테스트 결과에서 느린 테스트 분석 리포트 생성 |
| NuGet | `System.CommandLine`, `Microsoft.Extensions.FileSystemGlobbing` |

**생성하는 리포트:**
- 전체 테스트 통계 (passed, failed, skipped)
- 테스트 프로젝트별 실행 시간 분포
- 상위 100개 느린 테스트 목록
- 실패 테스트 요약
- 백분위 분석 (50th, 90th, 95th, 99th)

**실행:**

```powershell
dotnet .coverage/scripts/SummarizeSlowestTests.cs `
  --glob "**/*.trx" `
  --threshold 30 `
  --output .coverage/reports
```

**주요 파라미터:**

| 파라미터 | 설명 | 기본값 |
|---------|------|--------|
| `--glob` | TRX 파일 검색 패턴 | — |
| `--threshold` | 느린 테스트 판단 기준 (초) | 30 |
| `--output` | 리포트 출력 디렉토리 | — |

> `Build-Local.ps1` Step 9에서 자동으로 수행됩니다.

### ApiGenerator.cs

| 항목 | 값 |
|------|-----|
| 위치 | `.release-notes/scripts/ApiGenerator.cs` |
| 용도 | 컴파일된 DLL에서 Public API 표면 텍스트 생성 |
| NuGet | `PublicApiGenerator` |

**동작:**
1. 지정된 DLL의 Public API를 추출
2. .NET 10 / ASP.NET Core 참조 어셈블리를 자동으로 해석
3. 텍스트 또는 파일로 API 정의 출력

**실행:**

```powershell
dotnet .release-notes/scripts/ApiGenerator.cs `
  --dll-path bin/Release/net10.0/MyLib.dll `
  --output-path .api/MyLib.cs
```

### ExtractApiChanges.cs

| 항목 | 값 |
|------|-----|
| 위치 | `.release-notes/scripts/ExtractApiChanges.cs` |
| 용도 | 브랜치 간 API 변경 사항 추출 (릴리스 노트용) |
| NuGet | `System.CommandLine`, `Spectre.Console` |

**동작:**
1. Functorium 소스 프로젝트를 검색 (테스트 제외)
2. 각 프로젝트를 Release 모드로 publish
3. `ApiGenerator.cs`를 호출하여 API 파일 생성
4. 전체 API를 하나의 uber 파일로 병합
5. Git diff로 변경 사항 추출
6. 요약 리포트 생성

**출력:** `.analysis-output/api-changes-build-current/`

## 도구 전체 맵

### CLI 도구

| 패키지 | 명령 | 용도 | Build-Local.ps1 단계 |
|--------|------|------|---------------------|
| `dotnet-reportgenerator-globaltool` | `reportgenerator` | 커버리지 리포트 | Step 7 |
| `verify.tool` | `dotnet-verify` | 스냅샷 승인 | Build-VerifyAccept.ps1 |
| `gman.siren` | `siren-gen` | ER 다이어그램 | 수동 실행 |

### 소스 생성기

| 생성기 | 어트리뷰트 | 프로젝트 |
|--------|-----------|---------|
| EntityIdGenerator | `[GenerateEntityId]` | Functorium.SourceGenerators |
| ObservablePortGenerator | `[GenerateObservablePort]` | Functorium.SourceGenerators |
| Mediator.SourceGenerator | 인터페이스 기반 | NuGet (v3.0.1) |

### .NET 10 파일 기반 프로그램

| 파일 | 용도 | Build-Local.ps1 단계 |
|------|------|---------------------|
| `.coverage/scripts/SummarizeSlowestTests.cs` | 느린 테스트 분석 | Step 9 |
| `.release-notes/scripts/ApiGenerator.cs` | Public API 표면 생성 | 수동/릴리스 |
| `.release-notes/scripts/ExtractApiChanges.cs` | API 변경 사항 추출 | 수동/릴리스 |

## 새 도구 추가 체크리스트

### CLI 도구 추가

1. `dotnet tool install <package-name>` (매니페스트에 자동 등록)
2. `.config/dotnet-tools.json`에서 `rollForward` 설정 확인 (도구 대상 프레임워크가 현재 SDK보다 낮으면 `true`)
3. [02-solution-configuration.md](./02-solution-configuration.md)의 도구 목록 테이블 업데이트
4. 이 문서에 상세 사용법 섹션 추가

### 소스 생성기 추가

1. `Directory.Packages.props`에 `<PackageVersion>` 추가
2. 사용 프로젝트 `.csproj`에 `<PackageReference>` 추가
3. 테스트 프로젝트 참조 시 `ExcludeAssets="analyzers"` 필요 여부 확인

### .NET 10 스크립트 추가

1. 적절한 디렉토리에 `.cs` 파일 생성
2. `#:package` 지시문으로 NuGet 의존성 선언
3. 해당 폴더에 `Directory.Build.props` 존재 여부 확인 (루트 props 차단 필요 시 생성)
4. `Build-CleanRunFileCache.ps1`의 대상 패턴 업데이트 고려

## 트러블슈팅

### rollForward 관련 오류

**증상:** `dotnet tool restore` 시 "The tool ... is not supported on the current .NET SDK" 오류

**해결:** `.config/dotnet-tools.json`에서 해당 도구의 `rollForward`를 `true`로 설정합니다.

```json
"tool-name": {
  "version": "x.y.z",
  "commands": ["cmd"],
  "rollForward": true
}
```

### .NET 10 스크립트 패키지 로딩 오류

**증상:** `System.CommandLine` 등 패키지가 로드되지 않거나, 이전 버전 캐시가 사용됨

**해결:** `Build-CleanRunFileCache.ps1`로 runfile 캐시를 정리합니다.

```powershell
# 특정 스크립트 캐시 정리
./Build-CleanRunFileCache.ps1

# 모든 runfile 캐시 정리
./Build-CleanRunFileCache.ps1 -Pattern "All"

# 삭제 대상만 확인
./Build-CleanRunFileCache.ps1 -WhatIf
```

캐시 위치: `%TEMP%\dotnet\runfile\`

### Siren 어셈블리 모드 실패

**증상:** `siren-gen -a <dll>` 실행 시 NullReferenceException 또는 빈 결과

**원인:** EF Core Migrations를 사용하지 않는 프로젝트(`EnsureCreated()` 패턴)에서는 어셈블리 모드가 동작하지 않을 수 있습니다.

**해결:**
1. 연결 문자열 모드 사용: 먼저 DB를 생성한 후 `siren-gen -c "Data Source=..."` 실행
2. Mermaid ER 다이어그램 직접 작성 (수동 대안)

## 참고 문서

- [02-solution-configuration.md](./02-solution-configuration.md) — dotnet-tools.json 관리, 빌드 스크립트 파이프라인
- [01-project-structure.md](./01-project-structure.md) — 프로젝트 구조 및 의존성
- [15-unit-testing.md](./15-unit-testing.md) — 단위 테스트 (Verify.Xunit 스냅샷 포함)
- [16-testing-library.md](./16-testing-library.md) — Functorium.Testing 라이브러리 (소스 생성기 테스트 포함)
