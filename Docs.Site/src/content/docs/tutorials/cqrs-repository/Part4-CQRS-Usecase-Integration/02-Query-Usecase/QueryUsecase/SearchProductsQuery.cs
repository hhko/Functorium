using Functorium.Applications.Queries;
using Functorium.Applications.Usecases;
using LanguageExt;

namespace QueryUsecase;

public sealed class SearchProductsQuery
{
    public sealed record Request(string Keyword, PageRequest Page, SortExpression Sort)
        : IQueryRequest<Response>;
    public sealed record Response(PagedResult<ProductDto> Products);

    public sealed class Usecase(IProductQuery productQuery)
        : IQueryUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
        {
            var spec = new ProductNameSpec(request.Keyword);

            FinT<IO, Response> usecase =
                from products in productQuery.Search(spec, request.Page, request.Sort)
                select new Response(products);

            Fin<Response> result = await usecase.Run().RunAsync();
            return result.ToFinResponse();
        }
    }
}
