using Functorium.Applications.Usecases;
using LanguageExt;

namespace CommandUsecase;

public sealed class CreateProductCommand
{
    public sealed record Request(string Name, decimal Price);
    public sealed record Response(string ProductId, string Name, decimal Price, DateTime CreatedAt);

    public sealed class Usecase(IProductRepository productRepository)
    {
        public async Task<FinResponse<Response>> Handle(Request request)
        {
            var product = Product.Create(request.Name, request.Price);

            FinT<IO, Response> usecase =
                from created in productRepository.Create(product)
                select new Response(
                    created.Id.ToString(),
                    created.Name,
                    created.Price,
                    created.CreatedAt);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}
