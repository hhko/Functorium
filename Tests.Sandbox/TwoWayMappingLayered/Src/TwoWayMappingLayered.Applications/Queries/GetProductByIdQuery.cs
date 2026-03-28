using FluentValidation;
using Functorium.Applications.Linq;
using Functorium.Applications.Validations;
using Microsoft.Extensions.Logging;
using TwoWayMappingLayered.Domains.Repositories;
using TwoWayMappingLayered.Domains.ValueObjects;

namespace TwoWayMappingLayered.Applications.Queries;

/// <summary>
/// 상품 단건 조회 Query
///
/// Two-Way Mapping 특징:
/// - Repository가 Product(Domain) 직접 반환
/// - 비즈니스 메서드(FormattedPrice) 즉시 사용 가능
/// - 변환 오버헤드 없이 바로 비즈니스 로직 활용
///
/// Validation 패턴:
/// - FluentValidation Validator에서 Value Object Validate 메서드 통합
/// </summary>
public sealed class GetProductByIdQuery
{
    public sealed record Request(Guid ProductId) : IQueryRequest<Response>;

    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        string FormattedPrice,
        decimal Price,
        string Currency,
        int StockQuantity,
        DateTime CreatedAt,
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
                .MustSatisfyValidation(ProductId.Validate);
        }
    }

    /// <summary>
    /// Query Handler
    /// 검증은 Validator에서 완료됨 - Handler는 조회 로직에 집중
    /// </summary>
    public sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // Validator에서 검증 완료 - 안전하게 Value Object 생성
            ProductId productId = ProductId.FromValue(request.ProductId);

            // Two-Way Mapping: Repository가 Product(Domain) 반환
            // One-Way와 달리 비즈니스 메서드 즉시 사용 가능
            FinT<IO, Response> usecase =
                from product in _productRepository.GetById(productId)
                select new Response(
                    (Guid)product.Id,  // implicit operator를 통한 변환
                    product.Name,
                    product.Description,
                    product.FormattedPrice,  // Two-Way: 비즈니스 메서드 직접 사용
                    product.Price.Amount,
                    product.Price.Currency,
                    product.StockQuantity,
                    product.CreatedAt,
                    product.UpdatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
