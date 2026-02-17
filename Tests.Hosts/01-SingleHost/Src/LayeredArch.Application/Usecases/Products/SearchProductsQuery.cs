using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Products.Dtos;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// 상품 검색 Query - Specification 패턴 조합 데모
/// 가격 범위 등 선택적 필터를 Specification으로 조합하여 검색.
/// 재고 관련 검색은 Inventory Aggregate 관할입니다.
/// </summary>
public sealed class SearchProductsQuery
{
    /// <summary>
    /// Query Request - 선택적 검색 필터
    /// </summary>
    public sealed record Request(
        decimal? MinPrice,
        decimal? MaxPrice) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 검색 결과 상품 목록
    /// </summary>
    public sealed record Response(Seq<ProductSummaryDto> Products);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.MinPrice)
                .GreaterThan(0).When(x => x.MinPrice.HasValue)
                .WithMessage("최소 가격은 0보다 커야 합니다");

            RuleFor(x => x.MinPrice)
                .NotNull().When(x => x.MaxPrice.HasValue)
                .WithMessage("최대 가격을 지정할 때는 최소 가격도 함께 지정해야 합니다");

            RuleFor(x => x.MaxPrice)
                .GreaterThan(0).When(x => x.MaxPrice.HasValue)
                .WithMessage("최대 가격은 0보다 커야 합니다");

            RuleFor(x => x.MaxPrice)
                .NotNull().When(x => x.MinPrice.HasValue)
                .WithMessage("최소 가격을 지정할 때는 최대 가격도 함께 지정해야 합니다");

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
                .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
                .WithMessage("최대 가격은 최소 가격 이상이어야 합니다");
        }
    }

    /// <summary>
    /// Query Handler - Specification 조합으로 상품 검색
    /// </summary>
    public sealed class Usecase(IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var spec = BuildSpecification(request);

            FinT<IO, Response> usecase =
                from products in spec is not null
                    ? _productRepository.FindAll(spec)
                    : _productRepository.GetAll()
                select new Response(
                    products
                        .Select(p => new ProductSummaryDto(p.Id.ToString(), p.Name, p.Price))
                        .ToSeq());

            Fin<Response> response = await usecase.Run().RunAsync();

            return response.ToFinResponse();
        }

        private static Specification<Product>? BuildSpecification(Request request)
        {
            Specification<Product>? spec = null;

            if (request.MinPrice.HasValue && request.MaxPrice.HasValue)
            {
                spec = new ProductPriceRangeSpec(
                    Money.Create(request.MinPrice.Value).ThrowIfFail(),
                    Money.Create(request.MaxPrice.Value).ThrowIfFail());
            }

            return spec;
        }
    }
}
