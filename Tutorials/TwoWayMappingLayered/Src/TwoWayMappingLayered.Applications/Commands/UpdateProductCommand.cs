using FluentValidation;
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
/// 상품 업데이트 Command
///
/// Two-Way Mapping 특징:
/// - Repository에서 Product(Domain) 조회
/// - Domain 비즈니스 메서드(Update)로 상태 변경
/// - Repository가 다시 ProductEntity로 변환하여 저장
///
/// Validation 패턴:
/// - FluentValidation Validator에서 Value Object Validate 메서드 통합
/// - MustSatisfyValueObjectValidation 확장 메서드 사용
/// </summary>
public sealed class UpdateProductCommand
{
    public sealed record Request(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        string Currency,
        int StockQuantity) : ICommandRequest<Response>;

    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        string FormattedPrice,
        int StockQuantity,
        DateTime? UpdatedAt);

    /// <summary>
    /// Request Validator
    /// Value Object의 Validate 메서드를 FluentValidation과 통합
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            // ProductId Value Object 검증
            RuleFor(x => x.ProductId)
                .MustSatisfyValueObjectValidation<Request, Guid, Guid>(ProductId.Validate);

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
            ProductId productId = ProductId.FromValue(request.ProductId);
            Money price = Money.FromValues(request.Price, request.Currency.ToUpperInvariant());

            // Two-Way Mapping: Repository가 Product(Domain) 반환
            // let 표현식으로 비즈니스 메서드 호출 후 업데이트
            FinT<IO, Response> usecase =
                from product in _productRepository.GetById(productId)
                let updatedProduct = product.Update(request.Name, request.Description, price, request.StockQuantity)
                from saved in _productRepository.Update(updatedProduct)
                select new Response(
                    (Guid)saved.Id,  // implicit operator를 통한 변환
                    saved.Name,
                    saved.Description,
                    saved.FormattedPrice,  // Two-Way: 비즈니스 메서드 사용 가능
                    saved.StockQuantity,
                    saved.UpdatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
