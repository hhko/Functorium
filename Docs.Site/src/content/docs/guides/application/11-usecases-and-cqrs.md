---
title: "Use Cases and CQRS"
---

This document explains how to implement use cases with the CQRS pattern, which separates reads and writes for individual optimization.

## Introduction

"Should navigation properties be added to the domain model for query performance?"
"As business logic in the Application Service keeps growing, how can it be separated?"
"Where should SaveChanges be called, and who is responsible for publishing domain events?"

These are problems repeatedly encountered when designing the Application Layer. CQRS는 읽기와 쓰기를 분리하여 각각 최적의 기술을 Optional할 수 있게 하고, Functorium의 파이프라인 시스템은 트랜잭션과 이벤트 발행을 자동으로 처리하여 Usecase가 비즈니스 로직에만 집중할 수 있게 합니다.

### What You Will Learn

Through this document, you will learn:

1. **CQRS 패턴의 이점과 Command/Query 분리 기준** - 읽기/쓰기 경로 분리의 실질적 효과
2. **중첩 클래스 패턴으로 유스케이스 구현** - Request, Response, Validator, Usecase를 하나의 파일에 응집
3. **Apply 병합 패턴과 LINQ 기반 함수형 구현** - Value Object 검증과 함수형 체이닝
4. **UsecaseTransactionPipeline의 자동 처리** - SaveChanges와 도메인 이벤트 발행 자동화
5. **Application 에러와 FluentValidation 통합** - 이중 검증 전략

### Prerequisites

A basic understanding of the following concepts is required to understand this document:

- [DDD 전술적 설계 개요](../domain/04-ddd-tactical-overview)의 레이어 아키텍처
- [값 객체(Value Object)](../domain/05a-value-objects)의 Create/Validate 패턴
- LanguageExt의 `Fin<T>`, `FinT<IO, T>` 기본 개념

> **CQRS의 핵심은** 읽기와 쓰기를 분리하여 각각 최적의 기술을 Optional하는 것이고, Functorium의 파이프라인은 트랜잭션과 이벤트 발행을 자동 처리하여 Usecase가 비즈니스 로직에만 집중할 수 있게 합니다.

## Why CQRS

### Role of Application Service in DDD

The Application Layer is the layer that orchestrates domain objects to perform use cases. 도메인 로직 자체를 포함하지 않고, 도메인 객체에게 작업을 위임합니다.

전통적인 Application Service에서는 하나의 서비스 클래스가 생성, 조회, 수정, 삭제를 모두 처리합니다. 처음에는 간결해 보이지만, 비즈니스가 성장하면서 문제가 드러납니다.

조회 시에는 여러 테이블을 조인한 DTO가 필요하고, 생성 시에는 Aggregate Root를 통한 Immutable식 검증과 트랜잭션이 필요합니다. 하나의 모델로 두 가지를 모두 만족시키려 하면, 조회 성능을 위해 도메인 모델에 탐색용 프로퍼티를 추가하거나, 반대로 도메인 무결성을 위해 조회 쿼리가 불필요하게 복잡해집니다.

CQRS solves this problem by separating the read path (Query) from the write path (Command). Command는 EF Core로 Aggregate를 영속화하고, Query는 Dapper로 SQL을 직접 작성하여 각각 최적의 기술을 Optional할 수 있습니다.

### Benefits of Command/Query Separation

The following table 통합 모델과 CQRS의 차이를 비교합니다. 핵심 이점은 Command와 Query가 각각 최적의 기술 스택을 Optional할 수 있다는 점입니다.

| Aspect | 통합 모델 | CQRS |
|------|----------|------|
| 읽기/쓰기 최적화 | 동일 모델로 타협 | 각각 최적화 가능 |
| 기술 스택 | 동일 ORM 강제 | **Command: EF Core, Query: Dapper** 독립 Optional |
| 확장성 | 함께 확장 | 독립적 확장 |
| 복잡성 관리 | 한 곳에 집중 | 관심사 분리 |

### Technology Separation in the Adapter Layer

CQRS의 이점을 Adapter 계층에서 실현합니다:

| 측면 | Command | Query |
|------|---------|-------|
| **Adapter 유형** | Repository (`IRepository<T, TId>`) | Query Adapter (`IQueryPort<TEntity, TDto>`) |
| **ORM** | EF Core | Dapper + 명시적 SQL |
| **이유** | 변경 추적, UnitOfWork, 마이그레이션 | 성능 극대화, SQL 튜닝 용이 |
| **반환 타입** | Domain Entity (`FinT<IO, T>`) | DTO (`FinT<IO, PagedResult<TDto>>`) |
| **Port 위치** | Domain Layer | Application Layer |

> 상세 구현은 [13-adapters.md](../adapter/13-adapters) §2.6 Query Adapter 참조

### 유스케이스 = 비즈니스 의도의 명시적 표현

Functorium에서 각 유스케이스는 하나의 클래스로 표현됩니다. `CreateProductCommand`, `GetProductByIdQuery`처럼 비즈니스 의도가 클래스 이름에 드러납니다.

## Summary

### Key Interfaces

| Purpose | Request 인터페이스 | Handler 인터페이스 |
|------|-------------------|-------------------|
| Command | `ICommandRequest<TSuccess>` | `ICommandUsecase<TCommand, TSuccess>` |
| Query | `IQueryRequest<TSuccess>` | `IQueryUsecase<TQuery, TSuccess>` |
| Event | `IDomainEvent` | `IDomainEventHandler<TEvent>` |

### Key Types

| Type | Purpose | 계층 |
|------|------|------|
| `Fin<A>` | LanguageExt 성공/실패 타입 | Domain 또는 Adapter |
| `FinT<IO, A>` | IO 효과를 포함한 Fin 타입 | Repository/Adapter |
| `FinResponse<A>` | Functorium Response 성공/실패 타입 | Usecase |
| `Error` | 오류 정보 | 공통 |
| `ICacheable` | Query 캐싱 마커 인터페이스 (`CacheKey`, `Duration`) | Usecase |

### Recommended Implementation Pattern

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;

public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);
    public sealed class Validator : AbstractValidator<Request> { ... }

    internal sealed class Usecase(
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 검증 + Apply 병합
            var productResult = CreateProduct(request);
            if (productResult.IsFail)
            {
                return productResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 2. LINQ 쿼리로 비즈니스 로직 처리
            var productName = ProductName.Create(request.Name).Unwrap();

            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from product in _productRepository.Create((Product)productResult)
                select new Response(...);
            // SaveChanges + 도메인 이벤트 발행은 UsecaseTransactionPipeline이 자동 처리

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Fin<Product> CreateProduct(Request request)
        {
            var name = ProductName.Validate(request.Name);
            var description = ProductDescription.Validate(request.Description);
            var price = Money.Validate(request.Price);
            var stockQuantity = Quantity.Validate(request.StockQuantity);

            return (name, description, price, stockQuantity)
                .Apply((n, d, p, s) =>
                    Product.Create(
                        ProductName.Create(n).Unwrap(),
                        ProductDescription.Create(d).Unwrap(),
                        Money.Create(p).Unwrap(),
                        Quantity.Create(s).Unwrap()))
                .As()
                .ToFin();
        }
    }
}
```

### ApplyT vs Unwrap Optional 기준

| Criteria | Unwrap | ApplyT |
|------|--------|--------|
| VO 개수 | 1~2개 | 3개 이상 |
| 에러 처리 | 첫 에러에서 즉시 반환 | 모든 에러를 병렬 수집 |
| 코드 스타일 | 명령형 (`var x = ...`) | 선언형 (LINQ `from`) |
| 학습 곡선 | 낮음 | 높음 (모나드 트랜스포머) |
| 적합한 상황 | 간단한 Command, 내부 서비스 | 사용자 입력 폼, 복잡한 검증 |

**판단 기준:** VO가 1~2개이고 에러를 병렬 수집할 필요가 없으면 Unwrap이 더 간결합니다.
VO가 3개 이상이거나 사용자에게 모든 검증 오류를 한 번에 보여줘야 하면 ApplyT를 사용합니다.

요약에서 전체 구조를 파악했으니, 이제 CQRS 패턴의 구체적인 구조를 알아봅니다.

---

## CQRS Pattern Overview

### Command and Query Separation

| Category | Command | Query |
|------|---------|-------|
| Purpose | State change (write) | Data query (read) |
| Example | Create, Update, Delete | GetById, GetAll, Search |
| 반환 | Created/modified entity info | Retrieved data |

### Mediator Pattern Integration

Functorium CQRS는 [Mediator](https://github.com/martinothamar/Mediator) 라이브러리를 기반으로 합니다:

```csharp
// Request는 ICommand 또는 IQuery를 상속
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>> { }
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>> { }

// Handler는 ICommandHandler 또는 IQueryHandler를 상속
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess> { }
```

---

## Project Structure

### Recommended Folder Structure

```
{프로젝트}.Application/
├── Ports/
│   └── I{인터페이스}.cs                # 기술 관심사 인터페이스
└── Usecases/
    ├── {엔티티}/
    │   ├── Create{엔티티}Command.cs    # Command Use Case
    │   ├── Update{엔티티}Command.cs    # Command Use Case
    │   ├── Get{엔티티}ByIdQuery.cs     # Query Use Case
    │   ├── GetAll{엔티티}sQuery.cs     # Query Use Case
    │   ├── On{엔티티}Created.cs        # Event Use Case
    │   └── On{엔티티}Updated.cs        # Event Use Case
    └── ...
```

> **Note**: Event Handlers are also a type of Use Case. As Event-Driven Use Cases, they are placed in the same folder alongside Commands/Queries.

We have confirmed the overall structure of the CQRS pattern and Mediator integration. 다음 섹션에서는 하나의 유스케이스를 구성하는 중첩 클래스 패턴을 살펴봅니다.

---

## Nested Class Pattern

### Pattern Description

Request, Response, Validator, and Usecase composing a single use case are defined as nested classes in one file.

**Advantages:**
- 관련 코드가 한 곳에 모여 응집도 향상
- 파일 탐색 없이 유스케이스 전체 파악 가능
- 네이밍 충돌 방지 (`CreateProductCommand.Request` vs `UpdateProductCommand.Request`)

### Basic Structure

```csharp
/// <summary>
/// {기능 설명}
/// </summary>
public sealed class {동사}{엔티티}{Command|Query}
{
    /// <summary>
    /// {Command|Query} Request - {요청 데이터 설명}
    /// </summary>
    public sealed record Request(...) : I{Command|Query}Request<Response>;

    /// <summary>
    /// {Command|Query} Response - {응답 데이터 설명}
    /// </summary>
    public sealed record Response(...);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙 (선택)
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            // 검증 규칙 정의
        }
    }

    /// <summary>
    /// {Command|Query} Handler - {비즈니스 로직 설명}
    /// </summary>
    internal sealed class Usecase(...) : I{Command|Query}Usecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // 구현 (Validator 통과 후 실행됨)
            // Application 에러: ApplicationError.For<{UsecaseName}>(new {ErrorType}(), value, message) 사용
        }
    }
}
```

### Components

| 클래스 | Access Modifier | Required | Description |
|--------|-----------|----------|------|
| `Request` | `public` | Required | Input data definition |
| `Response` | `public` | Required | Output data definition |
| `Validator` | `public` | Optional | FluentValidation validation rules |
| `Usecase` | `internal` | Required | Business logic implementation |

> **Note**: When a `Validator` is defined, it is automatically validated before Handler execution through the Pipeline.

중첩 클래스의 구조를 이해했습니다. 다음 섹션에서는 Usecase 내에서 여러 Value Object를 동시에 검증하고 Entity를 생성하는 Apply 병합 패턴을 다룹니다.

---

## Value Object Validation and Apply Merge Pattern

### Dual Validation Strategy

Usecase에서는 두 가지 검증 레이어가 있습니다. FluentValidation은 빠른 형식 검증을, Value Object는 도메인 Immutable식 검증을 담당합니다.

| Validation Layer | Responsible | Purpose |
|------------|------|------|
| **FluentValidation** | Presentation Layer | 빠른 입력 형식 검증 |
| **Value Object Validate()** | Domain Layer | 도메인 Immutable식 검증 |

### Apply Merge Pattern

The Apply pattern is used when validating multiple Value Objects simultaneously and creating an Entity.

The key point in the following code is `Validate()`로 모든 필드를 먼저 검증한 뒤, `Apply()`로 병렬 검증 결과를 병합하고, 이미 검증된 값을 `Unwrap()`로 안전하게 변환한다는 것입니다.

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // 1. 모든 필드: VO Validate() 호출 (Validation<Error, T> 반환)
    var name = ProductName.Validate(request.Name);
    var description = ProductDescription.Validate(request.Description);
    var price = Money.Validate(request.Price);
    var stockQuantity = Quantity.Validate(request.StockQuantity);

    // 2. Apply로 병렬 검증 후 Entity 생성
    return (name, description, price, stockQuantity)
        .Apply((n, d, p, s) =>
            Product.Create(
                ProductName.Create(n).Unwrap(),
                ProductDescription.Create(d).Unwrap(),
                Money.Create(p).Unwrap(),
                Quantity.Create(s).Unwrap()))
        .As()
        .ToFin();
}
```

### Pattern Description

| Step | Method | Description |
|------|--------|------|
| 1 | `Validate()` | 모든 필드의 검증을 `Validation<Error, T>`로 수집 |
| 2 | `Apply()` | 모든 검증이 성공해야 Entity 생성 진행 (병렬 검증) |
| 3 | `Unwrap()` | 이미 검증된 값이므로 안전하게 VO 변환 |
| 4 | `As().ToFin()` | `Validation` 타입을 `Fin` 타입으로 변환 |

### VO가 없는 필드의 검증

모든 필드가 Value Object로 정의되지 않을 경우 Named Context 검증을 사용합니다:

```csharp
private static Fin<Product> CreateProduct(Request request)
{
    // VO가 있는 필드
    var name = ProductName.Validate(request.Name);
    var price = Money.Validate(request.Price);

    // VO가 없는 필드: Named Context 사용
    var note = ValidationRules.For("Note")
        .NotEmpty(request.Note)
        .ThenMaxLength(500);

    // 모두 튜플로 병합 - Apply로 병렬 검증
    return (name, price, note.Value)
        .Apply((n, p, noteValue) =>
            Product.Create(
                ProductName.Create(n).Unwrap(),
                noteValue,
                Money.Create(p).Unwrap()))
        .As()
        .ToFin();
}
```

> **Recommended**: Define frequently used fields as separate ValueObjects instead of Named Context.

---

## LINQ-Based Functional Implementation

### Recommendations

**LINQ 기반 함수형 구현을 우선 권장합니다.** 기존 명령형 구현보다 다음과 같은 장점이 있습니다:

- **코드 간결성**: 명령형 if문과 중간 변수 제거 (50-60% 코드 감소)
- **에러 처리 자동화**: Repository 실패 시 자동으로 `FinT.Fail` 반환
- **가독성 향상**: 선언적 LINQ 쿼리로 비즈니스 로직 명확화
- **유지보수성**: 함수형 체이닝으로 변경 영향 최소화

### guard를 활용한 조건 검사

LanguageExt의 `guard`를 사용하여 함수형 조건 검사를 구현합니다.

The key point in the following code is `guard(!exists, error)`가 조건이 `false`일 때 즉시 실패를 반환하여, 명령형 `if` + `return` 패턴 없이 LINQ 체인 안에서 조건 검사를 선언적으로 표현한다는 것입니다.

```csharp
using static Functorium.Applications.Errors.ApplicationErrorType;

// LINQ 쿼리에서 guard 사용
from exists in _productRepository.ExistsByName(productName)
from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
    new AlreadyExists(),
    request.Name,
    $"Product name already exists: '{request.Name}'"))
from product in _productRepository.Create(...)
select new Response(...)
```

`guard(condition, error)`는 조건이 `false`일 때 `FinT.Fail`을 반환합니다.

### guard() 함수란?

`guard()`는 LanguageExt가 제공하는 함수로, LINQ comprehension 구문에서 조건부 단락(short-circuit)을 수행합니다. 조건이 `false`이면 지정된 에러로 즉시 실패하고, `true`이면 `Unit`을 반환하여 다음 단계로 진행합니다.

```csharp
// guard() in LINQ comprehension
from _  in guard(condition, Error.New("error message"))

// 동등한 명령형 코드
if (!condition) return Fin.Fail<T>(Error.New("error message"));
```

`guard()`를 사용하면 명령형 `if` + `return` 패턴 없이 LINQ 체인 안에서 조건 검사를 선언적으로 표현할 수 있습니다. 반환 타입이 `Fin<Unit>`이므로 `FinT<IO, T>` 체인에서 자동 리프팅됩니다.

### 실행 흐름

```csharp
FinT<IO, Response> usecase = ...;

// FinT<IO, Response>
//  -Run()→           IO<Fin<Response>>
//  -RunAsync()→      Fin<Response>
//  -ToFinResponse()→ FinResponse<Response>
Fin<Response> response = await usecase.Run().RunAsync();
return response.ToFinResponse();
```

---

## Application Error Usage Patterns

### 사용 방법

`ApplicationError.For<TUsecase>()` 메서드와 `ApplicationErrorType` sealed record를 사용합니다:

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;

// LINQ 쿼리 내 guard에서 사용
from exists in _productRepository.ExistsByName(productName)
from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
    new AlreadyExists(),
    request.Name,
    $"Product name already exists: '{request.Name}'"))
from product in _productRepository.Create(...)
select new Response(...)

// 직접 반환할 때
return FinResponse.Fail<Response>(
    ApplicationError.For<GetProductByIdQuery>(
        new NotFound(),
        productId.ToString(),
        $"Product not found. ID: {productId}"));
```

### Key ApplicationErrorType

The following table Functorium이 제공하는 표준 Application 에러 타입입니다. 대부분의 유스케이스는 이 표의 타입만으로 충분하며, 특수한 경우 `Custom`을 상속하여 확장합니다.

| Error Type | Description | 사용 예시 |
|-----------|------|----------|
| `Empty` | 값이 비어있음 | `new Empty()` |
| `Null` | 값이 null임 | `new Null()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `InvalidState` | 유효하지 않은 상태 | `new InvalidState()` |
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |
| `ValidationFailed` | 검증 실패 | `new ValidationFailed(PropertyName: "Email")` |
| `BusinessRuleViolated` | 비즈니스 규칙 위반 | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `ConcurrencyConflict` | 동시성 충돌 | `new ConcurrencyConflict()` |
| `ResourceLocked` | 리소스 잠금 | `new ResourceLocked(ResourceName: "Order")` |
| `OperationCancelled` | 작업 취소됨 | `new OperationCancelled()` |
| `InsufficientPermission` | 권한 부족 | `new InsufficientPermission(Permission: "Admin")` |
| `Custom` | 커스텀 에러 (상속 정의) | `public sealed record PaymentDeclined : ApplicationErrorType.Custom;` → `new PaymentDeclined()` |

### Error Code Format

```
ApplicationErrors.{UsecaseName}.{ErrorTypeName}
```

예시:
- `ApplicationErrors.CreateProductCommand.AlreadyExists`
- `ApplicationErrors.GetProductByIdQuery.NotFound`
- `ApplicationErrors.UpdateOrderCommand.BusinessRuleViolated`

### 장점

- **타입 안전성**: sealed record 기반으로 컴파일 타임 검증
- **일관성**: DomainError, AdapterError와 동일한 API 패턴
- **간결함**: 별도 클래스 정의 없이 인라인 사용 가능
- **표준화**: `ApplicationErrorType`의 표준 에러 타입 활용

---

## Command Implementation

### 완전한 Command 예제

```csharp
using LayeredArch.Domain.Entities;
using LayeredArch.Domain.ValueObjects;
using LayeredArch.Domain.Repositories;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Create product Command - Apply 패턴 + LINQ 구현
/// </summary>
public sealed class CreateProductCommand
{
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(ProductName.MaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(ProductDescription.MaxLength);

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    internal sealed class Usecase(
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 검증 + Apply 병합
            var productResult = CreateProduct(request);
            if (productResult.IsFail)
            {
                return productResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 2. ProductName 생성 (중복 검사용)
            var productName = ProductName.Create(request.Name).Unwrap();

            // 3. LINQ로 중복 검사 + 저장 (SaveChanges + 이벤트 발행은 파이프라인이 자동 처리)
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from product in _productRepository.Create((Product)productResult)
                select new Response(
                    product.Id.ToString(),
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Fin<Product> CreateProduct(Request request)
        {
            var name = ProductName.Validate(request.Name);
            var description = ProductDescription.Validate(request.Description);
            var price = Money.Validate(request.Price);
            var stockQuantity = Quantity.Validate(request.StockQuantity);

            return (name, description, price, stockQuantity)
                .Apply((n, d, p, s) =>
                    Product.Create(
                        ProductName.Create(n).Unwrap(),
                        ProductDescription.Create(d).Unwrap(),
                        Money.Create(p).Unwrap(),
                        Quantity.Create(s).Unwrap()))
                .As()
                .ToFin();
        }
    }
}
```

---

## Query Implementation

> **Core Principle**: Queries do not use `IRepository`. `IQueryPort` 기반 Read Adapter를 통해 **Aggregate 재구성 없이 SQL → DTO 직접 매핑합니다.** 이 규칙은 `CqrsArchitectureRuleTests`로 강제됩니다.

### Query Port 정의 패턴

Query에서 사용하는 Port는 Application 레이어에 정의합니다 (Domain의 `IRepository`와 다름):

| Pattern | 인터페이스 | Purpose | Adapter 기반 클래스 |
|------|-----------|------|-------------------|
| 목록/검색 | `IQueryPort<TEntity, TDto>` | `Search(spec, page, sort)` → `PagedResult<TDto>` | `DapperQueryBase<TEntity, TDto>` |
| 단일 조회 | `IQueryPort` (비제네릭) | 커스텀 메서드 직접 정의 | 직접 구현 |

#### 목록 조회용 Port 정의

```csharp
// Application/Usecases/Products/Ports/IProductQuery.cs
using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IProductQuery : IQueryPort<Product, ProductSummaryDto> { }

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price);
```

#### 단일 조회용 Port 정의

```csharp
// Application/Usecases/Products/Ports/IProductDetailQuery.cs
using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product 단건 조회용 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IProductDetailQuery : IQueryPort
{
    FinT<IO, ProductDetailDto> GetById(ProductId id);
}

public sealed record ProductDetailDto(
    string ProductId,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAt,
    Option<DateTime> UpdatedAt);
```

### 단일 조회 Query 예제

`IQueryPort` (비제네릭)를 확장한 커스텀 Port를 주입합니다:

```csharp
// 참조: Tests.Hosts/01-SingleHost/.../GetCustomerByIdQuery.cs
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;

public sealed class GetCustomerByIdQuery
{
    public sealed record Request(string CustomerId) : IQueryRequest<Response>;

    public sealed record Response(
        string CustomerId,
        string Name,
        string Email,
        decimal CreditLimit,
        DateTime CreatedAt);

    public sealed class Usecase(ICustomerDetailQuery customerDetailQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly ICustomerDetailQuery _adapter = customerDetailQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var customerId = CustomerId.Create(request.CustomerId);
            FinT<IO, Response> usecase =
                from dto in _adapter.GetById(customerId)
                select new Response(
                    dto.CustomerId,
                    dto.Name,
                    dto.Email,
                    dto.CreditLimit,
                    dto.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

> **Key Point**: There is no Entity → DTO conversion code. The Adapter returns DTOs directly via SQL.

### 목록/검색 Query 예제

`IQueryPort<TEntity, TDto>`의 `Search()` 메서드와 Specification 패턴을 사용합니다:

```csharp
// 참조: samples/ecommerce-ddd/.../SearchProductsQuery.cs
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using ECommerce.Application.Usecases.Products.Ports;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;

public sealed class SearchProductsQuery
{
    private static readonly string[] AllowedSortFields = ["Name", "Price"];

    // Option<T>: 선택적 필터 필드. default(Option<T>) = None → 필터 미적용
    public sealed record Request(
        Option<string> Name = default,
        Option<decimal> MinPrice = default,
        Option<decimal> MaxPrice = default,
        int Page = 1,
        int PageSize = PageRequest.DefaultPageSize,
        string SortBy = "",
        string SortDirection = "") : IQueryRequest<Response>;

    public sealed record Response(
        IReadOnlyList<ProductSummaryDto> Products,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNextPage,
        bool HasPreviousPage);

    // Validator: Option<T> 전용 검증 확장 메서드 활용
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .MustSatisfyValidation(ProductName.Validate);

            this.MustBePairedRange(
                x => x.MinPrice,
                x => x.MaxPrice,
                Money.Validate,
                inclusive: true);

            RuleFor(x => x.SortBy).MustBeOneOf(AllowedSortFields);

            RuleFor(x => x.SortDirection)
                .MustBeEnumValue<Request, SortDirection>();
        }
    }

    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQuery _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);
            var pageRequest = new PageRequest(request.Page, request.PageSize);
            var sortExpression = SortExpression.By(request.SortBy, SortDirection.Parse(request.SortDirection));

            FinT<IO, Response> usecase =
                from result in _productQuery.Search(spec, pageRequest, sortExpression)
                select new Response(
                    result.Items,
                    result.TotalCount,
                    result.Page,
                    result.PageSize,
                    result.TotalPages,
                    result.HasNextPage,
                    result.HasPreviousPage);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        private static Specification<Product> BuildSpecification(Request request)
        {
            var spec = Specification<Product>.All;

            // Option<T>.Iter(): Some이면 필터 추가, None이면 무시
            request.Name.Iter(name =>
                spec &= new ProductNameSpec(
                    ProductName.Create(name).Unwrap()));

            // Bind().Map().Iter(): 두 Option이 모두 Some일 때만 범위 필터 추가
            request.MinPrice.Bind(min => request.MaxPrice.Map(max => (min, max)))
                .Iter(t => spec &= new ProductPriceRangeSpec(
                    Money.Create(t.min).Unwrap(),
                    Money.Create(t.max).Unwrap()));

            return spec;
        }
    }
}
```

> **Note**: Specification 패턴의 정의, 조합, Repository 통합에 대한 상세는 [10-specifications.md](../domain/10-specifications)를 참조하세요.

### 전체 조회 (필터 없음)

```csharp
// 참조: Tests.Hosts/01-SingleHost/.../GetAllProductsQuery.cs
public sealed class GetAllProductsQuery
{
    public sealed record Request() : IQueryRequest<Response>;

    public sealed record Response(IReadOnlyList<ProductSummaryDto> Products);

    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductQuery _productQuery = productQuery;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            PageRequest pageRequest = new(1, int.MaxValue);

            FinT<IO, Response> usecase =
                from result in _productQuery.Search(Specification<Product>.All, pageRequest, SortExpression.Empty)
                select new Response(result.Items);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

---

## Domain Events

도메인 이벤트 발행과 Event Handler 구현에 대한 내용은 [07-domain-events.md](../domain/07-domain-events)를 참조하세요.

---

## Source Generator CtxEnricher

### 자동 생성

`ICommandRequest<T>` 또는 `IQueryRequest<T>`를 구현하는 Request record를 정의하면, `CtxEnricherGenerator`가 해당 Request/Response의 스칼라 속성을 `ctx.*` 필드로 자동 변환하는 `IUsecaseCtxEnricher<TRequest, TResponse>` 구현 코드를 생성합니다.

```csharp
public sealed class PlaceOrderCommand
{
    public sealed record Request(string CustomerId, List<OrderLine> Lines)
        : ICommandRequest<Response>, ICustomerRequest;
    //   CustomerId → ctx.customer_id  (Root: ICustomerRequest에 [CtxRoot])
    //   Lines      → ctx.place_order_command.request.lines_count  (컬렉션 → _count)

    public sealed record Response(string OrderId, int LineCount, decimal TotalAmount);
    //   OrderId     → ctx.place_order_command.response.order_id
    //   LineCount   → ctx.place_order_command.response.line_count
    //   TotalAmount → ctx.place_order_command.response.total_amount
}
```

### `[CtxRoot]` — 교차 Usecase 검색

`[CtxRoot]`를 인터페이스에 적용하면, 해당 인터페이스의 속성이 Usecase prefix 없이 `ctx.{field}`로 승격됩니다. 여러 Usecase가 같은 인터페이스를 구현하면 OpenSearch에서 `ctx.customer_id: "CUST-001"` 하나로 모든 활동을 검색할 수 있습니다:

```csharp
[CtxRoot]
public interface ICustomerRequest { string CustomerId { get; } }
```

### `[CtxIgnore]` — 생성 제외

민감 정보나 불필요한 속성을 Enricher 생성에서 제외합니다:

```csharp
public sealed record Request(
    string CustomerId,
    [property: CtxIgnore] string InternalToken  // ctx 필드 생성 안 함
) : ICommandRequest<Response>;
```

> **상세**: [Logging 매뉴얼 §Source Generator CtxEnricher](../observability/19-observability-logging#source-generator-자동-생성-ctxenrichergenerator) 참조.

---

## Transactions and Event Publishing (UsecaseTransactionPipeline)

### 파이프라인 자동 처리

Command의 트랜잭션 커밋(`SaveChanges`)과 도메인 이벤트 발행은 `UsecaseTransactionPipeline`이 자동으로 처리합니다. **Usecase에서 `IUnitOfWork`나 `IDomainEventPublisher`를 직접 주입할 필요가 없습니다.**

```
[Command Handler]
  ↓ Repository.Create(aggregate)
  ↓   → IDomainEventCollector.Track(aggregate)  ← Repository가 자동 호출
  ↓ return FinResponse.Succ(response)
  ↓
[UsecaseTransactionPipeline]
  1. BeginTransactionAsync()           ← 명시적 트랜잭션 시작
  2. response = await next()           ← Handler 실행
  3. if (response.IsFail) return       ← 실패 시 트랜잭션 Dispose로 롤백
  4. UoW.SaveChanges()                 ← 변경사항 저장
  5. transaction.CommitAsync()         ← 트랜잭션 커밋
  6. PublishTrackedEvents()            ← 이벤트 수집·발행·클리어
  7. return response                   ← 원래 성공 응답 반환
```

### Usecase 생성자 패턴

```csharp
// Command: Repository만 주입 (SaveChanges + 이벤트 발행은 파이프라인이 처리)
internal sealed class Usecase(
    IProductRepository productRepository)
    : ICommandUsecase<Request, Response>

// Query: IQueryPort 기반 Read Adapter 주입 (Transaction은 where ICommand 제약으로 컴파일 타임 제외)
internal sealed class Usecase(IProductQuery productQuery)
    : IQueryUsecase<Request, Response>
```

### Command LINQ 패턴

```csharp
FinT<IO, Response> usecase =
    from product in _productRepository.Create(newProduct)  // Repository 변경
    select new Response(...);
// SaveChanges + 도메인 이벤트 발행은 UsecaseTransactionPipeline이 자동 처리
```

### 파이프라인 실행 순서

```
[Command] Request → Metrics → Tracing → Logging → Validation → Exception → Transaction → Custom → Handler
[Query]   Request → Metrics → Tracing → Logging → Validation → Caching → Exception → Custom → Handler
```

- **Transaction**은 `where TRequest : ICommand<TResponse>` 제약으로 Command에만 적용 (컴파일 타임 필터링)
- **Caching**은 `where TRequest : IQuery<TResponse>` 제약으로 Query에만 적용 (컴파일 타임 필터링)

- Transaction은 Exception 뒤에 위치 → `SaveChanges` 예외 발생 시 Exception 파이프라인이 처리
- Transaction은 `where ICommand<TResponse>` 제약으로 Command에만 적용 (컴파일 타임)
- Caching은 `where IQuery<TResponse>` 제약으로 Query에만 적용 (컴파일 타임)

### 파이프라인 등록

명시적 opt-in으로 Transaction 파이프라인을 활성화합니다:

```csharp
services
    .RegisterOpenTelemetry(configuration, AssemblyReference.Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()   // CtxEnricher, Metrics, Tracing, Logging 일괄 활성화
        .UseValidation()
        .UseCaching()         // Caching은 별도 활성화 필요
        .UseException()
        .UseTransaction())    // Transaction 명시적 활성화
    .Build();
```

> Transaction 파이프라인은 `IUnitOfWork`, `IDomainEventPublisher`, `IDomainEventCollector` 세 가지 모두 DI에 등록되어 있어야 합니다 (`HasTransactionDependencies`로 검증).

### 트랜잭션 격리와 동시성

여러 Repository가 하나의 DbContext를 공유하므로 기본 격리 수준은 Read Committed이며, 동시성 충돌은 EF Core의 Optimistic Concurrency(`[ConcurrencyCheck]` 또는 `IsConcurrencyToken()`)로 처리합니다. Optimistic Concurrency 충돌 시 `DbUpdateConcurrencyException`이 발생하고, `UsecaseExceptionPipeline`이 이를 `FinResponse.Fail`로 변환합니다.

### 핵심 원칙

| Principle | Description |
|------|------|
| SaveChanges 호출 위치 | **파이프라인이** 자동 처리 (Usecase에서 호출하지 않음) |
| Repository 역할 | 엔티티 변경 + `IDomainEventCollector.Track()` 호출 |
| 여러 Repository 호출 | 하나의 `SaveChanges()`로 트랜잭션에 묶임 (파이프라인 보장) |
| 이벤트 발행 시점 | `SaveChanges()` 성공 후에만 발행 (파이프라인 보장) |
| 이벤트 발행 실패 시 | 성공 응답 유지 (데이터는 이미 커밋됨, 경고 로그만 기록) |
| Query에서의 동작 | `where ICommand<TResponse>` 제약으로 컴파일 타임 제외 |

### IUnitOfWork 인터페이스

**Location**: `Functorium.Applications.Persistence`

```csharp
public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);

    /// <summary>
    /// 명시적 트랜잭션을 시작합니다.
    /// ExecuteDeleteAsync/ExecuteUpdateAsync 등 즉시 실행 SQL과 SaveChanges를
    /// 동일 트랜잭션으로 묶어야 할 때 사용합니다.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
```

**IUnitOfWorkTransaction 인터페이스:**

```csharp
/// <summary>
/// 명시적 트랜잭션 스코프.
/// Dispose 시 미커밋 트랜잭션은 자동 롤백됩니다.
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

- `IObservablePort`를 상속하므로 Pipeline 자동 생성 및 관찰성을 지원합니다.
- EF Core 환경에서는 `DbContext.SaveChangesAsync()`를 호출하고, InMemory 환경에서는 no-op입니다.
- `BeginTransactionAsync()`는 `UsecaseTransactionPipeline`이 자동 호출하므로 Usecase에서 직접 사용할 필요가 없습니다.

> **참조**: UoW Adapter 구현(EfCoreUnitOfWork, InMemoryUnitOfWork)은 [13-adapters.md](../adapter/13-adapters)를 참조하세요.

---

## FinResponse와 오류 처리

### FinResponse 타입

```csharp
public abstract record FinResponse<A>
{
    public sealed record Succ(A Value) : FinResponse<A>;
    public sealed record Fail(Error Error) : FinResponse<A>;

    public abstract bool IsSucc { get; }
    public abstract bool IsFail { get; }
}
```

### 암시적 변환

```csharp
// 성공 반환 - 값을 직접 반환
return new Response(productId, name);

// 실패 반환 - Error를 직접 반환
return Error.New("상품을 찾을 수 없습니다");

// FinResponse.Fail 사용
return FinResponse.Fail<Response>(error);
```

### Fin → FinResponse 변환

```csharp
Fin<Response> fin = await usecase.Run().RunAsync();

// 타입만 변환
FinResponse<Response> response = fin.ToFinResponse();

// 값을 매핑하면서 변환
return fin.ToFinResponse(product => new Response(...));
```

---

## FluentValidation 통합

### Define validation rules

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("상품명은 필수입니다")
            .MaximumLength(ProductName.MaxLength)
            .WithMessage($"상품명은 {ProductName.MaxLength}자를 초과할 수 없습니다");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");
    }
}
```

### Pipeline을 통한 자동 검증

`UsecaseValidationPipeline`은 `ConfigurePipelines`의 `UseValidation()`으로 등록됩니다. Handler 실행 전에 자동으로 Validator가 실행됩니다:

```csharp
services
    .AddValidatorsFromAssembly(typeof(Program).Assembly)
    .ConfigurePipelines(pipelines => pipelines
        .UseObservability()   // CtxEnricher, Metrics, Tracing, Logging 일괄 활성화
        .UseValidation()      // Validation 명시적 활성화
        .UseException());
```

### FluentValidation 실패와 에러 타입 매핑

FluentValidation 검증 실패는 `UsecaseValidationPipeline`에서 `AdapterErrorType.PipelineValidation`으로 변환됩니다. 이는 Application 레이어의 `ApplicationErrorType.ValidationFailed`와는 다른 에러 타입입니다:

| Validation Layer | Error Type | 사용 위치 |
|------------|----------|----------|
| FluentValidation (Pipeline) | `AdapterErrorType.PipelineValidation(PropertyName)` | `UsecaseValidationPipeline` 자동 처리 |
| VO/비즈니스 규칙 (Usecase) | `ApplicationErrorType.ValidationFailed(PropertyName)` | Usecase 내 수동 사용 |

FluentValidation 실패 시 각 `ValidationFailure`의 `PropertyName`과 `ErrorMessage`가 `AdapterError.For<UsecaseValidationPipeline>(new PipelineValidation(PropertyName), ...)` 형태로 변환되어 `FinResponse.Fail`로 반환됩니다.

### Value Object 검증 확장 메서드

Functorium은 C#14 extension members 문법을 사용하여 Value Object의 `Validate()` 메서드를 FluentValidation 규칙으로 통합하는 확장 메서드를 제공합니다:

| Method | 사용 조건 | Example |
|--------|----------|------|
| `MustSatisfyValidation` | 입력 타입 == 출력 타입 | `RuleFor(x => x.Price).MustSatisfyValidation(Money.ValidateAmount)` |
| `MustSatisfyValidationOf<TVO>` | 입력 타입 != 출력 타입 | `RuleFor(x => x.Name).MustSatisfyValidationOf<ProductName>(ProductName.Validate)` |

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // 입력/출력 타입 동일: decimal → Validation<Error, decimal>
        RuleFor(x => x.Price)
            .MustSatisfyValidation(Money.ValidateAmount);

        // 입력/출력 타입 다름: string → Validation<Error, ProductName>
        RuleFor(x => x.Name)
            .MustSatisfyValidationOf<ProductName>(ProductName.Validate);
    }
}
```

> **Note**: `MustSatisfyValidationOf`는 C#14 extension members의 타입 추론 제한으로 `IRuleBuilderInitial`에서 추가 제네릭 파라미터 해결이 안 되는 경우, 전통적인 확장 메서드 오버로드(`MustSatisfyValidationOf<TRequest, TProperty, TValueObject>`)도 제공됩니다.

### EntityId / OneOf / PairedRange Validation Extension Methods

Functorium additionally provides extension methods for frequently used validation patterns:

| Method | Purpose | Example |
|--------|------|------|
| `MustBeEntityId<TRequest, TEntityId>` | Validates that a string is a valid EntityId format (NotEmpty + TryParse combined) | `RuleFor(x => x.ProductId).MustBeEntityId<Request, ProductId>()` |
| `MustBeOneOf<TRequest>` | Validates that a value is one of the allowed string list (case-insensitive, skips null/empty) | `RuleFor(x => x.SortBy).MustBeOneOf<Request>(["Name", "Price"])` |
| `MustBePairedRange<TRequest, T>` | Validates `Option<T>` paired range filter (both None = pass, only one Some = fail, both Some = range validation) | See example below |

```csharp
public sealed class Validator : AbstractValidator<Request>
{
    public Validator()
    {
        // EntityId format validation
        RuleFor(x => x.ProductId)
            .MustBeEntityId<Request, ProductId>();

        // Allowed values list validation
        RuleFor(x => x.SortBy)
            .MustBeOneOf<Request>(["Name", "Price", "CreatedAt"]);

        // Option<T> paired range filter validation
        this.MustBePairedRange(
            x => x.MinPrice,
            x => x.MaxPrice,
            Money.Validate);
    }
}
```

### SmartEnum Validation Extension Methods

FluentValidation extension methods for Ardalis.SmartEnum are also provided:

| Method | Purpose |
|--------|------|
| `MustBeEnum<TRequest, TSmartEnum, TValue>` | SmartEnum Value로 검증 |
| `MustBeEnum<TRequest, TSmartEnum>` | int 기반 SmartEnum 간소화 오버로드 |
| `MustBeEnumName<TRequest, TSmartEnum, TValue>` | SmartEnum Name으로 검증 |
| `MustBeEnumValue<TRequest, TSmartEnum>` | string Value SmartEnum (대소문자 무시) |

### ICacheable Interface

Implementing `ICacheable` on a Query Request enables caching support:

```csharp
public sealed record Request(string ProductId) : IQueryRequest<Response>, ICacheable
{
    public string CacheKey => $"Product:{ProductId}";
    public TimeSpan? Duration => TimeSpan.FromMinutes(5);
}
```

`UsecaseCachingPipeline` applies only to Queries via the `where TRequest : IQuery<TResponse>` constraint and automatically caches Query Requests that implement `ICacheable`:
- Uses `IMemoryCache` for cache hit/miss handling based on `CacheKey`
- On cache hit, returns the cached response immediately without calling the Handler
- Only caches when `response.IsSucc` (failure responses are not cached)
- Default 5-minute cache when `Duration` is `null`

---

## Troubleshooting

### Apply 패턴에서 `Validation` → `Fin` 변환 시 컴파일 오류
**Cause:** `Apply()` 결과는 `Validation<Error, T>` 타입인데, `Fin<T>`를 기대하는 곳에서 직접 사용하려 하면 타입 불일치가 발생합니다.
**Solution:** `.As().ToFin()` 체이닝을 사용하여 `Validation` → `Fin`으로 변환하세요. 예: `(name, price).Apply((n, p) => Product.Create(...)).As().ToFin();`

### `FinT<IO, T>` LINQ 쿼리에서 Repository 호출 후 에러 처리가 안됨
**Cause:** LINQ `from...in` 구문에서 Repository가 `FinT.Fail`을 반환하면 자동으로 실패 트랙으로 전환됩니다. 별도의 에러 처리 코드가 필요 없습니다.
**Solution:** LINQ 쿼리 내에서는 `if` 문으로 에러를 처리하지 마세요. Repository 실패는 자동 전파됩니다. 조건 검사가 필요하면 `guard(condition, error)` 함수를 사용하세요.

### Usecase에서 `SaveChanges()`를 직접 호출하면 이중 커밋 발생
**Cause:** `UsecaseTransactionPipeline`이 Handler 성공 후 자동으로 `SaveChanges()`를 호출합니다. Usecase에서 직접 호출하면 두 번 커밋됩니다.
**Solution:** Usecase에서는 `IUnitOfWork`를 주입하지 마세요. `SaveChanges()`와 도메인 이벤트 발행은 모두 파이프라인이 자동 처리합니다. Repository의 `Create()`/`Update()` 호출까지만 작성하면 됩니다.

---

## FAQ

### Q1. FluentValidation과 VO Validate() 둘 다 필요한가요?

**A:** 네, 각각 다른 목적을 가집니다:
- **FluentValidation**: Presentation Layer에서 빠른 형식 검증
- **VO Validate()**: Domain Layer에서 도메인 Immutable식 검증

FluentValidation이 통과해도 VO 검증에서 실패할 수 있습니다 (예: 정규식 패턴 불일치).

### Q2. Apply 병합 패턴은 언제 사용하나요?

**A:** Entity를 생성할 때 여러 VO를 동시에 검증해야 하는 경우 사용합니다. 모든 검증 오류를 한 번에 수집하여 반환합니다.

### Q3. guard는 언제 사용하나요?

**A:** LINQ 쿼리 내에서 조건 검사를 수행할 때 사용합니다:

```csharp
from exists in _repository.ExistsByName(name)
from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
    new AlreadyExists(), name, $"Name already exists: '{name}'"))
```

### Q4. Application 에러는 어떻게 정의하나요?

**A:** `ApplicationError.For<TUsecase>(ApplicationErrorType, value, message)` 패턴을 사용합니다. 별도 클래스 정의 없이 인라인으로 사용합니다. 에러 코드는 자동으로 `ApplicationErrors.{UsecaseName}.{ErrorTypeName}` 형식으로 생성됩니다.

### Q5. Response에 도메인 엔티티를 직접 반환해도 되나요?

**A:** 권장하지 않습니다. Primitive 타입이나 DTO를 사용하세요:

```csharp
// ✗ 비권장 - 도메인 엔티티 노출
public sealed record Response(Product Product);

// ✓ 권장 - Primitive/DTO 사용
public sealed record Response(
    string ProductId,
    string Name,
    decimal Price);
```

### Q6. CancellationToken은 항상 전달해야 하나요?

**A:** 네, 비동기 메서드에는 항상 CancellationToken을 전달하세요. 다만 FinT<IO, T> 패턴을 사용하는 경우 Repository 내부에서 처리됩니다.

> **Query Handler 참고**: Query Handler의 `Handle` 메서드는 `CancellationToken cancellationToken` 파라미터를 받지만, FinT<IO, T> LINQ 체인 내에서는 직접 전달할 위치가 없습니다. CancellationToken은 Adapter 내부의 `IO.liftAsync` 블록에서 필요한 경우 Adapter 메서드 시그니처에 포함하여 전달합니다.

### Q7. SaveChanges와 이벤트 발행은 어디서 처리하나요?

**A:** `UsecaseTransactionPipeline`이 자동으로 처리합니다. Usecase에서 `IUnitOfWork`나 `IDomainEventPublisher`를 직접 주입할 필요가 없습니다.

1. **Usecase는 비즈니스 로직만 담당**: Repository의 `Create()`, `Update()` 호출까지만 작성합니다.
2. **파이프라인이 SaveChanges 자동 호출**: Handler 성공 시 `IUnitOfWork.SaveChanges()`를 호출하고, 실패 시 커밋하지 않습니다.
3. **파이프라인이 도메인 이벤트 자동 발행**: Repository가 `IDomainEventCollector.Track()`으로 추적한 Aggregate의 도메인 이벤트를 `SaveChanges()` 성공 후 자동 발행합니다.

활성화: `.ConfigurePipelines(pipelines => pipelines.UseObservability().UseValidation().UseException().UseTransaction())`

---

## Reference Documents

| Document | Description |
|------|------|
| [05a-value-objects.md](../domain/05a-value-objects) | Value Object implementation patterns |
| [06b-entity-aggregate-core.md](../domain/06b-entity-aggregate-core) | Entity core patterns and Create pattern |
| [07-domain-events.md](../domain/07-domain-events) | Domain event publishing and Event Handler |
| [08a-error-system.md](../domain/08a-error-system) | Error system: foundations and naming |
| [08b-error-system-domain-app.md](../domain/08b-error-system-domain-app) | Error system: Domain/Application errors |
| [08c-error-system-adapter-testing.md](../domain/08c-error-system-adapter-testing) | Error system: Adapter errors and testing |
| [10-specifications.md](../domain/10-specifications) | Specification pattern (used in Use Cases) |
| [12-ports.md](../adapter/12-ports) | Repository interface design |
| [15a-unit-testing.md](../testing/15a-unit-testing) | Usecase test writing methods |

**External References:**
- [Mediator](https://github.com/martinothamar/Mediator) - Base library
- [LanguageExt](https://github.com/louthy/language-ext) - Library providing Fin types

---

## Related Documents

- Port interface definitions used in Use Cases: [Port Definition](../adapter/12-ports)
- Writing Adapters that implement Ports: [Adapter Implementation](../adapter/13-adapters)
- Pipeline and DI registration: [Adapter Integration](../adapter/14a-adapter-pipeline-di)
