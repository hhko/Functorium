using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Applications.Events;
using Functorium.Applications.Linq;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// 재고 차감 Command - 트랜잭션 후 이벤트 발행 패턴 예제
/// </summary>
public sealed class DeductStockCommand
{
    /// <summary>
    /// Command Request - 재고 차감에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string ProductId,
        int Quantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 차감 후 재고 정보
    /// </summary>
    public sealed record Response(
        string ProductId,
        int RemainingStock);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("상품 ID는 필수입니다")
                .Must(id => ProductId.TryParse(id, null, out _))
                .WithMessage("유효하지 않은 상품 ID 형식입니다");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("차감 수량은 0보다 커야 합니다");
        }
    }

    /// <summary>
    /// Command Handler - 트랜잭션 후 이벤트 발행 패턴 적용
    /// </summary>
    public sealed class Usecase(
        IProductRepository productRepository,
        IDomainEventPublisher eventPublisher)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IDomainEventPublisher _eventPublisher = eventPublisher;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);
            var quantityResult = Quantity.Create(request.Quantity);

            if (quantityResult.IsFail)
            {
                return quantityResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            var quantity = (Quantity)quantityResult;

            // 트랜잭션 후 이벤트 발행 패턴:
            // 1. 조회 → 2. 도메인 로직 → 3. 영속화 → 4. 이벤트 발행
            FinT<IO, Response> usecase =
                from product in _productRepository.GetById(productId)                     // 1. 조회
                from _1 in product.DeductStock(quantity)                                  // 2. 도메인 로직 (이벤트 추가됨)
                from updated in _productRepository.Update(product)                        // 3. 영속화
                from _2 in _eventPublisher.PublishEvents(updated, cancellationToken)      // 4. 이벤트 발행
                select new Response(
                    updated.Id.ToString(),
                    updated.StockQuantity);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
