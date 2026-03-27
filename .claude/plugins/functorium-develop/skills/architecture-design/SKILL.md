---
name: architecture-design
description: "Functorium 프레임워크 기반 프로젝트의 아키텍처를 설계합니다. 프로젝트 구조, 레이어 구성, 네이밍 규칙, 영속성/관측성/API 인프라를 결정합니다. '아키텍처 설계', '프로젝트 구조', '레이어 설계', '솔루션 구성', '폴더 구조' 등의 요청에 반응합니다."
---

## 선행 조건

`project-spec` 스킬에서 생성한 `00-project-spec.md`가 있으면 읽어 Aggregate 후보와 유스케이스 개요를 확인합니다.
없으면 사용자에게 프로젝트 범위를 직접 질문합니다.

## 개요

Functorium 프레임워크 기반 프로젝트의 아키텍처를 설계합니다.
레이어 구성, 프로젝트 분리, 네이밍 규칙, 인프라 결정을 문서화합니다.

## 후속 스킬

```
project-spec → **architecture-design** → domain-develop → application-develop → adapter-develop → test-develop
```

## 워크플로우

### Phase 1: 프로젝트 구조 결정

사용자에게 다음을 질문합니다:
- 프로젝트 네이밍은? (예: `AiGovernance`, `ECommerce`)
- 단일 솔루션 vs 멀티 솔루션?
- Adapter 프로젝트 분리 수준은?
  - 단일: `{Name}.Adapters`
  - 분리: `{Name}.Adapters.Infrastructure` + `{Name}.Adapters.Persistence` + `{Name}.Adapters.Presentation`

상세 구조 규칙은 `references/project-structure.md`를 읽어 참고합니다.

### Phase 2: 레이어 구성

각 레이어의 폴더 구조를 결정합니다:

**Domain Layer:**
```
{Name}.Domain/
├── AggregateRoots/
│   └── {Aggregate}/
│       ├── {Aggregate}.cs
│       ├── I{Aggregate}Repository.cs
│       ├── ValueObjects/
│       └── Specifications/
└── SharedModels/
    └── Services/
```

**Application Layer:**
```
{Name}.Application/
└── Usecases/
    └── {Aggregate}/
        ├── Commands/
        ├── Queries/
        ├── Events/
        ├── I{Aggregate}Repository.cs     (또는 Domain에서 직접 참조)
        ├── I{Aggregate}Query.cs
        └── I{Aggregate}DetailQuery.cs
```

**Adapter Layer (Persistence):**
```
{Name}.Adapters.Persistence/
└── {Aggregate}/
    ├── {Aggregate}.Model.cs
    ├── {Aggregate}.Configuration.cs
    ├── Repositories/
    │   ├── {Aggregate}RepositoryEfCore.cs
    │   └── {Aggregate}RepositoryInMemory.cs
    └── Queries/
        ├── {Aggregate}QueryDapper.cs
        └── {Aggregate}QueryInMemory.cs
```

### Phase 3: 인프라 결정

상세 결정 가이드는 `references/infra-decisions.md`를 읽어 참고합니다.

사용자에게 다음을 질문합니다:
- 영속성 전략은? (InMemory 기본 + EfCore SQLite 옵션)
- 관측성 수준은? (OpenTelemetry 3-Pillar: Logging + Tracing + Metrics)
- HTTP API 프레임워크는? (FastEndpoints 기본)
- 외부 서비스 연동은? (IO 고급 기능: Retry, Timeout, Fork, Bracket)

### Phase 4: 문서 생성

**출력:** `{context}/01-architecture-design.md`

출력 문서에 포함할 내용:
- 프로젝트 구조도 (폴더 트리)
- 솔루션 파일 (.slnx) 구성
- 프로젝트 참조 관계도
- 레이어별 네이밍 규칙 표
- DI 등록 전략 (Registration 클래스별 역할)
- 영속성 Provider 전환 구성 (appsettings.json)
- 관측성 파이프라인 순서
- 빌드/테스트 명령어

### Phase 4 출력 후 안내

> 아키텍처 설계가 완성되었습니다.
>
> **다음 단계:**
> 1. `domain-develop` 스킬로 각 Aggregate를 상세 설계하고 구현하세요
> 2. Aggregate별로 `00-business-requirements.md` → `03-implementation-results.md` 4단계 문서를 생성합니다

## 핵심 원칙

- 레이어 의존 방향: Domain ← Application ← Adapter ← Host
- Adapter 내 폴더 구성: Aggregate 중심 (1차) + CQRS Role (2차) + Technology 접미사
- 네이밍: `{Subject}{Role}{Variant}` (예: `ProductRepositoryEfCore`)
- DB 모델/설정 파일: dot 표기 (예: `Product.Model.cs`, `Product.Configuration.cs`)
- 영속성 Provider 전환: appsettings.json의 `Persistence:Provider`로 InMemory/Sqlite 전환
- `$(FunctoriumSrcRoot)` 변수: csproj에서 프레임워크 참조 시 사용

## References

- 프로젝트 구조: `references/project-structure.md`를 읽으세요
- 인프라 결정: `references/infra-decisions.md`를 읽으세요
