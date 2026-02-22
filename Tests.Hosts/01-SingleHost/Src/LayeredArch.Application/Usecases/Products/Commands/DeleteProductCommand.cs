using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Applications.Linq;

namespace LayeredArch.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 삭제 Command - Soft Delete (도메인 모델 경유)
/// GetById → product.Delete(deletedBy) → repository.Update(product) 흐름으로
/// 도메인 이벤트(DeletedEvent)가 자동 발행됩니다.
/// </summary>
public sealed class DeleteProductCommand
{
    /// <summary>
    /// Command Request - 삭제할 상품 ID와 삭제자 정보
    /// </summary>
    public sealed record Request(
        string ProductId,
        string DeletedBy) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 삭제된 상품 ID
    /// </summary>
    public sealed record Response(
        string ProductId);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .Must(id => ProductId.TryParse(id, null, out _))
                .WithMessage("Invalid product ID format");

            RuleFor(x => x.DeletedBy)
                .NotEmpty()
                .WithMessage("DeletedBy must not be empty");
        }
    }

    /// <summary>
    /// Command Handler - GetById → Delete → Update 패턴
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
                from product in _productRepository.GetById(productId)
                let deleted = product.Delete(request.DeletedBy)
                from updated in _productRepository.Update(deleted)
                select new Response(updated.Id.ToString());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
