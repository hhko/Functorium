using LayeredArch.Domain.AggregateRoots.Products;
using Functorium.Applications.Errors;
using Functorium.Applications.Events;
using Functorium.Applications.Linq;
using Functorium.Applications.Persistence;
using static Functorium.Applications.Errors.ApplicationErrorType;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// 상품 생성 Command - Entity Guide의 Apply 패턴 데모
/// Value Object 생성 + Apply 병합 패턴 적용
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
                .MaximumLength(ProductDescription.MaxLength).WithMessage($"설명은 {ProductDescription.MaxLength}자를 초과할 수 없습니다");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("가격은 0보다 커야 합니다");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("재고 수량은 0 이상이어야 합니다");
        }
    }

    /// <summary>
    /// Command Handler - Entity Guide의 Apply 패턴 적용
    /// </summary>
    public sealed class Usecase(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IDomainEventPublisher _eventPublisher = eventPublisher;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. Value Object 생성 (Apply 패턴)
            var productResult = CreateProduct(request);

            // 2. 검증 실패 시 조기 반환
            if (productResult.IsFail)
            {
                return productResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            // 3. ProductName 생성 (중복 검사용)
            var productName = ProductName.Create(request.Name).ThrowIfFail();

            // 4. 중복 검사 및 저장
            FinT<IO, Response> usecase =
                from exists in _productRepository.ExistsByName(productName)
                from _ in guard(!exists, ApplicationError.For<CreateProductCommand>(
                    new AlreadyExists(),
                    request.Name,
                    $"Product name already exists: '{request.Name}'"))
                from product in _productRepository.Create((Product)productResult)
                from _1 in _unitOfWork.SaveChanges(cancellationToken)
                from _2 in _eventPublisher.PublishEvents(product, cancellationToken)
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
        /// Entity Guide 패턴: VO Validate() + Apply 병합
        /// Validation 타입을 사용하여 병렬 검증 후 Entity 생성
        /// </summary>
        private static Fin<Product> CreateProduct(Request request)
        {
            // 모든 필드: VO Validate() 사용 (Validation<Error, T> 반환)
            var name = ProductName.Validate(request.Name);
            var description = ProductDescription.Validate(request.Description);
            var price = Money.Validate(request.Price);
            var stockQuantity = Quantity.Validate(request.StockQuantity);

            // 모두 튜플로 병합 - Apply로 병렬 검증
            return (name, description, price, stockQuantity)
                .Apply((n, d, p, s) =>
                    Product.Create(
                        ProductName.Create(n).ThrowIfFail(),
                        ProductDescription.Create(d).ThrowIfFail(),
                        Money.Create(p).ThrowIfFail(),
                        Quantity.Create(s).ThrowIfFail()))
                .As()
                .ToFin();
        }
    }
}
