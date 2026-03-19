using Functorium.Adapters.Observabilities.Pipelines;

using Serilog.Context;

namespace ObservabilityHost.Usecases;

public sealed class PlaceOrderLogEnricher : IUsecaseLogEnricher<PlaceOrderCommand.Request>
{
    public IDisposable? EnrichRequestLog(PlaceOrderCommand.Request request)
    {
        var d1 = LogContext.PushProperty("CustomerId", request.CustomerId);
        var d2 = LogContext.PushProperty("OrderLineCount", request.Lines.Count);
        return new CompositeDisposable(d1, d2);
    }

    public IDisposable? EnrichResponseLog(PlaceOrderCommand.Request request)
    {
        decimal total = request.Lines.Sum(l => l.Quantity * l.UnitPrice);
        return LogContext.PushProperty("OrderTotalAmount", total);
    }

    private sealed class CompositeDisposable(params IDisposable[] disposables) : IDisposable
    {
        public void Dispose()
        {
            foreach (var d in disposables)
                d.Dispose();
        }
    }
}
