using LayeredArch.Domain.Entities;
using LayeredArch.Domain.ValueObjects;
using LayeredArch.Domain.Repositories;
using Functorium.Abstractions.Errors;
using Functorium.Applications.Linq;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// 상품 생성 Command - Entity Guide의 Apply 패턴 데모
/// Value Object 생성 + Named Context 검증 + Apply 병합 패턴 적용
/// </summary>
public sealed class CreateProductCommand
{
    /// <summary>
    /// Command Request - 상품 생성에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 상품 정보
    /// </summary>
    public sealed record Response(
        string ProductId,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        DateTime CreatedAt);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙 (Presentation Layer 검증)
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("상품명은 필수입니다")
                .MaximumLength(ProductName.MaxLength).WithMessage($"상품명은 {ProductName.MaxLength}자를 초과할 수 없습니다");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("설명은 500자를 초과할 수 없습니다");

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
            // 1. Value Object 생성 + VO 없는 필드 검증 (Apply 패턴)
            var productResult = CreateProduct(request);

            // 2. 검증 실패 시 조기 반환
            if (productResult.IsFail)
            {
                return productResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 3. 중복 검사 및 저장
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(request.Name)
                from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
                from product in _productRepository.Create((Product)productResult)
                select new Response(
                    product.Id.ToString(),
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        /// <summary>
        /// Entity Guide 패턴: VO Validate() + Named Context 검증 + Apply 병합
        /// Validation 타입을 사용하여 병렬 검증 후 Entity 생성
        /// </summary>
        private static Fin<Product> CreateProduct(Request request)
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
                    Product.Create(
                        ProductName.Create(name).ThrowIfFail(),
                        description,
                        Money.Create(price).ThrowIfFail(),
                        Quantity.Create(stockQuantity).ThrowIfFail()))
                .As()
                .ToFin();
        }
    }

    /// <summary>
    /// ApplicationErrors - Application 계층 오류 정의
    /// </summary>
    internal static class ApplicationErrors
    {
        public static Error ProductNameAlreadyExists(string productName) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
                errorCurrentValue: productName,
                errorMessage: $"Product name already exists. Current value: '{productName}'");
    }
}
