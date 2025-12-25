using CqrsPipeline.Demo.Domain;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace CqrsPipeline.Demo.Usecases;

/// <summary>
/// 상품 생성 Command - Validation Pipeline 데모
/// FluentValidation을 사용한 입력 검증 예제
/// </summary>
public sealed class CreateProductCommand
{
    /// <summary>
    /// Command Request - 상품 생성에 필요한 데이터
    /// </summary>
    public sealed record class Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 상품 정보
    /// </summary>
    public sealed record class Response(
        Guid ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt) : ResponseBase<Response>
    {
        public Response() : this(Guid.Empty, string.Empty, string.Empty, 0m, 0, default) { }
    }

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(100).WithMessage("상품명은 100자를 초과할 수 없습니다");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("설명은 500자를 초과할 수 없습니다");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    /// <summary>
    /// Command Usecase - 실제 비즈니스 로직 구현
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Creating product: {Name}, Price: {Price}", request.Name, request.Price);

            // 상품명 중복 검사
            Fin<bool> existsResult = await _productRepository.ExistsByNameAsync(request.Name, cancellationToken);

            if (existsResult.IsFail)
            {
                Error error = (Error)existsResult;
                //_logger.LogError("Failed to check product name existence: {Error}", error.Message);
                return Response.CreateFail(error);
            }

            bool exists = (bool)existsResult;
            if (exists)
            {
                //_logger.LogWarning("Product name already exists: {Name}", request.Name);
                return Response.CreateFail(
                    Error.New($"상품명 '{request.Name}'이(가) 이미 존재합니다"));
            }

            // 상품 생성
            Product newProduct = new(
                Id: Guid.NewGuid(),
                Name: request.Name,
                Description: request.Description,
                Price: request.Price,
                StockQuantity: request.StockQuantity,
                CreatedAt: DateTime.UtcNow);

            Fin<Product> createResult = await _productRepository.CreateAsync(newProduct, cancellationToken);

            return createResult.ToResponse(product => new Response(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.CreatedAt));
        }
    }
}
