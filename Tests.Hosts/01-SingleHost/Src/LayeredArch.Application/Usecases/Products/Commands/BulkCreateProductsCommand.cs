using LayeredArch.Domain.AggregateRoots.Inventories;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Commands;

/// <summary>
/// 상품 벌크 생성 Command - CreateRange + IDomainEventBatchHandler 데모
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
        IInventoryRepository inventoryRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // 1. 각 항목에서 Product + Quantity 생성
            var products = new List<Product>();
            var quantities = new List<Quantity>();

            foreach (var item in request.Products)
            {
                var name = ProductName.Create(item.Name).ThrowIfFail();
                var description = ProductDescription.Create(item.Description).ThrowIfFail();
                var price = Money.Create(item.Price).ThrowIfFail();
                var quantity = Quantity.Create(item.StockQuantity).ThrowIfFail();

                products.Add(Product.Create(name, description, price));
                quantities.Add(quantity);
            }

            // 2. 벌크 저장 (CreateRange → N개 CreatedEvent 발생 → BatchHandler 1회 호출)
            var inventories = products.Select((p, i) =>
                Inventory.Create(p.Id, quantities[i])).ToList();

            FinT<IO, Response> usecase =
                from createdProducts in productRepository.CreateRange(products)
                //from createdInventories in inventoryRepository.CreateRange(inventories)
                select new Response(
                    createdProducts.Count,
                    createdProducts.Select(p => p.Id.ToString()).ToList());

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
