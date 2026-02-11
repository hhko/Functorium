# 서비스 프로젝트 구성 가이드

## 목차

- [개요](#개요)
- [프로젝트 공통 파일](#프로젝트-공통-파일)
- [주 목표와 부수 목표](#주-목표와-부수-목표)
- [Domain 레이어](#domain-레이어)
- [Application 레이어](#application-레이어)
- [Adapter 레이어](#adapter-레이어)
- [Host 프로젝트](#host-프로젝트)
- [네임스페이스 규칙](#네임스페이스-규칙)
- [새 서비스 프로젝트 생성 체크리스트](#새-서비스-프로젝트-생성-체크리스트)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

## 개요

이 가이드는 서비스의 **프로젝트 구성** — 폴더 이름, 파일 배치, 의존성 방향 — 을 다룹니다.
"어떻게 구현하는가(HOW)"는 다른 가이드에 위임하고, "어디에 배치하는가(WHERE)"에만 집중합니다.

| WHERE (이 가이드) | HOW (참조 가이드) |
|---|---|
| AggregateRoots 폴더 구조 | [entity-guide.md](./entity-guide.md) — Entity/Aggregate 구현 |
| ValueObjects 위치 규칙 | [valueobject-guide.md](./valueobject-guide.md) — 값 객체 구현 패턴 |
| Domain Ports 위치 결정 기준 | [adapter-guide.md](./adapter-guide.md) — Adapter 설계 원칙 |
| Usecases 폴더/파일 네이밍 | [usecase-implementation-guide.md](./usecase-implementation-guide.md) — 유스케이스 구현 |
| Abstractions/Registrations 구조 | [adapter-guide.md](./adapter-guide.md) — 등록 코드 패턴 |

### 전체 프로젝트 구성 개요

서비스는 6개 프로젝트로 구성됩니다.

| # | 프로젝트 | 이름 패턴 | SDK | 역할 |
|---|---------|----------|-----|------|
| 1 | Domain | `{ServiceName}.Domain` | `Microsoft.NET.Sdk` | 도메인 모델, Aggregate, Value Object, Port |
| 2 | Application | `{ServiceName}.Application` | `Microsoft.NET.Sdk` | 유스케이스 (Command/Query/EventHandler), 외부 Port |
| 3 | Adapter: Presentation | `{ServiceName}.Adapters.Presentation` | `Microsoft.NET.Sdk` | HTTP 엔드포인트 (FastEndpoints) |
| 4 | Adapter: Persistence | `{ServiceName}.Adapters.Persistence` | `Microsoft.NET.Sdk` | Repository 구현 |
| 5 | Adapter: Infrastructure | `{ServiceName}.Adapters.Infrastructure` | `Microsoft.NET.Sdk` | 외부 API, Mediator, OpenTelemetry, 파이프라인 |
| 6 | Host | `{ServiceName}` | `Microsoft.NET.Sdk.Web` | Composition Root (Program.cs) |

### 프로젝트 이름 규칙

```
{ServiceName}                          ← Host
{ServiceName}.Domain                   ← Domain 레이어
{ServiceName}.Application              ← Application 레이어
{ServiceName}.Adapters.{Category}      ← Adapter 레이어 (Presentation | Persistence | Infrastructure)
```

### 프로젝트 의존성 방향

```
                    Host
                   / | \
                  /  |  \
                 v   v   v
  Presentation  Persistence  Infrastructure
         \         |         /
          \        |        /
           v       v       v
              Application
                  |
                  v
               Domain
```

**csproj 참조 예시:**

```xml
<!-- Host → 모든 Adapter + Application -->
<ProjectReference Include="..\LayeredArch.Adapters.Infrastructure\..." />
<ProjectReference Include="..\LayeredArch.Adapters.Persistence\..." />
<ProjectReference Include="..\LayeredArch.Adapters.Presentation\..." />
<ProjectReference Include="..\LayeredArch.Application\..." />

<!-- Adapter → Application (간접적으로 Domain 포함) -->
<ProjectReference Include="..\LayeredArch.Application\..." />

<!-- Application → Domain -->
<ProjectReference Include="..\LayeredArch.Domain\..." />
```

> **규칙:** 의존성은 항상 바깥에서 안쪽으로만 향합니다. Domain은 아무것도 참조하지 않고, Application은 Domain만, Adapter는 Application만 참조합니다.

## 프로젝트 공통 파일

모든 프로젝트는 두 가지 공통 파일을 포함합니다.

### AssemblyReference.cs

어셈블리 스캔을 위한 참조 포인트입니다. 모든 프로젝트에 동일한 패턴으로 배치합니다.

```csharp
using System.Reflection;

namespace {ServiceName}.{Layer};

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
```

**네임스페이스 예시:**

| 프로젝트 | 네임스페이스 |
|---------|------------|
| Domain | `{ServiceName}.Domain` |
| Application | `{ServiceName}.Application` |
| Adapters.Presentation | `{ServiceName}.Adapters.Presentation` |
| Adapters.Persistence | `{ServiceName}.Adapters.Persistence` |
| Adapters.Infrastructure | `{ServiceName}.Adapters.Infrastructure` |

**용도:** FluentValidation 자동 등록, Mediator 핸들러 스캔 등 `Assembly` 참조가 필요한 곳에서 사용합니다.

```csharp
// 사용 예 — Infrastructure Registration에서
services.AddValidatorsFromAssembly(AssemblyReference.Assembly);
services.AddValidatorsFromAssembly(LayeredArch.Application.AssemblyReference.Assembly);
```

### Using.cs (또는 Usings.cs)

레이어별 global using 선언 파일입니다. 파일 이름은 `Using.cs` 또는 `Usings.cs`를 사용합니다.

| 프로젝트 | global using 내용 |
|---------|------------------|
| Domain | LanguageExt, Functorium.Domains.*, 자체 SharedKernel |
| Application | LanguageExt, Functorium.Applications.Cqrs, FluentValidation, 자체 SharedKernel |
| Adapters.Presentation | FastEndpoints, Mediator, LanguageExt.Common |
| Adapters.Persistence | LanguageExt, Domain Aggregate, 자체 SharedKernel |
| Adapters.Infrastructure | FluentValidation, 자체 SharedKernel |

<details>
<summary>레이어별 Using.cs 전체 코드</summary>

**Domain — Using.cs**
```csharp
global using LanguageExt;
global using LanguageExt.Common;
global using Functorium.Domains.Entities;
global using Functorium.Domains.Events;
global using Functorium.Domains.SourceGenerators;
global using Functorium.Domains.ValueObjects;
global using Functorium.Domains.ValueObjects.Validations.Typed;
global using LayeredArch.Domain.SharedKernel.Events;
global using LayeredArch.Domain.SharedKernel.Entities;
global using LayeredArch.Domain.SharedKernel.ValueObjects;
```

**Application — Usings.cs**
```csharp
global using LanguageExt;
global using LanguageExt.Common;
global using static LanguageExt.Prelude;
global using Functorium.Applications.Cqrs;
global using Functorium.Domains.ValueObjects.Validations.Typed;
global using Functorium.Domains.ValueObjects.Validations.Contextual;
global using FluentValidation;
global using LayeredArch.Domain.SharedKernel.ValueObjects;
```

**Adapters.Presentation — Using.cs**
```csharp
global using LanguageExt.Common;
global using FastEndpoints;
global using Mediator;
```

**Adapters.Persistence — Using.cs**
```csharp
global using LanguageExt;
global using LanguageExt.Common;
global using LayeredArch.Domain.AggregateRoots.Products;
global using static LanguageExt.Prelude;
global using LayeredArch.Domain.SharedKernel.ValueObjects;
```

**Adapters.Infrastructure — Using.cs**
```csharp
global using FluentValidation;
global using LayeredArch.Domain.SharedKernel.ValueObjects;
```

</details>

## 주 목표와 부수 목표

각 프로젝트(레이어)에는 **주 목표**와 **부수 목표**가 있습니다.

- **주 목표** — 해당 레이어가 존재하는 이유. 비즈니스 로직이나 핵심 기술 구현 코드가 위치합니다.
- **부수 목표** — 레이어를 지원하는 보조 인프라. DI 등록, 확장 메서드 등이 위치합니다.

| 프로젝트 | 주 목표 폴더 | 부수 목표 폴더 |
|---------|------------|------------|
| Domain | `AggregateRoots/`, `SharedKernel/`, `Ports/` | *(없음)* |
| Application | `Usecases/`, `Ports/` | *(없음)* |
| Adapters.Presentation | `Endpoints/` | `Abstractions/` (Registrations/, Extensions/) |
| Adapters.Persistence | `Repositories/` | `Abstractions/` (Registrations/) |
| Adapters.Infrastructure | `ExternalApis/`, ... | `Abstractions/` (Registrations/) |

### Abstractions 폴더 규칙

Adapter 프로젝트의 부수 목표는 `Abstractions/` 폴더 아래에 배치합니다.

```
Abstractions/
├── Registrations/        ← DI 서비스 등록 확장 메서드
│   └── Adapter{Category}Registration.cs
└── Extensions/           ← 공유 확장 메서드 (필요 시)
    └── {Name}Extensions.cs
```

> **주의:** Domain과 Application에는 `Abstractions/` 폴더가 없습니다. [FAQ 참조](#faq)

## Domain 레이어

### 주 목표 폴더

```
{ServiceName}.Domain/
├── AggregateRoots/       ← Aggregate Root별 하위 폴더
├── SharedKernel/         ← 교차 Aggregate 공유 타입
├── Ports/                ← 교차 Aggregate Port 인터페이스
├── AssemblyReference.cs
└── Using.cs
```

### AggregateRoots 내부 구조

각 Aggregate Root는 자체 폴더를 가지며, 내부 구조는 다음과 같습니다.

```
AggregateRoots/
├── Products/
│   ├── Product.cs                 ← Aggregate Root Entity
│   ├── Entities/                  ← 이 Aggregate의 자식 Entity (필요 시)
│   │   └── ProductVariant.cs
│   ├── Ports/
│   │   └── IProductRepository.cs  ← 이 Aggregate 전용 Port
│   └── ValueObjects/
│       ├── ProductName.cs         ← 이 Aggregate 전용 Value Object
│       └── ProductDescription.cs
├── Customers/
│   ├── Customer.cs
│   ├── Ports/
│   │   └── ICustomerRepository.cs
│   └── ValueObjects/
│       ├── CustomerName.cs
│       └── Email.cs
└── Orders/
    ├── Order.cs
    ├── Entities/
    │   └── OrderLine.cs           ← 자식 Entity
    ├── Ports/
    │   └── IOrderRepository.cs
    └── ValueObjects/
        └── ShippingAddress.cs
```

**규칙:**
- Aggregate Root 파일(`{Aggregate}.cs`)은 해당 폴더의 루트에 배치
- Aggregate의 자식 Entity는 `{Aggregate}/Entities/` 에 배치
- Aggregate 전용 Port는 `{Aggregate}/Ports/` 에 배치
- Aggregate 전용 Value Object는 `{Aggregate}/ValueObjects/` 에 배치

### SharedKernel 내부 구조

여러 Aggregate에서 공유하는 타입을 배치합니다.

```
SharedKernel/
├── Entities/
│   └── Tag.cs                ← 공유 Entity
├── Events/
│   └── TagEvents.cs          ← 공유 Domain Event
└── ValueObjects/
    ├── Money.cs              ← 공유 Value Object
    ├── Quantity.cs
    └── TagName.cs
```

### Ports (교차 Aggregate)

하나의 Aggregate에 속하지 않고, 다른 Aggregate에서 참조하는 Port는 프로젝트 루트의 `Ports/` 폴더에 배치합니다.

```
Ports/
└── IProductCatalog.cs    ← Order에서 Product 검증용으로 사용
```

**Port 위치 결정 기준:**

| 기준 | 위치 | 예시 |
|------|------|------|
| 특정 Aggregate 전용 CRUD | `AggregateRoots/{Aggregate}/Ports/` | `IProductRepository` |
| 교차 Aggregate 읽기 전용 | `Ports/` (프로젝트 루트) | `IProductCatalog` |

## Application 레이어

### 주 목표 폴더

```
{ServiceName}.Application/
├── Usecases/             ← Aggregate별 유스케이스
├── Ports/                ← 외부 시스템 Port 인터페이스
├── AssemblyReference.cs
└── Usings.cs
```

### Usecases 내부 구조

Aggregate별 하위 폴더로 구분합니다.

```
Usecases/
├── Products/
│   ├── CreateProductCommand.cs
│   ├── UpdateProductCommand.cs
│   ├── DeductStockCommand.cs
│   ├── GetProductByIdQuery.cs
│   ├── GetAllProductsQuery.cs
│   ├── OnProductCreated.cs        ← Event Handler
│   ├── OnProductUpdated.cs
│   └── OnStockDeducted.cs
├── Customers/
│   ├── CreateCustomerCommand.cs
│   ├── GetCustomerByIdQuery.cs
│   └── OnCustomerCreated.cs
└── Orders/
    ├── CreateOrderCommand.cs
    ├── GetOrderByIdQuery.cs
    └── OnOrderCreated.cs
```

**파일 네이밍 규칙:**

| 유형 | 패턴 | 예시 |
|------|------|------|
| Command | `{동사}{Aggregate}Command.cs` | `CreateProductCommand.cs` |
| Query | `{Get 등}{설명}Query.cs` | `GetAllProductsQuery.cs` |
| Event Handler | `On{Event명}.cs` | `OnProductCreated.cs` |

### Ports — Domain Ports와의 차이

| 기준 | Domain Port | Application Port |
|------|------------|-----------------|
| 위치 | `Domain/AggregateRoots/{Aggregate}/Ports/` 또는 `Domain/Ports/` | `Application/Ports/` |
| 구현 주체 | 주로 Persistence Adapter | 주로 Infrastructure Adapter |
| 역할 | 도메인 객체의 저장/조회 | 외부 시스템 호출 (API, 메시징 등) |
| 예시 | `IProductRepository`, `IProductCatalog` | `IExternalPricingService` |

## Adapter 레이어

### 3분할 원칙

Adapter는 항상 3개 프로젝트로 분할합니다.

| 프로젝트 | 관심사 | 대표 폴더 |
|---------|--------|----------|
| `Adapters.Presentation` | HTTP 입출력 | `Endpoints/` |
| `Adapters.Persistence` | 데이터 저장/조회 | `Repositories/` |
| `Adapters.Infrastructure` | 외부 API, 횡단 관심사(Observability, Mediator 등) | `ExternalApis/`, ... |

### 주 목표 폴더가 고정되지 않는 이유

Adapter의 주 목표 폴더 이름은 구현 기술에 따라 달라집니다. Presentation은 `Endpoints/`가 되지만, gRPC라면 `Services/`가 될 수 있습니다. Persistence도 ORM에 따라 `Repositories/`, `DbContexts/` 등 다양합니다. **폴더 이름은 구현 기술을 반영합니다.**

### Adapters.Presentation 구조

```
{ServiceName}.Adapters.Presentation/
├── Endpoints/
│   ├── Products/
│   │   ├── CreateProductEndpoint.cs
│   │   ├── UpdateProductEndpoint.cs
│   │   ├── DeductStockEndpoint.cs
│   │   ├── GetProductByIdEndpoint.cs
│   │   └── GetAllProductsEndpoint.cs
│   ├── Customers/
│   │   ├── CreateCustomerEndpoint.cs
│   │   └── GetCustomerByIdEndpoint.cs
│   └── Orders/
│       ├── CreateOrderEndpoint.cs
│       └── GetOrderByIdEndpoint.cs
├── Abstractions/
│   ├── Registrations/
│   │   └── AdapterPresentationRegistration.cs
│   └── Extensions/
│       └── FinResponseExtensions.cs
├── AssemblyReference.cs
└── Using.cs
```

**Endpoints 폴더 규칙:** Aggregate별 하위 폴더, 엔드포인트 파일명은 `{동사}{Aggregate}Endpoint.cs` 패턴을 따릅니다.

### Adapters.Persistence 구조

```
{ServiceName}.Adapters.Persistence/
├── Repositories/
│   ├── InMemoryProductRepository.cs
│   ├── InMemoryCustomerRepository.cs
│   ├── InMemoryOrderRepository.cs
│   └── InMemoryProductCatalog.cs     ← 교차 Aggregate Port 구현
├── Abstractions/
│   └── Registrations/
│       └── AdapterPersistenceRegistration.cs
├── AssemblyReference.cs
└── Using.cs
```

### Adapters.Infrastructure 구조

```
{ServiceName}.Adapters.Infrastructure/
├── ExternalApis/
│   └── ExternalPricingApiService.cs   ← Application Port 구현
├── Abstractions/
│   └── Registrations/
│       └── AdapterInfrastructureRegistration.cs
├── AssemblyReference.cs
└── Using.cs
```

### 부수 목표: Abstractions/

각 Adapter의 `Abstractions/Registrations/` 폴더에는 DI 등록 확장 메서드를 배치합니다.

**등록 메서드 네이밍 규칙:**

| 메서드 | 패턴 |
|--------|------|
| 서비스 등록 | `RegisterAdapter{Category}(this IServiceCollection)` |
| 미들웨어 설정 | `UseAdapter{Category}(this IApplicationBuilder)` |

```csharp
// AdapterPresentationRegistration.cs
public static IServiceCollection RegisterAdapterPresentation(this IServiceCollection services) { ... }
public static IApplicationBuilder UseAdapterPresentation(this IApplicationBuilder app) { ... }

// AdapterPersistenceRegistration.cs
public static IServiceCollection RegisterAdapterPersistence(this IServiceCollection services) { ... }
public static IApplicationBuilder UseAdapterPersistence(this IApplicationBuilder app) { ... }

// AdapterInfrastructureRegistration.cs
public static IServiceCollection RegisterAdapterInfrastructure(this IServiceCollection services, IConfiguration configuration) { ... }
public static IApplicationBuilder UseAdapterInfrastructure(this IApplicationBuilder app) { ... }
```

## Host 프로젝트

### 역할 (Composition Root)

Host 프로젝트는 모든 레이어를 조합하는 유일한 프로젝트입니다. SDK는 `Microsoft.NET.Sdk.Web`을 사용합니다.

### Program.cs 레이어 등록 순서

```csharp
var builder = WebApplication.CreateBuilder(args);

// 레이어별 서비스 등록
builder.Services
    .RegisterAdapterPresentation()
    .RegisterAdapterPersistence()
    .RegisterAdapterInfrastructure(builder.Configuration);

// App 빌드 및 미들웨어 설정
var app = builder.Build();

app.UseAdapterInfrastructure()
   .UseAdapterPersistence()
   .UseAdapterPresentation();

app.Run();
```

**등록 순서:** Presentation → Persistence → Infrastructure (서비스 등록)
**미들웨어 순서:** Infrastructure → Persistence → Presentation (미들웨어 설정)

## 네임스페이스 규칙

네임스페이스는 프로젝트 루트 네임스페이스 + 폴더 경로로 결정됩니다.

| 폴더 경로 | 네임스페이스 |
|----------|------------|
| `Domain/` | `{ServiceName}.Domain` |
| `Domain/AggregateRoots/Products/` | `{ServiceName}.Domain.AggregateRoots.Products` |
| `Domain/AggregateRoots/Products/Ports/` | `{ServiceName}.Domain.AggregateRoots.Products` *(Port는 Aggregate 네임스페이스)* |
| `Domain/AggregateRoots/Products/ValueObjects/` | `{ServiceName}.Domain.AggregateRoots.Products.ValueObjects` |
| `Domain/SharedKernel/ValueObjects/` | `{ServiceName}.Domain.SharedKernel.ValueObjects` |
| `Domain/SharedKernel/Entities/` | `{ServiceName}.Domain.SharedKernel.Entities` |
| `Domain/SharedKernel/Events/` | `{ServiceName}.Domain.SharedKernel.Events` |
| `Domain/Ports/` | `{ServiceName}.Domain.Ports` |
| `Application/Usecases/Products/` | `{ServiceName}.Application.Usecases.Products` |
| `Application/Ports/` | `{ServiceName}.Application.Ports` |
| `Adapters.Presentation/Endpoints/Products/` | `{ServiceName}.Adapters.Presentation.Endpoints.Products` |
| `Adapters.Presentation/Abstractions/Registrations/` | `{ServiceName}.Adapters.Presentation.Abstractions.Registrations` |
| `Adapters.Persistence/Repositories/` | `{ServiceName}.Adapters.Persistence.Repositories` |
| `Adapters.Persistence/Abstractions/Registrations/` | `{ServiceName}.Adapters.Persistence.Abstractions.Registrations` |
| `Adapters.Infrastructure/ExternalApis/` | `{ServiceName}.Adapters.Infrastructure.ExternalApis` |
| `Adapters.Infrastructure/Abstractions/Registrations/` | `{ServiceName}.Adapters.Infrastructure.Abstractions.Registrations` |

## 새 서비스 프로젝트 생성 체크리스트

1. **Domain 프로젝트**
   - [ ] `{ServiceName}.Domain` 프로젝트 생성 (SDK: `Microsoft.NET.Sdk`)
   - [ ] `AssemblyReference.cs` 추가
   - [ ] `Using.cs` 추가
   - [ ] `AggregateRoots/` 폴더 생성
   - [ ] `SharedKernel/` 폴더 생성 (필요 시)
   - [ ] `Ports/` 폴더 생성 (교차 Aggregate Port가 있을 경우)

2. **Application 프로젝트**
   - [ ] `{ServiceName}.Application` 프로젝트 생성
   - [ ] `AssemblyReference.cs` 추가
   - [ ] `Usings.cs` 추가
   - [ ] `Usecases/` 폴더 생성
   - [ ] `Ports/` 폴더 생성 (외부 시스템 Port가 있을 경우)
   - [ ] Domain 프로젝트 참조 추가

3. **Adapters.Presentation 프로젝트**
   - [ ] `{ServiceName}.Adapters.Presentation` 프로젝트 생성
   - [ ] `AssemblyReference.cs` 추가
   - [ ] `Using.cs` 추가
   - [ ] `Endpoints/` 폴더 생성
   - [ ] `Abstractions/Registrations/AdapterPresentationRegistration.cs` 추가
   - [ ] Application 프로젝트 참조 추가

4. **Adapters.Persistence 프로젝트**
   - [ ] `{ServiceName}.Adapters.Persistence` 프로젝트 생성
   - [ ] `AssemblyReference.cs` 추가
   - [ ] `Using.cs` 추가
   - [ ] `Repositories/` 폴더 생성
   - [ ] `Abstractions/Registrations/AdapterPersistenceRegistration.cs` 추가
   - [ ] Application 프로젝트 참조 추가

5. **Adapters.Infrastructure 프로젝트**
   - [ ] `{ServiceName}.Adapters.Infrastructure` 프로젝트 생성
   - [ ] `AssemblyReference.cs` 추가
   - [ ] `Using.cs` 추가
   - [ ] `Abstractions/Registrations/AdapterInfrastructureRegistration.cs` 추가
   - [ ] Application 프로젝트 참조 추가

6. **Host 프로젝트**
   - [ ] `{ServiceName}` 프로젝트 생성 (SDK: `Microsoft.NET.Sdk.Web`)
   - [ ] 모든 Adapter + Application 프로젝트 참조 추가
   - [ ] `Program.cs` — 레이어 등록 메서드 호출 추가

## FAQ

### 1. Domain에 Abstractions/ 폴더가 없는 이유

Domain 레이어에는 부수 목표가 없습니다. Domain은 순수한 비즈니스 규칙만 포함하며, DI 등록이나 프레임워크 설정 같은 인프라 관심사가 존재하지 않기 때문입니다. Application도 동일한 이유로 Abstractions가 없습니다.

### 2. Adapter 주 목표 폴더 이름이 고정되지 않는 이유

Adapter의 주 목표 폴더 이름은 구현 기술에 따라 달라집니다. 예를 들어 Presentation이 FastEndpoints를 사용하면 `Endpoints/`, gRPC를 사용하면 `Services/`가 됩니다. 반면 부수 목표 폴더(`Abstractions/`)는 기술과 무관하게 항상 같은 이름을 사용합니다.

### 3. Value Object를 SharedKernel과 AggregateRoots 사이 어디에 둘지 판단 기준

- **하나의 Aggregate에서만 사용** → `AggregateRoots/{Aggregate}/ValueObjects/`
  - 예: `ProductName`, `ProductDescription` → `Products/ValueObjects/`
- **여러 Aggregate에서 공유** → `SharedKernel/ValueObjects/`
  - 예: `Money`, `Quantity` → `SharedKernel/ValueObjects/`

처음에는 Aggregate 전용으로 배치하고, 공유가 필요해지면 SharedKernel로 이동합니다.

### 4. Port를 Domain에 둘지 Application에 둘지 판단 기준

- **도메인 객체의 영속성/조회** → Domain의 `AggregateRoots/{Aggregate}/Ports/` 또는 `Ports/`
  - 예: `IProductRepository`, `IProductCatalog`
- **외부 시스템 통합** → Application의 `Ports/`
  - 예: `IExternalPricingService`

핵심 기준: 인터페이스의 메서드 시그니처가 도메인 타입만 사용하면 Domain, 외부 DTO나 기술적 관심사를 포함하면 Application에 배치합니다.

### 5. Infrastructure에 Observability 설정이 들어가는 이유

Observability(OpenTelemetry, Serilog 등)는 횡단 관심사로, 특정 Adapter 카테고리에 속하지 않습니다. Infrastructure Adapter가 Mediator, Validator, OpenTelemetry, Pipeline 등 횡단 관심사를 종합적으로 관리하는 역할을 담당하기 때문에 이곳에 배치합니다.

## 참고 문서

- [entity-guide.md](./entity-guide.md) — Entity/Aggregate Root 구현 패턴
- [valueobject-guide.md](./valueobject-guide.md) — 값 객체 구현 및 검증 패턴
- [usecase-implementation-guide.md](./usecase-implementation-guide.md) — 유스케이스 (Command/Query) 구현
- [adapter-guide.md](./adapter-guide.md) — Adapter 설계 원칙 + 단계별 활동
- [error-guide.md](./error-guide.md) — 레이어별 에러 시스템
- [observability-spec.md](./observability-spec.md) — Observability 사양
