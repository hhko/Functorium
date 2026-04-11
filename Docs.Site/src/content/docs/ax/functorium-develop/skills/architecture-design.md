---
title: "Architecture Design"
description: "Project structure and infrastructure design"
---

> project-spec -> **architecture-design** -> domain-develop -> application-develop -> adapter-develop -> observability-develop -> test-develop

## 선행 조건

`project-spec` 스킬에서 생성한 `00-project-spec.md`가 있으면 자동으로 읽어 Aggregate 후보와 유스케이스 개요를 확인합니다. 없으면 사용자에게 프로젝트 범위를 직접 질문합니다.

## 배경

Aggregate 후보와 비즈니스 규칙이 정의되었다면, 다음 질문은 "이 코드를 어디에, 어떤 이름으로 배치할 것인가?"입니다. 프로젝트 구조, 레이어 분리, 네이밍 규칙, 인프라 결정은 코드를 작성하기 전에 합의해야 합니다. 나중에 변경하면 모든 파일의 경로와 이름이 바뀌어야 하기 때문입니다.

`architecture-design` 스킬은 Functorium 프레임워크의 레이어드 아키텍처 원칙에 따라 프로젝트 뼈대를 설계합니다. 솔루션 구성, 폴더 트리, 네이밍 규칙, 프로젝트 참조 방향, 영속성/관측성/API 인프라 결정을 하나의 문서로 정리합니다.

## 스킬 개요

### 4 Phase 워크플로

| Phase | 활동 | 산출물 |
|-------|------|--------|
| 1. 프로젝트 구조 결정 | 프로젝트 이름, 솔루션 분리 수준, Adapter 분리 방식 | 솔루션 구성 초안 |
| 2. 레이어 구성 | Domain/Application/Adapter/Host 폴더 트리 | 레이어별 폴더 구조 |
| 3. 인프라 결정 | 영속성, 관측성, HTTP API, 외부 서비스 전략 | 인프라 결정 명세 |
| 4. 문서 생성 | 전체 내용을 구조화된 문서로 정리 | `01-architecture-design.md` |

### 트리거 예시

```text
아키텍처 설계해줘
프로젝트 구조 잡아줘
레이어 설계해줘
솔루션 구성해줘
폴더 구조 결정해줘
```

## Phase 1: 프로젝트 구조 결정

스킬은 대화를 통해 다음을 결정합니다.

**기본 결정 사항:**
- 프로젝트 네이밍 (예: `AiGovernance`, `ECommerce`)
- 단일 솔루션 vs 멀티 솔루션
- Adapter 프로젝트 분리 수준:
  - **단일**: `{Name}.Adapters` -- 소규모 프로젝트에 적합
  - **분리**: `{Name}.Adapters.Infrastructure` + `{Name}.Adapters.Persistence` + `{Name}.Adapters.Presentation` -- 중대형 프로젝트에 적합

## Phase 2: 레이어 구성

### 솔루션 전체 구조

```
{ProjectRoot}/
├── {name}.slnx                          # 솔루션 파일
├── Directory.Build.props                # FunctoriumSrcRoot + 공통 설정
├── Src/
│   ├── {Name}.Domain/                   # Domain Layer
│   ├── {Name}.Application/              # Application Layer
│   ├── {Name}.Adapters.Infrastructure/  # Mediator, OpenTelemetry, External Services
│   ├── {Name}.Adapters.Persistence/     # EfCore, Dapper, InMemory
│   ├── {Name}.Adapters.Presentation/    # FastEndpoints
│   └── {Name}/                          # Host (Program.cs)
└── Tests/
    ├── {Name}.Tests.Unit/               # Domain + Application + Architecture
    └── {Name}.Tests.Integration/        # HTTP Endpoint E2E
```

### Domain Layer 폴더

```
{Name}.Domain/
├── AggregateRoots/
│   └── {Aggregate}/
│       ├── {Aggregate}.cs               # AggregateRoot
│       ├── I{Aggregate}Repository.cs    # Repository Port
│       ├── ValueObjects/                # VO 모음
│       └── Specifications/              # 쿼리 조건
└── SharedModels/
    └── Services/                        # Domain Service
```

### Application Layer 폴더

```
{Name}.Application/
└── Usecases/
    └── {Aggregate}/
        ├── Commands/                    # 상태 변경 유스케이스
        ├── Queries/                     # 읽기 유스케이스
        ├── Events/                      # 도메인 이벤트 핸들러
        ├── I{Aggregate}Query.cs         # Read Port (목록 조회)
        └── I{Aggregate}DetailQuery.cs   # Read Port (상세 조회)
```

### Adapter Layer 폴더 (Persistence)

```
{Name}.Adapters.Persistence/
└── {Aggregate}/
    ├── {Aggregate}.Model.cs             # DB POCO 모델
    ├── {Aggregate}.Configuration.cs     # EF Core Fluent API
    ├── {Aggregate}.Mapper.cs            # Domain <-> Model 변환
    ├── Repositories/
    │   ├── {Aggregate}RepositoryEfCore.cs
    │   └── {Aggregate}RepositoryInMemory.cs
    └── Queries/
        ├── {Aggregate}QueryDapper.cs
        └── {Aggregate}QueryInMemory.cs
```

### 3차원 폴더 구조 원칙

Adapter 폴더는 3차원으로 구성합니다:

| 차원 | 표현 수단 | 예시 |
|------|-----------|------|
| Aggregate (무엇) | 1차 폴더 | `Products/`, `Orders/` |
| CQRS Role (읽기/쓰기) | 2차 폴더 | `Repositories/`, `Queries/` |
| Technology (어떻게) | 클래스 접미사 | `EfCore`, `InMemory`, `Dapper` |

### 네이밍 규칙

| 파일 유형 | 패턴 | 예시 |
|-----------|------|------|
| Repository | `{Aggregate}Repository{Variant}.cs` | `ProductRepositoryEfCore.cs` |
| Query | `{Aggregate}Query{Variant}.cs` | `ProductQueryDapper.cs` |
| DB 모델 | `{Aggregate}.Model.cs` | `Product.Model.cs` |
| EF 설정 | `{Aggregate}.Configuration.cs` | `Product.Configuration.cs` |
| EF 매퍼 | `{Aggregate}.Mapper.cs` | `Product.Mapper.cs` |

### 프로젝트 참조 방향

레이어 의존 방향은 안쪽으로만 흐릅니다:

```
Host -> Adapters.Infrastructure -> Application -> Domain
     -> Adapters.Persistence    -> Application -> Domain
     -> Adapters.Presentation   -> Application
```

Domain은 Functorium 프레임워크와 SourceGenerators만 참조합니다. Application은 Domain만 참조합니다. Adapter가 Application이나 Domain에 의존하되, 그 반대는 허용되지 않습니다.

## Phase 3: 인프라 결정

### 영속성 전략

`appsettings.json`의 `Persistence:Provider`로 InMemory/Sqlite를 전환합니다:

```json
{
  "Persistence": {
    "Provider": "InMemory",
    "ConnectionString": "Data Source=app.db"
  }
}
```

DI 등록에서 Provider에 따라 분기합니다:

```csharp
var provider = config["Persistence:Provider"];
return provider switch
{
    "Sqlite" => services.RegisterSqliteRepositories(config),
    _ => services.RegisterInMemoryRepositories()
};
```

### 관측성 전략

OpenTelemetry 3-Pillar(Logging + Tracing + Metrics) 파이프라인을 구성합니다:

```csharp
services.RegisterOpenTelemetry(config, assembly)
    .ConfigurePipelines(p => p
        .UseMetrics()        // 1. 메트릭 수집
        .UseTracing()        // 2. 분산 추적
        .UseCtxEnricher()    // 3. 비즈니스 컨텍스트 전파
        .UseLogging()        // 4. 구조화 로깅
        .UseException())     // 5. 예외 변환
    .Build();
```

### HTTP API 전략

FastEndpoints 기반으로 HTTP 엔드포인트를 구성합니다.

### 외부 서비스 전략

IO 모나드의 고급 기능을 활용합니다:

| 패턴 | 용도 | 예시 |
|------|------|------|
| `IO.Timeout` + `Catch` | 타임아웃 + 조건부 복구 | 헬스체크 API |
| `IO.Retry` + `Schedule` | 지수 백오프 재시도 | 외부 모니터링 API |
| `IO.Fork` + `Await` | 병렬 실행 | 병렬 컴플라이언스 체크 |
| `IO.Bracket` | 리소스 획득-사용-해제 | 레지스트리 세션 관리 |

## Phase 4: 문서 생성

수집한 모든 결정을 `{context}/01-architecture-design.md`로 정리합니다.

### 출력 문서에 포함되는 내용

- 프로젝트 구조도 (폴더 트리)
- 솔루션 파일 (.slnx) 구성
- 프로젝트 참조 관계도
- 레이어별 네이밍 규칙 표
- DI 등록 전략 (Registration 클래스별 역할)
- 영속성 Provider 전환 구성 (appsettings.json)
- 관측성 파이프라인 순서
- 빌드/테스트 명령어

### 다음 단계 안내

> 아키텍처 설계가 완성되었습니다.
>
> **다음 단계:**
> 1. `domain-develop` 스킬로 각 Aggregate를 상세 설계하고 구현하세요
> 2. Aggregate별로 `00-business-requirements.md` -> `03-implementation-results.md` 4단계 문서를 생성합니다

## 핵심 원칙

- **레이어 의존 방향**: Domain <- Application <- Adapter <- Host
- **Adapter 내 폴더 구성**: Aggregate 중심 (1차) + CQRS Role (2차) + Technology 접미사
- **네이밍**: `{Subject}{Role}{Variant}` (예: `ProductRepositoryEfCore`)
- **DB 모델/설정 파일**: dot 표기 (예: `Product.Model.cs`, `Product.Configuration.cs`)
- **영속성 Provider 전환**: appsettings.json의 `Persistence:Provider`로 InMemory/Sqlite 전환
- **`$(FunctoriumSrcRoot)` 변수**: csproj에서 프레임워크 참조 시 사용

## 참고 자료

- [워크플로](../workflow/) -- 7단계 전체 흐름
- [Project Spec 스킬](./project-spec/) -- 이전 단계: PRD 작성
- [Domain Develop 스킬](./domain-develop/) -- 다음 단계: 도메인 모델 구현
- [Adapter Develop 스킬](./adapter-develop/) -- 영속성/API 구현
