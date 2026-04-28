namespace ApplicationLayerRules.Applications;

public sealed class GetOrderById
{
    public sealed record Request(Guid OrderId);
    public sealed record Response(Guid OrderId, string CustomerName);

    public sealed class Usecase : IQueryUsecase<Request, Response>
    {
        public Task<Response> ExecuteAsync(Request request)
            => Task.FromResult(new Response(request.OrderId, "Test"));
    }
}
