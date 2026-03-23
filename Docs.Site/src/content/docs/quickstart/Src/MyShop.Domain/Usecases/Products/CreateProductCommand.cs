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
            var name = ProductName.Create(request.Name).ThrowIfFail();
            var price = Money.Create(request.Price).ThrowIfFail();

            var product = Product.Create(name, price);

            FinT<IO, Response> usecase =
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
