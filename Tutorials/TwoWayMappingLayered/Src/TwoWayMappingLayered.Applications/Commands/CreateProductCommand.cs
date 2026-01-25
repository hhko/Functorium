using FluentValidation;
using Functorium.Abstractions.Errors;
using Functorium.Applications.Linq;
using Functorium.Applications.Validations;
using Functorium.Domains.ValueObjects;
using Microsoft.Extensions.Logging;
using TwoWayMappingLayered.Domains.Entities;
using TwoWayMappingLayered.Domains.Repositories;
using TwoWayMappingLayered.Domains.ValueObjects;
using DomainValidate = Functorium.Domains.ValueObjects.Validate<TwoWayMappingLayered.Domains.ValueObjects.Money>;

namespace TwoWayMappingLayered.Applications.Commands;

/// <summary>
/// 상품 생성 Command
///
/// Two-Way Mapping 특징:
/// - Domain 엔티티(Product)를 생성하여 Repository에 전달
/// - Repository 내부에서 ProductEntity로 변환하여 저장
/// - 저장 후 다시 Product로 변환하여 반환
///
/// Validation 패턴:
/// - FluentValidation Validator에서 Value Object Validate 메서드 통합
/// - MustSatisfyValueObjectValidation 확장 메서드 사용
/// - Handler는 검증 완료된 데이터만 처리
/// </summary>
public sealed class CreateProductCommand
{
    /// <summary>
    /// Command Request
    /// </summary>
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        string Currency,
        int StockQuantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        string FormattedPrice,
        int StockQuantity,
        DateTime CreatedAt);

    /// <summary>
    /// Request Validator
    /// Value Object의 Validate 메서드를 FluentValidation과 통합
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

            // Money Value Object 검증: Amount
            RuleFor(x => x.Price)
                .MustSatisfyValueObjectValidation<Request, decimal, decimal>(
                    price => DomainValidate.NonNegative(price));

            // Money Value Object 검증: Currency
            RuleFor(x => x.Currency)
                .MustSatisfyValueObjectValidation<Request, string, string>(
                    currency => DomainValidate.NotEmpty(currency ?? "")
                        .ThenExactLength(3));

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    /// <summary>
    /// Command Handler
    /// LINQ 쿼리 표현식으로 함수형 체이닝 구현
    /// 검증은 Validator에서 완료됨 - Handler는 비즈니스 로직에 집중
    /// </summary>
    public sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // Validator에서 검증 완료 - 안전하게 Value Object 생성
            Money price = Money.FromValues(request.Price, request.Currency.ToUpperInvariant());

            // LINQ 쿼리 표현식: Repository의 FinT<IO, T>를 사용
            // Two-Way Mapping: Repository가 Domain 엔티티를 직접 반환
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(request.Name)
                from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
                from product in _productRepository.Create(
                    Product.Create(
                        request.Name,
                        request.Description,
                        price,
                        request.StockQuantity))
                select new Response(
                    (Guid)product.Id,  // implicit operator를 통한 변환
                    product.Name,
                    product.Description,
                    product.FormattedPrice,  // Two-Way: 비즈니스 메서드 사용 가능
                    product.StockQuantity,
                    product.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }

    /// <summary>
    /// Application 오류 정의
    /// </summary>
    internal static class ApplicationErrors
    {
        public static Error ProductNameAlreadyExists(string productName) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
                errorCurrentValue: productName,
                errorMessage: $"Product name already exists. Current value: '{productName}'");
    }
}
