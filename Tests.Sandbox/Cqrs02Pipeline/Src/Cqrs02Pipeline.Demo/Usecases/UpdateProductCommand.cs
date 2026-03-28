using Cqrs02Pipeline.Demo.Domain;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Cqrs02Pipeline.Demo.Usecases;

/// <summary>
/// 상품 업데이트 Command - Exception Pipeline 데모
/// 예외 발생 시 UsecaseExceptionPipeline의 동작 확인
/// </summary>
public sealed class UpdateProductCommand
{
    /// <summary>
    /// Command Request - 업데이트할 상품 정보
    /// </summary>
    public sealed record Request(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        bool SimulateException = false) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 업데이트된 상품 정보
    /// </summary>
    public sealed record Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime UpdatedAt);

    /// <summary>
    /// Request Validator
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("상품 ID는 필수입니다");

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
    /// Command Handler - 상품 업데이트 로직
    /// SimulateException이 true인 경우 의도적으로 예외 발생
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 예외 시뮬레이션 - UsecaseExceptionPipeline 데모용
            if (request.SimulateException)
            {
                throw new InvalidOperationException("시뮬레이션된 예외: 데모 목적으로 발생된 예외입니다");
            }

            // 기존 상품 조회 (없으면 Fail 반환)
            Fin<Product> getResult = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

            if (getResult.IsFail)
            {
                return (Error)getResult;
            }

            Product existingProduct = (Product)getResult;

            // 상품 업데이트
            Product updatedProduct = existingProduct with
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                UpdatedAt = DateTime.UtcNow
            };

            Fin<Product> updateResult = await _productRepository.UpdateAsync(updatedProduct, cancellationToken);

            return updateResult.ToFinResponse(product => new Response(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.UpdatedAt ?? DateTime.UtcNow));
        }
    }
}
