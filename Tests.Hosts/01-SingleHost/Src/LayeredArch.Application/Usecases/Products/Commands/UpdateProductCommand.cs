using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.AggregateRoots.Products.Specifications;
using Functorium.Applications.Errors;
using Functorium.Applications.Linq;
using static Functorium.Applications.Errors.ApplicationErrorKind;

namespace LayeredArch.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 업데이트 Command
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
        decimal Price,
        bool SimulateException = false) : ICommandRequest<Response>;

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
    /// Command Handler
    /// </summary>
    public sealed class Usecase(
        IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 예외 시뮬레이션 - UsecaseExceptionPipeline 데모용
            if (request.SimulateException)
            {
                throw new InvalidOperationException("Simulated exception: raised for demo purposes");
            }

            var productId = ProductId.Create(request.ProductId);

            // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
            FinT<IO, Response> usecase =
                from vos in (
                    ProductName.Create(request.Name),
                    ProductDescription.Create(request.Description),
                    Money.Create(request.Price)
                ).ApplyT((name, desc, price) => (Name: name, Desc: desc, Price: price))
                from existingProduct in _productRepository.GetById(productId)
                from exists in _productRepository.Exists(new ProductNameUniqueSpec(vos.Name, productId))
                from _ in guard(!exists, ApplicationError.For<UpdateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from updated in existingProduct.Update(vos.Name, vos.Desc, vos.Price)
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
    }
}
