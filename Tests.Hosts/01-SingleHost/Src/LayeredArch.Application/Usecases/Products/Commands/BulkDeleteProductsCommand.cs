using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 벌크 삭제 Command - N * DeletedEvent + IDomainEventBatchHandler 데모
/// </summary>
public sealed class BulkDeleteProductsCommand
{
    public sealed record Request(List<string> ProductIds) : ICommandRequest<Response>;

    public sealed record Response(int AffectedCount);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductIds).NotEmpty().WithMessage("최소 1개 이상의 상품 ID가 필요합니다");
        }
    }

    public sealed class Usecase(IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var ids = request.ProductIds
                .Select(id => ProductId.Parse(id, null))
                .ToList();

            FinT<IO, Response> usecase =
                from affected in productRepository.DeleteRange(ids)
                select new Response(affected);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
