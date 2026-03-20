using Functorium.Applications.Usecases;

namespace ObservabilityHost.Usecases;

public sealed class PlaceOrderCommand
{
    public sealed record OrderLine(string ProductId, int Quantity, decimal UnitPrice);
    public sealed record Request(string CustomerId, List<OrderLine> Lines) : ICommandRequest<Response>, ICustomerRequest;
    public sealed record Response(string OrderId, int LineCount, decimal TotalAmount);

    public sealed class Usecase : ICommandUsecase<Request, Response>
    {
        public ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
        {
            decimal total = request.Lines.Sum(l => l.Quantity * l.UnitPrice);
            return ValueTask.FromResult(
                FinResponse.Succ(new Response(Guid.NewGuid().ToString(), request.Lines.Count, total)));
        }
    }
}
