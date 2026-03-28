using Cqrs05EndpointLayered.Domains.Entities;
using Cqrs05EndpointLayered.Domains.Repositories;
using FluentValidation;
using Functorium.Abstractions.Errors;
using Functorium.Applications.Linq;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Cqrs05EndpointLayered.Applications.Commands;

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
    /// Command Handler - 실제 비즈니스 로직 구현
    /// </summary>
    public sealed class Usecase(
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
}
