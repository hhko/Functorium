using Functorium.Applications.Usecases;
using LanguageExt;

namespace QueryUsecase;

public sealed class SearchProductsQuery
{
    public sealed record Request(string Keyword);
    public sealed record Response(List<ProductDto> Products);

    public sealed class Usecase(IProductQuery productQuery)
    {
        public async Task<FinResponse<Response>> Handle(Request request)
        {
            FinT<IO, Response> usecase =
                from products in productQuery.SearchByName(request.Keyword)
                select new Response(products);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}
