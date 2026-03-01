using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace QueryUsecaseExample;

/// <summary>
/// Query Usecase 예제
/// </summary>
public sealed class GetProductQuery
{
    public sealed record Request(string ProductId) : IQueryRequest<Response>;

    public sealed record Response(
        string ProductId,
        string Name,
        decimal Price);

    /// <summary>
    /// 간단한 인메모리 Query Handler
    /// </summary>
    public sealed class Handler
    {
        private readonly Dictionary<string, Response> _products = new()
        {
            ["prod-001"] = new Response("prod-001", "Widget", 9.99m),
            ["prod-002"] = new Response("prod-002", "Gadget", 19.99m),
        };

        public FinResponse<Response> Handle(Request request)
        {
            if (_products.TryGetValue(request.ProductId, out var product))
                return product;

            return Error.New($"Product not found: {request.ProductId}");
        }
    }
}
