using Functorium.Applications.Usecases;

namespace ObservabilityHost.Usecases;

public sealed class GetOrderSummaryQuery
{
    public sealed record Request(string OrderId) : IQueryRequest<Response>;
    public sealed record Response(string OrderId, string Status);

    public sealed class Usecase : IQueryUsecase<Request, Response>
    {
        public ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
        {
            return ValueTask.FromResult(
                FinResponse.Succ(new Response(request.OrderId, "Completed")));
        }
    }
}
