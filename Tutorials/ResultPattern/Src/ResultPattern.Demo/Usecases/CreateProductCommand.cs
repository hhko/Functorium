using FluentValidation;
using ResultPattern.Demo.Cqrs;
using ResultPattern.Demo.Domain;

namespace ResultPattern.Demo.Usecases;

/// <summary>
/// 상품 생성 Command - Result 패턴 데모
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
        int StockQuantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 기본 생성자 없이 정의!
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
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(100).WithMessage("상품명은 100자를 초과할 수 없습니다");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    /// <summary>
    /// Command Handler
    /// </summary>
    internal sealed class Usecase(IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var product = new Product(
                Id: Guid.NewGuid(),
                Name: request.Name,
                Description: request.Description,
                Price: request.Price,
                StockQuantity: request.StockQuantity,
                CreatedAt: DateTime.UtcNow);

            // Fin -> FinResponse 변환
            return (await productRepository.CreateAsync(product, cancellationToken))
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
