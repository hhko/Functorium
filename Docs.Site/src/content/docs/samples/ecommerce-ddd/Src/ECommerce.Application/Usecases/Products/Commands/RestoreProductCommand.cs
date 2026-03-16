using ECommerce.Domain.AggregateRoots.Products;
using Functorium.Applications.Linq;

namespace ECommerce.Application.Usecases.Products.Commands;

/// <summary>
/// 삭제된 상품 복원 Command - Soft Delete 복원
/// GetByIdIncludingDeleted → product.Restore() → repository.Update(product) 흐름으로
/// 도메인 이벤트(RestoredEvent)가 자동 발행됩니다.
/// </summary>
public sealed class RestoreProductCommand
{
    /// <summary>
    /// Command Request - 복원할 상품 ID
    /// </summary>
    public sealed record Request(
        string ProductId) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 복원된 상품 정보
    /// </summary>
    public sealed record Response(
        string ProductId,
        string Name,
        decimal Price);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).MustBeEntityId<Request, ProductId>();
        }
    }

    /// <summary>
    /// Command Handler - GetByIdIncludingDeleted → Restore → Update 패턴
    /// </summary>
    public sealed class Usecase(
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);

            FinT<IO, Response> usecase =
                from product in _productRepository.GetByIdIncludingDeleted(productId)
                let restored = product.Restore()
                from updated in _productRepository.Update(restored)
                select new Response(
                    updated.Id.ToString(),
                    updated.Name,
                    updated.Price);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
