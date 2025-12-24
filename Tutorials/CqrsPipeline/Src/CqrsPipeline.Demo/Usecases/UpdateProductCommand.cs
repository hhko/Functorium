using CqrsPipeline.Demo.Domain;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Usecases;

/// <summary>
/// 상품 업데이트 Command - Exception Pipeline 데모
/// 예외 발생 시 UsecaseExceptionPipeline의 동작 확인
/// </summary>
public sealed class UpdateProductCommand
{
    /// <summary>
    /// Command Request - 업데이트할 상품 정보
    /// </summary>
    public sealed record class Request(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        bool SimulateException = false) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 업데이트된 상품 정보
    /// </summary>
    public sealed record class Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime UpdatedAt) : IResponse;

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
    /// Command Usecase - 상품 업데이트 로직
    /// SimulateException이 true인 경우 의도적으로 예외 발생
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<IFinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Updating product: {ProductId}", request.ProductId);

            // 예외 시뮬레이션 - UsecaseExceptionPipeline 데모용
            if (request.SimulateException)
            {
                //_logger.LogWarning("Simulating exception for demo purposes");
                throw new InvalidOperationException("시뮬레이션된 예외: 데모 목적으로 발생된 예외입니다");
            }

            // 기존 상품 조회
            Fin<Product?> getResult = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

            if (getResult.IsFail)
            {
                Error error = (Error)getResult;
                //_logger.LogError("Failed to get product: {Error}", error.Message);
                return FinResponseUtilites.ToResponseFail<Response>(error);
            }

            Product? existingProduct = (Product?)getResult;
            if (existingProduct is null)
            {
                //_logger.LogWarning("Product not found: {ProductId}", request.ProductId);
                return FinResponseUtilites.ToResponseFail<Response>(
                    Error.New($"상품 ID '{request.ProductId}'을(를) 찾을 수 없습니다"));
            }

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

            return updateResult.Match<IFinResponse<Response>>(
                Succ: product =>
                {
                    //_logger.LogInformation("Product updated successfully: {ProductId}", product.Id);
                    return FinResponseUtilites.ToResponse(
                        new Response(
                            product.Id,
                            product.Name,
                            product.Description,
                            product.Price,
                            product.StockQuantity,
                            product.UpdatedAt ?? DateTime.UtcNow));
                },
                Fail: error =>
                {
                    //_logger.LogError("Failed to update product: {Error}", error.Message);
                    return FinResponseUtilites.ToResponseFail<Response>(error);
                });
        }
    }
}
