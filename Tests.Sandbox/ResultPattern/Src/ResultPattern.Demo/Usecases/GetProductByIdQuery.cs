using FluentValidation;
using ResultPattern.Demo.Cqrs;
using ResultPattern.Demo.Domain;

namespace ResultPattern.Demo.Usecases;

/// <summary>
/// 상품 조회 Query - Result 패턴 데모
/// </summary>
public sealed class GetProductByIdQuery
{
    /// <summary>
    /// Query Request
    /// </summary>
    public sealed record Request(Guid ProductId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 기본 생성자 없이 정의!
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);
    // 기본 생성자 boilerplate 없음!

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("상품 ID는 필수입니다");
        }
    }

    /// <summary>
    /// Query Handler
    /// </summary>
    internal sealed class Usecase(IProductRepository productRepository)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // Fin -> FinResponse 변환 (Repository에서 Not Found 시 이미 Fail 반환)
            return (await productRepository.GetByIdAsync(request.ProductId, cancellationToken))
                .ToFinResponse(p => new Response(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.StockQuantity,
                    p.CreatedAt));
        }
    }
}
