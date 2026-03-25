using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 벌크 생성 Command - ProductBulkOperations Domain Service 데모
/// </summary>
public sealed class BulkCreateProductsCommand
{
    public sealed record ProductItem(string Name, string Description, decimal Price, int StockQuantity);

    public sealed record Request(List<ProductItem> Products) : ICommandRequest<Response>;

    public sealed record Response(int CreatedCount, List<string> ProductIds);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Products).NotEmpty().WithMessage("최소 1개 이상의 상품이 필요합니다");
            RuleForEach(x => x.Products).ChildRules(item =>
            {
                item.RuleFor(x => x.Name).MustSatisfyValidation(ProductName.Validate);
                item.RuleFor(x => x.Description).MustSatisfyValidation(ProductDescription.Validate);
                item.RuleFor(x => x.Price).MustSatisfyValidation(Money.Validate);
                item.RuleFor(x => x.StockQuantity).MustSatisfyValidation(Quantity.Validate);
            });
        }
    }

    public sealed class Usecase(
        IProductRepository productRepository,
        IDomainEventCollector eventCollector)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. 각 항목에서 VO 합성 + Product 생성
            var products = new List<Product>();

            foreach (var item in request.Products)
            {
                var vos = ApplyExtensions.Apply(
                    (ProductName.Create(item.Name),
                     ProductDescription.Create(item.Description),
                     Money.Create(item.Price)),
                    (name, desc, price) => (Name: name, Desc: desc, Price: price)).Unwrap();

                products.Add(Product.Create(vos.Name, vos.Desc, vos.Price));
            }

            // 2. Domain Service로 벌크 이벤트 생성 + 개별 이벤트 정리
            var bulkResult = ProductBulkOperations.BulkCreate(products);
            eventCollector.TrackEvent(bulkResult.Event);

            // 3. 벌크 저장
            FinT<IO, Response> usecase =
                from createdProducts in productRepository.CreateRange(bulkResult.Created.ToList())
                select new Response(
                    createdProducts.Count,
                    createdProducts.Select(p => p.Id.ToString()).ToList());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
