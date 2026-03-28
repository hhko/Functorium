namespace MyShop.Domain.Usecases.Products;

public sealed class CreateProductCommand
{
    public sealed record Request(string Name, decimal Price) : ICommandRequest<Response>;

    public sealed record Response(string ProductId, string Name, decimal Price);

    public sealed class Usecase(IProductRepository productRepository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            // 파이프라인 Validator가 검증 완료. Create()는 정규화 목적.
            // Unwrap 패턴:
            //   var name = ProductName.Create(request.Name).Unwrap();
            //   var price = Money.Create(request.Price).Unwrap();
            //   var product = Product.Create(name, price);

            // ApplyT: VO 합성 + 에러 수집 → FinT<IO, R> LINQ from 첫 구문
            FinT<IO, Response> usecase =
                from vos in (
                    ProductName.Create(request.Name),
                    Money.Create(request.Price)
                ).ApplyT((name, price) => (Name: name, Price: price))
                let product = Product.Create(vos.Name, vos.Price)
                from created in productRepository.Create(product)
                select new Response(
                    created.Id.ToString(),
                    created.Name,
                    created.Price);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
