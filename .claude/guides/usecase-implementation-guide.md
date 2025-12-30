# 유스케이스 구현 가이드

이 문서는 Functorium CQRS 패턴을 사용하여 유스케이스(Command/Query Handler)를 구현하는 방법을 설명합니다.

## TODO
- [x] Error을 guard 기반 ApplicationErrors로 재구성
- [x] Usecase 구현을 LINQ 기반으로 재구성

## 목차
- [요약](#요약)
- [CQRS 패턴 개요](#cqrs-패턴-개요)
- [프로젝트 구조](#프로젝트-구조)
- [중첩 클래스 패턴](#중첩-클래스-패턴)
- [Command 구현](#command-구현)
- [Query 구현](#query-구현)
- [FinResponse와 오류 처리](#finresponse와-오류-처리)
- [Repository 계층과 Fin 타입](#repository-계층과-fin-타입)
- [Validation 구현](#validation-구현)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

---

## 요약

### 주요 인터페이스

| 용도 | Request 인터페이스 | Handler 인터페이스 |
|------|-------------------|-------------------|
| Command | `ICommandRequest<TSuccess>` | `ICommandUsecase<TCommand, TSuccess>` |
| Query | `IQueryRequest<TSuccess>` | `IQueryUsecase<TQuery, TSuccess>` |

### 주요 타입

| 타입 | 용도 | 계층 |
|------|------|------|
| `Fin<A>` | LanguageExt 성공/실패 타입 | Domain 또는 Adapter |
| `FinResponse<A>` | Functorium Response 성공/실패 타입 | Usecase |
| `Error` | 오류 정보 | 공통 |

### 권장 구현 패턴

```csharp
// LINQ 기반 함수형 구현 (권장)
public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);
    public sealed class Validator : AbstractValidator<Request> { ... }

    /// <summary>
    /// ApplicationErrors 중첩 클래스 - Application 계층 오류 정의
    /// </summary>
    internal static class ApplicationErrors
    {
        public static Error ProductNameAlreadyExists(string productName) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
                errorCurrentValue: productName,
                errorMessage: $"Product name already exists. Current value: '{productName}'");
    }

    internal sealed class Usecase : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식으로 함수형 체이닝
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(request.Name)
                from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
                from product in _productRepository.Create(new Product(...))
                select new Response(...);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

---

## CQRS 패턴 개요

### Command와 Query 분리

| 구분 | Command | Query |
|------|---------|-------|
| 목적 | 상태 변경 (쓰기) | 데이터 조회 (읽기) |
| 예시 | Create, Update, Delete | GetById, GetAll, Search |
| 반환 | 생성/수정된 엔티티 정보 | 조회된 데이터 |

### Mediator 패턴 통합

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

## 프로젝트 구조

### 권장 폴더 구조

```
{프로젝트}.Application/
├── Ports/
│   ├── I{인터페이스}.cs                # 기술 관심사 인터페이스
│   └── {인터페이스}InMemory.cs         # 기술 관심사 인터페이스 메모리
└── Usecases/
    ├── Create{엔티티}Command.cs
    ├── Update{엔티티}Command.cs
    ├── Get{엔티티}ByIdQuery.cs
    └── GetAll{엔티티}sQuery.cs
```

---

## 중첩 클래스 패턴

### 패턴 설명

하나의 유스케이스를 구성하는 Request, Response, Validator, Handler를 하나의 파일에 중첩 클래스로 정의합니다.

**장점:**
- 관련 코드가 한 곳에 모여 응집도 향상
- 파일 탐색 없이 유스케이스 전체 파악 가능
- 네이밍 충돌 방지 (`CreateProductCommand.Request` vs `UpdateProductCommand.Request`)

### 기본 구조

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
        }
    }
}
```

### 구성 요소

| 클래스 | 접근 제한자 | 필수 여부 | 설명 |
|--------|-----------|----------|------|
| `Request` | `public` | 필수 | 입력 데이터 정의 |
| `Response` | `public` | 필수 | 출력 데이터 정의 |
| `Validator` | `public` | 선택 | FluentValidation 검증 규칙 |
| `ApplicationErrors` | `internal` | 선택 | Application 계층 오류 정의 |
| `Usecase` | `internal` | 필수 | 비즈니스 로직 구현 |

> **참고**: `Validator`가 정의되면 Pipeline을 통해 Handler 실행 전에 자동으로 검증됩니다.

---

## LINQ 기반 함수형 구현

### 권장사항

**LINQ 기반 함수형 구현을 우선 권장합니다.** 기존 명령형 구현보다 다음과 같은 장점이 있습니다:

- **코드 간결성**: 명령형 if문과 중간 변수 제거
- **에러 처리 자동화**: Repository 실패 시 자동으로 `FinT.Fail` 반환
- **가독성 향상**: 선언적 LINQ 쿼리로 비즈니스 로직 명확화
- **유지보수성**: 함수형 체이닝으로 변경 영향 최소화
- **표준화**: 모든 Usecase가 동일한 패턴 사용

### ApplicationErrors 패턴

ApplicationErrors 중첩 클래스를 사용하여 오류를 표준화합니다:

```csharp
/// <summary>
/// ApplicationErrors 중첩 클래스 - Application 계층 오류 정의
/// DomainErrors 패턴과 동일한 구조로 오류를 정의하여 일관성 유지
/// </summary>
internal static class ApplicationErrors
{
    /// <summary>
    /// 상품명이 이미 존재하는 경우 발생하는 오류
    /// </summary>
    public static Error ProductNameAlreadyExists(string productName) =>
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
            errorCurrentValue: productName,
            errorMessage: $"Product name already exists. Current value: '{productName}'");
}
```

#### 장점
- **일관성**: DomainErrors와 동일한 패턴
- **표준화**: `ErrorCodeFactory`를 통한 일관된 오류 코드 형식
- **유지보수성**: 오류 메시지와 코드를 한 곳에서 관리
- **타입 안전성**: 정적 메서드로 오류 생성, 컴파일 타임 검증

### guard를 활용한 조건 검사

LanguageExt의 `guard`를 사용하여 함수형 조건 검사를 구현합니다:

```csharp
// LINQ 쿼리에서 guard 사용
from exists in _productRepository.ExistsByName(request.Name)
from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
```

`guard(condition, error)`는 `Guard<Error, Unit>`를 반환하며, `FinTGuardExtensions.SelectMany`를 통해 자동으로 `FinT`로 변환됩니다.

---

## Command 구현

### LINQ 기반 Command 예제 (권장)

```csharp
using Functorium.Abstractions.Errors;
using Functorium.Applications.Linq;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace CqrsFunctional.Demo.Usecases;

/// <summary>
/// 상품 생성 Command - Validation Pipeline 데모
/// FluentValidation을 사용한 입력 검증 예제
/// </summary>
public sealed class CreateProductCommand
{
    /// <summary>
    /// Command Request - 상품 생성에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 상품 정보
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(100).WithMessage("상품명은 100자를 초과할 수 없습니다");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("설명은 500자를 초과할 수 없습니다");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    /// <summary>
    /// ApplicationErrors 중첩 클래스 - Application 계층 오류 정의
    /// DomainErrors 패턴과 동일한 구조로 오류를 정의하여 일관성 유지
    /// </summary>
    internal static class ApplicationErrors
    {
        /// <summary>
        /// 상품명이 이미 존재하는 경우 발생하는 오류
        /// </summary>
        public static Error ProductNameAlreadyExists(string productName) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
                errorCurrentValue: productName,
                errorMessage: $"Product name already exists. Current value: '{productName}'");
    }

    /// <summary>
    /// Command Handler - 실제 비즈니스 로직 구현
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        /// <summary>
        /// LINQ 쿼리 표현식을 사용한 함수형 체이닝
        /// FinTUtilites의 SelectMany 확장 메서드를 통해 FinT 모나드 트랜스포머를 LINQ로 처리
        /// guard를 사용하여 상품명 중복 검사 수행
        /// </summary>
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식: Repository의 FinT<IO, bool>를 사용하여 중복 검사 및 상품 생성
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            // guard를 사용하여 상품명이 존재하지 않을 때만 계속 진행 (exists가 false일 때)
            // ToFinT<IO>() 호출 없이 자동으로 FinT로 변환됨
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(request.Name)
                from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
                from product in _productRepository.Create(new Product(
                    Id: Guid.NewGuid(),
                    Name: request.Name,
                    Description: request.Description,
                    Price: request.Price,
                    StockQuantity: request.StockQuantity,
                    CreatedAt: DateTime.UtcNow))
                select new Response(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt);

            // FinT<IO, Response>
            //  -Run()→           IO<Fin<Response>>
            //  -RunAsync()→      Fin<Response>
            //  -ToFinResponse()→ FinResponse<Response>
            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

### 기존 명령형 Command 예제

```csharp
using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Usecases;

/// <summary>
/// 상품 생성 Command (기존 명령형 구현)
/// </summary>
public sealed class CreateProductCommand
{
    /// <summary>
    /// Command Request - 상품 생성에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 상품 정보
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);

    /// <summary>
    /// Command Handler - 실제 비즈니스 로직 구현
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // 1. 비즈니스 규칙 검증 (중복 검사)
            Fin<bool> existsResult = await _productRepository
                .ExistsByNameAsync(request.Name, cancellationToken);

            if (existsResult.IsFail)
            {
                return (Error)existsResult;
            }

            bool exists = (bool)existsResult;
            if (exists)
            {
                return Error.New($"상품명 '{request.Name}'이(가) 이미 존재합니다");
            }

            // 2. 엔티티 생성
            Product newProduct = new(
                Id: Guid.NewGuid(),
                Name: request.Name,
                Description: request.Description,
                Price: request.Price,
                StockQuantity: request.StockQuantity,
                CreatedAt: DateTime.UtcNow);

            // 3. 저장 및 응답 변환
            Fin<Product> createResult = await _productRepository
                .CreateAsync(newProduct, cancellationToken);

            return createResult.ToFinResponse(product => new Response(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CreatedAt));
        }
    }
}
```

### LINQ 기반 Update Command 예제 (권장)

```csharp
using Functorium.Applications.Linq;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace CqrsFunctional.Demo.Usecases;

/// <summary>
/// 상품 업데이트 Command - Exception Pipeline 데모
/// 예외 발생 시 UsecaseExceptionPipeline의 동작 확인
/// </summary>
public sealed class UpdateProductCommand
{
    /// <summary>
    /// Command Request - 업데이트할 상품 정보
    /// </summary>
    public sealed record Request(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        bool SimulateException = false) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 업데이트된 상품 정보
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime UpdatedAt);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("상품 ID는 필수입니다");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(100).WithMessage("상품명은 100자를 초과할 수 없습니다");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    /// <summary>
    /// Command Handler - 상품 업데이트 로직
    /// SimulateException이 true인 경우 의도적으로 예외 발생
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        /// <summary>
        /// LINQ 쿼리 표현식을 사용한 함수형 체이닝
        /// FinTUtilites의 SelectMany 확장 메서드를 통해 FinT 모나드 트랜스포머를 LINQ로 처리
        /// </summary>
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식: Repository의 FinT<IO, Product>를 사용하여 조회 및 업데이트
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            FinT<IO, Response> usecase =
                from existingProduct in _productRepository.GetById(request.ProductId)
                from updatedProduct in _productRepository.Update(existingProduct with
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    StockQuantity = request.StockQuantity,
                    UpdatedAt = DateTime.UtcNow
                })
                select new Response(
                    updatedProduct.Id,
                    updatedProduct.Name,
                    updatedProduct.Description,
                    updatedProduct.Price,
                    updatedProduct.StockQuantity,
                    updatedProduct.UpdatedAt ?? DateTime.UtcNow);

            // FinT<IO, Response>
            //  -Run()→           IO<Fin<Response>>
            //  -RunAsync()→      Fin<Response>
            //  -ToFinResponse()→ FinResponse<Response>
            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

### 기존 명령형 Update Command 예제

```csharp
/// <summary>
/// 상품 업데이트 Command (기존 명령형 구현)
/// </summary>
public sealed class UpdateProductCommand
{
    public sealed record Request(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime UpdatedAt);

    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // 1. 기존 엔티티 조회 (없으면 Fail 반환)
            Fin<Product> getResult = await _productRepository
                .GetByIdAsync(request.ProductId, cancellationToken);

            if (getResult.IsFail)
            {
                return (Error)getResult;
            }

            Product existingProduct = (Product)getResult;

            // 2. 엔티티 수정 (with 표현식 사용)
            Product updatedProduct = existingProduct with
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                UpdatedAt = DateTime.UtcNow
            };

            // 3. 저장 및 응답 변환
            Fin<Product> updateResult = await _productRepository
                .UpdateAsync(updatedProduct, cancellationToken);

            return updateResult.ToFinResponse(product => new Response(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.UpdatedAt ?? DateTime.UtcNow));
        }
    }
}
```

---

## Query 구현

### LINQ 기반 단일 조회 Query 예제 (권장)

```csharp
using Functorium.Applications.Linq;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace CqrsFunctional.Demo.Usecases;

/// <summary>
/// ID로 상품 조회 Query
/// </summary>
public sealed class GetProductByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 상품 ID
    /// </summary>
    public sealed record Request(Guid ProductId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 상품 정보
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    /// <summary>
    /// Query Handler - 상품 조회 로직
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식: Repository의 FinT<IO, Product>를 직접 사용하여 Response로 변환
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            FinT<IO, Response> usecase =
                from product in _productRepository.GetById(request.ProductId)
                select new Response(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt,
                    product.UpdatedAt);

            // FinT<IO, Response>
            //  -Run()→           IO<Fin<Response>>
            //  -RunAsync()→      Fin<Response>
            //  -ToFinResponse()→ FinResponse<Response>
            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

### 기존 명령형 단일 조회 Query 예제

```csharp
/// <summary>
/// ID로 상품 조회 Query (기존 명령형 구현)
/// </summary>
public sealed class GetProductByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 상품 ID
    /// </summary>
    public sealed record Request(Guid ProductId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 상품 정보
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    /// <summary>
    /// Query Handler - 상품 조회 로직
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Repository가 없는 경우 Fail을 반환하므로 간단하게 처리
            Fin<Product> getResult = await _productRepository
                .GetByIdAsync(request.ProductId, cancellationToken);

            return getResult.ToFinResponse(product => new Response(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CreatedAt,
                product.UpdatedAt));
        }
    }
}
```

### LINQ 기반 목록 조회 Query 예제 (권장)

```csharp
using Functorium.Applications.Linq;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace CqrsFunctional.Demo.Usecases;

/// <summary>
/// 모든 상품 조회 Query
/// </summary>
public sealed class GetAllProductsQuery
{
    /// <summary>
    /// Query Request - 파라미터 없음
    /// </summary>
    public sealed record Request() : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 상품 목록
    /// </summary>
    public sealed record Response(Seq<ProductDto> Products);

    /// <summary>
    /// 상품 DTO
    /// </summary>
    public sealed record ProductDto(
        Guid ProductId,
        string Name,
        decimal Price,
        int StockQuantity);

    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식: Repository의 FinT<IO, Seq<Product>>를 직접 사용하여 Response로 변환
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            FinT<IO, Response> usecase =
                from products in _productRepository.GetAll()
                select new Response(
                    products
                        .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.StockQuantity))
                        .ToSeq());

            // FinT<IO, Response>
            //  -Run()→           IO<Fin<Response>>
            //  -RunAsync()→      Fin<Response>
            //  -ToFinResponse()→ FinResponse<Response>
            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

### 기존 명령형 목록 조회 Query 예제

```csharp
/// <summary>
/// 모든 상품 조회 Query (기존 명령형 구현)
/// </summary>
public sealed class GetAllProductsQuery
{
    /// <summary>
    /// Query Request - 파라미터 없음
    /// </summary>
    public sealed record Request() : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 상품 목록
    /// </summary>
    public sealed record Response(Seq<ProductDto> Products);

    /// <summary>
    /// 상품 DTO
    /// </summary>
    public sealed record ProductDto(
        Guid ProductId,
        string Name,
        decimal Price);

    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            Fin<Seq<Product>> result = await _productRepository
                .GetAllAsync(cancellationToken);

            return result.ToFinResponse(products =>
            {
                Seq<ProductDto> dtos = products.Map(p =>
                    new ProductDto(p.Id, p.Name, p.Price));
                return new Response(dtos);
            });
        }
    }
}
```

---

## FinResponse와 오류 처리

### FinResponse 타입

`FinResponse<A>`는 성공(`Succ`) 또는 실패(`Fail`) 상태를 표현하는 타입입니다:

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

`FinResponse<A>`는 값과 Error로부터 암시적 변환을 지원합니다:

```csharp
// 성공 반환 - 값을 직접 반환
return new Response(productId, name);

// 실패 반환 - Error를 직접 반환
return Error.New("상품을 찾을 수 없습니다");
```

### Match 패턴

```csharp
FinResponse<Response> result = await usecase.Handle(request, ct);

// 패턴 매칭으로 처리
result.Match(
    Succ: response => Console.WriteLine($"성공: {response.ProductId}"),
    Fail: error => Console.WriteLine($"실패: {error.Message}"));

// 값 추출
Response value = result.Match(
    Succ: response => response,
    Fail: error => throw new Exception(error.Message));
```

### Map과 Bind

```csharp
// Map - 성공 값 변환
FinResponse<string> name = result.Map(r => r.Name);

// Bind - 다른 FinResponse로 체이닝
FinResponse<Details> details = result.Bind(r => GetDetails(r.ProductId));
```

---

## Repository 계층과 Fin 타입

### Repository 인터페이스 설계

Repository는 `Fin<A>` 타입을 반환합니다:

```csharp
public interface IProductRepository
{
    /// <summary>
    /// ID로 상품 조회.
    /// 상품이 없으면 실패(Error)를 반환합니다.
    /// </summary>
    Task<Fin<Product>> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 모든 상품 조회
    /// </summary>
    Task<Fin<Seq<Product>>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 상품 생성
    /// </summary>
    Task<Fin<Product>> CreateAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// 상품 업데이트
    /// </summary>
    Task<Fin<Product>> UpdateAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// 상품명 중복 확인
    /// </summary>
    Task<Fin<bool>> ExistsByNameAsync(string name, CancellationToken cancellationToken);
}
```

### "데이터 없음"을 Fail로 처리

**권장 패턴**: `GetByIdAsync`에서 데이터가 없으면 `Fin.Fail`을 반환합니다.

```csharp
// ✓ 권장: Fin<Product> - 없으면 Fail
public Task<Fin<Product>> GetByIdAsync(Guid id, CancellationToken ct)
{
    if (_products.TryGetValue(id, out Product? product))
    {
        return Task.FromResult(Fin.Succ(product));
    }

    return Task.FromResult(
        Fin.Fail<Product>(Error.New($"상품 ID '{id}'을(를) 찾을 수 없습니다")));
}

// ✗ 비권장: Fin<Product?> - null 체크 필요
public Task<Fin<Product?>> GetByIdAsync(Guid id, CancellationToken ct)
{
    _products.TryGetValue(id, out Product? product);
    return Task.FromResult(Fin.Succ(product));  // Handler에서 null 체크 필요
}
```

### Fin → FinResponse 변환

`ToFinResponse` 확장 메서드를 사용하여 변환합니다:

```csharp
// 1. 타입만 변환
Fin<Product> fin = await repository.GetByIdAsync(id, ct);
FinResponse<Product> response = fin.ToFinResponse();

// 2. 값을 매핑하면서 변환 (가장 일반적)
return fin.ToFinResponse(product => new Response(
    product.Id,
    product.Name,
    product.Price));

// 3. 성공 값이 필요 없을 때 (Delete 등)
Fin<Unit> deleteResult = await repository.DeleteAsync(id, ct);
return deleteResult.ToFinResponse(() => new DeleteResponse(id));
```

---

## Validation 구현

### FluentValidation 통합

```csharp
using FluentValidation;

public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(100).WithMessage("상품명은 100자를 초과할 수 없습니다");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("설명은 500자를 초과할 수 없습니다");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    internal sealed class Usecase(...) : ICommandUsecase<Request, Response>
    {
        // Handler는 이미 유효성 검사를 통과한 Request를 받음
    }
}
```

### Pipeline을 통한 자동 검증

`UsecaseValidationPipeline`을 등록하면 Handler 실행 전에 자동으로 Validator가 실행됩니다:

```csharp
// DI 등록
services.AddValidatorsFromAssembly(typeof(Program).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));
```

---

## 트러블슈팅

### Handler가 호출되지 않을 때

**원인**: Mediator에 Handler가 등록되지 않음

**해결:**
```csharp
// Program.cs에서 Mediator 등록
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
```

### "Cannot convert Fin<A> to FinResponse<A>" 오류

**원인**: 암시적 변환이 없음

**해결:** `ToFinResponse()` 확장 메서드 사용
```csharp
Fin<Product> fin = await repository.GetByIdAsync(id, ct);

// ✗ 잘못됨
return fin;

// ✓ 올바름
return fin.ToFinResponse();
```

### null 체크 코드가 중복될 때

**원인**: Repository가 `Fin<T?>`를 반환

**해결:** Repository가 없는 경우 `Fin.Fail`을 반환하도록 변경
```csharp
// Repository 변경 전
Task<Fin<Product?>> GetByIdAsync(Guid id, CancellationToken ct);

// Repository 변경 후
Task<Fin<Product>> GetByIdAsync(Guid id, CancellationToken ct);
// 데이터 없으면 Fin.Fail 반환
```

### Error 타입 캐스팅 오류

**원인**: `Fin<A>`에서 Error 추출 시 잘못된 캐스팅

**해결:**
```csharp
Fin<Product> result = await repository.GetByIdAsync(id, ct);

if (result.IsFail)
{
    // ✓ 명시적 캐스팅
    Error error = (Error)result;
    return error;
}
```

---

## 기존 vs LINQ 기반 구현 비교

### 코드 비교

| 측면 | 기존 명령형 구현 | LINQ 기반 구현 |
|------|------------------|----------------|
| **스타일** | 명령형 (if문, 변수 할당) | 선언적 (LINQ 쿼리) |
| **에러 처리** | 명시적 if문 + 캐스팅 | 자동 체이닝 + 암시적 변환 |
| **가독성** | 단계별 절차적 흐름 | 비즈니스 로직 명확화 |
| **중간 변수** | 다수 변수 필요 | 최소화 (컴파일러 최적화) |
| **테스트성** | 각 단계별 단위 테스트 | 전체 플로우 통합 테스트 |
| **유지보수성** | 변경 영향 범위 큼 | 함수형 체이닝으로 영향 최소화 |

### 구체적인 예제 비교

#### CreateProductCommand 비교

```csharp
// 기존 명령형 (약 35줄)
public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
{
    // 1. 중복 검사
    Fin<bool> existsResult = await _productRepository.ExistsByNameAsync(request.Name, cancellationToken);
    if (existsResult.IsFail) return (Error)existsResult;

    bool exists = (bool)existsResult;
    if (exists) return Error.New($"상품명 '{request.Name}'이(가) 이미 존재합니다");

    // 2. 엔티티 생성
    Product newProduct = new(...);

    // 3. 저장
    Fin<Product> createResult = await _productRepository.CreateAsync(newProduct, cancellationToken);

    return createResult.ToFinResponse(product => new Response(...));
}
```

```csharp
// LINQ 기반 (약 15줄)
public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
{
    FinT<IO, Response> usecase =
        from exists in _productRepository.ExistsByName(request.Name)
        from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
        from product in _productRepository.Create(new Product(...))
        select new Response(...);

    Fin<Response> response = await usecase.Run().RunAsync();
    return response.ToFinResponse();
}
```

### 장점 상세 설명

#### 1. 코드 간결성
- **기존**: 35줄의 명령형 코드
- **LINQ**: 15줄의 선언적 코드 (57% 감소)

#### 2. 에러 처리 자동화
- **기존**: 수동 if문 + 캐스팅 필요
- **LINQ**: `FinTGuardExtensions.SelectMany`가 자동 처리

#### 3. 가독성 향상
- **기존**: 기술적 세부사항 (if, 변수)에 집중
- **LINQ**: 비즈니스 로직 (중복 검사 → 생성 → 응답)에 집중

#### 4. 유지보수성
- **기존**: 한 단계 변경 시 다른 단계 영향 가능성
- **LINQ**: 독립적 체이닝으로 변경 영향 최소화

### 마이그레이션 권장사항

1. **새로운 Usecase는 LINQ 기반으로 구현**
2. **기존 Usecase는 점진적으로 LINQ로 마이그레이션**
3. **팀 컨벤션으로 LINQ 기반을 표준으로 채택**

### 주의사항

- **Repository 인터페이스**: `FinT<IO, T>` 반환하도록 변경 필요
- **ApplicationErrors**: 표준화된 오류 클래스 추가
- **FinTUtilites**: LINQ 확장 메서드 import 필요
- **LanguageExt**: `using LanguageExt;` 추가

---

## FAQ

### Q1. Command와 Query는 어떻게 구분하나요?

**A:** 상태 변경 여부로 구분합니다:

| 유형 | 상태 변경 | 예시 |
|------|----------|------|
| Command | O | Create, Update, Delete |
| Query | X | GetById, GetAll, Search |

### Q2. Handler 클래스 이름은 Usecase와 Handler 중 어느 것을 사용하나요?

**A:** 프로젝트 컨벤션에 따릅니다. 이 가이드에서는 `Usecase`를 사용합니다:

```csharp
// 프로젝트 컨벤션
internal sealed class Usecase : ICommandUsecase<Request, Response> { }
```

### Q3. Response에 도메인 엔티티를 직접 반환해도 되나요?

**A:** 권장하지 않습니다. DTO를 사용하세요:

```csharp
// ✗ 비권장 - 도메인 엔티티 노출
public sealed record Response(Product Product);

// ✓ 권장 - DTO 사용
public sealed record Response(
    Guid ProductId,
    string Name,
    decimal Price);
```

### Q4. 여러 Repository를 사용해야 할 때는 어떻게 하나요?

**A:** 생성자 주입으로 여러 Repository를 받습니다:

```csharp
internal sealed class Usecase(
    ILogger<Usecase> logger,
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IInventoryRepository inventoryRepository)
    : ICommandUsecase<Request, Response>
{
    // ...
}
```

### Q5. 트랜잭션은 어떻게 처리하나요?

**A:** UnitOfWork 패턴이나 Pipeline에서 처리합니다:

```csharp
// Option 1: UnitOfWork 주입
internal sealed class Usecase(
    IUnitOfWork unitOfWork,
    IProductRepository productRepository)
{
    public async ValueTask<FinResponse<Response>> Handle(...)
    {
        // ... 비즈니스 로직
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}

// Option 2: Transaction Pipeline
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionPipeline<,>));
```

### Q6. 비동기 작업에서 `ValueTask`와 `Task` 중 어느 것을 사용하나요?

**A:** Mediator 인터페이스가 `ValueTask`를 사용하므로 Handler도 `ValueTask`를 반환합니다:

```csharp
public async ValueTask<FinResponse<Response>> Handle(
    Request request,
    CancellationToken cancellationToken)
```

### Q7. CancellationToken은 항상 전달해야 하나요?

**A:** 네, 비동기 메서드에는 항상 CancellationToken을 전달하세요:

```csharp
public async ValueTask<FinResponse<Response>> Handle(
    Request request,
    CancellationToken cancellationToken)
{
    // Repository 호출 시 전달
    var result = await _repository.GetByIdAsync(id, cancellationToken);
}
```

### Q8. LINQ 기반 구현을 권장하는 이유는 무엇인가요?

**A:** LINQ 기반 구현은 다음과 같은 장점을 제공합니다:

- **코드 간결성**: 명령형 코드 대비 50-60% 줄 수 감소
- **에러 처리 자동화**: `FinTGuardExtensions`가 자동으로 에러 체이닝 처리
- **가독성 향상**: 선언적 LINQ 쿼리로 비즈니스 로직 명확화
- **유지보수성**: 함수형 체이닝으로 변경 영향 최소화
- **표준화**: 모든 Usecase가 동일한 패턴 사용

### Q9. ApplicationErrors는 어떻게 구현하나요?

**A:** DomainErrors 패턴과 동일하게 중첩 정적 클래스로 구현합니다:

```csharp
internal static class ApplicationErrors
{
    public static Error ProductNameAlreadyExists(string productName) =>
        ErrorCodeFactory.Create(
            errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
            errorCurrentValue: productName,
            errorMessage: $"Product name already exists. Current value: '{productName}'");
}
```

### Q10. guard는 언제 사용하나요?

**A:** `guard`는 LINQ 쿼리 내에서 조건 검사를 수행할 때 사용합니다:

```csharp
from exists in _repository.ExistsByName(request.Name)
from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
```

`guard(condition, error)`는 조건이 `false`일 때 `FinT.Fail`을 반환합니다.

## 참고 문서

- [단위 테스트 가이드](./unit-testing-guide.md) - 유스케이스 테스트 작성 방법
- [Mediator 라이브러리](https://github.com/martinothamar/Mediator) - 기반 라이브러리
- [LanguageExt](https://github.com/louthy/language-ext) - Fin 타입 제공 라이브러리

