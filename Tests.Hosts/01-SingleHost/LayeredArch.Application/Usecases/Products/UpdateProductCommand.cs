using LayeredArch.Domain.Entities;
using LayeredArch.Domain.ValueObjects;
using LayeredArch.Domain.Repositories;
using Functorium.Applications.Linq;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// 상품 업데이트 Command - Entity Guide의 Apply 패턴 + Exception Pipeline 데모
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
                .NotEmpty().WithMessage("상품 ID는 필수입니다")
                .Must(id => ProductId.TryParse(id, null, out _)).WithMessage("유효하지 않은 상품 ID 형식입니다");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(ProductName.MaxLength).WithMessage($"상품명은 {ProductName.MaxLength}자를 초과할 수 없습니다");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    /// <summary>
    /// Command Handler - Entity Guide의 Apply 패턴 적용
    /// </summary>
    public sealed class Usecase(IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 예외 시뮬레이션 - UsecaseExceptionPipeline 데모용
            if (request.SimulateException)
            {
                throw new InvalidOperationException("시뮬레이션된 예외: 데모 목적으로 발생된 예외입니다");
            }

            // 1. Value Object 생성 + VO 없는 필드 검증 (Apply 패턴)
            var updateData = CreateUpdateData(request);
            if (updateData.IsFail)
            {
                return updateData.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 2. 조회 및 업데이트
            var productId = ProductId.Create(request.ProductId);
            var (name, description, price, stockQuantity) = (UpdateData)updateData;

            FinT<IO, Response> usecase =
                from existingProduct in _productRepository.GetById(productId)
                from updatedProduct in _productRepository.Update(
                    existingProduct.Update(name, description, price, stockQuantity))
                select new Response(
                    updatedProduct.Id.ToString(),
                    updatedProduct.Name,
                    updatedProduct.Description,
                    updatedProduct.Price,
                    updatedProduct.StockQuantity,
                    updatedProduct.UpdatedAt ?? DateTime.UtcNow);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        /// <summary>
        /// Entity Guide 패턴: VO Validate() + Named Context 검증 + Apply 병합
        /// </summary>
        private static Fin<UpdateData> CreateUpdateData(Request request)
        {
            // VO가 있는 필드: Validate() 사용 (Validation<Error, T> 반환)
            var name = ProductName.Validate(request.Name);
            var price = Money.Validate(request.Price);
            var stockQuantity = Quantity.Validate(request.StockQuantity);

            // VO가 없는 필드: Named Context 사용
            var description = ValidationRules.For("Description")
                .NotNull(request.Description)
                .ThenMaxLength(500);

            // 모두 튜플로 병합 - Apply로 병렬 검증
            return (name, price, stockQuantity, description.Value)
                .Apply((name, price, stockQuantity, description) =>
                    new UpdateData(
                        ProductName.Create(name).ThrowIfFail(),
                        description,
                        Money.Create(price).ThrowIfFail(),
                        Quantity.Create(stockQuantity).ThrowIfFail()))
                .As()
                .ToFin();
        }

        private sealed record UpdateData(
            ProductName Name,
            string Description,
            Money Price,
            Quantity StockQuantity);
    }
}
