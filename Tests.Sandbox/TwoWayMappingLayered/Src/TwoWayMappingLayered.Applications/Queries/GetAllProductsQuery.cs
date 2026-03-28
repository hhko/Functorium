using Functorium.Applications.Linq;
using Microsoft.Extensions.Logging;
using TwoWayMappingLayered.Domains.Repositories;

namespace TwoWayMappingLayered.Applications.Queries;

/// <summary>
/// 상품 전체 조회 Query
///
/// Two-Way Mapping 특징:
/// - Repository가 Product(Domain) 컬렉션 반환
/// - 각 상품의 비즈니스 메서드 즉시 사용 가능
/// </summary>
public sealed class GetAllProductsQuery
{
    public sealed record Request() : IQueryRequest<Response>;

    public sealed record Response(IReadOnlyList<ProductItem> Products);

    public sealed record ProductItem(
        Guid ProductId,
        string Name,
        string Description,
        string FormattedPrice,
        int StockQuantity,
        DateTime CreatedAt);

    public sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // Two-Way Mapping: Repository가 Product(Domain) 컬렉션 반환
            FinT<IO, Response> usecase =
                from products in _productRepository.GetAll()
                select new Response(
                    products.Select(p => new ProductItem(
                        (Guid)p.Id,  // implicit operator를 통한 변환
                        p.Name,
                        p.Description,
                        p.FormattedPrice,  // Two-Way: 비즈니스 메서드 직접 사용
                        p.StockQuantity,
                        p.CreatedAt)).ToList());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
