using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorKind;

namespace LayeredArch.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 생성 Command
/// Value Object 생성 + Product 생성 후 Inventory도 함께 생성합니다.
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
    /// Command Handler
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
            // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
            FinT<IO, Response> usecase =
                from vos in (
                    ProductName.Create(request.Name),
                    ProductDescription.Create(request.Description),
                    Money.Create(request.Price),
                    Quantity.Create(request.StockQuantity)
                ).ApplyT((name, desc, price, qty) => (Name: name, Desc: desc, Price: price, Qty: qty))
                let product = Product.Create(vos.Name, vos.Desc, vos.Price)
                from exists in _productRepository.Exists(new ProductNameUniqueSpec(vos.Name))
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from createdProduct in _productRepository.Create(product)
                from createdInventory in _inventoryRepository.Create(
                    Inventory.Create(createdProduct.Id, vos.Qty))
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
    }
}
