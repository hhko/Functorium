using Cqrs03Functional.Demo.Domain;
using Cqrs03Functional.Demo.Domain.ValueObjects;
using FluentValidation;
using Functorium.Applications.Usecases;
using Functorium.Applications.Linq;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

namespace Cqrs03Functional.Demo.Usecases;

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
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        bool SimulateException = false) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 업데이트된 상품 정보
    /// </summary>
    public sealed record Response(
        string ProductId,
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

        /// <summary>
        /// LINQ 쿼리 표현식을 사용한 함수형 체이닝
        /// FinTUtilites의 SelectMany 확장 메서드를 통해 FinT 모나드 트랜스포머를 LINQ로 처리
        /// </summary>
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 예외 시뮬레이션 - UsecaseExceptionPipeline 데모용
            if (request.SimulateException)
            {
                throw new InvalidOperationException("시뮬레이션된 예외: 데모 목적으로 발생된 예외입니다");
            }

            // ProductId 파싱
            if (!Domain.ProductId.TryParse(request.ProductId, null, out var productId))
            {
                return FinResponse.Fail<Response>(Error.New($"Invalid ProductId format: {request.ProductId}"));
            }

            // Value Objects 생성
            var productNameResult = ProductName.Create(request.Name);
            if (productNameResult.IsFail)
            {
                return productNameResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: e => FinResponse.Fail<Response>(e));
            }
            var productName = productNameResult.Match(Succ: v => v, Fail: _ => null!);

            var priceResult = Domain.ValueObjects.Price.Create(request.Price);
            if (priceResult.IsFail)
            {
                return priceResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: e => FinResponse.Fail<Response>(e));
            }
            var price = priceResult.Match(Succ: v => v, Fail: _ => null!);

            var stockQuantityResult = StockQuantity.Create(request.StockQuantity);
            if (stockQuantityResult.IsFail)
            {
                return stockQuantityResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: e => FinResponse.Fail<Response>(e));
            }
            var stockQuantity = stockQuantityResult.Match(Succ: v => v, Fail: _ => null!);

            // LINQ 쿼리 표현식: Repository의 FinT<IO, Product>를 사용하여 조회 및 업데이트
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            FinT<IO, Response> usecase =
                from existingProduct in _productRepository.GetById(productId)
                let updated = existingProduct.Update(productName, request.Description, price, stockQuantity)
                from updatedProduct in _productRepository.Update(updated)
                select new Response(
                    updatedProduct.Id.ToString(),
                    (string)updatedProduct.Name,
                    updatedProduct.Description,
                    (decimal)updatedProduct.Price,
                    (int)updatedProduct.StockQuantity,
                    updatedProduct.UpdatedAt ?? DateTime.UtcNow);

            // FinT<IO, Response>
            //  -Run()→           IO<Fin<Response>>
            //  -RunAsync()→      Fin<Response>
            //  -ToFinResponse()→ FinResponse<Response>
            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
