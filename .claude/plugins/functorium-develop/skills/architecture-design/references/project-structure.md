# 프로젝트 구조 규칙

## 솔루션 구성

```
{ProjectRoot}/
├── {name}.slnx                          # 솔루션 파일
├── Directory.Build.props                # FunctoriumSrcRoot + 공통 설정
├── Directory.Build.targets              # root 상속 차단 (독립 솔루션 시)
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

## 프로젝트 참조 방향

```
Host → Adapters.Infrastructure
     → Adapters.Persistence
     → Adapters.Presentation
     → Application

Adapters.Infrastructure → Functorium + Functorium.Adapters + Application
Adapters.Persistence    → Functorium.Adapters + Application + SourceGenerators
Adapters.Presentation   → Application

Application → Domain
Domain      → Functorium + SourceGenerators
```

## 네이밍 규칙

### 3차원 구조

| 차원 | 표현 수단 | 예시 |
|------|-----------|------|
| Aggregate (무엇) | 1차 폴더 | `Products/`, `Orders/` |
| CQRS Role (읽기/쓰기) | 2차 폴더 | `Repositories/`, `Queries/` |
| Technology (어떻게) | 클래스 접미사 | `EfCore`, `InMemory`, `Dapper` |

### 파일명 패턴

| 파일 유형 | 패턴 | 예시 |
|-----------|------|------|
| Repository | `{Aggregate}Repository{Variant}.cs` | `ProductRepositoryEfCore.cs` |
| Query | `{Aggregate}Query{Variant}.cs` | `ProductQueryDapper.cs` |
| DB 모델 | `{Aggregate}.Model.cs` | `Product.Model.cs` |
| EF 설정 | `{Aggregate}.Configuration.cs` | `Product.Configuration.cs` |
| EF 매퍼 | `{Aggregate}.Mapper.cs` | `Product.Mapper.cs` |
| UnitOfWork | `UnitOfWork{Variant}.cs` | `UnitOfWorkEfCore.cs` |

### csproj 프레임워크 참조

```xml
<!-- $(FunctoriumSrcRoot) 변수 사용 (Directory.Build.props에 정의) -->
<ProjectReference Include="$(FunctoriumSrcRoot)\Functorium\Functorium.csproj" />
<ProjectReference Include="$(FunctoriumSrcRoot)\Functorium.Adapters\Functorium.Adapters.csproj" />
<ProjectReference Include="$(FunctoriumSrcRoot)\Functorium.Testing\Functorium.Testing.csproj" />
<ProjectReference Include="$(FunctoriumSrcRoot)\Functorium.SourceGenerators\Functorium.SourceGenerators.csproj"
                  OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```
