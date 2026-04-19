# CLAUDE.md

## 프로젝트 정체성

Functorium은 **Functional DDD + Observability** .NET 프레임워크다. LanguageExt 기반 함수형 타입 시스템(`Fin<T>`, `FinT<IO,T>`, `Validation<Error,T>`)으로 도메인 로직을 순수 함수로 표현하고, OpenTelemetry 통합 관측성을 제공한다.

핵심 철학:
- **코드가 비즈니스를 말한다** — DDD 전술 패턴(Entity, AggregateRoot, ValueObject, Specification)
- **운영이 코드를 읽는다** — `ctx.*` 컨텍스트 전파 기반 3-Pillar 관측성(Logging, Tracing, Metrics)
- **예외 대신 타입으로 실패를 표현한다** — `Fin<T>` Railway-Oriented Programming

---

## 아키텍처

### 프로젝트 구조와 의존성 방향

```
Functorium                  ← 핵심 (Domain + Application + Abstractions)
  ↑
Functorium.Adapters         ← 인프라 (Repository, Pipeline, Observability)
  ↑
Functorium.Testing          ← 테스트 유틸리티 라이브러리

Functorium.SourceGenerators ← 빌드 타임 코드 생성 (독립, netstandard2.0)
```

**의존성 규칙**: 위에서 아래로만 참조. 역방향 참조 금지.

### Functorium 내부 레이어

```
Src/Functorium/
├── Abstractions/     ← 레이어 간 공유 지원 관심사 (에러 인프라, 관측성 계약, 유틸리티)
├── Domains/          ← 순수 도메인 레이어 (Entity, ValueObject, Specification, DomainEvent)
└── Applications/     ← 유스케이스 레이어 (CQRS, Persistence Port, Query Port, Validation)
```

### Functorium.Adapters 내부 구조

```
Src/Functorium.Adapters/
├── Abstractions/     ← 지원 인프라 (에러 구조화, DI 등록, 옵션 설정)
├── Errors/           ← Adapter 레이어 에러 타입
├── Events/           ← 도메인 이벤트 발행/수집 + Observable 래퍼
├── Observabilities/  ← OpenTelemetry 빌더, 네이밍, 컨텍스트, 로거
├── Pipelines/        ← 유스케이스 파이프라인 (Logging, Metrics, Tracing, Validation, Exception, Caching, Transaction)
├── Repositories/     ← EF Core, Dapper, InMemory 구현체
└── SourceGenerators/  ← Observable Port 생성 어트리뷰트
```

---

## 설계 원칙

### 1. 함수형 우선

- 모든 실패는 `Fin<T>` 또는 `FinT<IO,T>`로 표현. 예외를 throw하지 않는다.
- 검증은 `Validation<Error,T>`로 모든 에러를 수집한다 (첫 번째 에러에서 중단하지 않음).
- `from ... in ... select` LINQ 구문으로 모나드를 합성한다.
- `Apply`/`ApplyT` 패턴으로 여러 검증 결과를 결합한다.

### 2. 레이어별 타입 안전 에러

```csharp
// Domain — DomainErrorType sealed record 사용
DomainError.For<Email>(new DomainErrorType.Empty(), value, "이메일은 비어있을 수 없습니다");
// → 에러 코드: "DomainErrors.Email.Empty"

// Application — ApplicationErrorType sealed record 사용
ApplicationError.For<CreateProductCommand>(new ApplicationErrorType.AlreadyExists(), id, "이미 존재합니다");
// → 에러 코드: "ApplicationErrors.CreateProductCommand.AlreadyExists"

// Adapter — AdapterErrorType sealed record 사용
AdapterError.For<ProductRepository>(new AdapterErrorType.NotFound(), id, "찾을 수 없습니다");
// → 에러 코드: "AdapterErrors.ProductRepository.NotFound"
```

에러 코드 형식: `{LayerPrefix}.{Context}.{ErrorName}`

커스텀 에러 확장: `public sealed record InsufficientStock : DomainErrorType.Custom;`

### 3. 파이프라인 Opt-in

파이프라인은 기본 비활성화. 명시적으로 활성화해야 한다.

```csharp
services.RegisterOpenTelemetry(configuration, assembly)
    .ConfigurePipelines(p => p
        .UseObservability()   // CtxEnricher + Metrics + Tracing + Logging
        .UseValidation()
        .UseException()
        .UseTransaction())
    .Build();
```

`UseAll()`, `ConfigurePipelines()` (무인자), `AddCustomPipelinesFromAssembly()` — 모두 제거됨.

### 4. 관측성 `ctx.*` 컨텍스트 전파

- `CtxPillar` 플래그: `Logging | Tracing | MetricsTag | MetricsValue`
- `CtxEnricherContext.Push(name, value, pillars)` — 3-Pillar에 동시 전파
- `[CtxRoot]`, `[CtxTarget]`, `[CtxIgnore]` 어트리뷰트 — Source Generator가 Enricher 자동 생성
- CtxEnricher는 Metrics/Tracing/Logging 중 하나라도 활성화되면 자동 포함

### 5. 내부 구현 공유 패턴

- `LayerErrorCore` — DomainError/ApplicationError/AdapterError의 공통 에러 생성 로직 (internal)
- `ErrorCodeExpectedBase` — ErrorCodeExpected 4개 타입의 공통 override 기반 클래스 (internal, `sealed override ToString()`)
- `ErrorAssertionCore` — 3개 레이어 Assertion의 공통 검증 로직 (internal)

---

## 빌드 & 테스트

### 솔루션 파일

| 솔루션 파일 | 용도 |
|-------------|------|
| `Functorium.slnx` | 핵심 라이브러리 개발 (Src/, Tests/) |
| `Docs.Site/.../tutorials/<name>/<PascalName>.slnx` | 튜토리얼 실습 코드 |
| `Docs.Site/.../samples/<name>/<PascalName>.slnx` | 예제 실습 코드 |
| `Docs.Site/.../quickstart/Quickstart.slnx` | 퀵스타트 코드 |

### 명령어

```bash
# 빌드
dotnet build Functorium.slnx

# 테스트 (--solution 필수)
dotnet test --solution Functorium.slnx

# 튜토리얼 빌드/테스트
dotnet build Docs.Site/src/content/docs/tutorials/<name>/<PascalName>.slnx
dotnet test --solution Docs.Site/src/content/docs/tutorials/<name>/<PascalName>.slnx
```

### 빌드 스크립트 (PowerShell 7+)

| 스크립트 | 용도 |
|----------|------|
| `Build-Local.ps1` | 빌드, 테스트, 코드 커버리지, NuGet 패키지 생성 |
| `Build-Clean.ps1` | bin/obj 정리 |
| `Build-CleanRunFileCache.ps1` | .NET run-file 캐시 정리 |
| `Build-SetAsSetupProject.ps1` | VSCode launch.json/tasks.json 설정 |
| `Build-VerifyAccept.ps1` | Verify.Xunit 스냅샷 승인 |
| `Build-ERDiagram.ps1` | Mermaid ER 다이어그램 생성 |

```powershell
./Build-Local.ps1                          # 전체 빌드 파이프라인
./Build-Local.ps1 -s <path>.slnx           # 특정 솔루션
./Build-Local.ps1 -SkipPack                # NuGet 패키지 생성 생략
```

도움말: `Get-Help ./Build-Local.ps1 -Examples` (표준 PowerShell 도움말)

---

## 코딩 규칙

### 네이밍

| 대상 | 규칙 | 예시 |
|------|------|------|
| ErrorType 카테고리 파일 | `{Layer}ErrorType.{Category}.cs` | `DomainErrorType.Presence.cs` |
| Helper 클래스 | `{Layer}Error.cs` | `DomainError.cs` |
| Assertion 클래스 | `{Layer}ErrorAssertions.cs` | `DomainErrorAssertions.cs` |
| Pipeline | `Usecase{Concern}Pipeline.cs` | `UsecaseLoggingPipeline.cs` |
| Repository | `{ORM}{Pattern}Base.cs` | `EfCoreRepositoryBase.cs` |
| Partial 파일 | `{ClassName}.{Category}.cs` | `ObservabilityNaming.Attributes.cs` |

### 패턴

- **ErrorType**: `abstract partial record` + 카테고리별 partial 파일로 분리
- **Helper 클래스**: `LayerErrorCore`에 위임하는 thin wrapper (`[AggressiveInlining]`)
- **Assertion**: `ErrorAssertionCore`에 위임하는 thin wrapper
- **Repository**: 추상 base 클래스 + `ToDomain`/`ToModel` abstract 메서드
- **Value Object**: `Validate` 정적 팩토리 메서드 → `Fin<T>` 반환
- **Entity**: private 생성자 + `Create` 정적 팩토리 → `Fin<T>` 반환
- **ID 타입**: `[GenerateEntityId]` 어트리뷰트로 Ulid 기반 자동 생성

### 금지사항

- `throw` 사용 금지 — `Fin.Fail<T>(error)` 사용
- 레이어 역방향 참조 금지 — Domains → Applications (단방향만)
- `DomainError.For`에 다른 레이어 ErrorType 전달 금지 — 컴파일 타임 강제
- `new` 키워드로 Entity/ValueObject 직접 생성 금지 — 팩토리 메서드 사용
- Pipeline에서 `UseAll()` 사용 금지 — 제거됨, 명시적 opt-in만
- `.api/` 폴더 파일 수동 수정 금지 — 빌드 시 자동 생성되는 Public API Surface 파일. `dotnet build`가 자동 갱신하므로 직접 편집하지 않는다

---

## Git 규칙

### 커밋

커밋 시 `.claude/commands/commit.md` 규칙을 준수한다. Conventional Commits 규격 사용.

### Git Hooks

`.githooks/` 디렉토리에 커밋 메시지 정리 hook이 있다.

```bash
# 필수 확인
git config core.hooksPath   # .githooks를 가리키는지 확인
git config core.hooksPath .githooks  # 설정
```

`.githooks/commit-msg`: Claude/AI 관련 텍스트를 커밋 메시지에서 자동 제거.

### Markdown 볼드 규칙

`**텍스트(...)**` 뒤에 한글 조사가 바로 오면 GitHub에서 볼드가 렌더링되지 않는다. 한글 조사를 볼드 안에 포함시킨다.

```markdown
# Bad
**공변성(Covariance)**은

# Good
**공변성(Covariance)은**
```

---

## 릴리스

### 버전 관리 (MinVer)

Git 태그에서 버전을 자동 추출한다. 태그 prefix: `v`

| Git 태그 | NuGet 버전 |
|---------|-----------|
| `v1.0.0-alpha.2` | `1.0.0-alpha.2` |
| `v1.0.0` | `1.0.0` |

### 배포 대상 (4개 NuGet 패키지)

- `Functorium` — 핵심 Domain + Application
- `Functorium.Adapters` — 인프라 (Repository, Pipeline, Observability)
- `Functorium.SourceGenerators` — 빌드 타임 코드 생성
- `Functorium.Testing` — 테스트 유틸리티

### 릴리스 노트 구조

```
.release-notes/
├── v1/
│   ├── v1.0.0-alpha.1/
│   │   ├── RELEASE-v1.0.0-alpha.1.md     (EN)
│   │   └── RELEASE-v1.0.0-alpha.1-KR.md  (KR)
│   └── v1.0.0-alpha.2/
│       ├── RELEASE-v1.0.0-alpha.2.md
│       └── RELEASE-v1.0.0-alpha.2-KR.md
├── scripts/
└── TEMPLATE.md
```

### CI/CD

- **태그 push** → `publish.yml` 실행 → 빌드, 테스트, NuGet Pack, NuGet.org Push, GitHub Release 생성
- **main push (Docs.Site 변경 시)** → `docs-site.yml` 실행 → Astro 빌드, GitHub Pages 배포
- **main push** → `build.yml` 실행 → 빌드, 테스트

---

## InternalsVisibleTo

| 프로젝트 | 내부 접근 허용 대상 |
|---------|-------------------|
| `Functorium` | Functorium.Adapters, Functorium.Tests.Unit, Functorium.Testing |
| `Functorium.Adapters` | Functorium.Tests.Unit, Functorium.Testing |
