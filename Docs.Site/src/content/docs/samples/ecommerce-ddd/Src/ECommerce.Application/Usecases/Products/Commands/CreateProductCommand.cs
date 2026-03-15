using ECommerce.Domain.AggregateRoots.Inventories;
using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Products.Specifications;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace ECommerce.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 생성 Command - Entity Guide의 Apply 패턴 데모
/// Value Object 생성 + Apply 병합 패턴 적용
/// Product 생성 후 Inventory도 함께 생성합니다.
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
            RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
            RuleFor(x => x.Description).MustSatisfyValidation(ProductDescription.Validate);
            RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);
            RuleFor(x => x.StockQuantity).MustSatisfyValidation(Quantity.Validate);
        }
    }

    /// <summary>
    /// Command Handler - Entity Guide의 Apply 패턴 적용
    /// Product 생성 후 Inventory도 함께 생성합니다.
    /// </summary>
    public sealed class Usecase(
        IProductRepository productRepository,
        IInventoryRepository inventoryRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IInventoryRepository _inventoryRepository = inventoryRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 생성 (Apply 패턴)
            var createData = CreateProductData(request);

            // 2. 검증 실패 시 조기 반환
            if (createData.IsFail)
            {
                return createData.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 3. ProductName 생성 (중복 검사용)
            var productName = ProductName.Create(request.Name).ThrowIfFail();

            // 4. 중복 검사, Product 저장, Inventory 생성
            var (product, stockQuantity) = (ProductData)createData;

            FinT<IO, Response> usecase =
                from exists in _productRepository.Exists(new ProductNameUniqueSpec(productName))
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from createdProduct in _productRepository.Create(product)
                from createdInventory in _inventoryRepository.Create(
                    Inventory.Create(createdProduct.Id, stockQuantity))
                select new Response(
                    createdProduct.Id.ToString(),
                    createdProduct.Name,
                    createdProduct.Description,
                    createdProduct.Price,
                    createdInventory.StockQuantity,
                    createdProduct.CreatedAt);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }

        /// <summary>
        /// Entity Guide 패턴: VO Validate() + Apply 병합
        /// Validation 타입을 사용하여 병렬 검증 후 Entity 생성
        /// </summary>
        private static Fin<ProductData> CreateProductData(Request request)
        {
            // 모든 필드: VO Validate() 사용 (Validation<Error, T> 반환)
            var name = ProductName.Validate(request.Name);
            var description = ProductDescription.Validate(request.Description);
            var price = Money.Validate(request.Price);
            var stockQuantity = Quantity.Validate(request.StockQuantity);

            // 모두 튜플로 병합 - Apply로 병렬 검증
            return (name, description, price, stockQuantity)
                .Apply((n, d, p, s) =>
                    new ProductData(
                        Product.Create(
                            ProductName.Create(n).ThrowIfFail(),
                            ProductDescription.Create(d).ThrowIfFail(),
                            Money.Create(p).ThrowIfFail()),
                        Quantity.Create(s).ThrowIfFail()))
                .As()
                .ToFin();
        }

        private sealed record ProductData(Product Product, Quantity StockQuantity);
    }
}
