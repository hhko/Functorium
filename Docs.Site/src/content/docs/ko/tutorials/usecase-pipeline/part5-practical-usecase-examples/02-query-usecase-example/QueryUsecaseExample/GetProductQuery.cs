using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace QueryUsecaseExample;

/// <summary>
/// Query Usecase 예제
/// </summary>
public sealed class GetProductQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>, ICacheable
    {
        public string CacheKey => $"product:{ProductId}";
        public TimeSpan? Duration => TimeSpan.FromMinutes(5);
    }

    public sealed record Response(
        string ProductId,
        string Name,
        decimal Price);

    /// <summary>
    /// 간단한 인메모리 Query Handler
    /// </summary>
    public sealed class Handler : IQueryUsecase<Request, Response>
    {
        private readonly Dictionary<string, Response> _products = new()
        {
            ["prod-001"] = new Response("prod-001", "Widget", 9.99m),
            ["prod-002"] = new Response("prod-002", "Gadget", 19.99m),
        };

        public ValueTask<FinResponse<Response>> Handle(Request query, CancellationToken cancellationToken)
        {
            FinResponse<Response> result = _products.TryGetValue(query.ProductId, out var product)
                ? product
                : Error.New($"Product not found: {query.ProductId}");

            return new ValueTask<FinResponse<Response>>(result);
        }
    }
}
