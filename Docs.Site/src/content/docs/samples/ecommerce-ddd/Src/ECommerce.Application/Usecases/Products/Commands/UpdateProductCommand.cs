using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace ECommerce.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 업데이트 Command - Entity Guide의 Apply 패턴
/// 재고(StockQuantity)는 Inventory Aggregate 관할이므로 제외됩니다.
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
        decimal Price) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 업데이트된 상품 정보
    /// </summary>
    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        DateTime UpdatedAt);

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

            RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
            RuleFor(x => x.Description).MustSatisfyValidation(ProductDescription.Validate);
            RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);
        }
    }

    /// <summary>
    /// Command Handler - Entity Guide의 Apply 패턴 적용
    /// </summary>
    public sealed class Usecase(
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 생성 (Apply 패턴)
            var updateData = CreateUpdateData(request);
            if (updateData.IsFail)
            {
                return updateData.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 2. 조회 및 업데이트
            var productId = ProductId.Create(request.ProductId);
            var (name, description, price) = (UpdateData)updateData;

            // 3. 중복 검사 및 업데이트
            FinT<IO, Response> usecase =
                from existingProduct in _productRepository.GetById(productId)
                from exists in _productRepository.Exists(new ProductNameUniqueSpec(name, productId))
                from _ in guard(!exists, ApplicationError.For<UpdateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from updated in existingProduct.Update(name, description, price)
                from updatedProduct in _productRepository.Update(updated)
                select new Response(
                    updatedProduct.Id.ToString(),
                    updatedProduct.Name,
                    updatedProduct.Description,
                    updatedProduct.Price,
                    updatedProduct.UpdatedAt.IfNone(DateTime.UtcNow));

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        /// <summary>
        /// Entity Guide 패턴: VO Validate() + Apply 병합
        /// </summary>
        private static Fin<UpdateData> CreateUpdateData(Request request)
        {
            // 모든 필드: VO Validate() 사용 (Validation<Error, T> 반환)
            var name = ProductName.Validate(request.Name);
            var description = ProductDescription.Validate(request.Description);
            var price = Money.Validate(request.Price);

            // 모두 튜플로 병합 - Apply로 병렬 검증
            return (name, description, price)
                .Apply((n, d, p) =>
                    new UpdateData(
                        ProductName.Create(n).ThrowIfFail(),
                        ProductDescription.Create(d).ThrowIfFail(),
                        Money.Create(p).ThrowIfFail()))
                .As()
                .ToFin();
        }

        private sealed record UpdateData(
            ProductName Name,
            ProductDescription Description,
            Money Price);
    }
}
