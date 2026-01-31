# 유스케이스 구현 가이드

이 문서는 Functorium CQRS 패턴을 사용하여 유스케이스(Command/Query Handler)를 구현하는 방법을 설명합니다.

## 목차

- [요약](#요약)
- [CQRS 패턴 개요](#cqrs-패턴-개요)
- [프로젝트 구조](#프로젝트-구조)
- [중첩 클래스 패턴](#중첩-클래스-패턴)
- [Value Object 검증과 Apply 병합 패턴](#value-object-검증과-apply-병합-패턴)
- [LINQ 기반 함수형 구현](#linq-기반-함수형-구현)
- [Application 에러 사용 패턴](#application-에러-사용-패턴)
- [Command 구현](#command-구현)
- [Query 구현](#query-구현)
- [FinResponse와 오류 처리](#finresponse와-오류-처리)
- [FluentValidation 통합](#fluentvalidation-통합)
- [FAQ](#faq)
- [참고 문서](#참고-문서)

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
| `FinT<IO, A>` | IO 효과를 포함한 Fin 타입 | Repository/Adapter |
| `FinResponse<A>` | Functorium Response 성공/실패 타입 | Usecase |
| `Error` | 오류 정보 | 공통 |

### 권장 구현 패턴

```csharp
using Functorium.Applications.Errors;
using static Functorium.Applications.Errors.ApplicationErrorType;

public sealed class CreateProductCommand
{
    public sealed record Request(...) : ICommandRequest<Response>;
    public sealed record Response(...);
    public sealed class Validator : AbstractValidator<Request> { ... }

    internal sealed class Usecase(IProductRepository productRepository)
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
            var productName = ProductName.Create(request.Name).ThrowIfFail();

            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from product in _productRepository.Create((Product)productResult)
                select new Response(...);

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
                        ProductName.Create(n).ThrowIfFail(),
                        ProductDescription.Create(d).ThrowIfFail(),
                        Money.Create(p).ThrowIfFail(),
                        Quantity.Create(s).ThrowIfFail()))
                .As()
                .ToFin();
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
│   └── I{인터페이스}.cs                # 기술 관심사 인터페이스
└── Usecases/
    ├── {엔티티}/
    │   ├── Create{엔티티}Command.cs
    │   ├── Update{엔티티}Command.cs
    │   ├── Get{엔티티}ByIdQuery.cs
    │   └── GetAll{엔티티}sQuery.cs
    └── ...
```

---

## 중첩 클래스 패턴

### 패턴 설명

하나의 유스케이스를 구성하는 Request, Response, Validator, Usecase를 하나의 파일에 중첩 클래스로 정의합니다.

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
            // Application 에러: ApplicationError.For<{UsecaseName}>(new {ErrorType}(), value, message) 사용
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
| `Usecase` | `internal` | 필수 | 비즈니스 로직 구현 |

> **참고**: `Validator`가 정의되면 Pipeline을 통해 Handler 실행 전에 자동으로 검증됩니다.

---

## Value Object 검증과 Apply 병합 패턴

### 이중 검증 전략

Usecase에서는 두 가지 검증 레이어가 있습니다:

| 검증 레이어 | 담당 | 목적 |
|------------|------|------|
| **FluentValidation** | Presentation Layer | 빠른 입력 형식 검증 |
| **Value Object Validate()** | Domain Layer | 도메인 불변식 검증 |

### Apply 병합 패턴

여러 Value Object를 동시에 검증하고 Entity를 생성할 때 Apply 패턴을 사용합니다:

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
                ProductName.Create(n).ThrowIfFail(),
                ProductDescription.Create(d).ThrowIfFail(),
                Money.Create(p).ThrowIfFail(),
                Quantity.Create(s).ThrowIfFail()))
        .As()
        .ToFin();
}
```

### 패턴 설명

| 단계 | 메서드 | 설명 |
|------|--------|------|
| 1 | `Validate()` | 모든 필드의 검증을 `Validation<Error, T>`로 수집 |
| 2 | `Apply()` | 모든 검증이 성공해야 Entity 생성 진행 (병렬 검증) |
| 3 | `ThrowIfFail()` | 이미 검증된 값이므로 안전하게 VO 변환 |
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
                ProductName.Create(n).ThrowIfFail(),
                noteValue,
                Money.Create(p).ThrowIfFail()))
        .As()
        .ToFin();
}
```

> **권장**: 자주 사용되는 필드는 Named Context 대신 별도의 ValueObject로 정의하세요.

---

## LINQ 기반 함수형 구현

### 권장사항

**LINQ 기반 함수형 구현을 우선 권장합니다.** 기존 명령형 구현보다 다음과 같은 장점이 있습니다:

- **코드 간결성**: 명령형 if문과 중간 변수 제거 (50-60% 코드 감소)
- **에러 처리 자동화**: Repository 실패 시 자동으로 `FinT.Fail` 반환
- **가독성 향상**: 선언적 LINQ 쿼리로 비즈니스 로직 명확화
- **유지보수성**: 함수형 체이닝으로 변경 영향 최소화

### guard를 활용한 조건 검사

LanguageExt의 `guard`를 사용하여 함수형 조건 검사를 구현합니다:

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

## Application 에러 사용 패턴

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

### 주요 ApplicationErrorType

| 에러 타입 | 설명 | 사용 예시 |
|-----------|------|----------|
| `AlreadyExists` | 이미 존재함 | `new AlreadyExists()` |
| `NotFound` | 찾을 수 없음 | `new NotFound()` |
| `Duplicate` | 중복됨 | `new Duplicate()` |
| `ValidationFailed` | 검증 실패 | `new ValidationFailed(PropertyName: "Email")` |
| `BusinessRuleViolated` | 비즈니스 규칙 위반 | `new BusinessRuleViolated(RuleName: "MaxOrderLimit")` |
| `Unauthorized` | 인증되지 않음 | `new Unauthorized()` |
| `Forbidden` | 접근 금지 | `new Forbidden()` |
| `Custom` | 커스텀 에러 | `new Custom("PaymentDeclined")` |

### 에러 코드 형식

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

## Command 구현

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
/// 상품 생성 Command - Apply 패턴 + LINQ 구현
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

    internal sealed class Usecase(IProductRepository productRepository)
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
            var productName = ProductName.Create(request.Name).ThrowIfFail();

            // 3. LINQ로 중복 검사 및 저장
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
                        ProductName.Create(n).ThrowIfFail(),
                        ProductDescription.Create(d).ThrowIfFail(),
                        Money.Create(p).ThrowIfFail(),
                        Quantity.Create(s).ThrowIfFail()))
                .As()
                .ToFin();
        }
    }
}
```

---

## Query 구현

### 단일 조회 Query 예제

```csharp
public sealed class GetProductByIdQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>;

    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt,
        DateTime? UpdatedAt);

    internal sealed class Usecase(IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!ProductId.TryParse(request.ProductId, null, out var productId))
            {
                return FinResponse.Fail<Response>(Error.New("Invalid ProductId"));
            }

            FinT<IO, Response> usecase =
                from product in _productRepository.GetById(productId)
                select new Response(
                    product.Id.ToString(),
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt,
                    product.UpdatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

### 목록 조회 Query 예제

```csharp
public sealed class GetAllProductsQuery
{
    public sealed record Request() : IQueryRequest<Response>;

    public sealed record Response(Seq<ProductDto> Products);

    public sealed record ProductDto(
        string ProductId,
        string Name,
        decimal Price,
        int StockQuantity);

    internal sealed class Usecase(IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            FinT<IO, Response> usecase =
                from products in _productRepository.GetAll()
                select new Response(
                    products
                        .Select(p => new ProductDto(
                            p.Id.ToString(),
                            p.Name,
                            p.Price,
                            p.StockQuantity))
                        .ToSeq());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

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

### 검증 규칙 정의

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

`UsecaseValidationPipeline`을 등록하면 Handler 실행 전에 자동으로 Validator가 실행됩니다:

```csharp
services.AddValidatorsFromAssembly(typeof(Program).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));
```

---

## FAQ

### Q1. FluentValidation과 VO Validate() 둘 다 필요한가요?

**A:** 네, 각각 다른 목적을 가집니다:
- **FluentValidation**: Presentation Layer에서 빠른 형식 검증
- **VO Validate()**: Domain Layer에서 도메인 불변식 검증

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

---

## 참고 문서

| 문서 | 설명 |
|------|------|
| [valueobject-guide.md](./valueobject-guide.md) | 값 객체 구현 및 검증 패턴 |
| [entity-guide.md](./entity-guide.md) | Entity 구현 및 Create 패턴 |
| [error-guide.md](./error-guide.md) | 에러 시스템 가이드 |
| [adapter-guide.md](./adapter-guide.md) | Repository 인터페이스 설계 |
| [unit-testing-guide.md](./unit-testing-guide.md) | Usecase 테스트 작성 방법 |

**외부 참고:**
- [Mediator](https://github.com/martinothamar/Mediator) - 기반 라이브러리
- [LanguageExt](https://github.com/louthy/language-ext) - Fin 타입 제공 라이브러리
